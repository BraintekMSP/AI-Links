# Anarchy-AI Full Repo Doctrine Audit - 2026-04-26

## Executive Summary

This audit reingested the AI-Links source-authoring repo and compared the newest Anarchy-AI feature work against the repo doctrine, scratchpads, schemas, runtime/setup source, contracts, plugin payload, skills, templates, scripts, generated surfaces, and narrative records.

Overall posture: directionally healthy, with several concrete drift/proof gaps that should be handled as follow-up remediation batches rather than mixed into this read-only audit.

High-confidence alignment points:

- The underlay/runtime lane split is represented in setup UI/source, setup tests, contracts, runtime posture handling, docs, and bug history.
- AA-BUG-036 is implemented in source and plugin contract mirrors: `repo_underlay` no longer requires repo-local marketplace discovery, while `repo_local_runtime` still treats missing marketplace discovery as a real runtime gap.
- Completed GOV2GOV reference mode is represented in contract and runtime semantics: absent root `GOV2GOV-*` packet files are valid after completion, while explicit active mode can materialize them.
- AppData build-artifact relocation is implemented through `Directory.Build.props`, setup build helper output roots, removal-safety fixtures, and documentation/gap-register entries. No repo-local `bin`, `obj`, or `.tmp` artifact directories were found by this audit pass.
- The new `chat-history-capture` skill addresses the observed timestamp-provenance failure mode and mirrors byte-for-byte across plugin, Codex repo-local, and Claude repo-local skill surfaces.
- Mechanical validation was strong: JSON parse, schema mirror sync, documentation truth, path canon, removal safety, `git diff --check`, server tests, and setup tests passed in this audit window.

Main findings requiring follow-up:

| ID | Severity | Class | Short finding |
|---|---|---|---|
| F-01 | High | Runtime/package gap | User-profile source is `0.1.13`, but the only Codex user-profile cache materialized in this session is `0.1.11`. |
| F-02 | Medium-High | Proof gap | `chat-history-capture` is not included in the schema/skill mirror compliance script's canonical skill list. |
| F-03 | Medium | Docs/source drift | `docs/README_ai_links.md` lists `.agents/plugins/marketplace.json`, but the source repo currently has no `.agents` tree. |
| F-04 | Medium | Provenance/docs drift | `docs/CHANGELOG_ai_links.md` records 2026-04-26 feature work under a `2026-04-25` heading. |
| F-05 | Medium | Docs drift | `plugins/anarchy-ai/skills/README.md` still says `Both skills` and `shared across both skills` after adding a third plugin skill. |
| F-06 | Medium-Low | Docs/tooling gap | Mojibake remains in visible docs, and current truth checks do not catch it. |
| F-07 | Low-Medium | Proof caveat | Installed Anarchy runtime output is useful evidence, but not source truth in this authoring repo; final source/cache alignment still needs explicit post-install cache proof for `0.1.13`. |

No automatic fixes were made. The only mutation from this audit is this report file.

## Baseline And Evidence Window

- Repo root: `C:\Users\herri\OneDrive - Braintek LLC\Documents\GitHub\AI-Links`
- Audit report generated: 2026-04-26, local session.
- Requested window: commits from `2026-04-24 00:00` through current `HEAD`, plus current dirty worktree.
- Visible file inventory: `146` files from `rg --files`.
- Git-tracked inventory: `156` paths from `git ls-files`.
- Current branch: `main`, ahead of `origin/main` by 1 commit at audit time.
- Dirty tracked files: 23.
- Untracked files in scope: 4, all related to `chat-history-capture` skill mirrors.
- Latest source plugin manifest version: `0.1.13`.
- Latest schema bundle version: `0.1.6`.
- User-profile installed source observed on disk: `0.1.13`.
- Codex user-profile cache observed on disk: only `0.1.11`.

Recent commit window from `git log --since="2026-04-24 00:00"`:

| Commit | Date | Subject |
|---|---|---|
| d054063 | 2026-04-25T23:06:02-05:00 | Align gap register with AppData artifact relocation |
| d2b685c | 2026-04-25T23:00:45-05:00 | Harden Anarchy setup lifecycle hygiene |
| 7abd44f | 2026-04-25T20:25:32-05:00 | Record underlay smoke evidence |
| 69faf1e | 2026-04-25T20:17:52-05:00 | Repair Anarchy Codex lane selection |
| 4bc238b | 2026-04-25T19:29:20-05:00 | Separate underlay and runtime install lanes |
| 144eba5 | 2026-04-25T17:20:49-05:00 | Trace Codex materialization state |
| 0106b94 | 2026-04-25T16:25:55-05:00 | Harden Anarchy install lifecycle and gov2gov arcs |
| ef09e07 | 2026-04-25T10:44:12-05:00 | Record Codex cache evidence caveat |
| 773ace1 | 2026-04-25T10:18:43-05:00 | Record underlay class boundary |
| 2ad56db | 2026-04-25T09:20:37-05:00 | Record AI-Links authoring boundary |
| 555e4d7 | 2026-04-25T09:11:20-05:00 | Record trust posture continuity |
| 3c55bcb | 2026-04-25T09:05:33-05:00 | Record work-path adoption doctrine |
| b2e6e7a | 2026-04-25T08:24:00-05:00 | Prefer public schema source by default |
| 6d67131 | 2026-04-25T08:17:50-05:00 | Record schema comparison source preference |
| 4094fd6 | 2026-04-25T08:16:59-05:00 | Document setup distribution caveats |
| db3aa0b | 2026-04-25T08:06:58-05:00 | Record deployable smoke proof and checklist |
| ed6aaba | 2026-04-25T08:05:30-05:00 | Record stale runtime packaging fix |
| 184a3a9 | 2026-04-25T08:02:53-05:00 | Package setup from a staged runtime payload |
| d12762e | 2026-04-24T07:38:09-05:00 | Add review commits workflow skill |
| 1306e2c | 2026-04-24T07:37:53-05:00 | Align ECC narrative terminology |
| 4ad5e72 | 2026-04-24T07:28:14-05:00 | Record harness boundary narrative |
| 621a2aa | 2026-04-24T07:27:55-05:00 | Remove repo-specific commit gate |
| c2f7c97 | 2026-04-24T07:27:21-05:00 | Align harness lifecycle doctrine |
| 9b0e48c | 2026-04-24T07:26:49-05:00 | Add setup lifecycle status records |
| 8707345 | 2026-04-24T07:26:14-05:00 | Add real commit workflow skill |

