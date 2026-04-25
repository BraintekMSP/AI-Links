# Implementation Gap Register

## Purpose

This document holds persistent implementation gaps that do not fit cleanly into:

- research scratchpads
- dated sample corpora
- one-pass changelog entries

It exists to preserve:

- what the gap is
- why it matters
- what choice or correction it points toward
- what problem that choice is trying to solve

This is intentionally less stateful than a scratchpad and less anecdotal than samples.

It is the place for:

- lessons learned from semi-functional implementations
- delivery-path failures
- schema-to-runtime inconsistencies
- claim-discipline gaps

It is not the place for:

- open-ended theory building
- poetic framing
- empirical sample transcripts

This is a rolling review register.

An entry may remain here until a later review confirms that the gap is actually closed in the delivered surface, not merely patched once in local state.

---

## Local Status Note - 2026-04-12

Addressed in current local state and ready for re-review after commit:

- first-class `preflight_session` and `assess_harness_gap_state` contracts in the shared harness core
- explicit repo-bootstrap lane through `plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1`
- separate Windows-first `AnarchyAi.Setup.exe` delivery lane with:
  - embedded plugin bundle materialization
  - repo-local marketplace registration
  - CLI assess/install/update behavior
  - minimal GUI assess/install behavior
- plugin trust metadata and user-facing trust surfaces
- direct bundled-runtime launch path as the primary documented delivery path
- stale `scaffold` / `first two tools` maturity framing across harness and plugin docs
- missing architecture sentence tying together schema family, underlay, and Anarchy-AI
- skill result-axis ambiguity between schema reality and possession
- overstrong `reliably improve` wording in scratchpad `#2`
- the most concentrated governance residual stop-language around:
  - measurement prose
  - migration-sequence approval language
  - required-field `missing` handling in the schema family where the exact abort phrase was present

Still live or only partially addressed:

- broader platform-installation alignment around Windows-first delivery versus marketplace/default-install posture
- the `net48` target still exists in source but is not a validated packaged/runtime lane like `net8.0`
- Claude-specific adapter packaging/registration is still not a delivered surface
- machine-level install and managed-device rollout are still deferred
- reflection workflow (`assess the last exchange and do better`) is still secondary, not delivered as a first-class lane
- deeper narrative-schema review
- the guarded future distinction between narrative and a possible journal / accounting capture lane

---

## Current Architecture Language

Current best framing:

- schema family = canonical layer
- AGENTS Heuristic Underlay = operative layer
- Anarchy-AI = runtime framework harness

Why this framing is better:

- `canonical layer` is narrower and safer than `truth layer`
- it avoids implying blind trust or universal truth claims
- it keeps the schema family as the declared source of package structure and intended operating artifacts
- it keeps the underlay as the broader operative environment built from those artifacts
- it keeps Anarchy-AI as the runtime harness/framework exposure rather than as the source of schema authorship

Working product sentence:

- the schema family is the canonical layer
- the AGENTS Heuristic Underlay is the operative layer built from that family
- Anarchy-AI is the runtime framework harness that evaluates, compiles, and reconciles local state without replacing the schema family

---

## Current Gaps

### 0.5. Path-role drift created unsafe update semantics

#### Gap

Important operational surfaces had been rebuilding path meaning with local string literals instead of one shared canon. That drift showed up in:

- setup result JSON
- harness gap/health inspection JSON
- repo-local bootstrap JSON
- generated README and `.mcp.json` publish surfaces
- marketplace writes and legacy-path detection

The same few facts were being represented multiple different ways:

- repo-authored origin
- actual update source
- destination target
- plugin-relative versus marketplace-relative references

#### Why it matters

- safe updates depend on knowing exactly which path is the origin, which is the actual source, and which is the destination
- flat fields like `workspace_root`, `repo_root`, and `plugin_root` were overloaded enough to hide that distinction
- future installer, runtime, or doc edits would have kept reintroducing drift because there was no single audit-backed path canon

#### Correction direction

- keep one repo-authored path canon under `harness/pathing/`
- generate code/script consumers from that canon
- use nested `paths.origin|source|destination` in setup/bootstrap/health-style outputs
- fail operational builds when forbidden hard-coded path literals reappear outside the allowlisted generated/evidence zones

### 0. Codex Home-Install Model Drift Polluted Setup Truth

#### Gap

The repo had drifted into a hybrid user-profile story that treated these as the primary Codex home-install truth:

- `~/plugins/anarchy-ai`
- required `[mcp_servers.anarchy-ai]` registration in `~/.codex/config.toml`

