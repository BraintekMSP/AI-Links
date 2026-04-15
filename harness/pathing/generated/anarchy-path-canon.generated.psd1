@{
  version = '0.1.0'
  names = @{
    default_plugin_name = 'anarchy-ai'
    repo_scoped_marketplace_name_template = 'anarchy-ai-local-<repo-slug>'
    repo_scoped_plugin_directory_name_template = 'anarchy-ai-local-<repo-slug>-<stable-path-hash>'
    runtime_executable_file_name = 'AnarchyAi.Mcp.Server.exe'
    setup_executable_file_name = 'AnarchyAi.Setup.exe'
    user_profile_marketplace_name = 'anarchy-ai-user-profile'
  }
  relative_paths = @{
    bundle_assets_directory_relative_path = 'assets'
    bundle_contracts_directory_relative_path = 'contracts'
    bundle_mcp_file_relative_path = '.mcp.json'
    bundle_pathing_directory_relative_path = 'pathing'
    bundle_pathing_psd1_file_relative_path = 'pathing/anarchy-path-canon.generated.psd1'
    bundle_plugin_manifest_file_relative_path = '.codex-plugin/plugin.json'
    bundle_privacy_file_relative_path = 'PRIVACY.md'
    bundle_readme_file_relative_path = 'README.md'
    bundle_runtime_directory_relative_path = 'runtime/win-x64'
    bundle_runtime_executable_file_relative_path = 'runtime/win-x64/AnarchyAi.Mcp.Server.exe'
    bundle_schema_manifest_file_relative_path = 'schemas/schema-bundle.manifest.json'
    bundle_schemas_directory_relative_path = 'schemas'
    bundle_scripts_directory_relative_path = 'scripts'
    bundle_skill_directory_relative_path = 'skills/anarchy-ai-harness'
    bundle_skill_file_relative_path = 'skills/anarchy-ai-harness/SKILL.md'
    bundle_terms_file_relative_path = 'TERMS.md'
    legacy_user_profile_plugin_parent_directory_relative_path = 'plugins'
    portable_schema_payload_directory_relative_path = 'portable-schema'
    repo_local_marketplace_file_relative_path = '.agents/plugins/marketplace.json'
    repo_local_plugin_parent_directory_relative_path = 'plugins'
    repo_source_generated_plugin_readme_source_relative_path = 'docs/ANARCHY_AI_PLUGIN_README_SOURCE.md'
    repo_source_generated_plugin_readme_target_relative_path = 'plugins/anarchy-ai/README.md'
    repo_source_plugin_directory_relative_path = 'plugins/anarchy-ai'
    repo_source_plugin_mcp_file_relative_path = 'plugins/anarchy-ai/.mcp.json'
    repo_source_setup_executable_file_relative_path = 'plugins/AnarchyAi.Setup.exe'
    user_profile_codex_config_file_relative_path = '.codex/config.toml'
    user_profile_install_root_directory_relative_path = '.codex'
    user_profile_marketplace_file_relative_path = '.agents/plugins/marketplace.json'
    user_profile_plugin_cache_parent_directory_relative_path = '.codex/plugins/cache'
    user_profile_plugin_parent_directory_relative_path = '.codex/plugins'
  }
  relative_references = @{
    bundle_runtime_command_relative_path = './runtime/win-x64/AnarchyAi.Mcp.Server.exe'
    bundle_runtime_windows_command_relative_path = '.\runtime\win-x64\AnarchyAi.Mcp.Server.exe'
    bundle_runtime_working_directory_relative_path = '.'
    repo_local_marketplace_plugin_source_prefix = './plugins/'
    user_profile_marketplace_plugin_source_prefix = './.codex/plugins/'
  }
  arrays = @{
    audit_allowlist_globs = @(
      '.agents/plugins/marketplace.json'
      'docs/ANARCHY_AI_BUG_REPORTS.md'
      'docs/CHANGELOG_ai_links.md'
      'docs/ANARCHY_AI_ENVIRONMENT_TRUTH_MATRIX.md'
      'docs/scripts/test-documentation-truth-compliance.ps1'
      'branding/**'
      'harness/branding/**'
      'harness/pathing/**'
      'harness/setup/tests/**'
      'harness/server/tests/**'
      'plugins/anarchy-ai/.mcp.json'
      'plugins/anarchy-ai/branding/**'
      'plugins/anarchy-ai/pathing/**'
      'plugins/anarchy-ai/README.md'
      'docs/ANARCHY_AI_PLUGIN_README_SOURCE.md'
    )
    audit_forbidden_path_patterns = @(
      '(?i)\.codex/plugins'
      '(?i)\.agents/plugins/marketplace\.json'
      '(?i)\.codex/config\.toml'
      '(?i)runtime[\\/]win-x64[\\/]AnarchyAi\.Mcp\.Server\.exe'
      '(?i)schemas[\\/]schema-bundle\.manifest\.json'
      '(?i)\./\.codex/plugins/'
      '(?i)\./plugins/anarchy-ai'
    )
    owned_marketplace_name_exact = @(
      'anarchy-ai-herringms-user-profile'
      'anarchy-ai-user-profile'
      'anarchy-user-profile'
    )
    owned_marketplace_name_prefixes = @(
      'anarchy-ai-local-'
      'anarchy-ai-herringms-local-'
      'anarchy-local-'
    )
    owned_mcp_server_names = @(
      'anarchy-ai-herringms'
      'anarchy-ai'
    )
    owned_plugin_name_exact = @(
      'anarchy-ai-herringms'
      'anarchy-ai'
    )
    owned_plugin_name_prefixes = @(
      'anarchy-ai-local-'
      'anarchy-ai-herringms-'
      'anarchy-local-'
    )
    plugin_surfaces = @(
      '.codex-plugin'
      'assets'
      'branding'
      'contracts'
      'pathing'
      'runtime'
      'schemas'
      'scripts'
      'skills'
      '.mcp.json'
      'README.md'
      'PRIVACY.md'
      'TERMS.md'
    )
    portable_schema_files = @(
      'AGENTS-schema-governance.json'
      'AGENTS-schema-1project.json'
      'AGENTS-schema-narrative.json'
      'AGENTS-schema-gov2gov-migration.json'
      'AGENTS-schema-triage.md'
      'Getting-Started-For-Humans.txt'
    )
  }
}
