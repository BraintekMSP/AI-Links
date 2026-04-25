<#
.SYNOPSIS
Generates code and script artifacts that bind setup and publish helpers to the repo-authored branding canon.
.DESCRIPTION
Reads branding/branding-canon.json and emits C#, PowerShell, and MSBuild artifacts so brand names, legal URLs,
and asset paths do not drift across setup, manifests, or publish helpers.
.PARAMETER RepoRoot
Optional repo root override. Defaults to the repository that contains this script.
.OUTPUTS
JSON describing the branding canon source and generated artifact paths.
.NOTES
Critical dependencies: branding/branding-canon.json, filesystem write access inside the repo, and stable key names in the branding canon.
#>
param(
  [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..'))
}
else {
  $RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
}

$sourcePath = Join-Path $RepoRoot 'branding\branding-canon.json'
$generatedCSharpPath = Join-Path $RepoRoot 'harness\branding\generated\GeneratedAnarchyBranding.g.cs'
$generatedPsd1Path = Join-Path $RepoRoot 'harness\branding\generated\anarchy-branding.generated.psd1'
$pluginGeneratedPsd1Path = Join-Path $RepoRoot 'plugins\anarchy-ai\branding\anarchy-branding.generated.psd1'
$generatedPropsPath = Join-Path $RepoRoot 'harness\branding\generated\AnarchyBranding.Generated.props'

if (-not (Test-Path $sourcePath)) {
  throw "Branding canon not found: $sourcePath"
}

$canon = Get-Content $sourcePath -Raw | ConvertFrom-Json

function Convert-ToCSharpStringLiteral {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Value
  )

  return $Value.Replace('\', '\\').Replace('"', '\"')
}

function Convert-ToXmlText {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Value
  )

  return [System.Security.SecurityElement]::Escape($Value)
}

function Write-IfChanged {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [Parameter(Mandatory = $true)]
    [string]$Content
  )

  $directory = Split-Path -Parent $Path
  if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
  }

  if (-not $Content.EndsWith("`n", [System.StringComparison]::Ordinal)) {
    $Content += [Environment]::NewLine
  }

  $existing = if (Test-Path $Path) { Get-Content $Path -Raw } else { '' }
  if (-not [string]::Equals($existing, $Content, [System.StringComparison]::Ordinal)) {
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
  }
}

$version = [string]$canon.version
$brandDisplayName = [string]$canon.names.brand_display_name
$setupDisplayName = [string]$canon.names.setup_display_name
$userProfileMarketplaceDisplayName = [string]$canon.names.user_profile_marketplace_display_name
$repoLocalMarketplaceDisplayNameTemplate = [string]$canon.names.repo_local_marketplace_display_name_template
$authorName = [string]$canon.metadata.author_name
$authorUrl = [string]$canon.metadata.author_url
$homepageUrl = [string]$canon.metadata.homepage_url
$repositoryUrl = [string]$canon.metadata.repository_url
$developerName = [string]$canon.metadata.developer_name
$pluginManifestVersion = [string]$canon.metadata.plugin_manifest_version
if ([string]::IsNullOrWhiteSpace($pluginManifestVersion)) {
  throw "Branding canon metadata.plugin_manifest_version is required so Codex cache invalidation is release-explicit."
}
$privacyPolicyUrl = [string]$canon.metadata.privacy_policy_url
$termsOfServiceUrl = [string]$canon.metadata.terms_of_service_url
$defaultUpdateSourceZipUrl = [string]$canon.metadata.default_update_source_zip_url
$brandColor = [string]$canon.metadata.brand_color
$pluginDescription = [string]$canon.messaging.plugin_description
$pluginShortDescription = [string]$canon.messaging.plugin_short_description
$pluginLongDescription = [string]$canon.messaging.plugin_long_description
$pluginDefaultPromptLines = @($canon.messaging.plugin_default_prompt_lines | ForEach-Object { [string]$_ })
$brandingRootDirectoryRelativePath = [string]$canon.relative_paths.branding_root_directory_relative_path
$brandingAssetsDirectoryRelativePath = [string]$canon.relative_paths.branding_assets_directory_relative_path
$brandingPublishedMaterialsDirectoryRelativePath = [string]$canon.relative_paths.branding_published_materials_directory_relative_path
$brandingInstructionAdditionsDirectoryRelativePath = [string]$canon.relative_paths.branding_instruction_additions_directory_relative_path
$bundleBrandingDirectoryRelativePath = [string]$canon.relative_paths.bundle_branding_directory_relative_path
$bundleBrandingPsd1FileRelativePath = [string]$canon.relative_paths.bundle_branding_psd1_file_relative_path
$bundleSetupHeaderImageRelativePath = [string]$canon.relative_paths.bundle_setup_header_image_relative_path
$bundleSetupIconRelativePath = [string]$canon.relative_paths.bundle_setup_icon_relative_path
$bundlePluginComposerIconRelativePath = [string]$canon.relative_paths.bundle_plugin_composer_icon_relative_path
$bundlePluginLogoRelativePath = [string]$canon.relative_paths.bundle_plugin_logo_relative_path

