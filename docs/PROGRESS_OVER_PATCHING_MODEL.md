# Progress Over Patching Model

## Purpose

This model exists to stop AI-assisted delivery from drifting into narrow patches that keep a surface alive while leaving the underlying module, schema, or workflow semantics weak.

The goal is durable product progress, not visible-but-fragile motion.

## Core rule

- Treat each change as module/product work for a parent solution.
- A patch is acceptable only when it is clearly the right size for the real impact surface.

## Waste patterns to avoid

- tiny patches when a broader impact sweep and scoped plan are needed
- fixing a page while leaving the data model or translation contract semantically wrong
- hiding missing structure in payload blobs, mapper-only code, or UI-only logic
- fixing a controller while ignoring the scripts, bootstrap helpers, or tests that the change depends on
- using fallback/degraded behavior to make errors disappear without making the authority model explicit
- naming code so vaguely that every change requires external documentation to understand likely impact

## Required behavior

Before changing a significant surface, inspect:

- the impacted schema and local table families
- the translation or hydration path
- the read and write paths
- the API or bundle contract
- the controller, service, and view seams
- the startup/bootstrap and supporting scripts
- the relevant validation and regression tests

## Local-first schema rule

- If local-first tables are missing fields needed to represent business meaning clearly, widen them.
- If a new local supporting table is needed, add it.
- Prefer additive local schema sprawl over repeated cross-system translation pain.
- Do not keep forcing missing business meaning into payload blobs, mapping glue, or hidden joins.

## Source-of-truth rule

- A change is not progress if it leaves source-of-truth ambiguous across mixed local, legacy, cache, and external-system lanes.
- Degraded or fallback behavior must be explicit and understandable to operators.
- Hidden fallback is not the same thing as a supported degraded mode.

## Cross-repo impact rule

- Treat repo-local work as part of a parent platform, not as a sealed local change.
- When a slice touches shared identity, workflow routing, client/contact truth, tickets, people operations, commercial data, or module handoff behavior, inspect the neighboring producer and consumer repos.
- The impact sweep should account for upstream producers, downstream consumers, orchestration/runtime repos, and any edge/intake utilities that create or mutate the same business objects.
- Do not call a slice complete when it improves one repo while silently breaking, bypassing, or confusing sibling repos that rely on the same contracts.

## Discoverable code rule

- Name tables, columns, classes, methods, variables, and properties so domain intent is readable from the code.
- Write code so a reviewer can discover likely impact by reading the code, not by repeatedly consulting documentation for every small change.

## Completion rule

A slice is not done just because it compiles or returns `200`.

It should be considered done only when it improves:

- correctness
- discoverability
- impact awareness
- validation coverage
- future maintainability
