using System.Text.Json;
using System.Runtime.CompilerServices;
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

        var narrativeArc = inventory.GetProperty("narrative_arc_structure");
        var narrativeRegister = narrativeArc.GetProperty("register");
        Assert.Equal(".agents/anarchy-ai/narratives/register.json", narrativeRegister.GetProperty("path").GetString());
        Assert.Equal("missing", narrativeRegister.GetProperty("presence_state").GetString());
        Assert.Equal("planned_to_deliver", narrativeRegister.GetProperty("delivery_plan_state").GetString());
        Assert.Equal("presence_only_workspace_specific_narrative_expected_to_diverge", narrativeArc.GetProperty("hash_mode").GetString());
    }

    /// <summary>
    /// Confirms that gov2gov non-destructive apply seeds the minimal narrative register and projects directory when the narrative schema travels with a consumer workspace.
    /// </summary>
    /// <remarks>Critical dependencies: AGENTS-schema-narrative carrying register templates and gov2gov non-destructive apply behavior.</remarks>
    [Fact]
    public void Gov2GovMigration_NonDestructiveApplySeedsNarrativeArcSurfaces()
    {
        using var workspace = TempWorkspace.Create();
        WritePlaceholderFiles(workspace.Path, GovernedAgentsFiles);
        var runner = new Gov2GovMigrationRunner(new SchemaRealityInspector());

        var result = runner.Run(
            workspace.Path,
            "AGENTS-schema-family",
            "copied_only",
            ["schema_files_present_without_materialization"],
            null,
            "non_destructive_apply");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = document.RootElement;
        var actionsTaken = root.GetProperty("actions_taken").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.Contains("seeded_narrative_register:.agents/anarchy-ai/narratives/register.json", actionsTaken);
        Assert.Contains("created_narrative_projects_directory:.agents/anarchy-ai/narratives/projects", actionsTaken);

        var registerPath = Path.Combine(workspace.Path, ".agents", "anarchy-ai", "narratives", "register.json");
        using var register = JsonDocument.Parse(File.ReadAllText(registerPath));
        Assert.True(register.RootElement.TryGetProperty("records", out var records));
        Assert.Equal(JsonValueKind.Array, records.ValueKind);
        Assert.True(Directory.Exists(Path.Combine(workspace.Path, ".agents", "anarchy-ai", "narratives", "projects")));

        var narrativeRegister = root
            .GetProperty("post_migration_inventory")
            .GetProperty("narrative_arc_structure")
            .GetProperty("register");
        Assert.Equal("present", narrativeRegister.GetProperty("presence_state").GetString());
        Assert.Equal("delivered_this_run", narrativeRegister.GetProperty("delivery_plan_state").GetString());
    }

    /// <summary>
    /// Locates the repo root from source location first, then explicit/current/runtime fallbacks.
    /// </summary>
    /// <returns>The absolute repo root path containing <c>AGENTS.md</c> and <c>docs/README_ai_links.md</c>.</returns>
    /// <remarks>Critical dependencies: the repo keeping startup and runbook surfaces at stable paths and build output potentially living outside the repo.</remarks>
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

        throw new DirectoryNotFoundException("Could not locate repo root for server tests.");
    }

    /// <summary>
    /// Walks upward from one candidate path until the repo root markers are found.
    /// </summary>
    /// <param name="startPath">Candidate file or directory path.</param>
    /// <returns>The repo root path, or null when the markers are not found.</returns>
    /// <remarks>Critical dependencies: root AGENTS.md and docs/README_ai_links.md markers.</remarks>
    private static string? TryFindRepoRoot(string startPath)
    {
        var fullPath = Path.GetFullPath(startPath);
        var current = new DirectoryInfo(File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")) &&
                File.Exists(Path.Combine(current.FullName, "docs", "README_ai_links.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
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
