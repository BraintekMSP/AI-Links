<#
.SYNOPSIS
Safely inventories, quarantines, or fully removes Anarchy-AI registration and bundle surfaces from repo-local, home-local, and documented device-app cache lanes.
.DESCRIPTION
Provides one bounded companion script for de-registering Anarchy-AI from workspace and user-profile marketplaces,
removing installed plugin bundles, rewriting shared marketplace/config files after backup, retiring optional custom MCP fallback config,
and clearing documented plugin-cache state. The script inventories first, quarantines before delete, preserves repo-authored source truth,
and reports nested paths plus per-target actions so future recovery remains legible.
.PARAMETER Mode
Selects whether the script only assesses state, quarantines active Anarchy-AI surfaces, or quarantines and then permanently removes the quarantined copies.
.PARAMETER RepoRoot
Optional repo root whose repo-local registration and installed bundle surfaces should be inspected.
.PARAMETER UserProfileRoot
Optional user-profile root used to inspect home-local and device-app surfaces; defaults to the current Windows profile.
.PARAMETER Targets
Optional scope list containing repo_local, user_profile, and/or device_app. When omitted, the script inspects every reachable scope.
.PARAMETER QuarantineRoot
Optional absolute quarantine root. Defaults to a temp-directory lane outside synced workspace paths.
.PARAMETER ForceRuntimeLockRelease
Requests the force-release runtime helper instead of the safe-release helper before destructive retirement work.
.PARAMETER AllowMixedMarketplaceRewrite
Retained for backward CLI compatibility. Shared marketplace files are now always backed up and rewritten in place to remove only Anarchy-AI entries, so this switch no longer changes behavior.
.OUTPUTS
JSON describing discovered targets, actions taken, quarantine/removal results, findings, and nested source/destination paths.
.NOTES
Critical dependencies: the generated path canon psd1, the bundled runtime stop helper, current marketplace and config formats,
filesystem write access, and exact path validation before any move or delete.
#>
param(
  [ValidateSet('Assess','Quarantine','Remove')]
  [string]$Mode = 'Assess',
  [string]$RepoRoot = '',
  [string]$UserProfileRoot = '',
  [string[]]$Targets = @(),
  [string]$QuarantineRoot = '',
  [switch]$ForceRuntimeLockRelease,
  [switch]$AllowMixedMarketplaceRewrite
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:pluginRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir '..'))
$script:expectedWorkingLocation = [System.IO.Path]::GetFullPath((Get-Location).Path)
$pathCanonPath = Join-Path $pluginRoot 'pathing\anarchy-path-canon.generated.psd1'
if (-not (Test-Path $pathCanonPath)) {
  throw "Path canon artifact not found: $pathCanonPath"
}

$pathCanon = Import-PowerShellDataFile -Path $pathCanonPath
$script:actionsTaken = New-Object System.Collections.Generic.List[string]
$script:findings = New-Object System.Collections.Generic.List[string]
$script:warnings = New-Object System.Collections.Generic.List[string]
$script:inventory = New-Object System.Collections.Generic.List[object]
$script:quarantinedItems = New-Object System.Collections.Generic.List[object]
$script:removedItems = New-Object System.Collections.Generic.List[object]
$script:skippedItems = New-Object System.Collections.Generic.List[object]
$script:preservedItems = New-Object System.Collections.Generic.List[object]
$script:scheduledDeferredItems = New-Object System.Collections.Generic.List[object]
$script:runtimeLockReports = New-Object System.Collections.Generic.List[object]

<#
.SYNOPSIS
Resolves a canon-relative path against a supplied root.
.DESCRIPTION
Normalizes slash direction and returns an absolute path for repo, home, or bundle surfaces.
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
Checks whether a value matches one of the canon-owned exact names or prefixes.
.DESCRIPTION
Provides one shared ownership check so all removal classifications use the same repo-authored identity lists.
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
Uses the generated canon arrays so removal only touches plugin entries and directories clearly owned by this repo.
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

