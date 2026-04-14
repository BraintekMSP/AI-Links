# Anarchy-AI Startup Instruction Template

## Purpose

Drop-in text for target repos that want an agent to start meaningful governed work correctly, whether the Anarchy-AI harness is installed or still pending.

The block below routes the agent through the harness when it is present and falls through to direct governance-file reading when it is absent. Installing the harness later changes which branch fires — the template itself does not need to change.

Place this block in one of:

- the target repo's auto-read surface for the host:
  - Claude Code: `./CLAUDE.md` or `./.claude/CLAUDE.md` at project root
  - Codex: `./AGENTS.md` at project root
  - Cursor: `./.cursorrules` or `./.cursor/rules/`
- a startup or control-plane prompt packet the agent reads first
- a primary README startup truth chain

Adjust the workspace-specific parts (workspace name, host context) where marked.

---

## Paste-ready block

Copy everything between the fences:

```markdown
## Governance entry — meaningful-governed-work startup

Meaningful governed work in this repo starts by checking for the Anarchy-AI
harness. The harness reads governance files correctly every session; direct
file reading is the fallback when the harness is absent.

### Step 1 — check for the harness

Look for the Anarchy-AI MCP tool `preflight_session` in the available tool
surface.

### Step 2a — when `preflight_session` is available (harness present)

Call the harness in this order as the situation calls for each tool:

1. `preflight_session` — decide whether the repo/session is ready for
   meaningful governed work; every governed-work session begins here
2. when `preflight_state` returns `ready` or `ready_with_gaps`, follow the
   `recommended_path` and continue
3. when `preflight_state` returns `bootstrap_needed`, follow the
   `recommended_path` to bring the harness online in this repo
4. when `preflight_state` returns `manual_review_required` (report-to-human
   at the schema level), report findings to the human and receive direction
   before continuing

Follow-on harness tools (use when preflight surfaces the need, or when the
situation calls for them directly):

- `assess_harness_gap_state` — when install, runtime, schema, or adoption
  state is unclear; one call returns all five gap categories together with
  admin and agent action lanes
- `is_schema_real_or_shadow_copied` — before trusting copied schema
  presence as operative reality; distinguishes real from shadow-copied
  schemas and reports integrity and possession state
- `run_gov2gov_migration` — in `plan_only` first when existing authority
  surfaces must be reconciled with the AGENTS schema family; apply
  non-destructively only after the plan has been reviewed
- `compile_active_work_state` — when session turbulence has accumulated
  and the work needs to reanchor to workspace reality

### Step 2b — else (harness absent, direct fallback)

Read the governance files at the repo root in this order before making any
changes:

1. `AGENTS.md` — entry point and routing
2. `AGENTS-hello.md` — scope, session contract, measurement references
3. `AGENTS-Terms.md` — term definitions this repo uses
4. `AGENTS-Vision.md` — scope-level direction and locked commitments
5. `AGENTS-Rules.md` — operating rules for this scope
6. `AGENTS-Pitfalls.md` — failure history

These files are authoritative. Copying their rules into this startup block
would fragment authority and drift. Read the source.

When the rules surface a conflict or ambiguity, report to the human and
receive direction before continuing — that is the schema-level
report-to-human pattern.

### Default rule

Meaningful governed work in this repo begins with one of:

- `preflight_session` when the harness is available
- reading the AGENTS files at the repo root, in order, when the harness is
  absent

Direct tool use or direct code work remains valid when the lane is already
clear. Report to the human at the first signal of ambiguity rather than
improvising.

### Why this entry exists

Agents do not auto-read arbitrary governance files. The host's auto-read
surface (this file) is what every session sees; anything outside it
loads only when the agent knows to look. This entry makes the lookup
explicit either through the harness or through direct reading.

For the full architecture see:

- `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md` — three-layer architecture
- `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md` — installation and adoption
- `docs/README_ai_links.md` — harness scope boundary
```

---

## Adaptation notes

### Where to place the block

The goal is that the governance entry is visible in the agent's first read
of the repo. The surface varies by host:

| Host | Auto-read surface (project-level) |
|------|-----------------------------------|
| Claude Code | `./CLAUDE.md` or `./.claude/CLAUDE.md` |
| Codex | `./AGENTS.md` at project root |
| Cursor | `./.cursorrules` or `./.cursor/rules/` |

User-level memory (e.g. `~/.claude/CLAUDE.md`) is personal and not
repo-editable. Target the project-level surface for this block.

When the repo uses a control-plane prompt packet, place the block high in
the packet — before the current-work section, so routing happens before
the agent commits to a work approach from context.

### Minimum viable adoption

When the full block is too heavy for the target surface, the one-line
default rule alone is enough:

> Meaningful governed work in this repo starts with `preflight_session` when
> the Anarchy-AI harness is available, else with reading the `AGENTS-*.md`
> files at the repo root in order.

Agents familiar with the system recognize this entry; agents new to the
system follow the link chain to the harness docs.

### Detection caveats

The block relies on the agent checking for the harness MCP tool list
before acting. Host capability varies:

- hosts that expose MCP tool surfaces to the agent context (Claude Code,
  Codex) can do this check directly
- hosts that hide the tool surface until a call is made may require a
  try-and-fallback pattern — call `preflight_session`, and when the call
  fails because the tool is absent, take the Step 2b fallback

When the host cannot expose tool availability ahead of calling, report to
the human the first time this branch is unclear, rather than guessing.

### Adoption verification

After placing the block, verify:

- the target file (e.g. `./CLAUDE.md`) contains the block verbatim
- the block is in the agent's auto-read surface for this host
- when the harness is installed, `AnarchyAi.Setup.exe /assess` returns
  `bootstrap_state = ready`
- when the harness is installed, `preflight_session` is callable from the
  agent surface

The block works in both pre-harness and post-harness states. Installing
the harness later changes which branch fires; the block itself does not
need to change.

---

## Linguistic discipline notes

This template follows the affirmative-framing discipline applied across
the AGENTS-schema-* family:

- "report-to-human" replaces "blocked until human confirms"
- "when X returns Y" replaces "if X is not Y"
- "else" replaces "if-unresolved" as the fallback branch label
- tool usage framed as active lanes rather than prohibitions
- "read the source" replaces "don't copy rules"

When adapting this template, preserve the affirmative framing. Negations
activate the prohibited concept first in both human neural processing
(Zuanazzi 2024) and LLM attention (Pink Elephant paper, 2025).

See `docs/SCRATCHPAD_prompt_efficacy_patterns.md` for the research basis.
