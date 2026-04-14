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

function Get-FileSha256Hex {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path
  )

  $sha256 = [System.Security.Cryptography.SHA256]::Create()
  try {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hashBytes = $sha256.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hashBytes)).Replace('-', '').ToLowerInvariant()
  }
  finally {
    $sha256.Dispose()
  }
}

function Sync-SchemaBundleManifest {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $schemaRoot = Join-Path $RepoRoot 'plugins\anarchy-ai\schemas'
  $manifestPath = Join-Path $schemaRoot 'schema-bundle.manifest.json'
  $schemaFiles = @(
    'AGENTS-schema-1project.json',
    'AGENTS-schema-gov2gov-migration.json',
    'AGENTS-schema-governance.json',
    'AGENTS-schema-narrative.json',
    'AGENTS-schema-triage.md',
    'Getting-Started-For-Humans.txt'
  )

  if (-not (Test-Path $schemaRoot)) {
    throw "Schema directory not found: $schemaRoot"
  }

  $fileHashes = @{}
  foreach ($fileName in $schemaFiles) {
    $path = Join-Path $schemaRoot $fileName
    if (-not (Test-Path $path)) {
      throw "Schema file missing for manifest sync: $path"
    }

    $fileHashes[$fileName] = Get-FileSha256Hex -Path $path
  }

  $existingManifest = $null
  if (Test-Path $manifestPath) {
    try {
      $existingManifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    }
    catch {
      $existingManifest = $null
    }
  }

  $bundleName = if ($existingManifest -and -not [string]::IsNullOrWhiteSpace([string]$existingManifest.bundle_name)) {
    [string]$existingManifest.bundle_name
  } else {
    'anarchy-ai-canonical-schema-bundle'
  }

  $bundleVersion = if ($existingManifest -and -not [string]::IsNullOrWhiteSpace([string]$existingManifest.bundle_version)) {
    [string]$existingManifest.bundle_version
  } else {
    '0.1.0'
  }

  $hashesChanged = $true
  if ($existingManifest -and $existingManifest.files) {
    $existingByName = @{}
    foreach ($existingFile in $existingManifest.files) {
      if ($existingFile -and -not [string]::IsNullOrWhiteSpace([string]$existingFile.file_name)) {
        $existingByName[[string]$existingFile.file_name] = ([string]$existingFile.sha256).ToLowerInvariant()
      }
    }

    if ($existingByName.Count -eq $schemaFiles.Count) {
      $hashesChanged = $false
      foreach ($fileName in $schemaFiles) {
        if (-not $existingByName.ContainsKey($fileName) -or $existingByName[$fileName] -ne $fileHashes[$fileName]) {
          $hashesChanged = $true
          break
        }
      }
    }
  }

  $generatedUtc = if (
    $hashesChanged -or
    -not $existingManifest -or
    [string]::IsNullOrWhiteSpace([string]$existingManifest.generated_utc)
  ) {
    [DateTime]::UtcNow.ToString('o')
  } else {
    [string]$existingManifest.generated_utc
  }

  $manifestObject = [ordered]@{
    bundle_name = $bundleName
    bundle_version = $bundleVersion
    generated_utc = $generatedUtc
    files = @()
  }

  foreach ($fileName in $schemaFiles) {
    $manifestObject.files += [ordered]@{
      file_name = $fileName
      sha256 = $fileHashes[$fileName]
    }
  }

  $newJson = $manifestObject | ConvertTo-Json -Depth 10
  $existingJson = if ($existingManifest) {
    try {
      $existingManifest | ConvertTo-Json -Depth 10
    }
    catch {
      ''
    }
  } else {
    ''
  }

  if (-not [string]::Equals($newJson, $existingJson, [System.StringComparison]::Ordinal)) {
    Set-Content -Path $manifestPath -Value $newJson -Encoding UTF8
    return $true
  }

  return $false
}