<#
.SYNOPSIS
Checks whether a marketplace name belongs to the current or legacy owned Anarchy-AI identities.
.DESCRIPTION
Uses the generated canon arrays so device-app cache retirement only targets documented Anarchy-AI marketplace roots.
.PARAMETER MarketplaceName
Marketplace name to classify.
.OUTPUTS
System.Boolean. True when the marketplace name is owned by this repo.
.NOTES
Critical dependencies: path canon owned marketplace-name arrays and Test-MatchesCanonIdentity.
#>
function Test-IsOwnedMarketplaceName {
  param(
    [string]$MarketplaceName
  )

  return Test-MatchesCanonIdentity `
    -Value $MarketplaceName `
    -Exact @($pathCanon.arrays.owned_marketplace_name_exact) `
    -Prefixes @($pathCanon.arrays.owned_marketplace_name_prefixes)
}

<#
.SYNOPSIS
Checks whether an MCP server name belongs to the current or legacy owned Anarchy-AI identities.
.DESCRIPTION
Uses the generated canon arrays so optional custom-MCP fallback cleanup recognizes both current and older server keys.
.PARAMETER ServerName
MCP server name to classify.
.OUTPUTS
System.Boolean. True when the server name is owned by this repo.
.NOTES
Critical dependencies: path canon owned MCP server-name arrays and exact case-insensitive comparison.
#>
function Test-IsOwnedMcpServerName {
  param(
    [string]$ServerName
  )

  if ([string]::IsNullOrWhiteSpace($ServerName)) {
    return $false
  }

  foreach ($candidate in @($pathCanon.arrays.owned_mcp_server_names | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
    if ([string]::Equals($ServerName, [string]$candidate, [System.StringComparison]::OrdinalIgnoreCase)) {
      return $true
    }
  }

  return $false
}

<#
.SYNOPSIS
Builds the regex used to find owned custom-MCP blocks in Codex config.
.DESCRIPTION
Escapes each owned server name from the generated path canon and matches any one complete TOML block.
.OUTPUTS
System.String. Regex pattern that matches owned custom-MCP blocks.
.NOTES
Critical dependencies: path canon owned MCP server-name arrays and regex escaping.
#>
function Get-OwnedCodexCustomMcpServerBlockPattern {
  $escapedNames = @($pathCanon.arrays.owned_mcp_server_names | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { [regex]::Escape([string]$_) })
  $nameAlternation = [string]::Join('|', $escapedNames)
  return "(?ms)^\[mcp_servers\.(?:$nameAlternation)\]\r?\n(?:.*?\r?\n)*(?=^\[|\z)"
}

$script:codexCustomMcpServerBlockPattern = Get-OwnedCodexCustomMcpServerBlockPattern

<#
.SYNOPSIS
Returns the current working location in normalized absolute form.
.DESCRIPTION
Used by destructive-operation guards so mutation paths cannot proceed after unexpected location drift.
.OUTPUTS
System.String. Normalized current working directory path.
.NOTES
Critical dependencies: Get-Location and full-path normalization.
#>
function Get-NormalizedCurrentLocationPath {
  return [System.IO.Path]::GetFullPath((Get-Location).Path)
}

<#
.SYNOPSIS
Stops a mutating operation when the current working location no longer matches the invocation location.
.DESCRIPTION
Protects against accidental continuation after unexpected location changes or failed working-directory transitions.
.PARAMETER OperationName
Human-readable operation label included in any thrown error.
.OUTPUTS
No direct return value. Throws when the location guard fails.
.NOTES
Critical dependencies: Get-NormalizedCurrentLocationPath and the captured invocation location.
#>
function Assert-GuardedCurrentLocation {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OperationName
  )

  $currentLocation = Get-NormalizedCurrentLocationPath
  if (-not [string]::Equals($currentLocation, $script:expectedWorkingLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "$OperationName refused to continue because the current working location changed from '$($script:expectedWorkingLocation)' to '$currentLocation'."
  }
}

<#
.SYNOPSIS
Validates a mutation target immediately before backup, move, rewrite, or delete work.
.DESCRIPTION
Checks the working location guard, optional existence, and optional root boundary in one place so destructive operations cannot proceed on stale or mis-scoped paths.
.PARAMETER OperationName
Human-readable operation label included in thrown errors.
.PARAMETER TargetPath
Target file or directory path being validated.
.PARAMETER ExpectedRoot
Optional root boundary that the target must remain beneath.
.PARAMETER RequireExisting
When set, the target must exist at validation time.
.OUTPUTS
System.String. Normalized absolute path for the validated target.
.NOTES
Critical dependencies: Assert-GuardedCurrentLocation, Test-Path, and Test-IsPathInsideRoot.
#>
function Assert-GuardedPath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OperationName,
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,
    [string]$ExpectedRoot = '',
    [switch]$RequireExisting
  )

  Assert-GuardedCurrentLocation -OperationName $OperationName

  if ([string]::IsNullOrWhiteSpace($TargetPath)) {
    throw "$OperationName refused to continue because the target path was blank."
  }

  $resolvedTargetPath = [System.IO.Path]::GetFullPath($TargetPath)
  if ($RequireExisting -and -not (Test-Path $resolvedTargetPath)) {
    throw "$OperationName refused to continue because the target path was missing: $resolvedTargetPath"
  }

  if (-not [string]::IsNullOrWhiteSpace($ExpectedRoot) -and -not (Test-IsPathInsideRoot -CandidatePath $resolvedTargetPath -ExpectedRoot $ExpectedRoot)) {
    throw "$OperationName refused to continue because the target path fell outside the expected root '$ExpectedRoot': $resolvedTargetPath"
  }

  return $resolvedTargetPath
}

<#
.SYNOPSIS
Infers the root that owns a marketplace file path.
.DESCRIPTION
Walks from `<root>\.agents\plugins\marketplace.json` back to the containing repo or user-profile root for guarded rewrites.
.PARAMETER MarketplacePath
Absolute marketplace file path.
.OUTPUTS
System.String. Marketplace owner root.
.NOTES
Critical dependencies: the current destination-relative marketplace layout.
#>
function Get-MarketplaceOwnerRoot {
  param(
    [Parameter(Mandatory = $true)]
    [string]$MarketplacePath
  )

  return Split-Path (Split-Path (Split-Path $MarketplacePath -Parent) -Parent) -Parent
}

<#
.SYNOPSIS
Builds a hashtable that omits null and blank values.
.DESCRIPTION
Keeps nested path and result output compact instead of emitting placeholder members.
.PARAMETER Values
Hashtable of candidate values.
.OUTPUTS
Ordered hashtable or null when every value is empty.
.NOTES
Critical dependencies: the nested output contract used across Anarchy-AI maintenance scripts.
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
Builds one nested path-role report for source or destination output.
.DESCRIPTION
Adds root, directories, files, and relative sections only when meaningful values are available.
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
Critical dependencies: New-OptionalMap and the shared nested path-report contract.
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

<#
.SYNOPSIS
Normalizes a repo or file-system label into a marketplace-safe slug.
.DESCRIPTION
Lowercases the value, collapses non-alphanumeric characters to dashes, and falls back to repo when empty.
.PARAMETER Value
Repo name or other label to normalize.
.OUTPUTS
System.String. Marketplace-safe slug.
.NOTES
Critical dependencies: repo-local marketplace naming rules and orphaned-cache detection.
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
Attempts to infer a repo root from the current plugin-root location.
.DESCRIPTION
Returns the surrounding repo root only when the current plugin root sits beneath the canon repo-local plugin parent path.
.OUTPUTS
System.String. Absolute repo root when detectable; otherwise an empty string.
.NOTES
Critical dependencies: the generated repo-local parent path and current plugin-root location.
#>
function Get-AutoRepoRoot {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedUserProfileRoot
  )

  $currentUserProfilePluginRoot = Resolve-CanonRelativePath `
    -RootPath $ResolvedUserProfileRoot `
    -RelativePath (Join-Path $pathCanon.relative_paths.user_profile_plugin_parent_directory_relative_path $pathCanon.names.default_plugin_name).Replace('\','/')
  if ([string]::Equals($script:pluginRoot, $currentUserProfilePluginRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    return ''
  }

  $candidate = [System.IO.Path]::GetFullPath((Join-Path $script:pluginRoot '..\..'))
  if (-not (Test-Path $candidate)) {
    return ''
  }

  $expectedSourcePluginRoot = Resolve-CanonRelativePath -RootPath $candidate -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path
  if ([string]::Equals($expectedSourcePluginRoot, $script:pluginRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $candidate
  }

  $repoLocalPluginParent = Resolve-CanonRelativePath -RootPath $candidate -RelativePath $pathCanon.relative_paths.repo_local_plugin_parent_directory_relative_path
  if ($script:pluginRoot.StartsWith($repoLocalPluginParent + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $candidate
  }

  return ''
}

<#
.SYNOPSIS
Normalizes the requested scope list into the reachable targets for this invocation.
.DESCRIPTION
Uses explicit target arguments when supplied and otherwise enables every reachable scope based on the supplied repo and user-profile roots.
.PARAMETER RequestedTargets
Optional explicit scope list from the caller.
.PARAMETER ResolvedRepoRoot
Normalized repo root candidate.
.PARAMETER ResolvedUserProfileRoot
Normalized user-profile root candidate.
.OUTPUTS
System.String[]. Reachable scope values.
.NOTES
Critical dependencies: repo-root auto detection and the current three-scope maintenance model.
#>
function Resolve-TargetScopes {
  param(
    [string[]]$RequestedTargets,
    [string]$ResolvedRepoRoot,
    [Parameter(Mandatory = $true)]
    [string]$ResolvedUserProfileRoot
  )

  if ($RequestedTargets.Count -gt 0) {
    $normalizedTargets = New-Object System.Collections.Generic.List[string]
    foreach ($requestedTarget in $RequestedTargets) {
      foreach ($candidateTarget in @([regex]::Split([string]$requestedTarget, '[,\s]+') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        switch ($candidateTarget) {
          'repo_local' { $normalizedTargets.Add('repo_local') }
          'user_profile' { $normalizedTargets.Add('user_profile') }
          'device_app' { $normalizedTargets.Add('device_app') }
          default { throw "Unsupported target scope: $candidateTarget" }
        }
      }
    }

    return @($normalizedTargets | Select-Object -Unique)
  }

  $resolvedTargets = New-Object System.Collections.Generic.List[string]
  if (-not [string]::IsNullOrWhiteSpace($ResolvedRepoRoot)) {
    $resolvedTargets.Add('repo_local')
  }

  if (-not [string]::IsNullOrWhiteSpace($ResolvedUserProfileRoot)) {
    $resolvedTargets.Add('user_profile')
    $resolvedTargets.Add('device_app')
  }

  return @($resolvedTargets | Select-Object -Unique)
}

<#
.SYNOPSIS
Checks whether one path remains beneath an expected root.
.DESCRIPTION
Normalizes both paths before performing an ordinal-insensitive prefix test.
.PARAMETER CandidatePath
Path to validate.
.PARAMETER ExpectedRoot
Root path that the candidate must remain beneath.
.OUTPUTS
System.Boolean. True when the candidate is inside the expected root.
.NOTES
Critical dependencies: exact full-path normalization before destructive operations.
#>
function Test-IsPathInsideRoot {
  param(
    [Parameter(Mandatory = $true)]
    [string]$CandidatePath,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRoot
  )

  $resolvedCandidatePath = [System.IO.Path]::GetFullPath($CandidatePath)
  $resolvedExpectedRoot = [System.IO.Path]::GetFullPath($ExpectedRoot)
  if ([string]::Equals($resolvedCandidatePath, $resolvedExpectedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $true
  }

  return $resolvedCandidatePath.StartsWith($resolvedExpectedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

<#
.SYNOPSIS
Normalizes file and directory attributes so quarantine or delete work does not fail on readonly surfaces.
.DESCRIPTION
Recursively clears child attributes for directories and normalizes single files in place.
.PARAMETER TargetPath
Existing file or directory path whose attributes should be normalized.
.OUTPUTS
No direct return value. Mutates attributes when possible.
.NOTES
Critical dependencies: Get-Item, Get-ChildItem, and Windows filesystem attribute support.
#>
function Set-NormalAttributes {
  param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath
  )

  if (-not (Test-Path $TargetPath)) {
    return
  }

  $item = Get-Item $TargetPath -Force
  if ($item.PSIsContainer) {
    Get-ChildItem $TargetPath -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
      try {
        $_.Attributes = 'Normal'
      }
      catch {
      }
    }

    try {
      $item.Attributes = 'Directory'
    }
    catch {
    }
  }
  else {
    try {
      $item.Attributes = 'Normal'
    }
    catch {
    }
  }
}

<#
.SYNOPSIS
Determines whether a marketplace entry represents Anarchy-AI.
.DESCRIPTION
Matches current and legacy Anarchy-AI plugin names and the supported marketplace source-path prefixes.
.PARAMETER Plugin
Marketplace plugin entry object to inspect.
.OUTPUTS
System.Boolean. True when the entry represents Anarchy-AI.
.NOTES
Critical dependencies: current naming contracts and the generated marketplace source-path prefixes.
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
Reads a marketplace file and summarizes whether it is Anarchy-only, mixed, or invalid.
.DESCRIPTION
Returns both the parsed plugin entries and any resolved plugin-root targets implied by Anarchy-AI marketplace entries.
.PARAMETER MarketplacePath
Absolute marketplace file path to inspect.
.PARAMETER MarketplaceSourceRoot
Absolute root that marketplace source.path values should resolve beneath.
.PARAMETER PreservedPluginRoot
Optional plugin root that must never be treated as disposable source truth.
.OUTPUTS
PSCustomObject carrying parse status, Anarchy entry counts, mixed-state flags, and resolved plugin-root paths.
.NOTES
Critical dependencies: ConvertFrom-Json, Test-IsAnarchyPluginEntry, and destination-relative marketplace source paths.
#>
function Get-MarketplaceInventory {
  param(
    [Parameter(Mandatory = $true)]
    [string]$MarketplacePath,
    [Parameter(Mandatory = $true)]
    [string]$MarketplaceSourceRoot,
    [string]$PreservedPluginRoot = ''
  )

  $result = [ordered]@{
    file_exists = Test-Path $MarketplacePath
    parse_status = 'missing'
    anarchy_entry_count = 0
    non_anarchy_entry_count = 0
    is_anarchy_only = $false
    is_mixed = $false
    plugin_root_paths = @()
    marketplace_object = $null
  }

  if (-not $result.file_exists) {
    return [pscustomobject]$result
  }

  try {
    $marketplaceObject = Get-Content $MarketplacePath -Raw | ConvertFrom-Json
    $plugins = @($marketplaceObject.plugins)
    if ($null -eq $marketplaceObject.plugins) {
      $result.parse_status = 'missing_plugins_array'
      $result.marketplace_object = $marketplaceObject
      return [pscustomobject]$result
    }

    $pluginRootPaths = New-Object System.Collections.Generic.List[string]
    foreach ($plugin in $plugins) {
      if (Test-IsAnarchyPluginEntry -Plugin $plugin) {
        $result.anarchy_entry_count++
        if ($null -ne $plugin.source -and $null -ne $plugin.source.path) {
          $pluginPath = Resolve-CanonRelativePath -RootPath $MarketplaceSourceRoot -RelativePath ([string]$plugin.source.path)
          if (-not [string]::IsNullOrWhiteSpace($PreservedPluginRoot) -and [string]::Equals($pluginPath, $PreservedPluginRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            $script:preservedItems.Add([pscustomobject]@{
              scope = 'repo_local'
              surface_kind = 'repo_authored_plugin_source'
              path = $pluginPath
              reason = 'repo_authored_source_truth_preserved'
            })
          }
          else {
            $pluginRootPaths.Add($pluginPath)
          }
        }
      }
      else {
        $result.non_anarchy_entry_count++
      }
    }

    $result.plugin_root_paths = @($pluginRootPaths | Select-Object -Unique)
    $result.is_anarchy_only = $result.anarchy_entry_count -gt 0 -and $result.non_anarchy_entry_count -eq 0
    $result.is_mixed = $result.anarchy_entry_count -gt 0 -and $result.non_anarchy_entry_count -gt 0
    $result.parse_status = 'valid'
    $result.marketplace_object = $marketplaceObject
  }
  catch {
    $result.parse_status = 'invalid'
  }

  return [pscustomobject]$result
}

<#
.SYNOPSIS
Finds orphaned repo-local plugin roots that are not presently referenced by the repo marketplace.
.DESCRIPTION
Scans the repo-local plugin parent directory for Anarchy-AI-shaped directories and excludes the canonical source plugin root and any paths already referenced.
.PARAMETER ResolvedRepoRoot
Absolute repo root whose plugin parent should be inspected.
.PARAMETER PreservedPluginRoot
Repo-authored source plugin root that must be preserved.
.PARAMETER ReferencedPluginRoots
Plugin roots already discovered from marketplace registration.
.OUTPUTS
System.String[]. Additional repo-local plugin root paths.
.NOTES
Critical dependencies: repo-local plugin parent path from the canon and current Anarchy-AI naming conventions.
#>
function Find-OrphanedRepoLocalPluginRoots {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedRepoRoot,
    [Parameter(Mandatory = $true)]
    [string]$PreservedPluginRoot,
    [string[]]$ReferencedPluginRoots
  )

  $pluginParent = Resolve-CanonRelativePath -RootPath $ResolvedRepoRoot -RelativePath $pathCanon.relative_paths.repo_local_plugin_parent_directory_relative_path
  if (-not (Test-Path $pluginParent)) {
    return @()
  }

  $results = New-Object System.Collections.Generic.List[string]
  foreach ($directory in Get-ChildItem $pluginParent -Directory -ErrorAction SilentlyContinue) {
    if (-not (Test-IsOwnedPluginName -PluginName $directory.Name)) {
      continue
    }

    if ([string]::Equals($directory.FullName, $PreservedPluginRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
      continue
    }

    if (@($ReferencedPluginRoots | Where-Object { [string]::Equals($_, $directory.FullName, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
      continue
    }

    $manifestPath = Join-Path $directory.FullName $pathCanon.relative_paths.bundle_plugin_manifest_file_relative_path.Replace('/', '\')
    if (-not (Test-Path $manifestPath)) {
      continue
    }

    $results.Add($directory.FullName)
  }

  return @($results | Select-Object -Unique)
}

<#
.SYNOPSIS
Builds one inventory target object for later actioning.
.DESCRIPTION
Records the scope, surface kind, path, and preferred operation for one discovered Anarchy-AI surface.
.PARAMETER Scope
Logical scope such as repo_local, user_profile, or device_app.
.PARAMETER SurfaceKind
Surface classification such as plugin_root_directory or marketplace_file.
.PARAMETER Path
Absolute path for the surface when applicable.
.PARAMETER PlannedAction
Preferred operation such as quarantine_directory, quarantine_file, or rewrite_file_after_backup.
.PARAMETER Details
Optional human-readable note for findings and later reporting.
.OUTPUTS
PSCustomObject describing the inventory target.
.NOTES
Critical dependencies: the current removal report contract and caller-supplied validated paths.
#>
function New-InventoryTarget {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind,
    [string]$Path,
    [Parameter(Mandatory = $true)]
    [string]$PlannedAction,
    [string]$Details = ''
  )

  return [pscustomobject]@{
    scope = $Scope
    surface_kind = $SurfaceKind
    path = $Path
    planned_action = $PlannedAction
    details = $Details
  }
}

<#
.SYNOPSIS
Collects repo-local marketplace and installed-bundle targets for retirement.
.DESCRIPTION
Discovers the repo-local marketplace file, preserves repo-authored source truth, and adds installed or orphaned repo-local plugin bundles as removable targets.
.PARAMETER ResolvedRepoRoot
Absolute repo root to inspect.
.OUTPUTS
No direct return value. Populates the shared inventory and findings collections.
.NOTES
Critical dependencies: repo-local marketplace shape, plugin-parent path from the canon, and preservation of repo-authored source surfaces.
#>
function Add-RepoLocalTargets {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedRepoRoot
  )

  $repoMarketplacePath = Resolve-CanonRelativePath -RootPath $ResolvedRepoRoot -RelativePath $pathCanon.relative_paths.repo_local_marketplace_file_relative_path
  $preservedPluginRoot = Resolve-CanonRelativePath -RootPath $ResolvedRepoRoot -RelativePath $pathCanon.relative_paths.repo_source_plugin_directory_relative_path
  $marketplaceInventory = Get-MarketplaceInventory -MarketplacePath $repoMarketplacePath -MarketplaceSourceRoot $ResolvedRepoRoot -PreservedPluginRoot $preservedPluginRoot

  if ($marketplaceInventory.parse_status -eq 'invalid') {
    $script:findings.Add('repo_marketplace_json_invalid')
    $script:inventory.Add((New-InventoryTarget -Scope 'repo_local' -SurfaceKind 'marketplace_file' -Path $repoMarketplacePath -PlannedAction 'manual_review_required' -Details 'marketplace_json_invalid'))
  }
  elseif ($marketplaceInventory.parse_status -eq 'missing_plugins_array') {
    $script:findings.Add('repo_marketplace_missing_plugins_array')
    $script:inventory.Add((New-InventoryTarget -Scope 'repo_local' -SurfaceKind 'marketplace_file' -Path $repoMarketplacePath -PlannedAction 'manual_review_required' -Details 'marketplace_missing_plugins_array'))
  }
  elseif ($marketplaceInventory.anarchy_entry_count -gt 0) {
    $marketplaceDetails = if ($marketplaceInventory.is_mixed) {
      'repo_marketplace_rewrite_preserve_non_anarchy_entries'
    }
    else {
      'repo_marketplace_rewrite_remove_anarchy_entries_keep_live_registry'
    }

    $script:inventory.Add((New-InventoryTarget -Scope 'repo_local' -SurfaceKind 'marketplace_file' -Path $repoMarketplacePath -PlannedAction 'rewrite_file_after_backup' -Details $marketplaceDetails))
  }

  foreach ($pluginRoot in @($marketplaceInventory.plugin_root_paths)) {
    $script:inventory.Add((New-InventoryTarget -Scope 'repo_local' -SurfaceKind 'plugin_root_directory' -Path $pluginRoot -PlannedAction 'quarantine_directory' -Details 'repo_local_plugin_root_from_marketplace'))
  }

  foreach ($orphanRoot in @(Find-OrphanedRepoLocalPluginRoots -ResolvedRepoRoot $ResolvedRepoRoot -PreservedPluginRoot $preservedPluginRoot -ReferencedPluginRoots @($marketplaceInventory.plugin_root_paths))) {
    $script:inventory.Add((New-InventoryTarget -Scope 'repo_local' -SurfaceKind 'plugin_root_directory' -Path $orphanRoot -PlannedAction 'quarantine_directory' -Details 'orphaned_repo_local_plugin_root'))
  }
}

<#
.SYNOPSIS
Collects user-profile plugin, marketplace, legacy-home, and config surfaces for retirement.
.DESCRIPTION
Discovers the current home-local plugin bundle, legacy plugin root, personal marketplace, and optional custom-MCP fallback config.
.PARAMETER ResolvedUserProfileRoot
Absolute user-profile root to inspect.
.OUTPUTS
No direct return value. Populates the shared inventory and findings collections.
.NOTES
Critical dependencies: user-profile path canon, current marketplace shape, and the custom-MCP config block pattern.
#>
function Add-UserProfileTargets {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedUserProfileRoot
  )

  $userProfilePluginRoot = Resolve-CanonRelativePath -RootPath $ResolvedUserProfileRoot -RelativePath (Join-Path $pathCanon.relative_paths.user_profile_plugin_parent_directory_relative_path $pathCanon.names.default_plugin_name).Replace('\','/')
  $legacyPluginRoot = Resolve-CanonRelativePath -RootPath $ResolvedUserProfileRoot -RelativePath (Join-Path $pathCanon.relative_paths.legacy_user_profile_plugin_parent_directory_relative_path $pathCanon.names.default_plugin_name).Replace('\','/')
  $userMarketplacePath = Resolve-CanonRelativePath -RootPath $ResolvedUserProfileRoot -RelativePath $pathCanon.relative_paths.user_profile_marketplace_file_relative_path
  $codexConfigPath = Resolve-CanonRelativePath -RootPath $ResolvedUserProfileRoot -RelativePath $pathCanon.relative_paths.user_profile_codex_config_file_relative_path

  if (Test-Path $userProfilePluginRoot) {
    $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'plugin_root_directory' -Path $userProfilePluginRoot -PlannedAction 'quarantine_directory' -Details 'user_profile_plugin_root'))
  }

  if (Test-Path $legacyPluginRoot) {
    $script:findings.Add('legacy_user_profile_plugin_root_present')
    $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'legacy_plugin_root_directory' -Path $legacyPluginRoot -PlannedAction 'quarantine_directory' -Details 'legacy_user_profile_plugin_root'))
  }

  $marketplaceInventory = Get-MarketplaceInventory -MarketplacePath $userMarketplacePath -MarketplaceSourceRoot $ResolvedUserProfileRoot
  if ($marketplaceInventory.parse_status -eq 'invalid') {
    $script:findings.Add('user_profile_marketplace_json_invalid')
    $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'marketplace_file' -Path $userMarketplacePath -PlannedAction 'manual_review_required' -Details 'marketplace_json_invalid'))
  }
  elseif ($marketplaceInventory.parse_status -eq 'missing_plugins_array') {
    $script:findings.Add('user_profile_marketplace_missing_plugins_array')
    $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'marketplace_file' -Path $userMarketplacePath -PlannedAction 'manual_review_required' -Details 'marketplace_missing_plugins_array'))
  }
  elseif ($marketplaceInventory.anarchy_entry_count -gt 0) {
    $marketplaceDetails = if ($marketplaceInventory.is_mixed) {
      'user_profile_marketplace_rewrite_preserve_non_anarchy_entries'
    }
    else {
      'user_profile_marketplace_rewrite_remove_anarchy_entries_keep_live_registry'
    }

    $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'marketplace_file' -Path $userMarketplacePath -PlannedAction 'rewrite_file_after_backup' -Details $marketplaceDetails))
  }

  if (Test-Path $codexConfigPath) {
    $configContent = Get-Content $codexConfigPath -Raw
    if ($configContent -match $script:codexCustomMcpServerBlockPattern) {
      $script:inventory.Add((New-InventoryTarget -Scope 'user_profile' -SurfaceKind 'codex_config_file' -Path $codexConfigPath -PlannedAction 'rewrite_file_after_backup' -Details 'remove_anarchy_custom_mcp_block'))
    }
  }
}

<#
.SYNOPSIS
Collects documented device-app cache surfaces for retirement.
.DESCRIPTION
Inspects the current plugin-cache parent and adds Anarchy-AI-related marketplace cache directories as removable targets.
.PARAMETER ResolvedUserProfileRoot
Absolute user-profile root to inspect.
.OUTPUTS
No direct return value. Populates the shared inventory and findings collections.
.NOTES
Critical dependencies: the user-profile plugin-cache parent path from the canon and current marketplace naming conventions.
#>
function Add-DeviceAppTargets {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ResolvedUserProfileRoot
  )

  $cacheParent = Resolve-CanonRelativePath -RootPath $ResolvedUserProfileRoot -RelativePath $pathCanon.relative_paths.user_profile_plugin_cache_parent_directory_relative_path
  if (-not (Test-Path $cacheParent)) {
    return
  }

  foreach ($marketplaceDirectory in Get-ChildItem $cacheParent -Directory -ErrorAction SilentlyContinue) {
    if (-not (Test-IsOwnedMarketplaceName -MarketplaceName $marketplaceDirectory.Name)) {
      continue
    }

    $script:inventory.Add((New-InventoryTarget -Scope 'device_app' -SurfaceKind 'plugin_cache_directory' -Path $marketplaceDirectory.FullName -PlannedAction 'quarantine_directory' -Details 'documented_plugin_cache_marketplace_root'))
  }
}

<#
.SYNOPSIS
Builds a readable and collision-safe quarantine path for one discovered surface.
.DESCRIPTION
Preserves drive, scope, and original relative path segments so rollback remains legible.
.PARAMETER OriginalPath
Original path that will be quarantined or backed up.
.PARAMETER Scope
Logical scope used for organization under the quarantine root.
.PARAMETER SurfaceKind
Surface classification used for organization under the quarantine root.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.OUTPUTS
System.String. Absolute destination path beneath the quarantine root.
.NOTES
Critical dependencies: exact original path capture and non-synced quarantine storage.
#>
function Get-QuarantinePath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OriginalPath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath
  )

  $resolvedOriginalPath = [System.IO.Path]::GetFullPath($OriginalPath)
  $pathRoot = [System.IO.Path]::GetPathRoot($resolvedOriginalPath)
  $driveLabel = $pathRoot.TrimEnd('\').Replace(':', '')
  if ([string]::IsNullOrWhiteSpace($driveLabel)) {
    $driveLabel = 'root'
  }

  $relativePath = $resolvedOriginalPath.Substring($pathRoot.Length).TrimStart('\')
  $quarantineBase = Join-Path $QuarantineRootPath (Join-Path $Scope (Join-Path $SurfaceKind $driveLabel))
  if ([string]::IsNullOrWhiteSpace($relativePath)) {
    return $quarantineBase
  }

  return Join-Path $quarantineBase $relativePath
}

<#
.SYNOPSIS
Copies a file into quarantine before the live copy is rewritten.
.DESCRIPTION
Creates the destination directory as needed and preserves one recoverable backup for file-edit operations.
.PARAMETER OriginalPath
Live file that will be edited after backup.
.PARAMETER Scope
Logical scope used to organize the quarantine backup.
.PARAMETER SurfaceKind
Surface classification used to organize the quarantine backup.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.OUTPUTS
System.String. Quarantine backup path.
.NOTES
Critical dependencies: Get-QuarantinePath, filesystem write access, and same-run backup before edit.
#>
function Backup-FileToQuarantine {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OriginalPath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath
  )

  $validatedOriginalPath = Assert-GuardedPath -OperationName 'Backup-FileToQuarantine' -TargetPath $OriginalPath -RequireExisting
  $quarantinePath = Get-QuarantinePath -OriginalPath $validatedOriginalPath -Scope $Scope -SurfaceKind $SurfaceKind -QuarantineRootPath $QuarantineRootPath
  $validatedQuarantinePath = Assert-GuardedPath -OperationName 'Backup-FileToQuarantine' -TargetPath $quarantinePath -ExpectedRoot $QuarantineRootPath
  $quarantineDirectory = Split-Path $validatedQuarantinePath -Parent

  try {
    Assert-GuardedPath -OperationName 'Backup-FileToQuarantine' -TargetPath $quarantineDirectory -ExpectedRoot $QuarantineRootPath | Out-Null
    if (-not (Test-Path $quarantineDirectory)) {
      New-Item -ItemType Directory -Path $quarantineDirectory -Force | Out-Null
    }

    Assert-GuardedPath -OperationName 'Backup-FileToQuarantine' -TargetPath $validatedOriginalPath -RequireExisting | Out-Null
    Copy-Item $validatedOriginalPath $validatedQuarantinePath -Force
  }
  catch {
    throw "Backup-FileToQuarantine failed for '$validatedOriginalPath': $($_.Exception.Message)"
  }

  $script:quarantinedItems.Add([pscustomobject]@{
    scope = $Scope
    surface_kind = $SurfaceKind
    original_path = $validatedOriginalPath
    quarantine_path = $validatedQuarantinePath
    operation = 'backup_before_edit'
  })
  return $validatedQuarantinePath
}

<#
.SYNOPSIS
Moves a file or directory into quarantine after validating the exact root boundary.
.DESCRIPTION
Normalizes attributes, creates destination parents, and preserves one recoverable copy outside the live lane.
.PARAMETER OriginalPath
Live file or directory path to retire.
.PARAMETER Scope
Logical scope used to organize the quarantine target.
.PARAMETER SurfaceKind
Surface classification used to organize the quarantine target.
.PARAMETER ExpectedRoot
Absolute root that the original path must remain beneath.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.OUTPUTS
PSCustomObject describing the quarantine result.
.NOTES
Critical dependencies: Test-IsPathInsideRoot, Set-NormalAttributes, Move-Item, and exact root validation immediately before mutation.
#>
function Move-PathToQuarantine {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OriginalPath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRoot,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath
  )

  if (-not (Test-Path $OriginalPath)) {
    return [pscustomobject]@{
      status = 'missing'
      quarantine_path = $null
    }
  }

  $validatedOriginalPath = Assert-GuardedPath -OperationName 'Move-PathToQuarantine' -TargetPath $OriginalPath -ExpectedRoot $ExpectedRoot -RequireExisting
  $quarantinePath = Get-QuarantinePath -OriginalPath $validatedOriginalPath -Scope $Scope -SurfaceKind $SurfaceKind -QuarantineRootPath $QuarantineRootPath
  $validatedQuarantinePath = Assert-GuardedPath -OperationName 'Move-PathToQuarantine' -TargetPath $quarantinePath -ExpectedRoot $QuarantineRootPath
  $quarantineDirectory = Split-Path $validatedQuarantinePath -Parent

  try {
    Assert-GuardedPath -OperationName 'Move-PathToQuarantine' -TargetPath $quarantineDirectory -ExpectedRoot $QuarantineRootPath | Out-Null
    if (-not (Test-Path $quarantineDirectory)) {
      New-Item -ItemType Directory -Path $quarantineDirectory -Force | Out-Null
    }

    Assert-GuardedPath -OperationName 'Move-PathToQuarantine' -TargetPath $validatedOriginalPath -ExpectedRoot $ExpectedRoot -RequireExisting | Out-Null
    Set-NormalAttributes -TargetPath $validatedOriginalPath
    Move-Item -LiteralPath $validatedOriginalPath -Destination $validatedQuarantinePath -Force
  }
  catch {
    throw "Move-PathToQuarantine failed for '$validatedOriginalPath': $($_.Exception.Message)"
  }

  $result = [pscustomobject]@{
    status = 'quarantined'
    quarantine_path = $validatedQuarantinePath
  }

  $script:quarantinedItems.Add([pscustomobject]@{
    scope = $Scope
    surface_kind = $SurfaceKind
    original_path = $validatedOriginalPath
    quarantine_path = $validatedQuarantinePath
    operation = 'move_to_quarantine'
  })
  return $result
}

<#
.SYNOPSIS
Schedules deferred quarantine work for the plugin root that is currently running this script.
.DESCRIPTION
Launches a short-lived helper in temp storage so the current bundle can retire itself after the active script file is released.
.PARAMETER OriginalPath
Live directory path that contains the currently running script.
.PARAMETER Scope
Logical scope used to organize the quarantine target.
.PARAMETER SurfaceKind
Surface classification used to organize the quarantine target.
.PARAMETER ExpectedRoot
Absolute root that the original path must remain beneath.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.PARAMETER PurgeAfterQuarantine
Requests delete-after-quarantine behavior when Mode is Remove.
.OUTPUTS
PSCustomObject describing the scheduled deferred action.
.NOTES
Critical dependencies: a writable temp directory, secondary PowerShell process launch, and same-run exact path validation.
#>
function Start-DeferredSelfRetirement {
  param(
    [Parameter(Mandatory = $true)]
    [string]$OriginalPath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedRoot,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath,
    [Parameter(Mandatory = $true)]
    [bool]$PurgeAfterQuarantine
  )

  Assert-GuardedCurrentLocation -OperationName 'Start-DeferredSelfRetirement'
  $validatedOriginalPath = Assert-GuardedPath -OperationName 'Start-DeferredSelfRetirement' -TargetPath $OriginalPath -ExpectedRoot $ExpectedRoot -RequireExisting
  $quarantinePath = Get-QuarantinePath -OriginalPath $validatedOriginalPath -Scope $Scope -SurfaceKind $SurfaceKind -QuarantineRootPath $QuarantineRootPath
  $validatedQuarantinePath = Assert-GuardedPath -OperationName 'Start-DeferredSelfRetirement' -TargetPath $quarantinePath -ExpectedRoot $QuarantineRootPath
  $workerScriptPath = Join-Path ([System.IO.Path]::GetTempPath()) ("anarchy-ai-retire-" + [guid]::NewGuid().ToString('N') + '.ps1')
$workerScript = @"
param(
  [string]`$TargetPath,
  [string]`$DestinationPath,
  [string]`$ExpectedRoot,
  [string]`$ExpectedDestinationRoot,
  [string]`$ExpectedWorkingLocation,
  [string]`$PurgeAfterMove
)

`$purgeAfterMoveFlag = [System.StringComparer]::OrdinalIgnoreCase.Equals([string]`$PurgeAfterMove, 'true')

function Get-NormalizedCurrentLocationPath {
  return [System.IO.Path]::GetFullPath((Get-Location).Path)
}

function Assert-GuardedCurrentLocation {
  param([string]`$OperationName)
  `$currentLocation = Get-NormalizedCurrentLocationPath
  if (-not [string]::Equals(`$currentLocation, `$ExpectedWorkingLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "`$OperationName refused to continue because the current working location changed from '`$ExpectedWorkingLocation' to '`$currentLocation'."
  }
}

