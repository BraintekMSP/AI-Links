<#
.SYNOPSIS
Installs, assesses, or refreshes the repo-local Anarchy-AI bundle after the bundle already exists.
.DESCRIPTION
Provides a bounded repo-local compatibility lane for marketplace registration, bundle refresh, schema refresh,
and nested path reporting using the generated path canon.
.PARAMETER Mode
Selects Assess or Install behavior for the repo-local bundle.
.PARAMETER HostContext
Carries the host label whose optional surfaces should be considered during assessment.
.PARAMETER Update
Requests a bundle refresh from a source path or source URL before the final assessment is returned.
.PARAMETER RefreshPortableSchemaFamily
Requests refresh of the portable schema family into the repo root during update.
.PARAMETER UpdateSourceZipUrl
Zip URL used when refreshing from a remote source.
.PARAMETER UpdateSourcePath
Optional local repo or zip path used instead of the remote source.
.OUTPUTS
JSON describing bootstrap state, actions, missing components, repairs, and nested origin/source/destination paths.
.NOTES
Critical dependencies: the generated path canon psd1, repo-local plugin bundle structure, marketplace.json, and local filesystem write access.
#>
param(
  [ValidateSet('Assess','Install')]
  [string]$Mode = 'Assess',
  [ValidateSet('codex','claude','cursor','generic')]
  [string]$HostContext = 'codex',
  [switch]$Update,
  [switch]$RefreshPortableSchemaFamily,
  [string]$UpdateSourceZipUrl = '',
  [string]$UpdateSourcePath = ''
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir '..'))
$pathCanonPath = Join-Path $pluginRoot 'pathing\anarchy-path-canon.generated.psd1'
if (-not (Test-Path $pathCanonPath)) {
  throw "Path canon artifact not found: $pathCanonPath"
}

$pathCanon = Import-PowerShellDataFile -Path $pathCanonPath
$brandingPath = Join-Path $pluginRoot 'branding\anarchy-branding.generated.psd1'
if (-not (Test-Path $brandingPath)) {
  throw "Branding artifact not found: $brandingPath"
}

$branding = Import-PowerShellDataFile -Path $brandingPath
if ([string]::IsNullOrWhiteSpace($UpdateSourceZipUrl)) {
  $UpdateSourceZipUrl = [string]$branding.metadata.default_update_source_zip_url
}

<#
.SYNOPSIS
Resolves a canon-relative path against a supplied root.
.DESCRIPTION
Normalizes slash direction and returns an absolute path for bundle and repo surfaces.
.PARAMETER RootPath
Absolute root path used as the base for resolution.
.PARAMETER RelativePath
Canon-relative path fragment to resolve beneath the root.
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
Resolves a bundle-relative path beneath the current plugin root.
.DESCRIPTION
Convenience wrapper over Resolve-CanonRelativePath for plugin-local surfaces.
.PARAMETER RelativePath
Bundle-relative canon path to resolve.
.OUTPUTS
System.String. Absolute plugin-local path.
.NOTES
Critical dependencies: $pluginRoot and Resolve-CanonRelativePath.
#>
function Resolve-BundlePath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RelativePath
  )

  return Resolve-CanonRelativePath -RootPath $pluginRoot -RelativePath $RelativePath
}

<#
.SYNOPSIS
Normalizes a target file's attributes and writes text content in one guarded step.
.DESCRIPTION
Ensures repo-local bootstrap can rewrite synced JSON files without relying on ambient filesystem attributes or editor state.
.PARAMETER Path
Absolute file path to create or overwrite.
.PARAMETER Content
Serialized text content to write.
.OUTPUTS
No direct return value. Creates or overwrites the target file.
.NOTES
Critical dependencies: filesystem write access, Set-ItemProperty-compatible attributes, and Set-Content -Force.
#>
function Write-TextFile {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [Parameter(Mandatory = $true)]
    [string]$Content
  )

  $directory = Split-Path -Parent $Path
  if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
  }

  if (Test-Path $Path) {
    try {
      (Get-Item $Path -Force).Attributes = 'Normal'
    }
    catch {
    }
  }

  $encoding = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

<#
.SYNOPSIS
Expands a generated canon naming template with supplied token values.
.DESCRIPTION
Replaces `<token>` placeholders in one template string without introducing ad hoc naming literals.
.PARAMETER Template
Canon template string that may contain placeholder tokens.
.PARAMETER Replacements
Hashtable of token values keyed without angle brackets.
.OUTPUTS
System.String. Expanded template text.
.NOTES
Critical dependencies: the generated path canon naming templates and exact placeholder replacement.
#>
function Expand-CanonTemplate {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Template,
    [Parameter(Mandatory = $true)]
    [hashtable]$Replacements
  )

  $expanded = $Template
  foreach ($key in $Replacements.Keys) {
    $expanded = $expanded.Replace("<$key>", [string]$Replacements[$key])
  }

  return $expanded
}