function Update-GeneratedPluginReadme {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $templatePath = Join-Path $RepoRoot 'docs\ANARCHY_AI_PLUGIN_README_SOURCE.md'
  $readmePath = Join-Path $RepoRoot 'plugins\anarchy-ai\README.md'

  if (-not (Test-Path $templatePath)) {
    throw "Plugin README source doc not found: $templatePath"
  }

  $template = Get-Content $templatePath -Raw
  $tokens = [ordered]@{
    '{{REPO_LOCAL_PLUGIN_ROOT}}' = '.\plugins\anarchy-ai-<repo-slug>-<stable-path-hash>'
    '{{USER_PROFILE_PLUGIN_ROOT}}' = '~\.codex\plugins\anarchy-ai'
    '{{USER_PROFILE_MARKETPLACE_PATH}}' = '~\.agents\plugins\marketplace.json'
    '{{USER_PROFILE_MARKETPLACE_SOURCE_PATH}}' = './.codex/plugins/anarchy-ai'
    '{{SETUP_EXE_PATH}}' = '../AnarchyAi.Setup.exe'
  }

  $rendered = $template
  foreach ($token in $tokens.Keys) {
    $rendered = $rendered.Replace($token, $tokens[$token])
  }

  $forbiddenPatterns = @(
    '\.\.\/\.\.\/\.\.\/',
    '~[\\/]+plugins[\\/]+anarchy-ai'
  )

  foreach ($pattern in $forbiddenPatterns) {
    if ($rendered -match $pattern) {
      throw "Generated plugin README still contains a forbidden source or legacy-home path pattern: $pattern"
    }
  }

  $existing = if (Test-Path $readmePath) { Get-Content $readmePath -Raw } else { '' }
  if (-not [string]::Equals($existing, $rendered, [System.StringComparison]::Ordinal)) {
    Set-Content -Path $readmePath -Value $rendered -Encoding UTF8
    return $true
  }

  return $false
}

function Assert-SetupCliHelpContract {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath
  )

  if (-not (Test-Path $ExePath)) {
    throw "Setup executable not found for help-contract check: $ExePath"
  }

  $helpOutput = (& $ExePath '/?' 2>&1 | Out-String)
  if ($LASTEXITCODE -ne 0) {
    throw "Setup help-contract check failed: '$ExePath /?' exited with code $LASTEXITCODE."
  }

  $requiredSnippets = @(
    'Anarchy-AI Setup',
    '/silent',
    '/assess',
    '/install',
    '/update',
    '5 core + 1 test harness tool'
  )

  foreach ($snippet in $requiredSnippets) {
    if (-not $helpOutput.Contains($snippet)) {
      throw "Setup help-contract check failed: '$ExePath /?' output was missing expected text '$snippet'."
    }
  }
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
$schemaManifestSynced = $false
$pluginReadmeGenerated = $false
$publishedHelpContractValidated = $false
$targetHelpContractValidated = $false

try {
  $resolvedDotnetPath = Resolve-DotnetPath -RequestedPath $DotnetPath

  if (-not (Test-Path $projectPath)) {
    throw "Setup project not found: $projectPath"
  }

  if (-not (Test-Path $pluginsRoot)) {
    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
  }

  $pluginReadmeGenerated = Update-GeneratedPluginReadme -RepoRoot $repoRoot
  $schemaManifestSynced = Sync-SchemaBundleManifest -RepoRoot $repoRoot

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

  Assert-SetupCliHelpContract -ExePath $publishedExePath
  $publishedHelpContractValidated = $true

  if (-not $SkipCopyToPlugins) {
    try {
      Copy-Item $publishedExePath $targetExePath -Force
      Assert-SetupCliHelpContract -ExePath $targetExePath
      $targetHelpContractValidated = $true
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
        plugin_readme_generated = $pluginReadmeGenerated
        schema_manifest_synced = $schemaManifestSynced
        published_help_contract_validated = $publishedHelpContractValidated
        target_help_contract_validated = $targetHelpContractValidated
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
    plugin_readme_generated = $pluginReadmeGenerated
    schema_manifest_synced = $schemaManifestSynced
    published_help_contract_validated = $publishedHelpContractValidated
    target_help_contract_validated = if ($SkipCopyToPlugins) { $false } else { $targetHelpContractValidated }
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
    plugin_readme_generated = $pluginReadmeGenerated
    schema_manifest_synced = $schemaManifestSynced
    published_help_contract_validated = $publishedHelpContractValidated
    target_help_contract_validated = $targetHelpContractValidated
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