Current dirty tracked files at audit time:

- `branding/branding-canon.json`
- `docs/ANARCHY_AI_BUG_REPORTS.md`
- `docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md`
- `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md`
- `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md`
- `docs/ANARCHY_AI_SETUP_EXE_SPEC.md`
- `docs/CHANGELOG_ai_links.md`
- `docs/README_ai_links.md`
- `harness/branding/generated/AnarchyBranding.Generated.props`
- `harness/branding/generated/GeneratedAnarchyBranding.g.cs`
- `harness/branding/generated/anarchy-branding.generated.psd1`
- `harness/contracts/gov2gov-migration.contract.json`
- `harness/contracts/schema-reality.contract.json`
- `harness/server/dotnet/Program.cs`
- `harness/server/tests/RuntimeEnvelopeTests.cs`
- `plugins/anarchy-ai/.codex-plugin/plugin.json`
- `plugins/anarchy-ai/README.md`
- `plugins/anarchy-ai/branding/anarchy-branding.generated.psd1`
- `plugins/anarchy-ai/contracts/gov2gov-migration.contract.json`
- `plugins/anarchy-ai/contracts/schema-reality.contract.json`
- `plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe`
- `plugins/anarchy-ai/skills/README.md`
- `plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md`

Current untracked files at audit time:

- `.claude/skills/chat-history-capture/SKILL.md`
- `.codex/skills/chat-history-capture/SKILL.md`
- `.codex/skills/chat-history-capture/agents/openai.yaml`
- `plugins/anarchy-ai/skills/chat-history-capture/SKILL.md`

## Doctrine Sources Reingested

Startup truth chain:

- `AGENTS.md`
- `docs/README_ai_links.md`
- `docs/TODO_ai_links.md`
- `docs/CHANGELOG_ai_links.md`

Core doctrine and vision:

- `docs/AI_COLLAB_STARTUP_PROMPT.md`
- `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md`
- `docs/VISION_anarchy_ai_harness_core.md`
- `docs/VISION_anarchy_ai_delivery_and_access.md`
- `docs/VISION_REGISTER_MODEL.md`
- `docs/VISION_negation_context_span_verbatim.md`
- `docs/STARTUP_CONTEXT_REFACTOR_GUIDE.md`
- `docs/STARTUP_CONTEXT_BUDGET_MODEL.md`
- `docs/CONTROL_PLANE_AGENT_PROMPT_MODEL.md`
- `docs/PROGRESS_OVER_PATCHING_MODEL.md`
- `docs/REPO_2_5_READINESS_MODEL.md`
- `docs/CROSS_REPO_CONTRACT_MODEL.md`
- `docs/SUBAGENT_SAFETY_MODEL.md`
- `docs/DOCUMENTATION_CLEANUP_METHOD.md`
- `docs/PUBLICATION_CHECKLIST.md`

Scratchpads and research pressure:

- `docs/SCRATCHPAD_prompt_efficacy_patterns.md`
- `docs/SCRATCHPAD_prophecy_precontext_influence.md`
- `docs/SAMPLES_prophecy_precontext_influence_2026-04-10.md`
- `docs/ASSUMPTION_FAILURE_PEN_TEST.md`

Schema, runtime, setup, contracts, plugin, skills, scripts, templates, generated surfaces, and narrative files were mapped in the appendix.

Doctrine signals used for comparison:

- AI-Links is the source-authoring repo; human owner remains the operative harness for source intent and promotion decisions.
- Runtime output is evidence and assistance, not source truth for source-authoring judgment.
- Underlay influences and shapes terrain; it does not claim hard host enforcement.
- Repo travel should prefer underlay; runtime should prefer user-profile; repo-local runtime is proving/debug.
- Helper outputs must not claim identity or downstream state facts unless they observe the authoritative source for that fact.
- Build/cache/scratch output should live in AppData or other non-synced lanes, not OneDrive-backed repo trees.
- Narrative/Arc provenance matters; date/time fields must not be fabricated from capture time.

## Newest Feature Inventory

### Underlay versus runtime lane split

Feature state: aligned.

Evidence:

- `harness/setup/dotnet/Program.cs:390-406` exposes GUI choices as `Repo underlay` and `User-profile install`.
- `harness/setup/dotnet/Program.cs:943-951` describes user-profile as normal runtime lane and repo underlay as schema/narrative/hygiene without runtime or host config.
- `harness/setup/dotnet/Program.cs:1632-1652` underlay disclosure explicitly says no `plugins/anarchy-ai`, no `.agents/plugins/marketplace.json`, no host config, no runtime process.
- `harness/setup/tests/SetupEngineTests.cs:148-157` validates the repo-underlay disclosure and CLI-only proving/debug lane language.
- `docs/CHANGELOG_ai_links.md:36-37` records AA-BUG-036 and posture semantics.

Doctrine comparison: satisfies underlay-as-influence, not runtime or enforcement; satisfies normal repo travel as underlay and user-profile runtime as the normal plugin lane.

### User-profile runtime and cache provenance

Feature state: directionally aligned but proof gap remains.

Evidence:

- `plugins/anarchy-ai/.codex-plugin/plugin.json:3` reports source plugin `0.1.13`.
- `branding/branding-canon.json:15` reports canonical `plugin_manifest_version` `0.1.13`.
- `C:\Users\herri\.codex\plugins\anarchy-ai\.codex-plugin\plugin.json:3` reports installed user-profile source `0.1.13`.
- `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.11\.codex-plugin\plugin.json:3` reports the only materialized Codex cache as `0.1.11`.
- `C:\Users\herri\.agents\plugins\marketplace.json` points user-profile marketplace source to `./.codex/plugins/anarchy-ai`.

