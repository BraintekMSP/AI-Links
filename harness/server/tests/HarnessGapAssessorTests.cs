using System.Text.Json;
using System.Runtime.CompilerServices;
using AnarchyAi.Pathing;
using Xunit;

namespace AnarchyAi.Mcp.Server.Tests;

/// <summary>
/// Verifies that harness gap assessment reports the normalized nested path contract.
/// </summary>
/// <remarks>
/// Purpose: catch regressions where assessment output falls back to retired flat path keys.
/// Expected input: the repo root and a live <see cref="HarnessGapAssessor"/> instance.
/// Expected output: xUnit assertions only.
/// Critical dependencies: <see cref="HarnessGapAssessor"/>, <see cref="SchemaRealityInspector"/>, and JSON serialization.
/// </remarks>
public sealed class HarnessGapAssessorTests
{
    /// <summary>
    /// Confirms that assessment output exposes destination marketplace paths through the nested path object and omits flat inspection keys.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure.</returns>
    /// <remarks>Critical dependencies: <see cref="HarnessGapAssessor.Assess(string, string?, string[]?)"/> and the nested assessment-path contract.</remarks>
    [Fact]
    public void Assess_ReportsNestedPathsContract()
    {
        var repoRoot = FindRepoRoot();
        var assessor = new HarnessGapAssessor(new SchemaRealityInspector());

        var result = assessor.Assess(repoRoot, "codex", null);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("paths", out var paths));
        var inspection = root.GetProperty("inspection");
        Assert.False(inspection.TryGetProperty("workspace_root", out _));
        Assert.False(inspection.TryGetProperty("plugin_root", out _));
        Assert.False(inspection.TryGetProperty("marketplace_path", out _));

        var destination = paths.GetProperty("destination");
        var files = destination.GetProperty("files");
        Assert.EndsWith(Path.Combine(".agents", "plugins", "marketplace.json"), files.GetProperty("marketplace_file_path").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Confirms that repo-local generated artifact directories are reported as hygiene findings without deleting or moving them.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure and retained test directories.</returns>
    /// <remarks>Critical dependencies: <see cref="HarnessGapAssessor.Assess(string, string?, string[]?)"/> and artifact hygiene output vocabulary.</remarks>
    [Fact]
    public void Assess_ReportsRepoLocalGeneratedArtifactHygiene()
    {
        var tempRoot = CreateTempWorkspace();
        try
        {
            var binPath = Directory.CreateDirectory(Path.Combine(tempRoot, "src", "Example", "bin")).FullName;
            var objPath = Directory.CreateDirectory(Path.Combine(tempRoot, "src", "Example", "obj")).FullName;
            var tmpPath = Directory.CreateDirectory(Path.Combine(tempRoot, ".tmp")).FullName;

            var assessor = new HarnessGapAssessor(new SchemaRealityInspector());
            var result = assessor.Assess(tempRoot, "generic", null);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
            var artifactHygiene = document.RootElement.GetProperty("artifact_hygiene");
            Assert.Equal("repo_local_artifacts_observed", artifactHygiene.GetProperty("artifact_hygiene_state").GetString());

            var observedPaths = ReadStringArray(artifactHygiene, "observed_artifact_paths");
            Assert.Contains("src/Example/bin", observedPaths);
            Assert.Contains("src/Example/obj", observedPaths);
            Assert.Contains(".tmp", observedPaths);

            var recommendedLanes = ReadStringArray(artifactHygiene, "recommended_artifact_lanes");
            Assert.Contains("%LOCALAPPDATA%\\Anarchy-AI\\<repo>\\", recommendedLanes);
            Assert.Contains("${XDG_CACHE_HOME:-~/.cache}/anarchy-ai/<repo>/", recommendedLanes);

            var safeRepairs = ReadStringArray(artifactHygiene, "safe_repairs");
            Assert.Contains("relocate_generated_artifacts_to_machine_local_cache", safeRepairs);
            Assert.Contains("inventory_and_quarantine_before_delete_never_delete_from_hygiene_assessment", safeRepairs);

            Assert.True(Directory.Exists(binPath));
            Assert.True(Directory.Exists(objPath));
            Assert.True(Directory.Exists(tmpPath));
        }
        finally
        {
            DeleteTempWorkspace(tempRoot);
        }
    }

    /// <summary>
    /// Confirms that a workspace without generated artifact directories reports a clean artifact hygiene state.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure.</returns>
    /// <remarks>Critical dependencies: <see cref="HarnessGapAssessor.Assess(string, string?, string[]?)"/> and artifact hygiene clean-state vocabulary.</remarks>
    [Fact]
    public void Assess_ReportsCleanArtifactHygieneWhenNoGeneratedArtifactsExist()
    {
        var tempRoot = CreateTempWorkspace();
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "docs"));
            File.WriteAllText(Path.Combine(tempRoot, "docs", "README.md"), "# Temp Workspace");

