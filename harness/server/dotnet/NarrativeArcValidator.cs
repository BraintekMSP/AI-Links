using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnarchyAi.Mcp.Server;

internal sealed record NarrativeArcValidationFinding(
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("rule_id")] string RuleId,
    [property: JsonPropertyName("file_path")] string FilePath,
    [property: JsonPropertyName("json_path")] string JsonPath,
    [property: JsonPropertyName("observed_state")] string ObservedState,
    [property: JsonPropertyName("suggested_correction")] string SuggestedCorrection);

internal sealed record NarrativeArcCheckedSurface(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("surface_type")] string SurfaceType,
    [property: JsonPropertyName("presence_state")] string PresenceState,
    [property: JsonPropertyName("json_state")] string JsonState);

// Purpose: Measures narrative Arc/register conformance against the current AGENTS narrative shape without writing files.
// Expected input: Workspace root, optional record paths, and validation scope.
// Expected output: Read-only conformance result with file/json-path findings and grounding labels.
// Critical dependencies: current narrative schema field rules, StructuralGroundingInspector, and repo narrative path conventions.
internal sealed class NarrativeArcValidator(StructuralGroundingInspector structuralGroundingInspector)
{
    internal const string ValidationStateConformant = "conformant";
    internal const string ValidationStateNonConformant = "non_conformant";
    internal const string ValidationStateNoNarrativeSurfaces = "no_narrative_arc_surfaces";
    internal const string ValidationStateInvalidInput = "invalid_input";

    private const string RegisterRelativePath = ".agents/anarchy-ai/narratives/register.json";
    private const string ProjectsDirectoryRelativePath = ".agents/anarchy-ai/narratives/projects";
    private const string SourceRegisterRelativePath = "narratives/register.json";
    private const string SourceProjectsDirectoryRelativePath = "narratives/projects";

