<#
.SYNOPSIS
Provides a visible top-level entrypoint for the Anarchy-AI retirement helper.
.DESCRIPTION
Finds the real bundled `remove-anarchy-ai.ps1` script from either the repo-authored source plugin
or a repo-local installed plugin bundle and forwards all arguments unchanged. This keeps removal
discoverable from the `plugins/` root without duplicating the retirement logic or widening scope.
.PARAMETER Mode
Selects whether the helper only assesses state, quarantines active surfaces, or quarantines and then removes the quarantined copies.
.PARAMETER RepoRoot
Optional repo root whose repo-local registration and installed bundle surfaces should be inspected.
.PARAMETER UserProfileRoot
Optional user-profile root used to inspect home-local and device-app surfaces.
.PARAMETER Targets
Optional scope list containing repo_local, user_profile, and/or device_app.
.PARAMETER QuarantineRoot
Optional absolute quarantine root.
.PARAMETER ForceRuntimeLockRelease
Requests the force-release runtime helper instead of the safe-release helper before destructive retirement work.
.PARAMETER AllowMixedMarketplaceRewrite
Retained for CLI compatibility with the bundled helper.
.PARAMETER RemainingArguments
Any additional passthrough arguments supported by newer bundled-helper versions.
.OUTPUTS
The same JSON output and exit code returned by the bundled retirement helper.
.NOTES
Critical dependencies: the repo `plugins/` directory layout, the bundled retirement helper path,
and exact candidate-path resolution before invocation.
#>
param(
  [ValidateSet('Assess','Quarantine','Remove')]
  [string]$Mode = 'Assess',
  [string]$RepoRoot = '',
  [string]$UserProfileRoot = '',
  [string[]]$Targets = @(),
  [string]$QuarantineRoot = '',
  [switch]$ForceRuntimeLockRelease,
  [switch]$AllowMixedMarketplaceRewrite,
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$RemainingArguments = @()
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginsRoot = [System.IO.Path]::GetFullPath($scriptDir)
$sourceHelperPath = [System.IO.Path]::GetFullPath((Join-Path $pluginsRoot 'anarchy-ai\scripts\remove-anarchy-ai.ps1'))
$candidatePaths = New-Object System.Collections.Generic.List[string]

if (Test-Path $sourceHelperPath) {
  $candidatePaths.Add($sourceHelperPath)
}

Get-ChildItem -Path $pluginsRoot -Directory -Filter 'anarchy-ai-herringms-*' -ErrorAction SilentlyContinue |
  Sort-Object -Property Name |
  ForEach-Object {
    $candidatePath = [System.IO.Path]::GetFullPath((Join-Path $_.FullName 'scripts\remove-anarchy-ai.ps1'))
    if (Test-Path $candidatePath) {
      $candidatePaths.Add($candidatePath)
    }
  }

$resolvedHelperPath = ''
if ($candidatePaths.Count -eq 1) {
  $resolvedHelperPath = $candidatePaths[0]
}
elseif ($candidatePaths.Count -gt 1 -and $candidatePaths.Contains($sourceHelperPath)) {
  $resolvedHelperPath = $sourceHelperPath
}
elseif ($candidatePaths.Count -gt 1) {
  $candidateList = ($candidatePaths | ForEach-Object { " - $_" }) -join [Environment]::NewLine
  throw "Multiple Anarchy-AI retirement helpers were found under '$pluginsRoot'. Invoke the desired bundled helper directly:`n$candidateList"
}

if ([string]::IsNullOrWhiteSpace($resolvedHelperPath)) {
  throw "No Anarchy-AI retirement helper was found under '$pluginsRoot'. Expected either '$sourceHelperPath' or an installed 'anarchy-ai-herringms-*\\scripts\\remove-anarchy-ai.ps1' bundle."
}

$forwardedParameters = @{
  Mode = $Mode
}

if ($PSBoundParameters.ContainsKey('RepoRoot')) {
  $forwardedParameters['RepoRoot'] = $RepoRoot
}

if ($PSBoundParameters.ContainsKey('UserProfileRoot')) {
  $forwardedParameters['UserProfileRoot'] = $UserProfileRoot
}

if ($PSBoundParameters.ContainsKey('Targets')) {
  $forwardedParameters['Targets'] = $Targets
}

if ($PSBoundParameters.ContainsKey('QuarantineRoot')) {
  $forwardedParameters['QuarantineRoot'] = $QuarantineRoot
}

if ($ForceRuntimeLockRelease.IsPresent) {
  $forwardedParameters['ForceRuntimeLockRelease'] = $true
}

if ($AllowMixedMarketplaceRewrite.IsPresent) {
  $forwardedParameters['AllowMixedMarketplaceRewrite'] = $true
}

& $resolvedHelperPath @forwardedParameters @RemainingArguments
exit $LASTEXITCODE
