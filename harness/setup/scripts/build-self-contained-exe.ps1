<#
.SYNOPSIS
Builds and republishes the self-contained Anarchy-AI setup executable from repo-authored sources.
.DESCRIPTION
Regenerates branding and path-canon artifacts, refreshes generated published surfaces, validates path and documentation-truth compliance,
publishes the setup project, and refreshes the repo-local handoff EXE.
.PARAMETER Configuration
Build configuration passed to dotnet publish.
.PARAMETER DotnetPath
Optional explicit path to dotnet.exe when SDK discovery should not use the default lookup order.
.PARAMETER SkipCopyToPlugins
Skips copying the published EXE back into the repo-local plugins folder.
.OUTPUTS
JSON build status describing generated surfaces, publish outputs, and validation results.
.NOTES
Critical dependencies: the branding/path-canon generators, dotnet SDK availability, generated README/.mcp/manifest flows, and the path/documentation/removal audit scripts.
#>
param(
  [ValidateSet('Release', 'Debug')]
  [string]$Configuration = 'Release',
  [string]$DotnetPath = '',
  [switch]$SkipCopyToPlugins
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$setupRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $setupRoot '..\..'))
$brandingGeneratorPath = Join-Path $repoRoot 'harness\branding\scripts\generate-branding-artifacts.ps1'
$pathCanonGeneratorPath = Join-Path $repoRoot 'harness\pathing\scripts\generate-path-canon-artifacts.ps1'

if (-not (Test-Path $brandingGeneratorPath)) {
  throw "Branding generator script not found: $brandingGeneratorPath"
}

if (-not (Test-Path $pathCanonGeneratorPath)) {
  throw "Path canon generator script not found: $pathCanonGeneratorPath"
}

& powershell -ExecutionPolicy Bypass -File $brandingGeneratorPath
if ($LASTEXITCODE -ne 0) {
  throw "Branding generator failed with exit code $LASTEXITCODE"
}

& powershell -ExecutionPolicy Bypass -File $pathCanonGeneratorPath
if ($LASTEXITCODE -ne 0) {
  throw "Path canon generator failed with exit code $LASTEXITCODE"
}

$brandingPath = Join-Path $repoRoot 'harness\branding\generated\anarchy-branding.generated.psd1'
if (-not (Test-Path $brandingPath)) {
  throw "Branding artifact not found: $brandingPath"
}

$branding = Import-PowerShellDataFile -Path $brandingPath

$pathCanonPath = Join-Path $repoRoot 'harness\pathing\generated\anarchy-path-canon.generated.psd1'
if (-not (Test-Path $pathCanonPath)) {
  throw "Path canon artifact not found: $pathCanonPath"
}

$pathCanon = Import-PowerShellDataFile -Path $pathCanonPath

<#
.SYNOPSIS
Writes text content as UTF-8 without a byte-order mark.
.DESCRIPTION
Uses the .NET UTF8Encoding overload with BOM disabled so Codex-facing JSON manifests remain consumable by strict parsers.
.PARAMETER Path
Absolute file path to write.
.PARAMETER Content
Text content to persist.
.OUTPUTS
No direct return value.
.NOTES
Critical dependencies: System.IO.File and System.Text.UTF8Encoding.
#>
function Set-Utf8NoBomContent {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [Parameter(Mandatory = $true)]
    [string]$Content
  )

  $directory = Split-Path -Parent $Path
  if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
  }

  $encoding = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

<#
.SYNOPSIS
Detects whether a text file starts with the UTF-8 byte-order mark.
.DESCRIPTION
Reads the first three bytes only so build-time normalization can distinguish BOM-only drift from content drift.
.PARAMETER Path
Absolute file path to inspect.
.OUTPUTS
System.Boolean. True when the file starts with EF BB BF.
.NOTES
Critical dependencies: readable file access and System.IO.File.
#>
function Test-HasUtf8Bom {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path
  )

  if (-not (Test-Path $Path)) {
    return $false
  }

  $bytes = [System.IO.File]::ReadAllBytes($Path)
  return $bytes.Length -ge 3 -and
    $bytes[0] -eq 0xEF -and
    $bytes[1] -eq 0xBB -and
    $bytes[2] -eq 0xBF
}

