# Purpose: Read-only snapshot of Claude host registration surfaces for Anarchy-AI Pass 2 baseline evidence.
# Usage: pwsh -File docs/scripts/capture-claude-baseline.ps1 -Label pre-update
#        pwsh -File docs/scripts/capture-claude-baseline.ps1 -Label post-update
# This script does NOT modify any Claude files. It only reads, copies, and hashes.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Label
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$timestamp = Get-Date -Format 'yyyy-MM-ddTHH-mm-ss'
$evidenceRoot = Join-Path $repoRoot "docs/EVIDENCE/claude-baseline/$timestamp-$Label"
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null

Write-Host "Capturing Claude baseline to: $evidenceRoot" -ForegroundColor Cyan

$userProfile = [Environment]::GetFolderPath('UserProfile')
$appData     = [Environment]::GetFolderPath('ApplicationData')
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')

$targets = @(
    @{ Key = 'claude_json';                  Source = Join-Path $userProfile '.claude.json' },
    @{ Key = 'known_marketplaces_json';      Source = Join-Path $userProfile '.claude/plugins/known_marketplaces.json' },
    @{ Key = 'claude_settings_json';         Source = Join-Path $userProfile '.claude/settings.json' },
    @{ Key = 'claude_settings_local_json';   Source = Join-Path $userProfile '.claude/settings.local.json' },
    @{ Key = 'desktop_config_standard';      Source = Join-Path $appData 'Claude/claude_desktop_config.json' },
    @{ Key = 'desktop_config_msix';          Source = Join-Path $localAppData 'Packages/Claude_pzs8sxrjxfjjc/LocalCache/Roaming/Claude/claude_desktop_config.json' },
    @{ Key = 'desktop_config_app_json';      Source = Join-Path $appData 'Claude/config.json' }
)

$manifest = [ordered]@{
    label           = $Label
    timestamp       = $timestamp
    user_profile    = $userProfile
    appdata         = $appData
    local_appdata   = $localAppData
    files           = @{}
    claude_cli      = @{}
}

foreach ($t in $targets) {
    $entry = [ordered]@{
        source_path = $t.Source
        exists      = $false
        size_bytes  = $null
        sha256      = $null
        captured_as = $null
    }
    if (Test-Path $t.Source) {
        $entry.exists = $true
        $info = Get-Item -LiteralPath $t.Source
        $entry.size_bytes = $info.Length
        $entry.sha256 = (Get-FileHash -LiteralPath $t.Source -Algorithm SHA256).Hash
        $copyName = "$($t.Key).json"
        $copyPath = Join-Path $evidenceRoot $copyName
        Copy-Item -LiteralPath $t.Source -Destination $copyPath -Force
        $entry.captured_as = $copyName
        Write-Host "  [captured] $($t.Key): $($info.Length) bytes" -ForegroundColor Green
    } else {
        Write-Host "  [absent]   $($t.Key): $($t.Source)" -ForegroundColor DarkGray
    }
    $manifest.files[$t.Key] = $entry
}

# Claude CLI discovery (read-only, tolerate absence without aborting)
$claudeCmd = Get-Command claude -ErrorAction SilentlyContinue
if ($claudeCmd) {
    $manifest.claude_cli.on_path = $true
    $manifest.claude_cli.where_output = $claudeCmd.Source
    Write-Host "  [found]    claude CLI on PATH" -ForegroundColor Green
    try {
        $claudeVersion = & claude --version 2>&1
        $manifest.claude_cli.version_output = "$claudeVersion"
    } catch {
        $manifest.claude_cli.version_output = "error: $($_.Exception.Message)"
    }
} else {
    $manifest.claude_cli.on_path = $false
    $manifest.claude_cli.where_output = $null
    $manifest.claude_cli.version_output = $null
    Write-Host "  [absent]   claude CLI not on PATH" -ForegroundColor DarkGray
}

# Claude Desktop install detection (registry read, no writes)
$desktopInstall = @{
    store_msix_path_exists    = Test-Path (Join-Path $localAppData 'Packages/Claude_pzs8sxrjxfjjc')
    standard_path_exists      = Test-Path (Join-Path $appData 'Claude')
    uninstall_entries         = @()
}
try {
    $uninstallRoots = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
    )
    foreach ($root in $uninstallRoots) {
        if (Test-Path $root) {
            Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
                $props = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
                if ($props.DisplayName -and $props.DisplayName -match 'Claude') {
                    $desktopInstall.uninstall_entries += [ordered]@{
                        key          = $_.PSPath
                        display_name = $props.DisplayName
                        version      = $props.DisplayVersion
                        publisher    = $props.Publisher
                        install_loc  = $props.InstallLocation
                    }
                }
            }
        }
    }
} catch {
    $desktopInstall.registry_error = $_.Exception.Message
}
$manifest.desktop_install = $desktopInstall

$manifestPath = Join-Path $evidenceRoot 'manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Out-File -FilePath $manifestPath -Encoding utf8

Write-Host ""
Write-Host "Baseline captured." -ForegroundColor Cyan
Write-Host "Evidence directory: $evidenceRoot"
Write-Host "Manifest:           $manifestPath"
Write-Host ""
Write-Host "Next: restart Claude (which will force an update), then re-run with -Label post-update to capture the delta."
