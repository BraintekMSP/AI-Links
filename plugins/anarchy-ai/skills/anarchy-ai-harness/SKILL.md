---
name: anarchy-ai-harness
description: Use Anarchy-AI to preflight complex changes, turn incomplete repo context into bounded working context, assess install/runtime/schema gaps, verify whether a schema package is materially real and canonically aligned, and run non-destructive gov2gov reconciliation when drift or partial materialization is present.
---

# Anarchy-AI Harness

## When to use

Use this skill when:

- complex changes are starting and the harness should preflight the session first
- the current repo context is incomplete, stale, or expensive to rebuild from chat alone
- installation, registration, adoption state, or repo-local artifact hygiene is unclear
- the workspace may contain a copied schema package
- schema presence does not prove materialization
- folder topology, package surfaces, or companion artifacts may be stale
- the current work is becoming fuzzy, negation-heavy, or difficult to resume cleanly

## Tool order

1. Call `preflight_session` before complex changes or when the agent is unsure whether the harness should take the first turn.
2. Call `assess_harness_gap_state` when installation, registration, runtime, underlay readiness, adoption state, or generated artifact hygiene is unclear.
3. Call `compile_active_work_state` when the current work needs to be normalized into one bounded packet.
4. Call `is_schema_real_or_shadow_copied` before trusting a schema package.
5. For `partial` or `copied_only` schema reality, call `run_gov2gov_migration`.
6. Call `validate_narrative_arc_state` before declaring Arc, narrative, chat-history-capture, or gov2gov narrative-record edits complete.
7. For `real` + `possessed` schema reality, prefer `plan_only` first so canonical divergence is reviewed before any apply step.

## Experimental/Test Tool

- `direction_assist_test` is an explicit test-lane helper and is not part of the default core tool order above.
- Use it only when you want bounded direction qualification for long prompts with fixed two-choice output and local test telemetry.

## Companion Skills

- Use `chat-history-capture` when the user asks to mine the current chat or pasted chat history for repo-specific decisions, direction, findings, or open threads that need to be added to narrative arc and vision docs.
- Before schema, arc, or gov2gov work, reject stale hardcoded Codex cache skill paths as authority unless their version matches the observed active harness lane.

## Rules

- Treat copied schema presence as a hint, not as operative reality.
- Treat `preflight_session` as the default entry for complex changes.
- Treat `schema_reality_state`, `integrity_state`, and `possession_state` as separate result axes.
- Treat install presence and full adoption as different conditions.
- Treat `underlay_readiness` as the distinction between portable schema/template availability and actual repo utilization. A repo can have the narrative schema available while still having no narrative register, projects directory, or arc records.
- Treat `doctor_summary` as measurement-first terrain: observed state, gaps, suggested corrections, and pitfalls. It is not an instruction surface and must not override repo or human authority.
- Treat `structural_grounding` as terrain/provenance, not refusal or authority. The tools still run; this payload labels which schema/underlay surfaces the output presumes, what was observed, and which advisory measurement or migration lane would improve grounding.
- Treat `validate_narrative_arc_state` as author-time/checkpoint measurement, not an authoring wrapper. It reports Arc/register conformance findings and suggested corrections, but does not write, block, or decide content.
- Treat `artifact_hygiene` findings as relocation guidance, not permission to delete generated output.
- Use `compile_active_work_state` when the agent is at risk of working directly from chat turbulence instead of bounded operational state.
- Prefer `plan_only` first when the user asked for diagnosis.
- Use `non_destructive_apply` only when the user wants reconciliation work performed.
- Keep schema truth in the underlay; do not treat harness runtime output as canonical schema authorship.
- The reflection workflow (`assess the last exchange and do better`) remains a secondary workflow, not a core tool in this skill yet.
