namespace AnarchyAi.Pathing;

internal static class GeneratedAnarchyPathCanon
{
    public const string Version = "0.1.0";
    public const string DefaultPluginName = "anarchy-ai";
    public const string RepoScopedMarketplaceNameTemplate = "anarchy-ai-repo-<repo-slug>";
    public const string RepoScopedPluginDirectoryNameTemplate = "anarchy-ai";
    public const string RuntimeExecutableFileName = "AnarchyAi.Mcp.Server.exe";
    public const string SetupExecutableFileName = "AnarchyAi.Setup.exe";
    public const string UserProfileMarketplaceName = "anarchy-ai-user-profile";
    public const string BundleAssetsDirectoryRelativePath = "assets";
    public const string BundleContractsDirectoryRelativePath = "contracts";
    public const string BundleMcpFileRelativePath = ".mcp.json";
    public const string BundleNarrativeRecordTemplateFileRelativePath = "templates/narratives/record.template.json";
    public const string BundleNarrativeRegisterTemplateFileRelativePath = "templates/narratives/register.template.json";
    public const string BundleNarrativeTemplatesDirectoryRelativePath = "templates/narratives";
    public const string BundlePathingDirectoryRelativePath = "pathing";
    public const string BundlePathingPsd1FileRelativePath = "pathing/anarchy-path-canon.generated.psd1";
    public const string BundlePluginManifestFileRelativePath = ".codex-plugin/plugin.json";
    public const string BundlePrivacyFileRelativePath = "PRIVACY.md";
    public const string BundleReadmeFileRelativePath = "README.md";
    public const string BundleRuntimeDirectoryRelativePath = "runtime/win-x64";
    public const string BundleRuntimeExecutableFileRelativePath = "runtime/win-x64/AnarchyAi.Mcp.Server.exe";
    public const string BundleSchemaManifestFileRelativePath = "schemas/schema-bundle.manifest.json";
    public const string BundleSchemasDirectoryRelativePath = "schemas";
    public const string BundleScriptsDirectoryRelativePath = "scripts";
    public const string BundleSkillDirectoryRelativePath = "skills/anarchy-ai-harness";
    public const string BundleSkillFileRelativePath = "skills/anarchy-ai-harness/SKILL.md";
    public const string BundleTemplatesDirectoryRelativePath = "templates";
    public const string BundleTermsFileRelativePath = "TERMS.md";
    public const string LegacyUserProfilePluginParentDirectoryRelativePath = "plugins";
    public const string PortableSchemaPayloadDirectoryRelativePath = "portable-schema";
    public const string RepoLocalMarketplaceFileRelativePath = ".agents/plugins/marketplace.json";
    public const string RepoLocalPluginParentDirectoryRelativePath = "plugins";
    public const string RepoSourceGeneratedPluginReadmeSourceRelativePath = "docs/ANARCHY_AI_PLUGIN_README_SOURCE.md";
    public const string RepoSourceGeneratedPluginReadmeTargetRelativePath = "plugins/anarchy-ai/README.md";
    public const string RepoSourcePluginDirectoryRelativePath = "plugins/anarchy-ai";
    public const string RepoSourcePluginMcpFileRelativePath = "plugins/anarchy-ai/.mcp.json";
    public const string RepoSourceSetupExecutableFileRelativePath = "plugins/AnarchyAi.Setup.exe";
    public const string UserProfileCodexConfigFileRelativePath = ".codex/config.toml";
    public const string UserProfileInstallRootDirectoryRelativePath = ".codex";
    public const string UserProfileMarketplaceFileRelativePath = ".agents/plugins/marketplace.json";
    public const string UserProfilePluginCacheParentDirectoryRelativePath = ".codex/plugins/cache";
    public const string UserProfilePluginParentDirectoryRelativePath = ".codex/plugins";
    public const string BundleRuntimeCommandRelativePath = "./runtime/win-x64/AnarchyAi.Mcp.Server.exe";
    public const string BundleRuntimeWindowsCommandRelativePath = ".\\runtime\\win-x64\\AnarchyAi.Mcp.Server.exe";
    public const string BundleRuntimeWorkingDirectoryRelativePath = ".";
    public const string RepoLocalMarketplacePluginSourcePrefix = "./plugins/";
    public const string UserProfileMarketplacePluginSourcePrefix = "./.codex/plugins/";

    public static readonly string[] AuditAllowlistGlobs =
    [
        ".agents/plugins/marketplace.json",
        "docs/ANARCHY_AI_BUG_REPORTS.md",
        "docs/CHANGELOG_ai_links.md",
        "docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md",
        "docs/scripts/test-documentation-truth-compliance.ps1",
        "docs/scripts/test-removal-safety-compliance.ps1",
        "docs/scripts/test-schema-mirror-sync-compliance.ps1",
        "branding/**",
        "harness/branding/**",
        "harness/pathing/**",
        "harness/setup/tests/**",
        "harness/server/tests/**",
        "narratives/projects/ai-links.json",
        "plugins/anarchy-ai/.mcp.json",
        "plugins/anarchy-ai/branding/**",
        "plugins/anarchy-ai/pathing/**",
        "plugins/anarchy-ai/README.md",
        "plugins/anarchy-ai/scripts/remove-anarchy-ai-human.ps1",
        "docs/ANARCHY_AI_PLUGIN_README_SOURCE.md",
    ];

    public static readonly string[] AuditForbiddenPathPatterns =
    [
        "(?i)\\.codex/plugins",
        "(?i)\\.agents/plugins/marketplace\\.json",
        "(?i)\\.codex/config\\.toml",
        "(?i)runtime[\\\\/]win-x64[\\\\/]AnarchyAi\\.Mcp\\.Server\\.exe",
        "(?i)schemas[\\\\/]schema-bundle\\.manifest\\.json",
        "(?i)\\./\\.codex/plugins/",
        "(?i)\\./plugins/anarchy-ai",
    ];

    public static readonly string[] OwnedMarketplaceNameExact =
    [
        "anarchy-ai-herringms-user-profile",
        "anarchy-ai-user-profile",
        "anarchy-user-profile",
    ];

    public static readonly string[] OwnedMarketplaceNamePrefixes =
    [
        "anarchy-ai-repo-",
        "anarchy-ai-local-",
        "anarchy-ai-herringms-local-",
        "anarchy-local-",
    ];

    public static readonly string[] OwnedMcpServerNames =
    [
        "anarchy-ai-herringms",
        "anarchy-ai",
    ];

    public static readonly string[] OwnedPluginNameExact =
    [
        "anarchy-ai-herringms",
        "anarchy-ai",
    ];

    public static readonly string[] OwnedPluginNamePrefixes =
    [
        "anarchy-ai-local-",
        "anarchy-ai-herringms-",
        "anarchy-local-",
    ];

    public static readonly string[] PluginSurfaces =
    [
        ".codex-plugin",
        "assets",
        "branding",
        "contracts",
        "pathing",
        "runtime",
        "schemas",
        "scripts",
        "skills",
        "templates",
        ".mcp.json",
        "README.md",
        "PRIVACY.md",
        "TERMS.md",
    ];

    public static readonly string[] PortableSchemaFiles =
    [
        "AGENTS-schema-governance.json",
        "AGENTS-schema-1project.json",
        "AGENTS-schema-narrative.json",
        "AGENTS-schema-gov2gov-migration.json",
        "AGENTS-schema-triage.md",
        "Getting-Started-For-Humans.txt",
    ];
}