That model no longer matched the current Codex personal-plugin lane or the controlled install evidence on this machine.

The stale model then bled into multiple places at once:

- setup path resolution
- readiness semantics
- disclosure/help text
- environment truth docs
- hand-maintained installed README copy

#### Why it matters

- it made a failed or partial PoC home install look closer to healthy than it really was
- it taught agents to chase the wrong surfaces when assessing presence
- it let documentation drift faster than the installer/runtime behavior

#### What this points toward

- keep Codex home install plugin-marketplace-first:
  - `~/.agents/plugins/marketplace.json`
  - `~/.codex/plugins/anarchy-ai`
- treat custom `mcp_servers.anarchy-ai` as fallback/debug evidence only
- treat older legacy `mcp_servers.anarchy-ai` entries as cleanup evidence only
- keep canonical docs and install assertions repo-authored, then publish/generate destination-relative install surfaces from those sources
- detect failed legacy PoC home layouts honestly and repair them through inventory plus manual cleanup guidance rather than compatibility drift

#### Problem being solved

- restoring one source of truth for Codex home install behavior and preventing future repo-to-installer documentation drift

---

### 1. Governance Schema Still Carries Residual Exit Grammar

#### Gap

The governance schema still contains high-salience stop/approval grammar in places the linguistic audit was trying to clean up.

Examples:

- `block on discrepancy`
- `may not proceed`
- `receive approval`
- `do not proceed until human acknowledges`

These still survive in measurement and migration prose even after the `report-to-human` key migration.

#### Why it matters

- the key names improved
- the prose still teaches similar stop-energy
- the schema therefore carries mixed steering signals

#### What this points toward

- a narrower cleanup pass on:
  - measurement prose
  - migration-sequence prose
  - approval language that reintroduces exit energy after the noun/key was cleaned

#### Problem being solved

- reducing abort-friendly grammar in high-authority schema locations

---

### 2. Required-Field `missing` Handling Still Teaches Abort Grammar

#### Gap

The repeated pattern:

- `missing: "invalid -- do not proceed"`

still appears on load-order-0 and other required fields in the governance schema.

#### Why it matters

- it is high-authority language
- it appears where the schema teaches how to respond to absence
- it preserves direct abort grammar even after other exit-language cleanups

#### What this points toward

- a dedicated rewrite pass for `missing` responses
- replacement language should preserve invalidity without teaching the agent that stopping is itself the operative success condition

#### Problem being solved

- removing one of the strongest remaining abort grammars from the canonical layer

---

### 3. Plugin Trust Surfaces Are Placeholder-Grade

#### Gap

The Anarchy-AI plugin still exposes placeholder metadata for:

- author
- homepage
- repository
- developer
- privacy
- terms

#### Why it matters

- this is now user-facing delivery
- placeholder metadata makes a working plugin feel fake, unsafe, or abandoned
- trust failure at first contact poisons the runtime harness story immediately

#### What this points toward

- replace placeholder trust metadata before broader delivery

#### Problem being solved

- making the plugin look real enough to match its actual functionality

---

### 4. Delivery Docs Still Teach Deprecated Launch Paths

#### Gap

The intended delivery path is now direct launch of the bundled executable, but adjacent docs still teach the older:

- `cmd.exe /c ... start-anarchy-ai.cmd`

path as if it were current setup guidance.

#### Why it matters

- users and agents will follow the wrong path if it remains documented as current
- this recreates exactly the kind of deployment confusion the plugin is supposed to remove

#### What this points toward

- make the bundled runtime the primary documented path everywhere
- demote `start-anarchy-ai.cmd` to development helper / fallback language only

#### Problem being solved

- eliminating contradictory installation guidance

---

### 5. Harness Maturity Framing Is Stale

#### Gap

The repo still overuses:

- `scaffold`
- `first two harness tools`

even though three tools are implemented and the runtime/plugin path is already semi-functional.

#### Why it matters

- it makes the repo internally inconsistent
- it understates what now exists
- it trains later agents to reason from an older maturity state

#### What this points toward

- tighten stale `scaffold` language
- replace `first two` with the current three-tool surface

#### Problem being solved

- making delivery language match actual runtime capability

---

### 6. Anarchy-AI's User-Facing Promise Is Too Narrow

#### Gap

The plugin and skill still frame Anarchy-AI mostly as:

- schema-reality inspection
- gov2gov reconciliation plumbing

while underplaying:

- `compile_active_work_state`