Doctrine comparison: source truth and installed-source provenance are coherent, but active Codex cache/runtime tool materialization is not yet proven for `0.1.13`.

### AA-BUG-036 workspace posture and GOV2GOV reference mode

Feature state: aligned.

Evidence:

- `harness/contracts/schema-reality.contract.json:22-31` defines `workspace_posture` and says `repo_underlay` does not require repo-local plugin marketplace discovery.
- `harness/contracts/gov2gov-migration.contract.json:46-65` defines `workspace_posture` plus `gov2gov_artifact_mode` and says absent root `GOV2GOV-*` files mean reference mode.
- `plugins/anarchy-ai/contracts/schema-reality.contract.json:22-31` mirrors the schema-reality contract.
- `plugins/anarchy-ai/contracts/gov2gov-migration.contract.json:46-65` mirrors the gov2gov contract.
- `harness/server/tests/RuntimeEnvelopeTests.cs:245-304` validates underlay ignores missing repo marketplace while repo-local runtime still flags it.
- `harness/server/tests/RuntimeEnvelopeTests.cs:308-386` validates reference mode does not materialize GOV2GOV packets and explicit active mode can materialize them without marketplace restoration.

Doctrine comparison: satisfies underlay/reference mode product direction and the Workorders remediation lesson.

### AppData build-artifact relocation

Feature state: aligned.

Evidence:

- `Directory.Build.props:3-12` redirects default .NET build/test intermediates and outputs into `%LOCALAPPDATA%\Anarchy-AI\AI-Links\dotnet` unless explicitly opted into repo-local artifacts.
- `harness/setup/scripts/build-self-contained-exe.ps1:664-672` uses `%LOCALAPPDATA%\Anarchy-AI\AI-Links\setup-build` for setup/server publish scratch and payload staging.
- `docs/ANARCHY_AI_BUG_REPORTS.md:429-447` records AA-BUG-026 and acceptance criteria for non-workspace SDK/build/test lanes.
- `docs/IMPLEMENTATION_GAP_REGISTER.md:878-889` records the AppData relocation implementation.
- Audit scan found no repo-local `bin`, `obj`, or `.tmp` directories outside `.git`.

Doctrine comparison: satisfies the OneDrive safety doctrine and the explicit user direction to stop dumping .NET build artifacts into the repo.

### `chat-history-capture` skill and timestamp provenance

Feature state: behavior direction aligned, proof guard incomplete.

Evidence:

- `plugins/anarchy-ai/skills/chat-history-capture/SKILL.md` exists and contains the archival timestamp rules.
- `.codex/skills/chat-history-capture/SKILL.md` exists as AI-Links repo-local Codex mirror.
- `.claude/skills/chat-history-capture/SKILL.md` exists as AI-Links Claude mirror.
- All three skill files hash-match: `D1B50B983926F0C9DA096A95DAD60E06264C6A370C90065BD78235A19F7F799D`.
- `docs/CHANGELOG_ai_links.md:38-39` records the addition and tightening through `0.1.13`.

Doctrine comparison: satisfies the Arc/provenance doctrine directionally, but does not yet have the same mirror compliance guard as the existing structured skills.

### Plugin versioning/cache invalidation through `0.1.13`

Feature state: source identity aligned, host cache still stale.

Evidence:

- `branding/branding-canon.json:15` has `plugin_manifest_version` `0.1.13`.
- `harness/branding/generated/GeneratedAnarchyBranding.g.cs:15` has `PluginManifestVersion = "0.1.13"`.
- `plugins/anarchy-ai/.codex-plugin/plugin.json:3` has `version` `0.1.13`.
- Installed user-profile source also has `0.1.13`.
- Codex user-profile cache only has `0.1.11` in this session.

Doctrine comparison: satisfies identity-source doctrine in repo canon, but needs downstream host/cache materialization proof before agents rely on the active tool lane as `0.1.13`.

## Doctrine Alignment Findings

### F-01 - High - Runtime/package gap - Codex cache has not materialized `0.1.13`

Affected feature or surface: plugin versioning, user-profile runtime lane, cache invalidation.

Doctrine/scratchpad source:

- `AGENTS.md` says source truth belongs in the repo and disposable cache/runtime scratch does not.
- `docs/VISION_anarchy_ai_delivery_and_access.md` says full adoption is more than installation and requires runtime, registration, callability, schema bundle, startup surfaces, instructions, and bounded gap assessment.
- `narratives/projects/ai-links.json` bad patterns include helper claims about identity/state facts they do not authoritatively own.

Direct file evidence:

- `plugins/anarchy-ai/.codex-plugin/plugin.json:3` source plugin version is `0.1.13`.
- `branding/branding-canon.json:15` source canonical plugin manifest version is `0.1.13`.
- `C:\Users\herri\.codex\plugins\anarchy-ai\.codex-plugin\plugin.json:3` installed user-profile source is `0.1.13`.
- `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.11\.codex-plugin\plugin.json:3` cache materialized version is `0.1.11`.
- No `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.1.13` directory existed during this audit.

Classification: runtime/package gap and proof gap.

Assessment: source packaging identity is correct, installed source identity is correct, but active host cache materialization is not yet proven for `0.1.13` and is observably stale to `0.1.11` in this session.

Recommended remediation:

- After current source changes are finalized, run the user-profile installer and restart/reindex Codex.
- Add a bounded status/check that reports source version, installed-source version, marketplace source path, cache versions, and active callable skill/tool version in one place.
- Do not claim active MCP/skill behavior is `0.1.13` until cache materialization or callable-lane proof observes it.

### F-02 - Medium-High - Proof gap - `chat-history-capture` is not in mirror compliance canonical skills

Affected feature or surface: chat-history-capture skill, skill mirror validation.

Doctrine/scratchpad source:

