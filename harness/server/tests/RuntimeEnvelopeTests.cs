using System.Text.Json;
using Xunit;

namespace AnarchyAi.Mcp.Server.Tests;

/// <summary>
/// Verifies runtime provenance, workspace-role classification, and non-gating migration inventory behavior.
/// </summary>
/// <remarks>
/// Purpose: keep harness claims labeled by speaker/runtime role before agents act on install, schema, or migration output.
/// Expected input: temporary workspaces plus the live repo root.
/// Expected output: xUnit assertions only.
/// Critical dependencies: <see cref="RuntimeEnvelopeBuilder"/>, <see cref="Gov2GovMigrationRunner"/>, and JSON serialization.
/// </remarks>
public sealed class RuntimeEnvelopeTests
{
    private static readonly string[] PortableSchemaFiles =
    [
        "AGENTS-schema-governance.json",
        "AGENTS-schema-1project.json",
        "AGENTS-schema-narrative.json",
        "AGENTS-schema-gov2gov-migration.json",
        "AGENTS-schema-triage.md",
        "Getting-Started-For-Humans.txt"
    ];

    private static readonly string[] GovernedAgentsFiles =
    [
        "AGENTS.md",
        "AGENTS-hello.md",
        "AGENTS-Terms.md",
        "AGENTS-Vision.md",
        "AGENTS-Rules.md"
    ];

    /// <summary>
    /// Confirms that the AI-Links source shape resolves as the deliberate hybrid source/delivery role.
    /// </summary>
    /// <remarks>Critical dependencies: root portable schema files, plugin source, harness source, and runbook surfaces.</remarks>
    [Fact]
    public void WorkspaceRole_IdentifiesAiLinksSourceDeliveryWorkspace()
    {
        var repoRoot = FindRepoRoot();

        var role = RuntimeEnvelopeBuilder.BuildWorkspaceRole(repoRoot);

        Assert.Equal(RuntimeEnvelopeBuilder.SchemaAuthoringAndPluginDeliveryWorkspace, role.MachineKey);
        Assert.Contains("deliberately combines schema/harness development with plugin delivery", role.Statement, StringComparison.Ordinal);
        Assert.Equal("not_applicable", RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(role));
    }

    /// <summary>
    /// Confirms that a declared role file can override filesystem auto-detection with high-confidence provenance.
    /// </summary>
    /// <remarks>Critical dependencies: .anarchy-ai/workspace-role.json declaration handling.</remarks>
    [Fact]
    public void WorkspaceRole_UsesDeclaredWorkspaceRoleWhenPresent()
    {
        using var workspace = TempWorkspace.Create();
        var roleDirectory = Path.Combine(workspace.Path, ".anarchy-ai");
        Directory.CreateDirectory(roleDirectory);
        File.WriteAllText(
            Path.Combine(roleDirectory, "workspace-role.json"),
            """
            {
              "machine_key": "material_governance_consumer_workspace",
              "statement": "Declared material governance consumer workspace for test coverage.",
              "evidence": [
                "declared_by_fixture"
              ],
              "claim_scope_effect": [
                "consumer_material_governance_check:applicable"
              ]
            }
            """);

        var role = RuntimeEnvelopeBuilder.BuildWorkspaceRole(workspace.Path);

        Assert.Equal(RuntimeEnvelopeBuilder.MaterialGovernanceConsumerWorkspace, role.MachineKey);
        Assert.Equal("declared_by_workspace", role.Confidence);
        Assert.Contains("declared_workspace_role_file:.anarchy-ai/workspace-role.json", role.Evidence);
    }

    /// <summary>
    /// Confirms that a material governed AGENTS fixture resolves as a consumer workspace.
    /// </summary>
    /// <remarks>Critical dependencies: governed AGENTS authority file presence checks.</remarks>
    [Fact]
    public void WorkspaceRole_IdentifiesMaterialGovernanceConsumerWorkspace()
    {
        using var workspace = TempWorkspace.Create();
        WritePlaceholderFiles(workspace.Path, GovernedAgentsFiles);

        var role = RuntimeEnvelopeBuilder.BuildWorkspaceRole(workspace.Path);

        Assert.Equal(RuntimeEnvelopeBuilder.MaterialGovernanceConsumerWorkspace, role.MachineKey);
        Assert.Equal("applicable", RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(role));
    }

    /// <summary>
    /// Confirms that a complete governed consumer remains a consumer even when it also carries portable schema files.
    /// </summary>
    /// <remarks>Critical dependencies: portable schema presence and governed AGENTS authority file presence checks.</remarks>
    [Fact]
    public void WorkspaceRole_TreatsPortableSchemasWithCompleteGovernedAgentsAsConsumerWorkspace()
    {
        using var workspace = TempWorkspace.Create();
        WritePlaceholderFiles(workspace.Path, PortableSchemaFiles);
        WritePlaceholderFiles(workspace.Path, GovernedAgentsFiles);

        var role = RuntimeEnvelopeBuilder.BuildWorkspaceRole(workspace.Path);

        Assert.Equal(RuntimeEnvelopeBuilder.MaterialGovernanceConsumerWorkspace, role.MachineKey);
        Assert.Equal("applicable", RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(role));
    }

