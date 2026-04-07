# AGENTS-schema-narrative.json

## The Problem

Every organization has critical knowledge that lives in people's heads. A client's history with your company. Why a vendor relationship works the way it does. What a specific employee has tried and failed at. Why a process exists in its current form. Who decided something three years ago, and whether that decision still holds.

This is tribal knowledge. It is the most valuable and least reliable information in any organization.

Documentation does not solve it. SOPs go stale. Changelogs track what changed, not why it mattered. Bug trackers track what is broken, not what the client experienced. CRMs track contacts and deals, not the story of the relationship. None of these tools answer the question that matters most when someone new picks up an account, joins a team, or inherits a process: **what actually happened here, and what do I need to know that nobody wrote down?**

The harder truth: humans operate on narratives. Narrative bias may be the strongest bias humans trend toward. Stripping narrative out of communication records — normalizing, summarizing, reducing to data points — does not produce a cleaner record. It produces a wrong one. The client's frustration is not a data point. It is a story, and the story is what predicts whether they stay or leave.

## What This Schema Does

AGENTS-schema-narrative.json is a protocol for building and maintaining structured narrative records. It does not produce markdown governance files like its siblings. It produces JSON data artifacts — structured records written and maintained by agents from human input.

It defines:

- **Record structure**: Every narrative record has the same shape regardless of entity type: header, record-state-at-review-open-and-close, entries (the log of what happened), open threads, known decisions, observed patterns, and a handoff note.
- **Entry scene structure**: Each entry captures not just what happened but who was involved (cast: owner, context-holder, requested-by, finalized-by), what the outcome meant to each party (definition-of-fixed), what constrained the result (technical and budgetary limitations, awaiting-external), how the relationship felt (sentiment-and-tension), and whether the story changed direction (narrative beats).
- **Edit modes**: Explicit governance over how records are modified — append (additive only), update (state changes on existing entries, old state preserved), compress (consolidation, highest risk, quarterly only, human-confirms required), and correct (error fix with attribution and preservation of original).
- **Compression with lossy-artifact-warning**: Compressed entries are explicitly lossy artifacts. Detail that was not promoted to a durable section (known-decisions, open-threads, observed-patterns, handoff-note) during authoring is accepted as lost after compression. This is the cost of compression and the incentive to author well. The never-drop list still governs what types of entries cannot be compressed away.
- **Criticality ranking**: What makes information load-bearing. Tribal knowledge is always highest criticality, regardless of how minor it looks. Medium escalates to high when sentiment-and-tension predicts churn.
- **Signal cues with false-positive-tracking**: Eight classes of passive signal that indicate something worth recording is happening — context truncation, cross-domain references, attribution gaps, implicit history, ownership ambiguity, stale open threads, definition divergence, and tribal flags. Signal cues will misfire. When they do, false positives are routed to review session patterns and trust in that cue class is reduced rather than the cue being disabled.
- **Capture workflows**: Named workflow patterns (technician mailbox, client email thread, ticket update, vendor exchange, internal handoff, review session) that map signal cues to specific operational contexts.
- **Cadence**: Monthly (iterative, no compress), quarterly (full review, compress-eligible), transient (one-off situations that cannot wait).
- **Register**: An index of all narrative records so nothing sprawls or goes stale silently.

## When to Use It

This schema is a plugin for any context where an entity has an evolving story and multiple parties need to stay aligned on what happened:

- **Client accounts**: Track relationship health, decisions, resolution alignment, sentiment
- **Vendor accounts**: Track dependency risk, commitments, technical limitations, external blockers
- **Employee workflows**: Surface training gaps, process friction, and frustration patterns
- **Internal processes**: Identify where workflows break down and why they evolved to their current shape
- **Personal projects**: Replace the memory of a solo creator working across long time gaps (a book, a side project, a research effort)
- **Any entity where tribal knowledge is the real documentation**

## When NOT to Use It

- The subject is code operations with rules and ownership — use governance
- The subject is a single focused project with one deliverable — use 1project
- The record does not involve multiple parties or an evolving story over time

## How the Solution Manifests