- `AGENTS.md` says source truth belongs in repo and helper outputs must be backed by the authoritative surface.
- `docs/ASSUMPTION_FAILURE_PEN_TEST.md` flags distribution versus realtime copy drift as a high-priority failure class.
- `narratives/projects/ai-links.json` records build helpers and scripts must not silently become alternate authorities for identity/state facts.

Direct file evidence:

- `docs/scripts/test-schema-mirror-sync-compliance.ps1:212-214` lists only `structured-commit/SKILL.md` and `structured-review/SKILL.md` in `$canonicalSkills`.
- `plugins/anarchy-ai/skills/chat-history-capture/SKILL.md` exists.
- `.codex/skills/chat-history-capture/SKILL.md` exists.
- `.claude/skills/chat-history-capture/SKILL.md` exists.
- Manual hash check shows all three capture skill copies match, but this proof is not automated by the compliance script.

Classification: proof gap.

Assessment: current files are aligned now, but the guardrail does not protect the new skill from future drift. The compliance script also documents only plugin-to-`.claude` mirror checks; it does not cover the new `.codex` mirror shape.

Recommended remediation:

- Add `chat-history-capture/SKILL.md` to the canonical skill mirror list.
- Decide whether `.codex/skills/chat-history-capture/SKILL.md` should be part of the same compliance contract or a separate Codex-skill mirror contract.
- Consider replacing the hard-coded skill list with a small skill mirror manifest so adding a skill cannot bypass proof accidentally.

### F-03 - Medium - Docs/source drift - README lists missing `.agents/plugins/marketplace.json`

Affected feature or surface: plugin delivery documentation, marketplace source-truth claims.

Doctrine/scratchpad source:

- `AGENTS.md` says when reporting ingest results, name what was actually loaded and do not imply a full state that was not observed.
- Current underlay/runtime doctrine says repo-local marketplace is not normal repo-travel truth.
- AA-BUG-036 says underlay-only repos should not be forced to restore `.agents/plugins/marketplace.json`.

Direct file evidence:

- `docs/README_ai_links.md:114` lists `../.agents/plugins/marketplace.json` as a repo-local plugin marketplace entry.
- `Test-Path .agents` returned `False` during audit.
- `Test-Path .agents\plugins\marketplace.json` returned `False` during audit.

Classification: docs/source drift and product-doctrine mismatch risk.

Assessment: if AI-Links source is intentionally not carrying repo-local marketplace metadata, README should not present that file as a current source surface. If AI-Links source should carry repo-local plugin marketplace metadata, then the file is missing. Given the current doctrine, absence is probably correct and the README wording is likely stale.

Recommended remediation:

- Update README to distinguish user-profile marketplace evidence from repo-local marketplace metadata.
- If a repo-local marketplace entry remains useful for AI-Links development, state it as optional/dev-local evidence rather than required repo truth.

### F-04 - Medium - Provenance/docs drift - 2026-04-26 work is under a 2026-04-25 changelog heading

Affected feature or surface: changelog provenance, feature-date accuracy.

Doctrine/scratchpad source:

- The new `chat-history-capture` skill explicitly treats timestamp provenance as load-bearing.
- Arc doctrine treats rationale timing as part of why decisions remain auditable.
- `AGENTS.md` requires clarity about what was actually loaded or changed.

Direct file evidence:

- `docs/CHANGELOG_ai_links.md:4` starts the active section as `## 2026-04-25`.
- `docs/CHANGELOG_ai_links.md:36-39` records AA-BUG-036, `chat-history-capture`, and `0.1.13` work that occurred during the 2026-04-26 session context.

Classification: docs drift and provenance drift.

Assessment: the changelog is otherwise capturing the right feature content, but the heading can misattribute when rationale and remediation were formed.

Recommended remediation:

- Split 2026-04-26 bullets into a `## 2026-04-26` section, or add explicit subheadings with exact decision/proof dates.
- Keep historical April 25 material under April 25.

### F-05 - Medium - Docs drift - Skills README still says `Both skills`

Affected feature or surface: Anarchy skill package documentation.

Doctrine/scratchpad source:

- `docs/README_ai_links.md` now lists `chat-history-capture` as part of plugin delivery.
- `plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md` now includes a companion-skill pointer to `chat-history-capture`.
- Product doctrine says documentation should not create false confidence about what exists.

Direct file evidence:

- `plugins/anarchy-ai/skills/README.md:17-20` documents `chat-history-capture`.
- `plugins/anarchy-ai/skills/README.md:24` still says `Both skills apply the same discipline`.
- `plugins/anarchy-ai/skills/README.md:26` still says `shared across both skills`.

Classification: docs drift.

Assessment: the README was partially updated for the new skill but not fully generalized. This is not a runtime blocker, but it is a direct source-package documentation drift.

Recommended remediation:

- Replace `Both skills` and `both skills` wording with `These skills` or split structured-code skills from governance-memory skill behavior.
- Clarify which verification checks apply to `structured-commit`/`structured-review` versus `chat-history-capture`.

### F-06 - Medium-Low - Docs/tooling gap - Mojibake remains and is not guarded

Affected feature or surface: human-facing docs and plugin skill README.

Doctrine/scratchpad source:

- Careful language is product behavior in `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md`.
- `AGENTS.md` says templates and docs should stay copy-friendly.

Direct file evidence:

- `docs/README_ai_links.md:7` contains mojibake in the em dash representation.
- `plugins/anarchy-ai/skills/README.md:3`, `24`, `33`, `35`, and `47` contain mojibake.
- `docs/ANARCHY_AI_BUG_REPORTS.md` contains multiple mojibake instances in quoted language sections.

Classification: docs drift and tooling gap.

Assessment: this does not break runtime behavior, but it weakens copy quality and credibility in a repo where language precision is load-bearing. Current documentation truth checks do not catch this class.

Recommended remediation:

- Run a bounded encoding/mojibake cleanup batch across docs and plugin docs.
- Add a lightweight doc hygiene check for common mojibake sequences if that does not create too much noise.

