param(
  [ValidateSet('Release', 'Debug')]
  [string]$Configuration = 'Release',
  [string]$DotnetPath = '',
  [switch]$SkipCopyToPlugins
)

$ErrorActionPreference = 'Stop'

function Resolve-DotnetPath {
  param(
    [string]$RequestedPath
  )

  function Test-DotnetSdkAvailable {
    param(
      [string]$CandidatePath
    )

    try {
      $sdkOutput = & $CandidatePath --list-sdks 2>$null
      return -not [string]::IsNullOrWhiteSpace(($sdkOutput | Out-String).Trim())
    }
    catch {
      return $false
    }
  }

  if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
    $resolvedRequestedPath = [System.IO.Path]::GetFullPath($RequestedPath)
    if (-not (Test-Path $resolvedRequestedPath)) {
      throw "Requested dotnet path not found: $resolvedRequestedPath"
    }

    if (-not (Test-DotnetSdkAvailable -CandidatePath $resolvedRequestedPath)) {
      throw "Requested dotnet path does not report an installed SDK: $resolvedRequestedPath"
    }

    return $resolvedRequestedPath
  }

  $candidates = @(
    (Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'),
    'C:\Program Files\dotnet\dotnet.exe'
  ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

  foreach ($candidate in $candidates) {
    if ((Test-Path $candidate) -and (Test-DotnetSdkAvailable -CandidatePath $candidate)) {
      return $candidate
    }
  }

  throw 'Could not resolve a dotnet.exe with an installed SDK. Install the .NET SDK or pass -DotnetPath explicitly.'
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$setupRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $setupRoot '..\..'))
$projectPath = Join-Path $setupRoot 'dotnet\AnarchyAi.Setup.csproj'
$pluginsRoot = Join-Path $repoRoot 'plugins'
$targetExePath = Join-Path $pluginsRoot 'AnarchyAi.Setup.exe'

$tempRoot = Join-Path $env:LOCALAPPDATA 'Temp\ai-links-setup-build'
$objRoot = Join-Path $tempRoot 'obj'
$binRoot = Join-Path $tempRoot 'bin'
$publishRoot = Join-Path $tempRoot 'publish'
$statusPath = Join-Path $tempRoot 'last-build-result.json'
$objRootMsbuild = (($objRoot -replace '\\', '/') + '/')
$binRootMsbuild = (($binRoot -replace '\\', '/') + '/')

function Write-BuildResult {
  param(
    [hashtable]$Result
  )

  $json = $Result | ConvertTo-Json -Depth 10
  $statusDirectory = Split-Path -Parent $statusPath
  if (-not (Test-Path $statusDirectory)) {
    New-Item -ItemType Directory -Path $statusDirectory -Force | Out-Null
  }

  Set-Content -Path $statusPath -Value $json
  Write-Output $json
}

$resolvedDotnetPath = ''
$publishedExePath = ''

try {
  $resolvedDotnetPath = Resolve-DotnetPath -RequestedPath $DotnetPath

  if (-not (Test-Path $projectPath)) {
    throw "Setup project not found: $projectPath"
  }

  if (-not (Test-Path $pluginsRoot)) {
    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
  }

  if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
  }

  $publishArguments = @(
    'publish',
    $projectPath,
    '-c', $Configuration,
    ('-p:BaseIntermediateOutputPath="{0}"' -f $objRootMsbuild),
    ('-p:BaseOutputPath="{0}"' -f $binRootMsbuild),
    '-p:NuGetAudit=false',
    '-o', ('"{0}"' -f $publishRoot)
  )

  & $resolvedDotnetPath @publishArguments
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
  }

  $publishedExePath = Join-Path $publishRoot 'AnarchyAi.Setup.exe'
  if (-not (Test-Path $publishedExePath)) {
    throw "Published setup executable not found: $publishedExePath"
  }

  if (-not $SkipCopyToPlugins) {
    try {
      Copy-Item $publishedExePath $targetExePath -Force
    }
    catch {
      $errorMessage = $_.Exception.Message
      $isLockError = $errorMessage -like '*being used by another process*'
      $failureResult = [ordered]@{
        status = 'failed'
        failure_stage = 'copy_to_plugins'
        project_path = $projectPath
        dotnet_path = $resolvedDotnetPath
        configuration = $Configuration
        publish_root = $publishRoot
        published_executable = $publishedExePath
        copy_to_plugins_requested = $true
        copied_to_plugins = $false
        target_executable = $targetExePath
        target_executable_stale = (Test-Path $targetExePath)
        status_file = $statusPath
        error = if ($isLockError) {
          "Build succeeded, but the repo-local handoff copy did not refresh because plugins\\AnarchyAi.Setup.exe is in use. Close every running AnarchyAi.Setup.exe window and rerun build-self-contained-exe.ps1."
        } else {
          $errorMessage
        }
        safe_repairs = if ($isLockError) {
          @(
            'close_running_anarchy_ai_setup_windows',
            'rerun_build_self_contained_exe',
            'do_not_hand_out_repo_local_exe_until_copy_succeeds'
          )
        } else {
          @('rerun_build_self_contained_exe')
        }
      }

      Write-BuildResult -Result $failureResult
      exit 1
    }
  }

  $result = [ordered]@{
    status = 'completed'
    project_path = $projectPath
    dotnet_path = $resolvedDotnetPath
    configuration = $Configuration
    publish_root = $publishRoot
    published_executable = $publishedExePath
    copy_to_plugins_requested = (-not $SkipCopyToPlugins)
    copied_to_plugins = (-not $SkipCopyToPlugins)
    target_executable = $targetExePath
    status_file = $statusPath
  }

  Write-BuildResult -Result $result
}
catch {
  $failureResult = [ordered]@{
    status = 'failed'
    failure_stage = 'build_or_publish'
    project_path = $projectPath
    dotnet_path = $resolvedDotnetPath
    configuration = $Configuration
    publish_root = $publishRoot
    published_executable = $publishedExePath
    copy_to_plugins_requested = (-not $SkipCopyToPlugins)
    copied_to_plugins = $false
    target_executable = $targetExePath
    status_file = $statusPath
    error = $_.Exception.Message
    safe_repairs = @('inspect_status_file_and_rerun_build')
  }

  Write-BuildResult -Result $failureResult
  exit 1
}