$csharp = @"
namespace AnarchyAi.Branding;

internal static class GeneratedAnarchyBranding
{
    public const string Version = "$(Convert-ToCSharpStringLiteral $version)";
    public const string BrandDisplayName = "$(Convert-ToCSharpStringLiteral $brandDisplayName)";
    public const string SetupDisplayName = "$(Convert-ToCSharpStringLiteral $setupDisplayName)";
    public const string UserProfileMarketplaceDisplayName = "$(Convert-ToCSharpStringLiteral $userProfileMarketplaceDisplayName)";
    public const string RepoLocalMarketplaceDisplayNameTemplate = "$(Convert-ToCSharpStringLiteral $repoLocalMarketplaceDisplayNameTemplate)";
    public const string AuthorName = "$(Convert-ToCSharpStringLiteral $authorName)";
    public const string AuthorUrl = "$(Convert-ToCSharpStringLiteral $authorUrl)";
    public const string HomepageUrl = "$(Convert-ToCSharpStringLiteral $homepageUrl)";
    public const string RepositoryUrl = "$(Convert-ToCSharpStringLiteral $repositoryUrl)";
    public const string DeveloperName = "$(Convert-ToCSharpStringLiteral $developerName)";
    public const string PluginManifestVersion = "$(Convert-ToCSharpStringLiteral $pluginManifestVersion)";
    public const string PrivacyPolicyUrl = "$(Convert-ToCSharpStringLiteral $privacyPolicyUrl)";
    public const string TermsOfServiceUrl = "$(Convert-ToCSharpStringLiteral $termsOfServiceUrl)";
    public const string DefaultUpdateSourceZipUrl = "$(Convert-ToCSharpStringLiteral $defaultUpdateSourceZipUrl)";
    public const string BrandColor = "$(Convert-ToCSharpStringLiteral $brandColor)";
    public const string PluginDescription = "$(Convert-ToCSharpStringLiteral $pluginDescription)";
    public const string PluginShortDescription = "$(Convert-ToCSharpStringLiteral $pluginShortDescription)";
    public const string PluginLongDescription = "$(Convert-ToCSharpStringLiteral $pluginLongDescription)";
    public const string BrandingRootDirectoryRelativePath = "$(Convert-ToCSharpStringLiteral $brandingRootDirectoryRelativePath)";
    public const string BrandingAssetsDirectoryRelativePath = "$(Convert-ToCSharpStringLiteral $brandingAssetsDirectoryRelativePath)";
    public const string BrandingPublishedMaterialsDirectoryRelativePath = "$(Convert-ToCSharpStringLiteral $brandingPublishedMaterialsDirectoryRelativePath)";
    public const string BrandingInstructionAdditionsDirectoryRelativePath = "$(Convert-ToCSharpStringLiteral $brandingInstructionAdditionsDirectoryRelativePath)";
    public const string BundleBrandingDirectoryRelativePath = "$(Convert-ToCSharpStringLiteral $bundleBrandingDirectoryRelativePath)";
    public const string BundleBrandingPsd1FileRelativePath = "$(Convert-ToCSharpStringLiteral $bundleBrandingPsd1FileRelativePath)";
    public const string BundleSetupHeaderImageRelativePath = "$(Convert-ToCSharpStringLiteral $bundleSetupHeaderImageRelativePath)";
    public const string BundleSetupIconRelativePath = "$(Convert-ToCSharpStringLiteral $bundleSetupIconRelativePath)";
    public const string BundlePluginComposerIconRelativePath = "$(Convert-ToCSharpStringLiteral $bundlePluginComposerIconRelativePath)";
    public const string BundlePluginLogoRelativePath = "$(Convert-ToCSharpStringLiteral $bundlePluginLogoRelativePath)";
    public static readonly string[] PluginDefaultPromptLines = new[]
    {
$(($pluginDefaultPromptLines | ForEach-Object { '        "' + (Convert-ToCSharpStringLiteral $_) + '"' }) -join ",`n")
    };
}
"@

