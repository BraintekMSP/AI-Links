---
name: anarchy-ai-harness
description: Use Anarchy-AI to preflight meaningful governed work, assess install/runtime/schema gaps, compile active work into bounded state, verify whether a schema package is materially real and canonically aligned, and run non-destructive gov2gov reconciliation when drift or partial materialization is present.
---

# Anarchy-AI Harness

## When to use

Use this skill when:

- meaningful governed work is starting and the harness should preflight the session first
- installation, registration, or adoption state is unclear
- the workspace may contain a copied schema package
- schema presence does not prove materialization
- folder topology, package surfaces, or companion artifacts may be stale
- the current work is becoming fuzzy, negation-heavy, or difficult to resume cleanly

## Tool order

1. Call `preflight_session` before meaningful governed work or when the agent is unsure whether the harness should take the first turn.
2. Call `assess_harness_gap_state` when installation, registration, runtime, or adoption state is unclear.
3. Call `compile_active_work_state` when the current work needs to be normalized into one bounded packet.
4. Call `is_schema_real_or_shadow_copied` before trusting a schema package.
5. For `partial` or `copied_only` schema reality, call `run_gov2gov_migration`.
6. For `real` + `possessed` schema reality, prefer `plan_only` first so canonical divergence is reviewed before any apply step.

## Experimental/Test Tool

- `direction_assist_test` is an explicit test-lane helper and is not part of the default core tool order above.
- Use it only when you want bounded direction qualification for long prompts with fixed two-choice output and local test telemetry.

## Rules

- Treat copied schema presence as a hint, not as operative reality.
- Treat `preflight_session` as the default entry for meaningful governed work.
- Treat `schema_reality_state`, `integrity_state`, and `possession_state` as separate result axes.
- Treat install presence and full adoption as different conditions.
- Use `compile_active_work_state` when the agent is at risk of working directly from chat turbulence instead of bounded operational state.
- Prefer `plan_only` first when the user asked for diagnosis.
- Use `non_destructive_apply` only when the user wants reconciliation work performed.
- Keep schema truth in the underlay; do not treat harness runtime output as canonical schema authorship.
- The reflection workflow (`assess the last exchange and do better`) remains a secondary workflow, not a core tool in this skill yet.
