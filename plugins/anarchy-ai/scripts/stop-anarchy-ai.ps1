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

$runtimePath = Join-Path $PluginRoot 'runtime\win-x64\AnarchyAi.Mcp.Server.exe'
$matchingProcesses = @()
$actionsTaken = New-Object System.Collections.Generic.List[string]
$stopErrors = New-Object System.Collections.Generic.List[string]

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
  plugin_root = $PluginRoot
  runtime_path = $runtimePath
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