$psd1 = @"
@{
  version = '$version'
  names = @{
    brand_display_name = '$brandDisplayName'
    setup_display_name = '$setupDisplayName'
    user_profile_marketplace_display_name = '$userProfileMarketplaceDisplayName'
    repo_local_marketplace_display_name_template = '$repoLocalMarketplaceDisplayNameTemplate'
  }
  metadata = @{
    author_name = '$authorName'
    author_url = '$authorUrl'
    homepage_url = '$homepageUrl'
    repository_url = '$repositoryUrl'
    developer_name = '$developerName'
    plugin_manifest_version = '$pluginManifestVersion'
    privacy_policy_url = '$privacyPolicyUrl'
    terms_of_service_url = '$termsOfServiceUrl'
    default_update_source_zip_url = '$defaultUpdateSourceZipUrl'
    brand_color = '$brandColor'
  }
  messaging = @{
    plugin_description = '$pluginDescription'
    plugin_short_description = '$pluginShortDescription'
    plugin_long_description = '$pluginLongDescription'
    plugin_default_prompt_lines = @(
$(($pluginDefaultPromptLines | ForEach-Object { "      '$_'" }) -join "`n")
    )
  }
  relative_paths = @{
    branding_root_directory_relative_path = '$brandingRootDirectoryRelativePath'
    branding_assets_directory_relative_path = '$brandingAssetsDirectoryRelativePath'
    branding_published_materials_directory_relative_path = '$brandingPublishedMaterialsDirectoryRelativePath'
    branding_instruction_additions_directory_relative_path = '$brandingInstructionAdditionsDirectoryRelativePath'
    bundle_branding_directory_relative_path = '$bundleBrandingDirectoryRelativePath'
    bundle_branding_psd1_file_relative_path = '$bundleBrandingPsd1FileRelativePath'
    bundle_setup_header_image_relative_path = '$bundleSetupHeaderImageRelativePath'
    bundle_setup_icon_relative_path = '$bundleSetupIconRelativePath'
    bundle_plugin_composer_icon_relative_path = '$bundlePluginComposerIconRelativePath'
    bundle_plugin_logo_relative_path = '$bundlePluginLogoRelativePath'
  }
}
"@

