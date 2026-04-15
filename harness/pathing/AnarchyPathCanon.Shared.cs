using System.Text.Json.Serialization;

namespace AnarchyAi.Pathing;

/// <summary>
/// Carries the normalized path facts for a single origin, source, or destination role in setup and health JSON.
/// </summary>
/// <remarks>
/// Purpose: expose a bounded path report without overloading flat path keys.
/// Expected input: optional root path plus keyed directory, file, and relative-path members from the caller.
/// Expected output: a JSON-serializable role object whose empty members are omitted.
/// Critical dependencies: <see cref="JsonIgnoreAttribute"/> null filtering and the path-key conventions enforced by <see cref="AnarchyPathCanon"/>.
/// </remarks>
internal sealed class PathRoleReport
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? root_path { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? directories { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? files { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? relative { get; set; }
}

/// <summary>
/// Groups the canonical path-role reports for origin, source, and destination into one response object.
/// </summary>
/// <remarks>
/// Purpose: keep install and assessment payloads explicit about where content was authored, read from, and written or inspected.
/// Expected input: zero to three <see cref="PathRoleReport"/> instances created by setup or runtime code.
/// Expected output: a JSON-serializable container with omitted null roles.
/// Critical dependencies: <see cref="PathRoleReport"/> and the calling code that decides which roles apply to an operation.
/// </remarks>
internal sealed class PathRoleCollection
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PathRoleReport? origin { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PathRoleReport? source { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PathRoleReport? destination { get; set; }
}

/// <summary>
/// Exposes the repo-authored path canon used by setup, runtime discovery, script generation, and published artifacts.
/// </summary>
/// <remarks>
/// Purpose: provide a single runtime binding over generated path constants so code stops rebuilding important paths with ad hoc literals.
/// Expected input: relative path fragments, install-scope decisions, and plugin names supplied by higher-level callers.
/// Expected output: normalized canon-relative strings, absolute resolved paths, and nested path-role reports.
/// Critical dependencies: <see cref="GeneratedAnarchyPathCanon"/>, <see cref="Path"/>, and the generated publish artifacts under harness/pathing/generated.
/// </remarks>
internal static class AnarchyPathCanon
{
    public static IReadOnlyList<string> PluginSurfaces => GeneratedAnarchyPathCanon.PluginSurfaces;

    public static IReadOnlyList<string> PortableSchemaFiles => GeneratedAnarchyPathCanon.PortableSchemaFiles;

    public static IReadOnlyList<string> OwnedPluginNameExact => GeneratedAnarchyPathCanon.OwnedPluginNameExact;

    public static IReadOnlyList<string> OwnedPluginNamePrefixes => GeneratedAnarchyPathCanon.OwnedPluginNamePrefixes;

    public static IReadOnlyList<string> OwnedMarketplaceNameExact => GeneratedAnarchyPathCanon.OwnedMarketplaceNameExact;

    public static IReadOnlyList<string> OwnedMarketplaceNamePrefixes => GeneratedAnarchyPathCanon.OwnedMarketplaceNamePrefixes;

    public static IReadOnlyList<string> OwnedMcpServerNames => GeneratedAnarchyPathCanon.OwnedMcpServerNames;

    public static string RepoSourcePluginDirectoryRelativePath => GeneratedAnarchyPathCanon.RepoSourcePluginDirectoryRelativePath;

    public static string RepoSourceSetupExecutableFileRelativePath => GeneratedAnarchyPathCanon.RepoSourceSetupExecutableFileRelativePath;

    public static string RepoSourceGeneratedPluginReadmeSourceRelativePath => GeneratedAnarchyPathCanon.RepoSourceGeneratedPluginReadmeSourceRelativePath;

    public static string RepoSourceGeneratedPluginReadmeTargetRelativePath => GeneratedAnarchyPathCanon.RepoSourceGeneratedPluginReadmeTargetRelativePath;

    public static string RepoSourcePluginMcpFileRelativePath => GeneratedAnarchyPathCanon.RepoSourcePluginMcpFileRelativePath;

    public static string BundlePluginManifestFileRelativePath => GeneratedAnarchyPathCanon.BundlePluginManifestFileRelativePath;

    public static string BundleMcpFileRelativePath => GeneratedAnarchyPathCanon.BundleMcpFileRelativePath;

    public static string BundleReadmeFileRelativePath => GeneratedAnarchyPathCanon.BundleReadmeFileRelativePath;

    public static string BundlePrivacyFileRelativePath => GeneratedAnarchyPathCanon.BundlePrivacyFileRelativePath;

    public static string BundleTermsFileRelativePath => GeneratedAnarchyPathCanon.BundleTermsFileRelativePath;

    public static string BundleRuntimeDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleRuntimeDirectoryRelativePath;

    public static string BundleRuntimeExecutableFileRelativePath => GeneratedAnarchyPathCanon.BundleRuntimeExecutableFileRelativePath;

    public static string BundleSkillDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleSkillDirectoryRelativePath;

    public static string BundleSkillFileRelativePath => GeneratedAnarchyPathCanon.BundleSkillFileRelativePath;

    public static string BundleSchemaManifestFileRelativePath => GeneratedAnarchyPathCanon.BundleSchemaManifestFileRelativePath;

    public static string BundleContractsDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleContractsDirectoryRelativePath;

    public static string BundleSchemasDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleSchemasDirectoryRelativePath;

    public static string BundleScriptsDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleScriptsDirectoryRelativePath;

    public static string BundleAssetsDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleAssetsDirectoryRelativePath;

    public static string BundlePathingDirectoryRelativePath => GeneratedAnarchyPathCanon.BundlePathingDirectoryRelativePath;

    public static string BundlePathingPsd1FileRelativePath => GeneratedAnarchyPathCanon.BundlePathingPsd1FileRelativePath;

    public static string UserProfileInstallRootDirectoryRelativePath => GeneratedAnarchyPathCanon.UserProfileInstallRootDirectoryRelativePath;

    public static string UserProfilePluginParentDirectoryRelativePath => GeneratedAnarchyPathCanon.UserProfilePluginParentDirectoryRelativePath;

    public static string UserProfilePluginCacheParentDirectoryRelativePath => GeneratedAnarchyPathCanon.UserProfilePluginCacheParentDirectoryRelativePath;

    public static string UserProfileMarketplaceFileRelativePath => GeneratedAnarchyPathCanon.UserProfileMarketplaceFileRelativePath;

    public static string UserProfileCodexConfigFileRelativePath => GeneratedAnarchyPathCanon.UserProfileCodexConfigFileRelativePath;

    public static string RepoLocalPluginParentDirectoryRelativePath => GeneratedAnarchyPathCanon.RepoLocalPluginParentDirectoryRelativePath;

    public static string RepoLocalMarketplaceFileRelativePath => GeneratedAnarchyPathCanon.RepoLocalMarketplaceFileRelativePath;

    public static string LegacyUserProfilePluginParentDirectoryRelativePath => GeneratedAnarchyPathCanon.LegacyUserProfilePluginParentDirectoryRelativePath;

    public static string RepoLocalMarketplacePluginSourcePrefix => GeneratedAnarchyPathCanon.RepoLocalMarketplacePluginSourcePrefix;

    public static string UserProfileMarketplacePluginSourcePrefix => GeneratedAnarchyPathCanon.UserProfileMarketplacePluginSourcePrefix;

    public static string BundleRuntimeCommandRelativePath => GeneratedAnarchyPathCanon.BundleRuntimeCommandRelativePath;

    public static string BundleRuntimeWindowsCommandRelativePath => GeneratedAnarchyPathCanon.BundleRuntimeWindowsCommandRelativePath;

    public static string BundleRuntimeWorkingDirectoryRelativePath => GeneratedAnarchyPathCanon.BundleRuntimeWorkingDirectoryRelativePath;

    public static string DefaultPluginName => GeneratedAnarchyPathCanon.DefaultPluginName;

    public static string UserProfileMarketplaceName => GeneratedAnarchyPathCanon.UserProfileMarketplaceName;

    public static string RepoScopedPluginNameTemplate => GeneratedAnarchyPathCanon.RepoScopedPluginNameTemplate;

    public static string RepoScopedMarketplaceNameTemplate => GeneratedAnarchyPathCanon.RepoScopedMarketplaceNameTemplate;

    /// <summary>
    /// Converts a path fragment into canon form with forward slashes and no leading slash.
    /// </summary>
    /// <param name="relativePath">Relative path text that may contain mixed separators.</param>
    /// <returns>A normalized canon-relative path suitable for storage, labels, and later combination.</returns>
    /// <remarks>Critical dependencies: the repo-authored path canon convention that all relative paths are slash-normalized.</remarks>
    public static string NormalizeCanonRelativePath(string relativePath)
    {
        return relativePath.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// Resolves a canon-relative path against an absolute root path.
    /// </summary>
    /// <param name="rootPath">Absolute root path that anchors the resolution.</param>
    /// <param name="relativePath">Canon-relative path to resolve beneath the root.</param>
    /// <returns>An absolute filesystem path for the requested relative surface.</returns>
    /// <remarks>Critical dependencies: <see cref="NormalizeCanonRelativePath(string)"/> and <see cref="Path.GetFullPath(string)"/>.</remarks>
    public static string ResolveRelativePath(string rootPath, string relativePath)
    {
        var normalizedRelativePath = NormalizeCanonRelativePath(relativePath);
        var systemRelativePath = normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(rootPath, systemRelativePath));
    }

    /// <summary>
    /// Combines multiple canon-relative fragments into one normalized canon-relative path.
    /// </summary>
    /// <param name="parts">Path fragments that may contain empty values or mixed separators.</param>
    /// <returns>A single normalized canon-relative path with empty fragments removed.</returns>
    /// <remarks>Critical dependencies: <see cref="NormalizeCanonRelativePath(string)"/> and caller-supplied fragments that already reflect the path canon.</remarks>
    public static string CombineCanonRelativePath(params string[] parts)
    {
        return string.Join(
            "/",
            parts
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(NormalizeCanonRelativePath)
                .Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    /// <summary>
    /// Resolves a bundle-relative file or directory path beneath an installed plugin root.
    /// </summary>
    /// <param name="pluginRoot">Absolute plugin root for the installed or repo-local bundle.</param>
    /// <param name="bundleRelativePath">Bundle-relative canon path to resolve.</param>
    /// <returns>An absolute path inside the plugin bundle.</returns>
    /// <remarks>Critical dependencies: <see cref="ResolveRelativePath(string,string)"/> and bundle-relative constants from <see cref="GeneratedAnarchyPathCanon"/>.</remarks>
    public static string ResolveBundleFilePath(string pluginRoot, string bundleRelativePath)
    {
        return ResolveRelativePath(pluginRoot, bundleRelativePath);
    }

    /// <summary>
    /// Resolves the repo-authored plugin source directory from a source root.
    /// </summary>
    /// <param name="sourceRoot">Repo or extracted source root that carries the plugin source tree.</param>
    /// <returns>The absolute path to the source plugin directory beneath that root.</returns>
    /// <remarks>Critical dependencies: <see cref="RepoSourcePluginDirectoryRelativePath"/> and <see cref="ResolveRelativePath(string,string)"/>.</remarks>
    public static string ResolveSourcePluginDirectory(string sourceRoot)
    {
        return ResolveRelativePath(sourceRoot, RepoSourcePluginDirectoryRelativePath);
    }

    /// <summary>
    /// Resolves the repo-local plugin root that should exist inside a target workspace.
    /// </summary>
    /// <param name="repoRoot">Workspace root for the repo-local installation target.</param>
    /// <param name="pluginName">Expected plugin directory name for that workspace.</param>
    /// <returns>The absolute repo-local plugin root path.</returns>
    /// <remarks>Critical dependencies: repo-local parent path constants and the caller's plugin-name selection logic.</remarks>
    public static string ResolveRepoLocalPluginRoot(string repoRoot, string pluginName)
    {
        return ResolveRelativePath(repoRoot, CombineCanonRelativePath(RepoLocalPluginParentDirectoryRelativePath, pluginName));
    }

    /// <summary>
    /// Resolves the home-local plugin root for a user-profile install.
    /// </summary>
    /// <param name="userProfileRoot">User-profile root that anchors the Codex home install.</param>
    /// <param name="pluginName">Expected plugin directory name beneath the user-profile plugin parent.</param>
    /// <returns>The absolute home-local plugin root path.</returns>
    /// <remarks>Critical dependencies: user-profile parent path constants and the current Codex home-install model.</remarks>
    public static string ResolveUserProfilePluginRoot(string userProfileRoot, string pluginName)
    {
        return ResolveRelativePath(userProfileRoot, CombineCanonRelativePath(UserProfilePluginParentDirectoryRelativePath, pluginName));
    }

    /// <summary>
    /// Resolves the deprecated legacy home-local plugin root used only for detection and cleanup guidance.
    /// </summary>
    /// <param name="userProfileRoot">User-profile root used to inspect legacy state.</param>
    /// <param name="pluginName">Expected legacy plugin directory name.</param>
    /// <returns>The absolute path of the legacy plugin root.</returns>
    /// <remarks>Critical dependencies: legacy path aliases in the canon and callers that treat this only as detection, not a write target.</remarks>
    public static string ResolveLegacyUserProfilePluginRoot(string userProfileRoot, string pluginName)
    {
        return ResolveRelativePath(userProfileRoot, CombineCanonRelativePath(LegacyUserProfilePluginParentDirectoryRelativePath, pluginName));
    }

    /// <summary>
    /// Resolves the repo-local plugin marketplace file path for a workspace.
    /// </summary>
    /// <param name="repoRoot">Workspace root that contains the repo-local marketplace.</param>
    /// <returns>The absolute path to the repo-local marketplace file.</returns>
    /// <remarks>Critical dependencies: <see cref="RepoLocalMarketplaceFileRelativePath"/> and repo-local installation conventions.</remarks>
    public static string ResolveRepoLocalMarketplaceFilePath(string repoRoot)
    {
        return ResolveRelativePath(repoRoot, RepoLocalMarketplaceFileRelativePath);
    }

    /// <summary>
    /// Resolves the home-local personal marketplace file path.
    /// </summary>
    /// <param name="userProfileRoot">User-profile root that contains the personal marketplace.</param>
    /// <returns>The absolute path to the personal marketplace file.</returns>
    /// <remarks>Critical dependencies: <see cref="UserProfileMarketplaceFileRelativePath"/> and Codex personal plugin registration.</remarks>
    public static string ResolveUserProfileMarketplaceFilePath(string userProfileRoot)
    {
        return ResolveRelativePath(userProfileRoot, UserProfileMarketplaceFileRelativePath);
    }

    /// <summary>
    /// Resolves the Codex config file used for optional custom-MCP fallback inspection.
    /// </summary>
    /// <param name="userProfileRoot">User-profile root that contains the Codex home directory.</param>
    /// <returns>The absolute path to the user-profile Codex config file.</returns>
    /// <remarks>Critical dependencies: <see cref="UserProfileCodexConfigFileRelativePath"/> and the host-specific config layer.</remarks>
    public static string ResolveUserProfileCodexConfigFilePath(string userProfileRoot)
    {
        return ResolveRelativePath(userProfileRoot, UserProfileCodexConfigFileRelativePath);
    }

    /// <summary>
    /// Builds the marketplace-relative source.path value for either repo-local or user-profile registration.
    /// </summary>
    /// <param name="userProfile">True for the user-profile marketplace lane; false for repo-local.</param>
    /// <param name="pluginName">Plugin directory name to append to the lane-specific prefix.</param>
    /// <returns>A marketplace-relative source.path value suitable for marketplace.json.</returns>
    /// <remarks>Critical dependencies: the lane-specific source prefixes defined in the generated path canon.</remarks>
    public static string BuildMarketplacePluginSourceRelativePath(bool userProfile, string pluginName)
    {
        return (userProfile ? UserProfileMarketplacePluginSourcePrefix : RepoLocalMarketplacePluginSourcePrefix) + pluginName;
    }

    /// <summary>
    /// Verifies whether a plugin name belongs to the current or legacy Anarchy-AI identities owned by this repo.
    /// </summary>
    /// <param name="pluginName">Plugin name to classify.</param>
    /// <returns><c>true</c> when the supplied name matches an exact owned name or an owned repo-scoped prefix.</returns>
    /// <remarks>Critical dependencies: the repo-authored owned-name lists generated from the path canon.</remarks>
    public static bool IsOwnedPluginName(string? pluginName)
    {
        return MatchesExactOrPrefix(pluginName, OwnedPluginNameExact, OwnedPluginNamePrefixes);
    }

    /// <summary>
    /// Verifies whether a marketplace name belongs to the current or legacy Anarchy-AI marketplace identities.
    /// </summary>
    /// <param name="marketplaceName">Marketplace name to classify.</param>
    /// <returns><c>true</c> when the supplied name matches an exact owned name or an owned marketplace prefix.</returns>
    /// <remarks>Critical dependencies: the repo-authored owned-marketplace lists generated from the path canon.</remarks>
    public static bool IsOwnedMarketplaceName(string? marketplaceName)
    {
        return MatchesExactOrPrefix(marketplaceName, OwnedMarketplaceNameExact, OwnedMarketplaceNamePrefixes);
    }

    /// <summary>
    /// Verifies whether an MCP server name belongs to the current or legacy Anarchy-AI identities.
    /// </summary>
    /// <param name="serverName">MCP server name to classify.</param>
    /// <returns><c>true</c> when the supplied name is one of the owned MCP server names.</returns>
    /// <remarks>Critical dependencies: the repo-authored owned MCP-name list generated from the path canon.</remarks>
    public static bool IsOwnedMcpServerName(string? serverName)
    {
        return !string.IsNullOrWhiteSpace(serverName)
               && OwnedMcpServerNames.Any(candidate => string.Equals(candidate, serverName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies whether a marketplace source.path uses a supported repo-local or user-profile prefix.
    /// </summary>
    /// <param name="sourcePath">Marketplace source.path value to classify.</param>
    /// <returns><c>true</c> when the path starts with a supported canon prefix; otherwise <c>false</c>.</returns>
    /// <remarks>Critical dependencies: lane prefixes from the generated path canon and callers that gate plugin discovery on those prefixes.</remarks>
    public static bool IsSupportedMarketplacePluginSourceRelativePath(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return false;
        }

        var normalizedSourcePath = NormalizeCanonRelativePath(sourcePath);
        var usesSupportedPrefix =
            normalizedSourcePath.StartsWith(NormalizeCanonRelativePath(RepoLocalMarketplacePluginSourcePrefix), StringComparison.OrdinalIgnoreCase) ||
            normalizedSourcePath.StartsWith(NormalizeCanonRelativePath(UserProfileMarketplacePluginSourcePrefix), StringComparison.OrdinalIgnoreCase);
        if (!usesSupportedPrefix)
        {
            return false;
        }

        var pluginDirectoryName = Path.GetFileName(normalizedSourcePath.Replace('/', Path.DirectorySeparatorChar));
        return IsOwnedPluginName(pluginDirectoryName);
    }

    /// <summary>
    /// Formats a canon-relative path for human-facing home-local labels.
    /// </summary>
    /// <param name="relativePath">Canon-relative path beneath the user profile.</param>
    /// <returns>A home-local label path prefixed with <c>~\</c>.</returns>
    /// <remarks>Critical dependencies: human-facing disclosure/help text that should stay destination-relative and Windows-friendly.</remarks>
    public static string BuildHomeLabelPath(string relativePath)
    {
        return @"~\" + NormalizeCanonRelativePath(relativePath).Replace('/', '\\');
    }

    /// <summary>
    /// Formats a canon-relative path for human-facing repo-local labels.
    /// </summary>
    /// <param name="relativePath">Canon-relative path beneath the workspace root.</param>
    /// <returns>A Windows-style repo-relative label path.</returns>
    /// <remarks>Critical dependencies: disclosure/help generation and the repo-relative path canon.</remarks>
    public static string BuildRepoLabelPath(string relativePath)
    {
        return NormalizeCanonRelativePath(relativePath).Replace('/', '\\');
    }

    /// <summary>
    /// Builds the assembly resource prefix for the embedded plugin bundle payload.
    /// </summary>
    /// <returns>The manifest-resource prefix used to locate embedded plugin bundle files.</returns>
    /// <remarks>Critical dependencies: setup payload embedding and the repo-source plugin directory constant.</remarks>
    public static string BuildPluginPayloadResourcePrefix()
    {
        return $"SetupPayload/{NormalizeCanonRelativePath(RepoSourcePluginDirectoryRelativePath)}/";
    }

    /// <summary>
    /// Builds the manifest-resource path for a specific embedded bundle surface.
    /// </summary>
    /// <param name="bundleRelativePath">Bundle-relative canon path beneath the plugin root.</param>
    /// <returns>The manifest-resource name for the requested embedded bundle surface.</returns>
    /// <remarks>Critical dependencies: <see cref="BuildPluginPayloadResourcePrefix"/> and bundle-relative path constants.</remarks>
    public static string BuildPluginPayloadResourcePath(string bundleRelativePath)
    {
        return BuildPluginPayloadResourcePrefix() + NormalizeCanonRelativePath(bundleRelativePath);
    }

    /// <summary>
    /// Builds the assembly resource prefix for the embedded portable schema payload.
    /// </summary>
    /// <returns>The manifest-resource prefix used for the portable schema family payload.</returns>
    /// <remarks>Critical dependencies: generated portable-schema payload directory constants and setup resource embedding.</remarks>
    public static string BuildPortableSchemaPayloadResourcePrefix()
    {
        return $"SetupPayload/{NormalizeCanonRelativePath(GeneratedAnarchyPathCanon.PortableSchemaPayloadDirectoryRelativePath)}/";
    }

    /// <summary>
    /// Builds the manifest-resource path for one embedded portable schema file.
    /// </summary>
    /// <param name="fileName">Portable schema file name carried by the payload.</param>
    /// <returns>The manifest-resource name for that schema file.</returns>
    /// <remarks>Critical dependencies: <see cref="BuildPortableSchemaPayloadResourcePrefix"/> and repo-authored portable schema filenames.</remarks>
    public static string BuildPortableSchemaPayloadResourcePath(string fileName)
    {
        return BuildPortableSchemaPayloadResourcePrefix() + fileName;
    }

    /// <summary>
    /// Creates a normalized path-role report and drops empty collections instead of emitting placeholders.
    /// </summary>
    /// <param name="rootPath">Optional absolute root path for the role.</param>
    /// <param name="directories">Optional keyed directory paths for the role.</param>
    /// <param name="files">Optional keyed file paths for the role.</param>
    /// <param name="relative">Optional keyed relative-path references for the role.</param>
    /// <returns>A populated <see cref="PathRoleReport"/> or <c>null</c> when every supplied value is empty.</returns>
    /// <remarks>Critical dependencies: <see cref="CreateDictionary"/> and callers that omit non-applicable path roles instead of guessing values.</remarks>
    public static PathRoleReport? CreateRoleReport(
        string? rootPath = null,
        IEnumerable<KeyValuePair<string, string?>>? directories = null,
        IEnumerable<KeyValuePair<string, string?>>? files = null,
        IEnumerable<KeyValuePair<string, string?>>? relative = null)
    {
        var normalizedDirectories = CreateDictionary(directories);
        var normalizedFiles = CreateDictionary(files);
        var normalizedRelative = CreateDictionary(relative);

        if (string.IsNullOrWhiteSpace(rootPath)
            && normalizedDirectories is null
            && normalizedFiles is null
            && normalizedRelative is null)
        {
            return null;
        }

        return new PathRoleReport
        {
            root_path = string.IsNullOrWhiteSpace(rootPath) ? null : rootPath,
            directories = normalizedDirectories,
            files = normalizedFiles,
            relative = normalizedRelative
        };
    }

    /// <summary>
    /// Creates a path-role collection for origin, source, and destination reporting.
    /// </summary>
    /// <param name="origin">Optional origin role report representing repo-authored truth.</param>
    /// <param name="source">Optional source role report representing the material actually inspected or copied.</param>
    /// <param name="destination">Optional destination role report representing the write or assessment target.</param>
    /// <returns>A <see cref="PathRoleCollection"/> carrying the supplied roles.</returns>
    /// <remarks>Critical dependencies: the calling workflow's role selection logic and the nested path contract expected by setup/runtime JSON.</remarks>
    public static PathRoleCollection CreateRoleCollection(
        PathRoleReport? origin = null,
        PathRoleReport? source = null,
        PathRoleReport? destination = null)
    {
        return new PathRoleCollection
        {
            origin = origin,
            source = source,
            destination = destination
        };
    }

    /// <summary>
    /// Normalizes a sequence of keyed path values into a sorted dictionary while dropping blank entries.
    /// </summary>
    /// <param name="source">Optional keyed values supplied by the caller.</param>
    /// <returns>A sorted read-only dictionary, or <c>null</c> when no nonblank entries remain.</returns>
    /// <remarks>Critical dependencies: ordinal key ordering for stable JSON output and callers that permit null when a category is not applicable.</remarks>
    private static IReadOnlyDictionary<string, string>? CreateDictionary(IEnumerable<KeyValuePair<string, string?>>? source)
    {
        if (source is null)
        {
            return null;
        }

        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            result[pair.Key] = pair.Value!;
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// Applies the shared exact-or-prefix owned-identity match used by plugin and marketplace naming guards.
    /// </summary>
    /// <param name="candidate">Name to inspect.</param>
    /// <param name="exactValues">Exact owned values accepted without prefix logic.</param>
    /// <param name="prefixValues">Owned prefixes accepted for repo-scoped generated identities.</param>
    /// <returns><c>true</c> when the candidate matches one of the supplied owned identity rules.</returns>
    /// <remarks>Critical dependencies: repo-authored owned-identity lists and ordinal-ignore-case matching.</remarks>
    private static bool MatchesExactOrPrefix(string? candidate, IEnumerable<string> exactValues, IEnumerable<string> prefixValues)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return exactValues.Any(value => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
               || prefixValues.Any(value => candidate.StartsWith(value, StringComparison.OrdinalIgnoreCase));
    }
}