which is the most novel bounded-operational-state contribution.

#### Why it matters

- the implementation is ahead of the description
- the harness can already do more than the copy suggests
- users and agents are not being taught the runtime framework promise clearly

#### What this points toward

- rewrite plugin-facing and skill-facing copy around the three-tool promise:
  - bounded active-work compilation
  - schema-reality evaluation
  - non-destructive gov2gov reconciliation

#### Problem being solved

- making Anarchy-AI legible as one coherent runtime framework harness rather than three disconnected helpers

---

### 7. The Product Architecture Sentence Is Missing

#### Gap

The repo still makes readers infer the relationship between:

- schema family
- AGENTS Heuristic Underlay
- Anarchy-AI

instead of stating it once, cleanly, in a central place.

#### Why it matters

- users should not have to reverse-engineer the product architecture
- agents will also infer different internal models if the relationship stays implicit

#### What this points toward

- add one canonical architecture sentence to central docs and delivery surfaces

#### Problem being solved

- reducing architectural ambiguity across repo, plugin, and harness surfaces

---

### 8. Skill Routing Blends Separate Result Axes

#### Gap

The skill currently compresses:

- `schema_reality_state`
- `possession_state`

into one sentence as if they were one result axis.

#### Why it matters

- this is exactly the kind of compressed ambiguity that later causes agent drift
- a careful reader can resolve it
- the point of the system is to reduce the need for careful reconstruction

#### What this points toward

- rewrite the skill so the routing logic distinguishes:
  - schema reality
  - integrity
  - possession

#### Problem being solved

- preserving bounded result semantics at the user-facing helper layer

---

### 9. Platform Story And Installation Policy Are Misaligned

#### Gap

The actual plugin bundle is Windows-first, but some docs still describe the harness as host-agnostic while the local plugin marketplace installs it by default.

#### Why it matters

- this creates a gap between packaging reality and delivery claims
- `host-agnostic` may be the long-term harness aspiration
- it is not the current packaged plugin reality

#### What this points toward

- state the current platform story explicitly at install and README surfaces:
  - Windows-first plugin delivery today
  - broader host-agnostic harness aspiration later

#### Problem being solved

- avoiding false portability signals at first contact

---

### 9A. Default Installation Solved Presence, Not Harness Ergonomics

#### Gap

Making Anarchy-AI `INSTALLED_BY_DEFAULT` solved the presence problem better than leaving it merely `AVAILABLE`.

That distinction matters because:

- `AVAILABLE` was not reliably making the harness present to the agent at startup
- the agent often needed explicit prompting before the plugin was effectively reachable
- even when reachable, the tool usage still felt like individual manual calls rather than one coherent harness surface

So the installation policy appears directionally correct, but it does not yet make Anarchy-AI feel like a true harness in use.

#### Why it matters

- default presence is necessary
- default presence is not sufficient
- a harness that is merely installed but still feels like scattered utilities has not yet achieved the intended runtime behavior

#### What this points toward

- keep `INSTALLED_BY_DEFAULT` as the likely correct installation policy
- treat the remaining problem as behavioral integration and invocation ergonomics rather than package presence
- improve:
  - startup legibility
  - skill routing
  - coherent default prompts
  - situations where the agent naturally reaches for Anarchy-AI without manual ceremony

#### Problem being solved

- distinguishing `present by default` from `operates like a harness`

---

### 9B. Delivery Should Prefer A Self-Contained Windows-First Runtime

#### Gap

The current delivery learning is pointing toward a simpler packaging model than the repo-local plugin-marketplace path:

- a self-contained `.exe` that carries the runtime payload is preferable
- delivery should stay heavily biased toward Windows for now
- after real installation testing, some form of self-signed certificate or comparable trust/signing story will likely be needed

This is not yet the finalized install architecture, but it is a stronger direction than copying full plugin directory structures into target repos.

#### Why it matters

- a single self-contained runtime is easier to install, move, verify, and reason about
- Windows-first bias matches the current packaged reality better than pretending to be broadly host-agnostic already
- installation trust will become a real user concern once the runtime is asked to cross repo boundaries or be installed outside source checkouts

#### What this points toward

- prefer a machine-level delivery model built around:
  - self-contained runtime executable
  - installer/bootstrap script
  - optional skills/config attachment
- keep the current plugin-marketplace path demoted to:
  - local test surface
  - packaging experiment
  - fallback reference path
- evaluate code-signing or at minimum a self-signed-cert story after installation behavior is tested in real environments

