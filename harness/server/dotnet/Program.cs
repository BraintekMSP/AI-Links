using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace SpindleMcp.Server;

internal sealed class ContractLoader
{
    private readonly string _contractsDir = Path.GetFullPath(
        Path.Combine(Environment.CurrentDirectory, "harness", "contracts"));

    public JsonElement LoadContract(string contractFileName)
    {
        var contractPath = Path.Combine(_contractsDir, contractFileName);
        return JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(contractPath));
    }
}

[McpServerToolType]
internal sealed class SpindleTools(ContractLoader contracts)
{
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
        return new
        {
            status = "scaffold_only",
            message = "Harness contract is loaded, but live schema-reality evaluation is not implemented yet.",
            workspace_root,
            expected_schema_package,
            startup_surfaces = startup_surfaces ?? [],
            contract = contracts.LoadContract("schema-reality.contract.json")
        };
    }

    [McpServerTool(
        Name = "run_gov2gov_migration",
        Title = "Run Gov2Gov Migration",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        UseStructuredContent = true)]
    [Description("Run non-destructive gov2gov reconciliation for a partial or copied_only schema package.")]
    public object RunGov2GovMigration(
        [Description("Absolute workspace root to inspect and reconcile.")] string workspace_root,
        [Description("Expected schema family or package name.")] string expected_schema_package,
        [Description("Input state from the schema-reality tool.")] string schema_reality_state,
        [Description("Reason list from the active schema reality state.")] string[] active_reasons,
        [Description("Optional startup or package surfaces that should align with the schema package.")] string[]? startup_surfaces = null,
        [Description("Plan changes only, or apply only non-destructive reconciliation steps.")] string migration_mode = "plan_only")
    {
        return new
        {
            status = "scaffold_only",
            message = "Harness contract is loaded, but gov2gov reconciliation is not implemented yet.",
            workspace_root,
            expected_schema_package,
            schema_reality_state,
            active_reasons,
            startup_surfaces = startup_surfaces ?? [],
            migration_mode,
            contract = contracts.LoadContract("gov2gov-migration.contract.json")
        };
    }
}

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddSimpleConsole();
        builder.Services.AddSingleton<ContractLoader>();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<SpindleTools>();

        await builder.Build().RunAsync();
    }
}
