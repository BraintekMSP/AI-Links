# AI-Links — AGENTS Heuristic Underlay

Portable schema family that engineers what AI agents read before they act — governance, session continuity, narrative memory, and migration contracts. Ships as AGENTS.md.

## What this is

AI-Links is a pre-context control layer for AI-assisted work. It shapes agent behavior by controlling what is presented before and around the session — not by enforcing rules at runtime.

Agent behavior is heuristic. It must be shaped before local heuristics take over.

The system is not trying to maximize accuracy. It is trying to reliably improve the trajectory of future AI work sessions before those sessions' specific task context is known.

## The AGENTS Schema Family

Four schemas, each solving a different structural problem:

| Schema | Problem it solves | When to use |
|---|---|---|
| **governance** | Rules, ownership, scope boundaries, complexity drift | Multi-scope code and operations projects |
| **1project** | Session continuity, goal tracking, failure memory | Single-goal bounded work |
| **narrative** | Tribal knowledge, relationship history, communication memory | Any entity with an evolving story and multiple parties |
| **gov2gov** | Semantic drift during governance-to-governance migration | Reconciling one governed system into another |

A triage document routes to the correct schema. An agent reads triage first, picks one schema, and starts working. No human needs to understand the schema system to get started.

## Portable deployment set

These 6 files travel to any workspace that wants to use the schema family:

```
AGENTS-schema-governance.json
AGENTS-schema-1project.json
AGENTS-schema-narrative.json
AGENTS-schema-gov2gov-migration.json
AGENTS-schema-triage.md
Getting-Started-For-Humans.txt
```

Everything else in this repo is reference, evaluation, or research material.

## Evaluation and reference material

Stays in AI-Links, does not deploy:

- **READMEs** — human-facing explainer per schema (README-AGENTS-schema-*.md)
- **Comparison matrix** — 4-column structural comparison with research scaffold (AGENTS-schema-comparison-matrix.md)
- **Research scratchpads** — prompt efficacy patterns, prophecy/pre-context influence theory, observed samples
- **Assumption-failure pen test** — stress test of the framework's control model
- **Framework docs** — startup prompts, cross-repo contracts, readiness models, safety patterns, templates

## Who this is for

- Internal software teams using AI agents
- MSPs and IT teams managing multiple client environments
- Consultants building client-specific delivery workflows
- Organizations wanting structured AI collaboration without chaotic repo behavior
- Anyone who has said "the AI keeps forgetting what we decided" or "nobody remembers why we did it this way"

## How it works

The schemas are read before work begins. They set the statistical landscape before the agent's first decision token is generated:

- **Conflation maps** override the model's training-data defaults for ambiguous terms
- **Well-named fields** reduce composition depth so the model pattern-matches in one step instead of composing across multiple
- **Verbatim repeated rules** provide structured re-exposure to critical context
- **Triage routing** ensures only one schema enters the context window
- **Session state tracking** writes durable state to files, not to context that compresses away

The system exploits how transformers work (pattern matching, causal masking, one-pass resolution) rather than fighting it.

## Research foundation

The methodology is grounded in two papers:

- **Dziri et al. (2023)** — "Faith and Fate: Limits of Transformers on Compositionality" (NeurIPS Spotlight). Transformers reduce multi-step reasoning to linearized subgraph matching. The schemas function as the external planning module the paper recommends.
- **Leviathan et al. (2025)** — "Prompt Repetition Improves Non-Reasoning LLMs" (Google Research). Repeated input exposure compensates for causal masking limitations. The schemas provide structured repeated exposure to critical context.

See `docs/SCRATCHPAD_prompt_efficacy_patterns.md` and `docs/SCRATCHPAD_prophecy_precontext_influence.md` for the full research synthesis.

## Startup path

For the framework docs (not the schema family):

1. Read `AGENTS.md`
2. Read `docs/README_ai_links.md`
3. Read `docs/AI_COLLAB_STARTUP_PROMPT.md`
4. Read `docs/REPO_2_5_READINESS_MODEL.md`
5. Read the templates in `templates/`

For the schema family specifically: start with `Getting-Started-For-Humans.txt` or hand an agent `AGENTS-schema-triage.md` and the 4 schema files.

## Publication note

This scaffold is public-safe by design, but review before publishing adapted versions:

- `LICENSE`
- `NOTICE`
- `docs/PUBLICATION_CHECKLIST.md`
- `SECURITY.md`
- `CONTRIBUTING.md`

## Usage notice

This repository is public for reference and evaluation.
Reuse requires prior written permission from the repository owner.

## Support and maintenance

- Public access is for reference and starter use only.
- No support obligation is created by publication.
- No maintenance commitment is created by publication.
- Pull requests may be ignored or closed without review.