#### Problem being solved

- reducing packaging sprawl, improving install trust, and aligning the delivery system with the current Windows-first reality

---

### 9C. The Preferred Delivery Surface Is A Separate Setup EXE, Not A Script-First Bootstrap

#### Gap

The current repo-bootstrap lane works, but the preferred delivery shape is now clearer and partially delivered:

- `AnarchyAi.Setup.exe` now exists locally as the installer/bootstrap surface
- `AnarchyAi.Mcp.Server.exe` should remain the runtime surface

The current PowerShell bootstrap script is still useful, but it is no longer the preferred long-term user-facing install path.

#### Why it matters

- users should not need to read or invoke a script just to install a repo-local harness
- agents need a bounded machine-readable installer lane that is semantically equivalent to the current bootstrap path
- the MCP runtime should not absorb installer/update responsibilities that would blur its role or complicate self-replacement

#### What this points toward

- keep the separate Windows-first `Setup.exe`
- expand the no-argument GUI beyond the current minimal assess/install surface
- keep switch-driven launch as silent/JSON CLI behavior for agents and automation
- preserve alternate repo targeting through an explicit `/repo` override

#### Problem being solved

- replacing script-first delivery with a cleaner installer surface without forking the underlying bootstrap semantics

---

### 10. Claim Discipline Gap In Scratchpad #2

#### Gap

Scratchpad `#2` currently contains a stronger working claim than the surrounding evidence standard cleanly supports:

- that AI-Links can `reliably improve` future session trajectory before task context is known

#### Why it matters

- the surrounding section still keeps major validity questions open
- the stronger sentence upgrades a live hypothesis into a more settled claim than the current evidence appears to warrant

#### What this points toward

- soften the working claim until the benchmark/evidence base earns stronger language

#### Problem being solved

- keeping theory ambitious without letting the scratchpad outrun its own evidence standard

---

### 11. Narrative Schema Needs A Deeper Review Pass

#### Gap

The narrative schema has not yet received the same depth of review as governance, even though it appears to be carrying a different class of work:

- storytelling
- communication memory
- relationship history
- evolving interpretation across multiple parties

It likely needs a more careful review of:

- naming
- negation / exit grammar
- compression pressure
- what should be durable record versus session-local inference

#### Why it matters

- narrative is not just a lighter governance variant
- it is carrying a different kind of memory and interpretive burden
- lessons learned there should shape later decisions before the family expands again

#### What this points toward

- perform a dedicated narrative-schema review rather than assuming governance lessons transfer cleanly without inspection

#### Problem being solved

- preventing under-reviewed narrative design from quietly setting the pattern for later schema-family expansion

---

### 12. Journal / Accounting Capture Lane Is Emerging But Not Ready

#### Gap

There appears to be a real need for a more rigid cousin of narrative:

- quality accurate capture
- stronger factual/accounting discipline
- higher emphasis on stats and bounded state
- less emphasis on storytelling and communication flow

This may eventually imply a journal / accounting schema or a similarly bounded capture lane.

But that should not be treated as permission to add another schema immediately.

The narrative lessons are still incomplete.

#### Why it matters

- not all durable capture problems are the same
- storytelling / communication memory and stats / accounting capture are materially different
- forcing both into one surface would likely blur the design and weaken both

#### What this points toward

- preserve the distinction now
- do not expand the schema family yet
- learn from narrative first, then decide whether a separate rigid capture surface is truly warranted

#### Problem being solved

- preventing premature schema proliferation while still preserving the observed need for a more rigid factual capture model

---

### 13. Vision Capture Still Lacks A Structured Register

#### Gap

The repo preserves vision artifacts, but it still lacks a durable vision-register shape that can track:

- what the user asked for
- the exact human quote that mattered
- why the request qualified as vision
- which implementation surfaces were implicated
- how implemented the vision actually is
- how many known detractors still undermine delivery quality

Minimum desired properties:

- `vision_request`
- `human_quote`
- `qualifying_context`
- `surfaces_affected`
- `implementation_assessed_at_percent`
- `implementation_grade_detractor_count`

#### Why it matters

- current vision capture is still too artifact-shaped and not traceable enough
- durable user intent can get preserved in prose without becoming implementation-legible
- later agents can inherit the artifact without inheriting a clean view of implementation progress or detractors

#### What this points toward

- define a real vision-register model and file shape
- preserve both normalized request and exact human quote
- separate implementation progress from implementation detractors

#### Problem being solved

- making durable human direction traceable without collapsing it into loose prose

