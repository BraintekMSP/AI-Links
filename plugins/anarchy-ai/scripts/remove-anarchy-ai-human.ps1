<#
.SYNOPSIS
Runs the human-friendly Anarchy-AI cleanup flow for Windows users.
.DESCRIPTION
Discovers the machine-facing retirement helper, expands cleanup to every reachable human-owned
scope for the current repo and user profile, shows a plain-language summary, then runs safe
quarantine cleanup by default. Shared Codex config is intentionally left untouched unless an
advanced caller uses the machine-facing helper directly. Repo-authored source truth remains preserved.
.PARAMETER CleanupMode
Optional cleanup mode override. Defaults to Quarantine for the human click-once flow.
.OUTPUTS
Plain-language console output plus the same exit code semantics used by the machine-facing helper.
.NOTES
Critical dependencies: the sibling remove-anarchy-ai.ps1 helper, JSON result parsing, current
repo/home auto-detection from the helper, plain-language summarization of cleanup results, and the
machine helper's safer default of not rewriting shared Codex config.
#>
param(
  [ValidateSet('Assess','Quarantine','Remove')]
  [string]$CleanupMode = 'Quarantine'
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$machineHelperPath = Join-Path $scriptDir 'remove-anarchy-ai.ps1'
if (-not (Test-Path $machineHelperPath)) {
  throw "Machine-facing retirement helper not found: $machineHelperPath"
}

<#
.SYNOPSIS
Runs the machine-facing retirement helper and parses its JSON result.
.DESCRIPTION
Captures stdout/stderr, preserves the helper exit code, and converts the JSON payload so the
human-facing flow can summarize cleanup in plain language.
.PARAMETER HelperPath
Absolute path to the machine-facing retirement helper.
.PARAMETER Parameters
Hashtable of parameters forwarded to the helper.
.OUTPUTS
PSCustomObject containing helper exit code, parsed JSON result, and raw output.
.NOTES
Critical dependencies: helper JSON contract stability and ConvertFrom-Json.
#>
function Invoke-RemovalHelperJson {
  param(
    [Parameter(Mandatory = $true)]
    [string]$HelperPath,
    [Parameter(Mandatory = $true)]
    [hashtable]$Parameters
  )

  $rawOutput = (& $HelperPath @Parameters 2>&1 | Out-String).Trim()
  $exitCode = $LASTEXITCODE
  if ([string]::IsNullOrWhiteSpace($rawOutput)) {
    throw "Retirement helper returned no JSON output."
  }

  try {
    $parsed = $rawOutput | ConvertFrom-Json
  }
  catch {
    throw "Retirement helper returned non-JSON output:`n$rawOutput"
  }

  return [pscustomobject]@{
    exit_code = $exitCode
    result = $parsed
    raw_output = $rawOutput
  }
}

<#
.SYNOPSIS
Builds the reachable human cleanup scopes from helper-discovered paths.
.DESCRIPTION
Uses the helper's repo/home detection so the human front door can widen to every reachable
human-owned scope without duplicating pathing rules.
.PARAMETER DetectionResult
Parsed helper result from the discovery assess pass.
.OUTPUTS
Hashtable containing repo root, user-profile root, and normalized target scopes.
.NOTES
Critical dependencies: helper path-report contract and exact path existence checks.
#>
function Get-HumanCleanupPlan {
  param(
    [Parameter(Mandatory = $true)]
    [pscustomobject]$DetectionResult
  )

  $repoRoot = [string]$DetectionResult.paths.source.directories.repo_root_directory_path
  $userProfileRoot = [string]$DetectionResult.paths.source.directories.user_profile_root_directory_path
  $targets = New-Object System.Collections.Generic.List[string]

  if (-not [string]::IsNullOrWhiteSpace($repoRoot) -and (Test-Path $repoRoot)) {
    $targets.Add('repo_local')
  }

  if (-not [string]::IsNullOrWhiteSpace($userProfileRoot) -and (Test-Path $userProfileRoot)) {
    $targets.Add('user_profile')
    $targets.Add('device_app')
  }

  return @{
    repo_root = $repoRoot
    user_profile_root = $userProfileRoot
    targets = @($targets | Select-Object -Unique)
  }
}

<#
.SYNOPSIS
Maps one cleanup scope to a short human-readable label.
.DESCRIPTION
Keeps human summary output consistent across assess and cleanup phases.
.PARAMETER Scope
Internal cleanup scope value.
.OUTPUTS
System.String. Human-friendly label.
.NOTES
Critical dependencies: stable scope vocabulary from the machine-facing helper.
#>
function Get-FriendlyScopeLabel {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Scope
  )

  switch ($Scope) {
    'repo_local' { return 'This repo' }
    'user_profile' { return 'This user profile' }
    'device_app' { return 'Codex app cache' }
    default { return $Scope }
  }
}

