# Anarchy-AI Documentation-Discovered Bug Register

Purpose:

- keep a durable register of bugs or hidden assumptions exposed by the code documentation pass
- separate "we saw this while documenting" from "we already fixed this"
- force uncertain behavior to stay visible until it is either proven true or repaired

Rule:

- add an entry when documenting a function, class, method, or script exposes a mismatch between stated intent and implemented behavior
- mark whether each entry is a proven fact, a code-level assumption, or an environment-level assumption
- do not silently collapse these findings into comments alone

Documentation completeness closure rule:

- do not call a change documentation-complete unless every non-generated executable surface is documented inline
- active requirements and dependency docs must match current code, manifest, and install-lane truth
- historical docs may retain retired identities and paths only when they are explicitly framed as historical or cleanup evidence
- the documentation-truth audit must pass in the same repo state being described as complete

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

### AA-DOCBUG-005: Documentation completeness was claimed before active docs and wrappers were fully reconciled

- Status: patched local
- Discovery type: proven fact
- Surface:
  - `docs/ANARCHY_AI_SETUP_EXE_SPEC.md`
  - `docs/ANARCHY_AI_REPO_INSTALL_PROCESS.md`
  - `docs/ANARCHY_AI_HARNESS_ARCHITECTURE.md`
  - `harness/server/README.md`
  - `harness/setup/scripts/publish-anarchy-ai-setup.ps1`
  - `plugins/anarchy-ai/scripts/start-anarchy-ai.cmd`
- What documentation exposed:
  - the core code paths had received inline comments, but several active docs and wrapper surfaces still described stale plugin identities, stale user-profile paths, or lacked inline operational documentation
- Why it matters:
  - a completion claim can outrun repo truth if it is not backed by a repo-wide audit over active docs and non-generated executable surfaces
- Investigation target:
  - keep the documentation-truth audit in the build and test loop and treat its passing state as part of any future completeness claim

## 2026-04-15

### AA-DOCBUG-006: Human cleanup missed legacy installed bundles and used an unsafe config rewrite path

- Status: patched local
- Discovery type: proven fact
- Surface:
  - `plugins/anarchy-ai/scripts/remove-anarchy-ai.ps1`
  - `plugins/anarchy-ai/scripts/remove-anarchy-ai-human.ps1`
- What documentation exposed:
  - the human click-once cleanup touched shared `~/.codex/config.toml` through a regex rewrite path that could strip unrelated sections after the owned MCP block
  - the same cleanup missed legacy installed home-local bundles such as `~/.codex/plugins/anarchy-ai-herringms`, so plugin removal looked successful while the live bundle remained in place
  - Anarchy-only marketplace files were rewritten to empty branded marketplace shells, which left visible marketplace sections behind instead of retiring the marketplace cleanly
- Why it matters:
  - a human-facing cleanup path must prefer bounded, legible removal over broad shared-config mutation
  - missed legacy bundles and empty marketplace shells make cleanup appear broken and undermine trust in the installer/removal story
  - rewriting shared config unsafely can affect unrelated trust or host settings
- Investigation target:
  - keep shared Codex config untouched by default
  - detect current and legacy owned plugin roots before retirement
  - retire Anarchy-only marketplace files after backup instead of leaving empty marketplace shells behind

### AA-DOCBUG-007: Generated plugin-facing JSON was valid text on disk but still invalid for Codex because it carried a UTF-8 BOM

- Status: patched local
- Discovery type: proven fact
- Surface:
  - `harness/setup/scripts/build-self-contained-exe.ps1`
  - `plugins/anarchy-ai/scripts/bootstrap-anarchy-ai.ps1`
  - `plugins/anarchy-ai/.codex-plugin/plugin.json`
  - `plugins/anarchy-ai/.mcp.json`
  - `plugins/anarchy-ai/schemas/schema-bundle.manifest.json`
- What documentation exposed:
  - the repo treated manifest generation as complete because the files existed and parsed locally, but the generated plugin-facing JSON still carried a UTF-8 byte-order mark from PowerShell's default UTF-8 writer behavior
  - Codex plugin detail loading appears stricter than the repo's local JSON readers and can reject those files as missing or invalid even when the path is correct
- Why it matters:
  - "file exists" was not enough proof of a healthy Codex plugin payload
  - without a no-BOM rule, the repo could repeatedly ship manifests that look healthy in local checks but fail in the real host UI
- Investigation target:
  - keep the no-BOM output path in the build and bootstrap writers
  - keep the UTF-8-without-BOM regression test in the setup test suite
