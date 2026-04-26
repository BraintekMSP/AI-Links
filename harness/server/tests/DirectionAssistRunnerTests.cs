using System.Text.Json;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using Xunit;

namespace AnarchyAi.Mcp.Server.Tests;

/// <summary>
/// Verifies the direction-assist test helper's trigger heuristics, user-choice packet, and local register writes.
/// </summary>
/// <remarks>
/// Purpose: keep the experimental clarification helper bounded and reproducible while the core harness remains stable.
/// Expected input: temporary workspaces, free-form direction text, and repo-root assets such as the skill file.
/// Expected output: xUnit assertions only.
/// Critical dependencies: <see cref="DirectionAssistRunner"/>, temp filesystem access, JSON serialization, and the shipped skill file.
/// </remarks>
public sealed class DirectionAssistRunnerTests
{
    /// <summary>
    /// Confirms that a short direct instruction does not trigger clarification assistance.
    /// </summary>
    /// <returns>No direct return value; the method asserts the helper's trigger state.</returns>
    /// <remarks>Critical dependencies: <see cref="Run(string, string)"/> and the current assist-trigger heuristics.</remarks>
    [Fact]
    public void ShortCommand_DoesNotTriggerAssist()
    {
        var workspace = CreateWorkspace();
        var result = Run(workspace, "go for it");

        Assert.False(result["assist_triggered"]!.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that the literal phrase "Purple elephants" is not treated as a trigger by itself.
    /// </summary>
    /// <returns>No direct return value; the method asserts the helper's trigger state.</returns>
    /// <remarks>Critical dependencies: <see cref="DirectionAssistRunner.Evaluate(string, string, string?)"/> and the current linguistic trigger rules.</remarks>
    [Fact]
    public void PurpleElephantsPhrase_DoesNotForceAssist()
    {
        var workspace = CreateWorkspace();
        var result = Run(workspace, "Purple elephants mean go");

        Assert.False(result["assist_triggered"]!.GetValue<bool>());
    }

    /// <summary>
    /// Confirms that a long single-sentence direction crosses the clarification threshold.
    /// </summary>
    /// <returns>No direct return value; the method asserts trigger state and word-count metrics.</returns>
    /// <remarks>Critical dependencies: the helper's word-count threshold and JSON trigger metrics.</remarks>
    [Fact]
    public void SingleSentenceOverThirtyWords_TriggersAssist()
    {
        var workspace = CreateWorkspace();
        var direction = "Implement the module in test mode with explicit findings and preserve a reusable runner seam while keeping default tool order unchanged and writing repeatable local telemetry for later comparison across sessions and builds.";
        var result = Run(workspace, direction);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.True(result["trigger_metrics"]!["word_count"]!.GetValue<int>() > 30);
    }

    /// <summary>
    /// Confirms that a short but multi-sentence direction crosses the clarification threshold.
    /// </summary>
    /// <returns>No direct return value; the method asserts trigger state and sentence-count metrics.</returns>
    /// <remarks>Critical dependencies: the helper's sentence-count threshold and JSON trigger metrics.</remarks>
    [Fact]
    public void ThreeSentenceDirectionUnderThirtyWords_TriggersAssist()
    {
        var workspace = CreateWorkspace();
        var direction = "Install in test lane. Keep core stable. Track a local register.";
        var result = Run(workspace, direction);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.True(result["trigger_metrics"]!["sentence_count"]!.GetValue<int>() > 2);
    }

    /// <summary>
    /// Verifies that the helper returns the exact two bounded user-choice options when clarification is triggered.
    /// </summary>
    /// <returns>No direct return value; the method asserts the returned option list.</returns>
    /// <remarks>Critical dependencies: the direction-assist output contract and the helper's choice-generation logic.</remarks>
    [Fact]
    public void OutputIncludesExactChoiceOptions()
    {
        var workspace = CreateWorkspace();
        var result = Run(workspace, "Install in test lane. Keep core stable. Track a local register.");
        var options = result["choice_options"]!.AsArray().Select(static node => node!.GetValue<string>()).ToArray();

        Assert.Equal(
            [
                "I need to ask clarification on a few things",
                "Do your best with what I gave you"
            ],
            options);
    }

    /// <summary>
    /// Confirms that a triggered response includes both a cleaned direction and detailed linguistic findings.
    /// </summary>
    /// <returns>No direct return value; the method asserts the returned diagnostic fields.</returns>
    /// <remarks>Critical dependencies: the helper's cleanup logic and finding builder.</remarks>
    [Fact]
    public void TriggeredOutputIncludesCleanedDirectionAndFindings()
    {
        var workspace = CreateWorkspace();
        var direction = "Do not keep this vague. Do not leave this unclear. Avoid broad phrasing that could drift implementation into wrong assumptions.";
        var result = Run(workspace, direction);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.False(string.IsNullOrWhiteSpace(result["cleaned_direction"]!.GetValue<string>()));
        Assert.NotEmpty(result["linguistic_findings"]!.AsArray());
    }

    /// <summary>
    /// Verifies that each triggered assist appends a machine-readable register entry in the workspace.
    /// </summary>
    /// <returns>No direct return value; the method asserts register-path existence and JSON content.</returns>
    /// <remarks>Critical dependencies: local filesystem writes beneath the temp workspace and the helper's register writer.</remarks>
    [Fact]
    public void AppendsLocalRegisterEntry()
    {
        var workspace = CreateWorkspace();
        var direction = "Install in test lane. Keep core stable. Track a local register.";
        var result = Run(workspace, direction);

        var registerPath = result["register_path"]!.GetValue<string>();
        Assert.True(File.Exists(registerPath));
        var lines = File.ReadAllLines(registerPath);
        Assert.NotEmpty(lines);

        var first = JsonNode.Parse(lines[0])!.AsObject();
        Assert.Equal(workspace, first["workspace_root"]!.GetValue<string>());
        Assert.NotNull(first["trigger_metrics"]);
    }

    /// <summary>
    /// Confirms that the shipped skill keeps the core tool order stable and labels the test helper as experimental.
    /// </summary>
    /// <returns>No direct return value; the method asserts skill file content.</returns>
    /// <remarks>Critical dependencies: the installed skill markdown and the current tool-order guidance.</remarks>
    [Fact]
    public void SkillFileKeepsCoreOrderAndDocumentsExperimentalTool()
    {
        var repoRoot = FindRepoRoot();
        var skillPath = Path.Combine(repoRoot, "plugins", "anarchy-ai", "skills", "anarchy-ai-harness", "SKILL.md");
        var skill = File.ReadAllText(skillPath);

        Assert.Contains("1. Call `preflight_session`", skill, StringComparison.Ordinal);
        Assert.Contains("2. Call `assess_harness_gap_state`", skill, StringComparison.Ordinal);
        Assert.Contains("3. Call `compile_active_work_state`", skill, StringComparison.Ordinal);
        Assert.Contains("4. Call `is_schema_real_or_shadow_copied`", skill, StringComparison.Ordinal);
        Assert.Contains("5. For `partial` or `copied_only` schema reality, call `run_gov2gov_migration`.", skill, StringComparison.Ordinal);
        Assert.Contains("`direction_assist_test` is an explicit test-lane helper", skill, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies a real user-prompt scenario that should trigger clarification and register writing.
    /// </summary>
    /// <returns>No direct return value; the method asserts trigger state, option count, and register writes.</returns>
    /// <remarks>Critical dependencies: the helper's real-text parsing, temp filesystem state, and JSON output contract.</remarks>
    [Fact]
    public void UserPromptScenario_TriggersClarificationChoicesAndWritesRegister()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "anarchy-ai-direction-assist-user-proof");
        Directory.CreateDirectory(workspace);
        var registerPath = Path.Combine(workspace, ".agents", "anarchy-ai", "direction-assist-test.jsonl");
        if (File.Exists(registerPath))
        {
            File.Delete(registerPath);
        }

        var prompt = """
I'd like you to test it on Fissure/docker-builder-project. This means you'll need to change pwd to the fissure repo, and run the update/install.

There are artifacts of the old method in there. I don't expect to account for those in the script, but would like them cleaned up, so we can measure scope creep into the future

When done, make sure to change pwd back over here
""";
        var result = Run(workspace, prompt);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.Equal(2, result["choice_options"]!.AsArray().Count);
        Assert.True(File.Exists(registerPath));
        Assert.NotEmpty(File.ReadAllLines(registerPath));
    }

    /// <summary>
    /// Runs the direction-assist helper and normalizes its anonymous object output into a mutable JSON object for assertions.
    /// </summary>
    /// <param name="workspace">Workspace root where the helper can write its local register.</param>
    /// <param name="direction">Free-form direction text to evaluate.</param>
    /// <returns>A JSON object representation of the helper output.</returns>
    /// <remarks>Critical dependencies: <see cref="DirectionAssistRunner"/> and <see cref="JsonSerializer"/>.</remarks>
    private static JsonObject Run(string workspace, string direction)
    {
        var runner = new DirectionAssistRunner();
        var result = runner.Evaluate(workspace, direction, null);
        return JsonSerializer.SerializeToNode(result)!.AsObject();
    }

    /// <summary>
    /// Creates a unique temporary workspace for each test scenario.
    /// </summary>
    /// <returns>The absolute path to the created temporary workspace.</returns>
    /// <remarks>Critical dependencies: OS temp storage and directory creation permissions.</remarks>
    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "anarchy-ai-direction-assist-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
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

        throw new DirectoryNotFoundException("Could not locate repo root.");
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
