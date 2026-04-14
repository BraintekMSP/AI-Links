using Xunit;

namespace AnarchyAi.Setup.Tests;

public sealed class SetupEngineTests
{
    [Fact]
    public void BuildPluginRelativePath_UserProfile_UsesCodexPluginsLane()
    {
        var relativePath = SetupEngine.BuildPluginRelativePath(InstallScope.UserProfile, null);

        Assert.Equal("./.codex/plugins/anarchy-ai", relativePath);
    }

    [Fact]
    public void ResolvePluginRoot_UserProfile_UsesCodexPluginsLane()
    {
        var pluginRoot = SetupEngine.ResolvePluginRoot(InstallScope.UserProfile, string.Empty);

        Assert.EndsWith(Path.Combine(".codex", "plugins", "anarchy-ai"), pluginRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildInstallDisclosure_UserProfile_DescribesPluginMarketplaceLane()
    {
        var disclosure = SetupEngine.BuildInstallDisclosure(string.Empty, InstallScope.UserProfile);

        Assert.Contains(@"~\.codex\plugins\anarchy-ai", disclosure);
        Assert.Contains(@"~/.agents/plugins/marketplace.json".Replace('/', Path.DirectorySeparatorChar), disclosure);
        Assert.DoesNotContain("Updates ~/.codex/config.toml with mcp_servers.anarchy-ai", disclosure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"~\plugins\anarchy-ai", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCommandLineHelp_UserProfile_DescribesPluginMarketplaceLane()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains(@"~\.codex\plugins\anarchy-ai", help);
        Assert.Contains("plugin marketplace lane", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("also registers mcp_servers.anarchy-ai", help, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildInstallDisclosure_ReportsCoreAndTestToolCounts()
    {
        var disclosure = SetupEngine.BuildInstallDisclosure(string.Empty, InstallScope.UserProfile);

        Assert.Contains("5 core + 1 test harness tool", disclosure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCommandLineHelp_ReportsCoreAndTestToolCounts()
    {
        var help = SetupEngine.BuildCommandLineHelp(null);

        Assert.Contains("5 core + 1 test harness tool", help, StringComparison.OrdinalIgnoreCase);
    }

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

    [Fact]
    public void GeneratedPluginReadme_StaysDestinationRelativeAndCodexNative()
    {
        var repoRoot = FindRepoRoot();
        var readmePath = Path.Combine(repoRoot, "plugins", "anarchy-ai", "README.md");
        var readme = File.ReadAllText(readmePath);

        Assert.Contains(@"~\.codex\plugins\anarchy-ai", readme);
        Assert.Contains("./.codex/plugins/anarchy-ai", readme);
        Assert.DoesNotContain("../../../", readme, StringComparison.Ordinal);
        Assert.DoesNotContain(@"~\plugins\anarchy-ai", readme, StringComparison.OrdinalIgnoreCase);
    }

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
}