<#
.SYNOPSIS
Resolves a canon-relative path beneath a supplied root.
.DESCRIPTION
Normalizes separators and returns the absolute path used by later build helpers.
.PARAMETER RootPath
Absolute root path for resolution.
.PARAMETER RelativePath
Canon-relative path fragment to resolve.
.OUTPUTS
System.String. Absolute filesystem path.
.NOTES
Critical dependencies: the generated path canon and Join-Path/GetFullPath.
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

<#
.SYNOPSIS
Builds a portable relative path from one absolute path to another.
.DESCRIPTION
Uses System.Uri so generated docs can carry destination-relative references without hard-coded source paths.
.PARAMETER BasePath
Absolute base path to relativize from.
.PARAMETER TargetPath
Absolute target path to relativize to.
.OUTPUTS
System.String. Relative path from the base path to the target path.
.NOTES
Critical dependencies: absolute path normalization and System.Uri relative-path behavior.
#>
function Get-PortableRelativePath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$BasePath,
    [Parameter(Mandatory = $true)]
    [string]$TargetPath
  )

  $normalizedBasePath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
  $normalizedTargetPath = [System.IO.Path]::GetFullPath($TargetPath)
  $baseUri = New-Object System.Uri($normalizedBasePath)
  $targetUri = New-Object System.Uri($normalizedTargetPath)
  return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

