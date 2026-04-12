using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace AnarchyAi.Setup;

internal enum OperationMode
{
    Assess,
    Install,
    Update
}

internal sealed class SetupOptions
{
    public OperationMode Mode { get; init; } = OperationMode.Assess;
    public string HostContext { get; init; } = "codex";
    public bool Silent { get; init; }
    public bool JsonOutput { get; init; }
    public bool RefreshPortableSchemaFamily { get; init; }
    public string UpdateSourceZipUrl { get; init; } = "https://github.com/BraintekMSP/AI-Links/archive/refs/heads/main.zip";
    public string UpdateSourcePath { get; init; } = string.Empty;
    public string RepoPath { get; init; } = string.Empty;
}

internal sealed class SetupResult
{
    public required string bootstrap_state { get; init; }
    public required string host_context { get; init; }
    public required bool update_requested { get; init; }
    public required string update_state { get; init; }
    public required string update_source_zip_url { get; init; }
    public required string update_source_path { get; init; }
    public required string[] update_notes { get; init; }
    public required string repo_root { get; init; }
    public required string plugin_root { get; init; }
    public required bool runtime_present { get; init; }
    public required bool marketplace_registered { get; init; }
    public required bool installed_by_default { get; init; }
    public required string[] actions_taken { get; init; }
    public required string[] missing_components { get; init; }
    public required string[] safe_repairs { get; init; }
    public required string next_action { get; init; }
}

internal static class ProgramJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (CliParser.RequestsHelp(args))
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(SetupEngine.BuildCommandLineHelp(CliParser.TryReadRepoPath(args)));
                return 0;
            }

            if (args.Length == 0)
            {
                ConsoleWindow.Hide();
                ApplicationConfiguration.Initialize();
                Application.Run(new SetupForm());
                return 0;
            }

            var options = CliParser.Parse(args);
            var engine = new SetupEngine();
            var result = engine.Execute(options);
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(JsonSerializer.Serialize(result, ProgramJson.Options));
            return result.bootstrap_state == "ready" ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                error = ex.Message,
                type = ex.GetType().FullName
            }, ProgramJson.Options));
            return 2;
        }
    }
}

internal static class ConsoleWindow
{
    private const int SwHide = 0;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void Hide()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SwHide);
        }
    }
}

internal sealed class SetupForm : Form
{
    private readonly TextBox _repoPathTextBox;
    private readonly TextBox _resultTextBox;
    private readonly Label _statusLabel;

    public SetupForm()
    {
        Text = "Anarchy-AI Setup";
        Width = 820;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;

        var rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        rootPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Install or assess Anarchy-AI in a repo. The local repo is preferred when this setup EXE is placed inside a repo's plugins folder."
        }, 0, 0);

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        pathPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Repo Path:",
            Margin = new Padding(0, 8, 8, 0)
        }, 0, 0);

        _repoPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = SetupEngine.TryResolveDefaultRepoRoot() ?? string.Empty
        };
        pathPanel.Controls.Add(_repoPathTextBox, 1, 0);

        var browseButton = new Button { Text = "Browse..." };
        browseButton.Click += BrowseButton_Click;
        pathPanel.Controls.Add(browseButton, 2, 0);
        rootPanel.Controls.Add(pathPanel, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true
        };

        var assessButton = new Button { Text = "Assess", AutoSize = true };
        assessButton.Click += (_, _) => Execute(OperationMode.Assess);
        buttonPanel.Controls.Add(assessButton);

        var installButton = new Button { Text = "Install", AutoSize = true };
        installButton.Click += (_, _) => Execute(OperationMode.Install);
        buttonPanel.Controls.Add(installButton);

        var closeButton = new Button { Text = "Close", AutoSize = true };
        closeButton.Click += (_, _) => Close();
        buttonPanel.Controls.Add(closeButton);

        _statusLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(16, 8, 0, 0),
            Text = "Ready."
        };
        buttonPanel.Controls.Add(_statusLabel);
        rootPanel.Controls.Add(buttonPanel, 0, 2);

        _resultTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new System.Drawing.Font("Consolas", 9.0f)
        };
        rootPanel.Controls.Add(_resultTextBox, 0, 3);

        Controls.Add(rootPanel);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the target repo root"
        };

        if (Directory.Exists(_repoPathTextBox.Text))
        {
            dialog.InitialDirectory = _repoPathTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _repoPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void Execute(OperationMode mode)
    {
        try
        {
            if (mode == OperationMode.Install)
            {
                var disclosure = new InstallDisclosureForm(
                    _repoPathTextBox.Text.Trim(),
                    SetupEngine.BuildInstallDisclosure(_repoPathTextBox.Text.Trim()));

                if (disclosure.ShowDialog(this) != DialogResult.OK)
                {
                    _statusLabel.Text = "Install cancelled.";
                    return;
                }
            }

            _statusLabel.Text = $"{mode} in progress...";
            var result = new SetupEngine().Execute(new SetupOptions
            {
                Mode = mode,
                HostContext = "codex",
                RepoPath = _repoPathTextBox.Text.Trim()
            });

            _resultTextBox.Text = JsonSerializer.Serialize(result, ProgramJson.Options);
            _statusLabel.Text = result.bootstrap_state == "ready"
                ? $"{mode} completed: ready"
                : $"{mode} completed: {result.bootstrap_state}";
        }
        catch (Exception ex)
        {
            _resultTextBox.Text = JsonSerializer.Serialize(new { error = ex.Message }, ProgramJson.Options);
            _statusLabel.Text = $"{mode} failed";
        }
    }
}

