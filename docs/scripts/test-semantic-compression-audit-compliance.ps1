<#
.SYNOPSIS
Prototype detector for two classes of post-migration semantic compression failure.
.DESCRIPTION
Implements the mechanical portion of the gov2gov "semantic-compression-handoff-to-measurement"
invariant. A migration is classified-and-budget-compliant but semantically incomplete when:

  Class 1 -- stale current-state language: a doc anchors claims to a past date using live-
            truth framing ("as of <stale date>", "latest <thing> -- <stale date>",
            "(by|before|on) <now-past future date>"), which means the reader is told the
            content is current while the timestamp shows it is not.

  Class 3 -- duplicate governance prose: after extracting governance content from a source
            into a target core, the source still carries near-verbatim paragraphs from the
            extraction, producing parallel authority.

Class 2 (dated snapshot claims without an explicit date token, e.g. "the system has 35
services" where 35 was true then and is not true now) is deliberately out of scope here.
It is not reliably detectable without semantic understanding, and routing it to a human
review checklist would relocate the failure rather than close it -- humans bring greater
context but also greater attention-deficit and heuristic error risk. Class 2 is tabled as an
acknowledged gap rather than a covered-by-checklist pseudo-check; see the patch list in
ASSUMPTION_FAILURE_PEN_TEST.md (PF-05).

This is a PROTOTYPE. It has not yet been validated against a real post-migration corpus.
Do not wire it into structured-commit or structured-review until at least one real corpus
(e.g. Workorders post-gov2gov) has been used to tune thresholds and confirm findings.
.PARAMETER RepoRoot
Optional repo root override; defaults to the current script's repo.
.PARAMETER TargetGlob
File glob relative to repo root. Default '**/*.md'. Excludes listed below are hard-coded.
.PARAMETER ReferenceDate
ISO date used as "now" for staleness calculations. Defaults to today. Parameterized so the
script is deterministic in tests.
.PARAMETER StaleAfterDays
Threshold in days past which a date-anchored live-truth claim is flagged. Default 180.
.PARAMETER ShingleSize
Word-count of the shingle window for duplicate-paragraph detection. Default 8.
.PARAMETER SimilarityThreshold
Jaccard threshold above which two paragraphs in different docs are flagged as duplicate.
Default 0.50.
.PARAMETER MinParagraphWords
Paragraphs shorter than this are skipped for Class 3. Default 20. Prevents headers,
one-line bullets, and boilerplate from producing false positives.
.OUTPUTS
JSON describing status, counts per class, and per-finding detail.
.NOTES
Exit code is 0 on clean, 1 on any finding, matching the existing compliance-script contract.
Until validated against a real corpus, a non-zero exit should be read as "something to
look at" rather than "the migration failed."
#>
param(
  [string]$RepoRoot = '',
  [string]$TargetGlob = '**/*.md',
  [string]$ReferenceDate = '',
  [int]$StaleAfterDays = 180,
  [int]$ShingleSize = 8,
  [double]$SimilarityThreshold = 0.50,
  [int]$MinParagraphWords = 20
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
  $RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..'))
}
else {
  $RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
}

if ([string]::IsNullOrWhiteSpace($ReferenceDate)) {
  $now = (Get-Date).ToUniversalTime().Date
}
else {
  $now = [datetime]::ParseExact($ReferenceDate, 'yyyy-MM-dd', $null).ToUniversalTime().Date
}

$staleCutoff = $now.AddDays(-$StaleAfterDays)

# Excluded directories (generated artifacts, mirrors, noise).
$excludedDirs = @('node_modules', '.git', '.agents', 'logs', '.codex', '.claude', '.tmp')

# Class-3 only: paths where duplicate paragraphs are expected by design.
# Two categories:
#   a) mirrored-by-verifier: enforced byte-for-byte elsewhere (mirror-sync reconciler);
#      flagging them here would double-count an invariant we have already closed.
#   b) generated-from-source: build-script output derived from a tracked source file.
#      These are not authored text, they are derived artifacts.
# Future work: replace this list with a self-declaring header convention
# (e.g. "Generated-From: path/to/source.md") the detector can honor automatically.
$classThreeExcludedPrefixes = @(
  'plugins/anarchy-ai/schemas/',  # mirrored schemas (category a)
  'plugins/anarchy-ai/README.md'  # generated from docs/ANARCHY_AI_PLUGIN_README_SOURCE.md (category b)
)

<#
.SYNOPSIS
Computes a repo-relative forward-slash path for display, PS 5.1 compatible.
.DESCRIPTION
[System.IO.Path]::GetRelativePath is .NET Core+ only; this shim strips the repo-root prefix
from an absolute path and normalizes separators.
.PARAMETER Root
Repo root as returned by GetFullPath.
.PARAMETER AbsolutePath
Absolute file path under the repo.
.OUTPUTS
Forward-slash relative path string.
#>
function Get-RelativePathForward {
  param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$AbsolutePath
  )
  $rootNorm = $Root.TrimEnd('\', '/')
  if ($AbsolutePath.StartsWith($rootNorm, [System.StringComparison]::OrdinalIgnoreCase)) {
    $tail = $AbsolutePath.Substring($rootNorm.Length).TrimStart('\', '/')
    return $tail.Replace('\', '/')
  }
  return $AbsolutePath.Replace('\', '/')
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
.PARAMETER FindingClass
Short finding classification -- 'stale-date-anchor' or 'duplicate-paragraph'.
.PARAMETER Detail
Human-readable explanation of the match.
.PARAMETER Context
Optional additional context (other file for duplicate, excerpt, etc).
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
    [string]$FindingClass,
    [Parameter(Mandatory = $true)]
    [string]$Detail,
    [string]$Context = ''
  )

  $finding = [ordered]@{
    file    = $RelativePath
    class   = $FindingClass
    detail  = $Detail
    context = $Context
  }

  $Findings.Add([pscustomobject]$finding)
}

<#
.SYNOPSIS
Parses a "<Month> <YYYY>" or "Q[1-4] <YYYY>" or "YYYY-MM-DD" date string to a DateTime.
.DESCRIPTION
Returns $null when the string does not match any supported form. Month-year defaults to
the first day of the month; quarter-year defaults to the first day of the quarter.
.PARAMETER DateText
Captured date substring from a regex match.
.OUTPUTS
DateTime or $null.
#>
function Resolve-DateToken {
  param([Parameter(Mandatory = $true)][string]$DateText)

  $trimmed = $DateText.Trim()

  # ISO YYYY-MM-DD
  if ($trimmed -match '^(\d{4})-(\d{2})-(\d{2})$') {
    try { return [datetime]::ParseExact($trimmed, 'yyyy-MM-dd', $null) } catch { return $null }
  }

  # Quarter: Q1 2024, Q2 2025, etc.
  if ($trimmed -match '^Q([1-4])\s+(\d{4})$') {
    $q = [int]$matches[1]; $y = [int]$matches[2]
    $month = (($q - 1) * 3) + 1
    return [datetime]::new($y, $month, 1)
  }

  # Month-name YYYY
  $monthMap = @{
    'january' = 1; 'february' = 2; 'march' = 3; 'april' = 4; 'may' = 5; 'june' = 6
    'july' = 7; 'august' = 8; 'september' = 9; 'october' = 10; 'november' = 11; 'december' = 12
  }
  if ($trimmed -match '^([A-Za-z]+)\s+(\d{4})$') {
    $m = $matches[1].ToLowerInvariant()
    if ($monthMap.ContainsKey($m)) {
      return [datetime]::new([int]$matches[2], $monthMap[$m], 1)
    }
  }

  return $null
}

<#
.SYNOPSIS
Splits markdown content into paragraphs for shingle comparison.
.DESCRIPTION
Strips YAML frontmatter, collapses runs of blank lines, yields non-empty paragraph strings.
Paragraphs are further filtered downstream by minimum word count.
.PARAMETER Content
Raw file content.
.OUTPUTS
String[] of paragraphs.
#>
function Get-Paragraphs {
  param([Parameter(Mandatory = $true)][string]$Content)

  # Strip YAML frontmatter between --- fences if present at start.
  $stripped = $Content -replace '^\s*---[\s\S]*?---\s*\r?\n', ''

  $parts = [regex]::Split($stripped, '\r?\n\s*\r?\n')
  return $parts | ForEach-Object { $_.Trim() } | Where-Object { $_.Length -gt 0 }
}

<#
.SYNOPSIS
Produces the n-gram shingle set for a paragraph.
.DESCRIPTION
Lowercases, strips markdown punctuation decorators, splits on whitespace, emits contiguous
word-windows of size $ShingleSize as hash-joined strings.
.PARAMETER Paragraph
Paragraph text.
.PARAMETER WindowSize
Shingle window size in words.
.OUTPUTS
HashSet[string] of shingles. Empty set when paragraph has fewer than WindowSize words.
#>
function Get-ShingleSet {
  param(
    [Parameter(Mandatory = $true)][string]$Paragraph,
    [Parameter(Mandatory = $true)][int]$WindowSize
  )

  $normalized = $Paragraph.ToLowerInvariant()
  $normalized = $normalized -replace '[*_`#>\[\]()|]', ' '
  $normalized = $normalized -replace '[^\w\s-]', ' '
  $words = ($normalized -split '\s+') | Where-Object { $_.Length -gt 0 }

  $set = New-Object 'System.Collections.Generic.HashSet[string]'
  if ($words.Count -lt $WindowSize) { return $set }

  for ($i = 0; $i -le $words.Count - $WindowSize; $i++) {
    $shingle = ($words[$i..($i + $WindowSize - 1)]) -join ' '
    [void]$set.Add($shingle)
  }
  return $set
}

<#
.SYNOPSIS
Computes Jaccard similarity between two shingle sets.
.PARAMETER A
First shingle set.
.PARAMETER B
Second shingle set.
.OUTPUTS
Double in [0, 1]. Returns 0 when either set is empty.
#>
function Get-JaccardSimilarity {
  param(
    [Parameter(Mandatory = $true)][System.Collections.Generic.HashSet[string]]$A,
    [Parameter(Mandatory = $true)][System.Collections.Generic.HashSet[string]]$B
  )

  if ($A.Count -eq 0 -or $B.Count -eq 0) { return 0.0 }

  $intersection = 0
  foreach ($s in $A) { if ($B.Contains($s)) { $intersection++ } }
  $union = $A.Count + $B.Count - $intersection
  if ($union -eq 0) { return 0.0 }
  return [double]$intersection / [double]$union
}

# ----- Enumerate target files -----

$findings = New-Object System.Collections.Generic.List[object]

$allMdFiles = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter '*.md' -ErrorAction SilentlyContinue
$targetFiles = foreach ($f in $allMdFiles) {
  $rel = Get-RelativePathForward -Root $RepoRoot -AbsolutePath $f.FullName
  $skip = $false
  foreach ($ex in $excludedDirs) {
    if ($rel -like "$ex/*" -or $rel -eq $ex) { $skip = $true; break }
  }
  if (-not $skip) { $f }
}

# ----- Class 1: stale date-anchored live claims -----

# Pattern 1: "as of <date>"
$asOfPattern = '(?i)\bas\s+of\s+((?:(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4})|(?:Q[1-4]\s+\d{4})|(?:\d{4}-\d{2}-\d{2}))\b'

# Pattern 2: "latest <noun> - <date>" or "latest <noun> -- <date>"
$latestPattern = '(?i)\blatest\s+[\w\s-]{1,30}?\s*[-\u2013\u2014]{1,2}\s*((?:(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4})|(?:Q[1-4]\s+\d{4})|(?:\d{4}-\d{2}-\d{2}))\b'

# Pattern 3: promised-future dates that are now in the past: "(by|before|on|deadline) <date>"
$futurePattern = '(?i)\b(?:by|before|on|deadline(?:\s*:)?)\s+((?:(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4})|(?:Q[1-4]\s+\d{4})|(?:\d{4}-\d{2}-\d{2}))\b'

foreach ($file in $targetFiles) {
  $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
  if ([string]::IsNullOrWhiteSpace($content)) { continue }
  $rel = Get-RelativePathForward -Root $RepoRoot -AbsolutePath $file.FullName

  foreach ($m in [regex]::Matches($content, $asOfPattern)) {
    $dateText = $m.Groups[1].Value
    $parsed = Resolve-DateToken -DateText $dateText
    if ($null -ne $parsed -and $parsed -lt $staleCutoff) {
      Add-Finding -Findings $findings -RelativePath $rel -FindingClass 'stale-date-anchor' `
        -Detail ("'as of {0}' older than {1} days vs reference {2:yyyy-MM-dd}" -f $dateText, $StaleAfterDays, $now) `
        -Context $m.Value
    }
  }

  foreach ($m in [regex]::Matches($content, $latestPattern)) {
    $dateText = $m.Groups[1].Value
    $parsed = Resolve-DateToken -DateText $dateText
    if ($null -ne $parsed -and $parsed -lt $staleCutoff) {
      Add-Finding -Findings $findings -RelativePath $rel -FindingClass 'stale-date-anchor' `
        -Detail ("'latest ... -- {0}' older than {1} days vs reference {2:yyyy-MM-dd}" -f $dateText, $StaleAfterDays, $now) `
        -Context $m.Value
    }
  }

  foreach ($m in [regex]::Matches($content, $futurePattern)) {
    $dateText = $m.Groups[1].Value
    $parsed = Resolve-DateToken -DateText $dateText
    # Future-promise pattern: flag when the promised date has now passed.
    if ($null -ne $parsed -and $parsed -lt $now) {
      Add-Finding -Findings $findings -RelativePath $rel -FindingClass 'stale-date-anchor' `
        -Detail ("forward-commitment date {0} has passed vs reference {1:yyyy-MM-dd}" -f $dateText, $now) `
        -Context $m.Value
    }
  }
}

# ----- Class 3: duplicate governance prose across files -----

# Index all qualifying paragraphs with their shingle sets.
$paragraphIndex = New-Object System.Collections.Generic.List[object]

foreach ($file in $targetFiles) {
  $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
  if ([string]::IsNullOrWhiteSpace($content)) { continue }
  $rel = Get-RelativePathForward -Root $RepoRoot -AbsolutePath $file.FullName

  $excludeFromClass3 = $false
  foreach ($prefix in $classThreeExcludedPrefixes) {
    if ($rel.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { $excludeFromClass3 = $true; break }
  }
  if ($excludeFromClass3) { continue }

  $paragraphs = Get-Paragraphs -Content $content
  foreach ($p in $paragraphs) {
    $wordCount = ($p -split '\s+').Count
    if ($wordCount -lt $MinParagraphWords) { continue }

    $shingles = Get-ShingleSet -Paragraph $p -WindowSize $ShingleSize
    if ($shingles.Count -lt 3) { continue }

    $paragraphIndex.Add([pscustomobject]@{
      file     = $rel
      text     = $p
      shingles = $shingles
    })
  }
}

# Pairwise shingle-similarity across different files.
# O(N^2); acceptable at the scale of a single repo's governance markdown.
$reportedPairs = New-Object 'System.Collections.Generic.HashSet[string]'

for ($i = 0; $i -lt $paragraphIndex.Count; $i++) {
  for ($j = $i + 1; $j -lt $paragraphIndex.Count; $j++) {
    $a = $paragraphIndex[$i]
    $b = $paragraphIndex[$j]
    if ($a.file -eq $b.file) { continue }

    $sim = Get-JaccardSimilarity -A $a.shingles -B $b.shingles
    if ($sim -ge $SimilarityThreshold) {
      $sortedFiles = @($a.file, $b.file) | Sort-Object
      $pairKey = ($sortedFiles -join '||') + "||$i||$j"
      if ($reportedPairs.Add($pairKey)) {
        $excerpt = if ($a.text.Length -gt 140) { $a.text.Substring(0, 140) + '...' } else { $a.text }
        Add-Finding -Findings $findings -RelativePath $a.file -FindingClass 'duplicate-paragraph' `
          -Detail ("paragraph shingle Jaccard={0:N2} with {1}" -f $sim, $b.file) `
          -Context $excerpt
      }
    }
  }
}

# ----- Emit result -----

$status = if ($findings.Count -eq 0) { 'clean' } else { 'findings' }
$classCounts = [ordered]@{
  'stale-date-anchor'   = ($findings | Where-Object { $_.class -eq 'stale-date-anchor' }).Count
  'duplicate-paragraph' = ($findings | Where-Object { $_.class -eq 'duplicate-paragraph' }).Count
}

$result = [ordered]@{
  status                = $status
  prototype             = $true
  reference_date        = $now.ToString('yyyy-MM-dd')
  repo_root             = $RepoRoot
  target_glob           = $TargetGlob
  files_scanned         = $targetFiles.Count
  paragraphs_indexed    = $paragraphIndex.Count
  stale_after_days      = $StaleAfterDays
  shingle_size          = $ShingleSize
  similarity_threshold  = $SimilarityThreshold
  min_paragraph_words   = $MinParagraphWords
  class_2_status        = 'tabled -- no mechanical detection; human review also unreliable; see script header'
  finding_count         = $findings.Count
  findings_per_class    = $classCounts
  findings              = $findings
}

$result | ConvertTo-Json -Depth 6

if ($findings.Count -gt 0) { exit 1 } else { exit 0 }