### F-07 - Low-Medium - Proof caveat - Runtime evidence remains evidence, not source truth

Affected feature or surface: audit method and harness output interpretation.

Doctrine/scratchpad source:

- `AGENTS.md` section 2.5 says the installed harness output is evidence and assistance, not final authority over source intent.
- `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md:32-33` says AI-Links is a source-authoring workspace and this boundary must not be published into consuming repos.

Direct file evidence:

- `harness/contracts/schema-reality.contract.json:156` and plugin mirror state that a callable user-profile harness is valid installed runtime but not source truth for the evaluated workspace.
- Preflight reported `schema_authoring_and_plugin_delivery_workspace` and `user_profile_installed_runtime` for this audit context.

Classification: proof caveat, not a source defect.

Assessment: this audit used Anarchy-AI preflight/final capture as requested and did not treat the installed MCP runtime as authoritative over source doctrine.

Recommended remediation:

- Keep this caveat in future source-authoring audit templates.
- Add a source-authoring audit checklist item: compare source manifest, installed source, marketplace path, Codex cache, and callable tool metadata separately.

## Scratchpad Alignment Findings

### Satisfied: underlay as influence, not enforcement

Scratchpad source:

- `docs/SCRATCHPAD_prophecy_precontext_influence.md:79-100` names underlay as a layer that biases what becomes thinkable, not a runtime or enforcement layer.
- `docs/SCRATCHPAD_prophecy_precontext_influence.md:149-187` argues for contract instead of enforcement and intentional non-enforcement.

Feature comparison:

- Setup GUI exposes `Repo underlay` and `User-profile install` only.
- Underlay disclosure states no runtime, no marketplace, no host config.
- AA-BUG-036 prevents GOV2GOV underlay from requiring runtime discovery.

Result: satisfied.

### Satisfied: source truth and helper authority boundaries

Scratchpad/Arc source:

- `narratives/projects/ai-links.json` observed bad patterns record helper-as-authority failures for identity and downstream state facts.
- `docs/ASSUMPTION_FAILURE_PEN_TEST.md` flags distribution versus realtime copy drift.

Feature comparison:

- Branding canon now carries `plugin_manifest_version`.
- Generated branding and plugin manifest reflect `0.1.13`.
- Runtime provenance language states user-profile installed runtime is not source truth.

Result: mostly satisfied, with F-01 and F-02 remaining as proof gaps.

### Satisfied: provenance pressure drove a skill rather than loose advice

Scratchpad/Arc source:

- Arc decisions establish narrative as the durable home for provenance.
- The new chat-history capture prompt exists because multiple agents misattributed decision dates.

Feature comparison:

- `chat-history-capture` requires decision date recovery from message/session provenance, not capture time.
- It asks to show entries before writing when timestamp audit is requested.
- It rejects stale hardcoded Codex cache skill paths as authority.

Result: satisfied in behavior design, but not yet fully guarded by mirror compliance.

### Satisfied: AppData relocation follows workspace-sprawl doctrine

Scratchpad/source source:

- `AGENTS.md` says disposable cache/runtime scratch belongs outside the repo.
- User direction specifically rejected moving repos out of OneDrive and asked to move build artifacts into AppData.

Feature comparison:

- `Directory.Build.props` and setup build script redirect normal .NET outputs into AppData.
- Audit found no repo-local `bin`, `obj`, or `.tmp` directories.

Result: satisfied.

## Proof And Environment Gaps

Mechanical checks run during this audit window:

| Check | Result |
|---|---|
| Anarchy-AI preflight | Ready; role detected as source-authoring/plugin-delivery; user-profile runtime evidence only |
| `rg --files` inventory | 146 visible repo files |
| JSON parse over `*.json` | 34 JSON files parsed, 0 failures |
| Schema mirror sync compliance | Passed, 0 findings |
| Documentation truth compliance | Passed, 0 findings |
| Path canon compliance | Passed, 0 findings |
| Removal safety compliance | Passed |
| `git diff --check` | Passed; only CRLF warnings from Git |
| MCP server tests | Passed 22/22 |
| setup tests | Passed 41/41 |
| repo-local build artifact scan | No repo-local `bin`, `obj`, or `.tmp` directories found |

Proof gaps that remain:

- Compliance scripts pass while missing the new `chat-history-capture` skill from canonical skill mirror coverage.
- Documentation truth checks pass while README refers to missing `.agents/plugins/marketplace.json` and visible mojibake remains.
- User-profile installed source is `0.1.13`, but Codex cache materialization is only `0.1.11` in this session.

## Remediation Disposition

The `.2` remediation plan supersedes the `0.1.13` proof target from this audit. The package is moving directly to `0.2.0` because the follow-up changes affect doctrine, portable heuristics, harness output contracts, mirror validation, and generated payload identity.

Disposition by finding:

- `F-01`: reconcile through `0.2.0` user-profile install and cache proof, not `0.1.13` proof.
- `F-02`: promote `chat-history-capture` into the portable narrative heuristic layer while keeping the plugin skill as the richer execution/checking lane.
- `F-03`: relabel AI-Links source README so `.agents/plugins/marketplace.json` is not treated as repo truth in source/underlay posture.
- `F-04`: preserve 2026-04-25 changelog entries unless a specific post-midnight decision timestamp is proven.
- `F-05`: rephrase the skills README so structured-code skills and chat-history capture are not collapsed into unsupported "both skills" wording or enforcement claims.
- `F-06`: treat mojibake as unintended encoding corruption in active docs/readmes and add an active-doc marker check.
- `F-07`: keep research/proof evidence in AI-Links source, but do not publish source-authoring research posture into the plugin payload.

Post-remediation proof note: the rebuilt setup installed user-profile source `0.2.0` with schema bundle `0.2.0`; Codex cache materialization remained host-owned and still showed only `0.1.11` in this running session.

## Packaging And Runtime Drift

Current package/source state:

