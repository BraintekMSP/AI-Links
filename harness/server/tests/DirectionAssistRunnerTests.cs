using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace AnarchyAi.Mcp.Server.Tests;

public sealed class DirectionAssistRunnerTests
{
    [Fact]
    public void ShortCommand_DoesNotTriggerAssist()
    {
        var workspace = CreateWorkspace();
        var result = Run(workspace, "go for it");

        Assert.False(result["assist_triggered"]!.GetValue<bool>());
    }

    [Fact]
    public void PurpleElephantsPhrase_DoesNotForceAssist()
    {
        var workspace = CreateWorkspace();
        var result = Run(workspace, "Purple elephants mean go");

        Assert.False(result["assist_triggered"]!.GetValue<bool>());
    }

    [Fact]
    public void SingleSentenceOverThirtyWords_TriggersAssist()
    {
        var workspace = CreateWorkspace();
        var direction = "Implement the module in test mode with explicit findings and preserve a reusable runner seam while keeping default tool order unchanged and writing repeatable local telemetry for later comparison across sessions and builds.";
        var result = Run(workspace, direction);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.True(result["trigger_metrics"]!["word_count"]!.GetValue<int>() > 30);
    }

    [Fact]
    public void ThreeSentenceDirectionUnderThirtyWords_TriggersAssist()
    {
        var workspace = CreateWorkspace();
        var direction = "Install in test lane. Keep core stable. Track a local register.";
        var result = Run(workspace, direction);

        Assert.True(result["assist_triggered"]!.GetValue<bool>());
        Assert.True(result["trigger_metrics"]!["sentence_count"]!.GetValue<int>() > 2);
    }

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
        Assert.Contains("5. If `schema_reality_state` is `partial` or `copied_only`, call `run_gov2gov_migration`.", skill, StringComparison.Ordinal);
        Assert.Contains("`direction_assist_test` is an explicit test-lane helper", skill, StringComparison.Ordinal);
    }

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

    private static JsonObject Run(string workspace, string direction)
    {
        var runner = new DirectionAssistRunner();
        var result = runner.Evaluate(workspace, direction, null);
        return JsonSerializer.SerializeToNode(result)!.AsObject();
    }

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "anarchy-ai-direction-assist-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
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

        throw new DirectoryNotFoundException("Could not locate repo root.");
    }
}
