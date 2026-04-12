param(
  [ValidateSet('Assess','Install')]
  [string]$Mode = 'Assess',
  [ValidateSet('codex','claude','cursor','generic')]
  [string]$HostContext = 'codex',
  [switch]$Update,
  [switch]$RefreshPortableSchemaFamily,
  [string]$UpdateSourceZipUrl = 'https://github.com/BraintekMSP/AI-Links/archive/refs/heads/main.zip',
  [string]$UpdateSourcePath = ''
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir '..'))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $pluginRoot '..\..'))
$marketplacePath = Join-Path $repoRoot '.agents\plugins\marketplace.json'
$runtimePath = Join-Path $pluginRoot 'runtime\win-x64\AnarchyAi.Mcp.Server.exe'
$pluginManifestPath = Join-Path $pluginRoot '.codex-plugin\plugin.json'
$mcpPath = Join-Path $pluginRoot '.mcp.json'
$skillPath = Join-Path $pluginRoot 'skills\anarchy-ai-harness\SKILL.md'
$schemaManifestPath = Join-Path $pluginRoot 'schemas\schema-bundle.manifest.json'
$portableSchemaFiles = @(
  'AGENTS-schema-governance.json',
  'AGENTS-schema-1project.json',
  'AGENTS-schema-narrative.json',
  'AGENTS-schema-gov2gov-migration.json',
  'AGENTS-schema-triage.md',
  'Getting-Started-For-Humans.txt'
)
$contractFiles = @(
  'active-work-state.contract.json',
  'schema-reality.contract.json',
  'gov2gov-migration.contract.json',
  'preflight-session.contract.json',
  'harness-gap-state.contract.json'
)

$actionsTaken = New-Object System.Collections.Generic.List[string]
$missingComponents = New-Object System.Collections.Generic.List[string]
$safeRepairs = New-Object System.Collections.Generic.List[string]
$updateState = 'not_requested'
$updateRuntimeLocked = $false
$updateNotes = New-Object System.Collections.Generic.List[string]

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

    $sourcePluginRoot = Join-Path $sourceRoot.FullName 'plugins\\anarchy-ai'
    if (-not (Test-Path $sourcePluginRoot)) {
      throw 'Update archive did not contain plugins\\anarchy-ai.'
    }

    $pluginSurfaces = @(
      '.codex-plugin',
      'assets',
      'contracts',
      'runtime',
      'schemas',
      'scripts',
      'skills',
      '.mcp.json',
      'README.md',
      'PRIVACY.md',
      'TERMS.md'
    )

    foreach ($surface in $pluginSurfaces) {
      Copy-CanonicalSurface `
        -SourcePath (Join-Path $sourcePluginRoot $surface) `
        -TargetPath (Join-Path $pluginRoot $surface) `
        -ExpectedRoot $pluginRoot
    }

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

if ($marketplaceExists) {
  try {
    $marketplaceObject = Get-Content $marketplacePath -Raw | ConvertFrom-Json
    if ($null -eq $marketplaceObject.plugins) {
      $missingComponents.Add('repo_marketplace_missing_plugins_array')
    }
    else {
      foreach ($plugin in $marketplaceObject.plugins) {
        if ($plugin.name -eq 'anarchy-ai') {
          $marketplaceHasEntry = $true
          if ($plugin.policy.installation -eq 'INSTALLED_BY_DEFAULT') {
            $installedByDefault = $true
          }
        }
      }
    }
  }
  catch {
    $missingComponents.Add('marketplace_json_invalid')
  }
}
else {
  $missingComponents.Add('repo_marketplace_missing')
}

if ($Mode -eq 'Install') {
  if (-not (Test-Path (Split-Path $marketplacePath -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $marketplacePath -Parent) -Force | Out-Null
    $actionsTaken.Add('created_marketplace_directory')
  }

  if (-not $marketplaceObject) {
    $marketplaceObject = [ordered]@{
      name = 'ai-links-local'
      interface = [ordered]@{ displayName = 'AI-Links Local' }
      plugins = @()
    }
  }

  if ($null -eq $marketplaceObject.plugins) {
    $marketplaceObject | Add-Member -NotePropertyName plugins -NotePropertyValue @() -Force
  }

  $existing = @($marketplaceObject.plugins | Where-Object { $_.name -eq 'anarchy-ai' })
  if ($existing.Count -eq 0) {
    $marketplaceObject.plugins += [pscustomobject]@{
      name = 'anarchy-ai'
      source = [pscustomobject]@{
        source = 'local'
        path = './plugins/anarchy-ai'
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
    foreach ($plugin in $existing) {
      $plugin.source.source = 'local'
      $plugin.source.path = './plugins/anarchy-ai'
      $plugin.policy.installation = 'INSTALLED_BY_DEFAULT'
      $plugin.policy.authentication = 'ON_INSTALL'
      $plugin.category = 'Productivity'
    }
    $actionsTaken.Add('updated_anarchy_ai_marketplace_entry')
  }

  $marketplaceObject | ConvertTo-Json -Depth 10 | Set-Content $marketplacePath
  $marketplaceExists = $true
  $marketplaceHasEntry = $true
  $installedByDefault = $true
  foreach ($resolvedMarketplaceGap in @(
    'repo_marketplace_missing',
    'repo_marketplace_missing_plugins_array',
    'marketplace_json_invalid'
  )) {
    [void]$missingComponents.Remove($resolvedMarketplaceGap)
  }
}

if (-not $runtimeExists) { $safeRepairs.Add('publish_or_restore_bundled_runtime') }
if (-not $marketplaceHasEntry -or -not $installedByDefault) { $safeRepairs.Add('run_bootstrap_harness_install') }
if ($missingComponents -contains 'claude_adapter_not_packaged') { $safeRepairs.Add('define_claude_mcp_registration') }
if ($missingComponents -contains 'cursor_adapter_not_implemented') { $safeRepairs.Add('define_cursor_adapter_strategy') }

$bootstrapState = if ($runtimeExists -and $marketplaceHasEntry -and $installedByDefault) {
  'ready'
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
  'repo_bundle_present_unregistered' { 'register_plugin_in_marketplace' }
  'runtime_only' { 'materialize_repo_plugin_bundle' }
  default { 'restore_runtime_or_complete_bundle' }
}

$result = [ordered]@{
  bootstrap_state = $bootstrapState
  host_context = $HostContext
  update_requested = [bool]$Update
  update_state = $updateState
  update_source_zip_url = $UpdateSourceZipUrl
  update_source_path = $UpdateSourcePath
  update_notes = @($updateNotes | Select-Object -Unique)
  repo_root = $repoRoot
  plugin_root = $pluginRoot
  runtime_present = $runtimeExists
  marketplace_registered = $marketplaceHasEntry
  installed_by_default = $installedByDefault
  actions_taken = @($actionsTaken | Select-Object -Unique)
  missing_components = @($missingComponents | Select-Object -Unique)
  safe_repairs = @($safeRepairs | Select-Object -Unique)
  next_action = $nextAction
}

$result | ConvertTo-Json -Depth 10

if ($bootstrapState -eq 'ready') {
  exit 0
}

exit 1