| Surface | Observed state |
|---|---|
| `branding/branding-canon.json` | `plugin_manifest_version` `0.1.13` |
| `harness/branding/generated/GeneratedAnarchyBranding.g.cs` | `PluginManifestVersion = "0.1.13"` |
| `plugins/anarchy-ai/.codex-plugin/plugin.json` | `version` `0.1.13` |
| `plugins/anarchy-ai/schemas/schema-bundle.manifest.json` | `bundle_version` `0.1.6` |
| user-profile installed source | `0.1.13` |
| user-profile Codex cache | only `0.1.11` materialized |
| plugin runtime binary | tracked binary modified in current dirty tree |
| setup EXE | ignored by `.gitignore`; not part of source truth |

Assessment:

- Source package identity is coherent.
- Installed source identity is coherent.
- Host cache identity is stale.
- This is the same evidence class as prior Codex materialization gaps: source, installed bundle, marketplace path, cache entry, and callable runtime must be observed separately.

## Recommended Remediation Batches

1. Runtime/cache proof batch

- Reinstall user-profile from the rebuilt source setup executable after the current worktree is ready.
- Restart/reindex Codex.
- Verify `C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai\0.2.0` exists.
- Verify callable Anarchy skill/tool metadata resolves to the intended version or explicitly records if Codex exposes older metadata.
- Do not bundle this proof with source changes unless the proof exposes a source bug.

2. Skill mirror proof batch

- Add `chat-history-capture/SKILL.md` to `docs/scripts/test-schema-mirror-sync-compliance.ps1` canonical skill coverage.
- Decide whether `.codex/skills/chat-history-capture/SKILL.md` belongs in the same proof or a new Codex mirror proof.
- Consider adding a skill mirror manifest to avoid future hard-coded omissions.

3. Documentation provenance cleanup batch

- Split `docs/CHANGELOG_ai_links.md` so 2026-04-26 work sits under a 2026-04-26 heading.
- Keep historical April 25 entries under April 25.
- Preserve exact commit times where they are known.

4. Plugin skill README cleanup batch

- Replace stale `Both skills`/`both skills` wording.
- Clarify which shared philosophy applies to all skills and which four verification checks apply only to structured commit/review.
- Clean mojibake in the same file or in a separate encoding batch.

5. README marketplace truth batch

- Decide whether `.agents/plugins/marketplace.json` is meant to exist in AI-Links source.
- If yes, restore it intentionally and guard it.
- If no, update `docs/README_ai_links.md` to avoid presenting it as a current source file.

6. Mojibake/document hygiene batch

- Sweep visible docs and plugin docs for mojibake sequences.
- Add a lightweight compliance check for common mojibake markers if the false-positive rate is acceptable.

7. Audit-template hardening batch

- Promote the method used here into a reusable source-authoring audit checklist.
- Include separate columns for source repo, installed source, marketplace, cache, and callable runtime.
- Keep the AI-Links source-authoring boundary explicit and non-portable.

## Appendix: Files Audited And Commands Run

### File Category Counts

| Category | Count |
|---|---:|
| contracts | 18 |
| docs_other | 8 |
| doctrine | 22 |
| generated_branding_build | 6 |
| harness_setup_source | 28 |
| narrative_history_evidence | 4 |
| plugin_payload | 20 |
| schema_family | 11 |
| scratchpad_research | 4 |
| scripts | 13 |
| skills | 5 |
| templates | 7 |

### File Map

