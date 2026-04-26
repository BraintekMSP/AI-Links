<#
.SYNOPSIS
Audits active documentation and wrapper surfaces for Anarchy-AI documentation-truth drift.
.DESCRIPTION
Checks current-facing specs, READMEs, and non-generated operational wrappers against the
current namespaced identity and Codex install model, while allowing historical evidence docs
to keep legacy references outside this audit scope.
.PARAMETER RepoRoot
Optional repo root override; defaults to the current script's repo.
.OUTPUTS
JSON describing audit status, finding count, and any missing required snippets or stale active-doc patterns.
.NOTES
Critical dependencies: repo-tracked active docs, current manifest and install-lane truth, and wrapper inline-documentation requirements.
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
    file = $RelativePath
    type = $FindingType
    detail = $Detail
  }

  $Findings.Add([pscustomobject]$finding)
}

$findings = New-Object System.Collections.Generic.List[object]

$activeDocRules = @(
  @{
    path = 'docs/ANARCHY_AI_SETUP_EXE_SPEC.md'
    required = @(
      'anarchy-ai-user-profile',
      '~/.codex/plugins/anarchy-ai',
      'plugins.<entry>.name = anarchy-ai',
      '.mcp.json -> mcpServers -> anarchy-ai'
    )
    forbidden = @(
      '~/.codex/plugins/anarchy-ai-herringms',
      'plugins.<entry>.name = anarchy-ai-herringms',
      '.mcp.json -> mcpServers -> anarchy-ai-herringms'
    )
  },
  @{
    path = 'docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md'
    required = @(
      './plugins/anarchy-ai',
      '~/.codex/plugins/anarchy-ai',
      'plugins.<entry>.name = anarchy-ai',
      'both lanes keep the plugin-local MCP server key stable as `anarchy-ai`'
    )
    forbidden = @(
      '~/.codex/plugins/anarchy-ai-herringms',
      'plugins.<entry>.name = anarchy-ai-herringms',
      'both lanes keep the plugin-local MCP server key stable as `anarchy-ai-herringms`'
    )
  },
  @{
    path = 'docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md'
    required = @(
      '~/.codex/plugins/anarchy-ai'
    )
    forbidden = @(
      '~/.codex/plugins/anarchy-ai-herringms'
    )
  },
  @{
    path = 'docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md'
    required = @(
      '~/.codex/plugins/anarchy-ai',
      'source.path = "./.codex/plugins/anarchy-ai"'
    )
    forbidden = @(
      'source.path = "./.codex/plugins/anarchy-ai-herringms"'
    )
  },
  @{
    path = 'harness/server/README.md'
    required = @(
      '`anarchy-ai`',
      'installed plugin root'
    )
    forbidden = @(
      'anarchy-ai-herringms',
      '`.../AI-Links/plugins/anarchy-ai`'
    )
  },
  @{
    path = 'docs/README_ai_links.md'
    required = @(
      'test-documentation-truth-compliance.ps1',
      'runs the path and documentation-truth audits'
    )
    forbidden = @()
  },
  @{
    path = 'docs/ANARCHY_AI_BUG_REPORTS.md'
    required = @(
      'AA-BUG-036',
      'Capture concrete defects observed during setup'
    )
    forbidden = @()
  },
  @{
    path = 'docs/ANARCHY_AI_PLUGIN_README_SOURCE.md'
    required = @(
      'All carried schema-family artifacts'
    )
    forbidden = @()
  },
  @{
    path = 'plugins/anarchy-ai/README.md'
    required = @(
      '~\.codex\plugins\anarchy-ai',
      'mcp_servers.anarchy-ai',
      './templates/narratives/'
    )
    forbidden = @(
      '~\.codex\plugins\anarchy-ai-herringms',
      'source.path` of `./.codex/plugins/anarchy-ai-herringms`',
      '~\plugins\anarchy-ai',
      '{{REPO_LOCAL_PLUGIN_ROOT}}',
      '{{USER_PROFILE_PLUGIN_ROOT}}'
    )
  },
  @{
    path = 'plugins/anarchy-ai/skills/README.md'
    required = @(
      'chat-history-capture',
      'portable narrative heuristic',
      'workflow method, not a guarantee of enforcement'
    )
    forbidden = @(
      'Both skills',
      'shared across both skills',
      'named, enforceable method'
    )
  },
  @{
    path = 'plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md'
    required = @(
      'complex changes'
    )
    forbidden = @()
  },
  @{
    path = 'harness/contracts/preflight-session.contract.json'
    required = @(
      'ready for complex changes'
    )
    forbidden = @()
  },
  @{
    path = 'plugins/anarchy-ai/contracts/preflight-session.contract.json'
    required = @(
      'ready for complex changes'
    )
    forbidden = @()
  }
)

