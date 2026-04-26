<#
.SYNOPSIS
Verifies that plugin-mirror schemas match the canonical root schemas and the bundle manifest,
and that the .claude skill mirror matches the plugin's canonical skill copies.
.DESCRIPTION
Two canonical/mirror pairs ship in this repo; they drift in opposite directions and both are
verified here.

Pair 1 - Schema family (root is canonical, plugin ships the mirror):
  root file <-> plugins/anarchy-ai/schemas/<file> <-> schema-bundle.manifest.json sha256 entry

Pair 2 - Skill family (plugin is canonical, .claude ships the local runtime mirror):
  plugins/anarchy-ai/skills/<skill>/SKILL.md <-> .claude/skills/<skill>/SKILL.md

There is no manifest on the skill side yet; a byte-for-byte sha256 match is the contract.
Catching drift here is the first closed-loop verification of Anarchy-AI invariants that do
not depend on agent reading discipline. See the "one closed loop" stance in
AGENTS-schema-comparison-matrix.md (and the pen test PF-01 context on fail-open governance).
.PARAMETER RepoRoot
Optional repo root override; defaults to the current script's repo.
.OUTPUTS
JSON describing audit status, finding count, and per-file drift details.
.NOTES
Critical dependencies: canonical root schemas, plugin mirror directory, bundle manifest,
plugin canonical skills, and .claude skill mirror.
Exit code is 0 on clean, 1 on any finding, matching the existing compliance-script contract.
#>
param(
  [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
  $RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..'))
}
else {
  $RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
}

<#
.SYNOPSIS
Adds one audit finding to the shared findings list.
.DESCRIPTION
Creates a stable finding record so the final JSON output stays easy to scan and machine-readable.
.PARAMETER Findings
Shared finding list.
.PARAMETER RelativePath
Repo-relative file path under audit.
.PARAMETER FindingType
Short finding classification.
.PARAMETER Detail
Human-readable explanation of the mismatch.
.OUTPUTS
No direct return value.
.NOTES
Critical dependencies: System.Collections.Generic.List and the final JSON result contract.
#>
function Add-Finding {
  param(
    [AllowEmptyCollection()]
    [Parameter(Mandatory = $true)]
    [System.Collections.Generic.List[object]]$Findings,
    [Parameter(Mandatory = $true)]
    [string]$RelativePath,
    [Parameter(Mandatory = $true)]
    [string]$FindingType,
    [Parameter(Mandatory = $true)]
    [string]$Detail
  )

  $finding = [ordered]@{
    file   = $RelativePath
    type   = $FindingType
    detail = $Detail
  }

  $Findings.Add([pscustomobject]$finding)
}

<#
.SYNOPSIS
Computes the lowercase SHA-256 hex digest of a file.
.DESCRIPTION
Uses Get-FileHash for a stable digest; normalizes to lowercase so manifest comparisons are
case-insensitive-safe without string tricks at each comparison site.
.PARAMETER Path
Absolute path to the target file.
.OUTPUTS
Lowercase hex SHA-256 string, or $null when the file does not exist.
.NOTES
Returning $null on missing file lets the caller raise a targeted "missing file" finding instead
of a generic hash-failure trace.
#>
function Get-Sha256Lower {
  param([Parameter(Mandatory = $true)][string]$Path)

  if (-not (Test-Path -LiteralPath $Path)) { return $null }
  return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$findings = New-Object System.Collections.Generic.List[object]

# The canonical schema family: files authored at repo root, mirrored into the plugin.
# Every entry here must round-trip root <-> mirror <-> manifest with the same sha256.
$canonicalFiles = @(
  'AGENTS-schema-governance.json',
  'AGENTS-schema-1project.json',
  'AGENTS-schema-narrative.json',
  'AGENTS-schema-gov2gov-migration.json',
  'AGENTS-schema-triage.md',
  'Getting-Started-For-Humans.txt'
)

$pluginMirrorDir = Join-Path $RepoRoot 'plugins\anarchy-ai\schemas'
$manifestPath    = Join-Path $pluginMirrorDir 'schema-bundle.manifest.json'

# Files that live only at the plugin (like the manifest itself) are not mirrored by definition;
# they only live in one place. The triage.md and Getting-Started.txt entries mirror into
# the plugin alongside the four JSON schemas -- verify the set the manifest claims to cover.
$manifestEntries = @{}
$manifestMissing = $false

if (-not (Test-Path -LiteralPath $manifestPath)) {
  Add-Finding -Findings $findings -RelativePath 'plugins/anarchy-ai/schemas/schema-bundle.manifest.json' `
    -FindingType 'manifest-missing' `
    -Detail 'Bundle manifest not found; cannot verify sha256 anchors. Regenerate the manifest.'
  $manifestMissing = $true
}
else {
  try {
    $manifestRaw = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8
    $manifest = $manifestRaw | ConvertFrom-Json
    foreach ($entry in $manifest.files) {
      $manifestEntries[$entry.file_name] = $entry.sha256.ToLowerInvariant()
    }
  }
  catch {
    Add-Finding -Findings $findings -RelativePath 'plugins/anarchy-ai/schemas/schema-bundle.manifest.json' `
      -FindingType 'manifest-unreadable' `
      -Detail ("Manifest parse failed: {0}" -f $_.Exception.Message)
    $manifestMissing = $true
  }
}

foreach ($file in $canonicalFiles) {
  $rootPath   = Join-Path $RepoRoot $file
  $mirrorPath = Join-Path $pluginMirrorDir $file

  $rootHash   = Get-Sha256Lower -Path $rootPath
  $mirrorHash = Get-Sha256Lower -Path $mirrorPath

  if ($null -eq $rootHash) {
    Add-Finding -Findings $findings -RelativePath $file `
      -FindingType 'root-missing' `
      -Detail 'Canonical root file not found. Restore from git history or re-author.'
    continue
  }

  if ($null -eq $mirrorHash) {
    Add-Finding -Findings $findings -RelativePath ("plugins/anarchy-ai/schemas/{0}" -f $file) `
      -FindingType 'mirror-missing' `
      -Detail 'Plugin mirror file not found. Copy from canonical root.'
    continue
  }

  if ($rootHash -ne $mirrorHash) {
    Add-Finding -Findings $findings -RelativePath ("plugins/anarchy-ai/schemas/{0}" -f $file) `
      -FindingType 'mirror-drift' `
      -Detail ("Mirror sha256 does not match canonical root. root={0} mirror={1}" -f $rootHash, $mirrorHash)
  }

  if (-not $manifestMissing) {
    if (-not $manifestEntries.ContainsKey($file)) {
      Add-Finding -Findings $findings -RelativePath $file `
        -FindingType 'manifest-entry-missing' `
        -Detail 'Canonical file is not listed in schema-bundle.manifest.json.'
    }
    else {
      $manifestHash = $manifestEntries[$file]
      if ($manifestHash -ne $rootHash) {
        Add-Finding -Findings $findings -RelativePath $file `
          -FindingType 'manifest-drift' `
          -Detail ("Manifest sha256 does not match canonical root. root={0} manifest={1}" -f $rootHash, $manifestHash)
      }
    }
  }
}

# Detect stray canonical-named files in the manifest that are not in our canonical list.
if (-not $manifestMissing) {
  foreach ($declared in $manifestEntries.Keys) {
    if (-not ($canonicalFiles -contains $declared)) {
      Add-Finding -Findings $findings -RelativePath $declared `
        -FindingType 'manifest-unknown-entry' `
        -Detail 'Manifest lists a file that is not part of the canonical schema family.'
    }
  }
}

# ---------------------------------------------------------------------------
# Pair 2: plugin canonical skills <-> host skill mirrors (inverse direction)
# ---------------------------------------------------------------------------
# Unlike the schema pair, the plugin is the authoring surface for skills, and
# host-local skill mirrors are convenience/runtime surfaces. There is no manifest
# anchor on this side yet; the contract is a byte-for-byte sha256 match per pair.
$pluginSkillsDir  = Join-Path $RepoRoot 'plugins\anarchy-ai\skills'
$claudeSkillsDir  = Join-Path $RepoRoot '.claude\skills'
$codexSkillsDir   = Join-Path $RepoRoot '.codex\skills'

$canonicalSkillMirrors = @(
  @{
    skill = 'structured-commit/SKILL.md'
    mirrors = @(
      @{
        root = $claudeSkillsDir
        root_relative = '.claude/skills'
      }
    )
  },
  @{
    skill = 'structured-review/SKILL.md'
    mirrors = @(
      @{
        root = $claudeSkillsDir
        root_relative = '.claude/skills'
      }
    )
  },
  @{
    skill = 'chat-history-capture/SKILL.md'
    mirrors = @(
      @{
        root = $claudeSkillsDir
        root_relative = '.claude/skills'
      },
      @{
        root = $codexSkillsDir
        root_relative = '.codex/skills'
      }
    )
  }
)

foreach ($skillEntry in $canonicalSkillMirrors) {
  $skillRel = [string]$skillEntry.skill
  # PS 5.1 Join-Path only takes one child segment at a time; resolve via a single string.
  $pluginPath = Join-Path $pluginSkillsDir ($skillRel -replace '/', '\')
  $pluginHash = Get-Sha256Lower -Path $pluginPath
  $pluginRel = "plugins/anarchy-ai/skills/$skillRel"

  if ($null -eq $pluginHash) {
    Add-Finding -Findings $findings -RelativePath $pluginRel `
      -FindingType 'skill-canonical-missing' `
      -Detail 'Canonical plugin skill file not found. Restore from git history or re-author.'
    continue
  }

  foreach ($mirror in @($skillEntry.mirrors)) {
    $mirrorRoot = [string]$mirror.root
    $mirrorRootRelative = [string]$mirror.root_relative
    $mirrorPath = Join-Path $mirrorRoot ($skillRel -replace '/', '\')
    $mirrorRel = "$mirrorRootRelative/$skillRel"
    $mirrorHash = Get-Sha256Lower -Path $mirrorPath

    if ($null -eq $mirrorHash) {
      Add-Finding -Findings $findings -RelativePath $mirrorRel `
        -FindingType 'skill-mirror-missing' `
        -Detail 'Local skill mirror not found. Copy from plugin canonical.'
      continue
    }

    if ($pluginHash -ne $mirrorHash) {
      Add-Finding -Findings $findings -RelativePath $mirrorRel `
        -FindingType 'skill-mirror-drift' `
        -Detail ("Skill mirror sha256 does not match plugin canonical. plugin={0} mirror={1}" -f $pluginHash, $mirrorHash)
    }
  }
}

$status = if ($findings.Count -eq 0) { 'clean' } else { 'findings' }

$result = [ordered]@{
  status           = $status
  repo_root        = $RepoRoot
  canonical_set    = $canonicalFiles
  canonical_skills = @($canonicalSkillMirrors | ForEach-Object { $_.skill })
  finding_count    = $findings.Count
  findings         = $findings
}

$result | ConvertTo-Json -Depth 6

if ($findings.Count -gt 0) { exit 1 } else { exit 0 }
