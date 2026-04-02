# AGENTS Template

## Scope
- define allowed edit scope
- define sensitive areas

## Startup truth chain
- define what to read first

## Documentation rules
- TODO = active only
- changelog = completed work
- README = runbook

## Safety rules
- destructive actions require explicit approval
- artifact boundaries must be explicit
- runtime patching is not the permanent fix

## Progress over patching
- inspect schema, translation/hydration, API, surface, script, and validation impact before changing significant behavior
- widen local tables when business meaning is missing instead of hiding meaning in blobs, mapper glue, or UI-only logic
- do not treat hidden fallback as success
- require explicit cross-repo impact review when the change affects shared business objects, workflow routing, people/user state, or producer/consumer contracts
- name code so likely impact is discoverable without constant reference to external docs

## Subagent rules
- default off
- read-only explorers
- exact write ownership for workers