<#
.SYNOPSIS
Finds a dotnet.exe that has an installed SDK.
.DESCRIPTION
Validates an explicit path when supplied, otherwise searches the common machine and user locations.
.PARAMETER RequestedPath
Optional explicit dotnet.exe path to test first.
.OUTPUTS
System.String. Resolved dotnet.exe path.
.NOTES
Critical dependencies: dotnet --list-sdks and access to the local .NET SDK installation.
#>
function Resolve-DotnetPath {
  param(
    [string]$RequestedPath
  )

  <#
  .SYNOPSIS
  Tests whether a candidate dotnet.exe reports at least one installed SDK.
  .DESCRIPTION
  Used as the inner probe for Resolve-DotnetPath so runtime-only dotnet installs are rejected.
  .PARAMETER CandidatePath
  Candidate dotnet.exe path.
  .OUTPUTS
  System.Boolean. True when the candidate reports installed SDKs.
  .NOTES
  Critical dependencies: `dotnet --list-sdks` and process execution rights.
  #>
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

<#
.SYNOPSIS
Computes the lowercase SHA-256 hex digest for a file.
.DESCRIPTION
Reads the full file into memory and hashes it for manifest and publish integrity checks.
.PARAMETER Path
Absolute file path to hash.
.OUTPUTS
System.String. Lowercase SHA-256 hash.
.NOTES
Critical dependencies: System.Security.Cryptography.SHA256 and readable file access.
#>
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

<#
.SYNOPSIS
Refreshes the bundled schema manifest hashes from the repo-authored schema files.
.DESCRIPTION
Computes current schema hashes, preserves stable manifest metadata when possible, and rewrites the manifest when content changed.
.PARAMETER RepoRoot
Repo root containing the canonical schema files and manifest target.
.OUTPUTS
System.Boolean. True when the manifest file was rewritten.
.NOTES
Critical dependencies: the path canon, Get-FileSha256Hex, and the canonical schema files remaining present in the repo.
#>
function Sync-SchemaBundleManifest {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $schemaRoot = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath ($pathCanon.relative_paths.repo_source_plugin_directory_relative_path + '/' + $pathCanon.relative_paths.bundle_schemas_directory_relative_path)
  $manifestPath = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath ($pathCanon.relative_paths.repo_source_plugin_directory_relative_path + '/' + $pathCanon.relative_paths.bundle_schema_manifest_file_relative_path)
  $schemaFiles = @($pathCanon.arrays.portable_schema_files)

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

  if ((-not [string]::Equals($newJson, $existingJson, [System.StringComparison]::Ordinal)) -or (Test-HasUtf8Bom -Path $manifestPath)) {
    Set-Utf8NoBomContent -Path $manifestPath -Content $newJson
    return $true
  }

  return $false
}

<#
.SYNOPSIS
Generates the published plugin README from the repo-authored README source doc.
.DESCRIPTION
Replaces destination-relative tokens, validates that forbidden source-layout or legacy-home paths do not survive,
and rewrites the published README when content changed.
.PARAMETER RepoRoot
Repo root containing the README source and target files.
.OUTPUTS
System.Boolean. True when the README file was rewritten.
.NOTES
Critical dependencies: the path canon, Get-PortableRelativePath, and docs/ANARCHY_AI_PLUGIN_README_SOURCE.md.
#>
function Update-GeneratedPluginReadme {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $templatePath = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath $pathCanon.relative_paths.repo_source_generated_plugin_readme_source_relative_path
  $readmePath = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath $pathCanon.relative_paths.repo_source_generated_plugin_readme_target_relative_path

  if (-not (Test-Path $templatePath)) {
    throw "Plugin README source doc not found: $templatePath"
  }

  $template = Get-Content $templatePath -Raw
  $repoLocalPluginRootLabel = '.\' + (($pathCanon.relative_paths.repo_local_plugin_parent_directory_relative_path + '/' + $pathCanon.names.repo_scoped_plugin_directory_name_template).Replace('/', '\'))
  $userProfilePluginRootLabel = '~\' + (($pathCanon.relative_paths.user_profile_plugin_parent_directory_relative_path + '/' + $pathCanon.names.default_plugin_name).Replace('/', '\'))
  $userProfileMarketplacePathLabel = '~\' + ($pathCanon.relative_paths.user_profile_marketplace_file_relative_path.Replace('/', '\'))
  $setupExeRelativePath = (Get-PortableRelativePath `
    -BasePath (Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path) `
    -TargetPath (Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath $pathCanon.relative_paths.repo_source_setup_executable_file_relative_path)).Replace('\', '/')
  $tokens = [ordered]@{
    '{{REPO_LOCAL_PLUGIN_ROOT}}' = $repoLocalPluginRootLabel
    '{{USER_PROFILE_PLUGIN_ROOT}}' = $userProfilePluginRootLabel
    '{{USER_PROFILE_MARKETPLACE_PATH}}' = $userProfileMarketplacePathLabel
    '{{USER_PROFILE_MARKETPLACE_SOURCE_PATH}}' = "$($pathCanon.relative_references.user_profile_marketplace_plugin_source_prefix)$($pathCanon.names.default_plugin_name)"
    '{{SETUP_EXE_PATH}}' = $setupExeRelativePath
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
  if ((-not [string]::Equals($existing, $rendered, [System.StringComparison]::Ordinal)) -or (Test-HasUtf8Bom -Path $readmePath)) {
    Set-Utf8NoBomContent -Path $readmePath -Content $rendered
    return $true
  }

  return $false
}

<#
.SYNOPSIS
Regenerates the plugin-local .mcp.json declaration from canon-backed runtime references.
.DESCRIPTION
Writes the expected server command, cwd, and empty args array when the declaration is missing or stale.
.PARAMETER RepoRoot
Repo root containing the source plugin bundle.
.OUTPUTS
System.Boolean. True when the .mcp.json file was rewritten.
.NOTES
Critical dependencies: the path canon and the repo-authored plugin bundle layout.
#>
function Update-GeneratedPluginMcpDeclaration {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $mcpPath = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_mcp_file_relative_path
  $expectedMcp = [ordered]@{
    mcpServers = [ordered]@{
      $pathCanon.names.default_plugin_name = [ordered]@{
        command = [string]$pathCanon.relative_references.bundle_runtime_windows_command_relative_path
        args = @()
        cwd = [string]$pathCanon.relative_references.bundle_runtime_working_directory_relative_path
      }
    }
  } | ConvertTo-Json -Depth 10

  $existing = if (Test-Path $mcpPath) { Get-Content $mcpPath -Raw } else { '' }
  if ((-not [string]::Equals($existing.Trim(), $expectedMcp.Trim(), [System.StringComparison]::Ordinal)) -or (Test-HasUtf8Bom -Path $mcpPath)) {
    Set-Utf8NoBomContent -Path $mcpPath -Content $expectedMcp
    return $true
  }

  return $false
}

<#
.SYNOPSIS
Regenerates the plugin manifest from the branding canon and current technical identity canon.
.DESCRIPTION
Writes the expected author, legal, visual, and install-surface metadata so future fork rebrands can change one repo-authored branding source instead of hand-editing the manifest.
.PARAMETER RepoRoot
Repo root containing the source plugin manifest.
.OUTPUTS
System.Boolean. True when the plugin manifest file was rewritten.
.NOTES
Critical dependencies: the branding canon, the path canon default plugin name, and the repo-authored plugin manifest location.
#>
function Update-GeneratedPluginManifest {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
  )

  $manifestPath = Resolve-CanonRelativePath -RootPath $RepoRoot -RelativePath ($pathCanon.relative_paths.repo_source_plugin_directory_relative_path + '/' + $pathCanon.relative_paths.bundle_plugin_manifest_file_relative_path)
  $manifestObject = [ordered]@{
    name = [string]$pathCanon.names.default_plugin_name
    version = '0.1.8'
    description = [string]$branding.messaging.plugin_description
    author = [ordered]@{
      name = [string]$branding.metadata.author_name
      url = [string]$branding.metadata.author_url
    }
    homepage = [string]$branding.metadata.homepage_url
    repository = [string]$branding.metadata.repository_url
    license = 'Proprietary - see LICENSE'
    keywords = @(
      'mcp',
      'schema',
      'governance',
      'harness',
      'codex',
      'preflight',
      'bootstrap'
    )
    skills = './skills/'
    mcpServers = './.mcp.json'
    interface = [ordered]@{
      displayName = [string]$branding.names.brand_display_name
      shortDescription = [string]$branding.messaging.plugin_short_description
      longDescription = [string]$branding.messaging.plugin_long_description
      developerName = [string]$branding.metadata.developer_name
      category = 'Coding'
      capabilities = @(
        'Interactive',
        'Write'
      )
      websiteURL = [string]$branding.metadata.homepage_url
      privacyPolicyURL = [string]$branding.metadata.privacy_policy_url
      termsOfServiceURL = [string]$branding.metadata.terms_of_service_url
      defaultPrompt = @($branding.messaging.plugin_default_prompt_lines)
      brandColor = [string]$branding.metadata.brand_color
      composerIcon = './' + ([string]$branding.relative_paths.bundle_plugin_composer_icon_relative_path)
      logo = './' + ([string]$branding.relative_paths.bundle_plugin_logo_relative_path)
      screenshots = @()
    }
  } | ConvertTo-Json -Depth 10

  $existing = if (Test-Path $manifestPath) { Get-Content $manifestPath -Raw } else { '' }
  if ((-not [string]::Equals($existing.Trim(), $manifestObject.Trim(), [System.StringComparison]::Ordinal)) -or (Test-HasUtf8Bom -Path $manifestPath)) {
    Set-Utf8NoBomContent -Path $manifestPath -Content $manifestObject
    return $true
  }

  return $false
}

<#
.SYNOPSIS
Validates that a setup executable still exposes the required CLI help contract.
.DESCRIPTION
Runs `/?` against the target EXE and fails when expected usage or tool-count lines disappear.
.PARAMETER ExePath
Absolute path to the setup executable under validation.
.OUTPUTS
No direct return value. Throws when the help contract is missing or the command fails.
.NOTES
Critical dependencies: the published CLI contract and local process execution of the EXE.
#>
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
    [string]$branding.names.setup_display_name,
    '/silent',
    '/assess',
    '/install',
    '/update',
    '/status',
    'install-state',
    '5 core + 1 test harness tool'
  )

  foreach ($snippet in $requiredSnippets) {
    if (-not $helpOutput.Contains($snippet)) {
      throw "Setup help-contract check failed: '$ExePath /?' output was missing expected text '$snippet'."
    }
  }
}

