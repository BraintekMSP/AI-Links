using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AnarchyAi.Pathing;

namespace AnarchyAi.Mcp.Server;

// Purpose: Loads JSON contracts from the installed bundle or repo-local harness source.
// Expected input: Contract file names requested by the MCP tool layer.
// Expected output: Parsed JsonElement contract documents.
// Critical dependencies: ResolveContractsDirectory, filesystem access, and the carried contract files.
internal sealed class ContractLoader
{
    private readonly string _contractsDir = ResolveContractsDirectory();

    // Purpose: Loads one JSON contract document by file name.
    // Expected input: Contract file name relative to the resolved contracts directory.
    // Expected output: Parsed JsonElement contract content.
    // Critical dependencies: _contractsDir, File.ReadAllText, and JsonSerializer.
    public JsonElement LoadContract(string contractFileName)
    {
        var contractPath = Path.Combine(_contractsDir, contractFileName);
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(contractPath));
    }

    // Purpose: Finds the first usable contracts directory across plugin-local and repo-local candidates.
    // Expected input: Current process base directory and current working directory.
    // Expected output: Absolute path to a directory containing the required harness contract files.
    // Critical dependencies: AnarchyPathCanon bundle helpers and the expected contract file set.
    private static string ResolveContractsDirectory()
    {
        var candidates = new[]
        {
            AnarchyPathCanon.ResolveBundleFilePath(Environment.CurrentDirectory, AnarchyPathCanon.BundleContractsDirectoryRelativePath),
            AnarchyPathCanon.ResolveBundleFilePath(AppContext.BaseDirectory, AnarchyPathCanon.BundleContractsDirectoryRelativePath),
            Path.Combine(AppContext.BaseDirectory, "..", AnarchyPathCanon.BundleContractsDirectoryRelativePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", AnarchyPathCanon.BundleContractsDirectoryRelativePath),
            Path.Combine(Environment.CurrentDirectory, "harness", "contracts")
        }
        .Select(Path.GetFullPath);

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "schema-reality.contract.json")) &&
                File.Exists(Path.Combine(candidate, "gov2gov-migration.contract.json")) &&
                File.Exists(Path.Combine(candidate, "active-work-state.contract.json")) &&
                File.Exists(Path.Combine(candidate, "preflight-session.contract.json")) &&
                File.Exists(Path.Combine(candidate, "harness-gap-state.contract.json")) &&
                File.Exists(Path.Combine(candidate, "verify-config-materialization.contract.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate harness contracts. Expected plugin-local contracts/ or repo-local harness/contracts/.");
    }
}

// Purpose: Represents the canonical schema bundle carried by the plugin and provides integrity lookup helpers.
// Expected input: Manifest and schema files discovered in plugin-local or repo-local bundle locations.
// Expected output: Availability, manifest metadata, and canonical file-hash lookup data.
// Critical dependencies: schema-bundle manifest JSON, HarnessInstallDiscovery, and local filesystem access.
internal sealed class CanonicalSchemaBundle
{
    public required bool IsAvailable { get; init; }
    public string? BundleName { get; init; }
    public string? BundleVersion { get; init; }
    public string? ManifestPath { get; init; }
    public string? SchemasDirectory { get; init; }
    public IReadOnlyDictionary<string, string> FileHashes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> CanonicalFileNames => FileHashes.Keys;