function Test-IsPathInsideRoot {
  param(
    [string]`$CandidatePath,
    [string]`$ExpectedRootPath
  )

  `$resolvedCandidatePath = [System.IO.Path]::GetFullPath(`$CandidatePath)
  `$resolvedExpectedRoot = [System.IO.Path]::GetFullPath(`$ExpectedRootPath)
  if ([string]::Equals(`$resolvedCandidatePath, `$resolvedExpectedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    return `$true
  }

  return `$resolvedCandidatePath.StartsWith(`$resolvedExpectedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-GuardedPath {
  param(
    [string]`$OperationName,
    [string]`$PathToValidate,
    [string]`$ExpectedRootPath,
    [bool]`$RequireExisting
  )

  Assert-GuardedCurrentLocation -OperationName `$OperationName
  if ([string]::IsNullOrWhiteSpace(`$PathToValidate)) {
    throw "`$OperationName refused to continue because the target path was blank."
  }

  `$resolvedPath = [System.IO.Path]::GetFullPath(`$PathToValidate)
  if (`$RequireExisting -and -not (Test-Path `$resolvedPath)) {
    throw "`$OperationName refused to continue because the target path was missing: `$resolvedPath"
  }

  if (-not [string]::IsNullOrWhiteSpace(`$ExpectedRootPath) -and -not (Test-IsPathInsideRoot -CandidatePath `$resolvedPath -ExpectedRootPath `$ExpectedRootPath)) {
    throw "`$OperationName refused to continue because the target path fell outside the expected root '`$ExpectedRootPath': `$resolvedPath"
  }

  return `$resolvedPath
}

function Set-NormalAttributes {
  param([string]`$PathToNormalize)
  if (-not (Test-Path `$PathToNormalize)) { return }
  `$item = Get-Item `$PathToNormalize -Force
  if (`$item.PSIsContainer) {
    Get-ChildItem `$PathToNormalize -Force -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
      try { `$_.Attributes = 'Normal' } catch {}
    }
    try { `$item.Attributes = 'Directory' } catch {}
  }
  else {
    try { `$item.Attributes = 'Normal' } catch {}
  }
}

for (`$attempt = 0; `$attempt -lt 20; `$attempt++) {
  Start-Sleep -Milliseconds 500
  if (-not (Test-Path `$TargetPath)) { break }
  try {
    `$resolvedTarget = Assert-GuardedPath -OperationName 'DeferredSelfRetirement' -PathToValidate `$TargetPath -ExpectedRootPath `$ExpectedRoot -RequireExisting `$true
    `$resolvedDestination = Assert-GuardedPath -OperationName 'DeferredSelfRetirement' -PathToValidate `$DestinationPath -ExpectedRootPath `$ExpectedDestinationRoot -RequireExisting `$false
    `$destinationDirectory = Assert-GuardedPath -OperationName 'DeferredSelfRetirement' -PathToValidate (Split-Path `$resolvedDestination -Parent) -ExpectedRootPath `$ExpectedDestinationRoot -RequireExisting `$false
    if (-not (Test-Path `$destinationDirectory)) {
      New-Item -ItemType Directory -Path `$destinationDirectory -Force | Out-Null
    }

    Set-NormalAttributes -PathToNormalize `$resolvedTarget
    Move-Item -LiteralPath `$resolvedTarget -Destination `$resolvedDestination -Force
    if (`$purgeAfterMoveFlag -and (Test-Path `$resolvedDestination)) {
      Assert-GuardedPath -OperationName 'DeferredSelfRetirementRemove' -PathToValidate `$resolvedDestination -ExpectedRootPath `$ExpectedDestinationRoot -RequireExisting `$true | Out-Null
      Set-NormalAttributes -PathToNormalize `$resolvedDestination
      Remove-Item -LiteralPath `$resolvedDestination -Recurse -Force
    }
    break
  }
  catch {
    if (`$attempt -ge 19) { throw }
  }
}

try { Remove-Item -LiteralPath `$PSCommandPath -Force } catch {}
"@

  try {
    Set-Content -Path $workerScriptPath -Value $workerScript -Encoding UTF8
  }
  catch {
    throw "Start-DeferredSelfRetirement failed while preparing the worker script '$workerScriptPath': $($_.Exception.Message)"
  }

  $powershellExe = Join-Path $PSHOME 'powershell.exe'
  try {
    Start-Process -FilePath $powershellExe -ArgumentList @(
      '-ExecutionPolicy', 'Bypass',
      '-File', $workerScriptPath,
      '-TargetPath', $validatedOriginalPath,
      '-DestinationPath', $validatedQuarantinePath,
      '-ExpectedRoot', $ExpectedRoot,
      '-ExpectedDestinationRoot', $QuarantineRootPath,
      '-ExpectedWorkingLocation', $script:expectedWorkingLocation,
      '-PurgeAfterMove', $PurgeAfterQuarantine.ToString().ToLowerInvariant()
    ) -WorkingDirectory $script:expectedWorkingLocation | Out-Null
  }
  catch {
    throw "Start-DeferredSelfRetirement failed while launching the worker for '$validatedOriginalPath': $($_.Exception.Message)"
  }

  $scheduled = [pscustomobject]@{
    scope = $Scope
    surface_kind = $SurfaceKind
    original_path = $validatedOriginalPath
    quarantine_path = $validatedQuarantinePath
    purge_after_quarantine = $PurgeAfterQuarantine
  }
  $script:scheduledDeferredItems.Add($scheduled)
  return $scheduled
}

<#
.SYNOPSIS
Deletes a quarantined file or directory after safe retirement has already occurred.
.DESCRIPTION
Normalizes attributes and performs a bounded recursive delete within the quarantine root only.
.PARAMETER QuarantinePath
Quarantined file or directory path to delete.
.PARAMETER QuarantineRootPath
Absolute quarantine root used for exact boundary validation.
.PARAMETER Scope
Logical scope used for reporting.
.PARAMETER SurfaceKind
Surface classification used for reporting.
.OUTPUTS
No direct return value. Removes the quarantined target and records the action.
.NOTES
Critical dependencies: Test-IsPathInsideRoot, Set-NormalAttributes, and Remove-Item.
#>
function Remove-QuarantinedPath {
  param(
    [Parameter(Mandatory = $true)]
    [string]$QuarantinePath,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind
  )

  if (-not (Test-Path $QuarantinePath)) {
    return
  }

  $validatedQuarantinePath = Assert-GuardedPath -OperationName 'Remove-QuarantinedPath' -TargetPath $QuarantinePath -ExpectedRoot $QuarantineRootPath -RequireExisting
  try {
    Set-NormalAttributes -TargetPath $validatedQuarantinePath
    Assert-GuardedPath -OperationName 'Remove-QuarantinedPath' -TargetPath $validatedQuarantinePath -ExpectedRoot $QuarantineRootPath -RequireExisting | Out-Null
    Remove-Item -LiteralPath $validatedQuarantinePath -Recurse -Force
  }
  catch {
    throw "Remove-QuarantinedPath failed for '$validatedQuarantinePath': $($_.Exception.Message)"
  }

  $script:removedItems.Add([pscustomobject]@{
    scope = $Scope
    surface_kind = $SurfaceKind
    quarantined_path = $validatedQuarantinePath
  })
}

<#
.SYNOPSIS
Uses the runtime lock helper to assess or release the bundled runtime for one plugin root.
.DESCRIPTION
Delegates lock handling to the dedicated stop helper so removal and lock semantics stay aligned.
.PARAMETER PluginRootPath
Plugin root whose runtime should be assessed or stopped.
.PARAMETER DestructiveMode
True when quarantine or remove work is about to run.
.OUTPUTS
PSCustomObject describing the lock report and whether retirement work may proceed.
.NOTES
Critical dependencies: the bundled stop helper script, the current runtime lock contract, and PowerShell process launch.
#>
function Invoke-RuntimeLockHelper {
  param(
    [Parameter(Mandatory = $true)]
    [string]$PluginRootPath,
    [Parameter(Mandatory = $true)]
    [bool]$DestructiveMode
  )

  $stopScriptPath = Join-Path (Resolve-CanonRelativePath -RootPath $PluginRootPath -RelativePath $pathCanon.relative_paths.bundle_scripts_directory_relative_path) 'stop-anarchy-ai.ps1'
  if (-not (Test-Path $stopScriptPath)) {
    return [pscustomobject]@{
      status = 'helper_missing'
      matching_process_count = 0
      runtime_lock_state = 'unknown'
      may_retire = $true
    }
  }

  $stopMode = if (-not $DestructiveMode) {
    'AssessRuntimeLock'
  }
  elseif ($ForceRuntimeLockRelease) {
    'ForceReleaseRuntimeLock'
  }
  else {
    'SafeReleaseRuntimeLock'
  }

  $powershellExe = Join-Path $PSHOME 'powershell.exe'
  $rawOutput = & $powershellExe -ExecutionPolicy Bypass -File $stopScriptPath -Mode $stopMode -PluginRoot $PluginRootPath
  $exitCode = $LASTEXITCODE
  $report = $rawOutput | ConvertFrom-Json
  $result = [pscustomobject]@{
    plugin_root = $PluginRootPath
    mode = $stopMode
    exit_code = $exitCode
    runtime_lock_state = [string]$report.runtime_lock_state
    matching_process_count = [int]$report.matching_process_count
    actions_taken = @($report.actions_taken)
    stop_errors = @($report.stop_errors)
    may_retire = ($report.runtime_lock_state -eq 'not_running' -or $report.runtime_lock_state -eq 'stopped')
  }

  $script:runtimeLockReports.Add($result)
  return $result
}

<#
.SYNOPSIS
Removes Anarchy-AI plugin entries from a marketplace file after backing it up.
.DESCRIPTION
Supports the mixed-marketplace opt-in path while preserving a quarantined backup of the original file first.
.PARAMETER MarketplacePath
Marketplace file path to rewrite.
.PARAMETER Scope
Logical scope used for reporting and quarantine organization.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.OUTPUTS
PSCustomObject describing rewrite status and any backup path created.
.NOTES
Critical dependencies: Get-MarketplaceInventory, ConvertTo-Json, and pre-edit backup discipline.
#>
function Rewrite-MarketplaceWithoutAnarchy {
  param(
    [Parameter(Mandatory = $true)]
    [string]$MarketplacePath,
    [Parameter(Mandatory = $true)]
    [string]$Scope,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath
  )

  $validatedMarketplacePath = Assert-GuardedPath -OperationName 'Rewrite-MarketplaceWithoutAnarchy' -TargetPath $MarketplacePath -ExpectedRoot (Get-MarketplaceOwnerRoot -MarketplacePath $MarketplacePath) -RequireExisting
  $marketplaceOwnerRoot = Get-MarketplaceOwnerRoot -MarketplacePath $validatedMarketplacePath
  $marketplaceInventory = Get-MarketplaceInventory -MarketplacePath $validatedMarketplacePath -MarketplaceSourceRoot $marketplaceOwnerRoot
  if ($marketplaceInventory.parse_status -ne 'valid') {
    return [pscustomobject]@{
      status = 'skipped'
      reason = 'marketplace_invalid'
      quarantine_path = $null
    }
  }

  if ($marketplaceInventory.anarchy_entry_count -eq 0) {
    return [pscustomobject]@{
      status = 'skipped'
      reason = 'anarchy_entries_not_present'
      quarantine_path = $null
    }
  }

  $backupPath = Backup-FileToQuarantine -OriginalPath $validatedMarketplacePath -Scope $Scope -SurfaceKind 'marketplace_file' -QuarantineRootPath $QuarantineRootPath
  $retainedPlugins = @()
  foreach ($plugin in @($marketplaceInventory.marketplace_object.plugins)) {
    if (-not (Test-IsAnarchyPluginEntry -Plugin $plugin)) {
      $retainedPlugins += $plugin
    }
  }

  $marketplaceInventory.marketplace_object.plugins = @($retainedPlugins)
  try {
    Assert-GuardedPath -OperationName 'Rewrite-MarketplaceWithoutAnarchy' -TargetPath $validatedMarketplacePath -ExpectedRoot $marketplaceOwnerRoot -RequireExisting | Out-Null
    $marketplaceInventory.marketplace_object | ConvertTo-Json -Depth 10 | Set-Content $validatedMarketplacePath
  }
  catch {
    throw "Rewrite-MarketplaceWithoutAnarchy failed for '$validatedMarketplacePath': $($_.Exception.Message)"
  }

  $script:actionsTaken.Add("rewrote_$($Scope)_marketplace_without_anarchy_entries")
  if ($retainedPlugins.Count -eq 0) {
    $script:actionsTaken.Add("left_empty_$($Scope)_marketplace_after_anarchy_removal")
  }

  return [pscustomobject]@{
    status = 'updated'
    reason = $(if ($retainedPlugins.Count -eq 0) { 'rewrote_marketplace_to_empty_after_backup' } else { 'rewrote_marketplace_after_backup' })
    quarantine_path = $backupPath
  }
}

<#
.SYNOPSIS
Removes the Anarchy-AI custom-MCP block from the Codex config file after backup.
.DESCRIPTION
Backs up the config, removes the matching TOML block, normalizes repeated blank lines, and preserves newline style.
.PARAMETER ConfigPath
Codex config file path to rewrite.
.PARAMETER QuarantineRootPath
Absolute quarantine root.
.OUTPUTS
PSCustomObject describing rewrite status and any backup path created.
.NOTES
Critical dependencies: the custom-MCP block regex, config backup discipline, and newline normalization.
#>
function Rewrite-CodexConfigWithoutAnarchyBlock {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath,
    [Parameter(Mandatory = $true)]
    [string]$QuarantineRootPath
  )

  if (-not (Test-Path $ConfigPath)) {
    return [pscustomobject]@{
      status = 'missing'
      quarantine_path = $null
    }
  }

  $validatedConfigPath = Assert-GuardedPath -OperationName 'Rewrite-CodexConfigWithoutAnarchyBlock' -TargetPath $ConfigPath -ExpectedRoot (Split-Path (Split-Path $ConfigPath -Parent) -Parent) -RequireExisting
  $configOwnerRoot = Split-Path (Split-Path $validatedConfigPath -Parent) -Parent
  $content = Get-Content $validatedConfigPath -Raw
  if ($content -notmatch $script:codexCustomMcpServerBlockPattern) {
    return [pscustomobject]@{
      status = 'skipped'
      quarantine_path = $null
      reason = 'anarchy_block_not_present'
    }
  }

  $backupPath = Backup-FileToQuarantine -OriginalPath $validatedConfigPath -Scope 'user_profile' -SurfaceKind 'codex_config_file' -QuarantineRootPath $QuarantineRootPath
  $newline = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
  $updatedContent = [regex]::Replace($content, $script:codexCustomMcpServerBlockPattern, '')
  $updatedContent = [regex]::Replace($updatedContent, "(\r?\n){3,}", $newline + $newline)
  $updatedContent = $updatedContent.TrimEnd("`r", "`n")
  if (-not [string]::IsNullOrWhiteSpace($updatedContent)) {
    $updatedContent += $newline
  }

  try {
    Assert-GuardedPath -OperationName 'Rewrite-CodexConfigWithoutAnarchyBlock' -TargetPath $validatedConfigPath -ExpectedRoot $configOwnerRoot -RequireExisting | Out-Null
    Set-Content $validatedConfigPath -Value $updatedContent
  }
  catch {
    throw "Rewrite-CodexConfigWithoutAnarchyBlock failed for '$validatedConfigPath': $($_.Exception.Message)"
  }

  $script:actionsTaken.Add('removed_codex_custom_mcp_entry')
  return [pscustomobject]@{
    status = 'updated'
    quarantine_path = $backupPath
  }
}

<#
.SYNOPSIS
Processes one discovered inventory target according to the requested mode.
.DESCRIPTION
Performs inventory-only reporting in Assess mode and otherwise quarantines, rewrites, or removes the target with exact path validation.
.PARAMETER Target
Inventory target previously discovered by one of the scope collectors.
.PARAMETER ResolvedRepoRoot
Normalized repo root used for exact boundary validation.
.PARAMETER ResolvedUserProfileRoot
Normalized user-profile root used for exact boundary validation.
.PARAMETER ResolvedQuarantineRoot
Absolute quarantine root used for backup and retirement work.
.OUTPUTS
No direct return value. Mutates the shared action and result collections.
.NOTES
Critical dependencies: Move-PathToQuarantine, Rewrite-MarketplaceWithoutAnarchy, Rewrite-CodexConfigWithoutAnarchyBlock, and same-scope root validation.
#>
function Invoke-InventoryTargetAction {
  param(
    [Parameter(Mandatory = $true)]
    $Target,
    [string]$ResolvedRepoRoot,
    [Parameter(Mandatory = $true)]
    [string]$ResolvedUserProfileRoot,
    [Parameter(Mandatory = $true)]
    [string]$ResolvedQuarantineRoot
  )

  if ($Mode -eq 'Assess') {
    return
  }

  $expectedRoot = switch ($Target.scope) {
    'repo_local' { $ResolvedRepoRoot }
    'user_profile' { $ResolvedUserProfileRoot }
    'device_app' { $ResolvedUserProfileRoot }
    default { '' }
  }

  if ($Target.surface_kind -like '*plugin_root_directory' -and -not [string]::IsNullOrWhiteSpace($Target.path) -and (Test-Path $Target.path)) {
    $lockReport = Invoke-RuntimeLockHelper -PluginRootPath $Target.path -DestructiveMode $true
    if (-not $lockReport.may_retire) {
      $script:skippedItems.Add([pscustomobject]@{
        scope = $Target.scope
        surface_kind = $Target.surface_kind
        path = $Target.path
        reason = 'runtime_lock_prevented_retirement'
      })
      return
    }
  }

  switch ($Target.planned_action) {
    'quarantine_directory' {
      $isSelfTarget = -not [string]::IsNullOrWhiteSpace($Target.path) -and (Test-IsPathInsideRoot -CandidatePath $PSCommandPath -ExpectedRoot $Target.path)
      if ($isSelfTarget) {
        [void](Start-DeferredSelfRetirement -OriginalPath $Target.path -Scope $Target.scope -SurfaceKind $Target.surface_kind -ExpectedRoot $expectedRoot -QuarantineRootPath $ResolvedQuarantineRoot -PurgeAfterQuarantine ($Mode -eq 'Remove'))
        $script:actionsTaken.Add("scheduled_deferred_retirement:$($Target.surface_kind)")
        return
      }

      $retirement = Move-PathToQuarantine -OriginalPath $Target.path -Scope $Target.scope -SurfaceKind $Target.surface_kind -ExpectedRoot $expectedRoot -QuarantineRootPath $ResolvedQuarantineRoot
      if ($Mode -eq 'Remove' -and $retirement.status -eq 'quarantined') {
        Remove-QuarantinedPath -QuarantinePath $retirement.quarantine_path -QuarantineRootPath $ResolvedQuarantineRoot -Scope $Target.scope -SurfaceKind $Target.surface_kind
      }
      return
    }
    'quarantine_file' {
      $retirement = Move-PathToQuarantine -OriginalPath $Target.path -Scope $Target.scope -SurfaceKind $Target.surface_kind -ExpectedRoot $expectedRoot -QuarantineRootPath $ResolvedQuarantineRoot
      if ($Mode -eq 'Remove' -and $retirement.status -eq 'quarantined') {
        Remove-QuarantinedPath -QuarantinePath $retirement.quarantine_path -QuarantineRootPath $ResolvedQuarantineRoot -Scope $Target.scope -SurfaceKind $Target.surface_kind
      }
      return
    }
    'rewrite_file_after_backup' {
      if ($Target.surface_kind -eq 'marketplace_file') {
        $rewrite = Rewrite-MarketplaceWithoutAnarchy -MarketplacePath $Target.path -Scope $Target.scope -QuarantineRootPath $ResolvedQuarantineRoot
        if ($rewrite.status -eq 'updated' -and $Mode -eq 'Remove' -and -not [string]::IsNullOrWhiteSpace($rewrite.quarantine_path)) {
          Remove-QuarantinedPath -QuarantinePath $rewrite.quarantine_path -QuarantineRootPath $ResolvedQuarantineRoot -Scope $Target.scope -SurfaceKind $Target.surface_kind
        }
      }
      elseif ($Target.surface_kind -eq 'codex_config_file') {
        $rewrite = Rewrite-CodexConfigWithoutAnarchyBlock -ConfigPath $Target.path -QuarantineRootPath $ResolvedQuarantineRoot
        if ($rewrite.status -eq 'updated' -and $Mode -eq 'Remove' -and -not [string]::IsNullOrWhiteSpace($rewrite.quarantine_path)) {
          Remove-QuarantinedPath -QuarantinePath $rewrite.quarantine_path -QuarantineRootPath $ResolvedQuarantineRoot -Scope $Target.scope -SurfaceKind $Target.surface_kind
        }
      }
      return
    }
    default {
      $script:skippedItems.Add([pscustomobject]@{
        scope = $Target.scope
        surface_kind = $Target.surface_kind
        path = $Target.path
        reason = $Target.planned_action
      })
      return
    }
  }
}

if ([string]::IsNullOrWhiteSpace($UserProfileRoot)) {
  $UserProfileRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
}
else {
  $UserProfileRoot = [System.IO.Path]::GetFullPath($UserProfileRoot)
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $RepoRoot = Get-AutoRepoRoot -ResolvedUserProfileRoot $UserProfileRoot
}
else {
  $RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
}

$resolvedTargets = Resolve-TargetScopes -RequestedTargets @($Targets) -ResolvedRepoRoot $RepoRoot -ResolvedUserProfileRoot $UserProfileRoot

if ([string]::IsNullOrWhiteSpace($QuarantineRoot)) {
  $QuarantineRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("anarchy-ai-retired-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N'))
}
else {
  $QuarantineRoot = [System.IO.Path]::GetFullPath($QuarantineRoot)
}

if ($resolvedTargets -contains 'repo_local') {
  Add-RepoLocalTargets -ResolvedRepoRoot $RepoRoot
}

if ($resolvedTargets -contains 'user_profile') {
  Add-UserProfileTargets -ResolvedUserProfileRoot $UserProfileRoot
}

if ($resolvedTargets -contains 'device_app') {
  Add-DeviceAppTargets -ResolvedUserProfileRoot $UserProfileRoot
}

$inventorySnapshot = @($script:inventory | Select-Object -Unique path,scope,surface_kind,planned_action,details)
foreach ($target in $inventorySnapshot) {
  if ($Mode -eq 'Assess' -and $target.surface_kind -like '*plugin_root_directory' -and -not [string]::IsNullOrWhiteSpace($target.path) -and (Test-Path $target.path)) {
    [void](Invoke-RuntimeLockHelper -PluginRootPath $target.path -DestructiveMode $false)
  }

  Invoke-InventoryTargetAction -Target $target -ResolvedRepoRoot $RepoRoot -ResolvedUserProfileRoot $UserProfileRoot -ResolvedQuarantineRoot $QuarantineRoot
}

if ($inventorySnapshot.Count -eq 0) {
  $script:actionsTaken.Add('no_anarchy_ai_surfaces_found_for_requested_targets')
}

$sourcePaths = New-PathRoleReport `
  -RootPath $pluginRoot `
  -Directories ([ordered]@{
    script_directory_path = $scriptDir
    plugin_root_directory_path = $pluginRoot
    repo_root_directory_path = $RepoRoot
    user_profile_root_directory_path = $UserProfileRoot
  }) `
  -Files ([ordered]@{
    path_canon_file_path = $pathCanonPath
    current_script_file_path = $PSCommandPath
  }) `
  -Relative ([ordered]@{
    repo_local_marketplace_file_relative_path = [string]$pathCanon.relative_paths.repo_local_marketplace_file_relative_path
    user_profile_marketplace_file_relative_path = [string]$pathCanon.relative_paths.user_profile_marketplace_file_relative_path
    user_profile_codex_config_file_relative_path = [string]$pathCanon.relative_paths.user_profile_codex_config_file_relative_path
    user_profile_plugin_cache_parent_directory_relative_path = [string]$pathCanon.relative_paths.user_profile_plugin_cache_parent_directory_relative_path
  })

$destinationPaths = New-PathRoleReport `
  -RootPath $QuarantineRoot `
  -Directories ([ordered]@{
    quarantine_root_directory_path = $QuarantineRoot
  }) `
  -Files ([ordered]@{}) `
  -Relative ([ordered]@{})

$result = New-Object PSObject
$result | Add-Member -NotePropertyName mode -NotePropertyValue $Mode
$result | Add-Member -NotePropertyName targets_requested -NotePropertyValue ([object[]]@($resolvedTargets))
$result | Add-Member -NotePropertyName quarantine_root -NotePropertyValue $QuarantineRoot
$result | Add-Member -NotePropertyName inventory -NotePropertyValue ([object[]]@($inventorySnapshot))
$result | Add-Member -NotePropertyName runtime_lock_reports -NotePropertyValue ([object[]]$script:runtimeLockReports.ToArray())
$result | Add-Member -NotePropertyName actions_taken -NotePropertyValue ([object[]]@($script:actionsTaken | Select-Object -Unique))
$result | Add-Member -NotePropertyName findings -NotePropertyValue ([object[]]@($script:findings | Select-Object -Unique))
$result | Add-Member -NotePropertyName warnings -NotePropertyValue ([object[]]@($script:warnings | Select-Object -Unique))
$result | Add-Member -NotePropertyName quarantined_items -NotePropertyValue ([object[]]$script:quarantinedItems.ToArray())
$result | Add-Member -NotePropertyName removed_items -NotePropertyValue ([object[]]$script:removedItems.ToArray())
$result | Add-Member -NotePropertyName skipped_items -NotePropertyValue ([object[]]$script:skippedItems.ToArray())
$result | Add-Member -NotePropertyName preserved_items -NotePropertyValue ([object[]]$script:preservedItems.ToArray())
$result | Add-Member -NotePropertyName deferred_items -NotePropertyValue ([object[]]$script:scheduledDeferredItems.ToArray())
$result | Add-Member -NotePropertyName next_action -NotePropertyValue $(if ($Mode -eq 'Assess') {
  'review_inventory_then_run_quarantine'
}
elseif ($script:scheduledDeferredItems.Count -gt 0) {
  'allow_deferred_self_retirement_to_finish_then_restart_codex'
}
else {
  'restart_codex_and_reinstall_only_if_needed'
})
$result | Add-Member -NotePropertyName paths -NotePropertyValue ([ordered]@{
  source = $sourcePaths
  destination = $destinationPaths
})

$result | ConvertTo-Json -Depth 10

$hasManualReview = @($inventorySnapshot | Where-Object { $_.planned_action -eq 'manual_review_required' }).Count -gt 0
$hasRuntimeLockSkips = @($script:skippedItems | Where-Object { $_.reason -eq 'runtime_lock_prevented_retirement' }).Count -gt 0
if ($hasManualReview -or $hasRuntimeLockSkips) {
  exit 1
}

exit 0
