using System.Text.Json;
using System.Runtime.CompilerServices;
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
}
