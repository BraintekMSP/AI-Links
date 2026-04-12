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
$objRootMsbuild = (($objRoot -replace '\\', '/') + '/')
$binRootMsbuild = (($binRoot -replace '\\', '/') + '/')

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
  Copy-Item $publishedExePath $targetExePath -Force
}

$result = [ordered]@{
  project_path = $projectPath
  dotnet_path = $resolvedDotnetPath
  configuration = $Configuration
  publish_root = $publishRoot
  published_executable = $publishedExePath
  copied_to_plugins = (-not $SkipCopyToPlugins)
  target_executable = $targetExePath
}

$result | ConvertTo-Json -Depth 10
