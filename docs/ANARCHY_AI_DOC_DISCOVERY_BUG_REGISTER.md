# Anarchy-AI Documentation-Discovered Bug Register

Purpose:

- keep a durable register of bugs or hidden assumptions exposed by the code documentation pass
- separate "we saw this while documenting" from "we already fixed this"
- force uncertain behavior to stay visible until it is either proven true or repaired

Rule:

- add an entry when documenting a function, class, method, or script exposes a mismatch between stated intent and implemented behavior
- mark whether each entry is a proven fact, a code-level assumption, or an environment-level assumption
- do not silently collapse these findings into comments alone

## 2026-04-14

### AA-DOCBUG-001: Home-local recovery lane does not carry the setup executable

- Status: open
- Discovery type: proven fact
- Surface:
  - `docs/ANARCHY_AI_SETUP_EXE_SPEC.md`
  - `plugins/anarchy-ai/README.md`
  - `harness/setup/scripts/build-self-contained-exe.ps1`
  - installed home-local bundle under `~/.codex/plugins/anarchy-ai`
- What documentation exposed:
  - the repo describes `AnarchyAi.Setup.exe` as the bounded repair and delivery lane
  - the installed home-local plugin bundle currently does not contain `AnarchyAi.Setup.exe`
- Why it matters:
  - recovery, rebootstrap, and schema-truing are not self-contained inside the home-local install
  - another agent or host cannot assume the installed home-local bundle carries its own immutable recovery artifact
- Investigation target:
  - decide whether the setup executable must be published into the installed home-local bundle
  - if not, document the actual immutable recovery location and lifecycle explicitly

### AA-DOCBUG-002: Runtime marketplace inspection assumes repo-local and user-profile marketplace paths stay identical

- Status: open
- Discovery type: code-level assumption
- Surface:
  - `harness/server/dotnet/Program.cs`
- What documentation exposed:
  - `HarnessInstallDiscovery.TryResolveMarketplacePluginRoot` and `HarnessGapAssessor.InspectMarketplaceAtRoot` resolve marketplace paths with `AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath` even when inspecting the user-profile root
  - this currently works only because both lanes presently use `.agents/plugins/marketplace.json`
- Why it matters:
  - if repo-local and user-profile marketplace paths ever diverge, runtime discovery and health inspection will drift silently
- Investigation target:
  - use lane-specific marketplace constants in runtime discovery and inspection instead of relying on shared current shape

### AA-DOCBUG-003: Legacy custom-MCP helpers remain live in setup code after readiness moved to plugin-marketplace-first

- Status: open
- Discovery type: proven fact
- Surface:
  - `harness/setup/dotnet/Program.cs`
- What documentation exposed:
  - setup still carries `EnsureCodexCustomMcpRegistration` and `InspectCodexCustomMcpConfiguration`
  - current Codex readiness and install guidance are now plugin-marketplace-first, with custom MCP treated as fallback/debug only
- Why it matters:
  - dormant legacy code can drift, confuse later maintainers, or accidentally be reintroduced as active truth
- Investigation target:
  - decide whether these helpers should be removed, isolated into a legacy-only path, or explicitly covered by tests and docs as non-primary behavior

### AA-DOCBUG-004: Retirement script treated shared marketplace registries as disposable files

- Status: patched local
- Discovery type: proven fact
- Surface:
  - `plugins/anarchy-ai/scripts/remove-anarchy-ai.ps1`
  - `docs/ANARCHY_AI_PLUGIN_README_SOURCE.md`
  - `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md`
- What documentation exposed:
  - the retirement script quarantined a live `marketplace.json` when it contained only Anarchy-AI entries
  - Codex marketplace guidance treats `marketplace.json` as a shared plugin registry whose `plugins[]` list may contain one or many entries
- Why it matters:
  - retiring the live registry file is broader than necessary and can hide the real ownership rule for shared marketplace state
  - safe retirement should remove only Anarchy-AI entries while leaving non-Anarchy entries and the live registry surface intact
- Investigation target:
  - keep validating that rewrite-in-place plus quarantined backup remains the correct ownership model for repo-local and user-profile marketplaces as Codex plugin behavior evolves
