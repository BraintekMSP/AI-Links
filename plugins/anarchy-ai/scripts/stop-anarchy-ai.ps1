<#
.SYNOPSIS
Assesses or releases the runtime lock held by the bundled Anarchy-AI MCP server process.
.DESCRIPTION
Provides a bounded recovery lane for detecting the installed runtime process, attempting a safe stop,
and optionally retrying with UAC elevation when a forced stop hits access-denied conditions.
.PARAMETER Mode
Selects whether the script only inspects the lock, performs a safe stop, or performs a forced stop with optional elevation.
.PARAMETER PluginRoot
Overrides the plugin root whose bundled runtime should be inspected.
.PARAMETER ElevatedRetry
Marks the invocation as the elevated retry path so the script does not loop on RunAs.
.OUTPUTS
JSON describing runtime presence, matching processes, actions taken, stop errors, nested paths, and runtime lock state.
.NOTES
Critical dependencies: the generated path canon psd1, the bundled runtime executable path, Windows process inspection, and Stop-Process.
#>
param(
  [ValidateSet('AssessRuntimeLock','SafeReleaseRuntimeLock','ForceReleaseRuntimeLock')]
  [string]$Mode = 'AssessRuntimeLock',
  [string]$PluginRoot = '',
  [switch]$ElevatedRetry
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($PluginRoot)) {
  $PluginRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir '..'))
}
else {
  $PluginRoot = [System.IO.Path]::GetFullPath($PluginRoot)
}

$pathCanonPath = Join-Path $PluginRoot 'pathing\anarchy-path-canon.generated.psd1'
if (-not (Test-Path $pathCanonPath)) {
  throw "Path canon artifact not found: $pathCanonPath"
}

$pathCanon = Import-PowerShellDataFile -Path $pathCanonPath

<#
.SYNOPSIS
Resolves a canon-relative path against a supplied root.
.DESCRIPTION
Normalizes slash direction and returns an absolute path for bundle-relative surfaces.
.PARAMETER RootPath
Absolute root path used as the base for resolution.
.PARAMETER RelativePath
Canon-relative path fragment to resolve beneath the root.
.OUTPUTS
System.String. Absolute filesystem path.
.NOTES
Critical dependencies: the generated path canon convention and Join-Path/GetFullPath.
#>
function Resolve-CanonRelativePath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath,
    [Parameter(Mandatory = $true)]
    [string]$RelativePath
  )

  $normalizedRelativePath = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
  return [System.IO.Path]::GetFullPath((Join-Path $RootPath $normalizedRelativePath))
}

$runtimePath = Resolve-CanonRelativePath -RootPath $PluginRoot -RelativePath $pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
$matchingProcesses = @()
$actionsTaken = New-Object System.Collections.Generic.List[string]
$stopErrors = New-Object System.Collections.Generic.List[string]

<#
.SYNOPSIS
Reports whether the current process token has administrator rights.
.DESCRIPTION
Checks the current Windows identity and principal so force-stop logic knows whether a UAC retry is needed.
.OUTPUTS
System.Boolean. True when the current token is an administrator role token.
.NOTES
Critical dependencies: WindowsIdentity, WindowsPrincipal, and the local Windows security model.
#>
function Test-IsAdministrator {
  try {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
  }
  catch {
    return $false
  }
}

<#
.SYNOPSIS
Finds running Anarchy-AI runtime processes that match the expected bundled executable path.
.DESCRIPTION
Enumerates candidate processes by name, filters them to the installed runtime path, and returns bounded metadata.
.PARAMETER ExpectedRuntimePath
Absolute runtime executable path that should be considered in-scope for this install.
.OUTPUTS
System.Object[]. Matching process records with id, name, executable path, and optional command line.
.NOTES
Critical dependencies: Get-Process, process.Path availability, and the bundled runtime path resolved from the path canon.
#>
function Get-MatchingRuntimeProcesses {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRuntimePath
  )

  $results = @()
  try {
    $candidateProcesses = @(Get-Process -Name 'AnarchyAi.Mcp.Server' -ErrorAction SilentlyContinue)
    foreach ($candidate in $candidateProcesses) {
      $candidatePath = $null
      try {
        $candidatePath = $candidate.Path
      }
      catch {
        continue
      }

      if ($candidatePath -eq $ExpectedRuntimePath) {
        $results += [pscustomobject]@{
          ProcessId = $candidate.Id
          Name = $candidate.ProcessName
          ExecutablePath = $candidatePath
          CommandLine = $null
        }
      }
    }
  }
  catch {
    $script:stopErrors.Add($_.Exception.Message)
  }

  return @($results)
}