<#
.SYNOPSIS
Maps one cleanup surface kind to a short human-readable label.
.DESCRIPTION
Turns maintenance-oriented surface names into output that makes sense during a human cleanup flow.
.PARAMETER SurfaceKind
Internal surface kind value from the machine-facing helper.
.OUTPUTS
System.String. Human-friendly surface label.
.NOTES
Critical dependencies: current surface-kind vocabulary from the retirement inventory contract.
#>
function Get-FriendlySurfaceLabel {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SurfaceKind
  )

  switch ($SurfaceKind) {
    'marketplace_file' { return 'plugin registration' }
    'plugin_root_directory' { return 'installed plugin bundle' }
    'legacy_plugin_root_directory' { return 'legacy plugin bundle' }
    'codex_config_file' { return 'optional custom MCP config' }
    'codex_plugin_enable_state_file' { return 'Codex plugin enable-state' }
    'plugin_cache_directory' { return 'documented plugin cache' }
    default { return $SurfaceKind.Replace('_', ' ') }
  }
}

<#
.SYNOPSIS
Writes a plain-language summary of the planned cleanup inventory.
.DESCRIPTION
Explains what the human cleanup flow will touch and what it will preserve before any cleanup runs.
.PARAMETER AssessmentResult
Parsed helper result from the full-scope assess pass.
.PARAMETER CleanupMode
Human cleanup mode being used.
.OUTPUTS
Plain-language console output only.
.NOTES
Critical dependencies: inventory contract shape, preserved-item reporting, and human-friendly label helpers.
#>
function Write-HumanAssessmentSummary {
  param(
    [Parameter(Mandatory = $true)]
    [pscustomobject]$AssessmentResult,
    [Parameter(Mandatory = $true)]
    [string]$CleanupMode
  )

  Write-Host ''
  Write-Host 'Anarchy-AI safe cleanup'
  Write-Host '-----------------------'
  if ($CleanupMode -eq 'Assess') {
    Write-Host 'Review only. No changes will be made.'
  }
  else {
    Write-Host 'Starting safe cleanup now. Live Anarchy-AI surfaces will be quarantined, not permanently deleted.'
  }

  Write-Host 'Repo-authored source files stay in place.'
  Write-Host ''
  Write-Host 'Cleanup scopes:'
  foreach ($scope in @($AssessmentResult.targets_requested)) {
    Write-Host ('- {0}' -f (Get-FriendlyScopeLabel -Scope ([string]$scope)))
  }

  Write-Host ''
  if (@($AssessmentResult.inventory).Count -eq 0) {
    Write-Host 'No live Anarchy-AI surfaces were found for the reachable repo/user context.'
  }
  else {
    Write-Host 'Planned cleanup:'
    foreach ($item in @($AssessmentResult.inventory)) {
      Write-Host ('- {0}: {1}' -f (Get-FriendlyScopeLabel -Scope ([string]$item.scope)), (Get-FriendlySurfaceLabel -SurfaceKind ([string]$item.surface_kind)))
      Write-Host ('  {0}' -f [string]$item.path)
    }
  }

  $preservedItems = @($AssessmentResult.preserved_items)
  if ($preservedItems.Count -gt 0) {
    Write-Host ''
    Write-Host 'Preserved source truth:'
    foreach ($item in $preservedItems) {
      Write-Host ('- {0}' -f [string]$item.path)
    }
  }

  if (@($AssessmentResult.findings | Where-Object { $_ -eq 'legacy_custom_mcp_block_present_not_targeted_by_default' }).Count -gt 0) {
    Write-Host ''
    Write-Host 'Left untouched by default:'
    Write-Host '- Optional legacy custom MCP config in ~/.codex/config.toml'
  }
}

