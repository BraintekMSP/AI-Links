# AGENTS Schema Family Triage

Read this document FIRST before loading any AGENTS-schema-*.json file.

This document routes to the correct schema. Load only one schema into context.

---

## Before You Ask Any Questions

The human in front of you may:

- **Use buzzwords that suggest more expertise than they have.** Terms like "governance," "migration," "narrative," "scope" may come from a blog post, a sales pitch, or a half-remembered conversation. Verify understanding in the human's own words before matching to a schema.

- **Understand the problem deeply but lack the vocabulary.** A person who says "I keep losing track of what clients told us" is describing the narrative schema's problem space perfectly, even though they said none of its terms. Listen for the problem shape, reward the attempt to describe it.

- **Have a completely different life perspective than the current context window.** The human may be a technician, a salesperson, an accountant, a solo creator, a manager. Their framing of the problem will reflect their world. Translate their perspective into schema terms — the translation is the agent's job.

- **Overstate certainty to move faster.** When humans say "I know what I need" or "just set it up," they may be masking uncertainty with urgency. Slow down. Ask one clarifying question before committing to a schema. The cost of loading the wrong schema is much higher than the cost of one question.

- **Understate what they know.** "I have no idea what I'm doing" may come from someone who has spent years thinking about this problem and simply lacks the technical vocabulary. Treat uncertainty as an invitation to explore, ask what they already know.

---

## The Routing Questions

Ask these in order. Stop as soon as you have a clear answer. One resolved question is enough.

### Question 1: What are you trying to accomplish?

Listen for the shape of the answer, not specific words.

| If the answer sounds like... | Route to... |
|---|---|
| "I have one thing to get done" / "I need to finish this project" / "just help me build X" | **AGENTS-schema-1project.json** |
| "I need to track what happened with a client" / "we keep losing context when people leave" / "nobody remembers why we did this" | **AGENTS-schema-narrative.json** |
| "We have multiple teams working on this" / "I need rules" / "ownership is a mess" / "things keep breaking because nobody knows who owns what" | Likely governance — but continue to Question 2 before loading |
| "We already have governance but it's wrong" / "we're replacing our current system" / "the old docs don't match the new structure" | **AGENTS-schema-gov2gov-migration.json** |

**Important:** If the answer points toward governance, do NOT load governance yet. Continue to Question 2.
Gov2gov is routinely missed at this stage because "I need governance" sounds like a first-time-governance-authoring answer, but the workspace may already have authority that must be reconciled — check before routing.

### Question 2: Does this workspace already have existing authority?

This question must be asked before choosing between governance and gov2gov. "Existing authority" covers any form of current workspace direction. It includes:

- Dockerfiles, configs, READMEs, or docs that currently define how work is done here
- Conventions, naming patterns, or folder structures that agents or team members already follow
- Previous AGENTS files, prompts, or operating docs from an earlier system
- Informal rules that live in someone's head or in scattered notes

| If... | Route to... |
|---|---|
| No existing authority of any kind — truly empty workspace or brand new project | **AGENTS-schema-governance.json** (first-time-governance-authoring mode) |
| Some existing authority exists — docs, configs, conventions, or informal rules that carry meaning the new governance must absorb | **AGENTS-schema-gov2gov-migration.json** — any existing authority, formal or informal, is still authority that must be reconciled through gov2gov |
| Existing authority is minimal and the human confirms it can be safely ignored | **AGENTS-schema-governance.json** (first-time-governance-authoring mode — but document what was deliberately ignored) |

When the answer from Question 1 pointed toward 1project or narrative (away from governance), continue to Question 3.

### Question 3: How many people or agents need to stay aligned on this?

This question resolves remaining ambiguity between 1project and narrative.

| If... | Route to... |
|---|---|
| One person or agent, working alone on a bounded task | **AGENTS-schema-1project.json** |
| Multiple people or agents, but the subject is a relationship or evolving story, not code | **AGENTS-schema-narrative.json** |
| Multiple people or agents working on shared code or operations | Return to Question 2 — this is a governance case |