internal sealed class InstallDisclosureForm : Form
{
    public InstallDisclosureForm(string repoPath, string disclosureText)
    {
        Text = "Install Disclosure";
        Width = 780;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        rootPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Review expected repo, user, and agent impact before continuing."
        }, 0, 0);

        rootPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"Target repo: {repoPath}",
            Margin = new Padding(0, 6, 0, 10)
        }, 0, 1);

        var disclosureBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Text = disclosureText,
            Font = new System.Drawing.Font("Consolas", 9.0f)
        };
        rootPanel.Controls.Add(disclosureBox, 0, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var continueButton = new Button
        {
            Text = "Continue Install",
            AutoSize = true,
            DialogResult = DialogResult.OK
        };
        buttonPanel.Controls.Add(continueButton);

        var backButton = new Button
        {
            Text = "Back",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        buttonPanel.Controls.Add(backButton);

        rootPanel.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = continueButton;
        CancelButton = backButton;
        Controls.Add(rootPanel);
    }
}

internal static class CliParser
{
    public static bool RequestsHelp(string[] args)
    {
        foreach (var arg in args)
        {
            if (IsHelpAlias(arg))
            {
                return true;
            }
        }

        return false;
    }

    public static string? TryReadRepoPath(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (NormalizeSwitch(args[i]) != "repo")
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                return null;
            }