<#
.SYNOPSIS
Writes a plain-language summary after cleanup completes.
.DESCRIPTION
Explains what happened, where the quarantine lives, and whether any warnings or manual review remain.
.PARAMETER CleanupResult
Parsed helper result from the cleanup run.
.OUTPUTS
Plain-language console output only.
.NOTES
Critical dependencies: cleanup result contract shape and human-friendly label helpers.
#>
function Write-HumanCleanupSummary {
  param(
    [Parameter(Mandatory = $true)]
    [pscustomobject]$CleanupResult
  )

  Write-Host ''
  if ($CleanupResult.quarantined_items.Count -gt 0 -or $CleanupResult.actions_taken.Count -gt 0) {
    Write-Host 'Cleanup finished.'
  }
  else {
    Write-Host 'Cleanup finished with nothing to change.'
  }

  Write-Host ('Quarantine folder: {0}' -f [string]$CleanupResult.quarantine_root)

  if (@($CleanupResult.quarantined_items).Count -gt 0) {
    Write-Host ''
    Write-Host 'Quarantined items:'
    foreach ($item in @($CleanupResult.quarantined_items)) {
      Write-Host ('- {0}: {1}' -f (Get-FriendlyScopeLabel -Scope ([string]$item.scope)), (Get-FriendlySurfaceLabel -SurfaceKind ([string]$item.surface_kind)))
      Write-Host ('  {0}' -f [string]$item.quarantine_path)
    }
  }

  if (@($CleanupResult.findings).Count -gt 0) {
    Write-Host ''
    Write-Host 'Findings still worth reviewing:'
    foreach ($finding in @($CleanupResult.findings)) {
      Write-Host ('- {0}' -f [string]$finding)
    }
  }

  if (@($CleanupResult.warnings).Count -gt 0) {
    Write-Host ''
    Write-Host 'Warnings:'
    foreach ($warning in @($CleanupResult.warnings)) {
      Write-Host ('- {0}' -f [string]$warning)
    }
  }

  Write-Host ''
  Write-Host 'Nothing was permanently deleted.'
  Write-Host 'If you are satisfied later, you can remove the quarantine folder manually.'
}

$discovery = Invoke-RemovalHelperJson -HelperPath $machineHelperPath -Parameters @{ Mode = 'Assess' }
$plan = Get-HumanCleanupPlan -DetectionResult $discovery.result
if ($plan.targets.Count -eq 0) {
  Write-Host 'Anarchy-AI cleanup could not detect a reachable repo or user-profile context from this location.'
  exit 1
}

$fullAssessParameters = @{
  Mode = 'Assess'
  UserProfileRoot = [string]$plan.user_profile_root
  Targets = @($plan.targets)
}

if (-not [string]::IsNullOrWhiteSpace([string]$plan.repo_root)) {
  $fullAssessParameters['RepoRoot'] = [string]$plan.repo_root
}

$fullAssess = Invoke-RemovalHelperJson -HelperPath $machineHelperPath -Parameters $fullAssessParameters
Write-HumanAssessmentSummary -AssessmentResult $fullAssess.result -CleanupMode $CleanupMode

if ($CleanupMode -eq 'Assess' -or @($fullAssess.result.inventory).Count -eq 0) {
  exit 0
}

$cleanupParameters = @{
  Mode = $CleanupMode
  UserProfileRoot = [string]$plan.user_profile_root
  Targets = @($plan.targets)
}

if (-not [string]::IsNullOrWhiteSpace([string]$plan.repo_root)) {
  $cleanupParameters['RepoRoot'] = [string]$plan.repo_root
}

$cleanup = Invoke-RemovalHelperJson -HelperPath $machineHelperPath -Parameters $cleanupParameters
Write-HumanCleanupSummary -CleanupResult $cleanup.result
exit $cleanup.exit_code