<#
.SYNOPSIS
Checks whether a value matches one of the canon-owned exact names or prefixes.
.DESCRIPTION
Provides one shared ownership check so repo-local bootstrap does not rely on broad ad hoc prefix matching.
.PARAMETER Value
Candidate string to classify.
.PARAMETER Exact
Exact owned values supplied by the generated path canon.
.PARAMETER Prefixes
Owned prefixes supplied by the generated path canon.
.OUTPUTS
System.Boolean. True when the value belongs to the owned identity set.
.NOTES
Critical dependencies: the generated path canon identity arrays and ordinal-ignore-case comparison.
#>
function Test-MatchesCanonIdentity {
  param(
    [string]$Value,
    [string[]]$Exact = @(),
    [string[]]$Prefixes = @()
  )

  if ([string]::IsNullOrWhiteSpace($Value)) {
    return $false
  }

  foreach ($candidate in @($Exact | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    if ([string]::Equals($Value, [string]$candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
      return $true
    }
  }

  foreach ($candidate in @($Prefixes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    if ($Value.StartsWith([string]$candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
      return $true
    }
  }

  return $false
}

<#
.SYNOPSIS
Checks whether a plugin name belongs to the current or legacy owned Anarchy-AI identities.
.DESCRIPTION
Uses the generated canon arrays so bootstrap and removal stay aligned on owned plugin names.
.PARAMETER PluginName
Plugin name to classify.
.OUTPUTS
System.Boolean. True when the plugin name is owned by this repo.
.NOTES
Critical dependencies: path canon owned plugin-name arrays and Test-MatchesCanonIdentity.
#>
function Test-IsOwnedPluginName {
  param(
    [string]$PluginName
  )

  return Test-MatchesCanonIdentity `
    -Value $PluginName `
    -Exact @($pathCanon.arrays.owned_plugin_name_exact) `
    -Prefixes @($pathCanon.arrays.owned_plugin_name_prefixes)
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $pluginRoot '..\..'))
$marketplacePath = Resolve-CanonRelativePath -RootPath $repoRoot -RelativePath $pathCanon.relative_paths.repo_local_marketplace_file_relative_path
$expectedPluginName = [string]$pathCanon.names.default_plugin_name
$expectedPluginRelativePath = "./$([string]$pathCanon.relative_paths.repo_source_plugin_directory_relative_path)"
$runtimePath = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
$pluginManifestPath = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_plugin_manifest_file_relative_path
$mcpPath = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_mcp_file_relative_path
$skillPath = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_skill_file_relative_path
$schemaManifestPath = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_schema_manifest_file_relative_path
$portableSchemaFiles = @($pathCanon.arrays.portable_schema_files)
$contractFiles = @(
  'active-work-state.contract.json',
  'schema-reality.contract.json',
  'gov2gov-migration.contract.json',
  'preflight-session.contract.json',
  'harness-gap-state.contract.json',
  'narrative-arc-validation.contract.json'
)

$actionsTaken = New-Object System.Collections.Generic.List[string]
$missingComponents = New-Object System.Collections.Generic.List[string]
$safeRepairs = New-Object System.Collections.Generic.List[string]
$updateState = 'not_requested'
$updateRuntimeLocked = $false
$updateNotes = New-Object System.Collections.Generic.List[string]
$effectiveSourceRoot = $pluginRoot

<#
.SYNOPSIS
Normalizes a repo name into a marketplace-safe slug.
.DESCRIPTION
Lowercases the value, collapses non-alphanumeric characters to dashes, and falls back to `repo` when empty.
.PARAMETER Value
Repo name or other label to normalize.
.OUTPUTS
System.String. Marketplace-safe slug.
.NOTES
Critical dependencies: repo-scoped identity generation and current slugging rules.
#>
function Get-NormalizedMarketplaceSlug {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Value
  )

  $normalized = $Value.Trim().ToLowerInvariant()
  $normalized = [regex]::Replace($normalized, '[^a-z0-9]+', '-')
  $normalized = $normalized.Trim('-')
  if ([string]::IsNullOrWhiteSpace($normalized)) {
    return 'repo'
  }

  return $normalized
}

<#
.SYNOPSIS
Builds the repo-scoped marketplace name and display name for the current repo root.
.DESCRIPTION
Uses the normalized repo name to create a stable repo-local marketplace identity without a path hash suffix.
.PARAMETER ResolvedRepoRoot
Absolute repo root used for naming.
.OUTPUTS
PSCustomObject with `name` and `display_name`.
.NOTES
Critical dependencies: Get-NormalizedMarketplaceSlug and the current repo-local marketplace naming contract.
#>
function Get-RepoScopedMarketplaceIdentity {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedRepoRoot
  )

  $repoName = Split-Path $ResolvedRepoRoot -Leaf
  $slug = Get-NormalizedMarketplaceSlug -Value $repoName

  return [pscustomobject]@{
    name = Expand-CanonTemplate `
      -Template ([string]$pathCanon.names.repo_scoped_marketplace_name_template) `
      -Replacements @{ 'repo-slug' = $slug }
    display_name = if ([string]::IsNullOrWhiteSpace($repoName)) {
      ([string]$branding.names.repo_local_marketplace_display_name_template).Replace('<RepoName>', 'Repo')
    } else {
      ([string]$branding.names.repo_local_marketplace_display_name_template).Replace('<RepoName>', $repoName)
    }
  }
}

<#
.SYNOPSIS
Returns the repo-local MCP server name expected inside the plugin-local .mcp.json file.
.DESCRIPTION
Currently returns the stable Anarchy-AI server name without repo scoping.
.PARAMETER ResolvedRepoRoot
Absolute repo root retained for future host- or repo-specific naming decisions.
.OUTPUTS
System.String. MCP server name.
.NOTES
Critical dependencies: the current repo-local MCP naming contract.
#>
function Get-RepoScopedMcpServerName {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedRepoRoot
  )

  return [string]$pathCanon.names.default_plugin_name
}

$expectedMarketplaceIdentity = Get-RepoScopedMarketplaceIdentity -ResolvedRepoRoot $repoRoot
$expectedMcpServerName = Get-RepoScopedMcpServerName -ResolvedRepoRoot $repoRoot

<#
.SYNOPSIS
Detects whether a marketplace entry represents Anarchy-AI.
.DESCRIPTION
Matches either the plugin name prefix or a supported repo-local or user-profile source path.
.PARAMETER Plugin
Marketplace plugin entry object to inspect.
.OUTPUTS
System.Boolean. True when the entry represents Anarchy-AI.
.NOTES
Critical dependencies: path-canon marketplace prefixes and the current plugin naming contract.
#>
function Test-IsAnarchyPluginEntry {
  param(
    [Parameter(Mandatory = $true)]
    $Plugin
  )

  $pluginName = ''
  if ($null -ne $Plugin.name) {
    $pluginName = [string]$Plugin.name
  }

  if (Test-IsOwnedPluginName -PluginName $pluginName) {
    return $true
  }

  $pluginPath = ''
  if ($null -ne $Plugin.source -and $null -ne $Plugin.source.path) {
    $pluginPath = [string]$Plugin.source.path
  }

  if (
    -not $pluginPath.StartsWith([string]$pathCanon.relative_references.repo_local_marketplace_plugin_source_prefix, [System.StringComparison]::OrdinalIgnoreCase) `
    -and -not $pluginPath.StartsWith([string]$pathCanon.relative_references.user_profile_marketplace_plugin_source_prefix, [System.StringComparison]::OrdinalIgnoreCase)
  ) {
    return $false
  }

  $normalizedPluginPath = $pluginPath.Replace('\', '/').TrimStart('./')
  $pluginLeaf = Split-Path -Leaf $normalizedPluginPath
  return Test-IsOwnedPluginName -PluginName $pluginLeaf
}

<#
.SYNOPSIS
Realigns the plugin-local .mcp.json declaration to the expected repo-scoped identity.
.DESCRIPTION
Loads or rebuilds the declaration, ensures only the expected server remains, and writes the canonical runtime command block.
.PARAMETER McpConfigPath
Plugin-local .mcp.json path to update.
.PARAMETER ExpectedServerName
Server name that should remain in the final declaration.
.OUTPUTS
No direct return value. Mutates the plugin-local .mcp.json file and action log when needed.
.NOTES
Critical dependencies: the generated path canon, JSON parsing, and the script-scoped actionsTaken list.
#>
function Set-RepoScopedMcpConfiguration {
  param(
    [Parameter(Mandatory = $true)]
    [string]$McpConfigPath,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedServerName
  )

  $configChanged = $false
  $rootObject = $null

  if (Test-Path $McpConfigPath) {
    try {
      $rootObject = Get-Content $McpConfigPath -Raw | ConvertFrom-Json
    }
    catch {
      $rootObject = $null
      $actionsTaken.Add('replaced_invalid_mcp_declaration')
      $configChanged = $true
    }
  }
  else {
    $configChanged = $true
  }

  if ($null -eq $rootObject) {
    $rootObject = [ordered]@{}
  }

  $currentServers = $rootObject.mcpServers
  $currentServer = $null
  $hasExpectedServer = $false

  if ($null -ne $currentServers) {
    foreach ($property in $currentServers.PSObject.Properties) {
      if ($property.Name -eq $ExpectedServerName) {
        $hasExpectedServer = $true
        $currentServer = $property.Value
      }
      else {
        $configChanged = $true
      }
    }
  }
  else {
    $configChanged = $true
  }

  if (-not $hasExpectedServer) {
    $configChanged = $true
  }

  if ($null -eq $currentServer) {
    $currentServer = [pscustomobject]@{}
  }

  if ($currentServer.command -ne [string]$pathCanon.relative_references.bundle_runtime_windows_command_relative_path) {
    $configChanged = $true
  }
  if ($currentServer.cwd -ne [string]$pathCanon.relative_references.bundle_runtime_working_directory_relative_path) {
    $configChanged = $true
  }
  if ($null -eq $currentServer.args -or @($currentServer.args).Count -ne 0) {
    $configChanged = $true
  }

  if ($configChanged) {
    $rootObject = [ordered]@{
      mcpServers = [ordered]@{
        $ExpectedServerName = [ordered]@{
          command = [string]$pathCanon.relative_references.bundle_runtime_windows_command_relative_path
          args = @()
          cwd = [string]$pathCanon.relative_references.bundle_runtime_working_directory_relative_path
        }
      }
    }

    Write-TextFile -Path $McpConfigPath -Content ($rootObject | ConvertTo-Json -Depth 10)
    $actionsTaken.Add('updated_repo_mcp_server_identity')
  }
}

<#
.SYNOPSIS
Realigns the plugin manifest name to the expected repo-local identity.
.DESCRIPTION
Loads or rebuilds the manifest and updates the `name` property when it is missing or stale.
.PARAMETER PluginManifestPath
Plugin manifest path to update.
.PARAMETER ExpectedPluginName
Repo-scoped plugin name that should be written.
.OUTPUTS
No direct return value. Mutates the plugin manifest file and action log when needed.
.NOTES
Critical dependencies: JSON parsing and the script-scoped actionsTaken list.
#>
function Set-RepoScopedPluginManifest {
  param(
    [Parameter(Mandatory = $true)]
    [string]$PluginManifestPath,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedPluginName
  )

  if (-not (Test-Path $PluginManifestPath)) {
    return
  }

  $manifestObject = $null
  try {
    $manifestObject = Get-Content $PluginManifestPath -Raw | ConvertFrom-Json
  }
  catch {
    $manifestObject = [ordered]@{}
    $actionsTaken.Add('replaced_invalid_plugin_manifest')
  }

  if ($null -eq $manifestObject.name -or $manifestObject.name -ne $ExpectedPluginName) {
    if ($manifestObject.PSObject.Properties.Name -contains 'name') {
      $manifestObject.name = $ExpectedPluginName
    }
    else {
      $manifestObject | Add-Member -NotePropertyName name -NotePropertyValue $ExpectedPluginName -Force
    }

    Write-TextFile -Path $PluginManifestPath -Content ($manifestObject | ConvertTo-Json -Depth 10)
    $actionsTaken.Add('updated_repo_plugin_identity')
  }
}

<#
.SYNOPSIS
Removes an existing file or directory only when it is still inside the expected root.
.DESCRIPTION
Normalizes attributes first so stale readonly or synced files can be removed safely.
.PARAMETER PathToRemove
Target file or directory path to remove.
.PARAMETER ExpectedRoot
Absolute root path that the target must remain beneath.
.OUTPUTS
No direct return value. Removes the target when present.
.NOTES
Critical dependencies: same-scope root validation and Remove-Item.
#>
function Remove-CanonicalTarget {
  param(
    [Parameter(Mandatory = $true)]
    [string]$PathToRemove,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRoot
  )

  if (-not (Test-Path $PathToRemove)) {
    return
  }

  $resolvedTarget = [System.IO.Path]::GetFullPath($PathToRemove)
  $resolvedRoot = [System.IO.Path]::GetFullPath($ExpectedRoot)
  if (-not $resolvedTarget.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove path outside expected root: $resolvedTarget"
  }

  $targetItem = Get-Item $resolvedTarget -Force
  if ($targetItem.PSIsContainer) {
    Get-ChildItem $resolvedTarget -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
      try {
        $_.Attributes = 'Normal'
      }
      catch {
      }
    }

    try {
      $targetItem.Attributes = 'Directory'
    }
    catch {
    }
  }
  else {
    try {
      $targetItem.Attributes = 'Normal'
    }
    catch {
    }
  }

  Remove-Item $resolvedTarget -Recurse -Force
}

<#
.SYNOPSIS
Copies one canonical bundle or schema surface into the expected destination root.
.DESCRIPTION
Creates missing parent directories and supports both file and directory copy behavior.
.PARAMETER SourcePath
Canonical source file or directory to copy.
.PARAMETER TargetPath
Destination file or directory path.
.PARAMETER ExpectedRoot
Expected destination root retained for caller-side safety reasoning.
.OUTPUTS
No direct return value. Copies the requested surface.
.NOTES
Critical dependencies: caller-provided source validation, filesystem write access, and Copy-Item.
#>
function Copy-CanonicalSurface {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRoot
  )

  if (-not (Test-Path $SourcePath)) {
    throw "Missing canonical source surface: $SourcePath"
  }

  $targetParent = Split-Path $TargetPath -Parent
  if (-not (Test-Path $targetParent)) {
    New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
  }

  $sourceItem = Get-Item $SourcePath -Force
  if ($sourceItem.PSIsContainer) {
    if (-not (Test-Path $TargetPath)) {
      New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
    }

    foreach ($child in Get-ChildItem $SourcePath -Force) {
      Copy-Item $child.FullName $TargetPath -Recurse -Force
    }
    return
  }

  Copy-Item $SourcePath $TargetPath -Force
}

<#
.SYNOPSIS
Refreshes script-scoped existence flags and missing-component findings for the current bundle.
.DESCRIPTION
Inspects the core plugin surfaces and expected contract files after assess, install, or update work.
.OUTPUTS
No direct return value. Mutates script-scoped existence flags and the missingComponents collection.
.NOTES
Critical dependencies: resolved bundle paths, contract file list, and the script-scoped collections.
#>
function Refresh-BundlePresenceState {
  $script:pluginManifestExists = Test-Path $pluginManifestPath
  $script:mcpExists = Test-Path $mcpPath
  $script:runtimeExists = Test-Path $runtimePath
  $script:skillExists = Test-Path $skillPath
  $script:schemaManifestExists = Test-Path $schemaManifestPath

  if (-not $pluginManifestExists) { $missingComponents.Add('codex_plugin_manifest_missing') }
  if (-not $mcpExists) { $missingComponents.Add('codex_mcp_declaration_missing') }
  if (-not $runtimeExists) { $missingComponents.Add('bundled_runtime_missing') }
  if (-not $skillExists -and $HostContext -eq 'codex') { $missingComponents.Add('codex_skill_surface_missing') }
  if (-not $schemaManifestExists) { $missingComponents.Add('schema_bundle_manifest_missing') }

  foreach ($contractFile in $contractFiles) {
    if (-not (Test-Path (Join-Path $pluginRoot (Join-Path 'contracts' $contractFile)))) {
      $missingComponents.Add("missing_contract:$contractFile")
    }
  }
}

<#
.SYNOPSIS
Downloads the update archive from the configured source URL.
.DESCRIPTION
Uses Invoke-WebRequest first and falls back to curl.exe when the PowerShell download path fails.
.PARAMETER Uri
Source archive URL.
.PARAMETER OutFile
Destination file path for the downloaded archive.
.OUTPUTS
System.String. Download method identifier.
.NOTES
Critical dependencies: outbound network access, TLS 1.2 support, and optional curl.exe availability.
#>
function Get-UpdateArchive {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Uri,
    [Parameter(Mandatory = $true)]
    [string]$OutFile
  )

  try {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $Uri -OutFile $OutFile
    return 'invoke_web_request'
  }
  catch {
    $invokeError = $_.Exception.Message
    $curl = Get-Command 'curl.exe' -ErrorAction SilentlyContinue
    if ($null -eq $curl) {
      throw "Invoke-WebRequest failed: $invokeError"
    }

    & $curl.Source -L --fail --silent --show-error $Uri -o $OutFile
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $OutFile)) {
      throw "Invoke-WebRequest failed: $invokeError; curl.exe fallback also failed with exit code $LASTEXITCODE."
    }

    return 'curl_fallback'
  }
}