            return args[i + 1];
        }

        return null;
    }

    public static SetupOptions Parse(string[] args)
    {
        var mode = OperationMode.Assess;
        var hostContext = "codex";
        var silent = false;
        var jsonOutput = false;
        var refreshPortableSchemaFamily = false;
        var updateSourceZipUrl = "https://github.com/BraintekMSP/AI-Links/archive/refs/heads/main.zip";
        var updateSourcePath = string.Empty;
        var repoPath = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var normalized = NormalizeSwitch(arg);

            switch (normalized)
            {
                case "assess":
                    mode = OperationMode.Assess;
                    break;
                case "install":
                    mode = OperationMode.Install;
                    break;
                case "update":
                    mode = OperationMode.Update;
                    break;
                case "silent":
                    silent = true;
                    break;
                case "json":
                    jsonOutput = true;
                    break;
                case "refreshschemas":
                    refreshPortableSchemaFamily = true;
                    break;
                case "repo":
                    repoPath = ReadValue(args, ref i, normalized);
                    break;
                case "host":
                    hostContext = ReadValue(args, ref i, normalized);
                    break;
                case "sourcepath":
                    updateSourcePath = ReadValue(args, ref i, normalized);
                    break;
                case "sourceurl":
                    updateSourceZipUrl = ReadValue(args, ref i, normalized);
                    break;
                default:
                    throw new ArgumentException($"Unsupported switch: {arg}");
            }
        }

        return new SetupOptions
        {
            Mode = mode,
            HostContext = hostContext,
            Silent = silent,
            JsonOutput = jsonOutput,
            RefreshPortableSchemaFamily = refreshPortableSchemaFamily,
            UpdateSourceZipUrl = updateSourceZipUrl,
            UpdateSourcePath = updateSourcePath,
            RepoPath = repoPath
        };
    }

    private static bool IsHelpAlias(string arg)
    {
        var normalized = NormalizeSwitch(arg);
        return normalized is "?" or "h" or "help";
    }

    private static string NormalizeSwitch(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            throw new ArgumentException("Empty switch is not allowed.");
        }

        return (arg.StartsWith('/') || arg.StartsWith('-'))
            ? arg.TrimStart('/', '-').ToLowerInvariant()
            : arg.ToLowerInvariant();
    }

    private static string ReadValue(string[] args, ref int index, string switchName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for /{switchName}.");
        }

        index++;
        return args[index];
    }
}
internal sealed class SetupEngine
{
    private static readonly string[] ContractFiles =
    [
        "active-work-state.contract.json",
        "schema-reality.contract.json",
        "gov2gov-migration.contract.json",
        "preflight-session.contract.json",
        "harness-gap-state.contract.json"
    ];

    private static readonly string[] PortableSchemaFiles =
    [
        "AGENTS-schema-governance.json",
        "AGENTS-schema-1project.json",
        "AGENTS-schema-narrative.json",
        "AGENTS-schema-gov2gov-migration.json",
        "AGENTS-schema-triage.md",
        "Getting-Started-For-Humans.txt"
    ];

    private static readonly string[] PluginSurfaces =
    [
        ".codex-plugin",
        "assets",
        "contracts",
        "runtime",
        "schemas",
        "scripts",
        "skills",
        ".mcp.json",
        "README.md",
        "PRIVACY.md",
        "TERMS.md"
    ];

    private static readonly string[] CurrentToolNames =
    [
        "preflight_session",
        "compile_active_work_state",
        "is_schema_real_or_shadow_copied",
        "assess_harness_gap_state",
        "run_gov2gov_migration"
    ];

    public static string? TryResolveDefaultRepoRoot()
    {
        var exeDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        var currentDirectory = Path.GetFullPath(Environment.CurrentDirectory);

        if (TryResolveRepoFromPluginsDirectory(exeDirectory, out var repoFromExe))
        {
            return repoFromExe;
        }

        return LooksLikeRepoRoot(currentDirectory) ? currentDirectory : null;
    }

    public static string BuildInstallDisclosure(string repoPath)
    {
        var targetRepo = string.IsNullOrWhiteSpace(repoPath) ? "(repo path unresolved)" : repoPath;
        var disclosureLines = new[]
        {
            "Responsible disclosure for repo-local Anarchy-AI install.",
            $"Target: {targetRepo}",
            "Repo impact:",
            $"- Adds plugins\\anarchy-ai\\ with {PluginSurfaces.Length} bundled surfaces.",
            "- Creates or updates .agents\\plugins\\marketplace.json.",
            "- Registers anarchy-ai as INSTALLED_BY_DEFAULT in the target repo.",
            "- Current GUI install does not rewrite AGENTS.md.",
            "- Current GUI install does not copy root schema files unless a schema-refresh path is added later.",
            "Product behavior:",
            $"- Exposes {CurrentToolNames.Length} harness tools for preflight, gap assessment, active-work compilation, schema reality, and gov2gov reconciliation.",
            "Human impact:",
            "- Repo-local only; no machine-wide install or settings change.",
            "- Install itself does not start the MCP runtime as a background process.",
            "AI impact:",
            "- Makes Anarchy-AI available by default to agents in this repo.",
            "- Strengthens startup/control surfaces; it does not rewrite project code by itself.",
            "Back out now if this repo should remain unchanged."
        };

        return string.Join(Environment.NewLine, disclosureLines);
    }

