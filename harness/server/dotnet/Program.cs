using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnarchyAi.Mcp.Server;

internal sealed class ContractLoader
{
    private readonly string _contractsDir = ResolveContractsDirectory();

    public JsonElement LoadContract(string contractFileName)
    {
        var contractPath = Path.Combine(_contractsDir, contractFileName);
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(contractPath));
    }

    private static string ResolveContractsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "contracts"),
            Path.Combine(AppContext.BaseDirectory, "contracts"),
            Path.Combine(AppContext.BaseDirectory, "..", "contracts"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "contracts"),
            Path.Combine(Environment.CurrentDirectory, "harness", "contracts")
        }
        .Select(Path.GetFullPath);

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "schema-reality.contract.json")) &&
                File.Exists(Path.Combine(candidate, "gov2gov-migration.contract.json")) &&
                File.Exists(Path.Combine(candidate, "active-work-state.contract.json")) &&
                File.Exists(Path.Combine(candidate, "preflight-session.contract.json")) &&
                File.Exists(Path.Combine(candidate, "harness-gap-state.contract.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate harness contracts. Expected plugin-local contracts/ or repo-local harness/contracts/.");
    }
}

internal sealed class CanonicalSchemaBundle
{
    public required bool IsAvailable { get; init; }
    public string? BundleName { get; init; }
    public string? BundleVersion { get; init; }
    public string? ManifestPath { get; init; }
    public string? SchemasDirectory { get; init; }
    public IReadOnlyDictionary<string, string> FileHashes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> CanonicalFileNames => FileHashes.Keys;

