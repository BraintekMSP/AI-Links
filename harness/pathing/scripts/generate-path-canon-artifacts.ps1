<#
.SYNOPSIS
Generates the code and script artifacts that bind Anarchy-AI to the repo-authored path canon.
.DESCRIPTION
Reads the canonical JSON spec and emits synchronized C#, PowerShell, and MSBuild artifacts consumed by setup,
runtime, bootstrap, and publish flows.
.PARAMETER RepoRoot
Optional repo root override; defaults to the current script's repo.
.OUTPUTS
JSON-like PowerShell object describing the source spec and generated artifact paths.
.NOTES
Critical dependencies: harness/pathing/anarchy-path-canon.json, ConvertFrom-Json, and write access to the generated artifact directories.
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

$sourcePath = Join-Path $RepoRoot 'harness\pathing\anarchy-path-canon.json'
$generatedRoot = Join-Path $RepoRoot 'harness\pathing\generated'
$pluginGeneratedRoot = Join-Path $RepoRoot 'plugins\anarchy-ai\pathing'
$generatedCSharpPath = Join-Path $generatedRoot 'GeneratedAnarchyPathCanon.g.cs'
$generatedPsd1Path = Join-Path $generatedRoot 'anarchy-path-canon.generated.psd1'
$generatedPropsPath = Join-Path $generatedRoot 'AnarchyPathCanon.Generated.props'
$pluginGeneratedPsd1Path = Join-Path $pluginGeneratedRoot 'anarchy-path-canon.generated.psd1'

if (-not (Test-Path $sourcePath)) {
  throw "Path canon source not found: $sourcePath"
}

New-Item -ItemType Directory -Path $generatedRoot -Force | Out-Null
New-Item -ItemType Directory -Path $pluginGeneratedRoot -Force | Out-Null

<#
.SYNOPSIS
Converts JSON-derived values into ordered PowerShell hashtables and arrays.
.DESCRIPTION
Normalizes nested PSCustomObject and dictionary content so later emitters can traverse a predictable structure.
.PARAMETER Value
Any JSON-derived value from the path canon source document.
.OUTPUTS
Hashtable, array, scalar, or null mirroring the source value in PowerShell-native form.
.NOTES
Critical dependencies: JSON source structure and recursive traversal of PSCustomObject and IDictionary values.
#>
function ConvertTo-Hashtable {
  param([Parameter(Mandatory = $true)]$Value)

  if ($null -eq $Value) {
    return $null
  }

  if ($Value -is [System.Collections.IDictionary]) {
    $table = [ordered]@{}
    foreach ($key in $Value.Keys) {
      $table[$key] = ConvertTo-Hashtable -Value $Value[$key]
    }
    return $table
  }

  if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
    $items = New-Object System.Collections.ArrayList
    foreach ($item in $Value) {
      [void]$items.Add((ConvertTo-Hashtable -Value $item))
    }
    return @($items)
  }

  if ($Value -is [pscustomobject]) {
    $table = [ordered]@{}
    foreach ($property in $Value.PSObject.Properties) {
      $table[$property.Name] = ConvertTo-Hashtable -Value $property.Value
    }
    return $table
  }

  return $Value
}

$canon = ConvertTo-Hashtable -Value (Get-Content $sourcePath -Raw | ConvertFrom-Json)

<#
.SYNOPSIS
Renders a PowerShell value as a PSD1 literal fragment.
.DESCRIPTION
Serializes scalars, arrays, and dictionaries into valid PSD1 syntax so the generated path canon can be imported later.
.PARAMETER Value
PowerShell-native value to serialize.
.PARAMETER Indent
Current indentation depth used for nested formatting.
.OUTPUTS
System.String. PSD1 literal text.
.NOTES
Critical dependencies: ConvertTo-Hashtable output and the PSD1 syntax expected by Import-PowerShellDataFile.
#>
function Convert-ToPsd1Literal {
  param([Parameter(Mandatory = $true)]$Value, [int]$Indent = 0)

  $pad = ' ' * $Indent
  if ($Value -is [string]) {
    return "'" + $Value.Replace("'", "''") + "'"
  }

  if ($Value -is [bool]) {
    return $(if ($Value) { '$true' } else { '$false' })
  }

  if ($Value -is [System.Collections.IDictionary]) {
    $lines = @('@{')
    foreach ($key in ($Value.Keys | Sort-Object)) {
      $literal = Convert-ToPsd1Literal -Value $Value[$key] -Indent ($Indent + 2)
      $lines += (' ' * ($Indent + 2)) + "$key = $literal"
    }
    $lines += $pad + '}'
    return ($lines -join [Environment]::NewLine)
  }

  if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
    $items = @($Value)
    if ($items.Count -eq 0) {
      return '@()'
    }

    $lines = @('@(')
    foreach ($item in $items) {
      $literal = Convert-ToPsd1Literal -Value $item -Indent ($Indent + 2)
      $lines += (' ' * ($Indent + 2)) + $literal
    }
    $lines += $pad + ')'
    return ($lines -join [Environment]::NewLine)
  }

  return [string]$Value
}

