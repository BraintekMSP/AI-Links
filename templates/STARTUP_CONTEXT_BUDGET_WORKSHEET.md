# Startup Context Budget Worksheet

Use this worksheet with `docs/STARTUP_CONTEXT_BUDGET_MODEL.md`.

## Repo
- name:
- type:
  - single-lane app
  - shared library/framework
  - operational shell / mixed host
  - cross-repo control plane

## Complexity score

Rate each `0-5`.

- domain breadth:
- external integration depth:
- runtime/deployment topology:
- state and data-lane count:
- ownership-boundary count:
- consequence of misunderstanding:
- legacy/coexistence burden:

`ComplexityScore =`

## Complication score

Rate each `0-5`.

- duplicate guidance drift:
- temporary artifact residue:
- fallback/helper sprawl:
- generated-artifact competition:
- history pollution in active docs:
- terminology or role-split drift:

`ComplicationScore =`

## Coverage score

Rate each `0-4`.

- AGENTS strength:
- runbook clarity:
- prompt quality:
- backlog discipline:
- history discipline:

`CoverageScore =`

## Role bonus
- `0` single-lane app/tool
- `500` shared library/framework
- `1000` operational shell or mixed host
- `2500` cross-repo control plane

`RoleBonus =`

## Risk premium
- `0` cheap under-reading
- `500` meaningful cleanup/review cost
- `1000` operationally dangerous under-reading

`RiskPremium =`

## Outputs

`ComplicationDensity = ComplicationScore / max(ComplexityScore, 1)`

`PredictedStartupBudget = 1500 + (ComplexityScore * 110) + (ComplicationScore * 70) + RoleBonus + RiskPremium`

Round to nearest `500`:

`RoundedStartupBudget =`

## Interpretation

- current startup budget:
- difference from predicted:
- docs likely:
  - under-documented
  - overgrown
  - healthy
- next action:
  - expand startup context
  - reconcile startup docs
  - trim duplicate/ephemeral material
  - leave as-is