$projectPath = Join-Path $setupRoot 'dotnet\AnarchyAi.Setup.csproj'
$serverProjectPath = Join-Path $repoRoot 'harness\server\dotnet\AnarchyAi.Mcp.Server.csproj'
$pluginsRoot = Join-Path $repoRoot 'plugins'
$targetExePath = Join-Path $pluginsRoot 'AnarchyAi.Setup.exe'
$pathAuditScriptPath = Join-Path $repoRoot 'harness\pathing\scripts\test-path-canon-compliance.ps1'
$documentationAuditScriptPath = Join-Path $repoRoot 'docs\scripts\test-documentation-truth-compliance.ps1'
$removalSafetyAuditScriptPath = Join-Path $repoRoot 'docs\scripts\test-removal-safety-compliance.ps1'

$tempRoot = Join-Path $env:LOCALAPPDATA 'Temp\ai-links-setup-build'
$objRoot = Join-Path $tempRoot 'obj'
$binRoot = Join-Path $tempRoot 'bin'
$serverObjRoot = Join-Path $tempRoot 'server-obj'
$serverBinRoot = Join-Path $tempRoot 'server-bin'
$publishRoot = Join-Path $tempRoot 'publish'
$serverPublishRoot = Join-Path $tempRoot 'server-publish'
$pluginPayloadStageRoot = Join-Path $tempRoot 'plugin-payload-stage'
$statusPath = Join-Path $tempRoot 'last-build-result.json'
$objRootMsbuild = (($objRoot -replace '\\', '/') + '/')
$binRootMsbuild = (($binRoot -replace '\\', '/') + '/')
$serverObjRootMsbuild = (($serverObjRoot -replace '\\', '/') + '/')
$serverBinRootMsbuild = (($serverBinRoot -replace '\\', '/') + '/')