<#
.SYNOPSIS
Resolves a local update source into an extracted repo root.
.DESCRIPTION
Accepts either a directory or zip file and returns the repo root that should be used for canonical refresh.
.PARAMETER SourcePath
Local repo directory or zip file path.
.PARAMETER TempRoot
Temporary root used when a zip must be extracted.
.OUTPUTS
FileSystemInfo representing the resolved source root.
.NOTES
Critical dependencies: local filesystem access and Expand-Archive for zip inputs.
#>
function Resolve-LocalUpdateSourceRoot {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,
    [Parameter(Mandatory = $true)]
    [string]$TempRoot
  )

  if (-not (Test-Path $SourcePath)) {
    throw "Update source path does not exist: $SourcePath"
  }

  $resolvedPath = [System.IO.Path]::GetFullPath($SourcePath)
  $item = Get-Item $resolvedPath

  if ($item.PSIsContainer) {
    return $item
  }

  if ($item.Extension -ieq '.zip') {
    $extractPath = Join-Path $TempRoot 'local-extract'
    Expand-Archive -Path $resolvedPath -DestinationPath $extractPath -Force
    $sourceRoot = Get-ChildItem $extractPath -Directory | Select-Object -First 1
    if ($null -eq $sourceRoot) {
      throw "Local update zip did not produce an extractable repository root: $resolvedPath"
    }

    return $sourceRoot
  }

  throw "Update source path must be a repository directory or zip file: $resolvedPath"
}

