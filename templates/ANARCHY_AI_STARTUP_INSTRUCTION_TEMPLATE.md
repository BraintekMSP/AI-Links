# Anarchy-AI Startup Instruction Template

## Purpose

Drop-in text for target repos that have installed Anarchy-AI and want the harness to behave like a harness, not like optional utilities.

Place this block in one of:

- the target repo's `AGENTS.md` (under a named section)
- the target repo's startup or control-plane prompt packet
- the target repo's primary README startup truth chain
- the target repo's scope read sequence

The block below is written for direct paste. Adjust only the workspace-specific parts (workspace name, host context) where marked.

---

## Paste-ready block

Copy everything between the fences:

```markdown
## Anarchy-AI harness — meaningful-governed-work entry

Meaningful governed work in this repo starts with `preflight_session`.

Before beginning code changes, migrations, governance edits, or cross-file
reconciliation work, call the harness:

1. `preflight_session` — decide whether the repo/session is ready for this work
2. when `preflight_state` returns `ready` or `ready_with_gaps`, follow the
   `recommended_path` and continue
3. when `preflight_state` returns `bootstrap_needed`, follow the
   `recommended_path` to bring the harness online in this repo
4. when `preflight_state` returns `manual_review_required` (report-to-human
   at the schema level), report findings to the human and receive direction
   before continuing

### Follow-on harness tools

Use these when preflight surfaces the need or when the situation calls for
them directly:

- `assess_harness_gap_state` — call this when install, runtime, schema, or
  adoption state is unclear; one call returns all five gap categories
  together with admin and agent action lanes
- `is_schema_real_or_shadow_copied` — call this before trusting copied
  schema presence as operative reality; distinguishes real from shadow-copied
  schemas and reports integrity and possession state
- `run_gov2gov_migration` — call this in `plan_only` first when existing
  authority surfaces must be reconciled with the AGENTS schema family;
  apply non-destructively only after the plan has been reviewed
- `compile_active_work_state` — call this when session turbulence has
  accumulated and the work needs to reanchor to workspace reality

### Default rule

The default agent rule for this repo:

- meaningful governed work starts with `preflight_session`
- direct tool use remains valid when the lane is already clear
- report to the human at the first signal of ambiguity rather than improvising

### Why this entry exists

The harness reads workspace reality before the agent commits to a work
path. That keeps the session anchored to what the repo actually contains
rather than what the conversation implies. See:

- `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md` for the three-layer architecture
- `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md` for installation and adoption
- `docs/README_ai_links.md` for the harness scope boundary
```

---

## Adaptation notes

### Where to place the block

Place the block high in whichever surface is the agent's first read. The
goal is that the preflight instruction is visible before the agent picks
a work approach from context.

Common placements:

- `AGENTS.md` top-level section after the startup truth chain
- control-plane prompt packet before the current-work section
- primary README under a "How agents start here" header

### Minimum viable adoption

If the target repo cannot accept the full block, the one-line default rule
on its own is the minimum viable adoption:

> Meaningful governed work in this repo starts with `preflight_session`.

This keeps the entry obvious even when the surrounding explanation is
elided. Agents familiar with the harness will recognize it; agents new to
the harness will follow the link chain to the harness docs.

### When the target repo has no harness installed yet

If the target repo has yet to install Anarchy-AI, leave the block out.
Install first through:

- `plugins/AnarchyAi.Setup.exe /install /repolocal` or `/userprofile`

See `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md` for the full install process.

The startup instruction presupposes the harness is callable. Placing it in
a repo where the harness is missing produces agent confusion about why the
referenced tools return errors.

### Adoption verification

After placing the block, verify adoption through:

- `AnarchyAi.Setup.exe /assess` returns `bootstrap_state = ready`
- `preflight_session` is callable from the agent surface
- the block is visible in the agent's default startup read path

When all three are true, the target repo satisfies the startup instruction
requirement from `ANARCHY_AI_REPO_INSTALL_PROCESS.md` section 2.

---

## Linguistic discipline notes

This template follows the affirmative-framing discipline applied across
the AGENTS-schema-* family:

- "report-to-human" replaces "blocked until human confirms"
- "when X returns Y" replaces "if X is not Y"
- "the default rule is" replaces "agents must not"
- tool usage framed as active lanes rather than prohibitions

When adapting this template, preserve the affirmative framing. Negations
activate the prohibited concept first in both human neural processing
(Zuanazzi 2024) and LLM attention (Pink Elephant paper, 2025).

See `docs/SCRATCHPAD_prompt_efficacy_patterns.md` for the research basis.