    public static CanonicalSchemaBundle TryLoad()
    {
        var candidateDirectories = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "schemas"),
            Path.Combine(AppContext.BaseDirectory, "schemas"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "schemas"),
            Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "plugins", "anarchy-ai", "schemas"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "plugins", "anarchy-ai", "schemas")
        }
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidateDirectory in candidateDirectories)
        {
            var manifestPath = Path.Combine(candidateDirectory, "schema-bundle.manifest.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var manifest = JsonSerializer.Deserialize<SchemaBundleManifest>(File.ReadAllText(manifestPath));
            if (manifest?.Files is null || manifest.Files.Count == 0)
            {
                continue;
            }

            return new CanonicalSchemaBundle
            {
                IsAvailable = true,
                BundleName = manifest.BundleName,
                BundleVersion = manifest.BundleVersion,
                ManifestPath = manifestPath,
                SchemasDirectory = candidateDirectory,
                FileHashes = manifest.Files.ToDictionary(
                    entry => entry.FileName,
                    entry => entry.Sha256,
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        return new CanonicalSchemaBundle { IsAvailable = false };
    }

    public bool TryResolveFilePath(string fileName, out string filePath)
    {
        filePath = string.Empty;
        if (!IsAvailable || string.IsNullOrWhiteSpace(SchemasDirectory))
        {
            return false;
        }

        var candidatePath = Path.Combine(SchemasDirectory, fileName);
        if (!File.Exists(candidatePath))
        {
            return false;
        }

        filePath = candidatePath;
        return true;
    }

    private sealed class SchemaBundleManifest
    {
        [JsonPropertyName("bundle_name")]
        public string? BundleName { get; set; }

        [JsonPropertyName("bundle_version")]
        public string? BundleVersion { get; set; }

        [JsonPropertyName("files")]
        public List<SchemaBundleFile>? Files { get; set; }
    }

    private sealed class SchemaBundleFile
    {
        [JsonPropertyName("file_name")]
        public required string FileName { get; set; }

        [JsonPropertyName("sha256")]
        public required string Sha256 { get; set; }
    }
}

internal sealed class SchemaRealityInspector
{
    private readonly CanonicalSchemaBundle _canonicalSchemaBundle = CanonicalSchemaBundle.TryLoad();

    private static readonly string[] FamilySchemaFiles =
    [
        "AGENTS-schema-governance.json",
        "AGENTS-schema-1project.json",
        "AGENTS-schema-narrative.json",
        "AGENTS-schema-gov2gov-migration.json",
        "AGENTS-schema-triage.md",
        "Getting-Started-For-Humans.txt"
    ];

    private static readonly string[] GovernedAuthorityFiles =
    [
        "AGENTS-hello.md",
        "AGENTS-Terms.md",
        "AGENTS-Vision.md",
        "AGENTS-Rules.md"
    ];

    public object Evaluate(string workspaceRoot, string expectedSchemaPackage, string[]? startupSurfaces)
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

        var normalizedStartupSurfaces = NormalizeStartupSurfaces(resolvedWorkspaceRoot, startupSurfaces);
        var startupSurfaceExists = normalizedStartupSurfaces.ToDictionary(path => path, File.Exists, StringComparer.OrdinalIgnoreCase);

        var agentsMdPath = Path.Combine(resolvedWorkspaceRoot, "AGENTS.md");
        var agentsMdExists = File.Exists(agentsMdPath);
        var agentsMdContent = agentsMdExists ? File.ReadAllText(agentsMdPath) : string.Empty;

        var familyFileExists = FamilySchemaFiles.ToDictionary(
            fileName => fileName,
            fileName => File.Exists(Path.Combine(resolvedWorkspaceRoot, fileName)),
            StringComparer.OrdinalIgnoreCase);

        var governedFileExists = GovernedAuthorityFiles.ToDictionary(
            fileName => fileName,
            fileName => File.Exists(Path.Combine(resolvedWorkspaceRoot, fileName)),
            StringComparer.OrdinalIgnoreCase);

        var packageFilesPresentCount = familyFileExists.Values.Count(static value => value);
        var governedFilesPresentCount = governedFileExists.Values.Count(static value => value);

        var schemaPathAligned = agentsMdExists && agentsMdContent.Contains("schema-path: AGENTS-schema-governance.json", StringComparison.Ordinal);
        var fileListAligned = agentsMdExists && GovernedAuthorityFiles.All(fileName => agentsMdContent.Contains(fileName, StringComparison.Ordinal));
        var startupDiscoveryPathReal = agentsMdExists && schemaPathAligned && fileListAligned;
        var packageSurfaceAligned = familyFileExists.Values.All(static value => value);
        var folderTopologyAligned = packageSurfaceAligned && governedFileExists.Values.All(static value => value);
        var startupSurfaceAligned = startupSurfaceExists.Count == 0 || startupSurfaceExists.Values.All(static value => value);

        var state = ClassifyState(
            agentsMdExists,
            packageFilesPresentCount,
            governedFilesPresentCount,
            startupDiscoveryPathReal,
            packageSurfaceAligned,
            startupSurfaceAligned);

        var integrityResult = EvaluateIntegrity(resolvedWorkspaceRoot);
        var activeReasons = BuildReasons(
            state,
            agentsMdExists,
            packageFilesPresentCount,
            governedFilesPresentCount,
            startupDiscoveryPathReal,
            packageSurfaceAligned,
            folderTopologyAligned,
            startupSurfaceAligned);

        return new
        {
            schema_reality_state = state,
            integrity_state = integrityResult.IntegrityState,
            possession_state = BuildPossessionState(state, integrityResult.IntegrityState),
            state_reasons = new
            {
                real = new[]
                {
                    "current_schema_files_present",
                    "package_surface_aligned",
                    "folder_topology_aligned",
                    "startup_discovery_path_real"
                },
                partial = new[]
                {
                    "current_schema_files_present_with_missing_surfaces",
                    "package_surface_out_of_sync",
                    "folder_topology_partial",
                    "startup_discovery_path_weakened"
                },
                copied_only = new[]
                {
                    "schema_files_present_without_materialization",
                    "stale_companion_artifacts",
                    "copied_without_package_alignment",
                    "folder_topology_mismatch",
                    "schema_present_not_governing"
                },
                fully_missing = new[]
                {
                    "schema_package_absent",
                    "startup_surface_absent",
                    "package_surface_absent"
                }
            },
            active_reasons = activeReasons,
            integrity_findings = integrityResult.Findings,
            recommended_next_action = state is "partial" or "copied_only" || integrityResult.IntegrityState == "diverged"
                ? "run_gov2gov_migration"
                : "none",
            safe_repairs = BuildSafeRepairs(state, startupSurfaceExists, integrityResult.IntegrityState),
            inspection = new
            {
                workspace_root = resolvedWorkspaceRoot,
                expected_schema_package = expectedSchemaPackage,
                agents_md_exists = agentsMdExists,
                package_files_present = familyFileExists.Where(static pair => pair.Value).Select(static pair => pair.Key).ToArray(),
                package_files_missing = familyFileExists.Where(static pair => !pair.Value).Select(static pair => pair.Key).ToArray(),
                governed_files_present = governedFileExists.Where(static pair => pair.Value).Select(static pair => pair.Key).ToArray(),
                governed_files_missing = governedFileExists.Where(static pair => !pair.Value).Select(static pair => pair.Key).ToArray(),
                startup_surfaces_present = startupSurfaceExists.Where(static pair => pair.Value).Select(static pair => pair.Key).ToArray(),
                startup_surfaces_missing = startupSurfaceExists.Where(static pair => !pair.Value).Select(static pair => pair.Key).ToArray(),
                startup_discovery_path_real = startupDiscoveryPathReal,
                canonical_schema_bundle_available = _canonicalSchemaBundle.IsAvailable,
                canonical_schema_bundle_version = _canonicalSchemaBundle.BundleVersion,
                canonical_schema_manifest_path = _canonicalSchemaBundle.ManifestPath,
                canonical_schema_files_aligned = integrityResult.AlignedFiles,
                canonical_schema_files_diverged = integrityResult.DivergedFiles,
                canonical_schema_files_missing = integrityResult.MissingFiles
            }
        };
    }

    private static string ClassifyState(
        bool agentsMdExists,
        int packageFilesPresentCount,
        int governedFilesPresentCount,
        bool startupDiscoveryPathReal,
        bool packageSurfaceAligned,
        bool startupSurfaceAligned)
    {
        var packagePresent = packageFilesPresentCount > 0;
        var governedPresent = governedFilesPresentCount > 0;

        if (!agentsMdExists && !packagePresent && !governedPresent && !startupSurfaceAligned)
        {
            return "fully_missing";
        }

        if (!agentsMdExists && !packagePresent && !governedPresent)
        {
            return "fully_missing";
        }

        if (packagePresent && governedFilesPresentCount == 0)
        {
            return "copied_only";
        }

        if (governedPresent && (!packageSurfaceAligned || !startupDiscoveryPathReal || !startupSurfaceAligned || governedFilesPresentCount < GovernedAuthorityFiles.Length))
        {
            return "partial";
        }

        if (agentsMdExists && packagePresent && governedFilesPresentCount == GovernedAuthorityFiles.Length && startupDiscoveryPathReal && packageSurfaceAligned && startupSurfaceAligned)
        {
            return "real";
        }

        return packagePresent ? "copied_only" : "fully_missing";
    }

    private static string[] BuildReasons(
        string state,
        bool agentsMdExists,
        int packageFilesPresentCount,
        int governedFilesPresentCount,
        bool startupDiscoveryPathReal,
        bool packageSurfaceAligned,
        bool folderTopologyAligned,
        bool startupSurfaceAligned)
    {
        var reasons = new List<string>();

        switch (state)
        {
            case "real":
                reasons.Add("current_schema_files_present");
                if (packageSurfaceAligned)
                {
                    reasons.Add("package_surface_aligned");
                }
                if (folderTopologyAligned)
                {
                    reasons.Add("folder_topology_aligned");
                }
                if (startupDiscoveryPathReal)
                {
                    reasons.Add("startup_discovery_path_real");
                }
                break;

            case "partial":
                reasons.Add("current_schema_files_present_with_missing_surfaces");
                if (!packageSurfaceAligned)
                {
                    reasons.Add("package_surface_out_of_sync");
                }
                if (governedFilesPresentCount < GovernedAuthorityFiles.Length)
                {
                    reasons.Add("folder_topology_partial");
                }
                if (!startupDiscoveryPathReal || !startupSurfaceAligned)
                {
                    reasons.Add("startup_discovery_path_weakened");
                }
                break;

            case "copied_only":
                if (packageFilesPresentCount > 0)
                {
                    reasons.Add("schema_files_present_without_materialization");
                }
                if (!packageSurfaceAligned)
                {
                    reasons.Add("stale_companion_artifacts");
                }
                if (!startupDiscoveryPathReal)
                {
                    reasons.Add("copied_without_package_alignment");
                }
                if (governedFilesPresentCount == 0 || !agentsMdExists)
                {
                    reasons.Add("folder_topology_mismatch");
                }
                reasons.Add("schema_present_not_governing");
                break;

            case "fully_missing":
                if (packageFilesPresentCount == 0)
                {
                    reasons.Add("schema_package_absent");
                    reasons.Add("package_surface_absent");
                }
                if (!agentsMdExists)
                {
                    reasons.Add("startup_surface_absent");
                }
                break;
        }

        return reasons.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string[] BuildSafeRepairs(string state, IReadOnlyDictionary<string, bool> startupSurfaceExists, string integrityState)
    {
        var repairs = state switch
        {
            "real" => new List<string>(),
            "partial" =>
            [
                "complete_missing_governed_surfaces",
                "refresh_package_surface",
                "realign_startup_discovery_paths",
                ..MissingStartupSurfaceRepairs(startupSurfaceExists)
            ],
            "copied_only" =>
            [
                "materialize_governed_AGENTS_family",
                "refresh_package_surface",
                "realign_startup_discovery_paths",
                "update_stale_companion_artifacts",
                ..MissingStartupSurfaceRepairs(startupSurfaceExists)
            ],
            _ =>
            [
                "materialize_startup_surface",
                "copy_schema_package"
            ]
        };

        if (integrityState == "diverged")
        {
            repairs.Add("refresh_from_canonical_schema_bundle");
            repairs.Add("audit_for_schema_tampering");
        }

        return repairs.Distinct(StringComparer.Ordinal).ToArray();
    }

    private IntegrityResult EvaluateIntegrity(string workspaceRoot)
    {
        if (!_canonicalSchemaBundle.IsAvailable)
        {
            return new IntegrityResult(
                IntegrityState: "unknown",
                Findings: ["canonical_schema_bundle_unavailable"],
                AlignedFiles: [],
                DivergedFiles: [],
                MissingFiles: []);
        }

        var findings = new List<string>();
        var alignedFiles = new List<string>();
        var divergedFiles = new List<string>();
        var missingFiles = new List<string>();

        foreach (var entry in _canonicalSchemaBundle.FileHashes.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var workspaceFilePath = Path.Combine(workspaceRoot, entry.Key);
            if (!File.Exists(workspaceFilePath))
            {
                missingFiles.Add(entry.Key);
                findings.Add($"canonical_schema_file_missing:{entry.Key}");
                continue;
            }

            var actualHash = ComputeSha256(workspaceFilePath);
            if (string.Equals(actualHash, entry.Value, StringComparison.OrdinalIgnoreCase))
            {
                alignedFiles.Add(entry.Key);
                continue;
            }

            divergedFiles.Add(entry.Key);
            findings.Add($"schema_hash_mismatch:{entry.Key}");
        }

        var integrityState = findings.Count == 0 ? "aligned" : "diverged";
        return new IntegrityResult(
            IntegrityState: integrityState,
            Findings: findings.ToArray(),
            AlignedFiles: alignedFiles.ToArray(),
            DivergedFiles: divergedFiles.ToArray(),
            MissingFiles: missingFiles.ToArray());
    }

    private static string BuildPossessionState(string schemaRealityState, string integrityState)
    {
        if (integrityState == "unknown")
        {
            return "unknown";
        }

        if (schemaRealityState == "fully_missing")
        {
            return "not_applicable";
        }

        return integrityState == "diverged" ? "possessed" : "unpossessed";
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string[] MissingStartupSurfaceRepairs(IReadOnlyDictionary<string, bool> startupSurfaceExists)
    {
        return startupSurfaceExists
            .Where(static pair => !pair.Value)
            .Select(static pair => $"create_startup_surface:{Path.GetFileName(pair.Key)}")
            .ToArray();
    }

    private static string[] NormalizeStartupSurfaces(string workspaceRoot, string[]? startupSurfaces)
    {
        if (startupSurfaces is null || startupSurfaces.Length == 0)
        {
            return [];
        }

        return startupSurfaces
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.IsPathRooted(value) ? value : Path.Combine(workspaceRoot, value))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record IntegrityResult(
        string IntegrityState,
        string[] Findings,
        string[] AlignedFiles,
        string[] DivergedFiles,
        string[] MissingFiles);
}

internal sealed class Gov2GovMigrationRunner(SchemaRealityInspector schemaRealityInspector)
{
    private readonly CanonicalSchemaBundle _canonicalSchemaBundle = CanonicalSchemaBundle.TryLoad();

    public object Run(
        string workspaceRoot,
        string expectedSchemaPackage,
        string schemaRealityState,
        string[] activeReasons,
        string[]? startupSurfaces,
        string migrationMode)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathRooted(workspaceRoot))
        {
            throw new ArgumentException("workspace_root must be an absolute path.", nameof(workspaceRoot));
        }

        if (migrationMode is not "plan_only" and not "non_destructive_apply")
        {
            throw new ArgumentException("migration_mode must be plan_only or non_destructive_apply.", nameof(migrationMode));
        }

        if (schemaRealityState is not "real" and not "partial" and not "copied_only")
        {
            throw new ArgumentException("schema_reality_state must be real, partial, or copied_only.", nameof(schemaRealityState));
        }

        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(resolvedWorkspaceRoot))
        {
            throw new DirectoryNotFoundException($"Workspace root not found: {resolvedWorkspaceRoot}");
        }

        var preEvaluation = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(resolvedWorkspaceRoot, expectedSchemaPackage, startupSurfaces));
        var preIntegrityState = preEvaluation.GetProperty("integrity_state").GetString() ?? "unknown";
        var prePossessionState = preEvaluation.GetProperty("possession_state").GetString() ?? "unknown";

        var plannedActions = new List<string>();
        var actionsTaken = new List<string>();
        var touchedSurfaces = new List<string>();
        var auditNeeded = new List<string>();

        if (!_canonicalSchemaBundle.IsAvailable)
        {
            auditNeeded.Add("canonical_schema_bundle_unavailable");
        }
        else
        {
            foreach (var fileName in _canonicalSchemaBundle.CanonicalFileNames.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase))
            {
                var workspaceFilePath = Path.Combine(resolvedWorkspaceRoot, fileName);
                touchedSurfaces.Add(fileName);

                if (!_canonicalSchemaBundle.TryResolveFilePath(fileName, out var bundleFilePath))
                {
                    auditNeeded.Add($"canonical_schema_source_missing:{fileName}");
                    continue;
                }

                if (!File.Exists(workspaceFilePath))
                {
                    plannedActions.Add($"copy_missing_canonical_schema_file:{fileName}");
                    if (migrationMode == "non_destructive_apply")
                    {
                        File.Copy(bundleFilePath, workspaceFilePath, overwrite: false);
                        actionsTaken.Add($"copied_canonical_schema_file:{fileName}");
                    }

                    continue;
                }

                var workspaceHash = ComputeSha256(workspaceFilePath);
                var bundleHash = _canonicalSchemaBundle.FileHashes[fileName];
                if (!string.Equals(workspaceHash, bundleHash, StringComparison.OrdinalIgnoreCase))
                {
                    plannedActions.Add($"audit_diverged_canonical_schema_file:{fileName}");
                    auditNeeded.Add($"canonical_schema_diverged:{fileName}");
                }
            }
        }

        if (activeReasons.Contains("package_surface_out_of_sync", StringComparer.Ordinal) ||
            activeReasons.Contains("stale_companion_artifacts", StringComparer.Ordinal))
        {
            plannedActions.Add("refresh_package_surface");
        }

        if (activeReasons.Contains("copied_without_package_alignment", StringComparer.Ordinal) ||
            activeReasons.Contains("startup_discovery_path_weakened", StringComparer.Ordinal))
        {
            plannedActions.Add("realign_startup_discovery_paths");
        }

        if (activeReasons.Contains("folder_topology_partial", StringComparer.Ordinal) ||
            activeReasons.Contains("folder_topology_mismatch", StringComparer.Ordinal) ||
            activeReasons.Contains("schema_present_not_governing", StringComparer.Ordinal))
        {
            plannedActions.Add("materialize_missing_governed_surfaces");
            auditNeeded.Add("governed_surfaces_require_human_authorship");
        }

        if (prePossessionState == "possessed" || preIntegrityState == "diverged")
        {
            plannedActions.Add("audit_for_schema_tampering");
        }

        var postEvaluation = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(resolvedWorkspaceRoot, expectedSchemaPackage, startupSurfaces));
        var postRealityState = postEvaluation.GetProperty("schema_reality_state").GetString() ?? "fully_missing";
        var postIntegrityState = postEvaluation.GetProperty("integrity_state").GetString() ?? "unknown";
        var postPossessionState = postEvaluation.GetProperty("possession_state").GetString() ?? "unknown";
        var remainingFindings = postEvaluation
            .GetProperty("active_reasons")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        var integrityFindings = postEvaluation
            .GetProperty("integrity_findings")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        auditNeeded.AddRange(integrityFindings);

        var migrationResultState = DetermineMigrationResultState(
            migrationMode,
            plannedActions,
            actionsTaken,
            auditNeeded,
            postRealityState,
            postIntegrityState);

        return new
        {
            migration_result_state = migrationResultState,
            planned_actions = plannedActions.Distinct(StringComparer.Ordinal).ToArray(),
            actions_taken = actionsTaken.Distinct(StringComparer.Ordinal).ToArray(),
            touched_surfaces = touchedSurfaces.Distinct(StringComparer.Ordinal).ToArray(),
            audit_needed = auditNeeded.Distinct(StringComparer.Ordinal).ToArray(),
            remaining_findings = remainingFindings.Distinct(StringComparer.Ordinal).ToArray(),
            resulting_schema_reality_state = postRealityState,
            resulting_integrity_state = postIntegrityState,
            resulting_possession_state = postPossessionState
        };
    }

    private static string DetermineMigrationResultState(
        string migrationMode,
        List<string> plannedActions,
        List<string> actionsTaken,
        List<string> auditNeeded,
        string postRealityState,
        string postIntegrityState)
    {
        var hasPlannedChanges = plannedActions.Count > 0;
        var hasAppliedChanges = actionsTaken.Count > 0;
        var hasAuditPressure = auditNeeded.Count > 0;

        if (!hasPlannedChanges && !hasAppliedChanges && !hasAuditPressure)
        {
            return "no_action_needed";
        }

        if (migrationMode == "plan_only")
        {
            return "planned";
        }

        if (hasAppliedChanges && postRealityState == "real" && postIntegrityState == "aligned" && !hasAuditPressure)
        {
            return "materialized";
        }

        if (hasAppliedChanges)
        {
            return "partially_applied";
        }

        return "manual_review_required";
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal sealed class ActiveWorkStateCompiler
{
    private static readonly string[] KnownLanes =
    [
        "governance",
        "gov2gov",
        "narrative",
        "1project",
        "triage"
    ];

    public object Compile(
        string workspaceRoot,
        string currentObjective,
        string[]? workingSet,
        string[]? knownBlockers,
        string[]? recentResidue,
        string? preferredLane)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathRooted(workspaceRoot))
        {
            throw new ArgumentException("workspace_root must be an absolute path.", nameof(workspaceRoot));
        }

        if (string.IsNullOrWhiteSpace(currentObjective))
        {
            throw new ArgumentException("current_objective is required.", nameof(currentObjective));
        }

        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(resolvedWorkspaceRoot))
        {
            throw new DirectoryNotFoundException($"Workspace root not found: {resolvedWorkspaceRoot}");
        }

        var normalizedObjective = NormalizeWhitespace(currentObjective);
        var normalizedWorkingSet = NormalizeArray(workingSet);
        var normalizedBlockers = NormalizeArray(knownBlockers);
        var normalizedResidue = NormalizeArray(recentResidue);
        var normalizedPreferredLane = NormalizeLane(preferredLane);

        var workingSurfacePresence = EvaluateWorkingSurfacePresence(resolvedWorkspaceRoot, normalizedWorkingSet);
        var laneInference = InferLane(
            normalizedObjective,
            normalizedWorkingSet,
            normalizedResidue,
            normalizedPreferredLane);

        var evidenceStatus = DetermineEvidenceStatus(
            workingSurfacePresence.Present.Count,
            normalizedWorkingSet.Length,
            normalizedResidue.Length,
            File.Exists(Path.Combine(resolvedWorkspaceRoot, "AGENTS.md")));

        var currentStatus = DetermineCurrentStatus(
            normalizedObjective,
            normalizedBlockers.Length,
            normalizedWorkingSet.Length,
            normalizedResidue.Length,
            evidenceStatus);

        var degradationSignals = BuildDegradationSignals(
            normalizedObjective,
            normalizedWorkingSet.Length,
            normalizedBlockers.Length,
            normalizedResidue.Length,
            workingSurfacePresence.Missing.Count,
            laneInference.CandidateLanes.Length,
            evidenceStatus,
            currentStatus);

        var nextRequiredAction = DetermineNextRequiredAction(
            laneInference.ActiveLane,
            currentStatus,
            evidenceStatus,
            workingSurfacePresence.Missing.Count);

        var orderedRemainingSteps = BuildOrderedRemainingSteps(
            laneInference.ActiveLane,
            nextRequiredAction,
            currentStatus);

        var stopPoint = BuildStopPoint(
            normalizedBlockers,
            normalizedResidue,
            workingSurfacePresence.Missing,
            nextRequiredAction);

        var measurementBasis = BuildMeasurementBasis(
            resolvedWorkspaceRoot,
            normalizedWorkingSet.Length,
            workingSurfacePresence.Present.Count,
            normalizedBlockers.Length,
            normalizedResidue.Length,
            normalizedPreferredLane);

        return new
        {
            active_objective = normalizedObjective,
            active_lane = laneInference.ActiveLane,
            current_status = currentStatus,
            next_required_action = nextRequiredAction,
            ordered_remaining_steps = orderedRemainingSteps,
            active_blockers = normalizedBlockers,
            stop_point = stopPoint,
            evidence_status = evidenceStatus,
            session_degradation_signals = degradationSignals,
            working_surface_presence = new
            {
                present = workingSurfacePresence.Present.ToArray(),
                missing = workingSurfacePresence.Missing.ToArray()
            },
            measurement_basis = measurementBasis
        };
    }

    private static string[] NormalizeArray(string[]? values)
    {
        return values is null
            ? []
            : values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(NormalizeWhitespace)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NormalizeLane(string? preferredLane)
    {
        if (string.IsNullOrWhiteSpace(preferredLane))
        {
            return null;
        }

        var normalized = NormalizeWhitespace(preferredLane).ToLowerInvariant();
        return KnownLanes.Contains(normalized, StringComparer.Ordinal) ? normalized : normalized;
    }

    private static (List<string> Present, List<string> Missing) EvaluateWorkingSurfacePresence(string workspaceRoot, string[] workingSet)
    {
        var present = new List<string>();
        var missing = new List<string>();

        foreach (var surface in workingSet)
        {
            if (!IsPathLike(surface))
            {
                continue;
            }

            var resolvedPath = Path.IsPathRooted(surface)
                ? surface
                : Path.Combine(workspaceRoot, surface);

            if (File.Exists(resolvedPath) || Directory.Exists(resolvedPath))
            {
                present.Add(surface);
            }
            else
            {
                missing.Add(surface);
            }
        }

        return (present, missing);
    }

    private static bool IsPathLike(string value)
    {
        if (Path.IsPathRooted(value))
        {
            return true;
        }

        if (value.Contains('\\') || value.Contains('/'))
        {
            return true;
        }

        if (value.StartsWith(".", StringComparison.Ordinal))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(Path.GetExtension(value));
    }

    private static LaneInference InferLane(
        string currentObjective,
        string[] workingSet,
        string[] recentResidue,
        string? preferredLane)
    {
        if (!string.IsNullOrWhiteSpace(preferredLane))
        {
            return new LaneInference(preferredLane, [preferredLane]);
        }

        var corpus = string.Join(" ", new[] { currentObjective }
            .Concat(workingSet)
            .Concat(recentResidue))
            .ToLowerInvariant();

        var scores = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["governance"] = Score(corpus, "governance", "agents", "schema", "rules", "terms", "vision", "pitfalls"),
            ["gov2gov"] = Score(corpus, "gov2gov", "migration", "reconcile", "materialize", "copied", "shadow"),
            ["narrative"] = Score(corpus, "narrative", "review", "compress", "editorial", "story"),
            ["1project"] = Score(corpus, "project", "deliverable", "milestone", "backlog", "roadmap", "task"),
            ["triage"] = Score(corpus, "triage", "stuck", "unclear", "unknown", "which", "diagnose", "debug")
        };

        var ranked = scores
            .Where(static pair => pair.Value > 0)
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal)
            .ToArray();

        if (ranked.Length == 0)
        {
            return new LaneInference("general", []);
        }

        var topScore = ranked[0].Value;
        var candidateLanes = ranked
            .Where(pair => pair.Value == topScore)
            .Select(static pair => pair.Key)
            .ToArray();

        return new LaneInference(candidateLanes[0], candidateLanes);
    }

    private static int Score(string corpus, params string[] keywords)
    {
        var score = 0;
        foreach (var keyword in keywords)
        {
            if (corpus.Contains(keyword, StringComparison.Ordinal))
            {
                score++;
            }
        }

        return score;
    }

    private static string DetermineEvidenceStatus(
        int presentWorkingSurfaceCount,
        int declaredWorkingSetCount,
        int recentResidueCount,
        bool agentsMdExists)
    {
        if (presentWorkingSurfaceCount > 0 || recentResidueCount >= 2)
        {
            return "grounded";
        }

        if (declaredWorkingSetCount > 0 || recentResidueCount == 1 || agentsMdExists)
        {
            return "partial";
        }

        return "none";
    }

    private static string DetermineCurrentStatus(
        string currentObjective,
        int blockerCount,
        int declaredWorkingSetCount,
        int recentResidueCount,
        string evidenceStatus)
    {
        if (blockerCount > 0)
        {
            return "blocked";
        }

        if (currentObjective.Length < 12 ||
            (evidenceStatus == "none" && declaredWorkingSetCount == 0 && recentResidueCount == 0))
        {
            return "needs_clarification";
        }

        if (declaredWorkingSetCount > 0 || recentResidueCount > 0 || evidenceStatus == "grounded")
        {
            return "in_progress";
        }

        return "ready";
    }

    private static string[] BuildDegradationSignals(
        string currentObjective,
        int declaredWorkingSetCount,
        int blockerCount,
        int recentResidueCount,
        int missingWorkingSurfaceCount,
        int candidateLaneCount,
        string evidenceStatus,
        string currentStatus)
    {
        var signals = new List<string>();

        if (blockerCount > 0)
        {
            signals.Add("blocker_pressure");
        }

        if (declaredWorkingSetCount == 0)
        {
            signals.Add("no_declared_working_set");
        }

        if (recentResidueCount == 0)
        {
            signals.Add("no_recent_residue");
        }

        if (missingWorkingSurfaceCount > 0)
        {
            signals.Add("missing_declared_working_surfaces");
        }

        if (candidateLaneCount > 1)
        {
            signals.Add("multi_lane_pressure");
        }

        if (evidenceStatus == "none")
        {
            signals.Add("weak_evidence");
        }

        if (currentStatus == "needs_clarification")
        {
            signals.Add("objective_underdefined");
        }

        if (IsNegationHeavy(currentObjective))
        {
            signals.Add("negation_heavy_direction");
        }

        return signals.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool IsNegationHeavy(string currentObjective)
    {
        var lowered = currentObjective.ToLowerInvariant();
        string[] tokens =
        [
            " not ",
            " don't ",
            " dont ",
            " no ",
            " avoid ",
            " stop ",
            " wrong "
        ];

        return tokens.Count(token => lowered.Contains(token, StringComparison.Ordinal)) >= 2;
    }

    private static string DetermineNextRequiredAction(
        string activeLane,
        string currentStatus,
        string evidenceStatus,
        int missingWorkingSurfaceCount)
    {
        if (currentStatus == "blocked")
        {
            return "resolve_or_report_blockers";
        }

        if (currentStatus == "needs_clarification")
        {
            return "clarify_objective_and_scope";
        }

        if (missingWorkingSurfaceCount > 0)
        {
            return "reconcile_missing_declared_working_surfaces";
        }

        if (evidenceStatus == "none")
        {
            return "identify_declared_working_surfaces";
        }

        return activeLane switch
        {
            "governance" => "inspect_startup_truth_chain",
            "gov2gov" => "inspect_source_and_target_surfaces",
            "narrative" => "inspect_review_context_and_sources",
            "1project" => "inspect_active_project_surfaces",
            "triage" => "determine_correct_lane",
            _ => "inspect_declared_working_surfaces"
        };
    }

    private static string[] BuildOrderedRemainingSteps(string activeLane, string nextRequiredAction, string currentStatus)
    {
        var steps = new List<string> { nextRequiredAction };

        if (currentStatus == "blocked")
        {
            steps.AddRange(
            [
                "re-evaluate_active_work_state",
                "inspect_declared_working_surfaces",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ]);

            return steps.Distinct(StringComparer.Ordinal).ToArray();
        }

        steps.AddRange(activeLane switch
        {
            "governance" =>
            [
                "inspect_declared_working_surfaces",
                "confirm_current_objective_against_workspace_state",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ],
            "gov2gov" =>
            [
                "classify_materialization_and_integrity_state",
                "plan_non_destructive_reconciliation",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ],
            "narrative" =>
            [
                "confirm_active_edit_mode",
                "inspect_declared_working_surfaces",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ],
            "1project" =>
            [
                "confirm_deliverable_and_remaining_work",
                "inspect_declared_working_surfaces",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ],
            "triage" =>
            [
                "inspect_startup_context_and_problem_shape",
                "inspect_declared_working_surfaces",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ],
            _ =>
            [
                "confirm_current_objective_against_workspace_state",
                "inspect_declared_working_surfaces",
                "execute_next_required_change",
                "record_stop_point_and_evidence"
            ]
        });

        return steps.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string BuildStopPoint(
        string[] knownBlockers,
        string[] recentResidue,
        List<string> missingWorkingSurfaces,
        string nextRequiredAction)
    {
        if (knownBlockers.Length > 0)
        {
            return Truncate($"blocked on {knownBlockers[0]}");
        }

        if (recentResidue.Length > 0)
        {
            return Truncate($"resume from {recentResidue[^1]}");
        }

        if (missingWorkingSurfaces.Count > 0)
        {
            return Truncate($"resume from missing surface {missingWorkingSurfaces[0]}");
        }

        return Truncate($"resume with {nextRequiredAction}");
    }

    private static string[] BuildMeasurementBasis(
        string workspaceRoot,
        int declaredWorkingSetCount,
        int presentWorkingSurfaceCount,
        int blockerCount,
        int recentResidueCount,
        string? preferredLane)
    {
        var basis = new List<string>
        {
            "workspace_root",
            "current_objective"
        };

        if (declaredWorkingSetCount > 0)
        {
            basis.Add("working_set");
        }

        if (presentWorkingSurfaceCount > 0)
        {
            basis.Add("observable_working_surfaces");
        }

        if (blockerCount > 0)
        {
            basis.Add("known_blockers");
        }

        if (recentResidueCount > 0)
        {
            basis.Add("recent_residue");
        }

        if (!string.IsNullOrWhiteSpace(preferredLane))
        {
            basis.Add("preferred_lane");
        }

        if (File.Exists(Path.Combine(workspaceRoot, "AGENTS.md")))
        {
            basis.Add("AGENTS.md");
        }

        return basis.ToArray();
    }

    private static string Truncate(string value)
    {
        return value.Length <= 140 ? value : $"{value[..137]}...";
    }

    private sealed record LaneInference(string ActiveLane, string[] CandidateLanes);
}

internal sealed class HarnessGapAssessor(SchemaRealityInspector schemaRealityInspector)
{
    private static readonly string[] ExpectedContractFiles =
    [
        "active-work-state.contract.json",
        "schema-reality.contract.json",
        "gov2gov-migration.contract.json",
        "preflight-session.contract.json",
        "harness-gap-state.contract.json"
    ];

    public object Assess(string workspaceRoot, string? hostContext, string[]? expectedCapabilities)
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

        var normalizedHostContext = NormalizeHostContext(hostContext);
        var normalizedExpectedCapabilities = NormalizeExpectedCapabilities(expectedCapabilities);

        var pluginRoot = Path.Combine(resolvedWorkspaceRoot, "plugins", "anarchy-ai");
        var pluginManifestPath = Path.Combine(pluginRoot, ".codex-plugin", "plugin.json");
        var mcpDeclarationPath = Path.Combine(pluginRoot, ".mcp.json");
        var runtimePath = Path.Combine(pluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe");
        var skillPath = Path.Combine(pluginRoot, "skills", "anarchy-ai-harness", "SKILL.md");
        var schemaManifestPath = Path.Combine(pluginRoot, "schemas", "schema-bundle.manifest.json");

        var contractPresence = ExpectedContractFiles.ToDictionary(
            fileName => fileName,
            fileName => File.Exists(Path.Combine(pluginRoot, "contracts", fileName)),
            StringComparer.OrdinalIgnoreCase);

        var pluginManifestExists = File.Exists(pluginManifestPath);
        var mcpDeclarationExists = File.Exists(mcpDeclarationPath);
        var runtimeExists = File.Exists(runtimePath);
        var skillExists = File.Exists(skillPath);
        var schemaManifestExists = File.Exists(schemaManifestPath);
        var anyRepoBundleSurfacePresent =
            Directory.Exists(pluginRoot) ||
            pluginManifestExists ||
            mcpDeclarationExists ||
            runtimeExists ||
            skillExists ||
            schemaManifestExists ||
            contractPresence.Values.Any(static value => value);

        var marketplaceInspection = InspectMarketplace(resolvedWorkspaceRoot);

        var schemaElement = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(resolvedWorkspaceRoot, "AGENTS-schema-family", null));

        var schemaRealityState = GetString(schemaElement, "schema_reality_state");
        var integrityState = GetString(schemaElement, "integrity_state");
        var possessionState = GetString(schemaElement, "possession_state");
        var schemaSafeRepairs = GetStringArray(schemaElement, "safe_repairs");
        var schemaRecommendedAction = GetString(schemaElement, "recommended_next_action");
        var schemaActiveReasons = GetStringArray(schemaElement, "active_reasons");

        var missingComponents = new List<string>();

        if (!runtimeExists)
        {
            missingComponents.Add("bundled_runtime_missing");
        }

        if (!schemaManifestExists)
        {
            missingComponents.Add("schema_bundle_manifest_missing");
        }

        foreach (var entry in contractPresence.Where(static pair => !pair.Value))
        {
            missingComponents.Add($"missing_contract:{entry.Key}");
        }

        if (normalizedHostContext is "codex" or "generic")
        {
            if (!pluginManifestExists)
            {
                missingComponents.Add("codex_plugin_manifest_missing");
            }

            if (!mcpDeclarationExists)
            {
                missingComponents.Add("codex_mcp_declaration_missing");
            }

            if (!skillExists)
            {
                missingComponents.Add("codex_skill_surface_missing");
            }

            if (!marketplaceInspection.Exists)
            {
                missingComponents.Add("repo_marketplace_missing");
            }
            else if (!marketplaceInspection.HasAnarchyPluginEntry)
            {
                missingComponents.Add("anarchy_ai_marketplace_entry_missing");
            }
            else if (!marketplaceInspection.InstalledByDefault)
            {
                missingComponents.Add("anarchy_ai_not_installed_by_default");
            }

            missingComponents.AddRange(marketplaceInspection.Findings);
        }

        if (normalizedHostContext == "claude")
        {
            missingComponents.Add("claude_adapter_not_packaged");
        }

        if (normalizedHostContext == "cursor")
        {
            missingComponents.Add("cursor_adapter_not_implemented");
        }

        if (normalizedExpectedCapabilities.Contains("assess_last_exchange_and_improve", StringComparer.Ordinal))
        {
            missingComponents.Add("reflection_workflow_not_available");
        }

        var installationState = DetermineInstallationState(
            anyRepoBundleSurfacePresent,
            pluginManifestExists,
            mcpDeclarationExists,
            runtimeExists,
            normalizedHostContext,
            marketplaceInspection);

        var runtimeState = DetermineRuntimeState(runtimeExists, anyRepoBundleSurfacePresent);
        var adoptionState = DetermineAdoptionState(
            installationState,
            schemaRealityState,
            integrityState,
            possessionState,
            normalizedHostContext,
            missingComponents);

        var safeRepairs = new List<string>();
        var adminActions = new List<string>();
        var agentActions = new List<string>();

        if (installationState is "bootstrap_needed" or "repo_bundle_present_unregistered")
        {
            safeRepairs.Add("run_repo_bootstrap_script");
            adminActions.Add("run_bootstrap_harness_install");
        }

        if (!runtimeExists)
        {
            safeRepairs.Add("publish_or_restore_bundled_runtime");
            adminActions.Add("restore_or_publish_windows_runtime");
        }

        if (!schemaManifestExists)
        {
            safeRepairs.Add("refresh_bundled_schema_bundle");
        }

        if (contractPresence.Values.Any(static value => !value))
        {
            safeRepairs.Add("refresh_bundled_contracts");
        }

        if (normalizedHostContext == "claude")
        {
            safeRepairs.Add("define_claude_mcp_registration");
            adminActions.Add("package_claude_instruction_surface");
        }

        if (normalizedHostContext == "cursor")
        {
            safeRepairs.Add("define_cursor_adapter_strategy");
            adminActions.Add("defer_cursor_to_later_adapter_pass");
        }

        if (schemaRecommendedAction == "run_gov2gov_migration")
        {
            agentActions.Add("run_gov2gov_migration:plan_only");
        }

        agentActions.Add("run_preflight_session_before_meaningful_work");
        agentActions.Add("treat_schema_state_as_input_not_completion");
        safeRepairs.AddRange(schemaSafeRepairs);

        return new
        {
            installation_state = installationState,
            runtime_state = runtimeState,
            schema_state = new
            {
                schema_reality_state = schemaRealityState,
                integrity_state = integrityState,
                possession_state = possessionState,
                active_reasons = schemaActiveReasons
            },
            adoption_state = adoptionState,
            missing_components = missingComponents.Distinct(StringComparer.Ordinal).ToArray(),
            safe_repairs = safeRepairs.Distinct(StringComparer.Ordinal).ToArray(),
            admin_actions = adminActions.Distinct(StringComparer.Ordinal).ToArray(),
            agent_actions = agentActions.Distinct(StringComparer.Ordinal).ToArray(),
            evidence_basis = new[]
            {
                "repo_plugin_surfaces",
                "runtime_presence",
                "contract_presence",
                "schema_bundle_manifest",
                "marketplace_registration",
                "schema_reality_inspection",
                $"host_context:{normalizedHostContext}"
            },
            inspection = new
            {
                workspace_root = resolvedWorkspaceRoot,
                host_context = normalizedHostContext,
                expected_capabilities = normalizedExpectedCapabilities,
                plugin_root = pluginRoot,
                plugin_manifest_exists = pluginManifestExists,
                mcp_declaration_exists = mcpDeclarationExists,
                runtime_exists = runtimeExists,
                skill_exists = skillExists,
                schema_manifest_exists = schemaManifestExists,
                contract_presence = contractPresence,
                marketplace_path = marketplaceInspection.Path,
                marketplace_exists = marketplaceInspection.Exists,
                marketplace_entry_present = marketplaceInspection.HasAnarchyPluginEntry,
                marketplace_installed_by_default = marketplaceInspection.InstalledByDefault
            }
        };
    }

    private static string DetermineInstallationState(
        bool anyRepoBundleSurfacePresent,
        bool pluginManifestExists,
        bool mcpDeclarationExists,
        bool runtimeExists,
        string hostContext,
        MarketplaceInspection marketplaceInspection)
    {
        var repoBundleReady = pluginManifestExists && mcpDeclarationExists && runtimeExists;
        var repoRegistrationSatisfied = hostContext is not ("codex" or "generic")
            || (marketplaceInspection.HasAnarchyPluginEntry && marketplaceInspection.InstalledByDefault);

        if (repoBundleReady && repoRegistrationSatisfied)
        {
            return "repo_bootstrapped";
        }

        if (anyRepoBundleSurfacePresent)
        {
            return "repo_bundle_present_unregistered";
        }

        return "external_runtime_only";
    }

    private static string DetermineRuntimeState(bool runtimeExists, bool anyRepoBundleSurfacePresent)
    {
        if (runtimeExists)
        {
            return "callable_and_repo_bundled";
        }

        return anyRepoBundleSurfacePresent ? "repo_runtime_missing" : "callable_external";
    }

    private static string DetermineAdoptionState(
        string installationState,
        string schemaRealityState,
        string integrityState,
        string possessionState,
        string hostContext,
        List<string> missingComponents)
    {
        var hasAdapterGap = hostContext is "claude" or "cursor";
        if (hasAdapterGap)
        {
            return "adapter_gap_present";
        }

        if (installationState == "external_runtime_only")
        {
            return "bootstrap_required";
        }

        if (schemaRealityState == "real" &&
            integrityState == "aligned" &&
            possessionState == "unpossessed" &&
            missingComponents.Count == 0 &&
            installationState == "repo_bootstrapped")
        {
            return "fully_adopted";
        }

        return "partially_adopted";
    }

    private static string NormalizeHostContext(string? hostContext)
    {
        if (string.IsNullOrWhiteSpace(hostContext))
        {
            return "codex";
        }

        var normalized = hostContext.Trim().ToLowerInvariant();
        return normalized switch
        {
            "codex" => "codex",
            "claude" => "claude",
            "cursor" => "cursor",
            _ => "generic"
        };
    }

    private static string[] NormalizeExpectedCapabilities(string[]? expectedCapabilities)
    {
        var normalized = expectedCapabilities?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return normalized is { Length: > 0 }
            ? normalized
            : [
                "preflight_session",
                "compile_active_work_state",
                "is_schema_real_or_shadow_copied",
                "run_gov2gov_migration",
                "assess_harness_gap_state"
            ];
    }

    private static MarketplaceInspection InspectMarketplace(string workspaceRoot)
    {
        var marketplacePath = Path.Combine(workspaceRoot, ".agents", "plugins", "marketplace.json");
        if (!File.Exists(marketplacePath))
        {
            return new MarketplaceInspection(marketplacePath, false, false, false, []);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(marketplacePath));
            if (!document.RootElement.TryGetProperty("plugins", out var pluginsElement) ||
                pluginsElement.ValueKind != JsonValueKind.Array)
            {
                return new MarketplaceInspection(
                    marketplacePath,
                    true,
                    false,
                    false,
                    ["marketplace_plugins_array_missing"]);
            }

            var findings = new List<string>();
            var hasEntry = false;
            var installedByDefault = false;

            foreach (var pluginElement in pluginsElement.EnumerateArray())
            {
                if (!pluginElement.TryGetProperty("name", out var nameElement) ||
                    !string.Equals(nameElement.GetString(), "anarchy-ai", StringComparison.Ordinal))
                {
                    continue;
                }

                hasEntry = true;

                if (pluginElement.TryGetProperty("policy", out var policyElement) &&
                    policyElement.TryGetProperty("installation", out var installationElement) &&
                    string.Equals(installationElement.GetString(), "INSTALLED_BY_DEFAULT", StringComparison.Ordinal))
                {
                    installedByDefault = true;
                }
                else
                {
                    findings.Add("anarchy_ai_installation_policy_not_default");
                }

                if (pluginElement.TryGetProperty("source", out var sourceElement) &&
                    sourceElement.TryGetProperty("path", out var pathElement))
                {
                    var sourcePath = pathElement.GetString();
                    if (!string.Equals(sourcePath, "./plugins/anarchy-ai", StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add("anarchy_ai_marketplace_path_unexpected");
                    }
                }
            }

            return new MarketplaceInspection(marketplacePath, true, hasEntry, installedByDefault, findings.ToArray());
        }
        catch (JsonException)
        {
            return new MarketplaceInspection(
                marketplacePath,
                true,
                false,
                false,
                ["marketplace_json_invalid"]);
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var propertyElement) &&
               propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return propertyElement
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    private sealed record MarketplaceInspection(
        string Path,
        bool Exists,
        bool HasAnarchyPluginEntry,
        bool InstalledByDefault,
        string[] Findings);
}

internal sealed class PreflightSessionRunner(
    HarnessGapAssessor harnessGapAssessor,
    SchemaRealityInspector schemaRealityInspector,
    ActiveWorkStateCompiler activeWorkStateCompiler)
{
    public object Run(
        string workspaceRoot,
        string currentObjective,
        string? hostContext,
        string[]? startupSurfaces,
        string? userIntent)
    {
        if (string.IsNullOrWhiteSpace(currentObjective))
        {
            throw new ArgumentException("current_objective is required.", nameof(currentObjective));
        }

        var activeWorkElement = JsonSerializer.SerializeToElement(
            activeWorkStateCompiler.Compile(
                workspaceRoot,
                currentObjective,
                null,
                null,
                string.IsNullOrWhiteSpace(userIntent) ? null : [userIntent],
                null));

        var gapElement = JsonSerializer.SerializeToElement(
            harnessGapAssessor.Assess(
                workspaceRoot,
                hostContext,
                [
                    "preflight_session",
                    "compile_active_work_state",
                    "is_schema_real_or_shadow_copied",
                    "run_gov2gov_migration",
                    "assess_harness_gap_state"
                ]));

        var schemaElement = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(workspaceRoot, "AGENTS-schema-family", startupSurfaces));

        var currentStatus = GetString(activeWorkElement, "current_status");
        var evidenceStatus = GetString(activeWorkElement, "evidence_status");
        var nextRequiredAction = GetString(activeWorkElement, "next_required_action");
        var activeLane = GetString(activeWorkElement, "active_lane");

        var installationState = GetString(gapElement, "installation_state");
        var adoptionState = GetString(gapElement, "adoption_state");

        var schemaRealityState = GetString(schemaElement, "schema_reality_state");
        var integrityState = GetString(schemaElement, "integrity_state");
        var possessionState = GetString(schemaElement, "possession_state");

        var missingComponents = GetStringArray(gapElement, "missing_components");
        var schemaReasons = GetStringArray(schemaElement, "active_reasons");
        var degradationSignals = GetStringArray(activeWorkElement, "session_degradation_signals");
        var measurementBasis = GetStringArray(activeWorkElement, "measurement_basis");

        var activeGaps = missingComponents
            .Concat(schemaReasons.Select(static value => $"schema:{value}"))
            .Concat(degradationSignals.Select(static value => $"session:{value}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        string preflightState;
        string recommendedPath;

        var bootstrapNeeded = installationState is "bootstrap_needed" or "external_runtime_only" or "repo_bundle_present_unregistered"
            || schemaRealityState == "fully_missing"
            || adoptionState == "bootstrap_required";

        if (bootstrapNeeded)
        {
            preflightState = "bootstrap_needed";
            recommendedPath = "bootstrap_harness";
        }
        else if (currentStatus == "blocked")
        {
            preflightState = "manual_review_required";
            recommendedPath = "stop_for_human";
        }
        else if (schemaRealityState != "real" || integrityState != "aligned" || possessionState == "possessed" || adoptionState != "fully_adopted")
        {
            preflightState = "ready_with_gaps";
            recommendedPath = "inspect_schema_reality";
        }
        else if (currentStatus == "needs_clarification" || evidenceStatus == "none")
        {
            preflightState = "ready_with_gaps";
            recommendedPath = "compile_active_work";
        }
        else
        {
            preflightState = "ready";
            recommendedPath = "continue";
        }

        var requiredAction = recommendedPath switch
        {
            "bootstrap_harness" => "run_repo_bootstrap_script",
            "stop_for_human" => "report_to_human_and_clarify_blockers",
            "inspect_schema_reality" => schemaRealityState is "partial" or "copied_only"
                ? "inspect_schema_reality_and_choose_safe_repair"
                : "audit_canonical_divergence_before_continuing",
            _ => nextRequiredAction
        };

        return new
        {
            preflight_state = preflightState,
            recommended_path = recommendedPath,
            active_gaps = activeGaps,
            required_next_action = requiredAction,
            adoption_state = adoptionState,
            evidence_basis = measurementBasis
                .Concat([
                    "schema_reality_inspection",
                    "harness_gap_assessment"
                ])
                .Concat(string.IsNullOrWhiteSpace(userIntent) ? [] : ["user_intent"])
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            active_lane = activeLane,
            current_status = currentStatus,
            schema_state = new
            {
                schema_reality_state = schemaRealityState,
                integrity_state = integrityState,
                possession_state = possessionState
            }
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var propertyElement) &&
               propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return propertyElement
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }
}

[McpServerToolType]
internal sealed class AnarchyAiHarnessTools(
    SchemaRealityInspector schemaRealityInspector,
    Gov2GovMigrationRunner gov2GovMigrationRunner,
    ActiveWorkStateCompiler activeWorkStateCompiler,
    HarnessGapAssessor harnessGapAssessor,
    PreflightSessionRunner preflightSessionRunner)
{
    [McpServerTool(
        Name = "preflight_session",
        Title = "Preflight Session",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Decide whether the current repo/session is ready for meaningful governed work before the agent continues.")]
    public object PreflightSession(
        [Description("Absolute workspace root to inspect.")] string workspace_root,
        [Description("Current work objective in plain language.")] string current_objective,
        [Description("Optional host context such as codex, claude, cursor, or generic.")] string? host_context = null,
        [Description("Optional startup or package surfaces that should align with the schema package.")] string[]? startup_surfaces = null,
        [Description("Optional user-intent summary that should sharpen preflight assessment.")] string? user_intent = null)
    {
        return preflightSessionRunner.Run(
            workspace_root,
            current_objective,
            host_context,
            startup_surfaces,
            user_intent);
    }

    [McpServerTool(
        Name = "is_schema_real_or_shadow_copied",
        Title = "Is Schema Real Or Shadow Copied",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Determine whether a schema package is real, partial, copied_only, or fully_missing.")]
    public object IsSchemaRealOrShadowCopied(
        [Description("Absolute workspace root to inspect.")] string workspace_root,
        [Description("Expected schema family or package name.")] string expected_schema_package,
        [Description("Optional startup or package surfaces that should align with the schema package.")] string[]? startup_surfaces = null)
    {
        return schemaRealityInspector.Evaluate(workspace_root, expected_schema_package, startup_surfaces);
    }

    [McpServerTool(
        Name = "compile_active_work_state",
        Title = "Compile Active Work State",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Compile current work into a bounded operational packet so the agent can continue from stable state instead of raw session turbulence.")]
    public object CompileActiveWorkState(
        [Description("Absolute workspace root to inspect.")] string workspace_root,
        [Description("Current work objective in plain language.")] string current_objective,
        [Description("Optional files, directories, or named surfaces currently believed to be in play.")] string[]? working_set = null,
        [Description("Explicit blockers already known to the agent or user.")] string[]? known_blockers = null,
        [Description("Short recent facts, interruptions, or unfinished observations that should survive the current turn.")] string[]? recent_residue = null,
        [Description("Optional preferred lane if the work should stay in governance, gov2gov, 1project, narrative, or triage.")] string? preferred_lane = null)
    {
        return activeWorkStateCompiler.Compile(
            workspace_root,
            current_objective,
            working_set,
            known_blockers,
            recent_residue,
            preferred_lane);
    }

    [McpServerTool(
        Name = "assess_harness_gap_state",
        Title = "Assess Harness Gap State",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Assess installation, runtime, schema, and adoption gaps so the harness can report what is missing and what to do next.")]
    public object AssessHarnessGapState(
        [Description("Absolute workspace root to inspect.")] string workspace_root,
        [Description("Optional host context such as codex, claude, cursor, or generic.")] string? host_context = null,
        [Description("Optional capabilities the current environment expects to have available.")] string[]? expected_capabilities = null)
    {
        return harnessGapAssessor.Assess(workspace_root, host_context, expected_capabilities);
    }

    [McpServerTool(
        Name = "run_gov2gov_migration",
        Title = "Run Gov2Gov Migration",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Run non-destructive gov2gov reconciliation for a partial, copied_only, or possessed schema package.")]
    public object RunGov2GovMigration(
        [Description("Absolute workspace root to inspect and reconcile.")] string workspace_root,
        [Description("Expected schema family or package name.")] string expected_schema_package,
        [Description("Input state from the schema-reality tool.")] string schema_reality_state,
        [Description("Reason list from the active schema reality state.")] string[] active_reasons,
        [Description("Optional startup or package surfaces that should align with the schema package.")] string[]? startup_surfaces = null,
        [Description("Plan changes only, or apply only non-destructive reconciliation steps.")] string migration_mode = "plan_only")
    {
        return gov2GovMigrationRunner.Run(
            workspace_root,
            expected_schema_package,
            schema_reality_state,
            active_reasons,
            startup_surfaces,
            migration_mode);
    }
}

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<ContractLoader>();
        builder.Services.AddSingleton<SchemaRealityInspector>();
        builder.Services.AddSingleton<Gov2GovMigrationRunner>();
        builder.Services.AddSingleton<ActiveWorkStateCompiler>();
        builder.Services.AddSingleton<HarnessGapAssessor>();
        builder.Services.AddSingleton<PreflightSessionRunner>();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AnarchyAiHarnessTools>();

        await builder.Build().RunAsync();
    }
}
