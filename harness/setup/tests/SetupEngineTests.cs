using Xunit;
using System.Diagnostics;
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
    /// Confirms that repo-local marketplace identity is stable across devices for the same repo name instead of leaking a path hash.
    /// </summary>
    /// <returns>No direct return value; the method asserts the generated repo-local marketplace identifier.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.BuildMarketplaceName(InstallScope, string?)"/> and the repo-slug identity contract.</remarks>
    [Fact]
    public void BuildMarketplaceName_RepoLocal_UsesRepoSlugWithoutPathHash()
    {
        var marketplaceName = SetupEngine.BuildMarketplaceName(
            InstallScope.RepoLocal,
            Path.Combine(Path.GetTempPath(), "AI-Links"));

        Assert.Equal("anarchy-ai-local-ai-links", marketplaceName);
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
        Assert.Equal("anarchy.install-state.v1", root.GetProperty("schema_version").GetString());
        Assert.Equal("install", root.GetProperty("setup_operation").GetString());
        Assert.Equal("repo_local", root.GetProperty("install_scope").GetString());
        Assert.Equal(tempRepo.Path, root.GetProperty("workspace_root").GetString(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("anarchy-ai", root.GetProperty("mcp_server_name").GetString());
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

    /// <summary>
    /// Verifies that legacy home-local evidence is reported as a custom-MCP fallback mode instead of a ready marketplace lane.
    /// </summary>
    /// <returns>No direct return value; the method asserts the computed registration mode.</returns>
    /// <remarks>Critical dependencies: <see cref="SetupEngine.DetermineRegistrationMode(InstallScope, string, LegacyUserProfileInspection)"/> and legacy-surface inspection rules.</remarks>
    [Fact]
    public void DetermineRegistrationMode_ReportsCustomFallbackForLegacyCodexHomeState()
    {
        var inspection = new LegacyUserProfileInspection(
            LegacyPluginRootPresent: true,
            LegacyCodexCustomMcpEntryPresent: true,
            NewPluginMarketplaceLaneReady: false,
            Findings: ["legacy_user_profile_plugin_root_present"]);

        var registrationMode = SetupEngine.DetermineRegistrationMode(InstallScope.UserProfile, "codex", inspection);

        Assert.Equal("custom_mcp_fallback", registrationMode);
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

        Assert.Contains(@".\plugins\anarchy-ai-local-<repo-slug>-<stable-path-hash>", readme);
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
    /// Walks upward from the test binary directory until the repo root is found.
    /// </summary>
    /// <returns>The absolute repo root path containing <c>AGENTS.md</c>.</returns>
    /// <remarks>Critical dependencies: the repo keeping <c>AGENTS.md</c> at its root and tests running from a descendant directory.</remarks>
    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root for setup tests.");
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
}