$pluginManifestExists = $false
$mcpExists = $false
$runtimeExists = $false
$skillExists = $false
$schemaManifestExists = $false

if ($Update) {
  $updateState = 'in_progress'
  $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("anarchy-ai-update-" + [guid]::NewGuid().ToString('N'))

  try {
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    $zipPath = Join-Path $tempRoot 'ai-links.zip'
    $extractPath = Join-Path $tempRoot 'extract'
    $sourceRoot = $null
    $usedLocalUpdateSource = $false

    if (-not [string]::IsNullOrWhiteSpace($UpdateSourcePath)) {
      $sourceRoot = Resolve-LocalUpdateSourceRoot -SourcePath $UpdateSourcePath -TempRoot $tempRoot
      $usedLocalUpdateSource = $true
      $updateNotes.Add('used_local_update_source_path')
    }
    else {
      $downloadMethod = Get-UpdateArchive -Uri $UpdateSourceZipUrl -OutFile $zipPath
      if ($downloadMethod -eq 'curl_fallback') {
        $updateNotes.Add('downloaded_update_archive_with_curl_fallback')
      }

      Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
      $sourceRoot = Get-ChildItem $extractPath -Directory | Select-Object -First 1
      if ($null -eq $sourceRoot) {
        throw 'Update archive did not produce an extractable repository root.'
      }
    }

    $effectiveSourceRoot = $sourceRoot.FullName
    $sourcePluginRoot = Resolve-CanonRelativePath -RootPath $effectiveSourceRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path
    if (-not (Test-Path $sourcePluginRoot)) {
      throw "Update archive did not contain $($pathCanon.relative_paths.repo_source_plugin_directory_relative_path)."
    }

    $pluginSurfaces = @($pathCanon.arrays.plugin_surfaces)

    foreach ($surface in $pluginSurfaces) {
      Copy-CanonicalSurface `
        -SourcePath (Join-Path $sourcePluginRoot $surface) `
        -TargetPath (Join-Path $pluginRoot $surface) `
        -ExpectedRoot $pluginRoot
    }

    Set-RepoScopedPluginManifest -PluginManifestPath $pluginManifestPath -ExpectedPluginName $expectedPluginName
    Set-RepoScopedMcpConfiguration -McpConfigPath $mcpPath -ExpectedServerName $expectedMcpServerName

    if ($usedLocalUpdateSource) {
      $actionsTaken.Add('refreshed_plugin_bundle_from_local_update_source')
    }
    else {
      $actionsTaken.Add('refreshed_plugin_bundle_from_public_repo')
    }

    $shouldRefreshPortableSchemaFamily = $RefreshPortableSchemaFamily

    if ($shouldRefreshPortableSchemaFamily) {
      foreach ($schemaFile in $portableSchemaFiles) {
        Copy-CanonicalSurface `
          -SourcePath (Join-Path $sourceRoot.FullName $schemaFile) `
          -TargetPath (Join-Path $repoRoot $schemaFile) `
          -ExpectedRoot $repoRoot
      }

      if ($usedLocalUpdateSource) {
        $actionsTaken.Add('refreshed_portable_schema_family_from_local_update_source')
      }
      else {
        $actionsTaken.Add('refreshed_portable_schema_family_from_public_repo')
      }
    }
    else {
      $updateNotes.Add('portable_schema_family_refresh_not_requested')
    }

    $updateState = 'completed'
  }
  catch {
    $updateState = 'failed'
    $updateFailureMessage = $_.Exception.Message
    if ($updateFailureMessage -like '*being used by another process*') {
      $updateRuntimeLocked = $true
    }

    $missingComponents.Add('update_pull_failed')
    if ($updateRuntimeLocked) {
      $safeRepairs.Add('release_runtime_lock_and_retry_update')
      $safeRepairs.Add('run_safe_release_runtime_lock')
      $safeRepairs.Add('run_force_release_runtime_lock')
    }
    elseif ([string]::IsNullOrWhiteSpace($UpdateSourcePath)) {
      $safeRepairs.Add('verify_public_repo_access_and_retry_update')
      $safeRepairs.Add('retry_update_with_local_source_path')
    }
    else {
      $safeRepairs.Add('verify_local_update_source_path_and_retry_update')
    }

    $updateNotes.Add($updateFailureMessage)
  }
  finally {
    if (Test-Path $tempRoot) {
      Remove-Item $tempRoot -Recurse -Force
    }
  }
}