$mojibakeMarkers = @(
  [string][char]0x00C3,
  [string][char]0x00E2,
  [string][char]0xFFFD
)

foreach ($rule in $activeDocRules) {
  $relativePath = [string]$rule.path
  $absolutePath = Join-Path $RepoRoot ($relativePath.Replace('/', '\'))
  if (-not (Test-Path $absolutePath)) {
    Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'missing_file' -Detail 'Required active documentation file was not found.'
    continue
  }

  $content = Get-Content -Path $absolutePath -Raw -Encoding UTF8

  foreach ($snippet in @($rule.required)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$snippet) -and -not $content.Contains([string]$snippet)) {
      Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'missing_required_snippet' -Detail ("Missing required snippet: {0}" -f $snippet)
    }
  }

  foreach ($snippet in @($rule.forbidden)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$snippet) -and $content.Contains([string]$snippet)) {
      Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'forbidden_stale_snippet' -Detail ("Found stale active-doc snippet: {0}" -f $snippet)
    }
  }

  if ($content.Contains('meaningful governed work')) {
    Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'forbidden_stale_language' -Detail 'Found stale active-surface language: meaningful governed work'
  }

  foreach ($marker in $mojibakeMarkers) {
    if ($content.Contains($marker)) {
      Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'mojibake_marker' -Detail ("Found common mojibake marker: {0}" -f $marker)
    }
  }
}

$wrapperRules = @(
  @{
    path = 'harness/setup/scripts/publish-anarchy-ai-setup.ps1'
    required = @(
      '.SYNOPSIS',
      '.DESCRIPTION',
      'Critical dependencies:'
    )
  },
  @{
    path = 'plugins/anarchy-ai/scripts/start-anarchy-ai.cmd'
    required = @(
      'REM Purpose:',
      'REM Expected input:',
      'REM Expected output:',
      'REM Critical dependencies:'
    )
  }
)

foreach ($rule in $wrapperRules) {
  $relativePath = [string]$rule.path
  $absolutePath = Join-Path $RepoRoot ($relativePath.Replace('/', '\'))
  if (-not (Test-Path $absolutePath)) {
    Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'missing_file' -Detail 'Required wrapper surface was not found.'
    continue
  }

  $content = Get-Content -Path $absolutePath -Raw
  foreach ($snippet in @($rule.required)) {
    if (-not $content.Contains([string]$snippet)) {
      Add-Finding -Findings $findings -RelativePath $relativePath -FindingType 'missing_inline_documentation' -Detail ("Missing inline documentation snippet: {0}" -f $snippet)
    }
  }
}

$status = if ($findings.Count -eq 0) { 'passed' } else { 'failed' }
$findingsArray = @($findings | ForEach-Object { $_ })
$result = [ordered]@{
  audit = 'documentation_truth_compliance'
  repo_root = $RepoRoot
  status = $status
  finding_count = $findings.Count
  findings = [object[]]$findingsArray
}

([pscustomobject]$result) | ConvertTo-Json -Depth 10

if ($findings.Count -gt 0) {
  exit 1
}

exit 0