    public static string BuildCommandLineHelp(string? repoPath)
    {
        var resolvedRepo = string.IsNullOrWhiteSpace(repoPath)
            ? TryResolveDefaultRepoRoot()
            : Path.GetFullPath(repoPath);

        var targetRepo = string.IsNullOrWhiteSpace(resolvedRepo) ? "(repo path unresolved)" : resolvedRepo;
        var availabilityLead = string.IsNullOrWhiteSpace(resolvedRepo)
            ? "Anarchy-AI can be installed into a target repo."
            : "This repo has Anarchy-AI available.";
        var lines = new[]
        {
            "Anarchy-AI Setup",
            string.Empty,
            "Usage:",
            "  AnarchyAi.Setup.exe /assess [/repo <path>] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /install [/repo <path>] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /update [/repo <path>] [/sourcepath <path>] [/sourceurl <url>] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /? | -? | /h | -h | /help | -help | --help | --?",
            string.Empty,
            "Availability:",
            $"  {availabilityLead}",
            "  Installing it would give the target repo preflight, gap assessment, and schema reality checks.",
            "  It also exposes active-work compilation and gov2gov reconciliation through the same harness surface.",
            $"  Target repo: {targetRepo}",
            string.Empty,
            "Here's what changes:",
            $"- Adds plugins\\anarchy-ai\\ with {PluginSurfaces.Length} bundled surfaces.",
            "- Creates or updates .agents\\plugins\\marketplace.json.",
            "- Registers anarchy-ai as INSTALLED_BY_DEFAULT in the target repo.",
            $"- Makes {CurrentToolNames.Length} bounded harness tools available to supported hosts.",
            "- Does not rewrite AGENTS.md.",
            "- Does not copy portable root schema files unless /refreshschemas is passed.",
            string.Empty,
            "Flags:",
            "  /repo <path>            Override repo auto-detection.",
            "  /sourcepath <path>      Refresh from a local AI-Links source path.",
            "  /sourceurl <url>        Refresh from a zip source URL.",
            "  /refreshschemas         Also materialize the portable schema family into repo root.",
            "  /json                   Emit JSON result for assess/install/update operations.",
            "  /silent                 Suppress GUI/prompt behavior for CLI use.",
            "  /host <name>            Carry host context such as codex, claude, cursor, or generic."
        };

        return string.Join(Environment.NewLine, lines);
    }