if ($Update) {
  $updateFailed = $updateState -eq 'failed'
  $missingComponents.Clear()
  Refresh-BundlePresenceState
  if ($updateFailed) {
    $missingComponents.Add('update_pull_failed')
    if ($updateRuntimeLocked) {
      $safeRepairs.Add('release_runtime_lock_and_retry_update')
      $safeRepairs.Add('run_safe_release_runtime_lock')
      $safeRepairs.Add('run_force_release_runtime_lock')
    }
    elseif ([string]::IsNullOrWhiteSpace($UpdateSourcePath)) {
      $safeRepairs.Add('verify_public_repo_access_and_retry_update')
      $safeRepairs.Add('retry_update_with_local_source_path')
    }
    else {
      $safeRepairs.Add('verify_local_update_source_path_and_retry_update')
    }
  }
}
else {
  Refresh-BundlePresenceState
}

$marketplaceExists = Test-Path $marketplacePath
$marketplaceHasEntry = $false
$installedByDefault = $false
$marketplaceObject = $null
$mcpIdentityAligned = $false
$pluginManifestIdentityAligned = $false

if ($marketplaceExists) {
  try {
    $marketplaceObject = Get-Content $marketplacePath -Raw | ConvertFrom-Json
    if ($null -eq $marketplaceObject.plugins) {
      $missingComponents.Add('repo_marketplace_missing_plugins_array')
    }
    else {
      $expectedPluginEntry = $null
      $legacyPluginEntry = $null
      foreach ($plugin in $marketplaceObject.plugins) {
        if ($plugin.name -eq $expectedPluginName -and $plugin.source.path -eq $expectedPluginRelativePath) {
          $expectedPluginEntry = $plugin
        }
        elseif ($null -eq $legacyPluginEntry -and (Test-IsAnarchyPluginEntry -Plugin $plugin)) {
          $legacyPluginEntry = $plugin
        }
      }

      $effectivePluginEntry = if ($null -ne $expectedPluginEntry) { $expectedPluginEntry } else { $legacyPluginEntry }
      if ($null -ne $effectivePluginEntry) {
        $marketplaceHasEntry = $true
        if ($effectivePluginEntry.policy.installation -eq 'INSTALLED_BY_DEFAULT') {
          $installedByDefault = $true
        }
      }

      if ($null -eq $expectedPluginEntry -and $null -ne $legacyPluginEntry) {
        $missingComponents.Add('repo_plugin_identity_outdated')
      }
    }

    if ($marketplaceObject.name -ne $expectedMarketplaceIdentity.name) {
      $missingComponents.Add('repo_marketplace_identity_outdated')
    }
  }
  catch {
    $missingComponents.Add('marketplace_json_invalid')
  }
}
else {
  $missingComponents.Add('repo_marketplace_missing')
}