    // Purpose: Attempts to load the canonical schema bundle from the first valid candidate location.
    // Expected input: Current base directory, current working directory, installed plugin root, and repo-local plugin source candidates.
    // Expected output: A populated CanonicalSchemaBundle when a valid manifest is present, otherwise an unavailable bundle object.
    // Critical dependencies: schema-bundle manifest JSON, AnarchyPathCanon, and HarnessInstallDiscovery.TryResolveInstalledPluginRoot.
    public static CanonicalSchemaBundle TryLoad()
    {
        var candidateDirectories = new[]
        {
            AnarchyPathCanon.ResolveBundleFilePath(Environment.CurrentDirectory, AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            AnarchyPathCanon.ResolveBundleFilePath(AppContext.BaseDirectory, AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            Path.Combine(Environment.CurrentDirectory, AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath, AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            Path.Combine(Environment.CurrentDirectory, "..", "..", "..", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath, AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath, AnarchyPathCanon.BundleSchemasDirectoryRelativePath),
            AnarchyPathCanon.ResolveBundleFilePath(HarnessInstallDiscovery.TryResolveInstalledPluginRoot() ?? string.Empty, AnarchyPathCanon.BundleSchemasDirectoryRelativePath)
        }
        .Concat(FindAncestorSourceSchemaDirectories(Environment.CurrentDirectory))
        .Concat(FindAncestorSourceSchemaDirectories(AppContext.BaseDirectory))
        .Where(static path => !string.IsNullOrWhiteSpace(path))
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidateDirectory in candidateDirectories)
        {
            var manifestPath = Path.Combine(candidateDirectory, Path.GetFileName(AnarchyPathCanon.BundleSchemaManifestFileRelativePath));
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

    // Purpose: Finds repo-source plugin schema bundle candidates by walking ancestors from a runtime path.
    // Expected input: Current directory or runtime base directory.
    // Expected output: Candidate plugins/anarchy-ai/schemas directories beneath ancestor repo roots.
    // Critical dependencies: RepoSourcePluginDirectoryRelativePath and local filesystem path traversal.
    private static IEnumerable<string> FindAncestorSourceSchemaDirectories(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            yield break;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            yield return Path.Combine(
                current.FullName,
                AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath,
                AnarchyPathCanon.BundleSchemasDirectoryRelativePath);
            current = current.Parent;
        }
    }

    // Purpose: Resolves the on-disk file path for one canonical schema file in the loaded bundle.
    // Expected input: Canonical schema file name.
    // Expected output: True with an absolute file path when the bundle is available and the file exists.
    // Critical dependencies: SchemasDirectory and local filesystem access.
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

    // Purpose: Matches the schema-bundle manifest JSON structure carried by the plugin bundle.
    // Expected input: Deserialized schema-bundle manifest JSON.
    // Expected output: Manifest metadata and file-hash entries for the canonical schema bundle.
    // Critical dependencies: JsonPropertyName bindings and the schema-bundle manifest contract.
    private sealed class SchemaBundleManifest
    {
        [JsonPropertyName("bundle_name")]
        public string? BundleName { get; set; }

        [JsonPropertyName("bundle_version")]
        public string? BundleVersion { get; set; }

        [JsonPropertyName("files")]
        public List<SchemaBundleFile>? Files { get; set; }
    }

    // Purpose: Matches one file entry inside the schema-bundle manifest JSON.
    // Expected input: Deserialized manifest file-entry JSON.
    // Expected output: Canonical schema file name and expected SHA-256 hash.
    // Critical dependencies: JsonPropertyName bindings and the schema-bundle manifest contract.
    private sealed class SchemaBundleFile
    {
        [JsonPropertyName("file_name")]
        public required string FileName { get; set; }

        [JsonPropertyName("sha256")]
        public required string Sha256 { get; set; }
    }
}

// Purpose: Discovers the plugin root that the runtime should inspect when reporting install and capability state.
// Expected input: Current process base directory, current working directory, workspace marketplace state, and user-profile marketplace state.
// Expected output: Absolute plugin-root paths or null when discovery fails.
// Critical dependencies: marketplace.json, AnarchyPathCanon, and the expected plugin bundle shape.
internal static class HarnessInstallDiscovery
{
    // Purpose: Detects whether the current process is already running from an installed plugin root.
    // Expected input: Current working directory and base-directory ancestry.
    // Expected output: Absolute plugin root or null when no installed plugin root is detected.
    // Critical dependencies: LooksLikePluginRoot.
    public static string? TryResolveInstalledPluginRoot()
    {
        var candidates = new[]
        {
            Environment.CurrentDirectory,
            Path.Combine(AppContext.BaseDirectory, "..", ".."),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
        }
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (LooksLikePluginRoot(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    // Purpose: Resolves the plugin root that should govern a given workspace.
    // Expected input: Workspace root plus repo-local and user-profile marketplace state.
    // Expected output: Absolute plugin root selected from marketplace entries, repo-local bundle directories, or a fallback repo-local target.
    // Critical dependencies: TryResolveMarketplacePluginRoot, LooksLikePluginRoot, and AnarchyPathCanon.
    public static string ResolveWorkspacePluginRoot(string workspaceRoot)
    {
        var marketplaceCandidate = TryResolveMarketplacePluginRoot(workspaceRoot);
        if (!string.IsNullOrWhiteSpace(marketplaceCandidate))
        {
            return marketplaceCandidate;
        }

        var userProfileMarketplaceCandidate = TryResolveMarketplacePluginRoot(GetUserProfileRoot());
        if (!string.IsNullOrWhiteSpace(userProfileMarketplaceCandidate))
        {
            return userProfileMarketplaceCandidate;
        }

        var pluginsRoot = Path.Combine(workspaceRoot, "plugins");
        if (Directory.Exists(pluginsRoot))
        {
            foreach (var candidate in Directory.GetDirectories(pluginsRoot)
                         .Where(static path => AnarchyPathCanon.IsOwnedPluginName(Path.GetFileName(path)))
                         .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (LooksLikePluginRoot(candidate))
                {
                    return candidate;
                }
            }
        }

        return TryResolveInstalledPluginRoot()
            ?? AnarchyPathCanon.ResolveRepoLocalPluginRoot(workspaceRoot, GeneratedAnarchyPathCanon.DefaultPluginName);
    }

    // Purpose: Resolves a plugin root from a marketplace file located beneath the supplied root.
    // Expected input: Marketplace root such as a workspace root or user-profile root.
    // Expected output: Absolute plugin root referenced by a valid supported marketplace entry, or null when none qualify.
    // Critical dependencies: marketplace.json, TryGetPluginName, IsSupportedPluginSourcePath, and LooksLikePluginRoot.
    private static string? TryResolveMarketplacePluginRoot(string marketplaceRoot)
    {
        if (string.IsNullOrWhiteSpace(marketplaceRoot))
        {
            return null;
        }

        var marketplacePath = AnarchyPathCanon.ResolveRelativePath(marketplaceRoot, AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath);
        if (!File.Exists(marketplacePath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(marketplacePath));
            if (!document.RootElement.TryGetProperty("plugins", out var pluginsElement) ||
                pluginsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var pluginElement in pluginsElement.EnumerateArray())
            {
                if (!TryGetPluginName(pluginElement, out var pluginName) ||
                    !AnarchyPathCanon.IsOwnedPluginName(pluginName))
                {
                    continue;
                }

                if (!pluginElement.TryGetProperty("source", out var sourceElement) ||
                    !sourceElement.TryGetProperty("path", out var pathElement))
                {
                    continue;
                }

                var sourcePath = pathElement.GetString();
                if (string.IsNullOrWhiteSpace(sourcePath) ||
                    !IsSupportedPluginSourcePath(sourcePath))
                {
                    continue;
                }

                var resolved = Path.GetFullPath(Path.Combine(marketplaceRoot, sourcePath[2..].Replace('/', Path.DirectorySeparatorChar)));
                if (LooksLikePluginRoot(resolved))
                {
                    return resolved;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    // Purpose: Checks whether a marketplace source.path uses a supported Anarchy-AI source prefix.
    // Expected input: Marketplace source.path string.
    // Expected output: True when the source path matches the shared path canon.
    // Critical dependencies: AnarchyPathCanon.IsSupportedMarketplacePluginSourceRelativePath.
    internal static bool IsSupportedPluginSourcePath(string sourcePath)
    {
        return AnarchyPathCanon.IsSupportedMarketplacePluginSourceRelativePath(sourcePath);
    }

    // Purpose: Returns the current user-profile root used for home-local plugin discovery.
    // Expected input: None.
    // Expected output: Absolute user-profile path for the current process.
    // Critical dependencies: Environment.SpecialFolder.UserProfile.
    private static string GetUserProfileRoot()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    // Purpose: Reads a plugin name from one marketplace plugin JSON element.
    // Expected input: Marketplace plugin JsonElement.
    // Expected output: True with a nonblank plugin name when the element exposes a valid name property.
    // Critical dependencies: The marketplace JSON contract.
    public static bool TryGetPluginName(JsonElement pluginElement, out string pluginName)
    {
        pluginName = string.Empty;
        if (!pluginElement.TryGetProperty("name", out var nameElement) ||
            nameElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        pluginName = nameElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(pluginName);
    }

    // Purpose: Determines whether a directory looks like an Anarchy-AI plugin root.
    // Expected input: Candidate directory path.
    // Expected output: True when the directory exists and carries the required plugin manifest and .mcp.json files.
    // Critical dependencies: AnarchyPathCanon bundle file paths and local filesystem access.
    private static bool LooksLikePluginRoot(string path)
    {
        return Directory.Exists(path) &&
               File.Exists(AnarchyPathCanon.ResolveBundleFilePath(path, AnarchyPathCanon.BundlePluginManifestFileRelativePath)) &&
               File.Exists(AnarchyPathCanon.ResolveBundleFilePath(path, AnarchyPathCanon.BundleMcpFileRelativePath));
    }
}

// Purpose: Builds the runtime provenance and workspace-role envelope attached to harness claims.
// Expected input: Workspace root plus the plugin root selected by discovery when available.
// Expected output: Stable machine keys paired with human/agent-readable statements, evidence, and claim-scope effects.
// Critical dependencies: AnarchyPathCanon, plugin manifest JSON, schema-bundle manifest JSON, and workspace source-shape heuristics.
internal static class RuntimeEnvelopeBuilder
{
    public const string SchemaAuthoringAndPluginDeliveryWorkspace = "schema_authoring_and_plugin_delivery_workspace";
    public const string MaterialGovernanceConsumerWorkspace = "material_governance_consumer_workspace";
    public const string MixedOrUndeterminedWorkspaceRole = "mixed_or_undetermined_workspace_role";
    public const string RepoUnderlayPosture = "repo_underlay";
    public const string RepoLocalRuntimePosture = "repo_local_runtime";
    public const string UndeterminedWorkspacePosture = "undetermined";

    private const string UserProfileInstalledRuntime = "user_profile_installed_runtime";
    private const string RepoLocalInstalledRuntime = "repo_local_installed_runtime";
    private const string SourceCheckoutRuntime = "source_checkout_runtime";
    private const string ExternalOrUnknownRuntime = "external_or_unknown_runtime";
    private const string AutoWorkspacePosture = "auto";

    private static readonly string[] PortableSchemaFiles =
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

    private static readonly string[] Gov2GovStructureFiles =
    [
        "GOV2GOV-hello.md",
        "GOV2GOV-source-target-map.md",
        "GOV2GOV-registry.json",
        "GOV2GOV-rules.md",
        "GOV2GOV-pitfalls.md"
    ];

    public static IReadOnlyList<string> PortableSchemaFamily => PortableSchemaFiles;
    public static IReadOnlyList<string> GovernedAgentsStructure => GovernedAuthorityFiles.Prepend("AGENTS.md").ToArray();
    public static IReadOnlyList<string> Gov2GovStructure => Gov2GovStructureFiles;

    public static string ResolveWorkspacePosture(string workspaceRoot, string? requestedPosture = null)
    {
        var normalized = NormalizeWorkspacePosture(requestedPosture);
        if (normalized != AutoWorkspacePosture)
        {
            return normalized;
        }

        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        if (IsSchemaAuthoringAndPluginDeliveryWorkspace(resolvedWorkspaceRoot, []))
        {
            return UndeterminedWorkspacePosture;
        }

        var marketplacePath = AnarchyPathCanon.ResolveRepoLocalMarketplaceFilePath(resolvedWorkspaceRoot);
        var marketplaceExists = File.Exists(marketplacePath);
        var underlayGitignorePolicyPresent = HasRepoUnderlayGitignorePolicy(resolvedWorkspaceRoot);
        var repoLocalRuntimePresent = HasRepoLocalRuntimeBundle(resolvedWorkspaceRoot);
        var portableFamilyPresent = PortableSchemaFiles.All(fileName => File.Exists(Path.Combine(resolvedWorkspaceRoot, fileName)));
        var narrativeRegisterPresent = File.Exists(Path.Combine(
            resolvedWorkspaceRoot,
            ".agents",
            "anarchy-ai",
            "narratives",
            "register.json"));

        if (underlayGitignorePolicyPresent && !marketplaceExists)
        {
            return RepoUnderlayPosture;
        }

        if (marketplaceExists || (repoLocalRuntimePresent && !underlayGitignorePolicyPresent))
        {
            return RepoLocalRuntimePosture;
        }

        if (portableFamilyPresent && (narrativeRegisterPresent || !repoLocalRuntimePresent))
        {
            return RepoUnderlayPosture;
        }

        return UndeterminedWorkspacePosture;
    }

    public static WorkspaceRoleEnvelope BuildWorkspaceRole(string workspaceRoot)
    {
        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        var evidence = new List<string>();
        if (TryBuildDeclaredWorkspaceRole(resolvedWorkspaceRoot, out var declaredRole, out var declaredRoleFinding))
        {
            return declaredRole;
        }

        if (!string.IsNullOrWhiteSpace(declaredRoleFinding))
        {
            evidence.Add(declaredRoleFinding);
        }

        if (IsSchemaAuthoringAndPluginDeliveryWorkspace(resolvedWorkspaceRoot, evidence))
        {
            return new WorkspaceRoleEnvelope(
                MachineKey: SchemaAuthoringAndPluginDeliveryWorkspace,
                Statement: "This workspace deliberately combines schema/harness development with plugin delivery in one source repo. It authors the portable schema family, harness source, contracts, plugin bundle, and delivery docs. It is not expected to materialize the governed AGENTS runtime family for itself, so missing AGENTS-hello.md / AGENTS-Terms.md / AGENTS-Vision.md / AGENTS-Rules.md is not by itself a consumer-adoption failure.",
                Evidence: evidence.ToArray(),
                ClaimScopeEffect:
                [
                    "consumer_material_governance_check:not_applicable_unless_explicitly_requested",
                    "source_integrity_and_delivery_bundle_checks:applicable",
                    "runtime_install_observations:must_be_read_with_runtime_provenance"
                ],
                Confidence: "inferred_from_workspace");
        }

        evidence.Clear();
        if (IsMaterialGovernanceConsumerWorkspace(resolvedWorkspaceRoot, evidence))
        {
            return new WorkspaceRoleEnvelope(
                MachineKey: MaterialGovernanceConsumerWorkspace,
                Statement: "This workspace carries a material governed AGENTS family. Consumer material-governance checks are applicable because AGENTS.md and the governed AGENTS authority files are present as workspace-specific operating context.",
                Evidence: evidence.ToArray(),
                ClaimScopeEffect:
                [
                    "consumer_material_governance_check:applicable",
                    "workspace_specific_AGENTS_content:presence_only_not_hash_compared",
                    "source_integrity_checks:only_apply_to_portable_schema_files"
                ],
                Confidence: "inferred_from_workspace");
        }

        evidence.Clear();
        AddIfExists(evidence, resolvedWorkspaceRoot, "AGENTS.md", "startup_surface_present:AGENTS.md");
        foreach (var fileName in PortableSchemaFiles)
        {
            AddIfExists(evidence, resolvedWorkspaceRoot, fileName, $"portable_schema_file_present:{fileName}");
        }

        return new WorkspaceRoleEnvelope(
            MachineKey: MixedOrUndeterminedWorkspaceRole,
            Statement: "This workspace does not clearly resolve as either the schema/harness authoring and delivery workspace or a material governed AGENTS consumer workspace. Harness claims should stay qualified to the exact evidence observed.",
            Evidence: evidence.Count == 0 ? ["no_role_deciding_surfaces_detected"] : evidence.ToArray(),
            ClaimScopeEffect:
            [
                "consumer_material_governance_check:undetermined",
                "source_integrity_checks:apply_only_when_source_surfaces_are_observed",
                "migration_or_adoption_claims:require_explicit_inventory_evidence"
            ],
            Confidence: "inferred_from_workspace_low_confidence");
    }

    private static bool TryBuildDeclaredWorkspaceRole(
        string workspaceRoot,
        out WorkspaceRoleEnvelope workspaceRole,
        out string? finding)
    {
        workspaceRole = default!;
        finding = null;

        var rolePath = Path.Combine(workspaceRoot, ".anarchy-ai", "workspace-role.json");
        if (!File.Exists(rolePath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(rolePath));
            var machineKey = document.RootElement.TryGetProperty("machine_key", out var machineKeyElement)
                ? machineKeyElement.GetString()
                : null;
            var statement = document.RootElement.TryGetProperty("statement", out var statementElement)
                ? statementElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(machineKey) ||
                string.IsNullOrWhiteSpace(statement) ||
                !IsAllowedWorkspaceRoleMachineKey(machineKey))
            {
                finding = "declared_workspace_role_invalid_or_incomplete:.anarchy-ai/workspace-role.json";
                return false;
            }

            workspaceRole = new WorkspaceRoleEnvelope(
                MachineKey: machineKey,
                Statement: statement,
                Evidence: ReadOptionalStringArray(document.RootElement, "evidence")
                    .Append("declared_workspace_role_file:.anarchy-ai/workspace-role.json")
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                ClaimScopeEffect: ReadOptionalStringArray(document.RootElement, "claim_scope_effect"),
                Confidence: "declared_by_workspace");
            return true;
        }
        catch (JsonException)
        {
            finding = "declared_workspace_role_json_invalid:.anarchy-ai/workspace-role.json";
            return false;
        }
    }

    public static RuntimeProvenanceEnvelope BuildRuntimeProvenance(string workspaceRoot, string? pluginRoot = null)
    {
        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        var resolvedPluginRoot = string.IsNullOrWhiteSpace(pluginRoot)
            ? HarnessInstallDiscovery.ResolveWorkspacePluginRoot(resolvedWorkspaceRoot)
            : Path.GetFullPath(pluginRoot);

        var sourceDetected = IsSchemaAuthoringAndPluginDeliveryWorkspace(resolvedWorkspaceRoot, []);
        var sourcePluginRoot = AnarchyPathCanon.ResolveSourcePluginDirectory(resolvedWorkspaceRoot);
        var pluginManifestPath = AnarchyPathCanon.ResolveBundleFilePath(resolvedPluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        var schemaManifestPath = AnarchyPathCanon.ResolveBundleFilePath(resolvedPluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath);
        var runtimeExecutablePath = AnarchyPathCanon.ResolveBundleFilePath(resolvedPluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);

        var machineKey = DetermineRuntimeMachineKey(resolvedWorkspaceRoot, resolvedPluginRoot, sourcePluginRoot);
        var statement = machineKey switch
        {
            UserProfileInstalledRuntime => "This callable harness is running from the current user's installed plugin bundle. It is convenient and valid as an installed runtime, but it is not source truth for the workspace being evaluated.",
            RepoLocalInstalledRuntime => "This callable harness is running from a repo-local installed plugin bundle. Its runtime and bundle claims apply to that installed bundle; repo-authored source files remain source truth when they are present separately.",
            SourceCheckoutRuntime => "This callable harness appears to be running from the source checkout. Runtime claims are closest to repo source, but installed-bundle and marketplace claims still require explicit install evidence.",
            _ => "This callable harness could not resolve a first-class installed or source-checkout runtime lane. Treat runtime claims as qualified by the observed paths."
        };

        return new RuntimeProvenanceEnvelope(
            MachineKey: machineKey,
            Statement: statement,
            PluginRoot: resolvedPluginRoot,
            PluginManifestVersion: TryReadJsonString(pluginManifestPath, "version"),
            SchemaBundleVersion: TryReadJsonString(schemaManifestPath, "bundle_version"),
            SchemaBundleManifestPath: File.Exists(schemaManifestPath) ? schemaManifestPath : null,
            RuntimeExecutablePath: File.Exists(runtimeExecutablePath) ? runtimeExecutablePath : null,
            WorkspaceSourceDetected: sourceDetected,
            WorkspaceSourceAlignment: DetermineWorkspaceSourceAlignment(sourceDetected, resolvedPluginRoot, sourcePluginRoot, machineKey),
            ClaimScopeEffect: machineKey == UserProfileInstalledRuntime
                ? [
                    "runtime_and_install_observations_apply_to_installed_user_profile_bundle",
                    "installed_user_profile_bundle_is_not_workspace_source_truth",
                    "workspace_source_claims_require_source_alignment_evidence"
                ]
                : [
                    "runtime_and_install_observations_apply_to_discovered_plugin_root",
                    "workspace_source_claims_require_source_alignment_evidence"
                ],
            Confidence: File.Exists(pluginManifestPath) ? "direct_path_evidence" : "inferred_from_runtime_path");
    }

    public static string ConsumerMaterialGovernanceCheck(WorkspaceRoleEnvelope workspaceRole)
    {
        return workspaceRole.MachineKey switch
        {
            SchemaAuthoringAndPluginDeliveryWorkspace => "not_applicable",
            MaterialGovernanceConsumerWorkspace => "applicable",
            _ => "undetermined"
        };
    }

    private static bool IsSchemaAuthoringAndPluginDeliveryWorkspace(string workspaceRoot, List<string> evidence)
    {
        var portablePresent = PortableSchemaFiles.All(fileName => File.Exists(Path.Combine(workspaceRoot, fileName)));
        var pluginSourcePresent = Directory.Exists(AnarchyPathCanon.ResolveSourcePluginDirectory(workspaceRoot));
        var harnessSourcePresent = File.Exists(Path.Combine(workspaceRoot, "harness", "server", "dotnet", "Program.cs"));
        var runbookPresent = File.Exists(Path.Combine(workspaceRoot, "docs", "README_ai_links.md"));

        if (portablePresent) { evidence.Add("root_portable_schema_family_present"); }
        if (pluginSourcePresent) { evidence.Add("plugin_source_bundle_present:plugins/anarchy-ai"); }
        if (harnessSourcePresent) { evidence.Add("harness_source_present:harness/server/dotnet/Program.cs"); }
        if (runbookPresent) { evidence.Add("runbook_present:docs/README_ai_links.md"); }

        return portablePresent && pluginSourcePresent && harnessSourcePresent && runbookPresent;
    }

    private static bool IsMaterialGovernanceConsumerWorkspace(string workspaceRoot, List<string> evidence)
    {
        var agentsMdExists = File.Exists(Path.Combine(workspaceRoot, "AGENTS.md"));
        var governedFilesPresent = GovernedAuthorityFiles
            .Where(fileName => File.Exists(Path.Combine(workspaceRoot, fileName)))
            .ToArray();

        if (agentsMdExists) { evidence.Add("startup_surface_present:AGENTS.md"); }
        evidence.AddRange(governedFilesPresent.Select(static fileName => $"governed_authority_file_present:{fileName}"));
        return agentsMdExists && governedFilesPresent.Length == GovernedAuthorityFiles.Length;
    }

    private static string DetermineRuntimeMachineKey(string workspaceRoot, string pluginRoot, string sourcePluginRoot)
    {
        var userProfilePluginParent = AnarchyPathCanon.ResolveRelativePath(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            AnarchyPathCanon.UserProfilePluginParentDirectoryRelativePath);
        var workspacePluginParent = AnarchyPathCanon.ResolveRelativePath(
            workspaceRoot,
            AnarchyPathCanon.RepoLocalPluginParentDirectoryRelativePath);

        if (IsSameOrChildPath(pluginRoot, userProfilePluginParent))
        {
            return UserProfileInstalledRuntime;
        }

        if (IsSameOrChildPath(pluginRoot, sourcePluginRoot) ||
            IsSameOrChildPath(AppContext.BaseDirectory, workspaceRoot))
        {
            return SourceCheckoutRuntime;
        }

        if (IsSameOrChildPath(pluginRoot, workspacePluginParent))
        {
            return RepoLocalInstalledRuntime;
        }

        return ExternalOrUnknownRuntime;
    }

    private static string DetermineWorkspaceSourceAlignment(bool sourceDetected, string pluginRoot, string sourcePluginRoot, string runtimeMachineKey)
    {
        if (!sourceDetected)
        {
            return "not_applicable_no_workspace_source_detected";
        }

        if (IsSameOrChildPath(pluginRoot, sourcePluginRoot))
        {
            return "runtime_points_at_workspace_source";
        }

        return runtimeMachineKey == UserProfileInstalledRuntime
            ? "not_checked_installed_user_profile_runtime_is_not_source_truth"
            : "not_checked_runtime_not_proven_aligned_to_workspace_source";
    }

    private static void AddIfExists(List<string> evidence, string workspaceRoot, string relativePath, string evidenceValue)
    {
        if (File.Exists(Path.Combine(workspaceRoot, relativePath)))
        {
            evidence.Add(evidenceValue);
        }
    }

    private static string? TryReadJsonString(string path, string propertyName)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string[] ReadOptionalStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    private static bool IsAllowedWorkspaceRoleMachineKey(string machineKey)
    {
        return machineKey is SchemaAuthoringAndPluginDeliveryWorkspace
            or MaterialGovernanceConsumerWorkspace
            or MixedOrUndeterminedWorkspaceRole;
    }

    private static string NormalizeWorkspacePosture(string? requestedPosture)
    {
        if (string.IsNullOrWhiteSpace(requestedPosture))
        {
            return AutoWorkspacePosture;
        }

        return requestedPosture.Trim().ToLowerInvariant() switch
        {
            AutoWorkspacePosture => AutoWorkspacePosture,
            RepoUnderlayPosture => RepoUnderlayPosture,
            RepoLocalRuntimePosture => RepoLocalRuntimePosture,
            UndeterminedWorkspacePosture => UndeterminedWorkspacePosture,
            _ => throw new ArgumentException("workspace_posture must be auto, repo_underlay, repo_local_runtime, or undetermined.", nameof(requestedPosture))
        };
    }

    private static bool HasRepoUnderlayGitignorePolicy(string workspaceRoot)
    {
        var gitignorePath = Path.Combine(workspaceRoot, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return false;
        }

        var text = File.ReadAllText(gitignorePath);
        return text.Contains("Anarchy-AI runtime/install artifacts", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("/" + AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasRepoLocalRuntimeBundle(string workspaceRoot)
    {
        var pluginRoot = AnarchyPathCanon.ResolveSourcePluginDirectory(workspaceRoot);
        var runtimePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var manifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        return File.Exists(runtimePath) || File.Exists(manifestPath);
    }

    private static bool IsSameOrChildPath(string candidatePath, string parentPath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(parentPath))
        {
            return false;
        }

        var candidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(parent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record WorkspaceRoleEnvelope(
    [property: JsonPropertyName("machine_key")] string MachineKey,
    [property: JsonPropertyName("statement")] string Statement,
    [property: JsonPropertyName("evidence")] string[] Evidence,
    [property: JsonPropertyName("claim_scope_effect")] string[] ClaimScopeEffect,
    [property: JsonPropertyName("confidence")] string Confidence);

internal sealed record RuntimeProvenanceEnvelope(
    [property: JsonPropertyName("machine_key")] string MachineKey,
    [property: JsonPropertyName("statement")] string Statement,
    [property: JsonPropertyName("plugin_root")] string PluginRoot,
    [property: JsonPropertyName("plugin_manifest_version")] string? PluginManifestVersion,
    [property: JsonPropertyName("schema_bundle_version")] string? SchemaBundleVersion,
    [property: JsonPropertyName("schema_bundle_manifest_path")] string? SchemaBundleManifestPath,
    [property: JsonPropertyName("runtime_executable_path")] string? RuntimeExecutablePath,
    [property: JsonPropertyName("workspace_source_detected")] bool WorkspaceSourceDetected,
    [property: JsonPropertyName("workspace_source_alignment")] string WorkspaceSourceAlignment,
    [property: JsonPropertyName("claim_scope_effect")] string[] ClaimScopeEffect,
    [property: JsonPropertyName("confidence")] string Confidence);

// Purpose: Evaluates whether a workspace has materially real Anarchy schema surfaces or only partial/copied traces.
// Expected input: Workspace root, expected schema-package label, and optional startup-surface list.
// Expected output: An anonymous object describing schema reality, integrity, possession, reasons, repairs, and inspection facts.
// Critical dependencies: CanonicalSchemaBundle, AGENTS/startup files, and workspace filesystem state.
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

    // Purpose: Evaluates schema reality, integrity, and possession state for one workspace.
    // Expected input: Absolute workspace root, expected schema-package label, and optional startup-surface paths.
    // Expected output: An anonymous object with state classification, reasons, repairs, and inspection details.
    // Critical dependencies: CanonicalSchemaBundle, NormalizeStartupSurfaces, EvaluateIntegrity, and workspace file inspection.
    public object Evaluate(
        string workspaceRoot,
        string expectedSchemaPackage,
        string[]? startupSurfaces,
        string? workspacePosture = null)
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

        var workspacePostureState = RuntimeEnvelopeBuilder.ResolveWorkspacePosture(resolvedWorkspaceRoot, workspacePosture);
        var normalizedStartupSurfaces = NormalizeStartupSurfaces(resolvedWorkspaceRoot, startupSurfaces);
        var startupSurfaceExists = normalizedStartupSurfaces.ToDictionary(path => path, File.Exists, StringComparer.OrdinalIgnoreCase);
        var startupSurfacePostureResult = EvaluateStartupSurfacesForPosture(
            resolvedWorkspaceRoot,
            workspacePostureState,
            startupSurfaceExists);

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
        var startupSurfaceAlignedForPosture = startupSurfacePostureResult.AlignedForPosture;

        var state = ClassifyState(
            agentsMdExists,
            packageFilesPresentCount,
            governedFilesPresentCount,
            startupDiscoveryPathReal,
            packageSurfaceAligned,
            startupSurfaceAlignedForPosture);

        var integrityResult = EvaluateIntegrity(resolvedWorkspaceRoot);
        var activeReasons = BuildReasons(
            state,
            agentsMdExists,
            packageFilesPresentCount,
            governedFilesPresentCount,
            startupDiscoveryPathReal,
            packageSurfaceAligned,
            folderTopologyAligned,
            startupSurfaceAlignedForPosture);
        var workspaceRole = RuntimeEnvelopeBuilder.BuildWorkspaceRole(resolvedWorkspaceRoot);
        var runtimeProvenance = RuntimeEnvelopeBuilder.BuildRuntimeProvenance(resolvedWorkspaceRoot);
        var consumerMaterialGovernanceCheck = RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(workspaceRole);
        var safeRepairs = BuildSafeRepairs(
            state,
            startupSurfacePostureResult.EffectiveStartupSurfaceExists,
            integrityResult.IntegrityState,
            consumerMaterialGovernanceCheck);
        var recommendedNextAction = DetermineRecommendedNextAction(state, integrityResult.IntegrityState, consumerMaterialGovernanceCheck);

        return new
        {
            workspace_role = workspaceRole,
            runtime_provenance = runtimeProvenance,
            workspace_posture = workspacePostureState,
            consumer_material_governance_check = consumerMaterialGovernanceCheck,
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
            recommended_next_action = recommendedNextAction,
            safe_repairs = safeRepairs,
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
                startup_surfaces_ignored_by_posture = startupSurfacePostureResult.IgnoredMissingSurfaces,
                startup_discovery_path_real = startupDiscoveryPathReal,
                startup_surfaces_aligned = startupSurfaceAligned,
                startup_surfaces_aligned_for_posture = startupSurfaceAlignedForPosture,
                canonical_schema_bundle_available = _canonicalSchemaBundle.IsAvailable,
                canonical_schema_bundle_version = _canonicalSchemaBundle.BundleVersion,
                canonical_schema_manifest_path = _canonicalSchemaBundle.ManifestPath,
                canonical_schema_files_aligned = integrityResult.AlignedFiles,
                canonical_schema_files_diverged = integrityResult.DivergedFiles,
                canonical_schema_files_missing = integrityResult.MissingFiles
            }
        };
    }

    // Purpose: Classifies the high-level schema reality state from the observed workspace surfaces.
    // Expected input: Presence counts and alignment booleans derived from the workspace.
    // Expected output: One of real, partial, copied_only, or fully_missing.
    // Critical dependencies: FamilySchemaFiles and GovernedAuthorityFiles.
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

    // Purpose: Builds the active reason codes that explain the current schema reality state.
    // Expected input: Classified state and the underlying presence/alignment facts.
    // Expected output: Distinct reason codes that justify the classification.
    // Critical dependencies: The schema-reality reason vocabulary.
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

    // Purpose: Builds the safe repair suggestions appropriate for the current schema reality and integrity state.
    // Expected input: Schema reality state, startup-surface presence map, and integrity state.
    // Expected output: Distinct safe-repair actions for the caller.
    // Critical dependencies: MissingStartupSurfaceRepairs and the schema repair vocabulary.
    private static string[] BuildSafeRepairs(
        string state,
        IReadOnlyDictionary<string, bool> startupSurfaceExists,
        string integrityState,
        string consumerMaterialGovernanceCheck)
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

        if (consumerMaterialGovernanceCheck == "not_applicable")
        {
            repairs.RemoveAll(static repair => repair is
                "complete_missing_governed_surfaces" or
                "materialize_governed_AGENTS_family" or
                "refresh_package_surface" or
                "realign_startup_discovery_paths" or
                "update_stale_companion_artifacts" or
                "materialize_startup_surface" or
                "copy_schema_package");
        }

        return repairs.Distinct(StringComparer.Ordinal).ToArray();
    }

    // Purpose: Chooses the next safe action while respecting source/delivery workspace role boundaries.
    // Expected input: Schema reality, integrity, and the consumer material-governance applicability result.
    // Expected output: A bounded action string for the MCP caller.
    // Critical dependencies: RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck and migration action vocabulary.
    private static string DetermineRecommendedNextAction(
        string state,
        string integrityState,
        string consumerMaterialGovernanceCheck)
    {
        if (consumerMaterialGovernanceCheck == "not_applicable")
        {
            return integrityState == "diverged"
                ? "audit_canonical_divergence_before_continuing"
                : "none";
        }

        return state is "partial" or "copied_only" || integrityState == "diverged"
            ? "run_gov2gov_migration"
            : "none";
    }

    // Purpose: Compares workspace schema files against the canonical schema bundle hashes.
    // Expected input: Absolute workspace root.
    // Expected output: IntegrityResult describing aligned, diverged, and missing canonical schema files.
    // Critical dependencies: CanonicalSchemaBundle, ComputeSha256, and local filesystem access.
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

    // Purpose: Derives possession state from schema reality and integrity.
    // Expected input: Schema-reality state and integrity state.
    // Expected output: unknown, not_applicable, possessed, or unpossessed.
    // Critical dependencies: The current possession-state vocabulary.
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

    // Purpose: Computes a lowercase SHA-256 hash for one file.
    // Expected input: Absolute file path.
    // Expected output: Lowercase SHA-256 hash string.
    // Critical dependencies: SHA256 and readable file access.
    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Purpose: Builds startup-surface repair actions for any missing startup surfaces.
    // Expected input: Map of startup-surface paths to existence booleans.
    // Expected output: Repair action strings for each missing startup surface.
    // Critical dependencies: Path.GetFileName and the startup-surface repair vocabulary.
    private static string[] MissingStartupSurfaceRepairs(IReadOnlyDictionary<string, bool> startupSurfaceExists)
    {
        return startupSurfaceExists
            .Where(static pair => !pair.Value)
            .Select(static pair => $"create_startup_surface:{Path.GetFileName(pair.Key)}")
            .ToArray();
    }

    // Purpose: Normalizes startup-surface arguments into distinct absolute paths beneath the workspace when needed.
    // Expected input: Workspace root and optional startup-surface paths.
    // Expected output: Distinct absolute startup-surface paths.
    // Critical dependencies: Path.GetFullPath and workspace-relative path resolution.
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

    // Purpose: Applies workspace posture to optional startup-surface checks without hiding unrelated missing surfaces.
    // Expected input: Workspace root, resolved Anarchy posture, and absolute startup-surface existence facts.
    // Expected output: Effective alignment facts after posture-specific non-requirements are removed.
    // Critical dependencies: repo_underlay not requiring repo-local marketplace discovery.
    private static StartupSurfacePostureResult EvaluateStartupSurfacesForPosture(
        string workspaceRoot,
        string workspacePosture,
        IReadOnlyDictionary<string, bool> startupSurfaceExists)
    {
        if (startupSurfaceExists.Count == 0)
        {
            return new StartupSurfacePostureResult(true, startupSurfaceExists, []);
        }

        var ignoredMissingSurfaces = startupSurfaceExists
            .Where(pair => !pair.Value && ShouldIgnoreMissingStartupSurfaceForPosture(workspaceRoot, workspacePosture, pair.Key))
            .Select(static pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effectiveStartupSurfaceExists = startupSurfaceExists
            .Where(pair => !ignoredMissingSurfaces.Contains(pair.Key))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var alignedForPosture = effectiveStartupSurfaceExists.Count == 0 ||
            effectiveStartupSurfaceExists.Values.All(static value => value);

        return new StartupSurfacePostureResult(
            alignedForPosture,
            effectiveStartupSurfaceExists,
            ignoredMissingSurfaces.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static bool ShouldIgnoreMissingStartupSurfaceForPosture(
        string workspaceRoot,
        string workspacePosture,
        string startupSurfacePath)
    {
        if (workspacePosture != RuntimeEnvelopeBuilder.RepoUnderlayPosture)
        {
            return false;
        }

        var repoMarketplacePath = AnarchyPathCanon.ResolveRepoLocalMarketplaceFilePath(workspaceRoot);
        return string.Equals(
            Path.GetFullPath(startupSurfacePath),
            Path.GetFullPath(repoMarketplacePath),
            StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Carries the result of canonical schema integrity comparison.
    // Expected input: Integrity facts computed during EvaluateIntegrity.
    // Expected output: Immutable integrity-state record for higher-level schema reporting.
    // Critical dependencies: EvaluateIntegrity and the schema-integrity vocabulary.
    private sealed record IntegrityResult(
        string IntegrityState,
        string[] Findings,
        string[] AlignedFiles,
        string[] DivergedFiles,
        string[] MissingFiles);

    private sealed record StartupSurfacePostureResult(
        bool AlignedForPosture,
        IReadOnlyDictionary<string, bool> EffectiveStartupSurfaceExists,
        string[] IgnoredMissingSurfaces);
}

// Purpose: Plans or applies non-destructive schema reconciliation using the canonical schema bundle.
// Expected input: Workspace root, precomputed schema-reality context, optional startup surfaces, and migration mode.
// Expected output: An anonymous object describing planned actions, applied actions, audit pressure, and resulting state.
// Critical dependencies: SchemaRealityInspector, CanonicalSchemaBundle, and workspace filesystem access.
internal sealed class Gov2GovMigrationRunner(SchemaRealityInspector schemaRealityInspector)
{
    private readonly CanonicalSchemaBundle _canonicalSchemaBundle = CanonicalSchemaBundle.TryLoad();

    internal const string Gov2GovArtifactModeActive = "active";
    internal const string Gov2GovArtifactModeReference = "reference";
    internal const string Gov2GovArtifactModeAuto = "auto";

    private const string NarrativeRegisterRelativePath = ".agents/anarchy-ai/narratives/register.json";
    private const string NarrativeProjectsDirectoryRelativePath = ".agents/anarchy-ai/narratives/projects";

    // Purpose: Runs the gov2gov migration planner or non-destructive apply flow.
    // Expected input: Absolute workspace root, schema package label, schema-reality state, active reasons, optional startup surfaces, and migration mode.
    // Expected output: An anonymous object describing migration planning, applied work, audit needs, and resulting state.
    // Critical dependencies: SchemaRealityInspector.Evaluate, CanonicalSchemaBundle, and local file copy operations.
    public object Run(
        string workspaceRoot,
        string expectedSchemaPackage,
        string schemaRealityState,
        string[] activeReasons,
        string[]? startupSurfaces,
        string migrationMode,
        string? workspacePosture = null,
        string? gov2GovArtifactMode = null)
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

        var workspaceRole = RuntimeEnvelopeBuilder.BuildWorkspaceRole(resolvedWorkspaceRoot);
        var runtimeProvenance = RuntimeEnvelopeBuilder.BuildRuntimeProvenance(resolvedWorkspaceRoot);
        var workspacePostureState = RuntimeEnvelopeBuilder.ResolveWorkspacePosture(resolvedWorkspaceRoot, workspacePosture);
        var gov2GovArtifactModeState = ResolveGov2GovArtifactMode(resolvedWorkspaceRoot, gov2GovArtifactMode);
        var consumerMaterialGovernanceCheck = RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(workspaceRole);

        var preEvaluation = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(resolvedWorkspaceRoot, expectedSchemaPackage, startupSurfaces, workspacePostureState));
        var preIntegrityState = preEvaluation.GetProperty("integrity_state").GetString() ?? "unknown";
        var prePossessionState = preEvaluation.GetProperty("possession_state").GetString() ?? "unknown";
        var evaluatedPlanningReasons = preEvaluation
            .GetProperty("active_reasons")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
        var planningReasons = evaluatedPlanningReasons.Length == 0 ? activeReasons : evaluatedPlanningReasons;

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

        if (planningReasons.Contains("package_surface_out_of_sync", StringComparer.Ordinal) ||
            planningReasons.Contains("stale_companion_artifacts", StringComparer.Ordinal))
        {
            plannedActions.Add("refresh_package_surface");
        }

        if (planningReasons.Contains("copied_without_package_alignment", StringComparer.Ordinal) ||
            planningReasons.Contains("startup_discovery_path_weakened", StringComparer.Ordinal))
        {
            plannedActions.Add("realign_startup_discovery_paths");
        }

        if (planningReasons.Contains("folder_topology_partial", StringComparer.Ordinal) ||
            planningReasons.Contains("folder_topology_mismatch", StringComparer.Ordinal) ||
            planningReasons.Contains("schema_present_not_governing", StringComparer.Ordinal))
        {
            plannedActions.Add("materialize_missing_governed_surfaces");
            auditNeeded.Add("governed_surfaces_require_human_authorship");
        }

        if (prePossessionState == "possessed" || preIntegrityState == "diverged")
        {
            plannedActions.Add("audit_for_schema_tampering");
        }

        if (gov2GovArtifactModeState == Gov2GovArtifactModeActive)
        {
            ReconcileGov2GovSurfaces(
                resolvedWorkspaceRoot,
                consumerMaterialGovernanceCheck,
                migrationMode,
                plannedActions,
                actionsTaken,
                touchedSurfaces);
        }

        ReconcileNarrativeArcSurfaces(
            resolvedWorkspaceRoot,
            consumerMaterialGovernanceCheck,
            migrationMode,
            plannedActions,
            actionsTaken,
            touchedSurfaces,
            auditNeeded);

        var postEvaluation = JsonSerializer.SerializeToElement(
            schemaRealityInspector.Evaluate(resolvedWorkspaceRoot, expectedSchemaPackage, startupSurfaces, workspacePostureState));
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
        var postMigrationInventory = BuildPostMigrationInventory(resolvedWorkspaceRoot, plannedActions, actionsTaken);

        return new
        {
            workspace_role = workspaceRole,
            runtime_provenance = runtimeProvenance,
            workspace_posture = workspacePostureState,
            gov2gov_artifact_mode = gov2GovArtifactModeState,
            consumer_material_governance_check = consumerMaterialGovernanceCheck,
            migration_result_state = migrationResultState,
            planned_actions = plannedActions.Distinct(StringComparer.Ordinal).ToArray(),
            actions_taken = actionsTaken.Distinct(StringComparer.Ordinal).ToArray(),
            touched_surfaces = touchedSurfaces.Distinct(StringComparer.Ordinal).ToArray(),
            audit_needed = auditNeeded.Distinct(StringComparer.Ordinal).ToArray(),
            remaining_findings = remainingFindings.Distinct(StringComparer.Ordinal).ToArray(),
            resulting_schema_reality_state = postRealityState,
            resulting_integrity_state = postIntegrityState,
            resulting_possession_state = postPossessionState,
            post_migration_inventory = postMigrationInventory
        };
    }

    // Purpose: Resolves whether GOV2GOV root artifacts should be treated as an active packet or absent reference-mode docs.
    // Expected input: Workspace root plus optional explicit artifact mode.
    // Expected output: active or reference.
    // Critical dependencies: AGENTS-schema-gov2gov-migration active/reference mode semantics.
    private static string ResolveGov2GovArtifactMode(string workspaceRoot, string? requestedMode)
    {
        if (string.IsNullOrWhiteSpace(requestedMode) ||
            string.Equals(requestedMode, Gov2GovArtifactModeAuto, StringComparison.OrdinalIgnoreCase))
        {
            return RuntimeEnvelopeBuilder.Gov2GovStructure.Any(relativePath => File.Exists(Path.Combine(workspaceRoot, relativePath)))
                ? Gov2GovArtifactModeActive
                : Gov2GovArtifactModeReference;
        }

        return requestedMode.Trim().ToLowerInvariant() switch
        {
            Gov2GovArtifactModeActive => Gov2GovArtifactModeActive,
            Gov2GovArtifactModeReference => Gov2GovArtifactModeReference,
            _ => throw new ArgumentException("gov2gov_artifact_mode must be auto, active, or reference.", nameof(requestedMode))
        };
    }

    // Purpose: Plans or applies the GOV2GOV companion packet when the migration schema travels with a consumer workspace.
    // Expected input: Workspace root, consumer-role classification, migration mode, and current plan/action collections.
    // Expected output: Updated plan/action/touched collections; non-destructive apply creates only missing companion files.
    // Critical dependencies: RuntimeEnvelopeBuilder.Gov2GovStructure and AGENTS-schema-gov2gov-migration.json presence.
    private static void ReconcileGov2GovSurfaces(
        string workspaceRoot,
        string consumerMaterialGovernanceCheck,
        string migrationMode,
        List<string> plannedActions,
        List<string> actionsTaken,
        List<string> touchedSurfaces)
    {
        if (consumerMaterialGovernanceCheck == "not_applicable")
        {
            return;
        }

        var gov2GovSchemaPath = Path.Combine(workspaceRoot, "AGENTS-schema-gov2gov-migration.json");
        var gov2GovSchemaPresentOrPlanned = File.Exists(gov2GovSchemaPath) ||
            plannedActions.Contains("copy_missing_canonical_schema_file:AGENTS-schema-gov2gov-migration.json", StringComparer.Ordinal);
        if (!gov2GovSchemaPresentOrPlanned)
        {
            return;
        }

        foreach (var relativePath in RuntimeEnvelopeBuilder.Gov2GovStructure)
        {
            touchedSurfaces.Add(relativePath);
            var targetPath = Path.Combine(workspaceRoot, relativePath);
            if (File.Exists(targetPath))
            {
                continue;
            }

            plannedActions.Add($"materialize_gov2gov_surface:{relativePath}");
            if (migrationMode != "non_destructive_apply")
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? workspaceRoot);
            File.WriteAllText(targetPath, BuildGov2GovSurfaceTemplate(relativePath), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            actionsTaken.Add($"materialized_gov2gov_surface:{relativePath}");
        }
    }

    private static string BuildGov2GovSurfaceTemplate(string relativePath)
    {
        return relativePath switch
        {
            "GOV2GOV-hello.md" => """
                # GOV2GOV Hello

                This workspace carries an active governance-to-governance migration packet.

                Start here, then read `GOV2GOV-source-target-map.md`, `GOV2GOV-registry.json`, `GOV2GOV-rules.md`, and `GOV2GOV-pitfalls.md`.
                Keep migration claims evidence-backed and preserve existing workspace authority until the human approves replacement.
                """,
            "GOV2GOV-source-target-map.md" => """
                # GOV2GOV Source Target Map

                ## Source Governance
                - Current authority surfaces: inventory before changing.
                - Owner of source meaning: human workspace owner.

                ## Target Governance
                - Target schema: `AGENTS-schema-governance.json`
                - Migration schema: `AGENTS-schema-gov2gov-migration.json`

                ## Mapping Notes
                - Do not collapse source meaning into the target schema without an evidence-backed mapping.
                - Mark unresolved source authority explicitly before claiming migration completion.
                """,
            "GOV2GOV-registry.json" => """
                {
                  "schema-path": "AGENTS-schema-gov2gov-migration.json",
                  "registry-version": 1,
                  "migration-state": "active",
                  "entries": [],
                  "open-questions": []
                }
                """,
            "GOV2GOV-rules.md" => """
                # GOV2GOV Rules

                - Preserve source authority until replacement is explicit.
                - Treat copied schema files, generated files, runtime bundles, and marketplace metadata as different evidence classes.
                - Use non-destructive changes by default.
                - Record unresolved mappings instead of inventing intent.
                - Do not require repo-local plugin marketplace discovery when the repo posture is `repo_underlay`.
                """,
            "GOV2GOV-pitfalls.md" => """
                # GOV2GOV Pitfalls

                - Do not confuse underlay presence with runtime plugin availability.
                - Do not treat ignored files as disposable without inventory and rollback planning.
                - Do not let generated/runtime residue become committed source truth.
                - Do not claim schema alignment until canonical schema files are hash-aligned or divergence is audited.
                """,
            _ => string.Empty
        };
    }

    // Purpose: Builds a non-gating post-migration inventory for human/agent verification.
    // Expected input: Workspace root plus this run's plan/action lists.
    // Expected output: Portable schema hash evidence and governed-structure presence evidence.
    // Critical dependencies: CanonicalSchemaBundle, RuntimeEnvelopeBuilder file families, and ComputeSha256.
    private object BuildPostMigrationInventory(
        string workspaceRoot,
        IEnumerable<string> plannedActions,
        IEnumerable<string> actionsTaken)
    {
        var plannedActionSet = plannedActions.ToHashSet(StringComparer.Ordinal);
        var actionTakenSet = actionsTaken.ToHashSet(StringComparer.Ordinal);

        return new
        {
            inventory_state = "non_gating_evidence_only",
            comparison_basis = new[]
            {
                "current_canonical_schema_bundle_manifest",
                "this_run_planned_actions",
                "this_run_actions_taken"
            },
            portable_schema_family = RuntimeEnvelopeBuilder.PortableSchemaFamily
                .Select(fileName => BuildPortableSchemaInventoryEntry(workspaceRoot, fileName, plannedActionSet, actionTakenSet))
                .ToArray(),
            governed_agents_structure = RuntimeEnvelopeBuilder.GovernedAgentsStructure
                .Select(fileName => BuildPresenceOnlyInventoryEntry(workspaceRoot, fileName))
                .ToArray(),
            narrative_arc_structure = BuildNarrativeArcInventory(workspaceRoot, plannedActionSet, actionTakenSet),
            gov2gov_structure = RuntimeEnvelopeBuilder.Gov2GovStructure
                .Select(fileName => BuildPresenceOnlyInventoryEntry(workspaceRoot, fileName))
                .ToArray(),
            notes = new[]
            {
                "portable_schema_family_hashes_compare_against_the_current_canonical_schema_bundle_manifest",
                "delivery_plan_state_compares_each_portable_schema_file_against_this_run_planned_actions_and_actions_taken",
                "governed_agents_and_gov2gov_structure_are_presence_only_because_workspace_specific_content_is_expected_to_diverge",
                "narrative_arc_structure_is_presence_checked_because_AGENTS_schema_narrative_carries_record_and_register_templates",
                "inventory_does_not_fail_or_block_migration"
            }
        };
    }

    // Purpose: Plans or applies the minimal Anarchy narrative register surfaces carried by AGENTS-schema-narrative.
    // Expected input: Workspace root, consumer-role classification, migration mode, and current plan/action collections.
    // Expected output: Updated plan/action/audit collections; non-destructive apply creates only missing narrative surfaces.
    // Critical dependencies: Narrative register path convention and the installed plugin narrative template payload.
    private static void ReconcileNarrativeArcSurfaces(
        string workspaceRoot,
        string consumerMaterialGovernanceCheck,
        string migrationMode,
        List<string> plannedActions,
        List<string> actionsTaken,
        List<string> touchedSurfaces,
        List<string> auditNeeded)
    {
        if (consumerMaterialGovernanceCheck == "not_applicable")
        {
            return;
        }

        var narrativeSchemaPath = Path.Combine(workspaceRoot, "AGENTS-schema-narrative.json");
        var narrativeSchemaPresentOrPlanned = File.Exists(narrativeSchemaPath) ||
            plannedActions.Contains("copy_missing_canonical_schema_file:AGENTS-schema-narrative.json", StringComparer.Ordinal);
        if (!narrativeSchemaPresentOrPlanned)
        {
            return;
        }

        var registerPath = Path.Combine(workspaceRoot, NarrativeRegisterRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var projectsDirectoryPath = Path.Combine(workspaceRoot, NarrativeProjectsDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        touchedSurfaces.Add(NarrativeRegisterRelativePath);
        touchedSurfaces.Add(NarrativeProjectsDirectoryRelativePath);

        if (!File.Exists(registerPath))
        {
            plannedActions.Add($"seed_missing_narrative_register:{NarrativeRegisterRelativePath}");
            if (migrationMode == "non_destructive_apply")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(registerPath)!);
                File.WriteAllText(registerPath, LoadNarrativeRegisterTemplateJson(), Encoding.UTF8);
                actionsTaken.Add($"seeded_narrative_register:{NarrativeRegisterRelativePath}");
            }
        }
        else if (!TryReadJson(registerPath))
        {
            auditNeeded.Add($"narrative_register_invalid_json:{NarrativeRegisterRelativePath}");
        }

        if (!Directory.Exists(projectsDirectoryPath))
        {
            plannedActions.Add($"create_missing_narrative_projects_directory:{NarrativeProjectsDirectoryRelativePath}");
            if (migrationMode == "non_destructive_apply")
            {
                Directory.CreateDirectory(projectsDirectoryPath);
                actionsTaken.Add($"created_narrative_projects_directory:{NarrativeProjectsDirectoryRelativePath}");
            }
        }
    }

    // Purpose: Builds presence evidence for the narrative/arc artifact family carried by the narrative schema.
    // Expected input: Workspace root plus this run's plan/action sets.
    // Expected output: Register and projects-directory evidence with delivery plan state.
    // Critical dependencies: NarrativeRegisterRelativePath and gov2gov planned/action string vocabulary.
    private static object BuildNarrativeArcInventory(
        string workspaceRoot,
        IReadOnlySet<string> plannedActions,
        IReadOnlySet<string> actionsTaken)
    {
        var registerPath = Path.Combine(workspaceRoot, NarrativeRegisterRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var projectsDirectoryPath = Path.Combine(workspaceRoot, NarrativeProjectsDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var registerExists = File.Exists(registerPath);
        var registerValidJson = registerExists && TryReadJson(registerPath);

        return new
        {
            register = new
            {
                path = NarrativeRegisterRelativePath,
                presence_state = registerExists ? "present" : "missing",
                json_state = registerExists ? (registerValidJson ? "valid_json" : "invalid_json") : "not_applicable",
                template_source = AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath,
                delivery_plan_state = DetermineNarrativeRegisterDeliveryPlanState(registerExists, plannedActions, actionsTaken)
            },
            projects_directory = new
            {
                path = NarrativeProjectsDirectoryRelativePath,
                presence_state = Directory.Exists(projectsDirectoryPath) ? "present" : "missing",
                delivery_plan_state = DetermineNarrativeProjectsDeliveryPlanState(Directory.Exists(projectsDirectoryPath), plannedActions, actionsTaken)
            },
            hash_mode = "presence_only_workspace_specific_narrative_expected_to_diverge"
        };
    }

    // Purpose: Determines delivery-plan state for the narrative register.
    // Expected input: Final presence and this run's plan/action sets.
    // Expected output: A bounded delivery state string.
    // Critical dependencies: ReconcileNarrativeArcSurfaces action string vocabulary.
    private static string DetermineNarrativeRegisterDeliveryPlanState(
        bool exists,
        IReadOnlySet<string> plannedActions,
        IReadOnlySet<string> actionsTaken)
    {
        if (actionsTaken.Contains($"seeded_narrative_register:{NarrativeRegisterRelativePath}"))
        {
            return "delivered_this_run";
        }

        if (plannedActions.Contains($"seed_missing_narrative_register:{NarrativeRegisterRelativePath}"))
        {
            return "planned_to_deliver";
        }

        return exists ? "already_present_not_targeted" : "missing_not_in_delivery_plan";
    }

    // Purpose: Determines delivery-plan state for the narrative projects directory.
    // Expected input: Final presence and this run's plan/action sets.
    // Expected output: A bounded delivery state string.
    // Critical dependencies: ReconcileNarrativeArcSurfaces action string vocabulary.
    private static string DetermineNarrativeProjectsDeliveryPlanState(
        bool exists,
        IReadOnlySet<string> plannedActions,
        IReadOnlySet<string> actionsTaken)
    {
        if (actionsTaken.Contains($"created_narrative_projects_directory:{NarrativeProjectsDirectoryRelativePath}"))
        {
            return "delivered_this_run";
        }

        if (plannedActions.Contains($"create_missing_narrative_projects_directory:{NarrativeProjectsDirectoryRelativePath}"))
        {
            return "planned_to_deliver";
        }

        return exists ? "already_present_not_targeted" : "missing_not_in_delivery_plan";
    }

    // Purpose: Loads the carried narrative register template, falling back to the minimal schema template shape during source tests.
    // Expected input: Current process directory and app base directory from the launched runtime.
    // Expected output: JSON text for a missing narrative register.
    // Critical dependencies: The plugin bundle carrying templates/narratives/register.template.json.
    private static string LoadNarrativeRegisterTemplateJson()
    {
        foreach (var candidateRoot in CandidatePluginRoots())
        {
            var templatePath = AnarchyPathCanon.ResolveBundleFilePath(candidateRoot, AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath);
            if (File.Exists(templatePath))
            {
                return File.ReadAllText(templatePath);
            }
        }

        return "{\n  \"records\": []\n}\n";
    }

    // Purpose: Finds likely plugin roots for runtime template lookup across installed and source-test executions.
    // Expected input: Current directory and app base directory.
    // Expected output: Distinct candidate roots from most to least likely.
    // Critical dependencies: Installed .mcp cwd convention and runtime/win-x64 bundle layout.
    private static IEnumerable<string> CandidatePluginRoots()
    {
        var candidates = new List<string> { Environment.CurrentDirectory };
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && current is not null; i++)
        {
            candidates.Add(current.FullName);
            current = current.Parent;
        }

        return candidates
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    // Purpose: Checks whether a JSON file can be parsed without throwing into the migration flow.
    // Expected input: Absolute JSON path.
    // Expected output: True when the file parses as JSON; false otherwise.
    // Critical dependencies: JsonDocument and readable file access.
    private static bool TryReadJson(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    // Purpose: Builds one hash-aware portable schema inventory row.
    // Expected input: Workspace root, file name, and this run's delivery plan/action sets.
    // Expected output: Presence, hash, alignment, and plan-state fields for one portable schema file.
    // Critical dependencies: CanonicalSchemaBundle and ComputeSha256.
    private object BuildPortableSchemaInventoryEntry(
        string workspaceRoot,
        string fileName,
        IReadOnlySet<string> plannedActions,
        IReadOnlySet<string> actionsTaken)
    {
        var workspaceFilePath = Path.Combine(workspaceRoot, fileName);
        var exists = File.Exists(workspaceFilePath);
        var observedHash = exists ? ComputeSha256(workspaceFilePath) : null;
        var expectedHash = _canonicalSchemaBundle.FileHashes.TryGetValue(fileName, out var manifestHash)
            ? manifestHash
            : null;
        var hashAlignment = DetermineHashAlignment(exists, observedHash, expectedHash);

        return new
        {
            path = fileName,
            presence_state = exists ? "present" : "missing",
            hash_mode = "canonical_hash_checked",
            observed_sha256 = observedHash,
            expected_sha256 = expectedHash,
            hash_alignment = hashAlignment,
            delivery_plan_state = DetermineDeliveryPlanState(fileName, exists, plannedActions, actionsTaken)
        };
    }

    // Purpose: Builds one presence-only inventory row for workspace-specific governed surfaces.
    // Expected input: Workspace root and governed file path.
    // Expected output: Presence-only evidence with no hash comparison fields.
    // Critical dependencies: RuntimeEnvelopeBuilder governed file lists.
    private static object BuildPresenceOnlyInventoryEntry(string workspaceRoot, string fileName)
    {
        return new
        {
            path = fileName,
            presence_state = File.Exists(Path.Combine(workspaceRoot, fileName)) ? "present" : "missing",
            hash_mode = "presence_only_workspace_specific_divergence_expected"
        };
    }

    // Purpose: Determines portable schema hash alignment without treating missing files as a thrown error.
    // Expected input: File presence, observed hash, and canonical manifest hash.
    // Expected output: aligned, diverged, missing, or canonical_unavailable.
    // Critical dependencies: CanonicalSchemaBundle manifest availability.
    private static string DetermineHashAlignment(bool exists, string? observedHash, string? expectedHash)
    {
        if (!exists)
        {
            return "missing";
        }

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return "canonical_unavailable";
        }

        return string.Equals(observedHash, expectedHash, StringComparison.OrdinalIgnoreCase)
            ? "aligned"
            : "diverged";
    }

    // Purpose: Relates one portable schema file to the migration's own delivery plan for this run.
    // Expected input: File name, final presence, and this run's planned/applied actions.
    // Expected output: A bounded delivery-plan-state string.
    // Critical dependencies: Gov2gov planned/action string vocabulary.
    private static string DetermineDeliveryPlanState(
        string fileName,
        bool exists,
        IReadOnlySet<string> plannedActions,
        IReadOnlySet<string> actionsTaken)
    {
        if (actionsTaken.Contains($"copied_canonical_schema_file:{fileName}"))
        {
            return "delivered_this_run";
        }

        if (plannedActions.Contains($"copy_missing_canonical_schema_file:{fileName}"))
        {
            return "planned_to_deliver";
        }

        if (plannedActions.Contains($"audit_diverged_canonical_schema_file:{fileName}"))
        {
            return "planned_for_audit";
        }

        return exists ? "already_present_not_targeted" : "missing_not_in_delivery_plan";
    }

    // Purpose: Classifies the outcome of a gov2gov migration run.
    // Expected input: Migration mode, planned actions, applied actions, audit needs, and resulting schema/integrity state.
    // Expected output: A bounded migration-result-state string.
    // Critical dependencies: The migration result-state vocabulary.
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

    // Purpose: Computes a lowercase SHA-256 hash for one file during migration integrity checks.
    // Expected input: Absolute file path.
    // Expected output: Lowercase SHA-256 hash string.
    // Critical dependencies: SHA256 and readable file access.
    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

// Purpose: Compiles bounded active-work state from the workspace, objective, working set, and residue.
// Expected input: Workspace root, current objective, optional working set, blockers, residue, and preferred lane.
// Expected output: An anonymous object describing active lane, status, blockers, remaining steps, and measurement basis.
// Critical dependencies: workspace filesystem state, lane heuristics, and working-surface presence checks.
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

    // Purpose: Compiles a bounded active-work packet for the current workspace and objective.
    // Expected input: Absolute workspace root, current objective, optional working-set paths, blockers, residue, and preferred lane.
    // Expected output: An anonymous object with active lane, status, remaining steps, stop point, and evidence basis.
    // Critical dependencies: NormalizeArray, InferLane, DetermineCurrentStatus, and working-surface checks.
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

    // Purpose: Normalizes optional string arrays by trimming blanks, normalizing whitespace, and de-duplicating values.
    // Expected input: Optional string array.
    // Expected output: Distinct normalized values.
    // Critical dependencies: NormalizeWhitespace and ordinal distinctness.
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

    // Purpose: Collapses repeated whitespace in a value into single spaces.
    // Expected input: Raw string value.
    // Expected output: Whitespace-normalized string.
    // Critical dependencies: string.Split and current whitespace-normalization rules.
    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }

    // Purpose: Normalizes the preferred-lane input while preserving unsupported labels for caller visibility.
    // Expected input: Optional preferred-lane string.
    // Expected output: Null for blank input or a normalized lowercase lane string.
    // Critical dependencies: KnownLanes and NormalizeWhitespace.
    private static string? NormalizeLane(string? preferredLane)
    {
        if (string.IsNullOrWhiteSpace(preferredLane))
        {
            return null;
        }

        var normalized = NormalizeWhitespace(preferredLane).ToLowerInvariant();
        return KnownLanes.Contains(normalized, StringComparer.Ordinal) ? normalized : normalized;
    }

    // Purpose: Checks which declared working-set surfaces are actually present in the workspace.
    // Expected input: Workspace root and normalized working-set entries.
    // Expected output: Lists of present and missing path-like working surfaces.
    // Critical dependencies: IsPathLike and local filesystem existence checks.
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

    // Purpose: Heuristically decides whether a value looks like a file or directory reference.
    // Expected input: One working-set value.
    // Expected output: True when the value looks path-like.
    // Critical dependencies: Path helpers and the current working-set heuristics.
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

    // Purpose: Infers the most likely Anarchy lane from the objective, working set, residue, and optional preference.
    // Expected input: Normalized objective, working-set values, residue, and optional preferred lane.
    // Expected output: Active lane plus any tied candidate lanes.
    // Critical dependencies: Score and the current keyword heuristics for each lane.
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

    // Purpose: Scores how strongly a text corpus matches a set of lane keywords.
    // Expected input: Lowercased text corpus and keyword list.
    // Expected output: Integer keyword-hit count.
    // Critical dependencies: Current keyword heuristics and ordinal string matching.
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

    // Purpose: Classifies whether the current work is grounded, partial, or missing evidence.
    // Expected input: Working-surface presence counts, residue count, and AGENTS.md presence.
    // Expected output: grounded, partial, or none.
    // Critical dependencies: The current evidence-status vocabulary.
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

    // Purpose: Classifies the current active-work status.
    // Expected input: Objective text, blocker count, declared working-set count, residue count, and evidence status.
    // Expected output: blocked, needs_clarification, in_progress, or ready.
    // Critical dependencies: The current active-work status vocabulary.
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

    // Purpose: Builds degradation signals that explain why the current work packet may be weak or unstable.
    // Expected input: Objective, working-set counts, blocker count, residue count, missing-surface count, candidate-lane count, evidence status, and current status.
    // Expected output: Distinct degradation-signal codes.
    // Critical dependencies: IsNegationHeavy and the degradation-signal vocabulary.
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

    // Purpose: Detects whether the objective text is dominated by negation cues.
    // Expected input: Current objective text.
    // Expected output: True when at least two negation tokens are present.
    // Critical dependencies: The current negation-token list.
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

    // Purpose: Determines the next bounded action the agent should take from the active-work state.
    // Expected input: Active lane, current status, evidence status, and missing-working-surface count.
    // Expected output: A next-required-action code.
    // Critical dependencies: The active-work action vocabulary and lane semantics.
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

    // Purpose: Builds the ordered remaining-step list for the active-work packet.
    // Expected input: Active lane, next required action, and current status.
    // Expected output: Distinct ordered step identifiers.
    // Critical dependencies: The active-work step vocabulary and lane-specific step ordering.
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

    // Purpose: Builds a short stop-point string that tells a later session where to resume.
    // Expected input: Known blockers, recent residue, missing working surfaces, and next required action.
    // Expected output: A truncated stop-point string.
    // Critical dependencies: Truncate and the current stop-point precedence rules.
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

    // Purpose: Lists the evidence inputs that supported the active-work packet.
    // Expected input: Workspace root, working-set counts, blocker count, residue count, and optional preferred lane.
    // Expected output: Ordered measurement-basis identifiers.
    // Critical dependencies: AGENTS.md existence checks and the measurement-basis vocabulary.
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

    // Purpose: Keeps stop-point text within the bounded output limit.
    // Expected input: Candidate stop-point string.
    // Expected output: Original text when <=140 characters, otherwise a truncated ellipsis form.
    // Critical dependencies: The current 140-character stop-point limit.
    private static string Truncate(string value)
    {
        return value.Length <= 140 ? value : $"{value[..137]}...";
    }

    // Purpose: Carries the active lane and any tied candidate lanes inferred for active-work compilation.
    // Expected input: Lane inference results from InferLane.
    // Expected output: Immutable lane-inference record.
    // Critical dependencies: InferLane.
    private sealed record LaneInference(string ActiveLane, string[] CandidateLanes);
}

// Purpose: Evaluates long or ambiguous user directions and records a bounded clarification packet for testing.
// Expected input: Workspace root, free-form direction text, and optional selected choice.
// Expected output: An anonymous object describing whether clarification was triggered, what findings were detected, and where the local register entry was written.
// Critical dependencies: regex heuristics, local register writes under .agents/anarchy-ai, and current choice-option wording.
internal sealed class DirectionAssistRunner
{
    private const string ClarificationOption = "I need to ask clarification on a few things";
    private const string BestEffortOption = "Do your best with what I gave you";

    private static readonly string[] ChoiceOptions =
    [
        ClarificationOption,
        BestEffortOption
    ];

    private static readonly Regex WordRegex = new(@"[A-Za-z0-9']+", RegexOptions.Compiled);
    private static readonly Regex SentenceRegex = new(@"[^.!?]+", RegexOptions.Compiled);
    private static readonly Regex FragmentRegex = new(@"[^,;:]+", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private static readonly string[] NegationTokens =
    [
        "do not",
        "don't",
        "dont",
        "not",
        "no",
        "never",
        "avoid",
        "stop",
        "wrong"
    ];

    private static readonly string[] AmbiguousTokens =
    [
        "this",
        "that",
        "it",
        "things",
        "stuff",
        "something",
        "somehow",
        "whatever",
        "etc"
    ];

    private static readonly string[] FillerTokens =
    [
        "just",
        "maybe",
        "kind of",
        "sort of",
        "somehow",
        "whatever"
    ];

    // Purpose: Evaluates a direction string and optionally records a clarification-support packet.
    // Expected input: Absolute workspace root, free-form direction text, and optional selected choice.
    // Expected output: An anonymous object with trigger metrics, choice options, findings, cleaned direction, and register path.
    // Critical dependencies: CountWords, CountSentences, BuildFindings, BuildCleanedDirection, and local filesystem writes.
    public object Evaluate(string workspaceRoot, string directionText, string? selectedOption)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Path.IsPathRooted(workspaceRoot))
        {
            throw new ArgumentException("workspace_root must be an absolute path.", nameof(workspaceRoot));
        }

        if (string.IsNullOrWhiteSpace(directionText))
        {
            throw new ArgumentException("direction_text is required.", nameof(directionText));
        }

        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(resolvedWorkspaceRoot))
        {
            throw new DirectoryNotFoundException($"Workspace root not found: {resolvedWorkspaceRoot}");
        }

        var normalizedDirection = NormalizeWhitespace(directionText);
        var wordCount = CountWords(normalizedDirection);
        var sentenceCount = CountSentences(normalizedDirection);
        var assistTriggered = wordCount > 30 || sentenceCount > 2;

        var findings = BuildFindings(normalizedDirection, wordCount, sentenceCount, assistTriggered);
        var cleanedDirection = BuildCleanedDirection(normalizedDirection);

        if (assistTriggered && findings.Count == 0)
        {
            findings.Add(new DirectionFinding(
                "long_direction_needs_qualification",
                "Direction crossed threshold and should be qualified with explicit language."));
        }

        var normalizedSelectedOption = NormalizeSelectedOption(selectedOption);
        if (!string.IsNullOrWhiteSpace(selectedOption) && normalizedSelectedOption is null)
        {
            findings.Add(new DirectionFinding(
                "selected_option_unrecognized",
                "Provided selected_option did not match either supported choice."));
        }

        var registerPath = Path.Combine(resolvedWorkspaceRoot, ".agents", "anarchy-ai", "direction-assist-test.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(registerPath)!);

        var registerEntry = new
        {
            timestamp_utc = DateTime.UtcNow.ToString("O"),
            workspace_root = resolvedWorkspaceRoot,
            trigger_metrics = new
            {
                word_count = wordCount,
                sentence_count = sentenceCount,
                triggered = assistTriggered
            },
            linguistic_findings = findings.Select(static finding => new
            {
                code = finding.Code,
                detail = finding.Detail
            }).ToArray(),
            cleaned_direction = cleanedDirection,
            selected_option = normalizedSelectedOption ?? string.Empty
        };

        File.AppendAllText(
            registerPath,
            JsonSerializer.Serialize(registerEntry) + Environment.NewLine);

        return new
        {
            assist_triggered = assistTriggered,
            trigger_metrics = new
            {
                word_count = wordCount,
                sentence_count = sentenceCount,
                threshold = "word_count > 30 OR sentence_count > 2"
            },
            choice_options = ChoiceOptions,
            selected_option = normalizedSelectedOption ?? string.Empty,
            linguistic_findings = findings.Select(static finding => new
            {
                code = finding.Code,
                detail = finding.Detail
            }).ToArray(),
            cleaned_direction = cleanedDirection,
            register_path = registerPath
        };
    }

    // Purpose: Counts word-like tokens in a direction string.
    // Expected input: Normalized direction text.
    // Expected output: Integer word count.
    // Critical dependencies: WordRegex.
    private static int CountWords(string text)
    {
        return WordRegex.Matches(text).Count;
    }

    // Purpose: Counts sentence-like fragments in a direction string.
    // Expected input: Normalized direction text.
    // Expected output: Integer sentence count.
    // Critical dependencies: SentenceRegex and current sentence-trimming rules.
    private static int CountSentences(string text)
    {
        var matches = SentenceRegex.Matches(text)
            .Select(static match => match.Value.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return matches.Length;
    }

    // Purpose: Builds linguistic findings that explain why a direction may need clarification.
    // Expected input: Direction text, word count, sentence count, and whether assistance was triggered.
    // Expected output: List of direction findings.
    // Critical dependencies: token lists, regex helpers, and current finding thresholds.
    private static List<DirectionFinding> BuildFindings(string text, int wordCount, int sentenceCount, bool assistTriggered)
    {
        var findings = new List<DirectionFinding>();
        var lowered = $" {text.ToLowerInvariant()} ";

        var negationCount = NegationTokens.Count(token =>
            lowered.Contains($" {token} ", StringComparison.Ordinal));
        if (negationCount >= 2)
        {
            findings.Add(new DirectionFinding(
                "negation_heavy_direction",
                $"Detected {negationCount} negation-heavy tokens that reduce directional determinacy."));
        }

        var tokens = WordRegex.Matches(lowered)
            .Select(static match => match.Value)
            .ToArray();
        var ambiguousCount = tokens.Count(token => AmbiguousTokens.Contains(token, StringComparer.Ordinal));
        if (ambiguousCount >= 2)
        {
            findings.Add(new DirectionFinding(
                "ambiguous_references_detected",
                $"Detected {ambiguousCount} ambiguous reference tokens that can blur intent."));
        }

        var underdefinedFragments = FragmentRegex.Matches(text)
            .Select(static match => NormalizeWhitespace(match.Value))
            .Where(static fragment => CountWords(fragment) > 0 && CountWords(fragment) < 4)
            .Count();
        if (underdefinedFragments >= 2)
        {
            findings.Add(new DirectionFinding(
                "underdefined_fragments_detected",
                $"Detected {underdefinedFragments} short fragments that may be underdefined."));
        }

        if (assistTriggered && wordCount > 30 && sentenceCount > 2)
        {
            findings.Add(new DirectionFinding(
                "compound_length_pressure",
                "Direction is both long and multi-sentence, increasing interpretation variance."));
        }

        return findings;
    }

    // Purpose: Produces a cleaned best-effort restatement of the direction by stripping negation and filler tokens.
    // Expected input: Direction text.
    // Expected output: Cleaned direction string ending in a sentence terminator when possible.
    // Critical dependencies: SentenceRegex, ReplaceToken, NormalizeWhitespace, and current cleanup rules.
    private static string BuildCleanedDirection(string text)
    {
        var sentences = SentenceRegex.Matches(text)
            .Select(static match => NormalizeWhitespace(match.Value))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        var keptSentences = new List<string>();
        foreach (var sentence in sentences)
        {
            var cleaned = sentence;
            foreach (var token in NegationTokens)
            {
                cleaned = ReplaceToken(cleaned, token, string.Empty);
            }

            foreach (var token in FillerTokens)
            {
                cleaned = ReplaceToken(cleaned, token, string.Empty);
            }

            cleaned = NormalizeWhitespace(cleaned)
                .Trim(',', ';', ':', '-', '.', ' ');

            if (CountWords(cleaned) >= 3)
            {
                keptSentences.Add(cleaned);
            }
        }

        if (keptSentences.Count == 0)
        {
            var fallback = text;
            foreach (var token in NegationTokens)
            {
                fallback = ReplaceToken(fallback, token, string.Empty);
            }

            fallback = NormalizeWhitespace(fallback).Trim(',', ';', ':', '-', '.', ' ');
            return string.IsNullOrWhiteSpace(fallback) ? NormalizeWhitespace(text) : fallback;
        }

        return string.Join(". ", keptSentences)
            .Trim()
            .TrimEnd('.') + ".";
    }

    // Purpose: Removes one token or phrase from a string using whole-word matching.
    // Expected input: Source text, token to remove, and replacement text.
    // Expected output: Updated string with matching tokens replaced.
    // Critical dependencies: Regex whole-word replacement.
    private static string ReplaceToken(string text, string token, string replacement)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var pattern = $@"\b{Regex.Escape(token)}\b";
        return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
    }

    // Purpose: Collapses repeated whitespace in a direction string.
    // Expected input: Raw text value.
    // Expected output: Trimmed single-space-normalized string.
    // Critical dependencies: MultiWhitespaceRegex.
    private static string NormalizeWhitespace(string value)
    {
        return MultiWhitespaceRegex.Replace(value, " ").Trim();
    }

    // Purpose: Normalizes a selected choice into one of the supported direction-assist options.
    // Expected input: Optional selected choice text.
    // Expected output: The canonical clarification or best-effort option, or null when unrecognized.
    // Critical dependencies: NormalizeWhitespace and the fixed choice-option strings.
    private static string? NormalizeSelectedOption(string? selectedOption)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
        {
            return null;
        }

        var normalized = NormalizeWhitespace(selectedOption);
        return string.Equals(normalized, ClarificationOption, StringComparison.Ordinal)
            ? ClarificationOption
            : string.Equals(normalized, BestEffortOption, StringComparison.Ordinal)
                ? BestEffortOption
                : null;
    }

    // Purpose: Carries one linguistic finding reported by the direction-assist helper.
    // Expected input: Finding code and human-readable detail.
    // Expected output: Immutable finding record.
    // Critical dependencies: BuildFindings and the direction-assist output contract.
    private sealed record DirectionFinding(string Code, string Detail);
}

// Purpose: Probes the workspace and environment to distinguish declared configuration from observable configuration.
// Expected input: Workspace root, optional prose claim, and any combination of required_files, required_config_values, required_env_vars, required_executables, and required_processes observable arrays.
// Expected output: Materialization state, per-item matches, divergences, missing observables, echoed claim text, and required next-action/next-call routing.
// Critical dependencies: filesystem access, System.Text.Json, Environment, and System.Diagnostics.Process.
internal sealed class VerifyConfigMaterializationRunner
{
    private static readonly TimeSpan ExecutableProbeTimeout = TimeSpan.FromSeconds(5);

    // Purpose: Runs the verify_config_materialization gate against declared observables for the current workspace.
    // Expected input: Absolute workspace root and any subset of the required_* arrays as JsonElement values.
    // Expected output: An anonymous object implementing the verify_config_materialization contract.
    // Critical dependencies: CheckRequiredFile, CheckRequiredConfigValue, CheckRequiredEnvVar, CheckRequiredExecutable, and CheckRequiredProcess.
    public object Verify(
        string workspaceRoot,
        string? claimText,
        JsonElement? requiredFiles,
        JsonElement? requiredConfigValues,
        JsonElement? requiredEnvVars,
        JsonElement? requiredExecutables,
        JsonElement? requiredProcesses)
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

        var matches = new List<object>();
        var divergences = new List<object>();
        var missingObservables = new List<object>();
        var evidenceBasis = new HashSet<string>(StringComparer.Ordinal);
        var itemsChecked = 0;

        if (IsPopulatedArray(requiredFiles))
        {
            evidenceBasis.Add("filesystem_probe");
            foreach (var entry in requiredFiles!.Value.EnumerateArray())
            {
                itemsChecked++;
                CheckRequiredFile(entry, resolvedWorkspaceRoot, matches, divergences, missingObservables);
            }
        }

        if (IsPopulatedArray(requiredConfigValues))
        {
            foreach (var entry in requiredConfigValues!.Value.EnumerateArray())
            {
                itemsChecked++;
                CheckRequiredConfigValue(entry, resolvedWorkspaceRoot, matches, divergences, missingObservables, evidenceBasis);
            }
        }

        if (IsPopulatedArray(requiredEnvVars))
        {
            evidenceBasis.Add("env_snapshot");
            foreach (var entry in requiredEnvVars!.Value.EnumerateArray())
            {
                itemsChecked++;
                CheckRequiredEnvVar(entry, matches, divergences, missingObservables);
            }
        }

        if (IsPopulatedArray(requiredExecutables))
        {
            foreach (var entry in requiredExecutables!.Value.EnumerateArray())
            {
                itemsChecked++;
                CheckRequiredExecutable(entry, resolvedWorkspaceRoot, matches, divergences, missingObservables, evidenceBasis);
            }
        }

        if (IsPopulatedArray(requiredProcesses))
        {
            evidenceBasis.Add("process_enum");
            foreach (var entry in requiredProcesses!.Value.EnumerateArray())
            {
                itemsChecked++;
                CheckRequiredProcess(entry, matches, divergences, missingObservables);
            }
        }

        var normalizedClaimText = claimText ?? string.Empty;
        var matchCount = matches.Count;
        var divergenceCount = divergences.Count;
        var unobservableCount = missingObservables.Count;

        string materializationState;
        string recommendedNextAction;
        string recommendedNextCall;

        if (itemsChecked == 0)
        {
            if (!string.IsNullOrWhiteSpace(normalizedClaimText))
            {
                materializationState = "acknowledged_only";
                recommendedNextAction = "sharpen the prose claim into at least one observable (file, config value, env var, executable, or process) and re-run verify_config_materialization";
            }
            else
            {
                materializationState = "no_observable_anchor";
                recommendedNextAction = "no observables and no claim_text were provided; restate the claim with at least one observable before re-calling verify_config_materialization";
            }

            recommendedNextCall = "report_to_human";
        }
        else if (matchCount == itemsChecked)
        {
            materializationState = "materialized";
            recommendedNextAction = "continue; every declared observable matched the workspace and environment";
            recommendedNextCall = "continue";
        }
        else if (divergenceCount == itemsChecked)
        {
            materializationState = "divergent";
            recommendedNextAction = "resolve each divergence between the claim and observable state, then re-run verify_config_materialization";
            recommendedNextCall = "verify_config_materialization";
        }
        else if (unobservableCount == itemsChecked)
        {
            materializationState = "acknowledged_only";
            recommendedNextAction = "no declared item could be mechanically probed; supply observables the contract supports (files, json or dotenv keys, env vars, executables, processes) and re-run";
            recommendedNextCall = "report_to_human";
        }
        else
        {
            materializationState = "partially_materialized";
            recommendedNextAction = "close the remaining divergences and missing observables, then re-run verify_config_materialization";
            recommendedNextCall = "verify_config_materialization";
        }

        return new
        {
            materialization_state = materializationState,
            matches = matches.ToArray(),
            divergences = divergences.ToArray(),
            missing_observables = missingObservables.ToArray(),
            echoed_claim_text = normalizedClaimText,
            recommended_next_action = recommendedNextAction,
            recommended_next_call = recommendedNextCall,
            evidence_basis = evidenceBasis.OrderBy(static value => value, StringComparer.Ordinal).ToArray()
        };
    }

    // Purpose: Returns true when a nullable JsonElement represents a non-empty array.
    // Expected input: Optional JsonElement from a tool argument.
    // Expected output: True when the value is an array with at least one item, false otherwise.
    // Critical dependencies: JsonValueKind.Array.
    private static bool IsPopulatedArray(JsonElement? element)
    {
        if (!element.HasValue)
        {
            return false;
        }

        var value = element.Value;
        return value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0;
    }

    // Purpose: Evaluates a required_files entry against the workspace and records the verdict.
    // Expected input: JsonElement entry with path and optional expected_sha256, expected_contains, expected_absent.
    // Expected output: No direct return; appends to matches, divergences, or missing_observables.
    // Critical dependencies: ResolveRelativeOrAbsolute, File.Exists, File.ReadAllText, and SHA256.
    private static void CheckRequiredFile(
        JsonElement entry,
        string workspaceRoot,
        List<object> matches,
        List<object> divergences,
        List<object> missingObservables)
    {
        var path = ReadString(entry, "path");
        if (string.IsNullOrWhiteSpace(path))
        {
            missingObservables.Add(new
            {
                item_type = "required_file",
                reason = "path_field_missing_or_empty"
            });
            return;
        }

        var expectedSha256 = ReadString(entry, "expected_sha256");
        var expectedContains = ReadString(entry, "expected_contains");
        var expectedAbsent = ReadBoolean(entry, "expected_absent") ?? false;

        var resolvedPath = ResolveRelativeOrAbsolute(path, workspaceRoot);
        var exists = File.Exists(resolvedPath);

        if (expectedAbsent)
        {
            if (!exists)
            {
                matches.Add(new
                {
                    item_type = "required_file",
                    path,
                    expected_absent = true,
                    evidence = "absent as claimed"
                });
            }
            else
            {
                divergences.Add(new
                {
                    item_type = "required_file",
                    path,
                    expected_absent = true,
                    observed = "present",
                    evidence = $"file found at {resolvedPath}"
                });
            }

            return;
        }

        if (!exists)
        {
            divergences.Add(new
            {
                item_type = "required_file",
                path,
                expected_sha256 = expectedSha256,
                expected_contains = expectedContains,
                observed = "absent",
                evidence = $"no file at {resolvedPath}"
            });
            return;
        }

        var fileInfo = new FileInfo(resolvedPath);

        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            var actualHash = ComputeSha256Lower(resolvedPath);
            if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                divergences.Add(new
                {
                    item_type = "required_file",
                    path,
                    expected_sha256 = expectedSha256,
                    observed_sha256 = actualHash,
                    evidence = $"sha256 mismatch at {resolvedPath}"
                });
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(expectedContains))
        {
            string content;
            try
            {
                content = File.ReadAllText(resolvedPath);
            }
            catch (Exception ex)
            {
                missingObservables.Add(new
                {
                    item_type = "required_file",
                    path,
                    reason = $"file_unreadable:{ex.GetType().Name}"
                });
                return;
            }

            if (!content.Contains(expectedContains!, StringComparison.Ordinal))
            {
                divergences.Add(new
                {
                    item_type = "required_file",
                    path,
                    expected_contains = expectedContains,
                    observed = "substring_not_found",
                    evidence = $"expected substring not found in {resolvedPath}"
                });
                return;
            }
        }

        matches.Add(new
        {
            item_type = "required_file",
            path,
            expected_sha256 = expectedSha256,
            expected_contains = expectedContains,
            evidence = $"present, {fileInfo.Length} bytes"
        });
    }

    // Purpose: Evaluates a required_config_values entry by reading the source file and locating the declared key.
    // Expected input: JsonElement entry with source_path, key_path, expected_value, and optional format.
    // Expected output: No direct return; appends to matches, divergences, or missing_observables.
    // Critical dependencies: ResolveRelativeOrAbsolute, DetectConfigFormat, ReadJsonConfigValue, and ReadDotenvConfigValue.
    private static void CheckRequiredConfigValue(
        JsonElement entry,
        string workspaceRoot,
        List<object> matches,
        List<object> divergences,
        List<object> missingObservables,
        HashSet<string> evidenceBasis)
    {
        var sourcePath = ReadString(entry, "source_path");
        var keyPath = ReadString(entry, "key_path");
        var expectedValue = ReadString(entry, "expected_value");
        var explicitFormat = ReadString(entry, "format");

        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(keyPath))
        {
            missingObservables.Add(new
            {
                item_type = "required_config_value",
                source_path = sourcePath,
                key_path = keyPath,
                reason = "source_path_or_key_path_missing"
            });
            return;
        }

        var resolvedPath = ResolveRelativeOrAbsolute(sourcePath!, workspaceRoot);

        if (!File.Exists(resolvedPath))
        {
            missingObservables.Add(new
            {
                item_type = "required_config_value",
                source_path = sourcePath,
                key_path = keyPath,
                expected_value = expectedValue,
                reason = $"source_path_not_found:{resolvedPath}"
            });
            return;
        }

        var format = string.IsNullOrWhiteSpace(explicitFormat)
            ? DetectConfigFormat(resolvedPath)
            : explicitFormat!.ToLowerInvariant();

        string? observedValue;
        string evidence;

        switch (format)
        {
            case "json":
                evidenceBasis.Add("config_read:json");
                if (!TryReadJsonConfigValue(resolvedPath, keyPath!, out observedValue, out var jsonReason))
                {
                    missingObservables.Add(new
                    {
                        item_type = "required_config_value",
                        source_path = sourcePath,
                        key_path = keyPath,
                        expected_value = expectedValue,
                        reason = jsonReason
                    });
                    return;
                }

                evidence = $"json key {keyPath} in {resolvedPath}";
                break;

            case "env":
            case "dotenv":
                evidenceBasis.Add("config_read:dotenv");
                if (!TryReadDotenvConfigValue(resolvedPath, keyPath!, out observedValue, out var envReason, out var lineNumber))
                {
                    missingObservables.Add(new
                    {
                        item_type = "required_config_value",
                        source_path = sourcePath,
                        key_path = keyPath,
                        expected_value = expectedValue,
                        reason = envReason
                    });
                    return;
                }

                evidence = lineNumber > 0
                    ? $"line {lineNumber} of {sourcePath}"
                    : $"dotenv key {keyPath} in {sourcePath}";
                break;

            default:
                missingObservables.Add(new
                {
                    item_type = "required_config_value",
                    source_path = sourcePath,
                    key_path = keyPath,
                    expected_value = expectedValue,
                    reason = $"unsupported_config_format:{format}"
                });
                return;
        }

        if (string.IsNullOrWhiteSpace(expectedValue))
        {
            matches.Add(new
            {
                item_type = "required_config_value",
                source_path = sourcePath,
                key_path = keyPath,
                observed_value = observedValue,
                evidence = evidence + " (presence check)"
            });
            return;
        }

        if (string.Equals(observedValue, expectedValue, StringComparison.Ordinal))
        {
            matches.Add(new
            {
                item_type = "required_config_value",
                source_path = sourcePath,
                key_path = keyPath,
                expected_value = expectedValue,
                observed_value = observedValue,
                evidence
            });
        }
        else
        {
            divergences.Add(new
            {
                item_type = "required_config_value",
                source_path = sourcePath,
                key_path = keyPath,
                expected_value = expectedValue,
                observed_value = observedValue,
                evidence
            });
        }
    }

    // Purpose: Evaluates a required_env_vars entry against the current process environment.
    // Expected input: JsonElement entry with name and optional expected_value.
    // Expected output: No direct return; appends to matches, divergences, or missing_observables.
    // Critical dependencies: Environment.GetEnvironmentVariable.
    private static void CheckRequiredEnvVar(
        JsonElement entry,
        List<object> matches,
        List<object> divergences,
        List<object> missingObservables)
    {
        var name = ReadString(entry, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            missingObservables.Add(new
            {
                item_type = "required_env_var",
                reason = "name_field_missing_or_empty"
            });
            return;
        }

        var expectedValue = ReadString(entry, "expected_value");
        var observedValue = Environment.GetEnvironmentVariable(name!);

        if (observedValue is null)
        {
            divergences.Add(new
            {
                item_type = "required_env_var",
                name,
                expected_value = expectedValue,
                observed = "unset",
                evidence = "current process environment"
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedValue))
        {
            matches.Add(new
            {
                item_type = "required_env_var",
                name,
                evidence = "set (presence check)"
            });
            return;
        }

        if (string.Equals(observedValue, expectedValue, StringComparison.Ordinal))
        {
            matches.Add(new
            {
                item_type = "required_env_var",
                name,
                expected_value = expectedValue,
                observed_value = observedValue,
                evidence = "current process environment"
            });
        }
        else
        {
            divergences.Add(new
            {
                item_type = "required_env_var",
                name,
                expected_value = expectedValue,
                observed_value = observedValue,
                evidence = "current process environment"
            });
        }
    }

    // Purpose: Evaluates a required_executables entry by checking filesystem presence and optionally running a read-only version probe.
    // Expected input: JsonElement entry with path and optional expected_version_probe_args array.
    // Expected output: No direct return; appends to matches, divergences, or missing_observables.
    // Critical dependencies: ResolveRelativeOrAbsolute, File.Exists, System.Diagnostics.Process, and ExecutableProbeTimeout.
    private static void CheckRequiredExecutable(
        JsonElement entry,
        string workspaceRoot,
        List<object> matches,
        List<object> divergences,
        List<object> missingObservables,
        HashSet<string> evidenceBasis)
    {
        var path = ReadString(entry, "path");
        if (string.IsNullOrWhiteSpace(path))
        {
            missingObservables.Add(new
            {
                item_type = "required_executable",
                reason = "path_field_missing_or_empty"
            });
            return;
        }

        var resolvedPath = ResolveRelativeOrAbsolute(path!, workspaceRoot);

        if (!File.Exists(resolvedPath))
        {
            divergences.Add(new
            {
                item_type = "required_executable",
                path,
                observed = "absent",
                evidence = $"no file at {resolvedPath}"
            });
            return;
        }

        evidenceBasis.Add("filesystem_probe");

        var probeArgs = ReadStringArray(entry, "expected_version_probe_args");
        if (probeArgs is null || probeArgs.Length == 0)
        {
            matches.Add(new
            {
                item_type = "required_executable",
                path,
                evidence = $"present at {resolvedPath}"
            });
            return;
        }

        evidenceBasis.Add("version_probe");

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo(resolvedPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workspaceRoot
            };

            foreach (var arg in probeArgs)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                missingObservables.Add(new
                {
                    item_type = "required_executable",
                    path,
                    reason = "process_start_returned_null"
                });
                return;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var exited = process.WaitForExit((int)ExecutableProbeTimeout.TotalMilliseconds);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* swallow */ }
                missingObservables.Add(new
                {
                    item_type = "required_executable",
                    path,
                    reason = "probe_timeout_exceeded_5s"
                });
                return;
            }

            var stdout = stdoutTask.Result?.Trim() ?? string.Empty;
            matches.Add(new
            {
                item_type = "required_executable",
                path,
                expected_version_probe_args = probeArgs,
                exit_code = process.ExitCode,
                probe_stdout = stdout,
                evidence = $"present at {resolvedPath}; version probe stdout captured"
            });
        }
        catch (Exception ex)
        {
            missingObservables.Add(new
            {
                item_type = "required_executable",
                path,
                reason = $"probe_invocation_failed:{ex.GetType().Name}"
            });
        }
    }

    // Purpose: Evaluates a required_processes entry by enumerating running processes by name.
    // Expected input: JsonElement entry with process_name.
    // Expected output: No direct return; appends to matches, divergences, or missing_observables.
    // Critical dependencies: System.Diagnostics.Process.GetProcessesByName.
    private static void CheckRequiredProcess(
        JsonElement entry,
        List<object> matches,
        List<object> divergences,
        List<object> missingObservables)
    {
        var processName = ReadString(entry, "process_name");
        if (string.IsNullOrWhiteSpace(processName))
        {
            missingObservables.Add(new
            {
                item_type = "required_process",
                reason = "process_name_field_missing_or_empty"
            });
            return;
        }

        try
        {
            var found = System.Diagnostics.Process.GetProcessesByName(processName!);
            try
            {
                if (found.Length > 0)
                {
                    matches.Add(new
                    {
                        item_type = "required_process",
                        process_name = processName,
                        observed_count = found.Length,
                        evidence = "process_enum:name_match (user scoping best-effort)"
                    });
                }
                else
                {
                    divergences.Add(new
                    {
                        item_type = "required_process",
                        process_name = processName,
                        observed = "not_running",
                        evidence = "process_enum:no_name_match"
                    });
                }
            }
            finally
            {
                foreach (var p in found)
                {
                    try { p.Dispose(); } catch { /* swallow */ }
                }
            }
        }
        catch (Exception ex)
        {
            missingObservables.Add(new
            {
                item_type = "required_process",
                process_name = processName,
                reason = $"process_enum_failed:{ex.GetType().Name}"
            });
        }
    }

    // Purpose: Resolves a user-supplied path as absolute or workspace-relative.
    // Expected input: Path string and absolute workspace root.
    // Expected output: Absolute path string.
    // Critical dependencies: Path.IsPathRooted, Path.Combine, and Path.GetFullPath.
    private static string ResolveRelativeOrAbsolute(string path, string workspaceRoot)
    {
        var combined = Path.IsPathRooted(path)
            ? path
            : Path.Combine(workspaceRoot, path);
        return Path.GetFullPath(combined);
    }

    // Purpose: Detects a config format from a file path's extension.
    // Expected input: Absolute or relative file path.
    // Expected output: Lowercase format identifier ("json", "env", "yaml", "toml", "ini", or "unknown").
    // Critical dependencies: Path.GetExtension and Path.GetFileName.
    private static string DetectConfigFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".json" => "json",
            ".env" => "env",
            ".yaml" => "yaml",
            ".yml" => "yaml",
            ".toml" => "toml",
            ".ini" => "ini",
            _ when Path.GetFileName(path).StartsWith(".env", StringComparison.OrdinalIgnoreCase) => "env",
            _ => "unknown"
        };
    }

    // Purpose: Reads one dotted-key value out of a JSON file.
    // Expected input: Absolute path to a JSON file and a dotted key_path (e.g. "app.database.url").
    // Expected output: True when the key resolves to a scalar or serialisable value; false with a reason otherwise.
    // Critical dependencies: JsonDocument, JsonElement.TryGetProperty, and JsonValueKind.
    private static bool TryReadJsonConfigValue(string path, string keyPath, out string? observedValue, out string reason)
    {
        observedValue = null;
        reason = string.Empty;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            reason = $"json_parse_failed:{ex.GetType().Name}";
            return false;
        }

        using (document)
        {
            var segments = keyPath.Split('.');
            var current = document.RootElement;

            foreach (var segment in segments)
            {
                if (current.ValueKind != JsonValueKind.Object)
                {
                    reason = $"json_key_path_unresolvable_at:{segment}";
                    return false;
                }

                if (!current.TryGetProperty(segment, out var next))
                {
                    reason = $"json_key_not_found:{segment}";
                    return false;
                }

                current = next;
            }

            observedValue = current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => current.GetRawText()
            };

            return true;
        }
    }

    // Purpose: Reads one key from a dotenv-style file (KEY=VALUE with optional quoted values).
    // Expected input: Absolute path to the dotenv file and a key name.
    // Expected output: True when the key appears on a non-comment line; false with a reason otherwise.
    // Critical dependencies: File.ReadAllLines and simple line parsing.
    private static bool TryReadDotenvConfigValue(string path, string keyName, out string? observedValue, out string reason, out int lineNumber)
    {
        observedValue = null;
        reason = string.Empty;
        lineNumber = 0;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception ex)
        {
            reason = $"dotenv_read_failed:{ex.GetType().Name}";
            return false;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var parsedKey = trimmed.Substring(0, separatorIndex).Trim();
            if (!string.Equals(parsedKey, keyName, StringComparison.Ordinal))
            {
                continue;
            }

            var value = trimmed.Substring(separatorIndex + 1).Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            observedValue = value;
            lineNumber = i + 1;
            return true;
        }

        reason = $"dotenv_key_not_found:{keyName}";
        return false;
    }

    // Purpose: Computes the lowercase SHA-256 hex digest of a file.
    // Expected input: Absolute file path that exists.
    // Expected output: Lowercase hex SHA-256 string.
    // Critical dependencies: SHA256.HashData and FileStream.
    private static string ComputeSha256Lower(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        var sb = new System.Text.StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    // Purpose: Safely reads a string property from a JsonElement.
    // Expected input: JsonElement object and property name.
    // Expected output: The string value when present and string-typed, null otherwise.
    // Critical dependencies: JsonElement.TryGetProperty and JsonValueKind.
    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    // Purpose: Safely reads a boolean property from a JsonElement.
    // Expected input: JsonElement object and property name.
    // Expected output: True/False when present as a bool, null otherwise.
    // Critical dependencies: JsonElement.TryGetProperty and JsonValueKind.
    private static bool? ReadBoolean(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    // Purpose: Safely reads a string array property from a JsonElement.
    // Expected input: JsonElement object and property name.
    // Expected output: Array of strings when present as an array of strings/numbers, null otherwise.
    // Critical dependencies: JsonElement.TryGetProperty and JsonValueKind.
    private static string[]? ReadStringArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var list = new List<string>(value.GetArrayLength());
        foreach (var item in value.EnumerateArray())
        {
            var text = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Number => item.GetRawText(),
                _ => null
            };

            if (text is not null)
            {
                list.Add(text);
            }
        }

        return list.ToArray();
    }
}

// Purpose: Assesses install, runtime, schema, and adoption gaps for the current workspace and plugin discovery state.
// Expected input: Workspace root, optional host context, and optional expected capability list.
// Expected output: An anonymous object describing installation, runtime, schema, adoption, findings, repairs, and nested path facts.
// Critical dependencies: HarnessInstallDiscovery, SchemaRealityInspector, marketplace inspection, and bundle-surface inspection.
internal sealed class HarnessGapAssessor(SchemaRealityInspector schemaRealityInspector)
{
    private static readonly string[] ExpectedContractFiles =
    [
        "active-work-state.contract.json",
        "schema-reality.contract.json",
        "gov2gov-migration.contract.json",
        "preflight-session.contract.json",
        "harness-gap-state.contract.json",
        "verify-config-materialization.contract.json"
    ];

    private const string DirectionAssistCapability = "direction_assist_test";
    private const string DirectionAssistContractFile = "direction-assist-test.contract.json";

    // Purpose: Assesses the current workspace and discovered plugin root for install/runtime/adoption gaps.
    // Expected input: Absolute workspace root, optional host context, and optional expected capability list.
    // Expected output: An anonymous object with state classifications, missing components, safe repairs, inspection data, and nested path reports.
    // Critical dependencies: HarnessInstallDiscovery, SchemaRealityInspector, InspectMarketplace, and BuildAssessmentPaths.
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

        var pluginRoot = HarnessInstallDiscovery.ResolveWorkspacePluginRoot(resolvedWorkspaceRoot);
        var workspaceRole = RuntimeEnvelopeBuilder.BuildWorkspaceRole(resolvedWorkspaceRoot);
        var runtimeProvenance = RuntimeEnvelopeBuilder.BuildRuntimeProvenance(resolvedWorkspaceRoot, pluginRoot);
        var consumerMaterialGovernanceCheck = RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(workspaceRole);
        var pluginManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        var mcpDeclarationPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath);
        var runtimePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var skillPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath);
        var schemaManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath);

        var contractPresence = ExpectedContractFiles.ToDictionary(
            fileName => fileName,
            fileName => File.Exists(AnarchyPathCanon.ResolveBundleFilePath(
                pluginRoot,
                AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, fileName))),
            StringComparer.OrdinalIgnoreCase);
        var directionAssistContractPresent = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(
            pluginRoot,
            AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, DirectionAssistContractFile)));

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
        var artifactHygiene = AssessArtifactHygiene(resolvedWorkspaceRoot);

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
                missingComponents.Add("marketplace_missing");
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

        if (normalizedExpectedCapabilities.Contains(DirectionAssistCapability, StringComparer.Ordinal) &&
            !directionAssistContractPresent)
        {
            missingComponents.Add($"missing_contract:{DirectionAssistContractFile}");
        }

        var installationState = DetermineInstallationState(
            anyRepoBundleSurfacePresent,
            pluginManifestExists,
            mcpDeclarationExists,
            runtimeExists,
            normalizedHostContext,
            marketplaceInspection,
            pluginRoot,
            resolvedWorkspaceRoot);

        var runtimeState = DetermineRuntimeState(runtimeExists, anyRepoBundleSurfacePresent);
        var adoptionState = DetermineAdoptionState(
            installationState,
            schemaRealityState,
            integrityState,
            possessionState,
            consumerMaterialGovernanceCheck,
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

        if (schemaRecommendedAction == "run_gov2gov_migration" &&
            consumerMaterialGovernanceCheck != "not_applicable")
        {
            agentActions.Add("run_gov2gov_migration:plan_only");
        }

        agentActions.Add("run_preflight_session_before_complex_changes");
        agentActions.Add("treat_schema_state_as_input_not_completion");
        safeRepairs.AddRange(schemaSafeRepairs);
        safeRepairs.AddRange(artifactHygiene.SafeRepairs);
        var assessmentPaths = BuildAssessmentPaths(
            resolvedWorkspaceRoot,
            pluginRoot,
            marketplaceInspection,
            pluginManifestPath,
            mcpDeclarationPath,
            runtimePath,
            skillPath,
            schemaManifestPath);

        return new
        {
            workspace_role = workspaceRole,
            runtime_provenance = runtimeProvenance,
            consumer_material_governance_check = consumerMaterialGovernanceCheck,
            installation_state = installationState,
            runtime_state = runtimeState,
            schema_state = new
            {
                schema_reality_state = schemaRealityState,
                integrity_state = integrityState,
                possession_state = possessionState,
                active_reasons = schemaActiveReasons
            },
            artifact_hygiene = new
            {
                artifact_hygiene_state = artifactHygiene.State,
                observed_artifact_paths = artifactHygiene.ObservedArtifactPaths,
                recommended_artifact_lanes = artifactHygiene.RecommendedArtifactLanes,
                safe_repairs = artifactHygiene.SafeRepairs
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
                "artifact_hygiene_scan",
                "schema_reality_inspection",
                $"host_context:{normalizedHostContext}"
            },
            paths = assessmentPaths,
            inspection = new
            {
                host_context = normalizedHostContext,
                expected_capabilities = normalizedExpectedCapabilities,
                plugin_manifest_exists = pluginManifestExists,
                mcp_declaration_exists = mcpDeclarationExists,
                runtime_exists = runtimeExists,
                skill_exists = skillExists,
                schema_manifest_exists = schemaManifestExists,
                contract_presence = contractPresence,
                optional_contract_presence = new
                {
                    direction_assist_test = directionAssistContractPresent
                },
                marketplace_exists = marketplaceInspection.Exists,
                marketplace_entry_present = marketplaceInspection.HasAnarchyPluginEntry,
                marketplace_installed_by_default = marketplaceInspection.InstalledByDefault
            }
        };
    }

    // Purpose: Classifies installation state from bundle presence, marketplace registration, and plugin-root placement.
    // Expected input: Bundle-surface presence, host context, marketplace inspection, plugin root, and workspace root.
    // Expected output: repo_bootstrapped, user_profile_bootstrapped, repo_bundle_present_unregistered, or external_runtime_only.
    // Critical dependencies: IsUserProfilePluginRoot and the current installation-state vocabulary.
    private static string DetermineInstallationState(
        bool anyRepoBundleSurfacePresent,
        bool pluginManifestExists,
        bool mcpDeclarationExists,
        bool runtimeExists,
        string hostContext,
        MarketplaceInspection marketplaceInspection,
        string pluginRoot,
        string workspaceRoot)
    {
        var repoBundleReady = pluginManifestExists && mcpDeclarationExists && runtimeExists;
        var repoRegistrationSatisfied = hostContext is not ("codex" or "generic")
            || (marketplaceInspection.HasAnarchyPluginEntry && marketplaceInspection.InstalledByDefault);

        if (repoBundleReady && repoRegistrationSatisfied)
        {
            return IsUserProfilePluginRoot(pluginRoot, workspaceRoot)
                ? "user_profile_bootstrapped"
                : "repo_bootstrapped";
        }

        if (anyRepoBundleSurfacePresent)
        {
            return "repo_bundle_present_unregistered";
        }

        return "external_runtime_only";
    }

    private static readonly string[] GeneratedArtifactDirectoryNames =
    [
        "bin",
        "obj",
        ".tmp",
        ".cache",
        "TestResults",
        "artifacts"
    ];

    private static readonly string[] ArtifactScanPruneDirectoryNames =
    [
        ".git",
        ".hg",
        ".svn",
        ".vs",
        ".idea",
        ".vscode",
        "node_modules"
    ];

    // Purpose: Finds generated artifact directories that have landed in repo-local space and reports relocation guidance.
    // Expected input: Absolute workspace root.
    // Expected output: Artifact hygiene state, observed relative paths, recommended machine-local lanes, and safe repair suggestions.
    // Critical dependencies: Directory enumeration and the non-destructive artifact hygiene vocabulary.
    private static ArtifactHygieneAssessment AssessArtifactHygiene(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
        {
            return new ArtifactHygieneAssessment(
                "not_checked",
                [],
                GetRecommendedArtifactLanes(),
                ["artifact_hygiene_not_checked_workspace_root_unavailable"]);
        }

        var observedArtifactPaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directoryPath in EnumerateDirectoriesBounded(workspaceRoot, maxDepth: 5))
        {
            var directoryName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!GeneratedArtifactDirectoryNames.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsExpectedSourceOrPluginPayloadArtifact(workspaceRoot, directoryPath))
            {
                continue;
            }

            observedArtifactPaths.Add(ToRepoRelativePath(workspaceRoot, directoryPath));
        }

        if (observedArtifactPaths.Count == 0)
        {
            return new ArtifactHygieneAssessment(
                "clean",
                [],
                GetRecommendedArtifactLanes(),
                []);
        }

        return new ArtifactHygieneAssessment(
            "repo_local_artifacts_observed",
            observedArtifactPaths.ToArray(),
            GetRecommendedArtifactLanes(),
            [
                "relocate_generated_artifacts_to_machine_local_cache",
                "prefer_windows_localappdata_for_dotnet_build_restore_and_scratch",
                "prefer_linux_xdg_cache_home_for_build_restore_and_scratch",
                "inventory_and_quarantine_before_delete_never_delete_from_hygiene_assessment"
            ]);
    }

    // Purpose: Enumerates child directories while pruning known heavy dependency/control roots and keeping scan cost bounded.
    // Expected input: Root directory and maximum depth.
    // Expected output: Directory paths reachable within the bounded depth.
    // Critical dependencies: Directory.EnumerateDirectories and defensive exception handling for unreadable paths.
    private static IEnumerable<string> EnumerateDirectoriesBounded(string rootPath, int maxDepth)
    {
        var stack = new Stack<(string Path, int Depth)>();
        stack.Push((rootPath, 0));

        while (stack.Count > 0)
        {
            var (currentPath, depth) = stack.Pop();
            string[] childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(currentPath).ToArray();
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var childDirectory in childDirectories)
            {
                yield return childDirectory;

                var childName = Path.GetFileName(childDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (depth + 1 >= maxDepth ||
                    ArtifactScanPruneDirectoryNames.Contains(childName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                stack.Push((childDirectory, depth + 1));
            }
        }
    }

    // Purpose: Suppresses directories that are valid source/plugin payload surfaces rather than accidental generated output dumps.
    // Expected input: Workspace root and observed candidate artifact path.
    // Expected output: True when the candidate belongs to a deliberate source/payload lane.
    // Critical dependencies: Current source repo topology for plugin runtime payloads.
    private static bool IsExpectedSourceOrPluginPayloadArtifact(string workspaceRoot, string directoryPath)
    {
        var relativePath = ToRepoRelativePath(workspaceRoot, directoryPath);
        return relativePath.StartsWith("plugins/anarchy-ai/runtime/", StringComparison.OrdinalIgnoreCase) ||
               relativePath.StartsWith("plugins/anarchy-ai/assets/", StringComparison.OrdinalIgnoreCase) ||
               relativePath.StartsWith("branding/assets/", StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Converts an absolute child path into a stable repo-relative path with forward slashes.
    // Expected input: Workspace root and absolute child path.
    // Expected output: Forward-slash repo-relative path.
    // Critical dependencies: Path.GetRelativePath.
    private static string ToRepoRelativePath(string workspaceRoot, string path)
    {
        return Path.GetRelativePath(workspaceRoot, path)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    // Purpose: Centralizes the recommended OS-local artifact lanes reported by artifact hygiene assessment.
    // Expected input: None.
    // Expected output: Portable lane guidance for Windows, Linux, and macOS hosts.
    // Critical dependencies: Current artifact hygiene doctrine.
    private static string[] GetRecommendedArtifactLanes()
    {
        return
        [
            "%LOCALAPPDATA%\\Anarchy-AI\\<repo>\\",
            "${XDG_CACHE_HOME:-~/.cache}/anarchy-ai/<repo>/",
            "~/Library/Caches/Anarchy-AI/<repo>/"
        ];
    }

    // Purpose: Detects whether the discovered plugin root belongs to the user-profile lane instead of the workspace lane.
    // Expected input: Plugin root and workspace root.
    // Expected output: True when the plugin root is outside the workspace plugins directory.
    // Critical dependencies: AnarchyPathCanon repo-local plugin parent path.
    private static bool IsUserProfilePluginRoot(string pluginRoot, string workspaceRoot)
    {
        var normalizedPluginRoot = Path.GetFullPath(pluginRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedWorkspacePlugins = Path.GetFullPath(
            AnarchyPathCanon.ResolveRelativePath(workspaceRoot, AnarchyPathCanon.RepoLocalPluginParentDirectoryRelativePath))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !normalizedPluginRoot.StartsWith(normalizedWorkspacePlugins, StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Classifies runtime state from runtime presence and bundle-surface presence.
    // Expected input: Runtime existence and whether any repo bundle surface is present.
    // Expected output: callable_and_repo_bundled, repo_runtime_missing, or callable_external.
    // Critical dependencies: The current runtime-state vocabulary.
    private static string DetermineRuntimeState(bool runtimeExists, bool anyRepoBundleSurfacePresent)
    {
        if (runtimeExists)
        {
            return "callable_and_repo_bundled";
        }

        return anyRepoBundleSurfacePresent ? "repo_runtime_missing" : "callable_external";
    }

    // Purpose: Classifies adoption state from install/schema health, host context, and missing-component pressure.
    // Expected input: Installation state, schema reality, integrity, possession, host context, and missing components.
    // Expected output: fully_adopted, partially_adopted, adapter_gap_present, or bootstrap_required.
    // Critical dependencies: The current adoption-state vocabulary and host adapter policy.
    private static string DetermineAdoptionState(
        string installationState,
        string schemaRealityState,
        string integrityState,
        string possessionState,
        string consumerMaterialGovernanceCheck,
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

        var schemaHealthyForWorkspaceRole = consumerMaterialGovernanceCheck == "not_applicable"
            ? integrityState == "aligned"
            : schemaRealityState == "real" &&
              integrityState == "aligned" &&
              possessionState == "unpossessed";

        if (schemaHealthyForWorkspaceRole &&
            missingComponents.Count == 0 &&
            installationState is "repo_bootstrapped" or "user_profile_bootstrapped")
        {
            return "fully_adopted";
        }

        return "partially_adopted";
    }

    // Purpose: Normalizes host-context input for gap assessment.
    // Expected input: Optional host-context string.
    // Expected output: One of codex, claude, cursor, or generic, defaulting to codex when blank.
    // Critical dependencies: Current host-context vocabulary.
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

    // Purpose: Normalizes the expected-capabilities list for comparison against bundle surfaces.
    // Expected input: Optional capability list.
    // Expected output: Distinct capability names, or the default core capability set when none were supplied.
    // Critical dependencies: The current capability vocabulary.
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

    // Purpose: Inspects marketplace state for the current workspace, falling back to the user-profile marketplace when needed.
    // Expected input: Workspace root.
    // Expected output: MarketplaceInspection summarizing entry presence, installation policy, and root/path facts.
    // Critical dependencies: InspectMarketplaceAtRoot and current marketplace conventions.
    private static MarketplaceInspection InspectMarketplace(string workspaceRoot)
    {
        var repoInspection = InspectMarketplaceAtRoot(workspaceRoot);
        if (repoInspection.Exists || repoInspection.Findings.Length > 0)
        {
            return repoInspection;
        }

        return InspectMarketplaceAtRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    // Purpose: Inspects marketplace state rooted at one filesystem root.
    // Expected input: Marketplace root such as a workspace root or user-profile root.
    // Expected output: MarketplaceInspection summarizing entry presence, policy, root path, and plugin source path.
    // Critical dependencies: marketplace.json, HarnessInstallDiscovery.TryGetPluginName, and current source-path rules.
    private static MarketplaceInspection InspectMarketplaceAtRoot(string marketplaceRoot)
    {
        var marketplacePath = AnarchyPathCanon.ResolveRelativePath(marketplaceRoot, AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath);
        if (!File.Exists(marketplacePath))
        {
            return new MarketplaceInspection(marketplacePath, marketplaceRoot, false, false, false, []);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(marketplacePath));
            if (!document.RootElement.TryGetProperty("plugins", out var pluginsElement) ||
                pluginsElement.ValueKind != JsonValueKind.Array)
            {
                return new MarketplaceInspection(
                    marketplacePath,
                    marketplaceRoot,
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
                if (!HarnessInstallDiscovery.TryGetPluginName(pluginElement, out var pluginName) ||
                    !AnarchyPathCanon.IsOwnedPluginName(pluginName))
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
                    if (string.IsNullOrWhiteSpace(sourcePath) ||
                        !HarnessInstallDiscovery.IsSupportedPluginSourcePath(sourcePath))
                    {
                        findings.Add("anarchy_ai_marketplace_path_unexpected");
                    }
                }
            }

            return new MarketplaceInspection(marketplacePath, marketplaceRoot, true, hasEntry, installedByDefault, findings.ToArray());
        }
        catch (JsonException)
        {
            return new MarketplaceInspection(
                marketplacePath,
                marketplaceRoot,
                true,
                false,
                false,
                ["marketplace_json_invalid"]);
        }
    }

    // Purpose: Builds the nested origin/source/destination path report for gap assessment output.
    // Expected input: Workspace root, discovered plugin root, marketplace inspection, and bundle surface paths.
    // Expected output: PathRoleCollection aligned with the runtime health contract.
    // Critical dependencies: BuildAssessmentOriginRoleReport, BuildAssessmentSourceRoleReport, BuildAssessmentDestinationRoleReport, and AnarchyPathCanon.
    private static PathRoleCollection BuildAssessmentPaths(
        string workspaceRoot,
        string pluginRoot,
        MarketplaceInspection marketplaceInspection,
        string pluginManifestPath,
        string mcpDeclarationPath,
        string runtimePath,
        string skillPath,
        string schemaManifestPath)
    {
        var origin = BuildAssessmentOriginRoleReport(workspaceRoot);
        var source = BuildAssessmentSourceRoleReport(pluginRoot);
        var destination = BuildAssessmentDestinationRoleReport(
            workspaceRoot,
            pluginRoot,
            marketplaceInspection,
            pluginManifestPath,
            mcpDeclarationPath,
            runtimePath,
            skillPath,
            schemaManifestPath);

        return AnarchyPathCanon.CreateRoleCollection(origin: origin, source: source, destination: destination);
    }

    // Purpose: Builds the origin role for gap assessment, pointing back to repo-authored source surfaces when they are discoverable.
    // Expected input: Workspace root.
    // Expected output: PathRoleReport for repo-authored source surfaces, or null when the source repo cannot be located.
    // Critical dependencies: repo-root heuristics and AnarchyPathCanon source-path helpers.
    private static PathRoleReport? BuildAssessmentOriginRoleReport(string workspaceRoot)
    {
        var sourcePluginDirectoryPath = AnarchyPathCanon.ResolveSourcePluginDirectory(workspaceRoot);
        if (!Directory.Exists(sourcePluginDirectoryPath))
        {
            return null;
        }

        return AnarchyPathCanon.CreateRoleReport(
            rootPath: workspaceRoot,
            directories:
            [
                CreatePathEntry("plugin_source_directory_path", sourcePluginDirectoryPath)
            ],
            files:
            [
                CreatePathEntry("plugin_mcp_file_path", AnarchyPathCanon.ResolveRelativePath(workspaceRoot, AnarchyPathCanon.RepoSourcePluginMcpFileRelativePath)),
                CreatePathEntry("setup_executable_file_path", AnarchyPathCanon.ResolveRelativePath(workspaceRoot, AnarchyPathCanon.RepoSourceSetupExecutableFileRelativePath)),
                CreatePathEntry("plugin_readme_source_file_path", AnarchyPathCanon.ResolveRelativePath(workspaceRoot, AnarchyPathCanon.RepoSourceGeneratedPluginReadmeSourceRelativePath))
            ],
            relative:
            [
                CreatePathEntry("plugin_source_directory_relative_path", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath),
                CreatePathEntry("plugin_mcp_file_relative_path", AnarchyPathCanon.RepoSourcePluginMcpFileRelativePath),
                CreatePathEntry("setup_executable_file_relative_path", AnarchyPathCanon.RepoSourceSetupExecutableFileRelativePath),
                CreatePathEntry("plugin_readme_source_file_relative_path", AnarchyPathCanon.RepoSourceGeneratedPluginReadmeSourceRelativePath)
            ]);
    }

    // Purpose: Builds the source role for gap assessment from the discovered plugin bundle.
    // Expected input: Discovered plugin root.
    // Expected output: PathRoleReport describing bundle directories, files, and relative paths.
    // Critical dependencies: AnarchyPathCanon bundle helpers.
    private static PathRoleReport BuildAssessmentSourceRoleReport(string pluginRoot)
    {
        return AnarchyPathCanon.CreateRoleReport(
            rootPath: pluginRoot,
            directories:
            [
                CreatePathEntry("contracts_directory_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleContractsDirectoryRelativePath)),
                CreatePathEntry("runtime_directory_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeDirectoryRelativePath)),
                CreatePathEntry("schemas_directory_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSchemasDirectoryRelativePath)),
                CreatePathEntry("scripts_directory_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleScriptsDirectoryRelativePath)),
                CreatePathEntry("skill_directory_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSkillDirectoryRelativePath))
            ],
            files:
            [
                CreatePathEntry("plugin_manifest_file_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath)),
                CreatePathEntry("mcp_declaration_file_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath)),
                CreatePathEntry("runtime_executable_file_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath)),
                CreatePathEntry("schema_manifest_file_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath)),
                CreatePathEntry("skill_file_path", AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath))
            ],
            relative:
            [
                CreatePathEntry("plugin_manifest_file_relative_path", AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                CreatePathEntry("mcp_declaration_file_relative_path", AnarchyPathCanon.BundleMcpFileRelativePath),
                CreatePathEntry("runtime_executable_file_relative_path", AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath),
                CreatePathEntry("schema_manifest_file_relative_path", AnarchyPathCanon.BundleSchemaManifestFileRelativePath),
                CreatePathEntry("skill_file_relative_path", AnarchyPathCanon.BundleSkillFileRelativePath)
            ])!;
    }

    // Purpose: Builds the destination role for gap assessment using the workspace and marketplace targets under inspection.
    // Expected input: Workspace root, discovered plugin root, marketplace inspection, and bundle surface paths.
    // Expected output: PathRoleReport describing destination directories, files, and source references.
    // Critical dependencies: AnarchyPathCanon, marketplace inspection, and workspace-root reporting rules.
    private static PathRoleReport BuildAssessmentDestinationRoleReport(
        string workspaceRoot,
        string pluginRoot,
        MarketplaceInspection marketplaceInspection,
        string pluginManifestPath,
        string mcpDeclarationPath,
        string runtimePath,
        string skillPath,
        string schemaManifestPath)
    {
        return AnarchyPathCanon.CreateRoleReport(
            rootPath: workspaceRoot,
            directories:
            [
                CreatePathEntry("plugin_root_directory_path", pluginRoot),
                CreatePathEntry("workspace_plugins_directory_path", AnarchyPathCanon.ResolveRelativePath(workspaceRoot, AnarchyPathCanon.RepoLocalPluginParentDirectoryRelativePath)),
                CreatePathEntry("marketplace_root_directory_path", marketplaceInspection.RootPath)
            ],
            files:
            [
                CreatePathEntry("marketplace_file_path", marketplaceInspection.Path),
                CreatePathEntry("plugin_manifest_file_path", pluginManifestPath),
                CreatePathEntry("mcp_declaration_file_path", mcpDeclarationPath),
                CreatePathEntry("runtime_executable_file_path", runtimePath),
                CreatePathEntry("skill_file_path", skillPath),
                CreatePathEntry("schema_manifest_file_path", schemaManifestPath)
            ],
            relative:
            [
                CreatePathEntry("workspace_marketplace_file_relative_path", AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath),
                CreatePathEntry(
                    "marketplace_file_relative_path",
                    string.Equals(marketplaceInspection.RootPath, workspaceRoot, StringComparison.OrdinalIgnoreCase)
                        ? AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath
                        : AnarchyPathCanon.UserProfileMarketplaceFileRelativePath),
                CreatePathEntry("plugin_manifest_file_relative_path", AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                CreatePathEntry("mcp_declaration_file_relative_path", AnarchyPathCanon.BundleMcpFileRelativePath),
                CreatePathEntry("runtime_executable_file_relative_path", AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath),
                CreatePathEntry("schema_manifest_file_relative_path", AnarchyPathCanon.BundleSchemaManifestFileRelativePath),
                CreatePathEntry("skill_file_relative_path", AnarchyPathCanon.BundleSkillFileRelativePath)
            ])!;
    }

    // Purpose: Creates a keyed path entry for assessment path reporting.
    // Expected input: Path key and optional value.
    // Expected output: A key/value pair consumed by AnarchyPathCanon.CreateRoleReport.
    // Critical dependencies: The nested assessment-path key vocabulary.
    private static KeyValuePair<string, string?> CreatePathEntry(string key, string? value)
    {
        return new KeyValuePair<string, string?>(key, value);
    }

    // Purpose: Reads a string property from a JsonElement and falls back to an empty string when absent.
    // Expected input: JsonElement and property name.
    // Expected output: Property string value or empty string.
    // Critical dependencies: The current anonymous-object JSON shape used by assessment helpers.
    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var propertyElement) &&
               propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString() ?? string.Empty
            : string.Empty;
    }

    // Purpose: Reads a string-array property from a JsonElement and drops blanks.
    // Expected input: JsonElement and property name.
    // Expected output: Array of nonblank strings, or an empty array when absent.
    // Critical dependencies: The current anonymous-object JSON shape used by assessment helpers.
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

    // Purpose: Carries marketplace inspection facts for runtime gap assessment.
    // Expected input: Marketplace discovery and JSON inspection data.
    // Expected output: Immutable marketplace inspection record for assessment branching and path reporting.
    // Critical dependencies: InspectMarketplaceAtRoot and BuildAssessmentDestinationRoleReport.
    private sealed record MarketplaceInspection(
        string Path,
        string RootPath,
        bool Exists,
        bool HasAnarchyPluginEntry,
        bool InstalledByDefault,
        string[] Findings);

    // Purpose: Carries non-destructive repo-local generated artifact hygiene facts.
    // Expected input: Artifact hygiene scan output.
    // Expected output: Immutable assessment record for tool output and safe repair propagation.
    // Critical dependencies: AssessArtifactHygiene.
    private sealed record ArtifactHygieneAssessment(
        string State,
        string[] ObservedArtifactPaths,
        string[] RecommendedArtifactLanes,
        string[] SafeRepairs);
}

// Purpose: Runs preflight by combining active-work, harness-gap, and schema-reality assessment into one bounded readiness decision.
// Expected input: Workspace root, current objective, optional host context, startup surfaces, and user-intent hint.
// Expected output: An anonymous object describing preflight state, recommended path, active gaps, and required next action.
// Critical dependencies: HarnessGapAssessor, SchemaRealityInspector, ActiveWorkStateCompiler, and their shared JSON contracts.
internal sealed class PreflightSessionRunner(
    HarnessGapAssessor harnessGapAssessor,
    SchemaRealityInspector schemaRealityInspector,
    ActiveWorkStateCompiler activeWorkStateCompiler)
{
    // Purpose: Produces one bounded preflight decision for complex changes.
    // Expected input: Workspace root, current objective, optional host context, optional startup surfaces, and optional user intent.
    // Expected output: An anonymous object describing readiness, gaps, required action, and schema state.
    // Critical dependencies: ActiveWorkStateCompiler.Compile, HarnessGapAssessor.Assess, and SchemaRealityInspector.Evaluate.
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
        var resolvedWorkspaceRoot = Path.GetFullPath(workspaceRoot);
        var workspaceRole = RuntimeEnvelopeBuilder.BuildWorkspaceRole(resolvedWorkspaceRoot);
        var runtimeProvenance = RuntimeEnvelopeBuilder.BuildRuntimeProvenance(resolvedWorkspaceRoot);
        var consumerMaterialGovernanceCheck = RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck(workspaceRole);

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
        var schemaReasonsForActiveGaps = FilterSchemaReasonsForConsumerCheck(schemaReasons, consumerMaterialGovernanceCheck);
        var degradationSignals = GetStringArray(activeWorkElement, "session_degradation_signals");
        var measurementBasis = GetStringArray(activeWorkElement, "measurement_basis");

        var activeGaps = missingComponents
            .Concat(schemaReasonsForActiveGaps.Select(static value => $"schema:{value}"))
            .Concat(degradationSignals.Select(static value => $"session:{value}"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        string preflightState;
        string recommendedPath;

        var schemaMissingRequiresBootstrap = consumerMaterialGovernanceCheck != "not_applicable" &&
            schemaRealityState == "fully_missing";
        var schemaRequiresInspection = consumerMaterialGovernanceCheck == "not_applicable"
            ? integrityState != "aligned" || possessionState == "possessed"
            : schemaRealityState != "real" || integrityState != "aligned" || possessionState == "possessed" || adoptionState != "fully_adopted";

        var bootstrapNeeded = installationState is "bootstrap_needed" or "external_runtime_only" or "repo_bundle_present_unregistered"
            || schemaMissingRequiresBootstrap
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
        else if (schemaRequiresInspection)
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
            "inspect_schema_reality" => consumerMaterialGovernanceCheck == "not_applicable"
                ? "audit_canonical_divergence_before_continuing"
                : schemaRealityState is "partial" or "copied_only"
                ? "inspect_schema_reality_and_choose_safe_repair"
                : "audit_canonical_divergence_before_continuing",
            _ => nextRequiredAction
        };

        return new
        {
            workspace_role = workspaceRole,
            runtime_provenance = runtimeProvenance,
            consumer_material_governance_check = consumerMaterialGovernanceCheck,
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

    // Purpose: Removes consumer-materialization reasons from source/delivery workspace preflight gaps.
    // Expected input: Schema reason codes and consumer material-governance applicability.
    // Expected output: Reason codes that remain applicable to this workspace role.
    // Critical dependencies: RuntimeEnvelopeBuilder.ConsumerMaterialGovernanceCheck and schema reason vocabulary.
    private static string[] FilterSchemaReasonsForConsumerCheck(string[] schemaReasons, string consumerMaterialGovernanceCheck)
    {
        if (consumerMaterialGovernanceCheck != "not_applicable")
        {
            return schemaReasons;
        }

        return schemaReasons
            .Where(static reason => reason is not
                "schema_files_present_without_materialization" and not
                "copied_without_package_alignment" and not
                "folder_topology_mismatch" and not
                "schema_present_not_governing" and not
                "folder_topology_partial" and not
                "startup_discovery_path_weakened")
            .ToArray();
    }

    // Purpose: Reads a string property from a JsonElement and falls back to an empty string when absent.
    // Expected input: JsonElement and property name.
    // Expected output: Property string value or empty string.
    // Critical dependencies: The current anonymous-object JSON shape used by preflight helpers.
    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var propertyElement) &&
               propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString() ?? string.Empty
            : string.Empty;
    }

    // Purpose: Reads a string-array property from a JsonElement and drops blanks.
    // Expected input: JsonElement and property name.
    // Expected output: Array of nonblank strings, or an empty array when absent.
    // Critical dependencies: The current anonymous-object JSON shape used by preflight helpers.
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
// Purpose: Exposes the Anarchy-AI MCP tools backed by the runtime harness services.
// Expected input: Structured MCP tool arguments for preflight, schema reality, active-work compilation, gap assessment, gov2gov migration, and direction-assist testing.
// Expected output: Tool-specific anonymous objects serialized by the MCP server transport.
// Critical dependencies: the injected runner services, ModelContextProtocol attributes, and the current MCP tool contract.
internal sealed class AnarchyAiHarnessTools(
    SchemaRealityInspector schemaRealityInspector,
    Gov2GovMigrationRunner gov2GovMigrationRunner,
    ActiveWorkStateCompiler activeWorkStateCompiler,
    HarnessGapAssessor harnessGapAssessor,
    PreflightSessionRunner preflightSessionRunner,
    DirectionAssistRunner directionAssistRunner,
    VerifyConfigMaterializationRunner verifyConfigMaterializationRunner)
{
    // Purpose: Runs the preflight_session MCP tool.
    // Expected input: Absolute workspace root, current objective, and optional host/startup/user-intent context.
    // Expected output: Preflight readiness packet for the current session.
    // Critical dependencies: PreflightSessionRunner.
    [McpServerTool(
        Name = "preflight_session",
        Title = "Preflight Session",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Decide whether the current repo/session is ready for complex changes before the agent continues.")]
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

    // Purpose: Runs the is_schema_real_or_shadow_copied MCP tool.
    // Expected input: Absolute workspace root, expected schema-package label, and optional startup surfaces.
    // Expected output: Schema reality and integrity packet for the workspace.
    // Critical dependencies: SchemaRealityInspector.
    [McpServerTool(
        Name = "is_schema_real_or_shadow_copied",
        Title = "Is Schema Real Or Shadow Copied",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Determine whether a schema package is real, partial, copied_only, or fully_missing.")]
    public object IsSchemaRealOrShadowCopied(
        [Description("Absolute workspace root to inspect.")] string workspace_root,
        [Description("Expected schema family or package name.")] string expected_schema_package,
        [Description("Optional startup or package surfaces that should align with the schema package.")] string[]? startup_surfaces = null,
        [Description("Optional Anarchy workspace posture: auto, repo_underlay, repo_local_runtime, or undetermined.")] string? workspace_posture = null)
    {
        return schemaRealityInspector.Evaluate(workspace_root, expected_schema_package, startup_surfaces, workspace_posture);
    }

    // Purpose: Runs the compile_active_work_state MCP tool.
    // Expected input: Absolute workspace root, current objective, and optional working-set, blocker, residue, and lane data.
    // Expected output: Active-work state packet for the current objective.
    // Critical dependencies: ActiveWorkStateCompiler.
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

    // Purpose: Runs the assess_harness_gap_state MCP tool.
    // Expected input: Absolute workspace root and optional host/capability expectations.
    // Expected output: Harness gap assessment packet for install/runtime/adoption state.
    // Critical dependencies: HarnessGapAssessor.
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

    // Purpose: Runs the run_gov2gov_migration MCP tool.
    // Expected input: Absolute workspace root, schema package label, current schema state, active reasons, optional startup surfaces, and migration mode.
    // Expected output: Non-destructive gov2gov migration plan or apply result.
    // Critical dependencies: Gov2GovMigrationRunner.
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
        [Description("Plan changes only, or apply only non-destructive reconciliation steps.")] string migration_mode = "plan_only",
        [Description("Optional Anarchy workspace posture: auto, repo_underlay, repo_local_runtime, or undetermined.")] string? workspace_posture = null,
        [Description("Optional GOV2GOV artifact mode: auto, active, or reference. Auto treats existing GOV2GOV-* files as active and absent GOV2GOV-* files as reference mode.")] string? gov2gov_artifact_mode = null)
    {
        return gov2GovMigrationRunner.Run(
            workspace_root,
            expected_schema_package,
            schema_reality_state,
            active_reasons,
            startup_surfaces,
            migration_mode,
            workspace_posture,
            gov2gov_artifact_mode);
    }

    // Purpose: Runs the experimental direction_assist_test MCP tool.
    // Expected input: Absolute workspace root, direction text, and optional selected choice.
    // Expected output: Direction-assist findings, cleaned direction, and register metadata.
    // Critical dependencies: DirectionAssistRunner.
    [McpServerTool(
        Name = "direction_assist_test",
        Title = "Direction Assist Test",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        UseStructuredContent = true)]
    [Description("Experimental test tool that qualifies long direction text, returns explicit findings plus cleaned direction, and appends a local test register entry.")]
    public object DirectionAssistTest(
        [Description("Absolute workspace root used for test register output.")] string workspace_root,
        [Description("Direction text to evaluate against the qualification threshold.")] string direction_text,
        [Description("Optional selected choice string after the two options are presented.")] string? selected_option = null)
    {
        return directionAssistRunner.Evaluate(workspace_root, direction_text, selected_option);
    }

    // Purpose: Runs the verify_config_materialization MCP tool.
    // Expected input: Absolute workspace root, optional claim text, and any subset of the required_* observable arrays.
    // Expected output: Materialization verdict, per-item matches and divergences, missing observables, echoed claim text, and next-action/next-call routing.
    // Critical dependencies: VerifyConfigMaterializationRunner.
    [McpServerTool(
        Name = "verify_config_materialization",
        Title = "Verify Config Materialization",
        ReadOnly = true,
        UseStructuredContent = true)]
    [Description("Mechanical gate that distinguishes declared configuration from observable configuration by probing files, config values, env vars, executables, and processes.")]
    public object VerifyConfigMaterialization(
        [Description("Absolute workspace root the claim is checked against.")] string workspace_root,
        [Description("Optional plain-language restatement of what the agent or user claimed; echoed in the result.")] string? claim_text = null,
        [Description("Optional array of required_files entries (path, expected_sha256, expected_contains, expected_absent).")] JsonElement? required_files = null,
        [Description("Optional array of required_config_values entries (source_path, key_path, expected_value, format).")] JsonElement? required_config_values = null,
        [Description("Optional array of required_env_vars entries (name, expected_value).")] JsonElement? required_env_vars = null,
        [Description("Optional array of required_executables entries (path, expected_version_probe_args).")] JsonElement? required_executables = null,
        [Description("Optional array of required_processes entries (process_name).")] JsonElement? required_processes = null)
    {
        return verifyConfigMaterializationRunner.Verify(
            workspace_root,
            claim_text,
            required_files,
            required_config_values,
            required_env_vars,
            required_executables,
            required_processes);
    }
}

// Purpose: Boots the hosted MCP server and registers the Anarchy-AI tool surface over stdio.
// Expected input: Process arguments passed to the runtime server.
// Expected output: A running MCP host process until shutdown.
// Critical dependencies: Microsoft.Extensions.Hosting, MCP stdio transport, and dependency-injection wiring for the harness services.
internal static class Program
{
    // Purpose: Builds and runs the hosted MCP server process.
    // Expected input: Process arguments for host configuration.
    // Expected output: No direct return value; starts the MCP stdio server and blocks until shutdown.
    // Critical dependencies: Host.CreateApplicationBuilder, dependency injection, and WithTools<AnarchyAiHarnessTools>.
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<ContractLoader>();
        builder.Services.AddSingleton<SchemaRealityInspector>();
        builder.Services.AddSingleton<Gov2GovMigrationRunner>();
        builder.Services.AddSingleton<ActiveWorkStateCompiler>();
        builder.Services.AddSingleton<DirectionAssistRunner>();
        builder.Services.AddSingleton<HarnessGapAssessor>();
        builder.Services.AddSingleton<PreflightSessionRunner>();
        builder.Services.AddSingleton<VerifyConfigMaterializationRunner>();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AnarchyAiHarnessTools>();

        await builder.Build().RunAsync();
    }
}