<#
.SYNOPSIS
Attempts to stop the supplied runtime processes.
.DESCRIPTION
Calls Stop-Process for each matching process, recording successful stops and failure messages for later reporting.
.PARAMETER ProcessesToStop
Process records previously returned by Get-MatchingRuntimeProcesses.
.OUTPUTS
No direct return value. Mutates the script-scoped action and error collections.
.NOTES
Critical dependencies: Stop-Process and the script-scoped actionsTaken and stopErrors collections.
#>
function Invoke-StopRuntimeProcesses {
  param(
    [Parameter(Mandatory = $true)]
    [object[]]$ProcessesToStop
  )

  foreach ($process in $ProcessesToStop) {
    try {
      Stop-Process -Id $process.ProcessId -ErrorAction Stop
      $script:actionsTaken.Add("stopped_process:$($process.ProcessId)")
    }
    catch {
      $script:stopErrors.Add("failed_to_stop_process:$($process.ProcessId):$($_.Exception.Message)")
    }
  }
}

try {
  $matchingProcesses = @(Get-MatchingRuntimeProcesses -ExpectedRuntimePath $runtimePath)
}
catch {
  $stopErrors.Add($_.Exception.Message)
}

if (($Mode -eq 'SafeReleaseRuntimeLock' -or $Mode -eq 'ForceReleaseRuntimeLock') -and $matchingProcesses.Count -gt 0) {
  Invoke-StopRuntimeProcesses -ProcessesToStop $matchingProcesses

  $matchingProcesses = @(Get-MatchingRuntimeProcesses -ExpectedRuntimePath $runtimePath)

  $hasAccessDenied = @($stopErrors | Where-Object { $_ -like '*Access is denied*' -or $_ -like '*access denied*' }).Count -gt 0
  if ($Mode -eq 'ForceReleaseRuntimeLock' -and $hasAccessDenied -and -not $ElevatedRetry -and -not (Test-IsAdministrator)) {
    try {
      $actionsTaken.Add('requested_uac_elevation_for_stop')
      $powershellExe = Join-Path $PSHOME 'powershell.exe'
      $elevatedArgs = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-Mode', 'ForceReleaseRuntimeLock',
        '-PluginRoot', $PluginRoot,
        '-ElevatedRetry'
      )
      $elevatedProcess = Start-Process -FilePath $powershellExe -ArgumentList $elevatedArgs -Verb RunAs -Wait -PassThru
      $actionsTaken.Add("elevated_retry_exit_code:$($elevatedProcess.ExitCode)")
      $matchingProcesses = @(Get-MatchingRuntimeProcesses -ExpectedRuntimePath $runtimePath)
      if ($elevatedProcess.ExitCode -eq 0) {
        $stopErrors.Clear()
      }
    }
    catch {
      $stopErrors.Add("elevation_request_failed:$($_.Exception.Message)")
    }
  }
}

$result = [ordered]@{
  mode = $Mode
  runtime_present = Test-Path $runtimePath
  matching_process_count = $matchingProcesses.Count
  matching_processes = @(
    $matchingProcesses | ForEach-Object {
      [ordered]@{
        process_id = $_.ProcessId
        name = $_.Name
        executable_path = $_.ExecutablePath
        command_line = $_.CommandLine
      }
    }
  )
  actions_taken = @($actionsTaken)
  stop_errors = @($stopErrors)
  paths = [ordered]@{
    source = [ordered]@{
      root_path = $PluginRoot
      files = [ordered]@{
        runtime_executable_file_path = $runtimePath
      }
      relative = [ordered]@{
        runtime_executable_file_relative_path = [string]$pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
      }
    }
  }
  runtime_lock_state = if ($Mode -eq 'AssessRuntimeLock') {
    if ($matchingProcesses.Count -gt 0) { 'running' } else { 'not_running' }
  }
  else {
    if ($stopErrors.Count -gt 0) {
      'stop_failed'
    }
    elseif ($matchingProcesses.Count -gt 0) {
      'still_running'
    }
    else {
      'stopped'
    }
  }
}

$result | ConvertTo-Json -Depth 10

if (($Mode -eq 'SafeReleaseRuntimeLock' -or $Mode -eq 'ForceReleaseRuntimeLock') -and $result.runtime_lock_state -ne 'stopped') {
  exit 1
}

exit 0