$pluginManifestExists = Test-Path $pluginManifestPath
if ($pluginManifestExists) {
  try {
    $pluginManifestObject = Get-Content $pluginManifestPath -Raw | ConvertFrom-Json
    if ($pluginManifestObject.name -eq $expectedPluginName) {
      $pluginManifestIdentityAligned = $true
    }
    else {
      $missingComponents.Add('repo_plugin_identity_outdated')
    }
  }
  catch {
    $missingComponents.Add('repo_plugin_identity_outdated')
  }
}

$mcpExists = Test-Path $mcpPath
if ($mcpExists) {
  try {
    $mcpObject = Get-Content $mcpPath -Raw | ConvertFrom-Json
    if ($null -ne $mcpObject.mcpServers) {
      $mcpProperties = @($mcpObject.mcpServers.PSObject.Properties)
      if ($mcpProperties.Count -eq 1 -and $mcpProperties[0].Name -eq $expectedMcpServerName) {
        $mcpIdentityAligned = $true
      }
    }

    if (-not $mcpIdentityAligned) {
      $missingComponents.Add('repo_mcp_server_identity_outdated')
    }
  }
  catch {
    $missingComponents.Add('repo_mcp_server_identity_outdated')
  }
}

if ($Mode -eq 'Install') {
  if (-not (Test-Path (Split-Path $marketplacePath -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $marketplacePath -Parent) -Force | Out-Null
    $actionsTaken.Add('created_marketplace_directory')
  }

  if (-not $marketplaceObject) {
    $marketplaceObject = [ordered]@{
      name = $expectedMarketplaceIdentity.name
      interface = [ordered]@{ displayName = $expectedMarketplaceIdentity.display_name }
      plugins = @()
    }
  }

  if ($marketplaceObject.name -ne $expectedMarketplaceIdentity.name) {
    $marketplaceObject.name = $expectedMarketplaceIdentity.name
    $actionsTaken.Add('updated_repo_marketplace_identity')
  }

  if ($null -eq $marketplaceObject.interface) {
    $marketplaceObject | Add-Member -NotePropertyName interface -NotePropertyValue ([pscustomobject]@{}) -Force
    $actionsTaken.Add('updated_repo_marketplace_identity')
  }

  if ($marketplaceObject.interface.displayName -ne $expectedMarketplaceIdentity.display_name) {
    $marketplaceObject.interface.displayName = $expectedMarketplaceIdentity.display_name
    $actionsTaken.Add('updated_repo_marketplace_identity')
  }

  if ($null -eq $marketplaceObject.plugins) {
    $marketplaceObject | Add-Member -NotePropertyName plugins -NotePropertyValue @() -Force
  }

  $retainedPlugins = @()
  $existing = @()
  foreach ($plugin in @($marketplaceObject.plugins)) {
    if (Test-IsAnarchyPluginEntry -Plugin $plugin) {
      $existing += $plugin
    }
    else {
      $retainedPlugins += $plugin
    }
  }

  $marketplaceObject.plugins = @($retainedPlugins)
  if ($existing.Count -eq 0) {
    $marketplaceObject.plugins += [pscustomobject]@{
      name = $expectedPluginName
      source = [pscustomobject]@{
        source = 'local'
        path = $expectedPluginRelativePath
      }
      policy = [pscustomobject]@{
        installation = 'INSTALLED_BY_DEFAULT'
        authentication = 'ON_INSTALL'
      }
      category = 'Productivity'
    }
    $actionsTaken.Add('created_anarchy_ai_marketplace_entry')
  }
  else {
    if ($existing.Count -gt 1) {
      $actionsTaken.Add('removed_stale_anarchy_ai_marketplace_entry')
    }

    $marketplaceObject.plugins += [pscustomobject]@{
      name = $expectedPluginName
      source = [pscustomobject]@{
        source = 'local'
        path = $expectedPluginRelativePath
      }
      policy = [pscustomobject]@{
        installation = 'INSTALLED_BY_DEFAULT'
        authentication = 'ON_INSTALL'
      }
      category = 'Productivity'
    }

    $actionsTaken.Add('updated_anarchy_ai_marketplace_entry')
  }

  Set-RepoScopedPluginManifest -PluginManifestPath $pluginManifestPath -ExpectedPluginName $expectedPluginName
  Set-RepoScopedMcpConfiguration -McpConfigPath $mcpPath -ExpectedServerName $expectedMcpServerName

  Write-TextFile -Path $marketplacePath -Content ($marketplaceObject | ConvertTo-Json -Depth 10)
  $marketplaceExists = $true
  $marketplaceHasEntry = $true
  $installedByDefault = $true
  $pluginManifestIdentityAligned = $true
  $mcpIdentityAligned = $true
  foreach ($resolvedMarketplaceGap in @(
    'repo_marketplace_missing',
    'repo_marketplace_missing_plugins_array',
    'marketplace_json_invalid',
    'repo_marketplace_identity_outdated',
    'repo_plugin_identity_outdated'
  )) {
    [void]$missingComponents.Remove($resolvedMarketplaceGap)
  }

  [void]$missingComponents.Remove('repo_mcp_server_identity_outdated')
}

if (-not $runtimeExists) { $safeRepairs.Add('publish_or_restore_bundled_runtime') }
if (-not $marketplaceHasEntry -or -not $installedByDefault) { $safeRepairs.Add('run_bootstrap_harness_install') }
if ($missingComponents -contains 'repo_plugin_identity_outdated') { $safeRepairs.Add('refresh_repo_plugin_identity') }
if ($missingComponents -contains 'repo_marketplace_identity_outdated') { $safeRepairs.Add('refresh_repo_marketplace_identity') }
if ($missingComponents -contains 'repo_mcp_server_identity_outdated') { $safeRepairs.Add('refresh_repo_mcp_server_identity') }
if ($missingComponents -contains 'claude_adapter_not_packaged') { $safeRepairs.Add('define_claude_mcp_registration') }
if ($missingComponents -contains 'cursor_adapter_not_implemented') { $safeRepairs.Add('define_cursor_adapter_strategy') }

$bootstrapState = if ($runtimeExists -and $marketplaceHasEntry -and $installedByDefault -and $pluginManifestIdentityAligned -and -not ($missingComponents -contains 'repo_marketplace_identity_outdated') -and -not ($missingComponents -contains 'repo_mcp_server_identity_outdated')) {
  'ready'
}
elseif ($runtimeExists -and $marketplaceHasEntry -and $installedByDefault) {
  'registration_refresh_needed'
}
elseif ($runtimeExists -and ($pluginManifestExists -or $mcpExists)) {
  'repo_bundle_present_unregistered'
}
elseif ($runtimeExists) {
  'runtime_only'
}
else {
  'bootstrap_needed'
}

$nextAction = switch ($bootstrapState) {
  'ready' { 'use_preflight_session' }
  'registration_refresh_needed' { 'refresh_plugin_registration' }
  'repo_bundle_present_unregistered' { 'register_plugin_in_marketplace' }
  'runtime_only' { 'materialize_repo_plugin_bundle' }
  default { 'restore_runtime_or_complete_bundle' }
}

<#
.SYNOPSIS
Builds a hashtable that omits null and blank values.
.DESCRIPTION
Used to keep nested path-role output compact and free of placeholder entries.
.PARAMETER Values
Hashtable of candidate values.
.OUTPUTS
Ordered hashtable or null when every value was omitted.
.NOTES
Critical dependencies: the nested path-report contract and caller-provided keyed values.
#>
function New-OptionalMap {
  param(
    [Parameter(Mandatory = $true)]
    [hashtable]$Values
  )

  $result = [ordered]@{}
  foreach ($key in $Values.Keys) {
    $value = $Values[$key]
    if ($null -eq $value) {
      continue
    }

    if ($value -is [string] -and [string]::IsNullOrWhiteSpace($value)) {
      continue
    }

    $result[$key] = $value
  }

  if ($result.Count -eq 0) {
    return $null
  }

  return $result
}

<#
.SYNOPSIS
Builds one nested path-role report for origin, source, or destination output.
.DESCRIPTION
Adds root, directories, files, and relative sections only when they contain meaningful values.
.PARAMETER RootPath
Optional absolute root path for the role.
.PARAMETER Directories
Optional keyed directory paths.
.PARAMETER Files
Optional keyed file paths.
.PARAMETER Relative
Optional keyed relative-path values.
.OUTPUTS
Ordered hashtable or null when the role has no populated members.
.NOTES
Critical dependencies: New-OptionalMap and the nested path-report contract.
#>
function New-PathRoleReport {
  param(
    [string]$RootPath,
    [hashtable]$Directories,
    [hashtable]$Files,
    [hashtable]$Relative
  )

  $report = [ordered]@{}
  if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
    $report.root_path = $RootPath
  }

  $directoryMap = if ($null -ne $Directories) { New-OptionalMap -Values $Directories } else { $null }
  $fileMap = if ($null -ne $Files) { New-OptionalMap -Values $Files } else { $null }
  $relativeMap = if ($null -ne $Relative) { New-OptionalMap -Values $Relative } else { $null }

  if ($null -ne $directoryMap) {
    $report.directories = $directoryMap
  }
  if ($null -ne $fileMap) {
    $report.files = $fileMap
  }
  if ($null -ne $relativeMap) {
    $report.relative = $relativeMap
  }

  if ($report.Count -eq 0) {
    return $null
  }

  return $report
}