<#
.SYNOPSIS
Converts a path-canon key into a PascalCase C# constant name.
.DESCRIPTION
Strips non-alphanumeric separators and capitalizes each resulting token.
.PARAMETER Name
Original path-canon key name from the source JSON.
.OUTPUTS
System.String. PascalCase constant name.
.NOTES
Critical dependencies: the source key names remaining stable and collision-free after normalization.
#>
function Convert-ToCSharpConstantName {
  param([Parameter(Mandatory = $true)][string]$Name)

  $parts = ($Name -replace '[^A-Za-z0-9]+', ' ').Trim().Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
  return ($parts | ForEach-Object { $_.Substring(0,1).ToUpperInvariant() + $_.Substring(1) }) -join ''
}

$psd1 = @"
@{
  version = '$(($canon.version).ToString())'
  names = $(Convert-ToPsd1Literal -Value $canon.names -Indent 2)
  relative_paths = $(Convert-ToPsd1Literal -Value $canon.relative_paths -Indent 2)
  relative_references = $(Convert-ToPsd1Literal -Value $canon.relative_references -Indent 2)
  arrays = $(Convert-ToPsd1Literal -Value $canon.arrays -Indent 2)
}
"@

Set-Content -Path $generatedPsd1Path -Value $psd1 -Encoding UTF8
Set-Content -Path $pluginGeneratedPsd1Path -Value $psd1 -Encoding UTF8

$csharpLines = New-Object System.Collections.Generic.List[string]
$csharpLines.Add('namespace AnarchyAi.Pathing;')
$csharpLines.Add('')
$csharpLines.Add('internal static class GeneratedAnarchyPathCanon')
$csharpLines.Add('{')
$csharpLines.Add("    public const string Version = ""$($canon.version)"";")

foreach ($sectionName in @('names', 'relative_paths', 'relative_references')) {
  foreach ($entry in ($canon[$sectionName].GetEnumerator() | Sort-Object Name)) {
    $constantName = Convert-ToCSharpConstantName -Name $entry.Name
    $value = $entry.Value.ToString().Replace('\', '\\').Replace('"', '\"')
    $csharpLines.Add("    public const string $constantName = ""$value"";")
  }
}

foreach ($entry in ($canon.arrays.GetEnumerator() | Sort-Object Name)) {
  $constantName = Convert-ToCSharpConstantName -Name $entry.Name
  $values = @($entry.Value) | ForEach-Object { '"' + $_.ToString().Replace('\', '\\').Replace('"', '\"') + '"' }
  $csharpLines.Add('')
  $csharpLines.Add("    public static readonly string[] $constantName =")
  $csharpLines.Add('    [')
  foreach ($value in $values) {
    $csharpLines.Add("        $value,")
  }
  $csharpLines.Add('    ];')
}

$csharpLines.Add('}')
Set-Content -Path $generatedCSharpPath -Value ($csharpLines -join [Environment]::NewLine) -Encoding UTF8

$propsLines = @(
  '<Project>',
  '  <PropertyGroup>',
  "    <AnarchyCanonRepoSourcePluginDirectoryRelativePath>$($canon.relative_paths.repo_source_plugin_directory_relative_path)</AnarchyCanonRepoSourcePluginDirectoryRelativePath>",
  "    <AnarchyCanonPluginPayloadPrefix>SetupPayload/$($canon.relative_paths.repo_source_plugin_directory_relative_path)/</AnarchyCanonPluginPayloadPrefix>",
  "    <AnarchyCanonPortableSchemaPayloadPrefix>SetupPayload/$($canon.relative_paths.portable_schema_payload_directory_relative_path)/</AnarchyCanonPortableSchemaPayloadPrefix>",
  "    <AnarchyCanonBundleAssetsDirectoryRelativePath>$($canon.relative_paths.bundle_assets_directory_relative_path)</AnarchyCanonBundleAssetsDirectoryRelativePath>",
  '  </PropertyGroup>',
  '</Project>'
)
Set-Content -Path $generatedPropsPath -Value ($propsLines -join [Environment]::NewLine) -Encoding UTF8

[pscustomobject]@{
  source = $sourcePath
  generated_csharp = $generatedCSharpPath
  generated_psd1 = $generatedPsd1Path
  plugin_generated_psd1 = $pluginGeneratedPsd1Path
  generated_props = $generatedPropsPath
}