| Category | File |
|---|---|
| contracts | harness/contracts/active-work-state.contract.json |
| contracts | harness/contracts/declare-proof-requirement.contract.json |
| contracts | harness/contracts/direction-assist-test.contract.json |
| contracts | harness/contracts/gov2gov-migration.contract.json |
| contracts | harness/contracts/harness-gap-state.contract.json |
| contracts | harness/contracts/preflight-session.contract.json |
| contracts | harness/contracts/schema-reality.contract.json |
| contracts | harness/contracts/validate-exit-readiness.contract.json |
| contracts | harness/contracts/verify-config-materialization.contract.json |
| contracts | plugins/anarchy-ai/contracts/active-work-state.contract.json |
| contracts | plugins/anarchy-ai/contracts/declare-proof-requirement.contract.json |
| contracts | plugins/anarchy-ai/contracts/direction-assist-test.contract.json |
| contracts | plugins/anarchy-ai/contracts/gov2gov-migration.contract.json |
| contracts | plugins/anarchy-ai/contracts/harness-gap-state.contract.json |
| contracts | plugins/anarchy-ai/contracts/preflight-session.contract.json |
| contracts | plugins/anarchy-ai/contracts/schema-reality.contract.json |
| contracts | plugins/anarchy-ai/contracts/validate-exit-readiness.contract.json |
| contracts | plugins/anarchy-ai/contracts/verify-config-materialization.contract.json |
| docs_other | CONTRIBUTING.md |
| docs_other | docs/ANARCHY_AI_DOC_DISCOVERY_BUG_REGISTER.md |
| docs_other | docs/ANARCHY_AI_PLUGIN_README_SOURCE.md |
| docs_other | docs/CHANGE_NOTES_governance_linguistic_audit.md |
| docs_other | LICENSE |
| docs_other | NOTICE |
| docs_other | README.md |
| docs_other | SECURITY.md |
| doctrine | AGENTS.md |
| doctrine | docs/AI_COLLAB_STARTUP_PROMPT.md |
| doctrine | docs/ANARCHY_AI_BUG_REPORTS.md |
| doctrine | docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md |
| doctrine | docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md |
| doctrine | docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md |
| doctrine | docs/ANARCHY_AI_SETUP_EXE_SPEC.md |
| doctrine | docs/CONTROL_PLANE_AGENT_PROMPT_MODEL.md |
| doctrine | docs/CROSS_REPO_CONTRACT_MODEL.md |
| doctrine | docs/DOCUMENTATION_CLEANUP_METHOD.md |
| doctrine | docs/IMPLEMENTATION_GAP_REGISTER.md |
| doctrine | docs/PROGRESS_OVER_PATCHING_MODEL.md |
| doctrine | docs/PUBLICATION_CHECKLIST.md |
| doctrine | docs/README_ai_links.md |
| doctrine | docs/REPO_2_5_READINESS_MODEL.md |
| doctrine | docs/STARTUP_CONTEXT_BUDGET_MODEL.md |
| doctrine | docs/STARTUP_CONTEXT_REFACTOR_GUIDE.md |
| doctrine | docs/SUBAGENT_SAFETY_MODEL.md |
| doctrine | docs/VISION_anarchy_ai_delivery_and_access.md |
| doctrine | docs/VISION_anarchy_ai_harness_core.md |
| doctrine | docs/VISION_negation_context_span_verbatim.md |
| doctrine | docs/VISION_REGISTER_MODEL.md |
| generated_branding_build | branding/assets/README.md |
| generated_branding_build | branding/branding-canon.json |
| generated_branding_build | branding/published-materials/instruction-additions/README.md |
| generated_branding_build | branding/published-materials/README.md |
| generated_branding_build | branding/README.md |
| generated_branding_build | Directory.Build.props |
| harness_setup_source | harness/branding/Branding.Shared.cs |
| harness_setup_source | harness/branding/generated/AnarchyBranding.Generated.props |
| harness_setup_source | harness/branding/generated/anarchy-branding.generated.psd1 |
| harness_setup_source | harness/branding/generated/GeneratedAnarchyBranding.g.cs |
| harness_setup_source | harness/branding/scripts/generate-branding-artifacts.ps1 |
| harness_setup_source | harness/pathing/anarchy-path-canon.json |
| harness_setup_source | harness/pathing/AnarchyPathCanon.Shared.cs |
| harness_setup_source | harness/pathing/generated/AnarchyPathCanon.Generated.props |
| harness_setup_source | harness/pathing/generated/anarchy-path-canon.generated.psd1 |
| harness_setup_source | harness/pathing/generated/GeneratedAnarchyPathCanon.g.cs |
| harness_setup_source | harness/pathing/scripts/generate-path-canon-artifacts.ps1 |
| harness_setup_source | harness/pathing/scripts/test-path-canon-compliance.ps1 |
| harness_setup_source | harness/README.md |
| harness_setup_source | harness/server/dotnet/AnarchyAi.Mcp.Server.csproj |
| harness_setup_source | harness/server/dotnet/Program.cs |
| harness_setup_source | harness/server/dotnet/Properties/AssemblyInfo.cs |
| harness_setup_source | harness/server/README.md |
| harness_setup_source | harness/server/tests/AnarchyAi.Mcp.Server.Tests.csproj |
| harness_setup_source | harness/server/tests/DirectionAssistRunnerTests.cs |
| harness_setup_source | harness/server/tests/HarnessGapAssessorTests.cs |
| harness_setup_source | harness/server/tests/RuntimeEnvelopeTests.cs |
| harness_setup_source | harness/setup/dotnet/AnarchyAi.Setup.csproj |
| harness_setup_source | harness/setup/dotnet/Program.cs |
| harness_setup_source | harness/setup/dotnet/Properties/AssemblyInfo.cs |
| harness_setup_source | harness/setup/scripts/build-self-contained-exe.ps1 |
| harness_setup_source | harness/setup/scripts/publish-anarchy-ai-setup.ps1 |
| harness_setup_source | harness/setup/tests/AnarchyAi.Setup.Tests.csproj |
| harness_setup_source | harness/setup/tests/SetupEngineTests.cs |
| narrative_history_evidence | docs/CHANGELOG_ai_links.md |
| narrative_history_evidence | docs/TODO_ai_links.md |
| narrative_history_evidence | narratives/projects/ai-links.json |
| narrative_history_evidence | narratives/register.json |
| plugin_payload | plugins/anarchy-ai/assets/anarchy_ai.svg |
| plugin_payload | plugins/anarchy-ai/assets/anarchy-ai.ico |
| plugin_payload | plugins/anarchy-ai/assets/anarchy-ai.png |
| plugin_payload | plugins/anarchy-ai/assets/anarchy-ai.svg |
| plugin_payload | plugins/anarchy-ai/branding/anarchy-branding.generated.psd1 |
| plugin_payload | plugins/anarchy-ai/pathing/anarchy-path-canon.generated.psd1 |
| plugin_payload | plugins/anarchy-ai/PRIVACY.md |
| plugin_payload | plugins/anarchy-ai/README.md |
| plugin_payload | plugins/anarchy-ai/runtime/win-x64/AnarchyAi.Mcp.Server.exe |
| plugin_payload | plugins/anarchy-ai/schemas/AGENTS-schema-1project.json |
| plugin_payload | plugins/anarchy-ai/schemas/AGENTS-schema-gov2gov-migration.json |
| plugin_payload | plugins/anarchy-ai/schemas/AGENTS-schema-governance.json |
| plugin_payload | plugins/anarchy-ai/schemas/AGENTS-schema-narrative.json |
| plugin_payload | plugins/anarchy-ai/schemas/AGENTS-schema-triage.md |
| plugin_payload | plugins/anarchy-ai/schemas/Getting-Started-For-Humans.txt |
| plugin_payload | plugins/anarchy-ai/schemas/schema-bundle.manifest.json |
| plugin_payload | plugins/anarchy-ai/templates/AGENTS.md.awareness-note.template |
| plugin_payload | plugins/anarchy-ai/templates/narratives/record.template.json |
| plugin_payload | plugins/anarchy-ai/templates/narratives/register.template.json |
| plugin_payload | plugins/anarchy-ai/TERMS.md |
| schema_family | AGENTS-schema-1project.json |
| schema_family | AGENTS-schema-comparison-matrix.md |
| schema_family | AGENTS-schema-gov2gov-migration.json |
| schema_family | AGENTS-schema-governance.json |
| schema_family | AGENTS-schema-narrative.json |
| schema_family | AGENTS-schema-triage.md |
| schema_family | Getting-Started-For-Humans.txt |
| schema_family | README-AGENTS-schema-1project.md |
| schema_family | README-AGENTS-schema-gov2gov-migration.md |
| schema_family | README-AGENTS-schema-governance.md |
| schema_family | README-AGENTS-schema-narrative.md |
| scratchpad_research | docs/ASSUMPTION_FAILURE_PEN_TEST.md |
| scratchpad_research | docs/SAMPLES_prophecy_precontext_influence_2026-04-10.md |
| scratchpad_research | docs/SCRATCHPAD_prompt_efficacy_patterns.md |
| scratchpad_research | docs/SCRATCHPAD_prophecy_precontext_influence.md |
| scripts | docs/scripts/capture-claude-baseline.ps1 |
| scripts | docs/scripts/diff-claude-baseline.ps1 |
| scripts | docs/scripts/test-documentation-truth-compliance.ps1 |
| scripts | docs/scripts/test-removal-safety-compliance.ps1 |
| scripts | docs/scripts/test-schema-mirror-sync-compliance.ps1 |
| scripts | docs/scripts/test-semantic-compression-audit-compliance.ps1 |
| scripts | plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1 |
| scripts | plugins/anarchy-ai/scripts/Remove Anarchy-AI.cmd |
| scripts | plugins/anarchy-ai/scripts/remove-anarchy-ai.ps1 |
| scripts | plugins/anarchy-ai/scripts/remove-anarchy-ai-human.ps1 |
| scripts | plugins/anarchy-ai/scripts/start-anarchy-ai.cmd |
| scripts | plugins/anarchy-ai/scripts/stop-anarchy-ai.ps1 |
| scripts | plugins/Remove Anarchy-AI.cmd |
| skills | plugins/anarchy-ai/skills/anarchy-ai-harness/SKILL.md |
| skills | plugins/anarchy-ai/skills/chat-history-capture/SKILL.md |
| skills | plugins/anarchy-ai/skills/README.md |
| skills | plugins/anarchy-ai/skills/structured-commit/SKILL.md |
| skills | plugins/anarchy-ai/skills/structured-review/SKILL.md |
| templates | templates/AGENTS_TEMPLATE.md |
| templates | templates/ANARCHY_AI_STARTUP_INSTRUCTION_TEMPLATE.md |
| templates | templates/CROSS_REPO_HANDOFF_TEMPLATE.md |
| templates | templates/MODULE_AGENT_PROMPT_TEMPLATE.md |
| templates | templates/PROMPT_PROJECT_TEMPLATE.md |
| templates | templates/README_PROJECT_TEMPLATE.md |
| templates | templates/STARTUP_CONTEXT_BUDGET_WORKSHEET.md |

