<#
.SYNOPSIS
Validates the Anarchy-AI retirement helpers against the recent live cleanup incident.
.DESCRIPTION
Creates temporary user-profile fixtures inside the repo workspace, then proves the machine-facing
retirement helper detects legacy bundles, retires Anarchy-only marketplace files instead of leaving
empty shells, and preserves unrelated Codex config sections unless explicit legacy custom-MCP cleanup
is requested.
.PARAMETER RepoRoot
Absolute repo root used to locate the helper and create workspace-safe temporary fixtures.
.OUTPUTS
Plain-language success output on pass; throws on the first failed assertion.
.NOTES
Critical dependencies: the current remove-anarchy-ai.ps1 helper, PowerShell JSON parsing, and a
writable temporary lane beneath the repo workspace.
#>
param(
  [Parameter(Mandatory = $true)]
  [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

<#
.SYNOPSIS
Throws when a required condition is false.
.DESCRIPTION
Keeps the fixture script short and fail-fast while still naming the exact regression that surfaced.
.PARAMETER Condition
Boolean expression that must evaluate to true.
.PARAMETER Message
Failure message to throw when the condition is false.
.OUTPUTS
No direct return value. Throws on failure.
.NOTES
Critical dependencies: standard PowerShell boolean evaluation.
#>
function Assert-Condition {
  param(
    [Parameter(Mandatory = $true)]
    [bool]$Condition,
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  if (-not $Condition) {
    throw $Message
  }
}

<#
.SYNOPSIS
Runs the machine-facing retirement helper and parses its JSON result.
.DESCRIPTION
Launches the live helper under test, captures its stdout, and converts the JSON payload for
assertions.
.PARAMETER HelperPath
Absolute helper script path.
.PARAMETER Parameters
Hashtable of helper parameters.
.OUTPUTS
PSCustomObject containing the parsed JSON result and helper exit code.
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
  Assert-Condition -Condition (-not [string]::IsNullOrWhiteSpace($rawOutput)) -Message 'Retirement helper returned no JSON output.'

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
Creates one temporary user-profile fixture for retirement-helper validation.
.DESCRIPTION
Builds a legacy home-local Anarchy bundle, Anarchy-only marketplace, optional custom-MCP config,
and documented plugin cache inside a workspace-safe path.
.PARAMETER RootPath
Absolute fixture root beneath the repo workspace.
.OUTPUTS
Hashtable describing the created paths.
.NOTES
Critical dependencies: New-Item, Set-Content, and the current legacy install shapes.
#>
function New-RemovalFixture {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath
  )

  if (Test-Path $RootPath) {
    Remove-Item -LiteralPath $RootPath -Recurse -Force
  }

  $userProfileRoot = Join-Path $RootPath 'home'
  $legacyPluginRoot = Join-Path $userProfileRoot '.codex\plugins\anarchy-ai-herringms'
  $manifestPath = Join-Path $legacyPluginRoot '.codex-plugin\plugin.json'
  $marketplacePath = Join-Path $userProfileRoot '.agents\plugins\marketplace.json'
  $configPath = Join-Path $userProfileRoot '.codex\config.toml'
  $cachePath = Join-Path $userProfileRoot '.codex\plugins\cache\anarchy-ai-user-profile'

  New-Item -ItemType Directory -Path (Split-Path $manifestPath -Parent) -Force | Out-Null
  New-Item -ItemType Directory -Path (Split-Path $marketplacePath -Parent) -Force | Out-Null
  New-Item -ItemType Directory -Path $cachePath -Force | Out-Null

  Set-Content -LiteralPath $manifestPath -Value '{"name":"anarchy-ai"}' -Encoding UTF8
  Set-Content -LiteralPath $marketplacePath -Encoding UTF8 -Value @'
{
  "name": "anarchy-ai-user-profile",
  "interface": {
    "displayName": "Anarchy-AI User Profile"
  },
  "plugins": [
    {
      "name": "anarchy-ai",
      "source": {
        "source": "local",
        "path": "./.codex/plugins/anarchy-ai"
      },
      "policy": {
        "installation": "INSTALLED_BY_DEFAULT",
        "authentication": "ON_INSTALL"
      },
      "category": "Productivity"
    }
  ]
}
'@
  Set-Content -LiteralPath $configPath -Encoding UTF8 -Value @'
[plugins."teams@openai-curated"]
enabled = true

[mcp_servers.anarchy-ai]
command = "C:\temp\AnarchyAi.Mcp.Server.exe"
cwd = "C:\temp"
enabled = true

[windows]
sandbox = "elevated"

[projects.'C:\Temp\TestRepo']
trust_level = "trusted"
'@

  return @{
    user_profile_root = $userProfileRoot
    legacy_plugin_root = $legacyPluginRoot
    marketplace_path = $marketplacePath
    config_path = $configPath
    cache_path = $cachePath
  }
}

$resolvedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$helperPath = Join-Path $resolvedRepoRoot 'plugins\anarchy-ai\scripts\remove-anarchy-ai.ps1'
Assert-Condition -Condition (Test-Path $helperPath) -Message "Retirement helper not found: $helperPath"

$fixtureRoot = Join-Path $resolvedRepoRoot '.tmp\removal-safety-compliance'
if (Test-Path $fixtureRoot) {
  Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
}
$defaultFixture = New-RemovalFixture -RootPath (Join-Path $fixtureRoot 'default')
$optInFixture = New-RemovalFixture -RootPath (Join-Path $fixtureRoot 'opt-in')
$defaultQuarantineRoot = Join-Path $fixtureRoot 'default-quarantine'
$optInQuarantineRoot = Join-Path $fixtureRoot 'opt-in-quarantine'

$defaultAssess = Invoke-RemovalHelperJson -HelperPath $helperPath -Parameters @{
  Mode = 'Assess'
  UserProfileRoot = [string]$defaultFixture.user_profile_root
  Targets = @('user_profile', 'device_app')
  QuarantineRoot = $defaultQuarantineRoot
}

$defaultInventory = @($defaultAssess.result.inventory)
Assert-Condition -Condition (@($defaultInventory | Where-Object { $_.surface_kind -eq 'legacy_plugin_root_directory' -and $_.path -eq [string]$defaultFixture.legacy_plugin_root }).Count -eq 1) -Message 'Default assess did not detect the legacy home-local plugin root.'
Assert-Condition -Condition (@($defaultInventory | Where-Object { $_.surface_kind -eq 'marketplace_file' }).Count -eq 1) -Message 'Default assess did not inventory the user-profile marketplace file.'
Assert-Condition -Condition (@($defaultInventory | Where-Object { $_.surface_kind -eq 'codex_config_file' }).Count -eq 0) -Message 'Default assess should not inventory shared Codex config.'
Assert-Condition -Condition (@($defaultAssess.result.findings | Where-Object { $_ -eq 'legacy_custom_mcp_block_present_not_targeted_by_default' }).Count -eq 1) -Message 'Default assess did not report the preserved legacy custom MCP block.'

$defaultCleanup = Invoke-RemovalHelperJson -HelperPath $helperPath -Parameters @{
  Mode = 'Quarantine'
  UserProfileRoot = [string]$defaultFixture.user_profile_root
  Targets = @('user_profile', 'device_app')
  QuarantineRoot = $defaultQuarantineRoot
}

Assert-Condition -Condition ($defaultCleanup.exit_code -eq 0) -Message 'Default quarantine cleanup failed.'
Assert-Condition -Condition (-not (Test-Path -LiteralPath $defaultFixture.marketplace_path)) -Message 'Anarchy-only marketplace file should be removed after backup instead of left empty.'
Assert-Condition -Condition (-not (Test-Path -LiteralPath $defaultFixture.legacy_plugin_root)) -Message 'Legacy home-local plugin root should be quarantined by default cleanup.'
Assert-Condition -Condition (-not (Test-Path -LiteralPath $defaultFixture.cache_path)) -Message 'Owned plugin cache directory should be quarantined by default cleanup.'

$defaultConfigContent = Get-Content -LiteralPath $defaultFixture.config_path -Raw
Assert-Condition -Condition ($defaultConfigContent -match '\[mcp_servers\.anarchy-ai\]') -Message 'Default cleanup should leave legacy custom MCP config untouched.'
Assert-Condition -Condition ($defaultConfigContent -match '\[windows\]') -Message 'Default cleanup should preserve unrelated windows config.'
Assert-Condition -Condition ($defaultConfigContent -match "\[projects\.'C:\\Temp\\TestRepo'\]") -Message 'Default cleanup should preserve unrelated project trust config.'

$optInCleanup = Invoke-RemovalHelperJson -HelperPath $helperPath -Parameters @{
  Mode = 'Quarantine'
  UserProfileRoot = [string]$optInFixture.user_profile_root
  Targets = @('user_profile')
  QuarantineRoot = $optInQuarantineRoot
  IncludeLegacyCustomMcpConfig = $true
}

Assert-Condition -Condition ($optInCleanup.exit_code -eq 0) -Message 'Opt-in legacy custom MCP cleanup failed.'
$optInConfigContent = Get-Content -LiteralPath $optInFixture.config_path -Raw
Assert-Condition -Condition ($optInConfigContent -notmatch '\[mcp_servers\.anarchy-ai\]') -Message 'Opt-in cleanup did not remove the owned custom MCP block.'
Assert-Condition -Condition ($optInConfigContent -match '\[windows\]') -Message 'Opt-in cleanup removed unrelated windows config.'
Assert-Condition -Condition ($optInConfigContent -match "\[projects\.'C:\\Temp\\TestRepo'\]") -Message 'Opt-in cleanup removed unrelated project trust config.'
Assert-Condition -Condition ($optInConfigContent -match '\[plugins\."teams@openai-curated"\]') -Message 'Opt-in cleanup removed unrelated curated plugin config.'

if (Test-Path $fixtureRoot) {
  Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
}

Write-Output 'Removal safety compliance passed.'
