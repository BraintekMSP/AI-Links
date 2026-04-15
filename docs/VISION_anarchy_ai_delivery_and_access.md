# Vision Artifact - Anarchy-AI Delivery, Compatibility, And Accessibility

source-note: captured from the April 12, 2026 prompt series that defined host compatibility, user and agent accessibility, repo bootstrap, and environment adoption expectations
scope-statement: define how Anarchy-AI should be delivered, exposed, and assessed across User, Agent, and Environment without coupling the harness core to one host or one installation model
what-done-looks-like-at-this-scope: the repo carries a stable delivery and accessibility vision that makes User, Agent, and Environment responsibilities explicit, identifies Codex and Claude as first-class compatibility targets, preserves repo bootstrap as the first install lane, and keeps machine-level rollout as a later extension instead of a rewrite
commitments-that-must-survive-scope-evolution:
- Codex and Claude are the first-class compatibility targets for the harness; Cursor remains compatibility-ready rather than first-class in v1.
- MCP remains the common callable transport layer for shared contracts and must stay usable across hosts.
- App Server remains the Codex-native lifecycle adapter and must not become the source of canonical harness logic.
- SDK remains a valid and important programmatic control surface for orchestration, reflection workflows, bootstrap helpers, and preflight control, but it is not the canonical logic layer.
- Repo bootstrap is the first install path; the initial delivery story must provide one obvious assess/install lane before machine-level rollout is attempted.
- Machine-level install and managed-device rollout for RMM/Immybot are preserved as future-compatible directions and must not be blocked by repo-bootstrap choices.
- The user may address the harness directly for install/bootstrap, preflight, gap assessment, schema reality, and later reflection, but the harness should remain primarily agent-facing for normal work.
- The default agent rule is preflight-first for complex changes; relaxing this later is safer than imposing it later.
- The environment must be able to answer explicit installation, runtime, schema, and adoption questions, including whether the runtime is present, callable, registered, aligned, and fully adopted.
- Full adoption means more than installation: runtime present, host adapter registered, preflight callable, schema bundle available, repo startup surfaces aligned, agent-facing instructions present where supported, and gap assessment returning bounded output.
- Host-native install suggestion chips are opportunistic and must not be treated as a safe architectural dependency.
- The harness should feel like one coherent surface rather than a pile of exposed utilities; default presence alone is not enough.
- Windows-first runtime delivery is the honest current packaged posture and should remain explicit until a broader host/runtime story is actually proven.
