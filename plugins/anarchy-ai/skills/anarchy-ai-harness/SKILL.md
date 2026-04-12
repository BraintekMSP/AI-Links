---
name: anarchy-ai-harness
description: Use the local Anarchy AI harness tools before trusting a copied or partially materialized schema package. Call the schema-reality tool first, then hand off to gov2gov reconciliation when the package is not yet real.
---

# Anarchy AI Harness

## When to use

Use this skill when:

- the workspace may contain a copied schema package
- schema presence does not prove materialization
- folder topology, package surfaces, or companion artifacts may be stale

## Tool order

1. Call `is_schema_real_or_shadow_copied`.
2. If the result is `partial` or `copied_only`, call `run_gov2gov_migration`.

## Rules

- Do not assume copied schema presence means operative reality.
- Prefer `plan_only` first when the user asked for diagnosis.
- Use `non_destructive_apply` only when the user wants reconciliation work performed.
- Keep schema truth in the underlay; do not treat harness runtime output as canonical schema authorship.