---

### 14. Vision Qualification And Detractor Controls Are Not Yet Harness Surfaces

#### Gap

The harness does not yet have bounded controls for:

- qualifying a prompt as vision
- capturing a vision entry
- attaching detractors to an existing vision item

Desired future helper surfaces:

- `qualify_as_vision`
- `capture_vision`
- `detract_from_vision_id_with_note`

#### Why it matters

- long prompts alone are not enough to distinguish durable vision from transient chat
- if capture stays ad hoc, later rebuilds or refactors can drift away from what the human actually constrained
- implementation quality is not binary, so vision needs a detractor lane instead of only `implemented / not implemented`

#### What this points toward

- define bounded qualification rules
- keep long human prompts as a cue, not as the sole criterion
- preserve exact quotes alongside normalized structure
- add later harness support for detractor notes against stable `vision_id`s

#### Problem being solved

- preventing durable product direction from being lost, misqualified, or flattened during later implementation work

---

### 15. Harness Lifecycle Needs Install-State And Doctor/Repair Surfaces

#### Gap

The current harness delivery story still leans too much on:

- setup success messages
- copied files
- plugin visibility
- prose instructions

Those are useful evidence, but they are not lifecycle truth.

ECC demonstrates a more durable pattern worth adapting without copying its product philosophy:

- declared install plans
- target adapters
- install-state records
- doctor/status diagnostics
- repair actions
- catalog or contract validation

#### Why it matters

- current Codex plugin behavior has changed enough that plugin visibility cannot be treated as stable proof
- a single setup executable is useful as a carrier, but dangerous if it becomes the only authority about what happened
- agents need one easy diagnostic path instead of reconstructing state from plugin files, marketplaces, config, runtime paths, and chat history
- future install lanes should be provable without relying on a currently working plugin UI

#### What this points toward

- add a versioned install-state ledger written by setup
- add a read-only status/doctor command that compares declared install intent against observed destination state
- add a bounded repair command that can restore only Anarchy-owned surfaces from source truth
- keep target adapters responsible for Codex, Claude Code, Claude Desktop, and later Cursor translation
- keep the setup executable as delivery/operator surface, not the sole install truth authority

#### Current source status

- Implemented in setup source:
  - `/status`
  - `/doctor`
  - `/selfcheck`
  - `/self-check`
  - versioned `.anarchy-ai/install-state.json` writing on install/update
  - `anarchy.install-state.v2` target/workspace separation so user-profile runtime state does not collapse into the last repo/schema target
  - managed-operation recording for setup-owned surfaces
  - `install_state` reporting in setup JSON
  - status-mode repair guidance for missing or drifted install-state records
- Still pending:
  - full declared install-plan output before apply
  - a bounded repair command that rematerializes only Anarchy-owned surfaces
  - operation-level doctor comparisons against every recorded managed operation
  - catalog validation beyond the current embedded payload/path-canon checks
  - truth-matrix promotion tests for host-specific surfacing after install

#### Problem being solved

- moving from "files exist and setup said ready" to "declared intent, observed state, drift, and repair path are all visible"

---

### 16. Schemas Cannot Self-Fulfill

#### Gap

Some product language and adoption flows can still imply that schema presence or startup text completes the behavior the schema describes.

That is wrong.

Schemas can:

- make terrain legible
- provide vocabulary
- reduce composition depth
- route attention
- preserve recovery residue

Schemas cannot, by themselves:

- prove install state
- keep an active proof lane alive
- verify config materialization
- prevent local exit-condition failures
- guarantee host plugin surfacing

#### Why it matters

- treating schema presence as fulfillment recreates the same failure class as treating config acknowledgement as config materialization
- agents can still locally decide they have enough to stop unless active state and next actions are anchored outside narration
- migrations can appear complete while missing surfaces or unverified schema reality remain

#### What this points toward

- every schema adoption claim should name the observable state that closes it
- active proof work needs runtime-adjacent state when continuation matters
- schema-reality outputs should keep copy, materialization, integrity, and adoption axes separate
- setup and runtime tools should make the next correct move obvious instead of requiring agents to infer it from broad rules

#### Problem being solved

- preventing schema language from being mistaken for schema fulfillment

---

### 17. Plugin Adapter Breakage Must Not Drive Core Harness Truth

#### Gap

The installed Codex plugin surface is currently out of date and incompatible with current Codex behavior while a separate adapter repair is underway.

The core harness docs and implementation direction still mention plugin delivery enough that later agents may accidentally treat plugin state as harness truth.