    /// <summary>
    /// Confirms that straddled but incomplete schema/governance signals do not silently claim source or consumer certainty.
    /// </summary>
    /// <remarks>Critical dependencies: mixed-or-undetermined fallback classification.</remarks>
    [Fact]
    public void WorkspaceRole_IdentifiesMixedRoleWhenObservedSignalsAreIncomplete()
    {
        using var workspace = TempWorkspace.Create();
        WritePlaceholderFiles(workspace.Path, PortableSchemaFiles);
        File.WriteAllText(Path.Combine(workspace.Path, "AGENTS.md"), "# fixture startup");
        File.WriteAllText(Path.Combine(workspace.Path, "AGENTS-hello.md"), "# partial governed file");

        var role = RuntimeEnvelopeBuilder.BuildWorkspaceRole(workspace.Path);

        Assert.Equal(RuntimeEnvelopeBuilder.MixedOrUndeterminedWorkspaceRole, role.MachineKey);
        Assert.Equal("undetermined", RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(role));
    }

    /// <summary>
    /// Confirms that user-profile plugin roots are labeled as installed runtime, not source truth.
    /// </summary>
    /// <remarks>Critical dependencies: user-profile plugin parent path canon and runtime-provenance machine key.</remarks>
    [Fact]
    public void RuntimeProvenance_LabelsUserProfileRuntimeAsInstalledRuntimeNotSourceTruth()
    {
        using var workspace = TempWorkspace.Create();
        var userProfilePluginRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "plugins",
            "anarchy-ai-test");

        var provenance = RuntimeEnvelopeBuilder.BuildRuntimeProvenance(workspace.Path, userProfilePluginRoot);

        Assert.Equal("user_profile_installed_runtime", provenance.MachineKey);
        Assert.Contains("not source truth", provenance.Statement, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that gov2gov inventory reports missing portable schemas and treats AGENTS surfaces as presence-only.
    /// </summary>
    /// <remarks>Critical dependencies: canonical schema bundle discovery from repo source and post_migration_inventory JSON shape.</remarks>
    [Fact]
    public void Gov2GovMigration_PostInventoryReportsMissingSchemasAndPresenceOnlyAgents()
    {
        using var workspace = TempWorkspace.Create();
        File.WriteAllText(Path.Combine(workspace.Path, "AGENTS.md"), "# fixture startup");
        var runner = new Gov2GovMigrationRunner(new SchemaRealityInspector());

        var result = runner.Run(
            workspace.Path,
            "AGENTS-schema-family",
            "copied_only",
            ["schema_files_present_without_materialization"],
            null,
            "plan_only");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var inventory = document.RootElement.GetProperty("post_migration_inventory");

        var portableSchemaEntry = inventory
            .GetProperty("portable_schema_family")
            .EnumerateArray()
            .Single(entry => entry.GetProperty("path").GetString() == "AGENTS-schema-narrative.json");
        Assert.Equal("missing", portableSchemaEntry.GetProperty("presence_state").GetString());
        Assert.Equal("canonical_hash_checked", portableSchemaEntry.GetProperty("hash_mode").GetString());
        Assert.Equal("planned_to_deliver", portableSchemaEntry.GetProperty("delivery_plan_state").GetString());

        var agentsEntry = inventory
            .GetProperty("governed_agents_structure")
            .EnumerateArray()
            .Single(entry => entry.GetProperty("path").GetString() == "AGENTS.md");
        Assert.Equal("present", agentsEntry.GetProperty("presence_state").GetString());
        Assert.Equal("presence_only_workspace_specific_divergence_expected", agentsEntry.GetProperty("hash_mode").GetString());
        Assert.False(agentsEntry.TryGetProperty("observed_sha256", out _));
        Assert.False(agentsEntry.TryGetProperty("expected_sha256", out _));
    }

    /// <summary>
    /// Walks upward from the test binary directory until the repo root is found.
    /// </summary>
    /// <returns>The absolute repo root path containing <c>AGENTS.md</c> and <c>docs/README_ai_links.md</c>.</returns>
    /// <remarks>Critical dependencies: the repo keeping startup and runbook surfaces at stable paths.</remarks>
    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")) &&
                File.Exists(Path.Combine(current.FullName, "docs", "README_ai_links.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root for server tests.");
    }

    private static void WritePlaceholderFiles(string workspaceRoot, IEnumerable<string> fileNames)
    {
        foreach (var fileName in fileNames)
        {
            File.WriteAllText(Path.Combine(workspaceRoot, fileName), "# test");
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Path { get; }

        private TempWorkspace(string path)
        {
            Path = path;
        }

        public static TempWorkspace Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-ai-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempWorkspace(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