            var assessor = new HarnessGapAssessor(new SchemaRealityInspector());
            var result = assessor.Assess(tempRoot, "generic", null);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
            var artifactHygiene = document.RootElement.GetProperty("artifact_hygiene");
            Assert.Equal("clean", artifactHygiene.GetProperty("artifact_hygiene_state").GetString());
            Assert.Empty(ReadStringArray(artifactHygiene, "observed_artifact_paths"));
        }
        finally
        {
            DeleteTempWorkspace(tempRoot);
        }
    }

    /// <summary>
    /// Confirms that a repo with portable schemas but no narrative/register usage reports availability separately from utilization.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure.</returns>
    /// <remarks>Critical dependencies: underlay readiness output and narrative schema/register path conventions.</remarks>
    [Fact]
    public void Assess_ReportsPortableUnderlayAvailableWithoutRepoUtilization()
    {
        var tempRoot = CreateTempWorkspace();
        try
        {
            SeedPortableSchemaFiles(tempRoot);
            SeedNarrativeTemplates(tempRoot);

            var assessor = new HarnessGapAssessor(new SchemaRealityInspector());
            var result = assessor.Assess(tempRoot, "generic", ["repo_underlay"]);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
            var underlayReadiness = document.RootElement.GetProperty("underlay_readiness");
            Assert.Equal("portable_underlay_available_no_repo_utilization", underlayReadiness.GetProperty("readiness_state").GetString());
            Assert.Equal("complete", underlayReadiness.GetProperty("portable_schema_state").GetString());
            Assert.True(underlayReadiness.GetProperty("narrative_schema_available").GetBoolean());
            Assert.True(underlayReadiness.GetProperty("narrative_templates_available").GetBoolean());
            Assert.False(underlayReadiness.GetProperty("narrative_register_present").GetBoolean());
            Assert.False(underlayReadiness.GetProperty("narrative_projects_directory_present").GetBoolean());
            Assert.Equal(0, underlayReadiness.GetProperty("narrative_record_count").GetInt32());
            Assert.Equal("none", underlayReadiness.GetProperty("repo_utilization_state").GetString());

            var utilizationGaps = ReadStringArray(underlayReadiness, "utilization_gaps");
            Assert.Contains("narrative_register_missing:.agents/anarchy-ai/narratives/register.json", utilizationGaps);
            Assert.Contains("narrative_records_absent", utilizationGaps);

            var safeRepairs = ReadStringArray(underlayReadiness, "safe_repairs");
            Assert.Contains("seed_narrative_register_and_projects_from_underlay", safeRepairs);

            var topLevelAgentActions = ReadStringArray(document.RootElement, "agent_actions");
            Assert.Contains("do_not_treat_schema_availability_as_repo_utilization", topLevelAgentActions);
        }
        finally
        {
            DeleteTempWorkspace(tempRoot);
        }
    }

    /// <summary>
    /// Confirms that an underlay with narrative register scaffolding but no records reports scaffolded state instead of full utilization.
    /// </summary>
    /// <returns>No direct return value; the method asserts serialized JSON structure.</returns>
    /// <remarks>Critical dependencies: narrative register validity and record-count readiness classification.</remarks>
    [Fact]
    public void Assess_ReportsNarrativeScaffoldedWithoutRecords()
    {
        var tempRoot = CreateTempWorkspace();
        try
        {
            SeedPortableSchemaFiles(tempRoot);
            SeedNarrativeTemplates(tempRoot);
            var narrativeDirectory = Path.Combine(tempRoot, ".agents", "anarchy-ai", "narratives");
            Directory.CreateDirectory(Path.Combine(narrativeDirectory, "projects"));
            File.WriteAllText(Path.Combine(narrativeDirectory, "register.json"), "{\"records\":[]}");

            var assessor = new HarnessGapAssessor(new SchemaRealityInspector());
            var result = assessor.Assess(tempRoot, "generic", ["repo_underlay"]);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
            var underlayReadiness = document.RootElement.GetProperty("underlay_readiness");
            Assert.Equal("portable_underlay_available_narrative_scaffolded", underlayReadiness.GetProperty("readiness_state").GetString());
            Assert.Equal("narrative_scaffolded_no_records", underlayReadiness.GetProperty("repo_utilization_state").GetString());
            Assert.True(underlayReadiness.GetProperty("narrative_register_present").GetBoolean());
            Assert.True(underlayReadiness.GetProperty("narrative_register_valid").GetBoolean());
            Assert.Equal(0, underlayReadiness.GetProperty("narrative_record_count").GetInt32());
            Assert.Contains("narrative_records_absent", ReadStringArray(underlayReadiness, "utilization_gaps"));
        }
        finally
        {
            DeleteTempWorkspace(tempRoot);
        }
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

        throw new DirectoryNotFoundException("Could not locate repo root for server tests.");
    }

    /// <summary>
    /// Walks upward from one candidate path until the repo root marker is found.
    /// </summary>
    /// <param name="startPath">Candidate file or directory path.</param>
    /// <returns>The repo root path, or null when the marker is not found.</returns>
    /// <remarks>Critical dependencies: root AGENTS.md marker.</remarks>
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
    /// Reads a string array from an assessment JSON property.
    /// </summary>
    /// <param name="element">JSON object containing the array property.</param>
    /// <param name="propertyName">Property name to read.</param>
    /// <returns>String values from the array.</returns>
    /// <remarks>Critical dependencies: System.Text.Json array shape.</remarks>
    private static string[] ReadStringArray(JsonElement element, string propertyName)
    {
        return element
            .GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    /// <summary>
    /// Seeds placeholder portable schema-family files into a temporary workspace.
    /// </summary>
    /// <param name="workspaceRoot">Temporary workspace root.</param>
    /// <remarks>Critical dependencies: <see cref="RuntimeEnvelopeBuilder.PortableSchemaFamily"/>.</remarks>
    private static void SeedPortableSchemaFiles(string workspaceRoot)
    {
        foreach (var fileName in RuntimeEnvelopeBuilder.PortableSchemaFamily)
        {
            var path = Path.Combine(workspaceRoot, fileName);
            File.WriteAllText(path, fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "{}" : "# temp");
        }
    }

    /// <summary>
    /// Seeds the narrative templates expected by underlay readiness into the temp repo-local plugin bundle path.
    /// </summary>
    /// <param name="workspaceRoot">Temporary workspace root.</param>
    /// <remarks>Critical dependencies: <see cref="AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath"/> and record template path.</remarks>
    private static void SeedNarrativeTemplates(string workspaceRoot)
    {
        var pluginRoot = Path.Combine(workspaceRoot, "plugins", "anarchy-ai");
        var registerTemplatePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath);
        var recordTemplatePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleNarrativeRecordTemplateFileRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(registerTemplatePath)!);
        File.WriteAllText(registerTemplatePath, "{\"records\":[]}");
        File.WriteAllText(recordTemplatePath, "{\"header\":{}}");
    }

    /// <summary>
    /// Creates an isolated temporary workspace for gap-assessment tests.
    /// </summary>
    /// <returns>Absolute path to the temporary workspace.</returns>
    /// <remarks>Critical dependencies: System.IO temp path and GUID uniqueness.</remarks>
    private static string CreateTempWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "anarchy-ai-gap-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Deletes an isolated temporary workspace after verifying it is still inside the expected temp parent.
    /// </summary>
    /// <param name="tempRoot">Temporary workspace path.</param>
    /// <remarks>Critical dependencies: temp-root guard before recursive deletion.</remarks>
    private static void DeleteTempWorkspace(string tempRoot)
    {
        var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "anarchy-ai-gap-tests"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolvedRoot = Path.GetFullPath(tempRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!resolvedRoot.StartsWith(expectedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to delete temp workspace outside expected parent: {resolvedRoot}");
        }

        if (Directory.Exists(resolvedRoot))
        {
            Directory.Delete(resolvedRoot, recursive: true);
        }
    }
}