#### Why it matters

- plugin visibility can break because of host indexing, app-version behavior, cache state, manifest parsing, or marketplace registration
- those failures are real delivery bugs, but they do not invalidate the source contracts, runtime model, or schema/harness doctrine
- conflating adapter breakage with core truth leads to noisy rewrites and half-migrations

#### What this points toward

- treat plugin compatibility as a host-adapter lane with its own proof tests
- keep MCP/runtime contracts, setup lifecycle, and schema materialization work independent from plugin UI state
- update truth docs whenever a host update invalidates prior proven plugin observations
- do not use plugin resources/templates discovery as a presence check unless that endpoint is explicitly implemented

#### Problem being solved

- preserving harness source progress while current host plugin mechanics are repaired separately

---

### 18. Schema-Carried Narrative Artifact Lanes Must Materialize Through Harness/Installer Surfaces

#### Gap

`AGENTS-schema-narrative.json` describes the narrative register and record shape, but the product previously relied on agents to infer `.agents/anarchy-ai/narratives` paths from schema text and field reports.

#### Why it matters

- a schema-carried artifact lane that does not travel with the installer recreates the schema self-fulfillment failure class
- agents may create incompatible narrative paths or one-off arc files even when they are trying to follow Anarchy
- gov2gov was checking the narrative schema file without checking the narrative register surface implied by that schema

#### What this points toward

- installer payloads should carry concrete templates for schema-carried artifact families
- gov2gov should inventory and non-destructively seed the minimum missing artifact surfaces when the schema is present or planned
- future narrative capture tools should write through the same template/register lane instead of inventing storage

#### Current patch state

- `AA-BUG-025` tracks the defect and acceptance criteria
- `templates/narratives/register.template.json` and `record.template.json` now travel in the plugin bundle
- `run_gov2gov_migration` now reports `narrative_arc_structure` and seeds missing register/projects surfaces in `non_destructive_apply`

#### Problem being solved

- turning narrative/arc from schema-only implication into a carried and harness-materialized surface

---

### 19. Build Prerequisites Must Stay Out Of Synced Workspaces

#### Gap

Repo-local install language drifted enough that .NET SDK/runtime placement could be confused with plugin bundle placement.

Repo-local Anarchy installation may write an Anarchy-owned plugin bundle under `plugins/anarchy-ai`, but build prerequisites and package caches are not repo truth.

#### Why it matters

- SDK/runtime folders and package caches create large, high-churn file trees
- OneDrive-backed workspaces are especially vulnerable to sync noise, partial file state, and corruption when toolchains are placed under the repo
- target repos do not need repo-local .NET because `AnarchyAi.Setup.exe` is self-contained for install

#### What this points toward

- keep .NET SDK/runtime prerequisites in non-workspace user/machine-local lanes such as `%USERPROFILE%\.dotnet`, `%LOCALAPPDATA%`, or `C:\Program Files\dotnet`
- keep restore scratch, NuGet caches, and publish intermediates outside repo and synced workspace trees
- make build helpers reject repo-local SDK paths instead of merely documenting the rule

#### Current patch state

- `AA-BUG-026` tracks the defect and acceptance criteria
- `build-self-contained-exe.ps1` rejects resolved `.NET SDK` paths inside the source workspace
- setup/repo install docs now state that repo-local install is plugin-bundle placement, not SDK placement

#### Problem being solved

- keeping repo-local deployment useful without turning the repo or OneDrive workspace into toolchain/cache storage

---

## Current Priority Order

Highest practical friction:

1. install-state / doctor / repair lifecycle gap
2. schema self-fulfillment gap
3. schema-carried narrative artifact materialization
4. build prerequisite/cache placement outside synced workspaces
5. plugin adapter breakage contaminating core harness truth
6. plugin trust surfaces
7. deprecated launch-path documentation
8. stale maturity framing
9. missing architecture sentence

Highest canonical consistency pressure:

10. governance residual exit grammar
11. required-field `missing` abort grammar
12. skill result-axis ambiguity

Evidence / theory discipline:

13. scratchpad working-claim strength

Platform clarity:

14. Windows-first vs host-agnostic delivery language

Schema-family review pressure:

14. dedicated narrative-schema review

Future expansion guardrail:

14. preserve the journal/accounting capture distinction without introducing a new schema yet

---

## Use

When a later change is made, this document should answer:

- what gap was being closed
- why that change existed
- what earlier failure, ambiguity, or delivery problem it was solving

