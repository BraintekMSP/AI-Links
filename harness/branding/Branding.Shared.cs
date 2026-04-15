namespace AnarchyAi.Branding;

/// <summary>
/// Exposes the repo-authored branding canon used by setup, publish helpers, and future fork-specific rebrands.
/// </summary>
/// <remarks>
/// Purpose: centralize brand names, author metadata, legal URLs, and bundle asset paths so operational code stops hard-coding brand literals repeatedly.
/// Expected input: optional repo name values for display formatting and generated constants emitted from the branding canon.
/// Expected output: stable brand metadata, bundle-relative asset paths, and marketplace display names derived from one source.
/// Critical dependencies: <see cref="GeneratedAnarchyBranding"/>, string replacement over the repo-local display template, and the repo-authored branding canon.
/// </remarks>
internal static class AnarchyBranding
{
    public static string BrandDisplayName => GeneratedAnarchyBranding.BrandDisplayName;

    public static string SetupDisplayName => GeneratedAnarchyBranding.SetupDisplayName;

    public static string UserProfileMarketplaceDisplayName => GeneratedAnarchyBranding.UserProfileMarketplaceDisplayName;

    public static string RepoLocalMarketplaceDisplayNameTemplate => GeneratedAnarchyBranding.RepoLocalMarketplaceDisplayNameTemplate;

    public static string AuthorName => GeneratedAnarchyBranding.AuthorName;

    public static string AuthorUrl => GeneratedAnarchyBranding.AuthorUrl;

    public static string HomepageUrl => GeneratedAnarchyBranding.HomepageUrl;

    public static string RepositoryUrl => GeneratedAnarchyBranding.RepositoryUrl;

    public static string DeveloperName => GeneratedAnarchyBranding.DeveloperName;

    public static string PrivacyPolicyUrl => GeneratedAnarchyBranding.PrivacyPolicyUrl;

    public static string TermsOfServiceUrl => GeneratedAnarchyBranding.TermsOfServiceUrl;

    public static string DefaultUpdateSourceZipUrl => GeneratedAnarchyBranding.DefaultUpdateSourceZipUrl;

    public static string BrandColor => GeneratedAnarchyBranding.BrandColor;

    public static string BrandingRootDirectoryRelativePath => GeneratedAnarchyBranding.BrandingRootDirectoryRelativePath;

    public static string BrandingAssetsDirectoryRelativePath => GeneratedAnarchyBranding.BrandingAssetsDirectoryRelativePath;

    public static string BrandingPublishedMaterialsDirectoryRelativePath => GeneratedAnarchyBranding.BrandingPublishedMaterialsDirectoryRelativePath;

    public static string BrandingInstructionAdditionsDirectoryRelativePath => GeneratedAnarchyBranding.BrandingInstructionAdditionsDirectoryRelativePath;

    public static string BundleBrandingDirectoryRelativePath => GeneratedAnarchyBranding.BundleBrandingDirectoryRelativePath;

    public static string BundleBrandingPsd1FileRelativePath => GeneratedAnarchyBranding.BundleBrandingPsd1FileRelativePath;

    public static string BundleSetupHeaderImageRelativePath => GeneratedAnarchyBranding.BundleSetupHeaderImageRelativePath;

    public static string BundleSetupIconRelativePath => GeneratedAnarchyBranding.BundleSetupIconRelativePath;

    public static string BundlePluginComposerIconRelativePath => GeneratedAnarchyBranding.BundlePluginComposerIconRelativePath;

    public static string BundlePluginLogoRelativePath => GeneratedAnarchyBranding.BundlePluginLogoRelativePath;

    /// <summary>
    /// Formats the repo-local marketplace display name using the configured template and optional repo name.
    /// </summary>
    /// <param name="repoName">Repo name to place into the display template. Blank values fall back to <c>Repo</c>.</param>
    /// <returns>A branded repo-local marketplace display name.</returns>
    /// <remarks>Critical dependencies: <see cref="RepoLocalMarketplaceDisplayNameTemplate"/> and the stable <c>&lt;RepoName&gt;</c> token contract in the branding canon.</remarks>
    public static string BuildRepoLocalMarketplaceDisplayName(string? repoName)
    {
        var effectiveRepoName = string.IsNullOrWhiteSpace(repoName) ? "Repo" : repoName;
        return RepoLocalMarketplaceDisplayNameTemplate.Replace("<RepoName>", effectiveRepoName, StringComparison.Ordinal);
    }
}
