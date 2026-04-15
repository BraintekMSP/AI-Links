<#
.SYNOPSIS
Audits tracked repo surfaces for forbidden hard-coded path literals.
.DESCRIPTION
Loads the generated path canon, skips allowlisted files, and fails when live tracked surfaces reintroduce
path strings that should come from the canon instead.
.PARAMETER RepoRoot
Optional repo root override; defaults to the current script's repo.
.OUTPUTS
JSON describing audit status, finding count, and any violating files or patterns.
.NOTES
Critical dependencies: git ls-files, the generated path canon psd1, and the forbidden-pattern allowlist model.
#>
param(
  [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
  $RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..'))
}
else {
  $RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
}

$pathCanonPath = Join-Path $RepoRoot 'harness\pathing\generated\anarchy-path-canon.generated.psd1'
if (-not (Test-Path $pathCanonPath)) {
  throw "Generated path canon artifact not found: $pathCanonPath"
}

$pathCanon = Import-PowerShellDataFile -Path $pathCanonPath

<#
.SYNOPSIS
Converts a glob pattern into a regular expression.
.DESCRIPTION
Supports the allowlist matching used by the path-canon audit.
.PARAMETER Glob
Glob pattern from the path canon allowlist.
.OUTPUTS
System.String. Equivalent regex pattern anchored to the whole relative path.
.NOTES
Critical dependencies: the current allowlist glob vocabulary used in the path canon source.
#>
function Convert-GlobToRegex {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Glob
  )

  $pattern = [regex]::Escape($Glob)
  $pattern = $pattern.Replace('\*\*', '.*')
  $pattern = $pattern.Replace('\*', '[^/]*')
  $pattern = $pattern.Replace('\?', '.')
  return '^' + $pattern + '$'
}

<#
.SYNOPSIS
Determines whether a tracked relative path is exempt from the forbidden-path audit.
.DESCRIPTION
Evaluates the path against each allowlisted glob until a match is found.
.PARAMETER RelativePath
Tracked repo-relative path under audit.
.PARAMETER AllowlistGlobs
Allowlisted glob patterns from the path canon.
.OUTPUTS
System.Boolean. True when the path is allowlisted.
.NOTES
Critical dependencies: Convert-GlobToRegex and the path canon allowlist definitions.
#>
function Test-IsAllowlisted {
  param(
    [Parameter(Mandatory = $true)]
    [string]$RelativePath,
    [Parameter(Mandatory = $true)]
    [string[]]$AllowlistGlobs
  )

  foreach ($glob in $AllowlistGlobs) {
    $regex = Convert-GlobToRegex -Glob $glob
    if ($RelativePath -match $regex) {
      return $true
    }
  }

  return $false
}

$trackedFiles = @(& git -C $RepoRoot ls-files)
if ($LASTEXITCODE -ne 0) {
  throw 'git ls-files failed during path canon audit.'
}

$operationalExtensions = @('.cs', '.ps1', '.psd1', '.json', '.props', '.csproj')
$findings = New-Object System.Collections.Generic.List[object]

foreach ($relativeFile in $trackedFiles) {
  $normalizedRelativeFile = $relativeFile.Replace('\', '/')
  $extension = [System.IO.Path]::GetExtension($normalizedRelativeFile)
  if ($operationalExtensions -notcontains $extension) {
    continue
  }

  if (Test-IsAllowlisted -RelativePath $normalizedRelativeFile -AllowlistGlobs @($pathCanon.arrays.audit_allowlist_globs)) {
    continue
  }

  $absolutePath = Join-Path $RepoRoot ($normalizedRelativeFile.Replace('/', '\'))
  if (-not (Test-Path $absolutePath)) {
    continue
  }

  $content = Get-Content -Path $absolutePath -Raw
  foreach ($pattern in @($pathCanon.arrays.audit_forbidden_path_patterns)) {
    if ($content -match $pattern) {
      $finding = New-Object PSObject
      $finding | Add-Member -NotePropertyName file -NotePropertyValue $normalizedRelativeFile
      $finding | Add-Member -NotePropertyName pattern -NotePropertyValue $pattern
      $findings.Add($finding)
    }
  }
}

$auditStatus = 'failed'
if ($findings.Count -eq 0) {
  $auditStatus = 'passed'
}

$findingsArray = @($findings | ForEach-Object { $_ })

$result = New-Object PSObject
$result | Add-Member -NotePropertyName audit -NotePropertyValue 'path_canon_compliance'
$result | Add-Member -NotePropertyName repo_root -NotePropertyValue $RepoRoot
$result | Add-Member -NotePropertyName status -NotePropertyValue $auditStatus
$result | Add-Member -NotePropertyName finding_count -NotePropertyValue $findings.Count
$result | Add-Member -NotePropertyName findings -NotePropertyValue ([object[]]$findingsArray)

$result | ConvertTo-Json -Depth 10

if ($findings.Count -gt 0) {
  exit 1
}

exit 0
