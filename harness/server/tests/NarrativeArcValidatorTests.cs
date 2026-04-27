using System.Text;
using System.Text.Json;
using Xunit;

namespace AnarchyAi.Mcp.Server.Tests;

/// <summary>
/// Verifies read-only narrative Arc conformance measurement.
/// </summary>
/// <remarks>
/// Purpose: catch regressions where narrative records are present and valid JSON but not conformant to the carried narrative shape.
/// Expected input: temporary workspaces with conformant and non-conformant narrative fixtures.
/// Expected output: xUnit assertions only.
/// Critical dependencies: <see cref="NarrativeArcValidator"/>, narrative register path conventions, and CLI warn-only behavior.
/// </remarks>
public sealed class NarrativeArcValidatorTests
{
    [Fact]
    public void ValidateNarrativeArcState_ReturnsConformantForAiLinksStyleFixture()
    {
        using var workspace = TestWorkspace.Create();
        WriteConformantNarrative(workspace.Path);

        var validator = CreateValidator();
        var result = validator.Validate(workspace.Path);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = document.RootElement;
        Assert.Equal(NarrativeArcValidator.ValidationStateConformant, root.GetProperty("validation_state").GetString());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("finding_count").GetInt32());
        Assert.Empty(root.GetProperty("findings").EnumerateArray());
        Assert.True(root.TryGetProperty("structural_grounding", out _));
    }

    [Fact]
    public void ValidateNarrativeArcState_DetectsWo2NonConformanceClasses()
    {
        using var workspace = TestWorkspace.Create();
        WriteWo2ShapedNonConformantNarrative(workspace.Path);

        var validator = CreateValidator();
        var result = validator.Validate(workspace.Path);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = document.RootElement;
        var ruleIds = root.GetProperty("findings")
            .EnumerateArray()
            .Select(finding => finding.GetProperty("rule_id").GetString())
            .Where(ruleId => !string.IsNullOrWhiteSpace(ruleId))
            .Cast<string>()
            .ToArray();

        Assert.Equal(NarrativeArcValidator.ValidationStateNonConformant, root.GetProperty("validation_state").GetString());
        Assert.Contains("narrative.register.subject.required", ruleIds);
        Assert.Contains("narrative.entry.id.required", ruleIds);
        Assert.Contains("narrative.known_decision.decided_by.required", ruleIds);
        Assert.Contains("narrative.observed_patterns.object.required", ruleIds);

        var jsonPaths = root.GetProperty("findings")
            .EnumerateArray()
            .Select(finding => finding.GetProperty("json_path").GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();
        Assert.Contains("$.records[0].subject", jsonPaths);
        Assert.Contains("$.entries[0].id", jsonPaths);
        Assert.Contains("$.known-decisions[0].decided-by", jsonPaths);
        Assert.Contains("$.observed-patterns", jsonPaths);
    }

    [Fact]
    public void ValidateNarrativeArcState_CanScopeToExplicitRecordPath()
    {
        using var workspace = TestWorkspace.Create();
        var recordPath = WriteConformantNarrative(workspace.Path);

        var validator = CreateValidator();
        var result = validator.Validate(workspace.Path, [recordPath], "records");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
        var root = document.RootElement;
        Assert.Equal(NarrativeArcValidator.ValidationStateConformant, root.GetProperty("validation_state").GetString());
        Assert.Single(root.GetProperty("checked_surfaces").EnumerateArray());
        Assert.Equal("project_record", root.GetProperty("checked_surfaces")[0].GetProperty("surface_type").GetString());
    }

    [Fact]
    public void NarrativeArcValidationCli_EmitsJsonAndExitsZeroForFindings()
    {
        using var workspace = TestWorkspace.Create();
        WriteWo2ShapedNonConformantNarrative(workspace.Path);
        using var writer = new StringWriter();

        var handled = NarrativeArcValidationCli.TryRun(
            ["--validate-narrative-arc", "--workspace-root", workspace.Path, "--json"],
            writer,
            out var exitCode);

        Assert.True(handled);
        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(writer.ToString());
        Assert.Equal(NarrativeArcValidator.ValidationStateNonConformant, document.RootElement.GetProperty("validation_state").GetString());
    }

    private static NarrativeArcValidator CreateValidator()
    {
        return new NarrativeArcValidator(new StructuralGroundingInspector(new SchemaRealityInspector()));
    }

    private static string WriteConformantNarrative(string workspaceRoot)
    {
        var narrativeRoot = Path.Combine(workspaceRoot, "narratives");
        var projectsRoot = Path.Combine(narrativeRoot, "projects");
        Directory.CreateDirectory(projectsRoot);
        var recordPath = Path.Combine(projectsRoot, "ai-links.json");
        File.WriteAllText(Path.Combine(narrativeRoot, "register.json"), """
{
  "schemaVersion": "0.2.0",
  "records": [
    {
      "id": "ai-links",
      "subject": "AI-Links",
      "entity-type": "project",
      "record-path": "narratives/projects/ai-links.json",
      "cadence": "as-needed",
      "last-updated": "2026-04-27T00:00:00-05:00",
      "owner": "repo-owner",
      "status": "active"
    }
  ]
}
""", Encoding.UTF8);

        File.WriteAllText(recordPath, """
{
  "schemaVersion": "0.2.0",
  "header": {
    "id": "ai-links",
    "subject": "AI-Links",
    "entity-type": "project",
    "primary-parties": ["repo-owner"],
    "cadence": "as-needed",
    "status": "active",
    "last-updated": "2026-04-27T00:00:00-05:00",
    "owner": "repo-owner"
  },
  "record-state-at-review-open-and-close": {
    "open": "review opened",
    "close": "review closed"
  },
  "entries": [
    {
      "id": "e001",
      "type": "beat",
      "date": "2026-04-27T00:00:00-05:00",
      "summary": "Conformant fixture entry.",
      "cast": { "owner": "repo-owner" },
      "status": "active"
    }
  ],
  "known-decisions": [
    {
      "id": "d001",
      "date": "2026-04-27T00:00:00-05:00",
      "decision": "Use read-only validation before declaring Arc edits complete.",
      "decided-by": "repo-owner",
      "reversed": false
    }
  ],
  "observed-patterns": {
    "good": [
      {
        "description": "Validation is easy to invoke before completion claims.",
        "tribal": false
      }
    ],
    "bad": [
      {
        "description": "Valid JSON can be mistaken for schema conformance.",
        "tribal": false,
        "resolved": false
      }
    ]
  }
}
""", Encoding.UTF8);
        return recordPath;
    }

    private static void WriteWo2ShapedNonConformantNarrative(string workspaceRoot)
    {
        var narrativeRoot = Path.Combine(workspaceRoot, ".agents", "anarchy-ai", "narratives");
        var projectsRoot = Path.Combine(narrativeRoot, "projects");
        Directory.CreateDirectory(projectsRoot);
        File.WriteAllText(Path.Combine(narrativeRoot, "register.json"), """
{
  "schemaVersion": "0.1.9",
  "records": [
    {
      "id": "gov2gov-underlay-workorders-20260426"
    }
  ]
}
""", Encoding.UTF8);

        File.WriteAllText(Path.Combine(projectsRoot, "gov2gov-underlay-workorders-20260426.json"), """
{
  "schemaVersion": "0.1.9",
  "header": {
    "id": "gov2gov-underlay-workorders-20260426",
    "subject": "Workorders GOV2GOV underlay",
    "entity-type": "migration",
    "primary-parties": ["repo-owner"],
    "cadence": "as-needed",
    "status": "complete",
    "last-updated": "2026-04-26",
    "owner": "repo-owner"
  },
  "entries": [
    {
      "date": "2026-04-26",
      "summary": "Entry missing id and type.",
      "cast": { "owner": "repo-owner" },
      "status": "complete"
    }
  ],
  "known-decisions": [
    {
      "id": "d001",
      "date": "2026-04-26",
      "decision": "Record completion relative to current harness.",
      "reversed": false
    }
  ],
  "observed-patterns": [
    {
      "description": "Old array shape.",
      "tribal": false
    }
  ]
}
""", Encoding.UTF8);
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TestWorkspace Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-narrative-validator-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TestWorkspace(path);
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