$originPaths = New-PathRoleReport `
  -RootPath $repoRoot `
  -Directories ([ordered]@{
    plugin_source_directory_path = Resolve-CanonRelativePath -RootPath $repoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path
  }) `
  -Files ([ordered]@{
    plugin_mcp_file_path = Resolve-CanonRelativePath -RootPath $repoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_mcp_file_relative_path
    setup_executable_file_path = Resolve-CanonRelativePath -RootPath $repoRoot -RelativePath $pathCanon.relative_paths.repo_source_setup_executable_file_relative_path
  }) `
  -Relative ([ordered]@{
    plugin_source_directory_relative_path = [string]$pathCanon.relative_paths.repo_source_plugin_directory_relative_path
    plugin_mcp_file_relative_path = [string]$pathCanon.relative_paths.repo_source_plugin_mcp_file_relative_path
    setup_executable_file_relative_path = [string]$pathCanon.relative_paths.repo_source_setup_executable_file_relative_path
  })

$sourcePaths = if ([string]::Equals($effectiveSourceRoot, $pluginRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
  New-PathRoleReport `
    -RootPath $pluginRoot `
    -Directories ([ordered]@{
      contracts_directory_path = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_contracts_directory_relative_path
      runtime_directory_path = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_runtime_directory_relative_path
      schemas_directory_path = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_schemas_directory_relative_path
      scripts_directory_path = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_scripts_directory_relative_path
      skill_directory_path = Resolve-BundlePath -RelativePath $pathCanon.relative_paths.bundle_skill_directory_relative_path
    }) `
    -Files ([ordered]@{
      plugin_manifest_file_path = $pluginManifestPath
      mcp_declaration_file_path = $mcpPath
      runtime_executable_file_path = $runtimePath
      schema_manifest_file_path = $schemaManifestPath
      skill_file_path = $skillPath
    }) `
    -Relative ([ordered]@{
      plugin_manifest_file_relative_path = [string]$pathCanon.relative_paths.bundle_plugin_manifest_file_relative_path
      mcp_declaration_file_relative_path = [string]$pathCanon.relative_paths.bundle_mcp_file_relative_path
      runtime_executable_file_relative_path = [string]$pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
      schema_manifest_file_relative_path = [string]$pathCanon.relative_paths.bundle_schema_manifest_file_relative_path
      skill_file_relative_path = [string]$pathCanon.relative_paths.bundle_skill_file_relative_path
    })
}
else {
  New-PathRoleReport `
    -RootPath $effectiveSourceRoot `
    -Directories ([ordered]@{
      plugin_source_directory_path = Resolve-CanonRelativePath -RootPath $effectiveSourceRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path
    }) `
    -Files ([ordered]@{
      plugin_mcp_file_path = Resolve-CanonRelativePath -RootPath $effectiveSourceRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_mcp_file_relative_path
      setup_executable_file_path = Resolve-CanonRelativePath -RootPath $effectiveSourceRoot -RelativePath $pathCanon.relative_paths.repo_source_setup_executable_file_relative_path
    }) `
    -Relative ([ordered]@{
      plugin_source_directory_relative_path = [string]$pathCanon.relative_paths.repo_source_plugin_directory_relative_path
      plugin_mcp_file_relative_path = [string]$pathCanon.relative_paths.repo_source_plugin_mcp_file_relative_path
      setup_executable_file_relative_path = [string]$pathCanon.relative_paths.repo_source_setup_executable_file_relative_path
    })
}

$destinationPaths = New-PathRoleReport `
  -RootPath $repoRoot `
  -Directories ([ordered]@{
    plugin_root_directory_path = $pluginRoot
    marketplace_directory_path = Split-Path -Parent $marketplacePath
    schema_target_root_directory_path = $repoRoot
  }) `
  -Files ([ordered]@{
    marketplace_file_path = $marketplacePath
    plugin_manifest_file_path = $pluginManifestPath
    mcp_declaration_file_path = $mcpPath
    runtime_executable_file_path = $runtimePath
    schema_manifest_file_path = $schemaManifestPath
    skill_file_path = $skillPath
  }) `
  -Relative ([ordered]@{
    marketplace_file_relative_path = [string]$pathCanon.relative_paths.repo_local_marketplace_file_relative_path
    plugin_source_relative_path = $expectedPluginRelativePath
    plugin_manifest_file_relative_path = [string]$pathCanon.relative_paths.bundle_plugin_manifest_file_relative_path
    mcp_declaration_file_relative_path = [string]$pathCanon.relative_paths.bundle_mcp_file_relative_path
    runtime_executable_file_relative_path = [string]$pathCanon.relative_paths.bundle_runtime_executable_file_relative_path
    schema_manifest_file_relative_path = [string]$pathCanon.relative_paths.bundle_schema_manifest_file_relative_path
    skill_file_relative_path = [string]$pathCanon.relative_paths.bundle_skill_file_relative_path
  })

$result = [ordered]@{
  bootstrap_state = $bootstrapState
  host_context = $HostContext
  update_requested = [bool]$Update
  update_state = $updateState
  update_source_zip_url = $UpdateSourceZipUrl
  update_notes = @($updateNotes | Select-Object -Unique)
  runtime_present = $runtimeExists
  marketplace_registered = $marketplaceHasEntry
  installed_by_default = $installedByDefault
  actions_taken = @($actionsTaken | Select-Object -Unique)
  missing_components = @($missingComponents | Select-Object -Unique)
  safe_repairs = @($safeRepairs | Select-Object -Unique)
  next_action = $nextAction
  paths = [ordered]@{
    origin = $originPaths
    source = $sourcePaths
    destination = $destinationPaths
  }
}

$result | ConvertTo-Json -Depth 10

if ($bootstrapState -eq 'ready') {
  exit 0
}

exit 1
