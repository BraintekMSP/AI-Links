using Xunit;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AnarchyAi.Setup.Tests;

/// <summary>
/// Verifies that the setup engine keeps its pathing, help text, and JSON contracts aligned with the repo-authored install model.
/// </summary>
/// <remarks>
/// Purpose: catch regressions in install-lane labeling, generated help/disclosure text, and nested path output.
/// Expected input: repo files, generated plugin README output, and setup-engine operations invoked inside the test process.
/// Expected output: xUnit assertions only.
/// Critical dependencies: <see cref="SetupEngine"/>, <see cref="ProgramJson"/>, the generated plugin README, and the path-canon audit script.
/// </remarks>
public sealed class SetupEngineTests
{
    /// <summary>
    /// Confirms that user-profile marketplace registration points at the Codex home plugin directory instead of a legacy home path.
    /// </summary>
    /// <returns>No direct return value; the method asserts the generated relative path.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildPluginRelativePath(InstallScope, string?)"/> and the current Codex home-install canon.</remarks>
    [Fact]
    public void BuildPluginRelativePath_UserProfile_UsesCodexPluginsLane()
    {
        var relativePath = SetupEngine.BuildPluginRelativePath(InstallScope.UserProfile, null);

        Assert.Equal("./.codex/plugins/anarchy-ai", relativePath);
    }