    public object Validate(string workspaceRoot, string[]? recordPaths = null, string validationScope = "all")
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathRooted(workspaceRoot))
        {
            throw new ArgumentException("workspace_root must be an absolute path.", nameof(workspaceRoot));
        }

        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(resolvedWorkspaceRoot))
        {
            throw new DirectoryNotFoundException($"Workspace root not found: {resolvedWorkspaceRoot}");
        }

        var scope = NormalizeScope(validationScope);
        var findings = new List<NarrativeArcValidationFinding>();
        var checkedSurfaces = new List<NarrativeArcCheckedSurface>();

        if (scope == ValidationStateInvalidInput)
        {
            findings.Add(new NarrativeArcValidationFinding(
                "P2",
                "narrative.validation_scope.invalid",
                resolvedWorkspaceRoot,
                "$.validation_scope",
                $"unsupported_scope:{validationScope}",
                "Use validation_scope all, register, or records."));
        }

        var roots = DiscoverNarrativeRoots(resolvedWorkspaceRoot, recordPaths);
        if (scope is "all" or "register")
        {
            foreach (var root in roots
                         .Where(static root =>
                             root.IsExplicit ||
                             File.Exists(root.RegisterPath) ||
                             Directory.Exists(root.ProjectsDirectoryPath))
                         .DistinctBy(static root => root.RegisterPath))
            {
                ValidateRegister(resolvedWorkspaceRoot, root.RegisterPath, checkedSurfaces, findings, required: true);
            }
        }

        if (scope is "all" or "records")
        {
            foreach (var recordPath in ResolveRecordPaths(resolvedWorkspaceRoot, roots, recordPaths))
            {
                ValidateRecord(resolvedWorkspaceRoot, recordPath, checkedSurfaces, findings);
            }
        }

        if (!checkedSurfaces.Any() && scope != ValidationStateInvalidInput)
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(
                RegisterRelativePath,
                "narrative_register",
                "missing",
                "not_applicable"));
        }

        var validationState = findings.Count > 0
            ? ValidationStateNonConformant
            : checkedSurfaces.Any(static surface => surface.PresenceState == "present")
                ? ValidationStateConformant
                : ValidationStateNoNarrativeSurfaces;

        if (scope == ValidationStateInvalidInput)
        {
            validationState = ValidationStateInvalidInput;
        }

        return new
        {
            validation_state = validationState,
            checked_surfaces = checkedSurfaces
                .OrderBy(static surface => surface.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            findings = findings
                .OrderBy(static finding => finding.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static finding => finding.JsonPath, StringComparer.Ordinal)
                .ToArray(),
            summary = new
            {
                checked_surface_count = checkedSurfaces.Count,
                finding_count = findings.Count,
                non_conformant_surface_count = findings
                    .Select(static finding => finding.FilePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            },
            structural_grounding = structuralGroundingInspector.EvaluateDiagnosticOutput(
                resolvedWorkspaceRoot,
                "validate_narrative_arc_state"),
            recommended_next_actions = BuildRecommendedNextActions(validationState, findings.Count)
        };
    }

    private static string NormalizeScope(string? validationScope)
    {
        if (string.IsNullOrWhiteSpace(validationScope))
        {
            return "all";
        }

        return validationScope.Trim().ToLowerInvariant() switch
        {
            "all" => "all",
            "register" => "register",
            "records" => "records",
            _ => ValidationStateInvalidInput
        };
    }

    private static NarrativeRoot[] DiscoverNarrativeRoots(string workspaceRoot, string[]? recordPaths)
    {
        var roots = new List<NarrativeRoot>
        {
            new(
                Path.Combine(workspaceRoot, RegisterRelativePath),
                Path.Combine(workspaceRoot, ProjectsDirectoryRelativePath),
                IsExplicit: false),
            new(
                Path.Combine(workspaceRoot, SourceRegisterRelativePath),
                Path.Combine(workspaceRoot, SourceProjectsDirectoryRelativePath),
                IsExplicit: false)
        };

        foreach (var recordPath in recordPaths ?? [])
        {
            var resolvedRecordPath = ResolveWorkspacePath(workspaceRoot, recordPath);
            var parent = Path.GetDirectoryName(resolvedRecordPath);
            if (string.IsNullOrWhiteSpace(parent))
            {
                continue;
            }

            var possibleNarrativeRoot = Directory.GetParent(parent)?.FullName;
            if (possibleNarrativeRoot is null)
            {
                continue;
            }

            roots.Add(new(
                Path.Combine(possibleNarrativeRoot, "register.json"),
                Path.Combine(possibleNarrativeRoot, "projects"),
                IsExplicit: true));
        }

        return roots
            .DistinctBy(static root => root.RegisterPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] ResolveRecordPaths(string workspaceRoot, NarrativeRoot[] roots, string[]? recordPaths)
    {
        if (recordPaths is { Length: > 0 })
        {
            return recordPaths
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .Select(path => ResolveWorkspacePath(workspaceRoot, path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return roots
            .Select(static root => root.ProjectsDirectoryPath)
            .Where(Directory.Exists)
            .SelectMany(static path => Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveWorkspacePath(string workspaceRoot, string path)
    {
        var candidate = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(workspaceRoot, path));
        var root = Path.GetFullPath(workspaceRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(normalizedCandidate, root, StringComparison.OrdinalIgnoreCase) &&
            !normalizedCandidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !normalizedCandidate.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"record_paths must stay inside workspace_root: {path}", nameof(path));
        }

        return candidate;
    }

    private static void ValidateRegister(
        string workspaceRoot,
        string registerPath,
        List<NarrativeArcCheckedSurface> checkedSurfaces,
        List<NarrativeArcValidationFinding> findings,
        bool required)
    {
        var relativePath = ToRepoRelativePath(workspaceRoot, registerPath);
        if (!File.Exists(registerPath))
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, "narrative_register", "missing", "not_applicable"));
            if (required)
            {
                AddFinding(findings, relativePath, "$", "narrative.register.file.required", "missing", "Add a narrative register file for this narrative root.");
            }

            return;
        }

        if (!TryParseObject(registerPath, relativePath, "narrative_register", checkedSurfaces, findings, out var root))
        {
            return;
        }

        if (!TryGetArray(root, "records", "$.records", relativePath, findings, out var records))
        {
            return;
        }

        var index = 0;
        foreach (var entry in records.EnumerateArray())
        {
            var path = $"$.records[{index}]";
            if (entry.ValueKind != JsonValueKind.Object)
            {
                AddFinding(findings, relativePath, path, "narrative.register.entry.object", "not_object", "Make each register entry an object.");
                index++;
                continue;
            }

            RequireString(entry, "id", $"{path}.id", relativePath, findings, "narrative.register.id.required");
            RequireString(entry, "subject", $"{path}.subject", relativePath, findings, "narrative.register.subject.required");
            RequireString(entry, "entity-type", $"{path}.entity-type", relativePath, findings, "narrative.register.entity_type.required");
            var recordPath = RequireString(entry, "record-path", $"{path}.record-path", relativePath, findings, "narrative.register.record_path.required");
            RequireString(entry, "cadence", $"{path}.cadence", relativePath, findings, "narrative.register.cadence.required");
            RequireString(entry, "last-updated", $"{path}.last-updated", relativePath, findings, "narrative.register.last_updated.required");
            RequireString(entry, "owner", $"{path}.owner", relativePath, findings, "narrative.register.owner.required");
            RequireString(entry, "status", $"{path}.status", relativePath, findings, "narrative.register.status.required");

            if (!string.IsNullOrWhiteSpace(recordPath))
            {
                var resolvedRecordPath = ResolveWorkspacePath(workspaceRoot, recordPath);
                if (!File.Exists(resolvedRecordPath))
                {
                    AddFinding(findings, relativePath, $"{path}.record-path", "narrative.register.record_path.exists", $"missing:{recordPath}", "Point record-path at an existing narrative record file.");
                }
            }

            index++;
        }
    }

    private static void ValidateRecord(
        string workspaceRoot,
        string recordPath,
        List<NarrativeArcCheckedSurface> checkedSurfaces,
        List<NarrativeArcValidationFinding> findings)
    {
        var relativePath = ToRepoRelativePath(workspaceRoot, recordPath);
        if (!File.Exists(recordPath))
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, "project_record", "missing", "not_applicable"));
            AddFinding(findings, relativePath, "$", "narrative.record.file.exists", "missing", "Point record_paths at existing narrative record files before declaring validation complete.");
            return;
        }

        if (!TryParseObject(recordPath, relativePath, "project_record", checkedSurfaces, findings, out var root))
        {
            return;
        }

        if (RequireObject(root, "header", "$.header", relativePath, findings, "narrative.record.header.required", out var header))
        {
            RequireString(header, "id", "$.header.id", relativePath, findings, "narrative.record.header.id.required");
            RequireString(header, "subject", "$.header.subject", relativePath, findings, "narrative.record.header.subject.required");
            RequireString(header, "entity-type", "$.header.entity-type", relativePath, findings, "narrative.record.header.entity_type.required");
            RequireNonEmptyArray(header, "primary-parties", "$.header.primary-parties", relativePath, findings, "narrative.record.header.primary_parties.required");
            RequireString(header, "cadence", "$.header.cadence", relativePath, findings, "narrative.record.header.cadence.required");
            RequireString(header, "status", "$.header.status", relativePath, findings, "narrative.record.header.status.required");
            RequireString(header, "last-updated", "$.header.last-updated", relativePath, findings, "narrative.record.header.last_updated.required");
            RequireString(header, "owner", "$.header.owner", relativePath, findings, "narrative.record.header.owner.required");
        }

        if (RequireObject(root, "record-state-at-review-open-and-close", "$.record-state-at-review-open-and-close", relativePath, findings, "narrative.record.review_state.required", out var reviewState))
        {
            RequireString(reviewState, "open", "$.record-state-at-review-open-and-close.open", relativePath, findings, "narrative.record.review_state.open.required");
            RequireString(reviewState, "close", "$.record-state-at-review-open-and-close.close", relativePath, findings, "narrative.record.review_state.close.required");
        }

        ValidateEntries(root, relativePath, findings);
        ValidateKnownDecisions(root, relativePath, findings);
        ValidateObservedPatterns(root, relativePath, findings);
    }

    private static void ValidateEntries(JsonElement root, string relativePath, List<NarrativeArcValidationFinding> findings)
    {
        if (!TryGetArray(root, "entries", "$.entries", relativePath, findings, out var entries))
        {
            return;
        }

        var index = 0;
        foreach (var entry in entries.EnumerateArray())
        {
            var path = $"$.entries[{index}]";
            if (entry.ValueKind != JsonValueKind.Object)
            {
                AddFinding(findings, relativePath, path, "narrative.entry.object", "not_object", "Make each entry an object.");
                index++;
                continue;
            }

            RequireString(entry, "id", $"{path}.id", relativePath, findings, "narrative.entry.id.required");
            RequireString(entry, "type", $"{path}.type", relativePath, findings, "narrative.entry.type.required");
            RequireString(entry, "date", $"{path}.date", relativePath, findings, "narrative.entry.date.required");
            RequireString(entry, "summary", $"{path}.summary", relativePath, findings, "narrative.entry.summary.required");
            RequireString(entry, "status", $"{path}.status", relativePath, findings, "narrative.entry.status.required");
            if (RequireObject(entry, "cast", $"{path}.cast", relativePath, findings, "narrative.entry.cast.required", out var cast))
            {
                RequireString(cast, "owner", $"{path}.cast.owner", relativePath, findings, "narrative.entry.cast.owner.required");
            }

            index++;
        }
    }

    private static void ValidateKnownDecisions(JsonElement root, string relativePath, List<NarrativeArcValidationFinding> findings)
    {
        if (!TryGetArray(root, "known-decisions", "$.known-decisions", relativePath, findings, out var decisions))
        {
            return;
        }

        var index = 0;
        foreach (var decision in decisions.EnumerateArray())
        {
            var path = $"$.known-decisions[{index}]";
            if (decision.ValueKind != JsonValueKind.Object)
            {
                AddFinding(findings, relativePath, path, "narrative.known_decision.object", "not_object", "Make each known decision an object.");
                index++;
                continue;
            }

            RequireString(decision, "id", $"{path}.id", relativePath, findings, "narrative.known_decision.id.required");
            RequireString(decision, "date", $"{path}.date", relativePath, findings, "narrative.known_decision.date.required");
            RequireString(decision, "decision", $"{path}.decision", relativePath, findings, "narrative.known_decision.decision.required");
            RequireString(decision, "decided-by", $"{path}.decided-by", relativePath, findings, "narrative.known_decision.decided_by.required");
            RequireBoolean(decision, "reversed", $"{path}.reversed", relativePath, findings, "narrative.known_decision.reversed.required");

            index++;
        }
    }

    private static void ValidateObservedPatterns(JsonElement root, string relativePath, List<NarrativeArcValidationFinding> findings)
    {
        if (!RequireObject(root, "observed-patterns", "$.observed-patterns", relativePath, findings, "narrative.observed_patterns.object.required", out var patterns))
        {
            return;
        }

        ValidatePatternArray(patterns, "good", "$.observed-patterns.good", relativePath, findings, requireResolved: false);
        ValidatePatternArray(patterns, "bad", "$.observed-patterns.bad", relativePath, findings, requireResolved: true);
    }

    private static void ValidatePatternArray(
        JsonElement patterns,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        bool requireResolved)
    {
        if (!TryGetArray(patterns, propertyName, jsonPath, relativePath, findings, out var entries))
        {
            return;
        }

        var index = 0;
        foreach (var pattern in entries.EnumerateArray())
        {
            var path = $"{jsonPath}[{index}]";
            if (pattern.ValueKind != JsonValueKind.Object)
            {
                AddFinding(findings, relativePath, path, "narrative.observed_pattern.object", "not_object", "Make each observed pattern an object.");
                index++;
                continue;
            }

            RequireString(pattern, "description", $"{path}.description", relativePath, findings, "narrative.observed_pattern.description.required");
            RequireBoolean(pattern, "tribal", $"{path}.tribal", relativePath, findings, "narrative.observed_pattern.tribal.required");
            if (requireResolved)
            {
                RequireBoolean(pattern, "resolved", $"{path}.resolved", relativePath, findings, "narrative.observed_pattern.resolved.required");
            }

            index++;
        }
    }

    private static bool TryParseObject(
        string filePath,
        string relativePath,
        string surfaceType,
        List<NarrativeArcCheckedSurface> checkedSurfaces,
        List<NarrativeArcValidationFinding> findings,
        out JsonElement root)
    {
        root = default;
        if (!File.Exists(filePath))
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "missing", "not_applicable"));
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filePath));
            root = document.RootElement.Clone();
            if (root.ValueKind != JsonValueKind.Object)
            {
                checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "present", "valid_json"));
                AddFinding(findings, relativePath, "$", "narrative.surface.object.required", root.ValueKind.ToString(), "Make the narrative surface a JSON object.");
                return false;
            }

            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "present", "valid_json"));
            return true;
        }
        catch (JsonException ex)
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "present", "invalid_json"));
            AddFinding(findings, relativePath, "$", "narrative.surface.valid_json.required", ex.Message, "Repair JSON syntax before checking narrative conformance.");
            return false;
        }
        catch (IOException ex)
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "present", "unreadable"));
            AddFinding(findings, relativePath, "$", "narrative.surface.readable.required", ex.GetType().Name, "Make the file readable before checking narrative conformance.");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            checkedSurfaces.Add(new NarrativeArcCheckedSurface(relativePath, surfaceType, "present", "unreadable"));
            AddFinding(findings, relativePath, "$", "narrative.surface.readable.required", ex.GetType().Name, "Make the file readable before checking narrative conformance.");
            return false;
        }
    }

    private static bool TryGetArray(
        JsonElement element,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        out JsonElement array)
    {
        array = default;
        if (!element.TryGetProperty(propertyName, out var value))
        {
            AddFinding(findings, relativePath, jsonPath, "narrative.array.required", "missing", $"Add {jsonPath} as an array.");
            return false;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            AddFinding(findings, relativePath, jsonPath, "narrative.array.required", value.ValueKind.ToString(), $"Make {jsonPath} an array.");
            return false;
        }

        array = value;
        return true;
    }

    private static bool RequireObject(
        JsonElement element,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        string ruleId,
        out JsonElement value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "missing", $"Add {jsonPath} as an object.");
            return false;
        }

        if (property.ValueKind != JsonValueKind.Object)
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, property.ValueKind.ToString(), $"Make {jsonPath} an object.");
            return false;
        }

        value = property;
        return true;
    }

    private static string RequireString(
        JsonElement element,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        string ruleId)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "missing", $"Add required string field {jsonPath}.");
            return string.Empty;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, value.ValueKind.ToString(), $"Make {jsonPath} a string.");
            return string.Empty;
        }

        var text = value.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "blank", $"Populate required string field {jsonPath} honestly; use unknown only when attribution is unrecoverable.");
        }

        return text;
    }

    private static void RequireBoolean(
        JsonElement element,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        string ruleId)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "missing", $"Add required boolean field {jsonPath}.");
            return;
        }

        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, value.ValueKind.ToString(), $"Make {jsonPath} a boolean.");
        }
    }

    private static void RequireNonEmptyArray(
        JsonElement element,
        string propertyName,
        string jsonPath,
        string relativePath,
        List<NarrativeArcValidationFinding> findings,
        string ruleId)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "missing", $"Add required array field {jsonPath}.");
            return;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, value.ValueKind.ToString(), $"Make {jsonPath} an array.");
            return;
        }

        if (value.GetArrayLength() == 0)
        {
            AddFinding(findings, relativePath, jsonPath, ruleId, "empty", $"Populate {jsonPath} with at least one honest party/role.");
        }
    }

    private static void AddFinding(
        List<NarrativeArcValidationFinding> findings,
        string relativePath,
        string jsonPath,
        string ruleId,
        string observedState,
        string suggestedCorrection)
    {
        findings.Add(new NarrativeArcValidationFinding(
            "P2",
            ruleId,
            relativePath,
            jsonPath,
            observedState,
            suggestedCorrection));
    }

    private static string[] BuildRecommendedNextActions(string validationState, int findingCount)
    {
        return validationState switch
        {
            ValidationStateConformant => ["continue_with_narrative_arc_conformance_visible"],
            ValidationStateNoNarrativeSurfaces => ["run_gov2gov_migration:plan_only", "seed_narrative_register_and_projects_from_underlay"],
            ValidationStateInvalidInput => ["rerun_validate_narrative_arc_state_with_valid_scope"],
            _ when findingCount > 0 => ["repair_narrative_arc_conformance_before_declaring_arc_work_complete", "rerun_validate_narrative_arc_state"],
            _ => ["rerun_validate_narrative_arc_state_after_arc_changes"]
        };
    }

    private static string ToRepoRelativePath(string workspaceRoot, string path)
    {
        return Path.GetRelativePath(workspaceRoot, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private sealed record NarrativeRoot(string RegisterPath, string ProjectsDirectoryPath, bool IsExplicit);
}

internal static class NarrativeArcValidationCli
{
    public static bool TryRun(string[] args, TextWriter output, out int exitCode)
    {
        exitCode = 0;
        if (!args.Any(static arg => string.Equals(arg, "--validate-narrative-arc", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var workspaceRoot = ReadOption(args, "--workspace-root");
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                validation_state = NarrativeArcValidator.ValidationStateInvalidInput,
                findings = new[]
                {
                    new
                    {
                        severity = "P2",
                        rule_id = "narrative.cli.workspace_root.required",
                        file_path = "",
                        json_path = "$.workspace_root",
                        observed_state = "missing",
                        suggested_correction = "Pass --workspace-root <absolute-path>."
                    }
                }
            }, SerializerOptions));
            return true;
        }

        var validationScope = ReadOption(args, "--validation-scope") ?? "all";
        var validator = new NarrativeArcValidator(
            new StructuralGroundingInspector(new SchemaRealityInspector()));
        var result = validator.Validate(workspaceRoot, null, validationScope);
        output.WriteLine(JsonSerializer.Serialize(result, SerializerOptions));
        exitCode = 0;
        return true;
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };
}