    public SetupResult Execute(SetupOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var repoRoot = ResolveRepoRoot(options.RepoPath);
        var pluginRoot = Path.Combine(repoRoot, "plugins", "anarchy-ai");
        var marketplacePath = Path.Combine(repoRoot, ".agents", "plugins", "marketplace.json");
        var runtimePath = Path.Combine(pluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe");
        var pluginManifestPath = Path.Combine(pluginRoot, ".codex-plugin", "plugin.json");
        var mcpPath = Path.Combine(pluginRoot, ".mcp.json");
        var skillPath = Path.Combine(pluginRoot, "skills", "anarchy-ai-harness", "SKILL.md");
        var schemaManifestPath = Path.Combine(pluginRoot, "schemas", "schema-bundle.manifest.json");

        var actionsTaken = new HashSet<string>(StringComparer.Ordinal);
        var missingComponents = new HashSet<string>(StringComparer.Ordinal);
        var safeRepairs = new HashSet<string>(StringComparer.Ordinal);
        var updateNotes = new HashSet<string>(StringComparer.Ordinal);

        var updateRequested = options.Mode == OperationMode.Update;
        var updateState = updateRequested ? "in_progress" : "not_requested";

        if (options.Mode == OperationMode.Install)
        {
            ExtractEmbeddedPluginBundle(pluginRoot, actionsTaken);
            EnsureMarketplaceRegistration(marketplacePath, actionsTaken);

            if (options.RefreshPortableSchemaFamily)
            {
                ExtractEmbeddedPortableSchemaFamily(repoRoot, actionsTaken);
            }
        }
        else if (options.Mode == OperationMode.Update)
        {
            try
            {
                RefreshFromUpdateSource(pluginRoot, repoRoot, options, actionsTaken, updateNotes);
                updateState = "completed";
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
            {
                updateState = "failed";
                updateNotes.Add(ex.Message);
                missingComponents.Add("update_pull_failed");
                safeRepairs.Add("release_runtime_lock_and_retry_update");
                safeRepairs.Add("run_safe_release_runtime_lock");
                safeRepairs.Add("run_force_release_runtime_lock");
            }
            catch (Exception ex)
            {
                updateState = "failed";
                updateNotes.Add(ex.Message);
                missingComponents.Add("update_pull_failed");

                if (string.IsNullOrWhiteSpace(options.UpdateSourcePath))
                {
                    safeRepairs.Add("verify_public_repo_access_and_retry_update");
                    safeRepairs.Add("retry_update_with_local_source_path");
                }
                else
                {
                    safeRepairs.Add("verify_local_update_source_path_and_retry_update");
                }
            }
        }

        var pluginManifestExists = File.Exists(pluginManifestPath);
        var mcpExists = File.Exists(mcpPath);
        var runtimeExists = File.Exists(runtimePath);
        var skillExists = File.Exists(skillPath);
        var schemaManifestExists = File.Exists(schemaManifestPath);

        if (!pluginManifestExists) { missingComponents.Add("codex_plugin_manifest_missing"); }
        if (!mcpExists) { missingComponents.Add("codex_mcp_declaration_missing"); }
        if (!runtimeExists) { missingComponents.Add("bundled_runtime_missing"); }
        if (!skillExists && string.Equals(options.HostContext, "codex", StringComparison.OrdinalIgnoreCase))
        {
            missingComponents.Add("codex_skill_surface_missing");
        }
        if (!schemaManifestExists) { missingComponents.Add("schema_bundle_manifest_missing"); }

        foreach (var contractFile in ContractFiles)
        {
            var contractPath = Path.Combine(pluginRoot, "contracts", contractFile);
            if (!File.Exists(contractPath))
            {
                missingComponents.Add($"missing_contract:{contractFile}");
            }
        }

        var marketplaceInspection = InspectMarketplace(marketplacePath);
        missingComponents.UnionWith(marketplaceInspection.Findings);
        if (!marketplaceInspection.Exists)
        {
            missingComponents.Add("repo_marketplace_missing");
        }
        if (marketplaceInspection.Exists && !marketplaceInspection.HasPluginsArray)
        {
            missingComponents.Add("repo_marketplace_missing_plugins_array");
        }
        if (marketplaceInspection.Exists && !marketplaceInspection.IsValidJson)
        {
            missingComponents.Add("marketplace_json_invalid");
        }

        if (!runtimeExists) { safeRepairs.Add("publish_or_restore_bundled_runtime"); }
        if (!marketplaceInspection.HasAnarchyPluginEntry || !marketplaceInspection.InstalledByDefault)
        {
            safeRepairs.Add("run_bootstrap_harness_install");
        }

        var bootstrapState = runtimeExists && marketplaceInspection.HasAnarchyPluginEntry && marketplaceInspection.InstalledByDefault
            ? "ready"
            : runtimeExists && (pluginManifestExists || mcpExists)
                ? "repo_bundle_present_unregistered"
                : runtimeExists
                    ? "runtime_only"
                    : "bootstrap_needed";

        var nextAction = bootstrapState switch
        {
            "ready" => "use_preflight_session",
            "repo_bundle_present_unregistered" => "register_plugin_in_marketplace",
            "runtime_only" => "materialize_repo_plugin_bundle",
            _ => "restore_runtime_or_complete_bundle"
        };

        return new SetupResult
        {
            bootstrap_state = bootstrapState,
            host_context = NormalizeHostContext(options.HostContext),
            update_requested = updateRequested,
            update_state = updateState,
            update_source_zip_url = options.UpdateSourceZipUrl,
            update_source_path = options.UpdateSourcePath,
            update_notes = updateNotes.ToArray(),
            repo_root = repoRoot,
            plugin_root = pluginRoot,
            runtime_present = runtimeExists,
            marketplace_registered = marketplaceInspection.HasAnarchyPluginEntry,
            installed_by_default = marketplaceInspection.InstalledByDefault,
            actions_taken = actionsTaken.ToArray(),
            missing_components = missingComponents.ToArray(),
            safe_repairs = safeRepairs.ToArray(),
            next_action = nextAction
        };
    }

    private static string NormalizeHostContext(string hostContext)
    {
        return hostContext.Trim().ToLowerInvariant() switch
        {
            "claude" => "claude",
            "cursor" => "cursor",
            "generic" => "generic",
            _ => "codex"
        };
    }

    private static string ResolveRepoRoot(string explicitRepoPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepoPath))
        {
            var resolvedPath = Path.GetFullPath(explicitRepoPath);
            if (!Directory.Exists(resolvedPath))
            {
                throw new DirectoryNotFoundException($"Repo root not found: {resolvedPath}");
            }

            return resolvedPath;
        }

        var detected = TryResolveDefaultRepoRoot();
        if (!string.IsNullOrWhiteSpace(detected))
        {
            return detected;
        }

        throw new InvalidOperationException("Could not resolve the repo root automatically. Provide /repo \"C:\\path\\to\\repo\".");
    }