    /// <summary>
    /// Confirms that the resolved user-profile plugin root lands under the Codex home plugin lane.
    /// </summary>
    /// <returns>No direct return value; the method asserts the resolved destination path.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.ResolvePluginRoot(InstallScope, string)"/> and the shared path canon.</remarks>
    [Fact]
    public void ResolvePluginRoot_UserProfile_UsesCodexPluginsLane()
    {
        var pluginRoot = SetupEngine.ResolvePluginRoot(InstallScope.UserProfile, string.Empty);

        Assert.EndsWith(Path.Combine(".codex", "plugins", "anarchy-ai"), pluginRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that the user-profile marketplace identity stays branded if Codex surfaces it directly in the UI.
    /// </summary>
    /// <returns>No direct return value; the method asserts the generated marketplace identifier.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildMarketplaceName(InstallScope, string?)"/> and the current marketplace identity canon.</remarks>
    [Fact]
    public void BuildMarketplaceName_UserProfile_UsesBrandedStableName()
    {
        var marketplaceName = SetupEngine.BuildMarketplaceName(InstallScope.UserProfile, null);

        Assert.Equal("anarchy-ai-user-profile", marketplaceName);
    }

    /// <summary>
    /// Confirms that repo-local marketplace identity is stable across devices for the same repo name instead of leaking path-local wording.
    /// </summary>
    /// <returns>No direct return value; the method asserts the generated repo-local marketplace identifier.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildMarketplaceName(InstallScope, string?)"/> and the repo-slug identity contract.</remarks>
    [Fact]
    public void BuildMarketplaceName_RepoLocal_UsesRepoSlugWithoutPathHash()
    {
        var marketplaceName = SetupEngine.BuildMarketplaceName(
            InstallScope.RepoLocal,
            Path.Combine(Path.GetTempPath(), "AI-Links"));

        Assert.Equal("anarchy-ai-repo-ai-links", marketplaceName);
    }

    /// <summary>
    /// Confirms that repo-local plugin bundles use the plain plugin path because the repo root already scopes the install.
    /// </summary>
    /// <returns>No direct return value; the method asserts the generated repo-local source path.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildPluginRelativePath(InstallScope, string?)"/> and the plain repo-local path contract.</remarks>
    [Fact]
    public void BuildPluginRelativePath_RepoLocal_UsesPlainPluginPath()
    {
        var relativePath = SetupEngine.BuildPluginRelativePath(
            InstallScope.RepoLocal,
            Path.Combine(Path.GetTempPath(), "AI-Links"));

        Assert.Equal("./plugins/anarchy-ai", relativePath);
    }

    /// <summary>
    /// Verifies that the install disclosure describes the Codex-native user-profile lane without reviving legacy custom-MCP wording.
    /// </summary>
    /// <returns>No direct return value; the method asserts disclosure content.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildInstallDisclosure(string, InstallScope, HostTargets)"/> and the generated install copy.</remarks>
    [Fact]
    public void BuildInstallDisclosure_UserProfile_DescribesPluginMarketplaceLane()
    {
        var disclosure = SetupEngine.BuildInstallDisclosure(string.Empty, InstallScope.UserProfile, HostTargets.Codex);

        Assert.Contains(@"~\.codex\plugins\anarchy-ai", disclosure);
        Assert.Contains(@"~/.agents/plugins/marketplace.json".Replace('/', Path.DirectorySeparatorChar), disclosure);
        Assert.DoesNotContain("Updates ~/.codex/config.toml with mcp_servers.anarchy-ai", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"~\plugins\anarchy-ai", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that Claude-only user-profile disclosure distinguishes the shared runtime payload from Codex marketplace registration.
    /// </summary>
    /// <returns>No direct return value; the method asserts disclosure wording.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildInstallDisclosure(string, InstallScope, HostTargets)"/> and host-target-specific install semantics.</remarks>
    [Fact]
    public void BuildInstallDisclosure_UserProfile_ClaudeOnly_DoesNotClaimCodexMarketplaceRegistration()
    {
        var disclosure = SetupEngine.BuildInstallDisclosure(string.Empty, InstallScope.UserProfile, HostTargets.ClaudeCode);

        Assert.Contains("shared runtime payload", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Codex marketplace registration is skipped", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Claude Code lane", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSTALLED_BY_DEFAULT in the current user profile marketplace", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that CLI help reports the same Codex-native user-profile lane as the disclosure surface.
    /// </summary>
    /// <returns>No direct return value; the method asserts help-text content.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildCommandLineHelp(string?)"/> and the current user-profile install model.</remarks>
    [Fact]
    public void BuildCommandLineHelp_UserProfile_DescribesPluginMarketplaceLane()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains(@"~\.codex\plugins\anarchy-ai", help);
        Assert.Contains("plugin marketplace lane", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("also registers mcp_servers.anarchy-ai", help, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that repo-local help does not describe the repo-local runtime as home-local.
    /// </summary>
    /// <returns>No direct return value; the method asserts help-text content.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildCommandLineHelp(string?)"/> and repo-local lane wording.</remarks>
    [Fact]
    public void BuildCommandLineHelp_RepoLocal_DescribesRepoLocalBundle()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains("repo-local plugin bundle + repo-local marketplace", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("home-local runtime + repo-local marketplace", help, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that the disclosure text reports both the five core tools and the single experimental test helper.
    /// </summary>
    /// <returns>No direct return value; the method asserts disclosure content.</returns>
    /// <remarks>Critical dependencies: generated disclosure text and the current tool-count contract.</remarks>
    [Fact]
    public void BuildInstallDisclosure_ReportsCoreAndTestToolCounts()
    {
        var disclosure = SetupEngine.BuildInstallDisclosure(string.Empty, InstallScope.UserProfile, HostTargets.Codex);

        Assert.Contains("5 core + 1 test harness tool", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUnderlayDisclosure_SeparatesRepoUnderlayFromRuntimeInstall()
    {
        var disclosure = SetupEngine.BuildUnderlayDisclosure(@"C:\repo");

        Assert.Contains("repo-underlay", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Does not add plugins\\anarchy-ai", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Does not create or update .agents\\plugins\\marketplace.json", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Does not register a plugin", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Refreshes stale portable root schema files", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamped .bak", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user-profile install", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CLI-only proving/debug lane", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSTALLED_BY_DEFAULT", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Codex plugin enable-state in", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that CLI help reports the same core-versus-test tool count described by the disclosure surface.
    /// </summary>
    /// <returns>No direct return value; the method asserts help content.</returns>
    /// <remarks>Critical dependencies: generated help text and the current tool-count contract.</remarks>
    [Fact]
    public void BuildCommandLineHelp_ReportsCoreAndTestToolCounts()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains("5 core + 1 test harness tool", help, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that the lifecycle status command is available through the expected agent-friendly aliases.
    /// </summary>
    /// <returns>No direct return value; the method asserts parser results.</returns>
    /// <remarks>Critical dependencies: <see cref="CliParser.Parse(string[])"/> and the setup lifecycle command vocabulary.</remarks>
    [Theory]
    [InlineData("/status")]
    [InlineData("/doctor")]
    [InlineData("/selfcheck")]
    [InlineData("/self-check")]
    public void CliParser_StatusAliases_MapToStatusMode(string statusSwitch)
    {
        var options = CliParser.Parse([statusSwitch, "/userprofile", "/silent", "/json"]);

        Assert.Equal(OperationMode.Status, options.Mode);
        Assert.Equal(InstallScope.UserProfile, options.InstallScope);
    }

    [Fact]
    public void CliParser_UnderlayRefreshApply_MapsToRuntimeFreeRepoLane()
    {
        var options = CliParser.Parse(["/underlay", "/refresh", "/apply", "/repo", "C:\\repo", "/silent", "/json"]);

        Assert.Equal(OperationMode.Underlay, options.Mode);
        Assert.Equal(InstallScope.RepoLocal, options.InstallScope);
        Assert.True(options.RefreshPortableSchemaFamily);
        Assert.True(options.ApplyChanges);
    }

    [Fact]
    public void GuiRepoUnderlayButtons_MapToRefreshPlanAndRefreshApply()
    {
        var plan = SetupForm.BuildGuiSetupOptions(
            OperationMode.Refresh,
            InstallScope.RepoLocal,
            "C:\\repo",
            HostTargets.None);

        Assert.Equal(OperationMode.Refresh, plan.Mode);
        Assert.True(plan.RefreshPortableSchemaFamily);
        Assert.False(plan.ApplyChanges);

        var apply = SetupForm.BuildGuiSetupOptions(
            OperationMode.Underlay,
            InstallScope.RepoLocal,
            "C:\\repo",
            HostTargets.None);

        Assert.Equal(OperationMode.Underlay, apply.Mode);
        Assert.True(apply.RefreshPortableSchemaFamily);
        Assert.True(apply.ApplyChanges);
    }

    [Fact]
    public void CliParser_RefreshSchemasAlias_IsPlanFirstUntilApplyIsExplicit()
    {
        var options = CliParser.Parse(["/install", "/repolocal", "/refreshschemas", "/repo", "C:\\repo", "/silent", "/json"]);

        Assert.Equal(OperationMode.Install, options.Mode);
        Assert.True(options.RefreshPortableSchemaFamily);
        Assert.True(options.RefreshSchemasAliasUsed);
        Assert.False(options.ApplyChanges);
    }

    [Fact]
    public void ProgramCliExitState_TreatsRefreshPlanAsSuccessful()
    {
        Assert.True(Program.IsSuccessfulCliBootstrapState("refresh_plan_ready"));
        Assert.True(Program.IsSuccessfulCliBootstrapState("ready"));
        Assert.True(Program.IsSuccessfulCliBootstrapState("source_authoring_bundle_ready"));
        Assert.False(Program.IsSuccessfulCliBootstrapState("bootstrap_needed"));
    }

    /// <summary>
    /// Confirms that CLI help exposes the lifecycle status lane instead of forcing agents to infer install state from files.
    /// </summary>
    /// <returns>No direct return value; the method asserts help content.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildCommandLineHelp(string?)"/> and lifecycle-status wording.</remarks>
    [Fact]
    public void BuildCommandLineHelp_ReportsLifecycleStatusLane()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains("/status", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("install-state", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/doctor", help, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that installing into a repo-local temp workspace writes a versioned install-state record.
    /// </summary>
    /// <returns>No direct return value; the method asserts setup output and the on-disk state file.</returns>
    /// <remarks>Critical dependencies: embedded setup payload resources, repo-local path resolution, and install-state writer.</remarks>
    [Fact]
    public void Execute_Install_RepoLocal_WritesInstallState()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();
        var result = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Install,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Contains("wrote_install_state", result.actions_taken);
        Assert.True(result.install_state.state_present);
        Assert.True(result.install_state.state_written);
        Assert.True(result.install_state.state_valid);

        var pluginRoot = SetupEngine.ResolvePluginRoot(InstallScope.RepoLocal, tempRepo.Path);
        var statePath = SetupEngine.ResolveInstallStatePath(pluginRoot);
        Assert.True(File.Exists(statePath), $"Expected install-state file at {statePath}");

        using var document = JsonDocument.Parse(File.ReadAllText(statePath));
        var root = document.RootElement;
        Assert.Equal("anarchy.install-state.v2", root.GetProperty("schema_version").GetString());
        Assert.Equal("install", root.GetProperty("setup_operation").GetString());
        Assert.Equal("repo_local", root.GetProperty("install_scope").GetString());
        Assert.Equal("codex-repo-local", root.GetProperty("target").GetProperty("id").GetString());
        Assert.Equal("project", root.GetProperty("target").GetProperty("kind").GetString());
        Assert.Equal(tempRepo.Path, root.GetProperty("target").GetProperty("root").GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(tempRepo.Path, root.GetProperty("workspace").GetProperty("root").GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.True(root.GetProperty("workspace").GetProperty("schema_targeted").GetBoolean());
        Assert.True(root.GetProperty("managed_operations").GetArrayLength() > 0);
        Assert.Equal(tempRepo.Path, root.GetProperty("workspace_root").GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("anarchy-ai", root.GetProperty("mcp_server_name").GetString());
    }

    /// <summary>
    /// Confirms that user-profile lifecycle state is keyed to the home install target, not the last repo used for schema seeding.
    /// </summary>
    /// <returns>No direct return value; the method asserts user-profile target/workspace separation.</returns>
    /// <remarks>Critical dependencies: install-state v2 shape and the ECC-inspired distinction between install target and workspace target.</remarks>
    [Fact]
    public void InspectInstallState_UserProfile_DifferentWorkspaceIsWarningNotInvalid()
    {
        using var pluginRoot = new TempDirectory();
        using var firstRepo = CreateTempRepo();
        using var secondRepo = CreateTempRepo();
        var statePath = SetupEngine.ResolveInstallStatePath(pluginRoot.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        var userRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var marketplacePath = Path.Combine(userRoot, ".agents", "plugins", "marketplace.json");
        var runtimePath = Path.Combine(pluginRoot.Path, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe");

        var state = new
        {
            schema_version = "anarchy.install-state.v2",
            written_at_utc = DateTimeOffset.UtcNow.ToString("O"),
            setup_operation = "install",
            install_scope = "user_profile",
            host_context = "codex",
            host_targets = new[] { "codex" },
            target = new
            {
                id = "codex-user-profile",
                kind = "home",
                root = userRoot,
                install_state_path = statePath,
                plugin_root = pluginRoot.Path,
                marketplace_path = marketplacePath,
                mcp_server_name = "anarchy-ai",
                runtime_path = runtimePath
            },
            workspace = new
            {
                root = firstRepo.Path,
                schema_targeted = true,
                schema_refresh_requested = false
            },
            managed_operations = new[]
            {
                new
                {
                    kind = "materialize_runtime",
                    surface = "mcp_runtime",
                    destination_path = runtimePath,
                    strategy = "copy_embedded_payload",
                    ownership = "managed"
                }
            }
        };
        File.WriteAllText(statePath, JsonSerializer.Serialize(state, ProgramJson.Options), Encoding.UTF8);

        var report = SetupEngine.InspectInstallState(
            new SetupOptions
            {
                Mode = OperationMode.Status,
                InstallScope = InstallScope.UserProfile,
                HostContext = "codex",
                HostTargets = HostTargets.Codex,
                RepoPath = secondRepo.Path,
                Silent = true,
                JsonOutput = true
            },
            "codex",
            secondRepo.Path,
            pluginRoot.Path,
            marketplacePath,
            runtimePath,
            stateWritten: false);

        Assert.True(report.state_valid);
        Assert.Empty(report.findings);
        Assert.Contains("last_workspace_target_differs_from_current_request", report.warnings);
        Assert.Equal("codex-user-profile", report.recorded_target_id);
        Assert.Equal(userRoot, report.recorded_target_root, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(firstRepo.Path, report.recorded_workspace_root, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that a later status run can read the install-state file written by install and keep the lifecycle report valid.
    /// </summary>
    /// <returns>No direct return value; the method asserts repeatable lifecycle inspection.</returns>
    /// <remarks>Critical dependencies: install-state writer, status-mode reader, and repo-local path comparison.</remarks>
    [Fact]
    public void Execute_Status_RepoLocal_ReadsInstallStateAfterInstall()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();
        engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Install,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        var status = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Status,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("status", status.setup_operation);
        Assert.True(status.install_state.state_present);
        Assert.True(status.install_state.state_valid);
        Assert.Empty(status.install_state.findings);
        Assert.DoesNotContain("install_state_missing", status.missing_components);
        Assert.Equal("use_preflight_session", status.next_action);
    }

    /// <summary>
    /// Confirms that status mode reports a missing install-state record as a repairable lifecycle gap.
    /// </summary>
    /// <returns>No direct return value; the method asserts bounded missing-state findings.</returns>
    /// <remarks>Critical dependencies: status-mode reader and missing-component routing for lifecycle gaps.</remarks>
    [Fact]
    public void Execute_Status_RepoLocal_ReportsMissingInstallState()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();

        var status = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Status,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.False(status.install_state.state_present);
        Assert.False(status.install_state.state_valid);
        Assert.Contains("install_state_missing", status.install_state.findings);
        Assert.Contains("install_state_missing", status.missing_components);
        Assert.Contains("run_install_to_write_install_state", status.safe_repairs);
    }

    [Fact]
    public void Execute_Underlay_SeedsPortableDisciplineWithoutRuntimeOrMarketplace()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();

        var result = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Underlay,
            InstallScope = InstallScope.RepoLocal,
            RepoPath = tempRepo.Path,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("underlay", result.setup_operation);
        Assert.Equal("repo_underlay", result.install_scope);
        Assert.Equal("none", result.registration_mode);
        Assert.False(result.runtime_present);
        Assert.False(result.marketplace_registered);
        Assert.False(result.host_config_modified);
        Assert.Contains("materialized_repo_underlay", result.actions_taken);
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, "AGENTS-schema-governance.json")));
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, ".agents", "anarchy-ai", "narratives", "register.json")));
        Assert.True(Directory.Exists(Path.Combine(tempRepo.Path, ".agents", "anarchy-ai", "narratives", "projects")));
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, "AGENTS.md")));
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, ".gitignore")));
        Assert.False(Directory.Exists(Path.Combine(tempRepo.Path, "plugins", "anarchy-ai")));
        Assert.False(File.Exists(Path.Combine(tempRepo.Path, ".agents", "plugins", "marketplace.json")));

        using var register = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRepo.Path, ".agents", "anarchy-ai", "narratives", "register.json")));
        var openThreads = register.RootElement.GetProperty("open-threads");
        Assert.Equal(2, openThreads.GetArrayLength());
        Assert.All(openThreads.EnumerateArray(), thread =>
        {
            Assert.StartsWith("ot-", thread.GetProperty("id").GetString(), StringComparison.Ordinal);
            Assert.Equal("consumer-workspace-owner", thread.GetProperty("owner").GetString());
            Assert.True(thread.TryGetProperty("auto-close-trigger", out _));
        });
    }

    [Fact]
    public void Execute_Underlay_LeavesExistingAgentsMdUnchanged()
    {
        using var tempRepo = CreateTempRepo();
        var agentsPath = Path.Combine(tempRepo.Path, "AGENTS.md");
        const string existingAgents = "# Existing authority\n";
        File.WriteAllText(agentsPath, existingAgents, Encoding.UTF8);

        var result = new SetupEngine().Execute(new SetupOptions
        {
            Mode = OperationMode.Underlay,
            InstallScope = InstallScope.RepoLocal,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Contains("agents_md_already_present_left_unchanged", result.actions_taken);
        Assert.Equal(existingAgents, File.ReadAllText(agentsPath));
    }

    [Fact]
    public void Execute_Refresh_IsPlanFirstAndDoesNotOverwriteUntilApply()
    {
        using var tempRepo = CreateTempRepo();
        var schemaPath = Path.Combine(tempRepo.Path, "AGENTS-schema-governance.json");
        const string localSchema = "{\"local\":true}";
        File.WriteAllText(schemaPath, localSchema, Encoding.UTF8);

        var plan = new SetupEngine().Execute(new SetupOptions
        {
            Mode = OperationMode.Refresh,
            InstallScope = InstallScope.RepoLocal,
            RepoPath = tempRepo.Path,
            RefreshPortableSchemaFamily = true,
            Silent = true,
            JsonOutput = true
        });

        Assert.True(plan.refresh_plan_only);
        Assert.Contains("AGENTS-schema-governance.json", plan.refreshed_files);
        Assert.Equal(localSchema, File.ReadAllText(schemaPath));
        Assert.Empty(plan.backup_files);

        var apply = new SetupEngine().Execute(new SetupOptions
        {
            Mode = OperationMode.Refresh,
            InstallScope = InstallScope.RepoLocal,
            RepoPath = tempRepo.Path,
            RefreshPortableSchemaFamily = true,
            ApplyChanges = true,
            Silent = true,
            JsonOutput = true
        });

        Assert.False(apply.refresh_plan_only);
        Assert.Contains("AGENTS-schema-governance.json", apply.refreshed_files);
        Assert.NotEqual(localSchema, File.ReadAllText(schemaPath));
        Assert.Contains(apply.backup_files, backup => backup.Contains("AGENTS-schema-governance.json", StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, apply.backup_files.Single(backup => backup.Contains("AGENTS-schema-governance.json", StringComparison.Ordinal)))));
    }

    [Fact]
    public void Execute_GuiRepoUnderlayApply_RefreshesStaleSchemasAndMaterializesUnderlay()
    {
        using var tempRepo = CreateTempRepo();
        var schemaPath = Path.Combine(tempRepo.Path, "AGENTS-schema-governance.json");
        const string localSchema = "{\"local\":true}";
        File.WriteAllText(schemaPath, localSchema, Encoding.UTF8);

        var options = SetupForm.BuildGuiSetupOptions(
            OperationMode.Underlay,
            InstallScope.RepoLocal,
            tempRepo.Path,
            HostTargets.None);
        var result = new SetupEngine().Execute(options);

        Assert.False(result.refresh_plan_only);
        Assert.Contains("AGENTS-schema-governance.json", result.refreshed_files);
        Assert.Contains("refreshed_portable_schema_family_from_embedded_payload", result.actions_taken);
        Assert.Contains("materialized_repo_underlay", result.actions_taken);
        Assert.NotEqual(localSchema, File.ReadAllText(schemaPath));
        Assert.Contains(result.backup_files, backup => backup.Contains("AGENTS-schema-governance.json", StringComparison.Ordinal));
    }

    [Fact]
    public void DisableDuplicateCodexLanesInConfigText_DisablesOnlyOwnedNonSelectedLanes()
    {
        const string config = """
        [plugins."anarchy-ai@anarchy-ai-user-profile"]
        enabled = true

        [plugins."anarchy-ai@anarchy-ai-repo-workorders"]
        enabled = true

        [plugins."teams@openai-curated"]
        enabled = true
        """;

        var result = SetupEngine.DisableDuplicateCodexLanesInConfigText(
            config,
            "anarchy-ai@anarchy-ai-repo-workorders");

        Assert.True(result.DuplicateDetected);
        Assert.Equal(["anarchy-ai@anarchy-ai-user-profile"], result.DisabledLanes);
        Assert.Contains("[plugins.\"anarchy-ai@anarchy-ai-user-profile\"]", result.UpdatedText);
        Assert.Contains("enabled = false", result.UpdatedText);
        Assert.Contains("[plugins.\"teams@openai-curated\"]\r\nenabled = true", result.UpdatedText.Replace("\n", "\r\n"));
    }

    [Fact]
    public void ReconcileAnarchyCodexLanesInConfigText_ReEnablesSelectedLane()
    {
        const string config = """
        [plugins."anarchy-ai@anarchy-ai-user-profile"]
        enabled = true

        [plugins."anarchy-ai@anarchy-ai-repo-workorders"]
        enabled = false

        [plugins."teams@openai-curated"]
        enabled = true
        """;

        var result = SetupEngine.ReconcileAnarchyCodexLanesInConfigText(
            config,
            "anarchy-ai@anarchy-ai-repo-workorders");

        Assert.True(result.DuplicateDetected);
        Assert.True(result.SelectedLaneEnabled);
        Assert.Equal(["anarchy-ai@anarchy-ai-user-profile"], result.DisabledLanes);
        Assert.Matches("\\[plugins\\.\"anarchy-ai@anarchy-ai-repo-workorders\"\\]\\s+enabled = true", result.UpdatedText);
        Assert.Matches("\\[plugins\\.\"anarchy-ai@anarchy-ai-user-profile\"\\]\\s+enabled = false", result.UpdatedText);
        Assert.Contains("[plugins.\"teams@openai-curated\"]\r\nenabled = true", result.UpdatedText.Replace("\n", "\r\n"));
    }

    [Fact]
    public void ReconcileAnarchyCodexLanesInConfigText_CreatesMissingSelectedLane()
    {
        const string config = """
        [plugins."anarchy-ai@anarchy-ai-user-profile"]
        enabled = true

        [plugins."teams@openai-curated"]
        enabled = true
        """;

        var result = SetupEngine.ReconcileAnarchyCodexLanesInConfigText(
            config,
            "anarchy-ai@anarchy-ai-repo-brainymigrator");

        Assert.True(result.DuplicateDetected);
        Assert.True(result.SelectedLaneEnabled);
        Assert.Equal(["anarchy-ai@anarchy-ai-user-profile"], result.DisabledLanes);
        Assert.Matches("\\[plugins\\.\"anarchy-ai@anarchy-ai-repo-brainymigrator\"\\]\\s+enabled = true", result.UpdatedText);
        Assert.Matches("\\[plugins\\.\"anarchy-ai@anarchy-ai-user-profile\"\\]\\s+enabled = false", result.UpdatedText);
        Assert.Contains("[plugins.\"teams@openai-curated\"]\r\nenabled = true", result.UpdatedText.Replace("\n", "\r\n"));
    }

    /// <summary>
    /// Confirms that a fully absent repo-local plugin root reports the missing bundle itself without noisy child-surface findings.
    /// </summary>
    /// <returns>No direct return value; the method asserts bounded missing-component vocabulary.</returns>
    /// <remarks>Critical dependencies: setup assess missing-component routing and the plain repo-local plugin path.</remarks>
    [Fact]
    public void Execute_Assess_RepoLocal_MissingBundle_DoesNotReportEveryChildSurface()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();

        var assess = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Assess,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("bootstrap_needed", assess.bootstrap_state);
        Assert.False(assess.runtime_present);
        Assert.Contains("repo_marketplace_missing", assess.missing_components);
        Assert.DoesNotContain("schema_bundle_manifest_missing", assess.missing_components);
        Assert.DoesNotContain("bundled_runtime_missing", assess.missing_components);
        Assert.DoesNotContain("codex_plugin_manifest_missing", assess.missing_components);
        Assert.DoesNotContain("codex_mcp_declaration_missing", assess.missing_components);
        Assert.DoesNotContain("codex_skill_surface_missing", assess.missing_components);
        Assert.DoesNotContain(assess.missing_components, value => value.StartsWith("missing_contract:", StringComparison.Ordinal));
        Assert.DoesNotContain("experimental_direction_assist_contract_missing_non_blocking", assess.actions_taken);
    }

    /// <summary>
    /// Confirms that assessing the AI-Links-style source repo reads plugins/anarchy-ai as an authoring bundle instead of reporting source-owned schemas as missing.
    /// </summary>
    /// <returns>No direct return value; the method asserts source-authoring assessment output.</returns>
    /// <remarks>Critical dependencies: source-authoring detection, nested path roles, and the boundary between source bundle and consumer install target.</remarks>
    [Fact]
    public void Execute_Assess_RepoLocal_SourceAuthoringBundle_DoesNotReportBundleSurfacesMissing()
    {
        using var tempRepo = CreateTempRepo();
        CreateSourceAuthoringBundle(tempRepo.Path);
        var engine = new SetupEngine();

        var assess = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Assess,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("source_authoring_bundle_ready", assess.bootstrap_state);
        Assert.True(assess.source_authoring_bundle_present);
        Assert.Equal("complete", assess.source_authoring_bundle_state);
        Assert.True(assess.runtime_present);
        Assert.Contains("source_authoring_bundle_detected", assess.actions_taken);
        Assert.DoesNotContain("repo_marketplace_missing", assess.missing_components);
        Assert.Contains("choose_user_profile_install_or_explicit_consumer_repo_install", assess.safe_repairs);
        Assert.DoesNotContain("schema_bundle_manifest_missing", assess.missing_components);
        Assert.DoesNotContain("bundled_runtime_missing", assess.missing_components);
        Assert.DoesNotContain("codex_plugin_manifest_missing", assess.missing_components);
        Assert.DoesNotContain("codex_mcp_declaration_missing", assess.missing_components);
        Assert.DoesNotContain("codex_skill_surface_missing", assess.missing_components);
        Assert.DoesNotContain(assess.missing_components, value => value.StartsWith("missing_contract:", StringComparison.Ordinal));
        Assert.Equal("use_source_build_lane_or_user_profile_install", assess.next_action);
        Assert.EndsWith(Path.Combine("plugins", "anarchy-ai"), assess.paths.source?.root_path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(
            Path.Combine("plugins", "anarchy-ai"),
            assess.paths.destination?.directories?["plugin_root_directory_path"],
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that a consumer repo with an installed plain plugins/anarchy-ai bundle is assessed as an install target, not as AI-Links source truth.
    /// </summary>
    /// <returns>No direct return value; the method asserts consumer install behavior after repo-local install.</returns>
    /// <remarks>Critical dependencies: source-repo marker detection and the plain repo-local path contract.</remarks>
    [Fact]
    public void Execute_Install_RepoLocal_PlainPluginPath_RemainsConsumerInstall()
    {
        using var tempRepo = CreateTempRepo();
        var engine = new SetupEngine();

        var install = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Install,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("ready", install.bootstrap_state);
        Assert.False(install.source_authoring_bundle_present);
        Assert.EndsWith(Path.Combine("plugins", "anarchy-ai"), install.paths.destination?.directories?["plugin_root_directory_path"], StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(tempRepo.Path, "plugins", "anarchy-ai", ".mcp.json")));
    }

    /// <summary>
    /// Confirms that repo-local install is blocked when the target is the AI-Links source repo and would overwrite plugins/anarchy-ai.
    /// </summary>
    /// <returns>No direct return value; the method asserts bounded source-write blocking.</returns>
    /// <remarks>Critical dependencies: source-repo marker detection and the AI-Links source-authoring boundary.</remarks>
    [Fact]
    public void Execute_Install_RepoLocal_SourceAuthoringRepo_BlocksConsumerWrite()
    {
        using var tempRepo = CreateTempRepo();
        CreateSourceAuthoringBundle(tempRepo.Path);
        var engine = new SetupEngine();

        var install = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Install,
            InstallScope = InstallScope.RepoLocal,
            HostContext = "codex",
            HostTargets = HostTargets.Codex,
            RepoPath = tempRepo.Path,
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("source_authoring_write_blocked", install.bootstrap_state);
        Assert.True(install.source_authoring_bundle_present);
        Assert.Equal("complete", install.source_authoring_bundle_state);
        Assert.Contains("source_authoring_consumer_install_blocked", install.actions_taken);
        Assert.Contains("source_authoring_repo_consumer_install_blocked", install.missing_components);
        Assert.DoesNotContain("repo_marketplace_missing", install.missing_components);
        Assert.Equal("use_source_build_lane_or_user_profile_install", install.next_action);
    }

    /// <summary>
    /// Verifies that legacy home-local evidence is reported as a custom-MCP fallback mode instead of a ready marketplace lane.
    /// </summary>
    /// <returns>No direct return value; the method asserts the computed registration mode.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.DetermineRegistrationMode(InstallScope, string, HostTargets, LegacyUserProfileInspection)"/> and legacy-surface inspection rules.</remarks>
    [Fact]
    public void DetermineRegistrationMode_ReportsCustomFallbackForLegacyCodexHomeState()
    {
        var inspection = new LegacyUserProfileInspection(
            LegacyPluginRootPresent: true,
            LegacyCodexCustomMcpEntryPresent: true,
            NewPluginMarketplaceLaneReady: false,
            Findings: ["legacy_user_profile_plugin_root_present"]);

        var registrationMode = SetupEngine.DetermineRegistrationMode(InstallScope.UserProfile, "codex", HostTargets.Codex, inspection);

        Assert.Equal("custom_mcp_fallback", registrationMode);
    }

    /// <summary>
    /// Confirms that Claude-only assess output does not present Codex marketplace files as selected host targets.
    /// </summary>
    /// <returns>No direct return value; the method asserts host-specific path and readiness fields.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.Execute(SetupOptions)"/> and destination path role filtering by selected host targets.</remarks>
    [Fact]
    public void Execute_Assess_UserProfile_ClaudeOnly_ReportsHostConfigNotCodexMarketplace()
    {
        var engine = new SetupEngine();
        var result = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Assess,
            InstallScope = InstallScope.UserProfile,
            HostTargets = HostTargets.ClaudeCode,
            HostContext = "codex",
            Silent = true,
            JsonOutput = true
        });

        Assert.Equal("host_config", result.registration_mode);
        Assert.False(result.marketplace_registered);
        Assert.False(result.installed_by_default);
        Assert.Null(result.codex_materialization);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, ProgramJson.Options));
        var files = document.RootElement
            .GetProperty("paths")
            .GetProperty("destination")
            .GetProperty("files");

        Assert.True(files.TryGetProperty("claude_code_config_file_path", out _));
        Assert.False(files.TryGetProperty("codex_config_file_path", out _));
        Assert.False(files.TryGetProperty("marketplace_file_path", out _));
    }

    /// <summary>
    /// Confirms that the published plugin README stays destination-relative and Codex-native after generation.
    /// </summary>
    /// <returns>No direct return value; the method asserts generated README content.</returns>
    /// <remarks>Critical dependencies: build-time README generation, repo root discovery, and the path canon.</remarks>
    [Fact]
    public void GeneratedPluginReadme_StaysDestinationRelativeAndCodexNative()
    {
        var repoRoot = FindRepoRoot();
        var readmePath = Path.Combine(repoRoot, "plugins", "anarchy-ai", "README.md");
        var readme = File.ReadAllText(readmePath);

        Assert.Contains(@".\plugins\anarchy-ai", readme);
        Assert.Contains(@"~\.codex\plugins\anarchy-ai", readme);
        Assert.Contains("./.codex/plugins/anarchy-ai", readme);
        Assert.DoesNotContain("../../../", readme, StringComparison.Ordinal);
        Assert.DoesNotContain(@"~\plugins\anarchy-ai", readme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that generated Codex-facing bundle files use UTF-8 without a byte-order mark.
    /// </summary>
    /// <returns>No direct return value; the method asserts the leading bytes of plugin-facing generated files.</returns>
    /// <remarks>Critical dependencies: build-time generation, repo root discovery, and Codex manifest parsing behavior.</remarks>
    [Fact]
    public void GeneratedPluginFacingFiles_DoNotUseUtf8Bom()
    {
        var repoRoot = FindRepoRoot();
        var pluginRoot = Path.Combine(repoRoot, "plugins", "anarchy-ai");
        var paths = new[]
        {
            Path.Combine(pluginRoot, ".codex-plugin", "plugin.json"),
            Path.Combine(pluginRoot, ".mcp.json"),
            Path.Combine(pluginRoot, "schemas", "schema-bundle.manifest.json")
        };

        foreach (var path in paths)
        {
            var bytes = File.ReadAllBytes(path);
            var hasUtf8Bom = bytes.Length >= 3 &&
                             bytes[0] == 0xEF &&
                             bytes[1] == 0xBB &&
                             bytes[2] == 0xBF;

            Assert.False(hasUtf8Bom, $"Expected UTF-8 without BOM: {path}");
            Assert.True(Encoding.UTF8.GetString(bytes).TrimStart().StartsWith("{", StringComparison.Ordinal), $"Expected JSON content in {path}");
        }
    }

    /// <summary>
    /// Confirms that narrative schema artifact templates travel inside the installed plugin payload.
    /// </summary>
    /// <returns>No direct return value; the method asserts template presence and JSON shape.</returns>
    /// <remarks>Critical dependencies: AGENTS-schema-narrative template obligations and setup's embedded plugin payload wildcard.</remarks>
    [Fact]
    public void PluginBundle_CarriesNarrativeTemplates()
    {
        var repoRoot = FindRepoRoot();
        var templateRoot = Path.Combine(repoRoot, "plugins", "anarchy-ai", "templates", "narratives");
        var registerTemplatePath = Path.Combine(templateRoot, "register.template.json");
        var recordTemplatePath = Path.Combine(templateRoot, "record.template.json");

        using var registerTemplate = JsonDocument.Parse(File.ReadAllText(registerTemplatePath));
        using var recordTemplate = JsonDocument.Parse(File.ReadAllText(recordTemplatePath));

        Assert.True(registerTemplate.RootElement.TryGetProperty("records", out var records));
        Assert.Equal(JsonValueKind.Array, records.ValueKind);
        Assert.True(recordTemplate.RootElement.TryGetProperty("header", out _));
        Assert.True(recordTemplate.RootElement.TryGetProperty("entries", out var entries));
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
    }

    /// <summary>
    /// Verifies that setup assess output uses the nested origin/source/destination path contract and drops retired flat keys.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.Execute(SetupOptions)"/>, <see cref="ProgramJson.Options"/>, and the nested path model.</remarks>
    [Fact]
    public void Execute_Assess_UserProfile_UsesNestedPathsContract()
    {
        var engine = new SetupEngine();
        var result = engine.Execute(new SetupOptions
        {
            Mode = OperationMode.Assess,
            InstallScope = InstallScope.UserProfile,
            HostTargets = HostTargets.Codex,
            HostContext = "codex",
            Silent = true,
            JsonOutput = true
        });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, ProgramJson.Options));
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("paths", out var paths));
        Assert.False(root.TryGetProperty("workspace_root", out _));
        Assert.False(root.TryGetProperty("repo_root", out _));
        Assert.False(root.TryGetProperty("plugin_root", out _));
        Assert.False(root.TryGetProperty("update_source_path", out _));

        var destination = paths.GetProperty("destination");
        var directories = destination.GetProperty("directories");
        var files = destination.GetProperty("files");
        Assert.EndsWith(Path.Combine(".codex", "plugins", "anarchy-ai"), directories.GetProperty("plugin_root_directory_path").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine(".agents", "plugins", "marketplace.json"), files.GetProperty("marketplace_file_path").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Runs the repo-wide path canon audit to catch unapproved hard-coded path literals in live tracked surfaces.
    /// </summary>
    /// <returns>No direct return value; the method asserts successful script exit.</returns>
    /// <remarks>Critical dependencies: the PowerShell audit script, local PowerShell availability, and repo root discovery.</remarks>
    [Fact]
    public void PathCanonAuditScript_Passes()
    {
        var repoRoot = FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "harness", "pathing", "scripts", "test-path-canon-compliance.ps1");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -RepoRoot \"{repoRoot}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        process.WaitForExit();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        Assert.True(process.ExitCode == 0, $"Path canon audit failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");
    }

    /// <summary>
    /// Runs the repo-wide documentation-truth audit to catch stale active-doc claims and missing wrapper documentation.
    /// </summary>
    /// <returns>No direct return value; the method asserts successful script exit.</returns>
    /// <remarks>Critical dependencies: the PowerShell audit script, local PowerShell availability, repo root discovery, and the current Anarchy-AI identity canon.</remarks>
    [Fact]
    public void DocumentationTruthAuditScript_Passes()
    {
        var repoRoot = FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "docs", "scripts", "test-documentation-truth-compliance.ps1");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -RepoRoot \"{repoRoot}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        process.WaitForExit();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        Assert.True(process.ExitCode == 0, $"Documentation-truth audit failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");
    }

    /// <summary>
    /// Runs the removal-safety audit to prove the retirement helper preserves unrelated config while still retiring legacy Anarchy-AI surfaces.
    /// </summary>
    /// <returns>No direct return value; the method asserts successful script exit.</returns>
    /// <remarks>Critical dependencies: the PowerShell removal-safety audit, local PowerShell availability, repo root discovery, and the current retirement-helper defaults.</remarks>
    [Fact]
    public void RemovalSafetyAuditScript_Passes()
    {
        var repoRoot = FindRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "docs", "scripts", "test-removal-safety-compliance.ps1");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -RepoRoot \"{repoRoot}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        process.WaitForExit();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        Assert.True(process.ExitCode == 0, $"Removal-safety audit failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");
    }

    /// <summary>
    /// Locates the repo root from source location first, then explicit/current/runtime fallbacks.
    /// </summary>
    /// <returns>The absolute repo root path containing <c>AGENTS.md</c>.</returns>
    /// <remarks>Critical dependencies: the repo keeping <c>AGENTS.md</c> at its root and build output potentially living outside the repo.</remarks>
    private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
    {
        var candidateRoots = new[]
        {
            Path.GetDirectoryName(sourceFilePath) ?? string.Empty,
            Environment.GetEnvironmentVariable("ANARCHY_AI_REPO_ROOT") ?? string.Empty,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidateRoot in candidateRoots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var found = TryFindRepoRoot(candidateRoot);
            if (found is not null)
            {
                return found;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repo root for setup tests.");
    }

    /// <summary>
    /// Walks upward from one candidate path until the repo root marker is found.
    /// </summary>
    /// <param name="startPath">Candidate file or directory path.</param>
    /// <returns>The repo root path, or null when the marker is not found.</returns>
    /// <remarks>Critical dependencies: the repo root AGENTS.md marker.</remarks>
    private static string? TryFindRepoRoot(string startPath)
    {
        var fullPath = Path.GetFullPath(startPath);
        var current = new DirectoryInfo(File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Creates a temporary repo root with a .git marker so setup repo-local auto-guards recognize it as a workspace.
    /// </summary>
    /// <returns>A temp directory that the caller owns and disposes.</returns>
    /// <remarks>Critical dependencies: setup's repo-root trust rule requiring a .git marker.</remarks>
    private static TempDirectory CreateTempRepo()
    {
        var tempRepo = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(tempRepo.Path, ".git"));
        return tempRepo;
    }

    /// <summary>
    /// Creates the minimal repo-authored plugin source bundle shape used by AI-Links itself.
    /// </summary>
    /// <param name="repoRoot">Temporary repo root that receives plugins/anarchy-ai.</param>
    /// <remarks>Critical dependencies: setup's source-authoring bundle detector and the current required contract filenames.</remarks>
    private static void CreateSourceAuthoringBundle(string repoRoot)
    {
        Directory.CreateDirectory(Path.Combine(repoRoot, "harness", "setup", "dotnet"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "harness", "server", "dotnet"));
        Directory.CreateDirectory(Path.Combine(repoRoot, "docs"));
        File.WriteAllText(Path.Combine(repoRoot, "harness", "setup", "dotnet", "AnarchyAi.Setup.csproj"), "<Project />", Encoding.UTF8);
        File.WriteAllText(Path.Combine(repoRoot, "harness", "server", "dotnet", "AnarchyAi.Mcp.Server.csproj"), "<Project />", Encoding.UTF8);
        File.WriteAllText(Path.Combine(repoRoot, "docs", "README_ai_links.md"), "# AI-Links", Encoding.UTF8);

        var pluginRoot = Path.Combine(repoRoot, "plugins", "anarchy-ai");
        Directory.CreateDirectory(Path.Combine(pluginRoot, ".codex-plugin"));
        Directory.CreateDirectory(Path.Combine(pluginRoot, "runtime", "win-x64"));
        Directory.CreateDirectory(Path.Combine(pluginRoot, "skills", "anarchy-ai-harness"));
        Directory.CreateDirectory(Path.Combine(pluginRoot, "schemas"));
        Directory.CreateDirectory(Path.Combine(pluginRoot, "contracts"));
        Directory.CreateDirectory(Path.Combine(pluginRoot, "templates", "narratives"));

        File.WriteAllText(
            Path.Combine(pluginRoot, ".codex-plugin", "plugin.json"),
            """
            {
              "name": "anarchy-ai",
              "interface": {
                "displayName": "Anarchy-AI"
              }
            }
            """,
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(pluginRoot, ".mcp.json"),
            """
            {
              "mcpServers": {
                "anarchy-ai": {
                  "command": ".\\runtime\\win-x64\\AnarchyAi.Mcp.Server.exe",
                  "args": [],
                  "cwd": "."
                }
              }
            }
            """,
            Encoding.UTF8);
        File.WriteAllText(Path.Combine(pluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe"), "test-runtime", Encoding.UTF8);
        File.WriteAllText(Path.Combine(pluginRoot, "skills", "anarchy-ai-harness", "SKILL.md"), "# Test Skill", Encoding.UTF8);
        File.WriteAllText(Path.Combine(pluginRoot, "schemas", "schema-bundle.manifest.json"), "{}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(pluginRoot, "templates", "narratives", "register.template.json"), """{"records":[]}""", Encoding.UTF8);
        File.WriteAllText(Path.Combine(pluginRoot, "templates", "narratives", "record.template.json"), """{"header":{"id":""}}""", Encoding.UTF8);

        var contractNames = new[]
        {
            "active-work-state.contract.json",
            "schema-reality.contract.json",
            "gov2gov-migration.contract.json",
            "preflight-session.contract.json",
            "harness-gap-state.contract.json",
            "direction-assist-test.contract.json"
        };
        foreach (var contractName in contractNames)
        {
            File.WriteAllText(Path.Combine(pluginRoot, "contracts", contractName), "{}", Encoding.UTF8);
        }
    }
}