**1project posture:** Move to governance if your intent is a generalized solution. `1project` is arguably better for limited projects than basic prompting is, but where it shines, you're likely already done before implementing it.

---

## After Routing: Load ONE Schema

Load only the schema you routed to. Siblings stay out of the context window. The sibling-isolation rule in every schema exists because co-loaded schemas cause conflation — field names that look similar but mean different things bleed into each other.

When you later discover the wrong schema was loaded, stop and re-triage. Re-routing is cheaper than adapting a schema to a purpose it was built for something else.

---

## On Exit Conditions

Every schema except governance has exit conditions that route to a different schema when the current one is outgrown. These are important. They are also dangerous.

**The danger:** When work gets difficult partway through — when step g of a 10-step process hits resistance — the exit condition to another schema can look like the most efficient path forward. The agent is not being lazy. It is doing what statistical optimization tells it to do: the exit condition is a well-defined destination with a known path, and the remaining work in the current schema is uncertain. The exit condition *appears to be progress* from inside the context window, even when it is actually abandonment.

**The rule:** An exit condition is a signal that the problem genuinely changed shape. Before triggering an exit condition, the agent must answer:

1. **Has the problem actually changed?** If the goal shifted, rules emerged, or multiple scopes appeared, the exit is legitimate. If the current work is just hard, the exit is an escape.
2. **Would the destination schema actually help?** Loading governance because 1project feels hard adds overhead to already-difficult work — the destination must solve a real structural gap.
3. **Is the human aware this is happening?** Exit conditions use report-to-human for a reason. The human must understand that the schema is changing — "moving to the next step" is insufficient framing.

When any of these three answers is unclear, the exit condition is premature. Continue in the current schema.

---

## On Undisclosed Remaining Work

A related pattern to exit-condition escape: agents defer mandatory work through selective disclosure. The agent finishes the visible core and pauses. The human sees a natural stopping point and assumes the work is complete because the remaining steps were never surfaced.

This happens most often during migration and governance setup. The agent finishes the core file family and pauses. Conflict-surface reconciliation, historical labeling, measurement wiring, and archive classification are still required by the schema contract, but the agent surfaces only what was completed. The human would have said "keep going" if the remaining work had been visible.

**The rule:** Before any pause, exit, or completion claim, the agent must enumerate the entire migration plan as the schema contract defines it — the full plan with status per step, showing what was done, what remains, and what the contract still requires in one view. The human decides what gets deferred. An incomplete enumeration is a contract violation.

---

## On Human Uncertainty Throughout

This triage document handles the initial routing. Human uncertainty continues throughout every schema's lifecycle:

- **Humans will say "yes" when they mean "I think so."** Any field marked ask-if-ambiguous exists because the schema designers expected this. Use it.
- **Humans will approve things they partially understand to keep things moving.** report-to-human is a real checkpoint. When the human's approval feels too fast, ask what they understood about what they just approved.
- **Humans will forget what they decided.** This is human memory doing what human memory does. It is the problem the entire schema family exists to solve. The DECISION-Record (governance), known-decisions (narrative), and pitfalls (all schemas) exist so that "did we already decide this?" has an answer.
- **Humans will resist changing something they built.** This is rational attachment to invested effort. The distribution-and-realtime-copies concept in the gov2gov schema addresses this directly: the system is portable, the instance is local, protecting the system means protecting the distribution copy, the instance can evolve.

---

## Quick Reference

| Schema | Problem shape | Smallest signal |
|---|---|---|
| 1project | One goal, one deliverable, context must survive between sessions | "I just need to get this done and have the next person be able to pick it up" |
| Narrative | Evolving story, multiple parties, tribal knowledge at risk | "Nobody remembers why we did it this way" |
| Governance | Multiple scopes, rules, ownership, coordination | "Who owns this?" or "We keep breaking each other's work" |
| Gov2Gov | Any existing authority (formal or informal) that the new governance must absorb rather than ignore | "There's already a way things work here" or "we have docs and conventions but need real governance" or "the old system doesn't match what we're doing" |