### Commands Run

```powershell
git status --short --branch --untracked-files=all
rg --files | Sort-Object
git diff --name-status
git ls-files --others --exclude-standard
git diff --stat
git log --since='2026-04-24 00:00' --date=iso-strict --pretty=format:'%h%x09%ad%x09%s' --stat --shortstat
powershell -NoProfile -ExecutionPolicy Bypass -File .\docs\scripts\test-schema-mirror-sync-compliance.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\docs\scripts\test-documentation-truth-compliance.ps1 -RepoRoot (Get-Location).Path
powershell -NoProfile -ExecutionPolicy Bypass -File .\harness\pathing\scripts\test-path-canon-compliance.ps1 -RepoRoot (Get-Location).Path
powershell -NoProfile -ExecutionPolicy Bypass -File .\docs\scripts\test-removal-safety-compliance.ps1 -RepoRoot (Get-Location).Path
rg --files -g "*.json" | ForEach-Object { Get-Content -Raw $_ | ConvertFrom-Json | Out-Null }
git diff --check
& 'C:\Users\herri\.dotnet\dotnet.exe' test .\harness\server\tests\AnarchyAi.Mcp.Server.Tests.csproj -c Debug --no-restore
& 'C:\Users\herri\.dotnet\dotnet.exe' test .\harness\setup\tests\AnarchyAi.Setup.Tests.csproj -c Debug --no-restore
Select-String -Path docs\scripts\test-schema-mirror-sync-compliance.ps1 -Pattern 'structured-commit|structured-review|chat-history-capture|Canonical|skills' -Context 2,2
Select-String -Path docs\CHANGELOG_ai_links.md,plugins\anarchy-ai\skills\README.md,docs\README_ai_links.md -Pattern 'chat-history-capture|Both skills|0.1.13|marketplace.json|mojibake markers'
Get-FileHash -Algorithm SHA256 AGENTS-schema-governance.json,AGENTS-schema-1project.json,AGENTS-schema-narrative.json,AGENTS-schema-gov2gov-migration.json
Get-FileHash -Algorithm SHA256 plugins\anarchy-ai\schemas\AGENTS-schema-governance.json,plugins\anarchy-ai\schemas\AGENTS-schema-1project.json,plugins\anarchy-ai\schemas\AGENTS-schema-narrative.json,plugins\anarchy-ai\schemas\AGENTS-schema-gov2gov-migration.json
Get-FileHash -Algorithm SHA256 plugins\anarchy-ai\skills\chat-history-capture\SKILL.md,.codex\skills\chat-history-capture\SKILL.md,.claude\skills\chat-history-capture\SKILL.md
Get-ChildItem -Path 'C:\Users\herri\.codex\plugins\cache\anarchy-ai-user-profile\anarchy-ai' -Directory
Test-Path .agents
Test-Path .agents\plugins\marketplace.json
Get-ChildItem -Recurse -Directory -Path . -Include bin,obj,.tmp -ErrorAction SilentlyContinue
```

### Anarchy-AI Harness Use

- `preflight_session` was used at audit start.
- The result classified this workspace as `schema_authoring_and_plugin_delivery_workspace` and the callable runtime as `user_profile_installed_runtime`.
- Per AI-Links doctrine, that harness output was treated as evidence and assistance, not as final authority over source intent.