You hand an agent this schema and tell it: "The workspace you are in represents a tool or human workflow that has evolving data and may be interacted with by nearly every stakeholder involved. Build a captured narrative around the experience."

The agent reads the schema and:

1. Asks the human to describe the entity and its primary parties
2. Asks for existing fragments (emails, tickets, notes, memory) that contain context
3. Asks for a cadence declaration (monthly, quarterly, transient)
4. Scans fragments for signal cues before writing anything
5. Authors the header, reconstructs known decisions and open threads from fragments
6. Records observed patterns if identifiable
7. Writes a handoff note summarizing what was reconstructed and what could not be recovered
8. Registers the record

On subsequent sessions, the agent reads the last close state, declares an edit mode, scans new input for signal cues, and updates the record. Monthly reviews are iterative. Quarterly reviews assess every open thread and may compress low-criticality entries. Transient records are created and closed for one-off situations.

The output is JSON — not human-edited directly. Humans provide input, agents write the structured record. This prevents inconsistency and context leakage between record sets.

## Relationship to Siblings

- **Independent** — narrative does not exit to governance or 1project. It is a different tool solving a different problem.
- **References siblings** when work materializes from a narrative thread. A known-decision entry can carry a project-ref pointing to a 1project goal or a governance scope. Sibling schemas can reference narrative records back via the narrative-ref field on their AGENTS-hello.md.
- **Sibling isolation**: If another AGENTS-schema-*.json is present in the context window, do not merge, inherit, or cross-reference fields unless an explicit pointer field (narrative-ref, project-ref) declares the relationship.
- **Shares structural patterns** — record-state-at-review-open-and-close (analogous to governance's agent-session-open-close-measurement and 1project's project-state-when-work-starts-and-stops), observed patterns (good/bad), and the context-ordering-rule are structurally familiar across the family. These are operationally independent despite shared shape.

## Known Limitations

- **JSON isolation is declarable but not enforceable**: The schema declares that record sets must not cross-contaminate (a client record must not bleed into a vendor record in the same agent context). Whether the consuming system honors this depends entirely on implementation.
- **Signal cues will produce false positives**: Agents cannot reliably distinguish casual language ("again" as repetition vs. "again" as implicit history) in all cases. The false-positive-tracking mechanism reduces trust over time rather than disabling cues.
- **Compression is lossy**: Despite the never-drop list, compressed entries lose detail. If information was not promoted to a durable section during authoring, it is gone after compression. This is an accepted cost.
- **Cadence is recommended, not enforced**: If no one triggers the monthly or quarterly review, the record goes stale. The schema can detect stale threads but not a stale record overall.

## Key Design Decisions

- **Narrative is preserved, not stripped**: The schema works with human narrative bias rather than against it. Entries capture human language as-is.
- **JSON not markdown**: Records are data artifacts, not human-editable docs. This prevents context leakage between record sets and maintains structural consistency that narrative text alone cannot guarantee.
- **Compression is the highest-risk operation**: It gets its own governance — quarterly only, human-confirms, explicit never-drop list, lossy-artifact-warning. Compressed summaries are not authoritative for detail-level questions.
- **Tribal knowledge is always highest criticality**: Even when it looks minor. If only one person knows it, it is load-bearing by definition.
- **Signal cues detect, capture workflows contextualize**: The schema does not just store information — it teaches agents how to recognize that something worth recording is happening in the first place. False positives are expected and tracked.
- **Definition-of-fixed is the central tension**: A resolution is only real if the person who asked for it and the person who delivered it agree on what "fixed" means. The schema forces that comparison.
- **Cadence is a forcing function**: Even when nothing broke, the record gets touched. Monthly review surfaces drift. Quarterly review surfaces stale threads. The "seamless change that no one noticed" problem is addressed by making silence itself a signal.
- **Context compression is not trustworthy**: Acceptable compression in the current context window does not guarantee fidelity in future context. Durable state must be written to record sections, not held in context.
- **Bug reports and feature requests are not governed here**: Same boundary as siblings. The schema acknowledges they exist and explicitly tells agents to look elsewhere.