$props = @"
<Project>
  <PropertyGroup>
    <AnarchyBrandingVersion>$(Convert-ToXmlText $version)</AnarchyBrandingVersion>
    <AnarchyBrandingBrandDisplayName>$(Convert-ToXmlText $brandDisplayName)</AnarchyBrandingBrandDisplayName>
    <AnarchyBrandingSetupDisplayName>$(Convert-ToXmlText $setupDisplayName)</AnarchyBrandingSetupDisplayName>
    <AnarchyBrandingUserProfileMarketplaceDisplayName>$(Convert-ToXmlText $userProfileMarketplaceDisplayName)</AnarchyBrandingUserProfileMarketplaceDisplayName>
    <AnarchyBrandingRepoLocalMarketplaceDisplayNameTemplate>$(Convert-ToXmlText $repoLocalMarketplaceDisplayNameTemplate)</AnarchyBrandingRepoLocalMarketplaceDisplayNameTemplate>
    <AnarchyBrandingAuthorName>$(Convert-ToXmlText $authorName)</AnarchyBrandingAuthorName>
    <AnarchyBrandingAuthorUrl>$(Convert-ToXmlText $authorUrl)</AnarchyBrandingAuthorUrl>
    <AnarchyBrandingHomepageUrl>$(Convert-ToXmlText $homepageUrl)</AnarchyBrandingHomepageUrl>
    <AnarchyBrandingRepositoryUrl>$(Convert-ToXmlText $repositoryUrl)</AnarchyBrandingRepositoryUrl>
    <AnarchyBrandingDeveloperName>$(Convert-ToXmlText $developerName)</AnarchyBrandingDeveloperName>
    <AnarchyBrandingPluginManifestVersion>$(Convert-ToXmlText $pluginManifestVersion)</AnarchyBrandingPluginManifestVersion>
    <AnarchyBrandingPrivacyPolicyUrl>$(Convert-ToXmlText $privacyPolicyUrl)</AnarchyBrandingPrivacyPolicyUrl>
    <AnarchyBrandingTermsOfServiceUrl>$(Convert-ToXmlText $termsOfServiceUrl)</AnarchyBrandingTermsOfServiceUrl>
    <AnarchyBrandingDefaultUpdateSourceZipUrl>$(Convert-ToXmlText $defaultUpdateSourceZipUrl)</AnarchyBrandingDefaultUpdateSourceZipUrl>
    <AnarchyBrandingBrandColor>$(Convert-ToXmlText $brandColor)</AnarchyBrandingBrandColor>
    <AnarchyBrandingPluginDescription>$(Convert-ToXmlText $pluginDescription)</AnarchyBrandingPluginDescription>
    <AnarchyBrandingPluginShortDescription>$(Convert-ToXmlText $pluginShortDescription)</AnarchyBrandingPluginShortDescription>
    <AnarchyBrandingPluginLongDescription>$(Convert-ToXmlText $pluginLongDescription)</AnarchyBrandingPluginLongDescription>
    <AnarchyBrandingBrandingRootDirectoryRelativePath>$(Convert-ToXmlText $brandingRootDirectoryRelativePath)</AnarchyBrandingBrandingRootDirectoryRelativePath>
    <AnarchyBrandingBrandingAssetsDirectoryRelativePath>$(Convert-ToXmlText $brandingAssetsDirectoryRelativePath)</AnarchyBrandingBrandingAssetsDirectoryRelativePath>
    <AnarchyBrandingBrandingPublishedMaterialsDirectoryRelativePath>$(Convert-ToXmlText $brandingPublishedMaterialsDirectoryRelativePath)</AnarchyBrandingBrandingPublishedMaterialsDirectoryRelativePath>
    <AnarchyBrandingBrandingInstructionAdditionsDirectoryRelativePath>$(Convert-ToXmlText $brandingInstructionAdditionsDirectoryRelativePath)</AnarchyBrandingBrandingInstructionAdditionsDirectoryRelativePath>
    <AnarchyBrandingBundleBrandingDirectoryRelativePath>$(Convert-ToXmlText $bundleBrandingDirectoryRelativePath)</AnarchyBrandingBundleBrandingDirectoryRelativePath>
    <AnarchyBrandingBundleBrandingPsd1FileRelativePath>$(Convert-ToXmlText $bundleBrandingPsd1FileRelativePath)</AnarchyBrandingBundleBrandingPsd1FileRelativePath>
    <AnarchyBrandingBundleSetupHeaderImageRelativePath>$(Convert-ToXmlText $bundleSetupHeaderImageRelativePath)</AnarchyBrandingBundleSetupHeaderImageRelativePath>
    <AnarchyBrandingBundleSetupIconRelativePath>$(Convert-ToXmlText $bundleSetupIconRelativePath)</AnarchyBrandingBundleSetupIconRelativePath>
    <AnarchyBrandingBundlePluginComposerIconRelativePath>$(Convert-ToXmlText $bundlePluginComposerIconRelativePath)</AnarchyBrandingBundlePluginComposerIconRelativePath>
    <AnarchyBrandingBundlePluginLogoRelativePath>$(Convert-ToXmlText $bundlePluginLogoRelativePath)</AnarchyBrandingBundlePluginLogoRelativePath>
  </PropertyGroup>
</Project>
"@

Write-IfChanged -Path $generatedCSharpPath -Content $csharp
Write-IfChanged -Path $generatedPsd1Path -Content $psd1
Write-IfChanged -Path $pluginGeneratedPsd1Path -Content $psd1
Write-IfChanged -Path $generatedPropsPath -Content $props

[pscustomobject]@{
  source = $sourcePath
  generated_csharp = $generatedCSharpPath
  generated_psd1 = $generatedPsd1Path
  plugin_generated_psd1 = $pluginGeneratedPsd1Path
  generated_props = $generatedPropsPath
} | ConvertTo-Json -Depth 5