    private static bool TryResolveRepoFromPluginsDirectory(string path, out string repoRoot)
    {
        repoRoot = string.Empty;
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
        {
            return false;
        }

        if (string.Equals(directory.Name, "plugins", StringComparison.OrdinalIgnoreCase) &&
            directory.Parent is not null &&
            LooksLikeRepoRoot(directory.Parent.FullName))
        {
            repoRoot = directory.Parent.FullName;
            return true;
        }

        return false;
    }

    private static bool LooksLikeRepoRoot(string path)
    {
        return Directory.Exists(Path.Combine(path, ".git")) ||
               File.Exists(Path.Combine(path, "AGENTS.md")) ||
               File.Exists(Path.Combine(path, "README.md")) ||
               Directory.Exists(Path.Combine(path, "plugins")) ||
               Directory.Exists(Path.Combine(path, ".agents"));
    }

    private static void ExtractEmbeddedPluginBundle(string pluginRoot, HashSet<string> actionsTaken)
    {
        Directory.CreateDirectory(pluginRoot);
        foreach (var resource in PayloadResources.GetPluginBundleResources())
        {
            var relativePath = resource["SetupPayload/plugins/anarchy-ai/".Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var targetPath = Path.Combine(pluginRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using var stream = PayloadResources.OpenResource(resource);
            using var output = File.Create(targetPath);
            stream.CopyTo(output);
        }

        actionsTaken.Add("materialized_plugin_bundle_from_embedded_payload");
    }

    private static void ExtractEmbeddedPortableSchemaFamily(string repoRoot, HashSet<string> actionsTaken)
    {
        foreach (var resource in PayloadResources.GetPortableSchemaResources())
        {
            var fileName = resource["SetupPayload/portable-schema/".Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var targetPath = Path.Combine(repoRoot, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using var stream = PayloadResources.OpenResource(resource);
            using var output = File.Create(targetPath);
            stream.CopyTo(output);
        }

        actionsTaken.Add("materialized_portable_schema_family_from_embedded_payload");
    }

    private static void EnsureMarketplaceRegistration(string marketplacePath, HashSet<string> actionsTaken)
    {
        var marketplaceDirectory = Path.GetDirectoryName(marketplacePath)!;
        if (!Directory.Exists(marketplaceDirectory))
        {
            Directory.CreateDirectory(marketplaceDirectory);
            actionsTaken.Add("created_marketplace_directory");
        }

        JsonObject marketplaceObject;
        if (File.Exists(marketplacePath))
        {
            try
            {
                marketplaceObject = JsonNode.Parse(File.ReadAllText(marketplacePath))?.AsObject()
                    ?? CreateDefaultMarketplace();
            }
            catch (JsonException)
            {
                marketplaceObject = CreateDefaultMarketplace();
                actionsTaken.Add("replaced_invalid_marketplace_json");
            }
        }
        else
        {
            marketplaceObject = CreateDefaultMarketplace();
        }

        marketplaceObject["name"] ??= "ai-links-local";
        marketplaceObject["interface"] ??= new JsonObject { ["displayName"] = "AI-Links Local" };

        var plugins = marketplaceObject["plugins"] as JsonArray;
        if (plugins is null)
        {
            plugins = [];
            marketplaceObject["plugins"] = plugins;
        }

        var existingEntry = plugins
            .Select(node => node as JsonObject)
            .FirstOrDefault(node => string.Equals(node?["name"]?.GetValue<string>(), "anarchy-ai", StringComparison.Ordinal));

        if (existingEntry is null)
        {
            plugins.Add(CreateAnarchyPluginEntry());
            actionsTaken.Add("created_anarchy_ai_marketplace_entry");
        }
        else
        {
            existingEntry["source"] = new JsonObject
            {
                ["source"] = "local",
                ["path"] = "./plugins/anarchy-ai"
            };
            existingEntry["policy"] = new JsonObject
            {
                ["installation"] = "INSTALLED_BY_DEFAULT",
                ["authentication"] = "ON_INSTALL"
            };
            existingEntry["category"] = "Productivity";
            actionsTaken.Add("updated_anarchy_ai_marketplace_entry");
        }

        File.WriteAllText(marketplacePath, marketplaceObject.ToJsonString(ProgramJson.Options));
    }
    private static JsonObject CreateDefaultMarketplace()
    {
        return new JsonObject
        {
            ["name"] = "ai-links-local",
            ["interface"] = new JsonObject
            {
                ["displayName"] = "AI-Links Local"
            },
            ["plugins"] = new JsonArray()
        };
    }

    private static JsonObject CreateAnarchyPluginEntry()
    {
        return new JsonObject
        {
            ["name"] = "anarchy-ai",
            ["source"] = new JsonObject
            {
                ["source"] = "local",
                ["path"] = "./plugins/anarchy-ai"
            },
            ["policy"] = new JsonObject
            {
                ["installation"] = "INSTALLED_BY_DEFAULT",
                ["authentication"] = "ON_INSTALL"
            },
            ["category"] = "Productivity"
        };
    }

    private static MarketplaceInspection InspectMarketplace(string marketplacePath)
    {
        if (!File.Exists(marketplacePath))
        {
            return new MarketplaceInspection(false, false, false, false, false, []);
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(marketplacePath))?.AsObject();
            var pluginsArray = root?["plugins"] as JsonArray;
            if (pluginsArray is null)
            {
                return new MarketplaceInspection(true, false, false, false, true, []);
            }

            var existingEntry = pluginsArray
                .Select(node => node as JsonObject)
                .FirstOrDefault(node => string.Equals(node?["name"]?.GetValue<string>(), "anarchy-ai", StringComparison.Ordinal));

            var hasEntry = existingEntry is not null;
            var installedByDefault = string.Equals(
                existingEntry?["policy"]?["installation"]?.GetValue<string>(),
                "INSTALLED_BY_DEFAULT",
                StringComparison.Ordinal);

            return new MarketplaceInspection(true, true, hasEntry, installedByDefault, true, []);
        }
        catch (JsonException)
        {
            return new MarketplaceInspection(true, false, false, false, false, []);
        }
    }

    private static void RefreshFromUpdateSource(
        string pluginRoot,
        string repoRoot,
        SetupOptions options,
        HashSet<string> actionsTaken,
        HashSet<string> updateNotes)
    {
        using var tempRoot = new TempDirectory();
        var sourceRoot = ResolveUpdateSourceRoot(options, tempRoot.Path, updateNotes);
        var sourcePluginRoot = Path.Combine(sourceRoot, "plugins", "anarchy-ai");
        if (!Directory.Exists(sourcePluginRoot))
        {
            throw new DirectoryNotFoundException("Update source did not contain plugins\\anarchy-ai.");
        }

        foreach (var surface in PluginSurfaces)
        {
            CopySurface(Path.Combine(sourcePluginRoot, surface), Path.Combine(pluginRoot, surface));
        }

        actionsTaken.Add(string.IsNullOrWhiteSpace(options.UpdateSourcePath)
            ? "refreshed_plugin_bundle_from_public_repo"
            : "refreshed_plugin_bundle_from_local_update_source");

        if (options.RefreshPortableSchemaFamily)
        {
            foreach (var schemaFile in PortableSchemaFiles)
            {
                CopySurface(Path.Combine(sourceRoot, schemaFile), Path.Combine(repoRoot, schemaFile));
            }

            actionsTaken.Add(string.IsNullOrWhiteSpace(options.UpdateSourcePath)
                ? "refreshed_portable_schema_family_from_public_repo"
                : "refreshed_portable_schema_family_from_local_update_source");
        }
        else
        {
            updateNotes.Add("portable_schema_family_refresh_not_requested");
        }
    }

    private static string ResolveUpdateSourceRoot(SetupOptions options, string tempRoot, HashSet<string> updateNotes)
    {
        if (!string.IsNullOrWhiteSpace(options.UpdateSourcePath))
        {
            var resolvedPath = Path.GetFullPath(options.UpdateSourcePath);
            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                throw new DirectoryNotFoundException($"Update source path does not exist: {resolvedPath}");
            }

            updateNotes.Add("used_local_update_source_path");
            if (Directory.Exists(resolvedPath))
            {
                return resolvedPath;
            }

            var extractPath = Path.Combine(tempRoot, "local-extract");
            ZipFile.ExtractToDirectory(resolvedPath, extractPath, true);
            return ResolveFirstExtractedRoot(extractPath);
        }

        var zipPath = Path.Combine(tempRoot, "ai-links.zip");
        using var client = new HttpClient();
        var response = client.GetAsync(options.UpdateSourceZipUrl).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using (var responseStream = response.Content.ReadAsStream())
        using (var fileStream = File.Create(zipPath))
        {
            responseStream.CopyTo(fileStream);
        }

        var extractRoot = Path.Combine(tempRoot, "extract");
        ZipFile.ExtractToDirectory(zipPath, extractRoot, true);
        return ResolveFirstExtractedRoot(extractRoot);
    }

    private static string ResolveFirstExtractedRoot(string extractPath)
    {
        var firstDirectory = Directory.GetDirectories(extractPath).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstDirectory))
        {
            throw new InvalidOperationException("Update archive did not produce an extractable repository root.");
        }

        return firstDirectory;
    }

    private static void CopySurface(string sourcePath, string targetPath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.CreateDirectory(targetPath);
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourcePath, file);
                var destination = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }

            return;
        }

        if (File.Exists(sourcePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath, true);
            return;
        }

        throw new FileNotFoundException($"Missing source surface: {sourcePath}");
    }
}

internal sealed record MarketplaceInspection(
    bool Exists,
    bool HasPluginsArray,
    bool HasAnarchyPluginEntry,
    bool InstalledByDefault,
    bool IsValidJson,
    string[] Findings);

internal static class PayloadResources
{
    private const string PluginPrefix = "SetupPayload/plugins/anarchy-ai/";
    private const string PortableSchemaPrefix = "SetupPayload/portable-schema/";

    public static IEnumerable<string> GetPluginBundleResources()
    {
        return typeof(PayloadResources).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(PluginPrefix, StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal);
    }

    public static IEnumerable<string> GetPortableSchemaResources()
    {
        return typeof(PayloadResources).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(PortableSchemaPrefix, StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal);
    }

    public static Stream OpenResource(string resourceName)
    {
        return typeof(PayloadResources).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded setup payload resource: {resourceName}");
    }
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-ai-setup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
        catch
        {
        }
    }
}
