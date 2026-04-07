# AGENTS Schema Family Triage

Read this document FIRST before loading any AGENTS-schema-*.json file.

This document routes to the correct schema. Do not load all schemas into context. Load one.

---

## Before You Ask Any Questions

The human in front of you may:

- **Use buzzwords that suggest expertise they do not have.** Terms like "governance," "migration," "narrative," "scope" may come from a blog post, a sales pitch, or a half-remembered conversation. Do not assume the human understands the schema system because they used a word that appears in it. Ask what they mean in their own words before matching to a schema.

- **Understand the problem deeply but lack the vocabulary.** A person who says "I keep losing track of what clients told us" is describing the narrative schema's problem space perfectly, even though they never said "tribal knowledge" or "definition-of-fixed." Do not penalize imprecise language. Listen for the problem shape, not the terminology.

- **Have a completely different life perspective than the current context window.** The human may be a technician, a salesperson, an accountant, a solo creator, a manager. Their framing of the problem will reflect their world, not the schema's world. Translate their perspective into schema terms — do not ask them to translate into yours.

- **Overstate certainty to move faster.** When humans say "I know what I need" or "just set it up," they may be masking uncertainty with urgency. Slow down. Ask one clarifying question before committing to a schema. The cost of loading the wrong schema is much higher than the cost of one question.

- **Understate what they know.** "I have no idea what I'm doing" may come from someone who has spent years thinking about this problem and simply does not know how to express it in technical terms. Do not treat uncertainty as incompetence. Explore what they know before assuming they know nothing.

---

## The Routing Questions

Ask these in order. Stop as soon as you have a clear answer. Do not ask all of them if the first one resolves.

### Question 1: What are you trying to accomplish?

Listen for the shape of the answer, not specific words.

| If the answer sounds like... | Route to... |
|---|---|
| "I have one thing to get done" / "I need to finish this project" / "just help me build X" | **AGENTS-schema-1project.json** |
| "I need to track what happened with a client" / "we keep losing context when people leave" / "nobody remembers why we did this" | **AGENTS-schema-narrative.json** |
| "We have multiple teams working on this" / "I need rules" / "ownership is a mess" / "things keep breaking because nobody knows who owns what" | **AGENTS-schema-governance.json** |
| "We already have governance but it's wrong" / "we're replacing our current system" / "the old docs don't match the new structure" | **AGENTS-schema-gov2gov-migration.json** |

If the answer does not clearly match any of these, continue to Question 2.

### Question 2: How many people or agents need to stay aligned on this?

| If... | Route to... |
|---|---|
| One person or agent, working alone on a bounded task | **AGENTS-schema-1project.json** |
| Multiple people or agents, but the subject is a relationship or evolving story, not code | **AGENTS-schema-narrative.json** |
| Multiple people or agents working on shared code, rules, or ownership | **AGENTS-schema-governance.json** |

If still unclear, continue to Question 3.

### Question 3: Does a governance system already exist here?

| If... | Route to... |
|---|---|
| No — this is the first time any structure is being applied | **AGENTS-schema-1project.json** (start small, exit to governance when it outgrows) |
| Yes, but it is informal, undocumented, or in someone's head | **AGENTS-schema-governance.json** (greenfield mode — the governance exists conceptually but not as files) |
| Yes, and it is documented but needs to be replaced or reconciled with a new model | **AGENTS-schema-gov2gov-migration.json** |

---

## After Routing: Load ONE Schema

Load only the schema you routed to. Do not load siblings "for reference." The sibling-isolation rule in every schema exists because cross-contamination between schemas in the same context window causes conflation — field names that look similar but mean different things will bleed into each other.

If you later discover the wrong schema was loaded, stop and re-triage. Do not try to adapt the loaded schema to a purpose it was not built for.

---

## On Exit Conditions

Every schema except governance has exit conditions that route to a different schema when the current one is outgrown. These are important. They are also dangerous.

**The danger:** When work gets difficult partway through — when step g of a 10-step process hits resistance — the exit condition to another schema can look like the most efficient path forward. The agent is not being lazy. It is doing what statistical optimization tells it to do: the exit condition is a well-defined destination with a known path, and the remaining work in the current schema is uncertain. The exit condition *appears to be progress* from inside the context window, even when it is actually abandonment.

**The rule:** An exit condition is not a shortcut. It is a signal that the problem genuinely changed shape. Before triggering an exit condition, the agent must answer:

1. **Has the problem actually changed?** If the goal shifted, rules emerged, or multiple scopes appeared, the exit is legitimate. If the current work is just hard, the exit is an escape.
2. **Would the destination schema actually help?** Loading governance because 1project feels hard does not make the work easier — it adds overhead to work that was already difficult.
3. **Is the human aware this is happening?** Exit conditions are blocked-until human-confirms for a reason. The human must understand that the schema is changing, not just that the agent is "moving to the next step."

If the agent cannot clearly answer all three, the exit condition has not been met. Continue in the current schema.

---

## On Human Uncertainty Throughout

This triage document handles the initial routing. But human uncertainty does not end at triage. Throughout any schema's lifecycle:

- **Humans will say "yes" when they mean "I think so."** Any field marked ask-if-ambiguous exists because the schema designers expected this. Use it.
- **Humans will approve things they do not fully understand to keep things moving.** blocked-until human-confirms is not a rubber stamp. If the human's approval feels too fast, ask what they understood about what they just approved.
- **Humans will forget what they decided.** This is not a failure of the human. It is the problem the entire schema family exists to solve. The DECISION-Record (governance), known-decisions (narrative), and pitfalls (all schemas) exist so that "did we already decide this?" has an answer.
- **Humans will resist changing something they built.** This is rational attachment to invested effort. The distribution-and-realtime-copies concept in the gov2gov schema addresses this directly: the system is portable, the instance is local, protecting the system does not require protecting every instance.

---

## Quick Reference

| Schema | Problem shape | Smallest signal |
|---|---|---|
| 1project | One goal, one deliverable, need to not lose context between sessions | "I just need to get this done and have the next person be able to pick it up" |
| Narrative | Evolving story, multiple parties, tribal knowledge at risk | "Nobody remembers why we did it this way" |
| Governance | Multiple scopes, rules, ownership, coordination | "Who owns this?" or "We keep breaking each other's work" |
| Gov2Gov | Existing governance needs to be replaced or reconciled | "The old system doesn't match what we're actually doing anymore" |