<#
.SYNOPSIS
Writes the machine-readable build result file and echoes it to stdout.
.DESCRIPTION
Creates the status directory when needed and persists the final build status for later troubleshooting.
.PARAMETER Result
Hashtable describing the build outcome.
.OUTPUTS
JSON text written to the status file and stdout.
.NOTES
Critical dependencies: the temp build status path and ConvertTo-Json.
#>
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
$pluginMcpDeclarationGenerated = $false
$pluginManifestGenerated = $false
$pathAuditValidated = $false
$documentationTruthAuditValidated = $false
$removalSafetyAuditValidated = $false
$publishedHelpContractValidated = $false
$targetHelpContractValidated = $false
$pluginPayloadStaged = $false
$publishedRuntimeExePath = ''
$stagedPluginPayloadRoot = ''

try {
  $resolvedDotnetPath = Resolve-DotnetPath -RequestedPath $DotnetPath

  if (-not (Test-Path $projectPath)) {
    throw "Setup project not found: $projectPath"
  }

  if (-not (Test-Path $serverProjectPath)) {
    throw "MCP server project not found: $serverProjectPath"
  }

  if (-not (Test-Path $pluginsRoot)) {
    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
  }

  $pluginReadmeGenerated = Update-GeneratedPluginReadme -RepoRoot $repoRoot
  $pluginMcpDeclarationGenerated = Update-GeneratedPluginMcpDeclaration -RepoRoot $repoRoot
  $pluginManifestGenerated = Update-GeneratedPluginManifest -RepoRoot $repoRoot
  $schemaManifestSynced = Sync-SchemaBundleManifest -RepoRoot $repoRoot
  if (-not (Test-Path $pathAuditScriptPath)) {
    throw "Path audit script not found: $pathAuditScriptPath"
  }
  & powershell -ExecutionPolicy Bypass -File $pathAuditScriptPath -RepoRoot $repoRoot
  if ($LASTEXITCODE -ne 0) {
    throw "Path audit script failed with exit code $LASTEXITCODE"
  }
  $pathAuditValidated = $true
  if (-not (Test-Path $documentationAuditScriptPath)) {
    throw "Documentation truth audit script not found: $documentationAuditScriptPath"
  }
  & powershell -ExecutionPolicy Bypass -File $documentationAuditScriptPath -RepoRoot $repoRoot
  if ($LASTEXITCODE -ne 0) {
    throw "Documentation truth audit script failed with exit code $LASTEXITCODE"
  }
  $documentationTruthAuditValidated = $true
  if (-not (Test-Path $removalSafetyAuditScriptPath)) {
    throw "Removal safety audit script not found: $removalSafetyAuditScriptPath"
  }
  & powershell -ExecutionPolicy Bypass -File $removalSafetyAuditScriptPath -RepoRoot $repoRoot
  if ($LASTEXITCODE -ne 0) {
    throw "Removal safety audit script failed with exit code $LASTEXITCODE"
  }
  $removalSafetyAuditValidated = $true

  if (Test-Path $serverPublishRoot) {
    Remove-Item $serverPublishRoot -Recurse -Force
  }

  $serverPublishArguments = @(
    'publish',
    $serverProjectPath,
    '-c', $Configuration,
    '-f', 'net8.0',
    ('-p:BaseIntermediateOutputPath="{0}"' -f $serverObjRootMsbuild),
    ('-p:BaseOutputPath="{0}"' -f $serverBinRootMsbuild),
    '-p:NuGetAudit=false',
    '-o', ('"{0}"' -f $serverPublishRoot)
  )

  & $resolvedDotnetPath @serverPublishArguments
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish for MCP server failed with exit code $LASTEXITCODE"
  }

  $publishedRuntimeExePath = Join-Path $serverPublishRoot 'AnarchyAi.Mcp.Server.exe'
  if (-not (Test-Path $publishedRuntimeExePath)) {
    throw "Published MCP server executable not found: $publishedRuntimeExePath"
  }

  if (Test-Path $pluginPayloadStageRoot) {
    Remove-Item $pluginPayloadStageRoot -Recurse -Force
  }

  $stagedPluginPayloadRoot = Join-Path $pluginPayloadStageRoot 'anarchy-ai'
  Copy-Item (Resolve-CanonRelativePath -RootPath $repoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path) $stagedPluginPayloadRoot -Recurse -Force
  $stagedRuntimePath = Resolve-CanonRelativePath -RootPath $stagedPluginPayloadRoot -RelativePath $pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
  $stagedRuntimeDirectory = Split-Path -Parent $stagedRuntimePath
  if (-not (Test-Path $stagedRuntimeDirectory)) {
    New-Item -ItemType Directory -Path $stagedRuntimeDirectory -Force | Out-Null
  }

  Copy-Item $publishedRuntimeExePath $stagedRuntimePath -Force
  $pluginPayloadStaged = $true

  if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
  }

  $publishArguments = @(
    'publish',
    $projectPath,
    '-c', $Configuration,
    ('-p:BaseIntermediateOutputPath="{0}"' -f $objRootMsbuild),
    ('-p:BaseOutputPath="{0}"' -f $binRootMsbuild),
    ('-p:AnarchySetupPluginPayloadRoot="{0}"' -f (($stagedPluginPayloadRoot -replace '\\', '/') + '/')),
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
        server_project_path = $serverProjectPath
        dotnet_path = $resolvedDotnetPath
        configuration = $Configuration
        publish_root = $publishRoot
        server_publish_root = $serverPublishRoot
        published_executable = $publishedExePath
        published_runtime_executable = $publishedRuntimeExePath
        plugin_payload_staged = $pluginPayloadStaged
        staged_plugin_payload_root = $stagedPluginPayloadRoot
        plugin_readme_generated = $pluginReadmeGenerated
        plugin_mcp_declaration_generated = $pluginMcpDeclarationGenerated
        plugin_manifest_generated = $pluginManifestGenerated
        schema_manifest_synced = $schemaManifestSynced
        path_audit_validated = $pathAuditValidated
        documentation_truth_audit_validated = $documentationTruthAuditValidated
        removal_safety_audit_validated = $removalSafetyAuditValidated
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
    server_project_path = $serverProjectPath
    dotnet_path = $resolvedDotnetPath
    configuration = $Configuration
    publish_root = $publishRoot
    server_publish_root = $serverPublishRoot
    published_executable = $publishedExePath
    published_runtime_executable = $publishedRuntimeExePath
    plugin_payload_staged = $pluginPayloadStaged
    staged_plugin_payload_root = $stagedPluginPayloadRoot
    plugin_readme_generated = $pluginReadmeGenerated
    plugin_mcp_declaration_generated = $pluginMcpDeclarationGenerated
    plugin_manifest_generated = $pluginManifestGenerated
    schema_manifest_synced = $schemaManifestSynced
    path_audit_validated = $pathAuditValidated
    documentation_truth_audit_validated = $documentationTruthAuditValidated
    removal_safety_audit_validated = $removalSafetyAuditValidated
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
    server_project_path = $serverProjectPath
    dotnet_path = $resolvedDotnetPath
    configuration = $Configuration
    publish_root = $publishRoot
    server_publish_root = $serverPublishRoot
    published_executable = $publishedExePath
    published_runtime_executable = $publishedRuntimeExePath
    plugin_payload_staged = $pluginPayloadStaged
    staged_plugin_payload_root = $stagedPluginPayloadRoot
    plugin_readme_generated = $pluginReadmeGenerated
    plugin_mcp_declaration_generated = $pluginMcpDeclarationGenerated
    plugin_manifest_generated = $pluginManifestGenerated
    schema_manifest_synced = $schemaManifestSynced
    path_audit_validated = $pathAuditValidated
    documentation_truth_audit_validated = $documentationTruthAuditValidated
    removal_safety_audit_validated = $removalSafetyAuditValidated
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
