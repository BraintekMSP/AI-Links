using System.IO.Compression;
using System.Globalization;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AnarchyAi.Branding;
using AnarchyAi.Pathing;

namespace AnarchyAi.Setup;

// Purpose: Declares the bounded operations supported by the setup executable.
// Expected input: Selection from CLI switches or GUI button clicks.
// Expected output: A stable enum value used to route assess, install, or update behavior.
// Critical dependencies: CliParser, SetupForm, and SetupEngine branching logic.
internal enum OperationMode
{
    Assess,
    Install,
    Update,
    Status,
    Underlay,
    Refresh
}

// Purpose: Declares the two install lanes supported by the setup executable.
// Expected input: Selection from CLI switches or GUI lane controls.
// Expected output: A stable enum value for repo-local or user-profile installs.
// Critical dependencies: SetupEngine path resolution and marketplace registration rules.
internal enum InstallScope
{
    RepoLocal,
    UserProfile
}

// Purpose: Declares the host registration surfaces the installer can target in a single run.
// Expected input: Selection from CLI switches (/codex, /claudecode, /claudedesktop) or GUI host checkboxes.
// Expected output: A flags bitmask describing every host lane the current run should register with.
// Critical dependencies: SetupEngine host-lane dispatch and disclosure text construction.
[Flags]
internal enum HostTargets
{
    None = 0,
    Codex = 1 << 0,
    ClaudeCode = 1 << 1,
    ClaudeDesktop = 1 << 2
}

// Purpose: Renders HostTargets flag combinations into stable string labels for disclosure, JSON output, and help text.
// Expected input: A HostTargets bitmask value.
// Expected output: Canonical short labels ("codex", "claude_code", "claude_desktop") per selected flag.
// Critical dependencies: SetupResult JSON contract and disclosure text construction.
internal static class HostTargetLabels
{
    public const string CodexLabel = "codex";
    public const string ClaudeCodeLabel = "claude_code";
    public const string ClaudeDesktopLabel = "claude_desktop";

    public static string[] ToLabelArray(HostTargets targets)
    {
        var labels = new List<string>(3);
        if (targets.HasFlag(HostTargets.Codex)) { labels.Add(CodexLabel); }
        if (targets.HasFlag(HostTargets.ClaudeCode)) { labels.Add(ClaudeCodeLabel); }
        if (targets.HasFlag(HostTargets.ClaudeDesktop)) { labels.Add(ClaudeDesktopLabel); }
        return labels.ToArray();
    }

    public static string ToDisplayString(HostTargets targets)
    {
        var labels = ToLabelArray(targets);
        if (labels.Length == 0) { return "(none)"; }
        return string.Join(" + ", labels.Select(DisplayFor));
    }

    public static string DisplayFor(string label) => label switch
    {
        CodexLabel => "Codex",
        ClaudeCodeLabel => "Claude Code",
        ClaudeDesktopLabel => "Claude Desktop",
        _ => label
    };
}

// Purpose: Carries normalized setup inputs from the CLI or GUI into the execution engine.
// Expected input: Parsed mode, install lane, optional repo/source values, and output-mode flags.
// Expected output: An immutable options object consumed by SetupEngine.
// Critical dependencies: CliParser, SetupForm, and the current setup contract.
internal sealed class SetupOptions
{
    public OperationMode Mode { get; init; } = OperationMode.Assess;
    public InstallScope InstallScope { get; init; } = InstallScope.RepoLocal;
    public string HostContext { get; init; } = "codex";
    public HostTargets HostTargets { get; init; } = HostTargets.Codex;
    public bool Silent { get; init; }
    public bool JsonOutput { get; init; }
    public bool RefreshPortableSchemaFamily { get; init; }
    public bool ApplyChanges { get; init; }
    public bool RefreshSchemasAliasUsed { get; init; }
    public string UpdateSourceZipUrl { get; init; } = AnarchyBranding.DefaultUpdateSourceZipUrl;
    public string UpdateSourcePath { get; init; } = string.Empty;
    public string RepoPath { get; init; } = string.Empty;
}

// Purpose: Carries the bounded result of a setup assess, install, or update operation.
// Expected input: Execution-state facts collected by SetupEngine after filesystem and registration work completes.
// Expected output: A JSON-serializable result object for CLI, GUI, and recovery workflows.
// Critical dependencies: SetupEngine, PathRoleCollection, and the published JSON contract.
internal sealed class SetupResult
{
    public required string setup_operation { get; init; }
    public required string bootstrap_state { get; init; }
    public required string registration_mode { get; init; }
    public required string host_context { get; init; }
    public required string[] host_targets { get; init; }
    public required string install_scope { get; init; }
    public required bool update_requested { get; init; }
    public required string update_state { get; init; }
    public required string update_source_zip_url { get; init; }
    public required string[] update_notes { get; init; }
    public required bool runtime_present { get; init; }
    public required bool marketplace_registered { get; init; }
    public required bool installed_by_default { get; init; }
    public bool host_config_modified { get; init; }
    public bool refresh_plan_only { get; init; }
    public string[] refreshed_files { get; init; } = [];
    public string[] unchanged_files { get; init; } = [];
    public string[] backup_files { get; init; } = [];
    public string? selected_codex_primary_lane { get; init; }
    public string[] disabled_duplicate_codex_lanes { get; init; } = [];
    public bool duplicate_codex_skill_lanes_detected { get; init; }
    public bool source_authoring_bundle_present { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? source_authoring_bundle_state { get; init; }
    public required string[] actions_taken { get; init; }
    public required string[] missing_components { get; init; }
    public required string[] safe_repairs { get; init; }
    public required string next_action { get; init; }
    public required InstallStateReport install_state { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodexMaterializationReport? codex_materialization { get; init; }
    public required PathRoleCollection paths { get; init; }
}

// Purpose: Reports Codex-owned plugin-cache materialization separately from setup-owned install state.
// Expected input: Filesystem inspection of the selected marketplace/plugin cache root and installed source manifest.
// Expected output: A JSON-serializable host-cache report that distinguishes source visibility from active cache materialization.
// Critical dependencies: Codex plugin-cache path shape and setup's marketplace/plugin identity builders.
internal sealed class CodexMaterializationReport
{
    public required string marketplace_name { get; init; }
    public required string plugin_name { get; init; }
    public required string codex_config_path { get; init; }
    public required string config_plugin_key { get; init; }
    public bool? codex_plugin_enabled { get; init; }
    public required string expected_cache_root { get; init; }
    public required bool expected_cache_root_present { get; init; }
    public string? source_plugin_manifest_version { get; init; }
    public required string[] cache_entries { get; init; }
    public string? newest_cache_entry { get; init; }
    public required bool source_version_present_in_cache { get; init; }
    public required string[] findings { get; init; }
}

// Purpose: Reports whether setup can see a durable install-state record for the selected install lane.
// Expected input: Filesystem inspection of the state file written by setup install/update operations.
// Expected output: A JSON-serializable lifecycle report that agents can use before guessing from plugin UI state.
// Critical dependencies: SetupEngine install-state writer/reader and the versioned install-state JSON shape.
internal sealed class InstallStateReport
{
    public required string schema_version { get; init; }
    public required string state_path { get; init; }
    public required bool state_present { get; init; }
    public required bool state_written { get; init; }
    public required bool state_valid { get; init; }
    public required string[] findings { get; init; }
    public string[] warnings { get; init; } = [];
    public string? recorded_at_utc { get; init; }
    public string? recorded_install_scope { get; init; }
    public string? recorded_host_context { get; init; }
    public string[] recorded_host_targets { get; init; } = [];
    public string? recorded_target_id { get; init; }
    public string? recorded_target_kind { get; init; }
    public string? recorded_target_root { get; init; }
    public string? recorded_workspace_root { get; init; }
    public bool? recorded_workspace_schema_targeted { get; init; }
    public string? recorded_plugin_root { get; init; }
    public string? recorded_marketplace_path { get; init; }
    public string? recorded_install_state_path { get; init; }
    public string? recorded_runtime_path { get; init; }
    public string? recorded_mcp_server_name { get; init; }
    public int? recorded_managed_operation_count { get; init; }
}

// Purpose: Centralizes JSON serialization settings for setup output.
// Expected input: Anonymous objects or strongly typed setup result payloads.
// Expected output: Stable indented JSON with relaxed escaping for human-readable CLI output.
// Critical dependencies: System.Text.Json and every code path that prints setup JSON.
internal static class ProgramJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}

// Purpose: Hosts the setup executable entrypoint and routes execution into help, GUI, or CLI operation.
// Expected input: Raw process arguments from the setup executable invocation.
// Expected output: Process exit code plus optional JSON/help text written to stdout.
// Critical dependencies: CliParser, SetupForm, SetupEngine, ProgramJson, and Windows Forms initialization.
internal static class Program
{
    // Purpose: Starts the setup executable in help, GUI, or CLI mode based on incoming arguments.
    // Expected input: Raw command-line arguments from the current process.
    // Expected output: Exit code 0 for success/help, 1 for non-ready setup results, or 2 for unhandled failures.
    // Critical dependencies: CliParser, ConsoleWindow, Windows Forms, SetupEngine, and JSON serialization.
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
            return IsSuccessfulCliBootstrapState(result.bootstrap_state) ? 0 : 1;
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

    internal static bool IsSuccessfulCliBootstrapState(string bootstrapState)
    {
        return bootstrapState is "ready" or "source_authoring_bundle_ready" or "refresh_plan_ready";
    }
}

// Purpose: Hides the console window when the setup executable is launched in GUI mode.
// Expected input: No caller-supplied data beyond the current process window state.
// Expected output: No return value; the console is hidden when one exists.
// Critical dependencies: Win32 GetConsoleWindow and ShowWindow APIs.
internal static class ConsoleWindow
{
    private const int SwHide = 0;

    // Purpose: Retrieves the current process console window handle from Win32.
    // Expected input: None.
    // Expected output: A console window handle or IntPtr.Zero when none exists.
    // Critical dependencies: kernel32.dll.
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    // Purpose: Applies a requested show or hide state to a native window handle.
    // Expected input: A native window handle and a show-state constant.
    // Expected output: True when the requested window-state change succeeds.
    // Critical dependencies: user32.dll.
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Purpose: Hides the console window so the GUI setup flow does not leave a shell window behind.
    // Expected input: None; the method inspects the current process window state.
    // Expected output: No return value.
    // Critical dependencies: GetConsoleWindow and ShowWindow.
    public static void Hide()
    {
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SwHide);
        }
    }
}

// Purpose: Provides the Windows Forms front end for bounded underlay and user-profile install actions.
// Expected input: User-selected setup lane, optional repo path, and button-click actions.
// Expected output: GUI state updates plus serialized setup results displayed to the user.
// Critical dependencies: SetupEngine, ProgramJson, Windows Forms controls, and the shared path canon.
internal sealed class SetupForm : Form
{
    private readonly Label _introLabel;
    private readonly Label _pathLabel;
    private readonly TextBox _repoPathTextBox;
    private readonly Label _pathHelpLabel;
    private readonly TextBox _resultTextBox;
    private readonly Label _statusLabel;
    private Label _subtitleLabel = null!;
    private readonly RadioButton _repoUnderlayRadioButton;
    private readonly RadioButton _userProfileRadioButton;
    private readonly CheckBox _codexHostCheckBox;
    private readonly CheckBox _claudeCodeHostCheckBox;
    private readonly CheckBox _claudeDesktopHostCheckBox;
    private readonly Button _assessButton;
    private readonly Button _installButton;
    private readonly Button _browseButton;
    private readonly TableLayoutPanel _targetRepoPanel;
    private readonly TextBox _targetRepoTextBox;
    private readonly Button _targetRepoBrowseButton;
    private string _selectedRepoPath;
    private bool _updatingPathPresentation;

    // Purpose: Builds the setup form and initializes the lane controls, path fields, and action buttons.
    // Expected input: No direct caller input; uses current process resources and default install lane state.
    // Expected output: A ready-to-show setup form instance.
    // Critical dependencies: SetupWindowIcon, BuildHeaderPanel, BuildIntroLabel, and Windows Forms layout controls.
    public SetupForm()
    {
        Text = AnarchyBranding.SetupDisplayName;
        Width = 1200;
        Height = 720;
        MinimumSize = new System.Drawing.Size(1100, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new System.Drawing.Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = SetupWindowIcon.Create();

        var rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(14)
        };
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        rootPanel.Controls.Add(BuildHeaderPanel(), 0, 0);
        _introLabel = BuildIntroLabel(string.Empty);
        rootPanel.Controls.Add(_introLabel, 0, 1);

        var lanePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };
        lanePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        lanePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        var installLaneGroup = new GroupBox
        {
            Text = "Setup Lane",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12, 12, 12, 8),
            Margin = new Padding(0, 0, 12, 0)
        };
        var installLaneFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        _repoUnderlayRadioButton = new RadioButton
        {
            AutoSize = true,
            Text = "Repo underlay",
            Checked = true,
            Margin = new Padding(0, 2, 0, 4)
        };
        _repoUnderlayRadioButton.CheckedChanged += (_, _) => UpdateActionButtons();
        installLaneFlow.Controls.Add(_repoUnderlayRadioButton);

        _userProfileRadioButton = new RadioButton
        {
            AutoSize = true,
            Text = "User-profile install",
            Margin = new Padding(0, 2, 0, 0)
        };
        _userProfileRadioButton.CheckedChanged += (_, _) => UpdateActionButtons();
        installLaneFlow.Controls.Add(_userProfileRadioButton);
        installLaneGroup.Controls.Add(installLaneFlow);
        lanePanel.Controls.Add(installLaneGroup, 0, 0);

        var platformGroup = new GroupBox
        {
            Text = "Platform Payload",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12, 12, 12, 8),
            Margin = new Padding(0)
        };
        var platformFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        platformFlow.Controls.Add(new RadioButton
        {
            AutoSize = true,
            Text = "Windows (current payload)",
            Checked = true,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 4)
        });
        platformFlow.Controls.Add(new RadioButton
        {
            AutoSize = true,
            Text = "Linux (planned)",
            Checked = false,
            Enabled = false,
            Margin = new Padding(0, 2, 0, 4)
        });
        platformFlow.Controls.Add(new RadioButton
        {
            AutoSize = true,
            Text = "macOS (planned)",
            Checked = false,
            Enabled = false,
            Margin = new Padding(0, 2, 0, 0)
        });
        platformGroup.Controls.Add(platformFlow);
        lanePanel.Controls.Add(platformGroup, 1, 0);
        rootPanel.Controls.Add(lanePanel, 0, 2);

        var hostTargetGroup = new GroupBox
        {
            Text = "Host Target",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12, 12, 12, 8),
            Margin = new Padding(0, 12, 0, 0)
        };
        var hostTargetFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        _codexHostCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Codex",
            Checked = true,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 4)
        };
        _codexHostCheckBox.CheckedChanged += (_, _) => UpdatePathPresentation(GetSelectedInstallScope());
        hostTargetFlow.Controls.Add(_codexHostCheckBox);

        _claudeCodeHostCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Claude Code (user scope; unverified on this machine)",
            Checked = false,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 4)
        };
        _claudeCodeHostCheckBox.CheckedChanged += (_, _) => UpdatePathPresentation(GetSelectedInstallScope());
        hostTargetFlow.Controls.Add(_claudeCodeHostCheckBox);

        _claudeDesktopHostCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Claude Desktop (auto-detected install; unverified on this machine)",
            Checked = false,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 0)
        };
        _claudeDesktopHostCheckBox.CheckedChanged += (_, _) => UpdatePathPresentation(GetSelectedInstallScope());
        hostTargetFlow.Controls.Add(_claudeDesktopHostCheckBox);
        hostTargetGroup.Controls.Add(hostTargetFlow);
        rootPanel.Controls.Add(hostTargetGroup, 0, 3);

        var pathSectionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };
        pathSectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pathSectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pathSectionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            Margin = new Padding(0)
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _pathLabel = new Label
        {
            AutoSize = true,
            Text = "Repo Path:",
            Margin = new Padding(0, 10, 10, 0)
        };
        pathPanel.Controls.Add(_pathLabel, 0, 0);

        _selectedRepoPath = SetupEngine.TryResolveDefaultRepoRoot() ?? string.Empty;
        _repoPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _selectedRepoPath,
            Margin = new Padding(0, 6, 8, 0)
        };
        _repoPathTextBox.TextChanged += RepoPathTextBox_TextChanged;
        pathPanel.Controls.Add(_repoPathTextBox, 1, 0);

        _browseButton = new Button
        {
            Text = "Choose Repo...",
            AutoSize = false,
            Width = 165,
            Height = 34,
            Margin = new Padding(0, 4, 0, 0)
        };
        _browseButton.Click += BrowseButton_Click;
        pathPanel.Controls.Add(_browseButton, 2, 0);
        pathSectionPanel.Controls.Add(pathPanel, 0, 0);

        _pathHelpLabel = new Label
        {
            AutoSize = true,
            Text = string.Empty,
            MaximumSize = new System.Drawing.Size(1030, 0),
            ForeColor = System.Drawing.Color.FromArgb(90, 90, 90),
            Margin = new Padding(108, 2, 0, 0)
        };
        pathSectionPanel.Controls.Add(_pathHelpLabel, 0, 1);

        _targetRepoPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };
        _targetRepoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _targetRepoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _targetRepoPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _targetRepoPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Target Repo:",
            Margin = new Padding(0, 10, 10, 0)
        }, 0, 0);

        _targetRepoTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = _selectedRepoPath,
            Margin = new Padding(0, 6, 8, 0)
        };
        _targetRepoTextBox.TextChanged += TargetRepoTextBox_TextChanged;
        _targetRepoPanel.Controls.Add(_targetRepoTextBox, 1, 0);

        _targetRepoBrowseButton = new Button
        {
            Text = "Choose Repo...",
            AutoSize = false,
            Width = 165,
            Height = 34,
            Margin = new Padding(0, 4, 0, 0)
        };
        _targetRepoBrowseButton.Click += TargetRepoBrowseButton_Click;
        _targetRepoPanel.Controls.Add(_targetRepoBrowseButton, 2, 0);
        pathSectionPanel.Controls.Add(_targetRepoPanel, 0, 2);

        rootPanel.Controls.Add(pathSectionPanel, 0, 4);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };

        _assessButton = new Button { Text = "Assess", AutoSize = false, Width = 220, Height = 36 };
        _assessButton.Click += (_, _) => Execute(GetSecondaryActionMode());
        buttonPanel.Controls.Add(_assessButton);

        _installButton = new Button { Text = "Install", AutoSize = false, Width = 220, Height = 36 };
        _installButton.Click += (_, _) => Execute(GetPrimaryActionMode());
        buttonPanel.Controls.Add(_installButton);

        var closeButton = new Button { Text = "Close", AutoSize = false, Width = 110, Height = 36 };
        closeButton.Click += (_, _) => Close();
        buttonPanel.Controls.Add(closeButton);

        _statusLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(18, 10, 0, 0),
            Text = "Ready."
        };
        buttonPanel.Controls.Add(_statusLabel);
        rootPanel.Controls.Add(buttonPanel, 0, 5);

        _resultTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            WordWrap = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new System.Drawing.Font("Consolas", 10.0f),
            Margin = new Padding(0, 12, 0, 0)
        };
        rootPanel.Controls.Add(_resultTextBox, 0, 6);

        Controls.Add(rootPanel);
        UpdateActionButtons();
    }

    // Purpose: Builds the branded header section shown at the top of the setup window.
    // Expected input: Current embedded branding assets and form-level subtitle label storage.
    // Expected output: A populated header control ready to add to the form layout.
    // Critical dependencies: BuildLogoPictureBox and Windows Forms layout controls.
    private Control BuildHeaderPanel()
    {
        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var logo = BuildLogoPictureBox();
        if (logo is not null)
        {
            headerPanel.Controls.Add(logo, 0, 0);
        }

        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(16, 14, 0, 0)
        };
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = AnarchyBranding.SetupDisplayName,
            Font = new System.Drawing.Font("Segoe UI Semibold", 21.0f),
            Margin = new Padding(0)
        };
        titlePanel.Controls.Add(titleLabel, 0, 0);

        _subtitleLabel = new Label
        {
            AutoSize = true,
            Text = "Harness install and assessment",
            Font = new System.Drawing.Font("Segoe UI", 11.0f),
            ForeColor = System.Drawing.Color.FromArgb(80, 80, 80),
            Margin = new Padding(2, 4, 0, 0)
        };
        titlePanel.Controls.Add(_subtitleLabel, 0, 1);

        headerPanel.Controls.Add(titlePanel, 1, 0);

        return headerPanel;
    }

    // Purpose: Loads the embedded Anarchy-AI logo image and wraps it in a picture box for the setup header.
    // Expected input: No caller input; resolves the bundle asset through the embedded payload resource path.
    // Expected output: A picture box when the image is available, otherwise null.
    // Critical dependencies: ResourceImageLoader and the embedded plugin payload resources.
    private static PictureBox? BuildLogoPictureBox()
    {
        var image = ResourceImageLoader.TryLoadPng(
            AnarchyPathCanon.BuildPluginPayloadResourcePath(
                AnarchyBranding.BundleSetupHeaderImageRelativePath));
        if (image is null)
        {
            return null;
        }

        return new PictureBox
        {
            Image = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 96,
            Height = 96,
            Margin = new Padding(0, 4, 12, 0)
        };
    }

    // Purpose: Creates a standard intro label used for high-level explanatory copy in the setup window.
    // Expected input: Introductory text for the current lane or state.
    // Expected output: A configured label control.
    // Critical dependencies: Windows Forms label rendering.
    private static Label BuildIntroLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            MaximumSize = new System.Drawing.Size(900, 0),
            Margin = new Padding(0, 8, 0, 0),
            Font = new System.Drawing.Font("Segoe UI", 12.0f)
        };
    }

    // Purpose: Opens repo selection only for the repo-local lane.
    // Expected input: Standard WinForms click arguments plus the current repo-path textbox value.
    // Expected output: No direct return value; updates the repo textbox when the user picks a folder.
    // Critical dependencies: GetSelectedInstallScope and BrowseForRepoSelection.
    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return;
        }

        BrowseForRepoSelection(_repoPathTextBox.Text, path => _repoPathTextBox.Text = path);
    }

    // Purpose: Opens the secondary target-repo selector when that lane is shown.
    // Expected input: Standard WinForms click arguments plus the target textbox value.
    // Expected output: No direct return value; updates the target repo textbox after selection.
    // Critical dependencies: BrowseForRepoSelection.
    private void TargetRepoBrowseButton_Click(object? sender, EventArgs e)
    {
        BrowseForRepoSelection(_targetRepoTextBox.Text, path => _targetRepoTextBox.Text = path);
    }

    // Purpose: Shows a folder picker and writes the selected repo path back through a caller-provided callback.
    // Expected input: Initial folder path and a callback that accepts the chosen repo path.
    // Expected output: No direct return value; invokes the callback only when the user confirms a selection.
    // Critical dependencies: FolderBrowserDialog and caller-managed textbox state.
    private void BrowseForRepoSelection(string initialPath, Action<string> onSelected)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the target repo root"
        };

        if (Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            onSelected(dialog.SelectedPath);
        }
    }

    // Purpose: Keeps the repo-local selected path in sync with manual textbox edits.
    // Expected input: Standard text-changed event arguments and the current textbox value.
    // Expected output: No direct return value; updates the cached selected repo path.
    // Critical dependencies: Current setup-lane selection and path-presentation update guards.
    private void RepoPathTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_updatingPathPresentation || GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return;
        }

        _selectedRepoPath = _repoPathTextBox.Text;
    }

    // Purpose: Keeps the secondary target repo path in sync with manual edits when that control is visible.
    // Expected input: Standard text-changed event arguments and the current textbox value.
    // Expected output: No direct return value; updates the cached selected repo path.
    // Critical dependencies: Path-presentation update guards.
    private void TargetRepoTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_updatingPathPresentation)
        {
            return;
        }

        _selectedRepoPath = _targetRepoTextBox.Text;
    }

    // Purpose: Runs a GUI action and displays the resulting JSON state.
    // Expected input: The requested operation mode plus the current lane and repo-path selections.
    // Expected output: No direct return value; updates status text and the result textbox.
    // Critical dependencies: SetupEngine, InstallDisclosureForm, and ProgramJson serialization.
    private void Execute(OperationMode mode)
    {
        try
        {
            var installScope = GetSelectedInstallScope();
            var effectiveRepoPath = GetEffectiveRepoPath();
            var selectedHostTargets = GetSelectedHostTargets();
            if (mode is OperationMode.Install or OperationMode.Underlay)
            {
                var disclosureText = mode == OperationMode.Underlay
                    ? SetupEngine.BuildUnderlayDisclosure(effectiveRepoPath)
                    : SetupEngine.BuildInstallDisclosure(effectiveRepoPath, installScope, selectedHostTargets);
                var laneLabel = mode == OperationMode.Underlay
                    ? "Repo underlay"
                    : "User-profile install";
                var hostTargetsLabel = mode == OperationMode.Underlay
                    ? "(none; underlay does not register a host runtime)"
                    : HostTargetLabels.ToDisplayString(selectedHostTargets);
                var continueButtonText = mode == OperationMode.Underlay
                    ? "Apply Underlay"
                    : "Continue Install";
                var displayRepoPath = string.IsNullOrWhiteSpace(effectiveRepoPath)
                    ? "(not specified)"
                    : effectiveRepoPath;
                var disclosure = new InstallDisclosureForm(
                    displayRepoPath,
                    disclosureText,
                    laneLabel,
                    hostTargetsLabel,
                    continueButtonText);

                if (disclosure.ShowDialog(this) != DialogResult.OK)
                {
                    _statusLabel.Text = mode == OperationMode.Underlay
                        ? "Underlay cancelled."
                        : "Install cancelled.";
                    return;
                }
            }

            _statusLabel.Text = $"{mode} in progress...";
            var result = new SetupEngine().Execute(BuildGuiSetupOptions(
                mode,
                installScope,
                effectiveRepoPath,
                selectedHostTargets));

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

    // Purpose: Maps GUI button semantics onto the same setup options users would expect from CLI lanes.
    // Expected input: The selected GUI operation, lane, repo path, and host targets.
    // Expected output: Setup options with repo-underlay planning/apply mapped to explicit schema refresh flags.
    // Critical dependencies: GetSecondaryActionMode/GetPrimaryActionMode button labels and /underlay /refresh /apply contract.
    internal static SetupOptions BuildGuiSetupOptions(
        OperationMode mode,
        InstallScope installScope,
        string effectiveRepoPath,
        HostTargets selectedHostTargets)
    {
        var isRepoUnderlayLane = installScope == InstallScope.RepoLocal;
        var refreshPortableSchemaFamily = isRepoUnderlayLane && (mode is OperationMode.Refresh or OperationMode.Underlay);

        return new SetupOptions
        {
            Mode = mode,
            InstallScope = installScope,
            HostContext = "codex",
            HostTargets = selectedHostTargets,
            RepoPath = effectiveRepoPath,
            RefreshPortableSchemaFamily = refreshPortableSchemaFamily,
            ApplyChanges = isRepoUnderlayLane && mode == OperationMode.Underlay
        };
    }

    // Purpose: Resolves the install scope selected by the radio-button group.
    // Expected input: Current radio-button state.
    // Expected output: RepoLocal or UserProfile.
    // Critical dependencies: The setup form radio controls.
    private InstallScope GetSelectedInstallScope()
    {
        return _userProfileRadioButton.Checked ? InstallScope.UserProfile : InstallScope.RepoLocal;
    }

    // Purpose: Resolves the secondary/read-only GUI action for the selected lane.
    // Expected input: Current radio-button state.
    // Expected output: Refresh planning for repo underlay, or runtime assess for user-profile installs.
    // Critical dependencies: SetupEngine operation modes and the GUI lane contract.
    private OperationMode GetSecondaryActionMode()
    {
        return _userProfileRadioButton.Checked ? OperationMode.Assess : OperationMode.Refresh;
    }

    // Purpose: Resolves the primary/write GUI action for the selected lane.
    // Expected input: Current radio-button state.
    // Expected output: Underlay materialization for repo underlay, or runtime install for user-profile installs.
    // Critical dependencies: SetupEngine operation modes and the GUI lane contract.
    private OperationMode GetPrimaryActionMode()
    {
        return _userProfileRadioButton.Checked ? OperationMode.Install : OperationMode.Underlay;
    }

    // Purpose: Resolves the selected host targets from the host-target checkbox group.
    // Expected input: Current checkbox state of Codex, Claude Code, and Claude Desktop host toggles.
    // Expected output: A HostTargets bitmask describing every host lane the current GUI run should register.
    // Critical dependencies: The host-target checkbox controls and the default-to-Codex fallback when all are unchecked.
    private HostTargets GetSelectedHostTargets()
    {
        if (!_userProfileRadioButton.Checked)
        {
            return HostTargets.None;
        }

        var targets = HostTargets.None;
        if (_codexHostCheckBox.Checked) { targets |= HostTargets.Codex; }
        if (_claudeCodeHostCheckBox.Checked) { targets |= HostTargets.ClaudeCode; }
        if (_claudeDesktopHostCheckBox.Checked) { targets |= HostTargets.ClaudeDesktop; }
        return targets == HostTargets.None ? HostTargets.Codex : targets;
    }

    // Purpose: Refreshes action-button labels and lane-specific explanatory UI when the selected install lane changes.
    // Expected input: Current lane selection in the radio buttons.
    // Expected output: No direct return value; mutates button text and supporting UI copy.
    // Critical dependencies: GetSelectedInstallScope, UpdateHeaderCopy, and UpdatePathPresentation.
    private void UpdateActionButtons()
    {
        var selectedScope = GetSelectedInstallScope();
        _assessButton.Text = selectedScope == InstallScope.UserProfile
            ? "Assess User-Profile"
            : "Plan Repo Refresh";
        _installButton.Text = selectedScope == InstallScope.UserProfile
            ? "Install User-Profile"
            : "Apply Repo Underlay";
        UpdateHostTargetPresentation(selectedScope);
        UpdateHeaderCopy(selectedScope);
        UpdatePathPresentation(selectedScope);
    }

    // Purpose: Updates the subtitle and introductory copy to match the active install lane.
    // Expected input: The selected install scope.
    // Expected output: No direct return value; updates form labels.
    // Critical dependencies: The current wording contract for repo-local versus user-profile installs.
    private void UpdateHeaderCopy(InstallScope installScope)
    {
        if (installScope == InstallScope.UserProfile)
        {
            _subtitleLabel.Text = "User-profile runtime install and assessment";
            _introLabel.Text = $"Install or assess {AnarchyBranding.BrandDisplayName} through the current user profile. User-profile install is the normal runtime lane and avoids creating one Codex plugin distribution per repo.";
            return;
        }

        _subtitleLabel.Text = "Repo underlay setup";
        _introLabel.Text = $"Seed the portable {AnarchyBranding.BrandDisplayName} underlay into a selected repo. Repo underlay carries schema, narrative, and hygiene surfaces without installing a runtime, creating a marketplace entry, or touching host config.";
    }

    // Purpose: Disables host-target controls when the selected lane does not register any host runtime.
    // Expected input: The selected setup lane.
    // Expected output: Host controls enabled only for user-profile runtime installation.
    // Critical dependencies: GetSelectedHostTargets and the repo-underlay runtime-free contract.
    private void UpdateHostTargetPresentation(InstallScope installScope)
    {
        var hostTargetsEnabled = installScope == InstallScope.UserProfile;
        _codexHostCheckBox.Enabled = hostTargetsEnabled;
        _claudeCodeHostCheckBox.Enabled = hostTargetsEnabled;
        _claudeDesktopHostCheckBox.Enabled = hostTargetsEnabled;
    }

    // Purpose: Switches the path UI between editable repo-local selection and fixed user-profile presentation.
    // Expected input: The selected install scope.
    // Expected output: No direct return value; updates path labels, textbox state, and browse-button visibility.
    // Critical dependencies: AnarchyPathCanon, GeneratedAnarchyPathCanon, and the current Windows Forms controls.
    private void UpdatePathPresentation(InstallScope installScope)
    {
        _updatingPathPresentation = true;
        try
        {
            if (installScope == InstallScope.UserProfile)
            {
                _pathLabel.Text = "Runtime Payload:";
                _repoPathTextBox.Text = AnarchyPathCanon.ResolveUserProfilePluginRoot(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    GeneratedAnarchyPathCanon.DefaultPluginName);
                _pathHelpLabel.Text = BuildUserProfilePathHelp(GetSelectedHostTargets());
                _repoPathTextBox.ReadOnly = true;
                _repoPathTextBox.BackColor = System.Drawing.SystemColors.Control;
                _browseButton.Visible = false;
                _browseButton.Enabled = false;
                _targetRepoPanel.Visible = false;
                return;
            }

            _pathLabel.Text = "Repo Path:";
            _repoPathTextBox.Text = _selectedRepoPath;
            _pathHelpLabel.Text = "Repo underlay writes portable schema, narrative, and hygiene surfaces only; it does not install a runtime or modify host config.";
            _repoPathTextBox.ReadOnly = false;
            _repoPathTextBox.BackColor = System.Drawing.SystemColors.Window;
            _browseButton.Visible = true;
            _browseButton.Enabled = true;
            _targetRepoPanel.Visible = false;
        }
        finally
        {
            _updatingPathPresentation = false;
        }
    }

    // Purpose: Returns the repo path the current GUI action should target.
    // Expected input: The current install lane and cached repo selection.
    // Expected output: Empty string for user-profile operations or the trimmed repo path for repo-local operations.
    // Critical dependencies: GetSelectedInstallScope and the cached _selectedRepoPath value.
    private string GetEffectiveRepoPath()
    {
        if (GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return string.Empty;
        }

        return _selectedRepoPath.Trim();
    }

    // Purpose: Explains that user-profile host targets share one runtime payload but write different host integration surfaces.
    // Expected input: Current host-target checkbox selection.
    // Expected output: A concise UI line naming selected host config targets without changing the shared payload path.
    // Critical dependencies: HostTargets labels and user-profile install lane semantics.
    private static string BuildUserProfilePathHelp(HostTargets selectedHostTargets)
    {
        var targets = new List<string>();
        if (selectedHostTargets.HasFlag(HostTargets.Codex))
        {
            targets.Add("Codex marketplace/config");
        }

        if (selectedHostTargets.HasFlag(HostTargets.ClaudeCode))
        {
            targets.Add("Claude Code ~/.claude.json");
        }

        if (selectedHostTargets.HasFlag(HostTargets.ClaudeDesktop))
        {
            targets.Add("Claude Desktop detected config");
        }

        return $"Shared runtime payload used by selected hosts. Host targets: {string.Join("; ", targets)}.";
    }
}

// Purpose: Presents the setup disclosure that the user must review before a GUI write action proceeds.
// Expected input: Repo path context, generated disclosure text, and display labels for the chosen setup action.
// Expected output: A modal dialog with continue/back actions.
// Critical dependencies: SetupEngine.BuildInstallDisclosure, SetupEngine.BuildUnderlayDisclosure, and Windows Forms controls.
internal sealed class InstallDisclosureForm : Form
{
    // Purpose: Builds the modal disclosure dialog for a pending setup write action.
    // Expected input: Repo-path context, disclosure text, lane label, host-target label, and continue-button text.
    // Expected output: A ready-to-show modal form instance.
    // Critical dependencies: Generated disclosure text, SetupWindowIcon, and Windows Forms layout controls.
    public InstallDisclosureForm(string repoPath, string disclosureText, string laneLabel, string hostTargetsLabel, string continueButtonText)
    {
        Text = "Setup Disclosure";
        Width = 920;
        Height = 660;
        MinimumSize = new System.Drawing.Size(860, 620);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = new System.Drawing.Font("Segoe UI", 10);
        Icon = SetupWindowIcon.Create();

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

        rootPanel.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = System.Drawing.SystemColors.Control,
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Review expected repo, user, and agent impact before continuing.",
            Font = new System.Drawing.Font("Segoe UI Semibold", 11.0f),
            TabStop = false
        }, 0, 0);

        rootPanel.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = System.Drawing.SystemColors.Control,
            Dock = DockStyle.Top,
            Height = 74,
            Text = $"Target repo:{Environment.NewLine}{repoPath}{Environment.NewLine}Setup lane:{Environment.NewLine}{laneLabel}{Environment.NewLine}Host targets:{Environment.NewLine}{hostTargetsLabel}",
            Margin = new Padding(0, 6, 0, 10),
            TabStop = false
        }, 0, 1);

        var disclosureBox = new RichTextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Text = disclosureText,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = System.Drawing.SystemColors.Window,
            Font = new System.Drawing.Font("Consolas", 10.0f),
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0, 0, 0, 10)
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
            Text = continueButtonText,
            AutoSize = false,
            Width = 140,
            Height = 36,
            DialogResult = DialogResult.OK
        };
        buttonPanel.Controls.Add(continueButton);

        var backButton = new Button
        {
            Text = "Back",
            AutoSize = false,
            Width = 100,
            Height = 36,
            DialogResult = DialogResult.Cancel
        };
        buttonPanel.Controls.Add(backButton);

        rootPanel.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = continueButton;
        CancelButton = backButton;
        Controls.Add(rootPanel);
    }
}

// Purpose: Resolves the window icon used by the setup GUI.
// Expected input: Embedded icon resources carried by the published payload.
// Expected output: An icon instance when one can be loaded, otherwise null.
// Critical dependencies: ResourceIconLoader and the executable-associated icon fallback.
internal static class SetupWindowIcon
{
    // Purpose: Creates the best available icon for the setup window.
    // Expected input: No direct caller input; inspects embedded resources and the executable icon.
    // Expected output: The embedded Anarchy-AI icon when present, otherwise an extracted executable icon, otherwise null.
    // Critical dependencies: ResourceIconLoader, AnarchyPathCanon, and SafeExtractExecutableIcon.
    public static System.Drawing.Icon? Create()
    {
        return ResourceIconLoader.TryLoadIcon(
                AnarchyPathCanon.BuildPluginPayloadResourcePath(
                    AnarchyBranding.BundleSetupIconRelativePath))
            ?? SafeExtractExecutableIcon();
    }

    // Purpose: Falls back to the executable-associated icon when the embedded icon cannot be loaded.
    // Expected input: No direct caller input.
    // Expected output: An associated executable icon or null if extraction fails.
    // Critical dependencies: System.Drawing.Icon and Application.ExecutablePath.
    private static System.Drawing.Icon? SafeExtractExecutableIcon()
    {
        try
        {
            return System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return null;
        }
    }
}

// Purpose: Loads embedded bitmap assets from the setup payload.
// Expected input: Logical resource suffixes that identify bundle-relative image files.
// Expected output: Decoded images ready for Windows Forms consumption or null when absent.
// Critical dependencies: PayloadResources and System.Drawing image decoding.
internal static class ResourceImageLoader
{
    // Purpose: Loads an embedded PNG asset from the plugin payload.
    // Expected input: Bundle-relative logical suffix for the PNG resource.
    // Expected output: A detached bitmap instance or null when the resource is missing.
    // Critical dependencies: TryOpenResourceStream and System.Drawing.Image.
    public static System.Drawing.Image? TryLoadPng(string logicalSuffix)
    {
        using var stream = TryOpenResourceStream(logicalSuffix);
        if (stream is null)
        {
            return null;
        }

        using var image = System.Drawing.Image.FromStream(stream);
        return new System.Drawing.Bitmap(image);
    }

    // Purpose: Resolves the manifest-resource stream for a bundle-relative image asset.
    // Expected input: Bundle-relative logical suffix for the desired resource.
    // Expected output: A readable resource stream or null when no matching resource exists.
    // Critical dependencies: PayloadResources.GetPluginBundleResources and PayloadResources.OpenResource.
    private static Stream? TryOpenResourceStream(string logicalSuffix)
    {
        var normalizedSuffix = logicalSuffix.Replace('\\', '/');
        var resourceName = PayloadResources.GetPluginBundleResources()
            .FirstOrDefault(name =>
                name.Replace('\\', '/').EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase));

        return resourceName is null ? null : PayloadResources.OpenResource(resourceName);
    }
}

// Purpose: Loads embedded icon assets from the setup payload.
// Expected input: Logical resource suffixes that identify bundle-relative icon files.
// Expected output: Decoded icon instances or null when absent.
// Critical dependencies: PayloadResources and System.Drawing.Icon.
internal static class ResourceIconLoader
{
    // Purpose: Loads an embedded icon asset from the plugin payload.
    // Expected input: Bundle-relative logical suffix for the icon resource.
    // Expected output: An icon instance or null when the resource is missing.
    // Critical dependencies: TryOpenResourceStream and System.Drawing.Icon.
    public static System.Drawing.Icon? TryLoadIcon(string logicalSuffix)
    {
        using var stream = TryOpenResourceStream(logicalSuffix);
        return stream is null ? null : new System.Drawing.Icon(stream);
    }

    // Purpose: Resolves the manifest-resource stream for a bundle-relative icon asset.
    // Expected input: Bundle-relative logical suffix for the desired resource.
    // Expected output: A readable resource stream or null when no matching resource exists.
    // Critical dependencies: PayloadResources.GetPluginBundleResources and PayloadResources.OpenResource.
    private static Stream? TryOpenResourceStream(string logicalSuffix)
    {
        var normalizedSuffix = logicalSuffix.Replace('\\', '/');
        var resourceName = PayloadResources.GetPluginBundleResources()
            .FirstOrDefault(name =>
                name.Replace('\\', '/').EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase));

        return resourceName is null ? null : PayloadResources.OpenResource(resourceName);
    }
}

// Purpose: Parses CLI switches into a bounded setup-options object.
// Expected input: Raw command-line arguments from the setup executable.
// Expected output: Normalized flags, lane selections, and optional repo/source values.
// Critical dependencies: switch-normalization helpers and the published CLI contract.
internal static class CliParser
{
    // Purpose: Detects whether the incoming CLI arguments request help text.
    // Expected input: Raw command-line arguments.
    // Expected output: True when any supported help alias is present.
    // Critical dependencies: IsHelpAlias.
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

    // Purpose: Extracts the repo-path argument without fully parsing the command line.
    // Expected input: Raw command-line arguments.
    // Expected output: The repo path following /repo when present; otherwise null.
    // Critical dependencies: NormalizeSwitch and the current /repo switch contract.
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

    // Purpose: Parses all supported CLI switches into a setup-options object.
    // Expected input: Raw command-line arguments.
    // Expected output: A populated SetupOptions instance or an ArgumentException for unsupported or malformed switches.
    // Critical dependencies: NormalizeSwitch, ReadValue, and the current CLI switch vocabulary.
    public static SetupOptions Parse(string[] args)
    {
        var mode = OperationMode.Assess;
        var installScope = InstallScope.RepoLocal;
        var hostContext = "codex";
        var hostTargets = HostTargets.None;
        var silent = false;
        var jsonOutput = false;
        var refreshPortableSchemaFamily = false;
        var applyChanges = false;
        var refreshSwitchSeen = false;
        var refreshSchemasAliasUsed = false;
        var updateSourceZipUrl = AnarchyBranding.DefaultUpdateSourceZipUrl;
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
                case "underlay":
                    mode = OperationMode.Underlay;
                    installScope = InstallScope.RepoLocal;
                    break;
                case "refresh":
                    refreshSwitchSeen = true;
                    refreshPortableSchemaFamily = true;
                    if (mode == OperationMode.Assess)
                    {
                        mode = OperationMode.Refresh;
                    }
                    break;
                case "repolocal":
                    installScope = InstallScope.RepoLocal;
                    break;
                case "userprofile":
                    installScope = InstallScope.UserProfile;
                    break;
                case "update":
                    mode = OperationMode.Update;
                    break;
                case "status":
                case "doctor":
                case "selfcheck":
                case "self-check":
                    mode = OperationMode.Status;
                    break;
                case "silent":
                    silent = true;
                    break;
                case "json":
                    jsonOutput = true;
                    break;
                case "apply":
                    applyChanges = true;
                    break;
                case "refreshschemas":
                    refreshSchemasAliasUsed = true;
                    refreshPortableSchemaFamily = true;
                    if (mode == OperationMode.Assess)
                    {
                        mode = OperationMode.Refresh;
                    }
                    break;
                case "repo":
                    repoPath = ReadValue(args, ref i, normalized);
                    break;
                case "host":
                    hostContext = ReadValue(args, ref i, normalized);
                    break;
                case "codex":
                    hostTargets |= HostTargets.Codex;
                    break;
                case "claudecode":
                    hostTargets |= HostTargets.ClaudeCode;
                    break;
                case "claudedesktop":
                    hostTargets |= HostTargets.ClaudeDesktop;
                    break;
                case "allhosts":
                    hostTargets |= HostTargets.Codex | HostTargets.ClaudeCode | HostTargets.ClaudeDesktop;
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

        // Default to Codex when no host flag supplied, preserving prior single-host behavior.
        if (hostTargets == HostTargets.None)
        {
            hostTargets = HostTargets.Codex;
        }

        if (mode == OperationMode.Underlay && installScope != InstallScope.RepoLocal)
        {
            throw new ArgumentException("/underlay is repo-scoped and cannot be combined with /userprofile.");
        }

        if (refreshSwitchSeen && mode is OperationMode.Install or OperationMode.Update)
        {
            throw new ArgumentException("/refresh is a standalone or /underlay operation. Use deprecated /refreshschemas [/apply] for install/update compatibility.");
        }

        if (applyChanges && !refreshPortableSchemaFamily)
        {
            throw new ArgumentException("/apply requires /refresh or /refreshschemas.");
        }

        return new SetupOptions
        {
            Mode = mode,
            InstallScope = installScope,
            HostContext = hostContext,
            HostTargets = hostTargets,
            Silent = silent,
            JsonOutput = jsonOutput,
            RefreshPortableSchemaFamily = refreshPortableSchemaFamily,
            ApplyChanges = applyChanges,
            RefreshSchemasAliasUsed = refreshSchemasAliasUsed,
            UpdateSourceZipUrl = updateSourceZipUrl,
            UpdateSourcePath = updateSourcePath,
            RepoPath = repoPath
        };
    }

    // Purpose: Determines whether a single argument is one of the supported help aliases.
    // Expected input: One raw argument token.
    // Expected output: True when the token maps to ?, h, or help.
    // Critical dependencies: NormalizeSwitch.
    private static bool IsHelpAlias(string arg)
    {
        var normalized = NormalizeSwitch(arg);
        return normalized is "?" or "h" or "help";
    }

    // Purpose: Normalizes a switch token by trimming CLI prefixes and lowercasing it.
    // Expected input: A nonblank raw switch argument.
    // Expected output: A normalized switch name without leading slash or dash characters.
    // Critical dependencies: The setup CLI contract and callers that use normalized switch names in comparisons.
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

    // Purpose: Reads the value that follows a value-bearing CLI switch.
    // Expected input: Full argument array, current switch index, and the normalized switch name for error reporting.
    // Expected output: The argument immediately following the current switch.
    // Critical dependencies: The CLI contract requiring a value after /repo, /host, /sourcepath, and /sourceurl.
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
// Purpose: Executes setup assess, install, and update flows using the embedded payload and the shared path canon.
// Expected input: Normalized setup options, current filesystem state, and published payload resources.
// Expected output: A SetupResult describing readiness, repairs, actions, and nested path facts.
// Critical dependencies: PayloadResources, AnarchyPathCanon, JSON manifests, local filesystem access, and current host-marketplace conventions.
internal sealed class SetupEngine
{
    private static readonly string CodexCustomMcpServerBlockPattern = BuildOwnedCodexCustomMcpServerBlockPattern();

    private static readonly string[] CoreContractFiles =
    [
        "active-work-state.contract.json",
        "schema-reality.contract.json",
        "gov2gov-migration.contract.json",
        "preflight-session.contract.json",
        "harness-gap-state.contract.json"
    ];

    private const string ExperimentalDirectionAssistContract = "direction-assist-test.contract.json";

    private static readonly string[] NarrativeTemplateFiles =
    [
        AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath,
        AnarchyPathCanon.BundleNarrativeRecordTemplateFileRelativePath
    ];

    private const string AgentsAwarenessNoteTemplateRelativePath = "templates/AGENTS.md.awareness-note.template";
    private const string ConsumerNarrativeRegisterRelativePath = ".agents/anarchy-ai/narratives/register.json";
    private const string ConsumerNarrativeProjectsDirectoryRelativePath = ".agents/anarchy-ai/narratives/projects";
    private const string ConsumerDirectionAssistTestRegisterRelativePath = ".agents/anarchy-ai/direction-assist-test.jsonl";

    private static IReadOnlyList<string> PortableSchemaFiles => AnarchyPathCanon.PortableSchemaFiles;

    private static IReadOnlyList<string> PluginSurfaces => AnarchyPathCanon.PluginSurfaces;

    private static readonly string[] CoreToolNames =
    [
        "preflight_session",
        "compile_active_work_state",
        "is_schema_real_or_shadow_copied",
        "assess_harness_gap_state",
        "run_gov2gov_migration"
    ];

    private static readonly string[] ExperimentalToolNames =
    [
        "direction_assist_test"
    ];

    private const string InstallStateSchemaVersion = "anarchy.install-state.v2";
    private const string InstallStateFileRelativePath = ".anarchy-ai/install-state.json";

    // Purpose: Guesses a default repo root for CLI help and repo-local operations when /repo is omitted.
    // Expected input: Current executable location and current working directory.
    // Expected output: The detected repo root or null when no trustworthy repo marker is found.
    // Critical dependencies: TryResolveRepoFromPluginsDirectory and LooksLikeRepoRoot.
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

    // Purpose: Builds the plain-text install disclosure shown before a GUI install continues.
    // Expected input: Optional repo path, selected install lane, and selected host targets.
    // Expected output: A bounded disclosure string describing destination paths, tool count, and actor impact.
    // Critical dependencies: BuildInstallRootLabel, BuildPluginFolderLabel, BuildMarketplacePathLabel, and the current install contract.
    public static string BuildInstallDisclosure(string repoPath, InstallScope installScope, HostTargets hostTargets)
    {
        var hasWorkspaceTarget = !string.IsNullOrWhiteSpace(repoPath);
        var targetRepo = string.IsNullOrWhiteSpace(repoPath)
            ? "(not specified)"
            : repoPath;
        var installRoot = BuildInstallRootLabel(installScope);
        var pluginFolder = hasWorkspaceTarget
            ? BuildPluginFolderLabel(installScope, repoPath)
            : BuildPluginFolderLabel(installScope, null);
        var pluginName = hasWorkspaceTarget
            ? BuildPluginName(installScope, repoPath)
            : BuildPluginName(installScope, null);
        var marketplacePath = BuildMarketplacePathLabel(installScope, repoPath);
        var codexSelected = hostTargets.HasFlag(HostTargets.Codex);
        var disclosureLines = new List<string>
        {
            $"Responsible disclosure for {BuildInstallScopeLabel(installScope).ToLowerInvariant()} {AnarchyBranding.BrandDisplayName} install.",
            "All carried schema, contract, and install surfaces remain authored in this repo and are published into the standalone installer payload.",
            $"Install root: {installRoot}",
            installScope == InstallScope.UserProfile
                ? $"Workspace target: {targetRepo}"
                : $"Target repo: {targetRepo}",
            "Install impact:",
            installScope == InstallScope.UserProfile
                ? $"- Adds shared runtime payload {pluginFolder}\\ with {PluginSurfaces.Count} bundled surfaces."
                : $"- Adds {pluginFolder}\\ with {PluginSurfaces.Count} bundled surfaces.",
            "- Writes a versioned install-state record inside the owned plugin bundle for later status/doctor inspection.",
            "- Current GUI install does not rewrite AGENTS.md."
        };

        if (codexSelected)
        {
            disclosureLines.Add($"- Creates or updates {marketplacePath}.");
            disclosureLines.Add(installScope == InstallScope.UserProfile
                ? $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the current user profile marketplace."
                : $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the target repo.");
            disclosureLines.Add(installScope == InstallScope.UserProfile
                ? $"- Uses the Codex-native plugin marketplace lane; it does not require a custom mcp_servers.{BuildMcpServerName()} block to count as ready."
                : $"- Updates only Anarchy-owned Codex plugin enable-state in {AnarchyPathCanon.BuildHomeLabelPath(AnarchyPathCanon.UserProfileCodexConfigFileRelativePath)} when needed to select this repo-local runtime lane; unrelated Codex settings are preserved.");
        }
        else
        {
            disclosureLines.Add("- Codex marketplace registration is skipped because Codex is not selected.");
            disclosureLines.Add("- Selected non-Codex hosts are pointed at the shared runtime payload through their own MCP host config files.");
        }

        if (installScope == InstallScope.RepoLocal && hostTargets.HasFlag(HostTargets.Codex))
        {
            disclosureLines.Add("- Repo-local Codex caveat: the installer writes the Codex-documented repo-local shape (marketplace.json + plugins/ bundle) and may set this Anarchy lane to enabled in Codex config while disabling other Anarchy lanes. Codex can surface that repo-local source in the Plugins UI before its chat cache/runtime materializes the same plugin version, so setup readiness and plugin-card visibility are separate from active runtime proof. The bundled runtime is also per-machine, so a committed marketplace entry does not carry a working runtime to collaborators.");
        }

        disclosureLines.Add($"- Host targets: {HostTargetLabels.ToDisplayString(hostTargets)}.");

        if (hostTargets.HasFlag(HostTargets.ClaudeCode))
        {
            disclosureLines.Add($"- Claude Code lane: adds an mcpServers.{BuildMcpServerName()} entry to ~/.claude.json at user scope (read-merge-write; creates a .bak on first modification). Unverified on this machine -- needs a promotion test in the Truth Matrix before it is treated as proven.");
            disclosureLines.Add("- Claude Code restart: the running Claude Code session must be restarted for the new MCP server to appear.");
        }

        if (hostTargets.HasFlag(HostTargets.ClaudeDesktop))
        {
            disclosureLines.Add($"- Claude Desktop lane: auto-detects MSIX vs classic install and merges mcpServers.{BuildMcpServerName()} into the active claude_desktop_config.json (read-merge-write; creates a .bak on first modification; no-op when no install is detected). Unverified on this machine -- needs a promotion test in the Truth Matrix before it is treated as proven.");
            disclosureLines.Add("- Claude Desktop restart: the full app (including the tray process) must be quit and relaunched; Claude Desktop has no hot-reload for mcpServers.");
            disclosureLines.Add("- Claude Desktop MSIX caveat: an open upstream issue can cause older MSIX builds to ignore mcpServers entries even when placed correctly; if the entry does not appear after restart, update Claude Desktop and retry.");
        }

        if (hasWorkspaceTarget)
        {
            disclosureLines.Add("- Seeds missing portable root schema files from the embedded payload.");
            disclosureLines.Add("- Existing root schema files are left in place unless an explicit schema refresh is requested.");
        }
        else
        {
            disclosureLines.Add("- No repo workspace target is selected in this run.");
            disclosureLines.Add("- Portable root schema seeding is skipped until /repo \"<path>\" is provided.");
        }

        disclosureLines.AddRange(
        [
            "Product behavior:",
            $"- Exposes {CoreToolNames.Length} core + {ExperimentalToolNames.Length} test harness tool for preflight, gap assessment, active-work compilation, schema reality, gov2gov reconciliation, and direction-assist testing.",
            "Human impact:",
            installScope == InstallScope.UserProfile
                ? "- Installs once for the current user profile; no machine-wide install or admin change."
                : "- Repo-local only; no machine-wide install or settings change.",
            "- Install itself does not start the MCP runtime as a background process.",
            "AI impact:",
            installScope == InstallScope.UserProfile
                ? $"- Makes {AnarchyBranding.BrandDisplayName} available through the current user profile for supported hosts."
                : $"- Makes {AnarchyBranding.BrandDisplayName} available by default to agents in this repo.",
            "- Strengthens startup/control surfaces; it does not rewrite project code by itself.",
            "Back out now if this repo should remain unchanged."
        ]);

        return string.Join(Environment.NewLine, disclosureLines);
    }

    // Purpose: Builds the plain-text disclosure shown before the GUI applies repo underlay surfaces.
    // Expected input: Target repo path selected in the GUI.
    // Expected output: A bounded disclosure string that distinguishes underlay materialization from runtime install.
    // Critical dependencies: MaterializeUnderlay and the current repo-travel product posture.
    public static string BuildUnderlayDisclosure(string repoPath)
    {
        var targetRepo = string.IsNullOrWhiteSpace(repoPath)
            ? "(not specified)"
            : repoPath;
        var disclosureLines = new[]
        {
            $"Responsible disclosure for repo-underlay {AnarchyBranding.BrandDisplayName} setup.",
            "All carried schema, narrative, and hygiene surfaces remain authored in this repo and are published into the standalone installer payload.",
            $"Target repo: {targetRepo}",
            "Underlay impact:",
            "- Seeds missing portable root schema files from the embedded payload.",
            "- Refreshes stale portable root schema files from the embedded payload and writes timestamped .bak files for overwritten files.",
            "- Seeds the Anarchy narrative register and projects directory when missing.",
            "- Adds Anarchy-scoped .gitignore hygiene lines when missing.",
            "- Creates a small AGENTS.md awareness stub only when AGENTS.md is absent.",
            "- Leaves existing AGENTS.md and existing narrative records in place.",
            "- Does not add plugins\\anarchy-ai\\.",
            "- Does not create or update .agents\\plugins\\marketplace.json.",
            "- Does not register a plugin, modify Codex plugin enable-state, write host MCP config, or start a runtime process.",
            "Product behavior:",
            "- Makes the repo carry portable Anarchy startup/schema discipline without making the repo a runtime source.",
            "- Keeps user-profile install as the normal runtime plugin lane.",
            "- Keeps repo-local runtime install as a CLI-only proving/debug lane.",
            "Human impact:",
            "- Repo-local text/schema surfaces only; no machine-wide install or admin change.",
            "AI impact:",
            "- Helps future agents discover and apply the underlay before task-local shortcuts dominate.",
            "- Does not make Anarchy MCP tools available by itself; use user-profile install for runtime tools.",
            "Back out now if this repo should remain unchanged."
        };

        return string.Join(Environment.NewLine, disclosureLines);
    }

    // Purpose: Builds the CLI help text and lane summary for the setup executable.
    // Expected input: Optional repo path override used for availability and destination labels.
    // Expected output: Plain-text help content that matches the current setup contract.
    // Critical dependencies: TryResolveDefaultRepoRoot, lane label builders, and the current CLI vocabulary.
    public static string BuildCommandLineHelp(string? repoPath)
    {
        var resolvedRepo = string.IsNullOrWhiteSpace(repoPath)
            ? TryResolveDefaultRepoRoot()
            : Path.GetFullPath(repoPath);
        var workspaceTargeted = !string.IsNullOrWhiteSpace(resolvedRepo);
        var targetRepo = workspaceTargeted ? resolvedRepo! : "(repo path unresolved)";
        var availabilityLead = workspaceTargeted
            ? $"This repo has {AnarchyBranding.BrandDisplayName} available."
            : $"{AnarchyBranding.BrandDisplayName} can be installed into a target repo.";
        var schemaSeedingLine = workspaceTargeted
            ? "- Seeds missing portable root schema files during install."
            : "- Seeds missing portable root schema files only when a workspace root is targeted (/repolocal or /userprofile with /repo).";
        var lines = new[]
        {
            AnarchyBranding.SetupDisplayName,
            string.Empty,
            "Usage:",
            "  AnarchyAi.Setup.exe /assess [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /underlay [/repo <path>] [/refresh] [/apply] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /refresh [/repo <path>] [/apply] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /install [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/refreshschemas [/apply]] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /update [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/sourcepath <path>] [/sourceurl <url>] [/refreshschemas [/apply]] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /status [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /? | -? | /h | -h | /help | -help | --help | --?",
            string.Empty,
            "Availability:",
            $"  {availabilityLead}",
            "  Installing it would give the target repo preflight, gap assessment, and schema reality checks.",
            "  It also exposes active-work compilation and gov2gov reconciliation through the same harness surface.",
            $"  Target repo: {targetRepo}",
            string.Empty,
            "Here's what changes:",
            "- /underlay seeds portable schema and narrative discipline into a repo. It does NOT install the runtime plugin, register an MCP server, create marketplace entries, or touch host config.",
            "- /refresh is plan-first schema alignment for the canonical portable schema files; add /apply to overwrite and create timestamped .bak files.",
            $"- /repolocal (repo-local plugin bundle + repo-local marketplace, Codex) adds {BuildPluginFolderLabel(InstallScope.RepoLocal, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.RepoLocal, resolvedRepo)}.",
            "- /repolocal is a proving/debug runtime carrier, not the default committed repo-truth lane.",
            $"- /userprofile (home-local runtime + home-local marketplace, Codex) adds {BuildPluginFolderLabel(InstallScope.UserProfile, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.UserProfile, resolvedRepo)}.",
            $"- /repolocal registers {BuildPluginName(InstallScope.RepoLocal, resolvedRepo)} for the selected repo.",
            $"- /userprofile registers {BuildPluginName(InstallScope.UserProfile, resolvedRepo)} for the current user profile.",
            $"- /userprofile uses the Codex-native plugin marketplace lane rather than requiring a custom mcp_servers.{BuildMcpServerName()} block.",
            "- /repolocal places the runtime binary under the target repo's plugins folder on this machine; collaborators need their own install to get a working runtime even when the marketplace entry is committed.",
            "- Host targets default to Codex when no /codex|/claudecode|/claudedesktop|/allhosts flag is passed; multiple host flags combine (e.g. /codex /claudecode).",
            $"- /claudecode adds an mcpServers.{BuildMcpServerName()} entry to ~/.claude.json at user scope (read-merge-write; creates a .bak on first modification). Requires a Claude Code restart. Unverified on this machine -- promotion pending.",
            $"- /claudedesktop auto-detects MSIX vs classic Claude Desktop and merges mcpServers.{BuildMcpServerName()} into the active claude_desktop_config.json (read-merge-write; creates a .bak on first modification; no-op when no install is detected). Requires a full app restart; older MSIX builds may ignore mcpServers (upstream issue) -- update and retry. Unverified on this machine -- promotion pending.",
            $"- Makes {CoreToolNames.Length} core + {ExperimentalToolNames.Length} test harness tool available to supported hosts.",
            "- Writes and reads a versioned install-state record so later assess/status runs can inspect lifecycle state without trusting plugin UI visibility.",
            "- Does not modify existing AGENTS.md; /underlay creates a small awareness stub only when AGENTS.md is absent.",
            schemaSeedingLine,
            "- Leaves existing root schema files in place unless /refresh /apply or deprecated /refreshschemas /apply is passed.",
            string.Empty,
            "Flags:",
            "  /repolocal             Install or assess through the selected repo root.",
            "  /userprofile           Install or assess through the current user profile.",
            "  /underlay              Seed repo-portable Anarchy discipline without runtime, marketplace, MCP, or host config changes.",
            "  /refresh               Plan portable schema alignment; compose with /underlay or run standalone.",
            "  /apply                 Apply a planned schema refresh; without this, refresh is read-only/plan-first.",
            "  /status                Read-only lifecycle status; /doctor, /selfcheck, and /self-check are aliases.",
            "  /repo <path>            Override repo auto-detection.",
            "  /sourcepath <path>      Refresh from a local AI-Links source path.",
            "  /sourceurl <url>        Refresh from a zip source URL.",
            "  /refreshschemas         Deprecated alias for portable schema refresh; plan-first unless /apply is also supplied.",
            "  /json                   Emit JSON result for assess/install/update operations.",
            "  /silent                 Suppress GUI/prompt behavior for CLI use.",
            "  /host <name>            Carry host context such as codex, claude, cursor, or generic.",
            "  /codex                  Target the Codex host lane (default when no host flag is passed).",
            "  /claudecode             Target the Claude Code user-scope lane (~/.claude.json).",
            "  /claudedesktop          Target the Claude Desktop lane (MSIX or classic claude_desktop_config.json).",
            "  /allhosts               Target every implemented host lane at once (Codex + Claude Code + Claude Desktop)."
        };

        return string.Join(Environment.NewLine, lines);
    }

    // Purpose: Runs the selected setup assess, install, or update operation and summarizes the resulting state.
    // Expected input: Parsed setup options plus current filesystem, marketplace, and optional update-source state.
    // Expected output: A SetupResult containing readiness, repairs, actions, and nested path reports.
    // Critical dependencies: ResolveWorkspaceRoot, payload extraction helpers, marketplace inspection, and the shared path canon.
    public SetupResult Execute(SetupOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var normalizedHostContext = NormalizeHostContext(options.HostContext);
        var workspaceRoot = ResolveWorkspaceRoot(options.InstallScope, options.RepoPath);
        var pluginRoot = ResolvePluginRoot(options.InstallScope, workspaceRoot);
        var marketplacePath = ResolveMarketplacePath(options.InstallScope, workspaceRoot);
        var destinationRuntimePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var destinationPluginManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        var destinationMcpPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath);
        var destinationSkillPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath);
        var destinationSchemaManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath);
        var sourceAuthoringBundle = InspectSourceAuthoringBundle(workspaceRoot);
        var useSourceAuthoringBundleForReadOnlyInspection = ShouldInspectSourceAuthoringBundle(options, sourceAuthoringBundle);
        var inspectedPluginRoot = useSourceAuthoringBundleForReadOnlyInspection
            ? sourceAuthoringBundle.PluginRoot
            : pluginRoot;
        var runtimePath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var pluginManifestPath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        var mcpPath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath);
        var skillPath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath);
        var schemaManifestPath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath);
        var updateSourceRoot = string.Empty;
        var installStateWritten = false;

        var actionsTaken = new HashSet<string>(StringComparer.Ordinal);
        var missingComponents = new HashSet<string>(StringComparer.Ordinal);
        var safeRepairs = new HashSet<string>(StringComparer.Ordinal);
        var updateNotes = new HashSet<string>(StringComparer.Ordinal);

        var updateRequested = options.Mode == OperationMode.Update;
        var updateState = updateRequested ? "in_progress" : "not_requested";
        var runtimeFreeOperation = options.Mode is OperationMode.Underlay or OperationMode.Refresh;
        var codexSelected = options.HostTargets.HasFlag(HostTargets.Codex);
        var refreshResult = RefreshSchemaResult.Empty;
        var duplicateLaneResult = CodexDuplicateLaneResult.Empty;

        if (options.RefreshSchemasAliasUsed)
        {
            updateNotes.Add("/refreshschemas is deprecated because it used to overwrite schema files as a convenience path; use /refresh /apply for deliberate schema alignment.");
        }

        var blockSourceAuthoringConsumerWrite = ShouldBlockSourceAuthoringConsumerWrite(options, sourceAuthoringBundle);

        if (blockSourceAuthoringConsumerWrite)
        {
            actionsTaken.Add("source_authoring_bundle_detected");
            actionsTaken.Add("source_authoring_consumer_install_blocked");
            missingComponents.Add("source_authoring_repo_consumer_install_blocked");
            safeRepairs.Add("use_source_build_lane_or_user_profile_install");
            updateState = updateRequested ? "blocked_source_authoring_repo" : updateState;
        }
        else if (options.Mode == OperationMode.Underlay)
        {
            MaterializeUnderlay(workspaceRoot, options, actionsTaken);
            if (options.RefreshPortableSchemaFamily)
            {
                refreshResult = RefreshEmbeddedPortableSchemaFamily(workspaceRoot, options.ApplyChanges, actionsTaken);
            }
        }
        else if (options.Mode == OperationMode.Refresh)
        {
            refreshResult = RefreshEmbeddedPortableSchemaFamily(workspaceRoot, options.ApplyChanges, actionsTaken);
        }
        else if (options.Mode == OperationMode.Install)
        {
            try
            {
                // Runtime bundle + plugin-bundle-integrity steps are prerequisites for every host lane, so they run unconditionally.
                ExtractEmbeddedPluginBundle(pluginRoot, actionsTaken);
                EnsurePluginManifestIdentity(pluginManifestPath, options.InstallScope, workspaceRoot, actionsTaken);
                EnsurePluginMcpConfiguration(mcpPath, actionsTaken);

                // Codex lane: plugin marketplace registration only runs when the Codex host is selected.
                if (options.HostTargets.HasFlag(HostTargets.Codex))
                {
                    EnsureMarketplaceRegistration(marketplacePath, options.InstallScope, workspaceRoot, actionsTaken);
                }
                else
                {
                    actionsTaken.Add("codex_marketplace_registration_skipped_host_not_selected");
                }

                // Claude Code lane: write mcpServers entry to ~/.claude.json at user scope.
                if (options.HostTargets.HasFlag(HostTargets.ClaudeCode))
                {
                    ClaudeCodeUserScopeLane.Register(
                        serverName: BuildMcpServerName(),
                        commandPath: runtimePath,
                        args: null,
                        env: null,
                        actionsTaken: actionsTaken);
                }

                // Claude Desktop lane: detect MSIX vs classic install and merge mcpServers into the active config.
                if (options.HostTargets.HasFlag(HostTargets.ClaudeDesktop))
                {
                    ClaudeDesktopLane.Register(
                        serverName: BuildMcpServerName(),
                        commandPath: runtimePath,
                        args: null,
                        env: null,
                        actionsTaken: actionsTaken);
                }

                if (string.IsNullOrWhiteSpace(workspaceRoot))
                {
                    actionsTaken.Add("portable_schema_family_not_targeted");
                }
                else if (options.RefreshPortableSchemaFamily)
                {
                    refreshResult = RefreshEmbeddedPortableSchemaFamily(workspaceRoot, options.ApplyChanges, actionsTaken);
                }
                else
                {
                    SeedMissingEmbeddedPortableSchemaFamily(workspaceRoot, actionsTaken);
                }

                WriteInstallState(
                    options,
                    normalizedHostContext,
                    workspaceRoot,
                    pluginRoot,
                    marketplacePath,
                    runtimePath,
                    updateSourceRoot,
                    actionsTaken);
                installStateWritten = true;
            }
            catch (IOException ex) when (IsRuntimeLockException(ex))
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("install_apply_failed");
                safeRepairs.Add("release_runtime_lock_and_retry_install");
                safeRepairs.Add("run_safe_release_runtime_lock");
                safeRepairs.Add("run_force_release_runtime_lock");
            }
            catch (UnauthorizedAccessException ex)
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("install_apply_failed");
                safeRepairs.Add("release_runtime_lock_and_retry_install");
                safeRepairs.Add("run_safe_release_runtime_lock");
                safeRepairs.Add("run_force_release_runtime_lock");
            }
        }
        else if (options.Mode == OperationMode.Update)
        {
            try
            {
                updateSourceRoot = RefreshFromUpdateSource(pluginRoot, workspaceRoot, options, actionsTaken, updateNotes);
                EnsurePluginManifestIdentity(pluginManifestPath, options.InstallScope, workspaceRoot, actionsTaken);
                EnsurePluginMcpConfiguration(mcpPath, actionsTaken);

                if (options.HostTargets.HasFlag(HostTargets.Codex))
                {
                    EnsureMarketplaceRegistration(marketplacePath, options.InstallScope, workspaceRoot, actionsTaken);
                }
                else
                {
                    actionsTaken.Add("codex_marketplace_registration_skipped_host_not_selected");
                }

                if (options.HostTargets.HasFlag(HostTargets.ClaudeCode))
                {
                    ClaudeCodeUserScopeLane.Register(
                        serverName: BuildMcpServerName(),
                        commandPath: runtimePath,
                        args: null,
                        env: null,
                        actionsTaken: actionsTaken);
                }

                if (options.HostTargets.HasFlag(HostTargets.ClaudeDesktop))
                {
                    ClaudeDesktopLane.Register(
                        serverName: BuildMcpServerName(),
                        commandPath: runtimePath,
                        args: null,
                        env: null,
                        actionsTaken: actionsTaken);
                }

                WriteInstallState(
                    options,
                    normalizedHostContext,
                    workspaceRoot,
                    pluginRoot,
                    marketplacePath,
                    runtimePath,
                    updateSourceRoot,
                    actionsTaken);
                installStateWritten = true;
                updateState = "completed";
            }
            catch (IOException ex) when (IsRuntimeLockException(ex))
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

        if ((options.Mode == OperationMode.Install || options.Mode == OperationMode.Update)
            && options.HostTargets.HasFlag(HostTargets.Codex)
            && string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            try
            {
                duplicateLaneResult = UpdateCodexPrimaryLaneForSelectedInstall(options, workspaceRoot, actionsTaken);
            }
            catch (IOException ex)
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("duplicate_codex_lane_cleanup_failed");
                safeRepairs.Add("inventory_and_disable_duplicate_anarchy_codex_lanes");
            }
            catch (UnauthorizedAccessException ex)
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("duplicate_codex_lane_cleanup_failed");
                safeRepairs.Add("inventory_and_disable_duplicate_anarchy_codex_lanes");
            }
        }

        if ((options.Mode == OperationMode.Install || options.Mode == OperationMode.Update)
            && options.InstallScope == InstallScope.UserProfile
            && string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            try
            {
                RemoveLegacyCodexCustomMcpEntry(options.InstallScope, normalizedHostContext, actionsTaken);
            }
            catch (IOException ex)
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("legacy_codex_custom_mcp_cleanup_failed");
                safeRepairs.Add("inventory_and_remove_stale_codex_custom_mcp_entry");
            }
            catch (UnauthorizedAccessException ex)
            {
                updateNotes.Add(ex.Message);
                missingComponents.Add("legacy_codex_custom_mcp_cleanup_failed");
                safeRepairs.Add("inventory_and_remove_stale_codex_custom_mcp_entry");
            }
        }

        if (actionsTaken.Contains("skipped_locked_bundle_surface_with_unknown_drift"))
        {
            missingComponents.Add("locked_bundle_surface_write_skipped");
            safeRepairs.Add("release_runtime_lock_and_retry_install");
            safeRepairs.Add("run_safe_release_runtime_lock");
            safeRepairs.Add("run_force_release_runtime_lock");
        }

        if (useSourceAuthoringBundleForReadOnlyInspection)
        {
            actionsTaken.Add("source_authoring_bundle_detected");
        }

        var pluginManifestExists = File.Exists(pluginManifestPath);
        var mcpExists = File.Exists(mcpPath);
        var runtimeExists = File.Exists(runtimePath);
        var skillExists = File.Exists(skillPath);
        var schemaManifestExists = File.Exists(schemaManifestPath);
        var pluginRootExists = Directory.Exists(inspectedPluginRoot);
        var inspectChildBundleSurfaces = !runtimeFreeOperation && (pluginRootExists
            || pluginManifestExists
            || mcpExists
            || runtimeExists
            || skillExists
            || schemaManifestExists);

        if (inspectChildBundleSurfaces && !pluginManifestExists) { missingComponents.Add("codex_plugin_manifest_missing"); }
        if (inspectChildBundleSurfaces && !mcpExists) { missingComponents.Add("codex_mcp_declaration_missing"); }
        if (inspectChildBundleSurfaces && !runtimeExists) { missingComponents.Add("bundled_runtime_missing"); }
        if (inspectChildBundleSurfaces && !skillExists && string.Equals(options.HostContext, "codex", StringComparison.OrdinalIgnoreCase))
        {
            missingComponents.Add("codex_skill_surface_missing");
        }
        if (inspectChildBundleSurfaces && !schemaManifestExists) { missingComponents.Add("schema_bundle_manifest_missing"); }

        if (inspectChildBundleSurfaces)
        {
            foreach (var contractFile in CoreContractFiles)
            {
                var contractPath = AnarchyPathCanon.ResolveBundleFilePath(
                    inspectedPluginRoot,
                    AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, contractFile));
                if (!File.Exists(contractPath))
                {
                    missingComponents.Add($"missing_contract:{contractFile}");
                }
            }
        }

        if (inspectChildBundleSurfaces)
        {
            var experimentalDirectionAssistContractPath = AnarchyPathCanon.ResolveBundleFilePath(
                inspectedPluginRoot,
                AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, ExperimentalDirectionAssistContract));
            if (!File.Exists(experimentalDirectionAssistContractPath))
            {
                actionsTaken.Add("experimental_direction_assist_contract_missing_non_blocking");
            }
        }

        if (inspectChildBundleSurfaces)
        {
            foreach (var templateFile in NarrativeTemplateFiles)
            {
                var templatePath = AnarchyPathCanon.ResolveBundleFilePath(inspectedPluginRoot, templateFile);
                if (!File.Exists(templatePath))
                {
                    missingComponents.Add($"missing_narrative_template:{templateFile}");
                }
            }
        }

        var pluginManifestInspection = runtimeFreeOperation
            ? new PluginManifestInspection(false, false, true, [])
            : InspectPluginManifest(pluginManifestPath, options.InstallScope, workspaceRoot);
        missingComponents.UnionWith(pluginManifestInspection.Findings);
        var marketplaceInspection = runtimeFreeOperation
            ? new MarketplaceInspection(false, false, false, false, true, true, [])
            : !codexSelected
            ? new MarketplaceInspection(false, false, false, false, true, true, [])
            : InspectMarketplace(marketplacePath, options.InstallScope, workspaceRoot);
        missingComponents.UnionWith(marketplaceInspection.Findings);
        var mcpInspection = runtimeFreeOperation
            ? new McpConfigurationInspection(false, true, true, [])
            : InspectMcpConfiguration(mcpPath);
        missingComponents.UnionWith(mcpInspection.Findings);
        var hostConfigInspection = runtimeFreeOperation
            ? HostConfigInspection.NotRequired
            : InspectSelectedHostConfigs(options.HostTargets, destinationRuntimePath);
        missingComponents.UnionWith(hostConfigInspection.Findings);
        var marketplaceMissingFinding = BuildMarketplaceMissingFinding(options.InstallScope);
        var marketplaceMissingPluginsArrayFinding = BuildMarketplaceMissingPluginsArrayFinding(options.InstallScope);
        if (!runtimeFreeOperation && codexSelected && !marketplaceInspection.Exists && !useSourceAuthoringBundleForReadOnlyInspection && !blockSourceAuthoringConsumerWrite)
        {
            missingComponents.Add(marketplaceMissingFinding);
        }
        if (!runtimeFreeOperation && codexSelected && marketplaceInspection.Exists && !marketplaceInspection.HasPluginsArray && !useSourceAuthoringBundleForReadOnlyInspection && !blockSourceAuthoringConsumerWrite)
        {
            missingComponents.Add(marketplaceMissingPluginsArrayFinding);
        }
        if (!runtimeFreeOperation && codexSelected && marketplaceInspection.Exists && !marketplaceInspection.IsValidJson && !useSourceAuthoringBundleForReadOnlyInspection && !blockSourceAuthoringConsumerWrite)
        {
            missingComponents.Add("marketplace_json_invalid");
        }

        var marketplaceRegistrationReady = runtimeExists
            && (!codexSelected || marketplaceInspection.HasAnarchyPluginEntry)
            && (!codexSelected || marketplaceInspection.InstalledByDefault)
            && pluginManifestInspection.IdentityAligned
            && (!codexSelected || marketplaceInspection.MarketplaceIdentityAligned)
            && mcpInspection.IdentityAligned
            && hostConfigInspection.Ready;
        var legacyUserProfileInspection = InspectLegacyUserProfileSurfaces(options.InstallScope, normalizedHostContext, marketplaceRegistrationReady);
        missingComponents.UnionWith(legacyUserProfileInspection.Findings);

        if (!runtimeFreeOperation && !runtimeExists) { safeRepairs.Add("publish_or_restore_bundled_runtime"); }
        if (!runtimeFreeOperation
            && codexSelected
            && (!marketplaceInspection.HasAnarchyPluginEntry || !marketplaceInspection.InstalledByDefault)
            && !blockSourceAuthoringConsumerWrite)
        {
            safeRepairs.Add(useSourceAuthoringBundleForReadOnlyInspection
                ? "choose_user_profile_install_or_explicit_consumer_repo_install"
                : options.InstallScope == InstallScope.UserProfile
                    ? "run_user_profile_harness_install"
                    : "run_bootstrap_harness_install");
        }
        if (!pluginManifestInspection.IdentityAligned)
        {
            safeRepairs.Add(options.InstallScope == InstallScope.UserProfile
                ? "refresh_user_profile_plugin_identity"
                : "refresh_repo_plugin_identity");
        }
        if (codexSelected && !marketplaceInspection.MarketplaceIdentityAligned && !useSourceAuthoringBundleForReadOnlyInspection && !blockSourceAuthoringConsumerWrite)
        {
            safeRepairs.Add(options.InstallScope == InstallScope.UserProfile
                ? "refresh_user_profile_marketplace_identity"
                : "refresh_repo_marketplace_identity");
        }
        if (!mcpInspection.IdentityAligned)
        {
            safeRepairs.Add("refresh_mcp_server_identity");
        }
        safeRepairs.UnionWith(hostConfigInspection.SafeRepairs);
        if (legacyUserProfileInspection.LegacyPluginRootPresent)
        {
            safeRepairs.Add("inventory_and_manually_quarantine_legacy_user_profile_plugin_root");
        }
        if (legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent)
        {
            safeRepairs.Add("inventory_and_remove_stale_codex_custom_mcp_entry");
        }

        var installStateReport = runtimeFreeOperation
            ? BuildRuntimeFreeInstallStateReport()
            : InspectInstallState(
                options,
                normalizedHostContext,
                workspaceRoot,
                pluginRoot,
                marketplacePath,
                runtimePath,
                installStateWritten);
        if (options.Mode == OperationMode.Status)
        {
            missingComponents.UnionWith(installStateReport.findings);
            if (!installStateReport.state_present)
            {
                safeRepairs.Add("run_install_to_write_install_state");
            }
            else if (!installStateReport.state_valid)
            {
                safeRepairs.Add("rerun_install_to_refresh_install_state");
            }
        }

        var hasLockedBundleSurfaceSkip = missingComponents.Contains("locked_bundle_surface_write_skipped");
        var hasBlockingLegacySurface = legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent;
        var hasInstallStateStatusGap = options.Mode == OperationMode.Status && !installStateReport.state_valid;
        var bootstrapState = blockSourceAuthoringConsumerWrite
            ? "source_authoring_write_blocked"
            : useSourceAuthoringBundleForReadOnlyInspection
            && sourceAuthoringBundle.IsComplete
            && !marketplaceRegistrationReady
            && !hasLockedBundleSurfaceSkip
            && !hasInstallStateStatusGap
            ? "source_authoring_bundle_ready"
            : !hasLockedBundleSurfaceSkip
            && marketplaceRegistrationReady
            && !hasBlockingLegacySurface
            && !hasInstallStateStatusGap
            ? "ready"
        : runtimeExists && marketplaceInspection.HasAnarchyPluginEntry && marketplaceInspection.InstalledByDefault
            ? "registration_refresh_needed"
                : runtimeExists && (pluginManifestExists || mcpExists)
                    ? "repo_bundle_present_unregistered"
                : runtimeExists
                    ? "runtime_only"
                    : "bootstrap_needed";
        var registrationMode = runtimeFreeOperation
            ? "none"
            : DetermineRegistrationMode(options.InstallScope, normalizedHostContext, options.HostTargets, legacyUserProfileInspection);

        var nextAction = hasLockedBundleSurfaceSkip
            ? "release_runtime_lock_and_retry_install"
            : blockSourceAuthoringConsumerWrite
                ? "use_source_build_lane_or_user_profile_install"
            : hasBlockingLegacySurface
                ? "inventory_legacy_home_install_and_run_user_profile_install"
            : hasInstallStateStatusGap
                ? (installStateReport.state_present ? "rerun_install_to_refresh_install_state" : "run_install_to_write_install_state")
            : bootstrapState == "source_authoring_bundle_ready"
                ? "use_source_build_lane_or_user_profile_install"
            : bootstrapState switch
        {
            "ready" => "use_preflight_session",
            "registration_refresh_needed" => "refresh_plugin_registration",
            "repo_bundle_present_unregistered" => "register_plugin_in_marketplace",
            "runtime_only" => "materialize_repo_plugin_bundle",
            _ => "restore_runtime_or_complete_bundle"
        };

        if (runtimeFreeOperation && !blockSourceAuthoringConsumerWrite)
        {
            bootstrapState = refreshResult.RefreshNeeded && !options.ApplyChanges
                ? "refresh_plan_ready"
                : "ready";
            nextAction = refreshResult.RefreshNeeded && !options.ApplyChanges
                ? "run_refresh_with_apply_to_materialize"
                : options.Mode == OperationMode.Underlay
                    ? "review_underlay_artifacts"
                    : "review_refresh_result";
        }

        var resultPaths = BuildSetupResultPaths(
            options,
            workspaceRoot,
            pluginRoot,
            marketplacePath,
            destinationRuntimePath,
            destinationPluginManifestPath,
            destinationMcpPath,
            destinationSkillPath,
            destinationSchemaManifestPath,
            updateSourceRoot);
        var codexMaterialization = options.HostTargets.HasFlag(HostTargets.Codex) && !runtimeFreeOperation
            ? InspectCodexMaterialization(
                options.InstallScope,
                workspaceRoot,
                destinationPluginManifestPath)
            : null;

        return new SetupResult
        {
            setup_operation = BuildSetupOperationLabel(options.Mode),
            bootstrap_state = bootstrapState,
            registration_mode = registrationMode,
            host_context = normalizedHostContext,
            host_targets = HostTargetLabels.ToLabelArray(options.HostTargets),
            install_scope = BuildInstallScopeJsonLabel(options),
            update_requested = updateRequested,
            update_state = updateState,
            update_source_zip_url = options.UpdateSourceZipUrl,
            update_notes = updateNotes.ToArray(),
            runtime_present = !runtimeFreeOperation && runtimeExists,
            marketplace_registered = !runtimeFreeOperation && codexSelected && marketplaceInspection.HasAnarchyPluginEntry,
            installed_by_default = !runtimeFreeOperation && codexSelected && marketplaceInspection.InstalledByDefault,
            host_config_modified = duplicateLaneResult.SelectedLaneEnabled
                || duplicateLaneResult.DisabledLanes.Length > 0
                || actionsTaken.Contains("removed_legacy_codex_custom_mcp_entry")
                || DidModifyHostConfig(actionsTaken),
            refresh_plan_only = options.RefreshPortableSchemaFamily && !options.ApplyChanges,
            refreshed_files = refreshResult.RefreshedFiles,
            unchanged_files = refreshResult.UnchangedFiles,
            backup_files = refreshResult.BackupFiles,
            selected_codex_primary_lane = duplicateLaneResult.SelectedPrimaryLane,
            disabled_duplicate_codex_lanes = duplicateLaneResult.DisabledLanes,
            duplicate_codex_skill_lanes_detected = duplicateLaneResult.DuplicateLanesDetected,
            source_authoring_bundle_present = sourceAuthoringBundle.Present,
            source_authoring_bundle_state = sourceAuthoringBundle.Present
                ? sourceAuthoringBundle.State
                : null,
            actions_taken = actionsTaken.ToArray(),
            missing_components = missingComponents.ToArray(),
            safe_repairs = safeRepairs.ToArray(),
            next_action = nextAction,
            install_state = installStateReport,
            codex_materialization = codexMaterialization,
            paths = resultPaths
        };
    }

    // Purpose: Normalizes host-context values into the bounded host labels supported by setup.
    // Expected input: Raw host-context text from CLI or GUI state.
    // Expected output: One of codex, claude, cursor, or generic, defaulting to codex.
    // Critical dependencies: Host-specific branching elsewhere in SetupEngine.
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

    // Purpose: Resolves the workspace root that repo-local work should target.
    // Expected input: Install scope and optional explicit repo path.
    // Expected output: Absolute repo root for repo-local operations, or an empty string for user-profile operations with no workspace target.
    // Critical dependencies: TryResolveDefaultRepoRoot, directory existence checks, and the current lane semantics.
    private static string ResolveWorkspaceRoot(InstallScope installScope, string explicitRepoPath)
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

        if (installScope == InstallScope.UserProfile)
        {
            return string.Empty;
        }

        var detected = TryResolveDefaultRepoRoot();
        if (!string.IsNullOrWhiteSpace(detected))
        {
            return detected;
        }

        throw new InvalidOperationException("Could not resolve the repo root automatically. Provide /repo \"C:\\path\\to\\repo\".");
    }

    // Purpose: Detects whether a path under a plugins directory can be safely mapped back to a repo root.
    // Expected input: Candidate path plus an output slot for the resolved repo root.
    // Expected output: True when the candidate represents a repo-local plugins directory and exposes the resolved repo root.
    // Critical dependencies: DirectoryInfo and LooksLikeRepoRoot.
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

    // Purpose: Applies the repo-root trust rule used for setup auto-detection.
    // Expected input: Candidate filesystem path.
    // Expected output: True when the path looks like a real git repo root.
    // Critical dependencies: .git presence and the guardrail against trusting generic parent folders.
    private static bool LooksLikeRepoRoot(string path)
    {
        // Auto-detection should only trust an actual repo marker.
        // Generic parent folders can also contain "plugins" or ".agents",
        // which makes those signals too weak for safe default resolution.
        return Directory.Exists(Path.Combine(path, ".git")) ||
               File.Exists(Path.Combine(path, ".git"));
    }

    // Purpose: Classifies file-write failures that probably came from a live runtime lock.
    // Expected input: An IOException raised during setup or update file operations.
    // Expected output: True when the message matches the lock-related failure patterns we currently treat as recoverable.
    // Critical dependencies: The current Windows runtime-lock error strings.
    private static bool IsRuntimeLockException(IOException ex)
    {
        return ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("access is denied", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("access to the path", StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Hash-compares an embedded payload resource against an on-disk target file.
    // Expected input: Embedded resource name and target file path.
    // Expected output: True when comparison completed, with the out parameter indicating content alignment.
    // Critical dependencies: PayloadResources, SHA256 hashing, and read access to the target file.
    private static bool TryIsResourceContentAligned(string resourceName, string targetPath, out bool aligned)
    {
        aligned = false;
        if (!File.Exists(targetPath))
        {
            return false;
        }

        try
        {
            using var resourceStream = PayloadResources.OpenResource(resourceName);
            using var targetStream = new FileStream(
                targetPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            if (resourceStream.CanSeek && targetStream.CanSeek && resourceStream.Length != targetStream.Length)
            {
                return false;
            }

            var resourceHash = SHA256.HashData(resourceStream);
            var targetHash = SHA256.HashData(targetStream);
            aligned = resourceHash.AsSpan().SequenceEqual(targetHash);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Purpose: Materializes the embedded plugin bundle into the chosen install root while tolerating aligned locked files.
    // Expected input: Target plugin root and mutable action log.
    // Expected output: No direct return value; writes bundle files and records lock-related actions.
    // Critical dependencies: PayloadResources.GetPluginBundleResources, TryIsResourceContentAligned, and filesystem write access.
    private static void ExtractEmbeddedPluginBundle(string pluginRoot, HashSet<string> actionsTaken)
    {
        var retainedLockedSurfaceWithoutDrift = false;
        var skippedLockedSurfaceWithUnknownDrift = false;

        Directory.CreateDirectory(pluginRoot);
        foreach (var resource in PayloadResources.GetPluginBundleResources())
        {
            var relativePath = resource[AnarchyPathCanon.BuildPluginPayloadResourcePrefix().Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var targetPath = Path.Combine(pluginRoot, relativePath);
            if (TryIsResourceContentAligned(resource, targetPath, out var alignedBeforeWrite) && alignedBeforeWrite)
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            try
            {
                using var stream = PayloadResources.OpenResource(resource);
                using var output = File.Create(targetPath);
                stream.CopyTo(output);
            }
            catch (IOException ex)
            {
                if (IsRuntimeLockException(ex) && File.Exists(targetPath))
                {
                    if (TryIsResourceContentAligned(resource, targetPath, out var alignedAfterFailure) && alignedAfterFailure)
                    {
                        retainedLockedSurfaceWithoutDrift = true;
                    }
                    else
                    {
                        skippedLockedSurfaceWithUnknownDrift = true;
                    }

                    continue;
                }

                throw;
            }
            catch (UnauthorizedAccessException) when (File.Exists(targetPath))
            {
                if (TryIsResourceContentAligned(resource, targetPath, out var alignedAfterFailure) && alignedAfterFailure)
                {
                    retainedLockedSurfaceWithoutDrift = true;
                }
                else
                {
                    skippedLockedSurfaceWithUnknownDrift = true;
                }

                continue;
            }
        }

        if (retainedLockedSurfaceWithoutDrift)
        {
            actionsTaken.Add("retained_locked_bundle_surface_without_content_drift");
        }

        if (skippedLockedSurfaceWithUnknownDrift)
        {
            actionsTaken.Add("skipped_locked_bundle_surface_with_unknown_drift");
        }

        actionsTaken.Add("materialized_plugin_bundle_from_embedded_payload");
    }

    // Purpose: Realigns the plugin manifest's name with the install lane's expected plugin identity.
    // Expected input: Plugin manifest path, install scope, repo root, and mutable action log.
    // Expected output: No direct return value; rewrites the manifest when its name is missing or stale.
    // Critical dependencies: BuildPluginName, JSON parsing, and file-write access.
    private static void EnsurePluginManifestIdentity(string pluginManifestPath, InstallScope installScope, string repoRoot, HashSet<string> actionsTaken)
    {
        if (!File.Exists(pluginManifestPath))
        {
            return;
        }

        var expectedPluginName = BuildPluginName(installScope, repoRoot);
        JsonObject pluginManifest;
        try
        {
            pluginManifest = JsonNode.Parse(File.ReadAllText(pluginManifestPath))?.AsObject()
                ?? new JsonObject();
        }
        catch (JsonException)
        {
            pluginManifest = new JsonObject();
            actionsTaken.Add("replaced_invalid_plugin_manifest");
        }

        if (!string.Equals(pluginManifest["name"]?.GetValue<string>(), expectedPluginName, StringComparison.Ordinal))
        {
            pluginManifest["name"] = expectedPluginName;
            File.WriteAllText(pluginManifestPath, pluginManifest.ToJsonString(ProgramJson.Options));
            actionsTaken.Add(installScope == InstallScope.UserProfile
                ? "updated_user_profile_plugin_identity"
                : "updated_repo_plugin_identity");
        }
    }

    // Purpose: Writes the full portable schema family from the embedded payload into the targeted repo root.
    // Expected input: Repo root and mutable action log.
    // Expected output: No direct return value; overwrites the portable schema family surfaces from the embedded payload.
    // Critical dependencies: PayloadResources.GetPortableSchemaResources and filesystem write access.
    private static void ExtractEmbeddedPortableSchemaFamily(string repoRoot, HashSet<string> actionsTaken)
    {
        foreach (var resource in PayloadResources.GetPortableSchemaResources())
        {
            var fileName = resource[AnarchyPathCanon.BuildPortableSchemaPayloadResourcePrefix().Length..]
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

    // Purpose: Seeds only missing portable schema files into the targeted repo root.
    // Expected input: Repo root and mutable action log.
    // Expected output: No direct return value; copies only absent portable schema files and records whether any copy occurred.
    // Critical dependencies: PayloadResources.GetPortableSchemaResources and file-existence checks.
    private static void SeedMissingEmbeddedPortableSchemaFamily(string repoRoot, HashSet<string> actionsTaken)
    {
        var copiedAny = false;

        foreach (var resource in PayloadResources.GetPortableSchemaResources())
        {
            var fileName = resource[AnarchyPathCanon.BuildPortableSchemaPayloadResourcePrefix().Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var targetPath = Path.Combine(repoRoot, fileName);
            if (File.Exists(targetPath))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using var stream = PayloadResources.OpenResource(resource);
            using var output = File.Create(targetPath);
            stream.CopyTo(output);
            copiedAny = true;
        }

        actionsTaken.Add(copiedAny
            ? "seeded_missing_portable_schema_family_from_embedded_payload"
            : "portable_schema_family_already_present");
    }

    private static void MaterializeUnderlay(string repoRoot, SetupOptions options, HashSet<string> actionsTaken)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new DirectoryNotFoundException("/underlay requires a repo root. Pass /repo <path> or run from a repo root.");
        }

        SeedMissingEmbeddedPortableSchemaFamily(repoRoot, actionsTaken);
        SeedNarrativeRegister(repoRoot, actionsTaken);
        EnsureNarrativeProjectsDirectory(repoRoot, actionsTaken);
        SeedAgentsAwarenessNoteIfMissing(repoRoot, actionsTaken);
        EnsureAnarchyGitignoreBlock(repoRoot, actionsTaken);
        actionsTaken.Add("materialized_repo_underlay");
    }

    private static RefreshSchemaResult RefreshEmbeddedPortableSchemaFamily(string repoRoot, bool applyChanges, HashSet<string> actionsTaken)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            actionsTaken.Add("portable_schema_family_not_targeted");
            return RefreshSchemaResult.Empty;
        }

        var refreshedFiles = new List<string>();
        var unchangedFiles = new List<string>();
        var backupFiles = new List<string>();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        foreach (var resource in PayloadResources.GetPortableSchemaResources())
        {
            var fileName = resource[AnarchyPathCanon.BuildPortableSchemaPayloadResourcePrefix().Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var targetPath = Path.Combine(repoRoot, fileName);

            if (TryIsResourceContentAligned(resource, targetPath, out var aligned) && aligned)
            {
                unchangedFiles.Add(fileName);
                continue;
            }

            refreshedFiles.Add(fileName);
            if (!applyChanges)
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            if (File.Exists(targetPath))
            {
                var backupPath = $"{targetPath}.{timestamp}.bak";
                File.Copy(targetPath, backupPath, overwrite: false);
                backupFiles.Add(Path.GetRelativePath(repoRoot, backupPath));
            }

            using var stream = PayloadResources.OpenResource(resource);
            using var output = File.Create(targetPath);
            stream.CopyTo(output);
        }

        actionsTaken.Add(applyChanges
            ? "refreshed_portable_schema_family_from_embedded_payload"
            : "planned_portable_schema_family_refresh_from_embedded_payload");

        return new RefreshSchemaResult(
            refreshedFiles.OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            unchangedFiles.OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            backupFiles.OrderBy(static value => value, StringComparer.Ordinal).ToArray());
    }

    private static void SeedNarrativeRegister(string repoRoot, HashSet<string> actionsTaken)
    {
        var registerPath = Path.Combine(repoRoot, ConsumerNarrativeRegisterRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(registerPath))
        {
            actionsTaken.Add("narrative_register_already_present");
            return;
        }

        var templateText = ReadPluginPayloadText(AnarchyPathCanon.BundleNarrativeRegisterTemplateFileRelativePath);
        var register = JsonNode.Parse(templateText) as JsonObject ?? new JsonObject();
        var workspaceHash = BuildWorkspaceHash(repoRoot);
        var openedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        register["open-threads"] = new JsonArray
        {
            new JsonObject
            {
                ["id"] = $"ot-{workspaceHash}-001",
                ["summary"] = "decide whether to install Anarchy runtime in this workspace (user-profile or repo-local)",
                ["opened-date"] = openedDate,
                ["owner"] = "consumer-workspace-owner",
                ["stale"] = false,
                ["auto-close-trigger"] = "a runtime install lane registers a marketplace entry for this workspace"
            },
            new JsonObject
            {
                ["id"] = $"ot-{workspaceHash}-002",
                ["summary"] = "decide whether to run gov2gov migration to align portable schemas with canon",
                ["opened-date"] = openedDate,
                ["owner"] = "consumer-workspace-owner",
                ["stale"] = false,
                ["auto-close-trigger"] = "gov2gov runs in any mode against this workspace"
            }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(registerPath)!);
        File.WriteAllText(registerPath, register.ToJsonString(ProgramJson.Options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        actionsTaken.Add("seeded_narrative_register");
    }

    private static void EnsureNarrativeProjectsDirectory(string repoRoot, HashSet<string> actionsTaken)
    {
        var projectsPath = Path.Combine(repoRoot, ConsumerNarrativeProjectsDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(projectsPath);
        actionsTaken.Add("ensured_narrative_projects_directory");
    }

    private static void SeedAgentsAwarenessNoteIfMissing(string repoRoot, HashSet<string> actionsTaken)
    {
        var agentsPath = Path.Combine(repoRoot, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            actionsTaken.Add("agents_md_already_present_left_unchanged");
            return;
        }

        var templateText = ReadPluginPayloadText(AgentsAwarenessNoteTemplateRelativePath);
        File.WriteAllText(agentsPath, templateText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        actionsTaken.Add("seeded_agents_md_awareness_note");
    }

    private static void EnsureAnarchyGitignoreBlock(string repoRoot, HashSet<string> actionsTaken)
    {
        var gitignorePath = Path.Combine(repoRoot, ".gitignore");
        var existingText = File.Exists(gitignorePath) ? File.ReadAllText(gitignorePath) : string.Empty;
        var underlayGitignoreLines = BuildUnderlayGitignoreLines();
        if (underlayGitignoreLines.Skip(1).All(line => existingText.Contains(line, StringComparison.OrdinalIgnoreCase)))
        {
            actionsTaken.Add("anarchy_gitignore_block_already_present");
            return;
        }

        var builder = new StringBuilder(existingText);
        if (builder.Length > 0 && !existingText.EndsWith('\n'))
        {
            builder.AppendLine();
        }

        builder.AppendLine();
        foreach (var line in underlayGitignoreLines)
        {
            if (line.StartsWith('#') || !existingText.Contains(line, StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine(line);
            }
        }

        File.WriteAllText(gitignorePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        actionsTaken.Add("updated_anarchy_gitignore_block");
    }

    private static string[] BuildUnderlayGitignoreLines()
    {
        return
        [
            "# Anarchy-AI runtime/install artifacts. Portable schemas and .agents/anarchy-ai/narratives remain repo truth when intentionally committed.",
            "/" + AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath + "/",
            "/" + AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath,
            "/" + ConsumerDirectionAssistTestRegisterRelativePath
        ];
    }

    private static string ReadPluginPayloadText(string relativePath)
    {
        var normalizedRelativePath = AnarchyPathCanon.NormalizeCanonRelativePath(relativePath);
        var resourceName = PayloadResources.GetPluginBundleResources()
            .FirstOrDefault(name =>
            {
                var resourceRelativePath = name[AnarchyPathCanon.BuildPluginPayloadResourcePrefix().Length..]
                    .Replace('\\', '/');
                return string.Equals(resourceRelativePath, normalizedRelativePath, StringComparison.Ordinal);
            })
            ?? AnarchyPathCanon.BuildPluginPayloadResourcePath(normalizedRelativePath);
        using var stream = PayloadResources.OpenResource(resourceName);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string BuildWorkspaceHash(string repoRoot)
    {
        var normalized = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    // Purpose: Creates or refreshes the repo-local or user-profile marketplace entry for the current install lane.
    // Expected input: Marketplace path, install scope, repo root, and mutable action log.
    // Expected output: No direct return value; writes marketplace identity and the current Anarchy-AI plugin entry.
    // Critical dependencies: BuildPluginName, BuildPluginRelativePath, BuildMarketplaceName, and JSON marketplace manipulation.
    private static void EnsureMarketplaceRegistration(string marketplacePath, InstallScope installScope, string repoRoot, HashSet<string> actionsTaken)
    {
        var marketplaceDirectory = Path.GetDirectoryName(marketplacePath)!;
        if (!Directory.Exists(marketplaceDirectory))
        {
            Directory.CreateDirectory(marketplaceDirectory);
            actionsTaken.Add("created_marketplace_directory");
        }

        var expectedPluginName = BuildPluginName(installScope, repoRoot);
        var expectedPluginPath = BuildPluginRelativePath(installScope, repoRoot);
        var expectedMarketplaceName = BuildMarketplaceName(installScope, repoRoot);
        var expectedMarketplaceDisplayName = BuildMarketplaceDisplayName(installScope, repoRoot);
        var marketplaceChanged = false;
        JsonObject marketplaceObject;
        if (File.Exists(marketplacePath))
        {
            try
            {
                marketplaceObject = JsonNode.Parse(File.ReadAllText(marketplacePath))?.AsObject()
                    ?? CreateDefaultMarketplace(installScope, repoRoot);
            }
            catch (JsonException)
            {
                marketplaceObject = CreateDefaultMarketplace(installScope, repoRoot);
                actionsTaken.Add("replaced_invalid_marketplace_json");
                marketplaceChanged = true;
            }
        }
        else
        {
            marketplaceObject = CreateDefaultMarketplace(installScope, repoRoot);
            marketplaceChanged = true;
        }

        var updatedMarketplaceIdentity = false;
        var currentMarketplaceName = marketplaceObject["name"]?.GetValue<string>() ?? string.Empty;
        if (!string.Equals(currentMarketplaceName, expectedMarketplaceName, StringComparison.Ordinal))
        {
            marketplaceObject["name"] = expectedMarketplaceName;
            updatedMarketplaceIdentity = true;
        }

        var interfaceObject = marketplaceObject["interface"] as JsonObject;
        if (interfaceObject is null)
        {
            interfaceObject = new JsonObject();
            marketplaceObject["interface"] = interfaceObject;
            updatedMarketplaceIdentity = true;
        }

        var currentDisplayName = interfaceObject["displayName"]?.GetValue<string>() ?? string.Empty;
        if (!string.Equals(currentDisplayName, expectedMarketplaceDisplayName, StringComparison.Ordinal))
        {
            interfaceObject["displayName"] = expectedMarketplaceDisplayName;
            updatedMarketplaceIdentity = true;
        }

        var plugins = marketplaceObject["plugins"] as JsonArray;
        if (plugins is null)
        {
            plugins = [];
            marketplaceObject["plugins"] = plugins;
            marketplaceChanged = true;
        }

        var matchingEntries = plugins
            .Select(node => node as JsonObject)
            .Where(IsAnarchyPluginEntry)
            .ToList();

        var existingEntry = matchingEntries
            .FirstOrDefault(node => string.Equals(node?["name"]?.GetValue<string>(), expectedPluginName, StringComparison.Ordinal));
        if (existingEntry is null && matchingEntries.Count > 0)
        {
            existingEntry = matchingEntries[0];
        }

        if (existingEntry is null)
        {
            plugins.Add(CreateAnarchyPluginEntry(installScope, repoRoot));
            actionsTaken.Add("created_anarchy_ai_marketplace_entry");
            marketplaceChanged = true;
        }
        else
        {
            var replacementEntry = CreateAnarchyPluginEntry(installScope, repoRoot);
            if (!JsonNode.DeepEquals(existingEntry, replacementEntry))
            {
                existingEntry.Clear();
                foreach (var property in replacementEntry)
                {
                    existingEntry[property.Key] = property.Value?.DeepClone();
                }

                actionsTaken.Add("updated_anarchy_ai_marketplace_entry");
                marketplaceChanged = true;
            }
        }

        foreach (var duplicateEntry in matchingEntries)
        {
            if (!ReferenceEquals(duplicateEntry, existingEntry))
            {
                plugins.Remove(duplicateEntry);
                actionsTaken.Add("removed_stale_anarchy_ai_marketplace_entry");
                marketplaceChanged = true;
            }
        }

        if (updatedMarketplaceIdentity)
        {
            actionsTaken.Add(installScope == InstallScope.UserProfile
                ? "updated_user_profile_marketplace_identity"
                : "updated_repo_marketplace_identity");
            marketplaceChanged = true;
        }

        if (marketplaceChanged)
        {
            File.WriteAllText(marketplacePath, marketplaceObject.ToJsonString(ProgramJson.Options));
        }
    }

    // Purpose: Realigns the plugin-local .mcp.json declaration with the current runtime command contract.
    // Expected input: .mcp.json path and mutable action log.
    // Expected output: No direct return value; rewrites the declaration when it is missing or stale.
    // Critical dependencies: BuildMcpServerName, GeneratedAnarchyPathCanon runtime references, and JSON file writes.
    private static void EnsurePluginMcpConfiguration(string mcpPath, HashSet<string> actionsTaken)
    {
        var expectedServerName = BuildMcpServerName();
        var updatedMcpIdentity = false;
        JsonObject root;
        if (File.Exists(mcpPath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(mcpPath))?.AsObject()
                    ?? new JsonObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
                actionsTaken.Add("replaced_invalid_mcp_declaration");
                updatedMcpIdentity = true;
            }
        }
        else
        {
            root = new JsonObject();
            updatedMcpIdentity = true;
        }

        var mcpServers = root["mcpServers"] as JsonObject;
        if (mcpServers is null)
        {
            mcpServers = new JsonObject();
            root["mcpServers"] = mcpServers;
            updatedMcpIdentity = true;
        }

        var existingServer = mcpServers[expectedServerName] as JsonObject;
        if (existingServer is null || mcpServers.Count != 1)
        {
            existingServer = new JsonObject();
            updatedMcpIdentity = true;
        }

        if (!string.Equals(existingServer["command"]?.GetValue<string>(), GeneratedAnarchyPathCanon.BundleRuntimeWindowsCommandRelativePath, StringComparison.Ordinal) ||
            existingServer["args"] is not JsonArray ||
            !string.Equals(existingServer["cwd"]?.GetValue<string>(), GeneratedAnarchyPathCanon.BundleRuntimeWorkingDirectoryRelativePath, StringComparison.Ordinal))
        {
            updatedMcpIdentity = true;
        }

        existingServer["command"] = GeneratedAnarchyPathCanon.BundleRuntimeWindowsCommandRelativePath;
        existingServer["args"] = new JsonArray();
        existingServer["cwd"] = GeneratedAnarchyPathCanon.BundleRuntimeWorkingDirectoryRelativePath;

        if (updatedMcpIdentity)
        {
            root["mcpServers"] = new JsonObject
            {
                [expectedServerName] = existingServer
            };

            File.WriteAllText(mcpPath, root.ToJsonString(ProgramJson.Options));
            actionsTaken.Add("updated_mcp_server_identity");
        }
    }

    // Purpose: Builds a fresh marketplace object when the destination marketplace is missing or invalid.
    // Expected input: Install scope and repo root context.
    // Expected output: A default marketplace JSON object with the correct identity and an empty plugin list.
    // Critical dependencies: BuildMarketplaceName and BuildMarketplaceDisplayName.
    private static JsonObject CreateDefaultMarketplace(InstallScope installScope, string repoRoot)
    {
        return new JsonObject
        {
            ["name"] = BuildMarketplaceName(installScope, repoRoot),
            ["interface"] = new JsonObject
            {
                ["displayName"] = BuildMarketplaceDisplayName(installScope, repoRoot)
            },
            ["plugins"] = new JsonArray()
        };
    }

    // Purpose: Builds the canonical marketplace entry for the current Anarchy-AI install lane.
    // Expected input: Install scope and repo root context.
    // Expected output: A JSON object representing the expected plugin entry.
    // Critical dependencies: BuildPluginName and BuildPluginRelativePath.
    private static JsonObject CreateAnarchyPluginEntry(InstallScope installScope, string repoRoot)
    {
        return new JsonObject
        {
            ["name"] = BuildPluginName(installScope, repoRoot),
            ["source"] = new JsonObject
            {
                ["source"] = "local",
                ["path"] = BuildPluginRelativePath(installScope, repoRoot)
            },
            ["policy"] = new JsonObject
            {
                ["installation"] = "INSTALLED_BY_DEFAULT",
                ["authentication"] = "ON_INSTALL"
            },
            ["category"] = "Productivity"
        };
    }

    // Purpose: Inspects a marketplace file and classifies whether the current Anarchy-AI entry is present and aligned.
    // Expected input: Marketplace path, install scope, and repo root context.
    // Expected output: A MarketplaceInspection record summarizing JSON validity, entry presence, and identity drift.
    // Critical dependencies: BuildPluginName, BuildPluginRelativePath, BuildMarketplaceName, and IsAnarchyPluginEntry.
    private static MarketplaceInspection InspectMarketplace(string marketplacePath, InstallScope installScope, string repoRoot)
    {
        if (!File.Exists(marketplacePath))
        {
            return new MarketplaceInspection(false, false, false, false, false, false, []);
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(marketplacePath))?.AsObject();
            var pluginsArray = root?["plugins"] as JsonArray;
            if (pluginsArray is null)
            {
                return new MarketplaceInspection(true, false, false, false, false, true, []);
            }

            var marketplaceIdentityAligned = string.Equals(
                root?["name"]?.GetValue<string>(),
                BuildMarketplaceName(installScope, repoRoot),
                StringComparison.Ordinal);

            var expectedPluginName = BuildPluginName(installScope, repoRoot);
            var expectedPluginPath = BuildPluginRelativePath(installScope, repoRoot);
            var expectedEntry = pluginsArray
                .Select(node => node as JsonObject)
                .FirstOrDefault(node =>
                    string.Equals(node?["name"]?.GetValue<string>(), expectedPluginName, StringComparison.Ordinal) &&
                    string.Equals(node?["source"]?["path"]?.GetValue<string>(), expectedPluginPath, StringComparison.Ordinal));
            var legacyEntry = pluginsArray
                .Select(node => node as JsonObject)
                .FirstOrDefault(IsAnarchyPluginEntry);

            var effectiveEntry = expectedEntry ?? legacyEntry;
            var hasEntry = effectiveEntry is not null;
            var installedByDefault = string.Equals(
                effectiveEntry?["policy"]?["installation"]?.GetValue<string>(),
                "INSTALLED_BY_DEFAULT",
                StringComparison.Ordinal);

            var findings = new List<string>();
            if (!marketplaceIdentityAligned)
            {
                findings.Add("repo_marketplace_identity_outdated");
            }

            if (expectedEntry is null && legacyEntry is not null)
            {
                findings.Add("repo_plugin_identity_outdated");
            }

            return new MarketplaceInspection(true, true, hasEntry, installedByDefault, marketplaceIdentityAligned, true, findings.ToArray());
        }
        catch (JsonException)
        {
            return new MarketplaceInspection(true, false, false, false, false, false, []);
        }
    }

    // Purpose: Inspects the plugin manifest and classifies whether its identity matches the expected install lane.
    // Expected input: Plugin manifest path, install scope, and repo root context.
    // Expected output: A PluginManifestInspection record summarizing existence, JSON validity, and identity alignment.
    // Critical dependencies: BuildPluginName and JSON parsing.
    private static PluginManifestInspection InspectPluginManifest(string pluginManifestPath, InstallScope installScope, string repoRoot)
    {
        if (!File.Exists(pluginManifestPath))
        {
            return new PluginManifestInspection(false, false, false, []);
        }

        try
        {
            var manifest = JsonNode.Parse(File.ReadAllText(pluginManifestPath))?.AsObject();
            var identityAligned = string.Equals(
                manifest?["name"]?.GetValue<string>(),
                BuildPluginName(installScope, repoRoot),
                StringComparison.Ordinal);

            return new PluginManifestInspection(
                true,
                true,
                identityAligned,
                identityAligned ? [] : ["repo_plugin_identity_outdated"]);
        }
        catch (JsonException)
        {
            return new PluginManifestInspection(true, false, false, ["repo_plugin_identity_outdated"]);
        }
    }

    // Purpose: Inspects the plugin-local .mcp.json declaration and checks whether its server identity is aligned.
    // Expected input: .mcp.json path.
    // Expected output: An McpConfigurationInspection record summarizing existence, JSON validity, and identity alignment.
    // Critical dependencies: BuildMcpServerName and JSON parsing.
    private static McpConfigurationInspection InspectMcpConfiguration(string mcpPath)
    {
        if (!File.Exists(mcpPath))
        {
            return new McpConfigurationInspection(false, false, false, []);
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(mcpPath))?.AsObject();
            var mcpServers = root?["mcpServers"] as JsonObject;
            if (mcpServers is null)
            {
                return new McpConfigurationInspection(true, false, true, ["repo_mcp_server_identity_outdated"]);
            }

            var identityAligned = mcpServers.ContainsKey(BuildMcpServerName()) && mcpServers.Count == 1;
            var findings = identityAligned
                ? Array.Empty<string>()
                : ["repo_mcp_server_identity_outdated"];

            return new McpConfigurationInspection(true, identityAligned, true, findings);
        }
        catch (JsonException)
        {
            return new McpConfigurationInspection(true, false, false, ["repo_mcp_server_identity_outdated"]);
        }
    }

    // Purpose: Measures whether selected Claude host integrations actually point at the runtime payload.
    // Expected input: Selected host-target flags and the absolute runtime executable path.
    // Expected output: Ready/not-ready plus bounded findings and suggested repairs for missing or stale host configs.
    // Critical dependencies: Claude host config locations and the mcpServers entry shape.
    private static HostConfigInspection InspectSelectedHostConfigs(HostTargets hostTargets, string runtimePath)
    {
        var findings = new List<string>();
        var safeRepairs = new List<string>();

        if (hostTargets.HasFlag(HostTargets.ClaudeCode))
        {
            var claudeCode = InspectClaudeHostConfigFile(
                ClaudeCodeUserScopeLane.GetUserScopeConfigPath(),
                runtimePath,
                missingFinding: "claude_code_user_scope_registration_missing",
                staleFinding: "claude_code_user_scope_registration_stale",
                invalidFinding: "claude_code_user_scope_config_invalid",
                safeRepair: "run_harness_install_with_claude_code");
            findings.AddRange(claudeCode.Findings);
            safeRepairs.AddRange(claudeCode.SafeRepairs);
        }

        if (hostTargets.HasFlag(HostTargets.ClaudeDesktop))
        {
            var installKind = ClaudeDesktopInstallDetector.Detect();
            var configPath = ClaudeDesktopInstallDetector.ResolveActiveConfigPath(installKind);
            if (configPath is null)
            {
                findings.Add("claude_desktop_install_not_detected");
                safeRepairs.Add("install_or_launch_claude_desktop_then_rerun_user_profile_install");
            }
            else
            {
                var claudeDesktop = InspectClaudeHostConfigFile(
                    configPath,
                    runtimePath,
                    missingFinding: "claude_desktop_registration_missing",
                    staleFinding: "claude_desktop_registration_stale",
                    invalidFinding: "claude_desktop_config_invalid",
                    safeRepair: "run_harness_install_with_claude_desktop");
                findings.AddRange(claudeDesktop.Findings);
                safeRepairs.AddRange(claudeDesktop.SafeRepairs);
            }
        }

        return new HostConfigInspection(
            Ready: findings.Count == 0,
            Findings: findings.Distinct(StringComparer.Ordinal).ToArray(),
            SafeRepairs: safeRepairs.Distinct(StringComparer.Ordinal).ToArray());
    }

    // Purpose: Checks one Claude host config for a matching Anarchy mcpServers entry.
    // Expected input: Config path, expected runtime path, and host-specific finding/repair labels.
    // Expected output: Ready only when mcpServers.anarchy-ai exists and its command matches the expected runtime path.
    // Critical dependencies: ClaudeHostConfigWriter tolerant parser and EntryMatchesCommand.
    internal static HostConfigInspection InspectClaudeHostConfigFile(
        string configPath,
        string runtimePath,
        string missingFinding,
        string staleFinding,
        string invalidFinding,
        string safeRepair)
    {
        if (!File.Exists(configPath))
        {
            return new HostConfigInspection(false, [missingFinding], [safeRepair]);
        }

        var (root, fileExistedAndParsed) = ClaudeHostConfigWriter.ReadTolerant(configPath);
        if (!fileExistedAndParsed)
        {
            return new HostConfigInspection(false, [invalidFinding], [safeRepair]);
        }

        if (root["mcpServers"] is not JsonObject mcpServers
            || mcpServers[BuildMcpServerName()] is not JsonObject existingEntry)
        {
            return new HostConfigInspection(false, [missingFinding], [safeRepair]);
        }

        if (!ClaudeHostConfigWriter.EntryMatchesCommand(existingEntry, runtimePath))
        {
            return new HostConfigInspection(false, [staleFinding], [safeRepair]);
        }

        return HostConfigInspection.ReadyState;
    }

    // Purpose: Detects whether a marketplace JSON node represents any Anarchy-AI plugin entry, including legacy shapes.
    // Expected input: Candidate plugin JSON object.
    // Expected output: True when the node matches Anarchy-AI by name or supported source path.
    // Critical dependencies: AnarchyPathCanon.IsSupportedMarketplacePluginSourceRelativePath.
    private static bool IsAnarchyPluginEntry(JsonObject? pluginNode)
    {
        if (pluginNode is null)
        {
            return false;
        }

        var pluginName = pluginNode["name"]?.GetValue<string>() ?? string.Empty;
        if (AnarchyPathCanon.IsOwnedPluginName(pluginName))
        {
            return true;
        }

        var pluginPath = pluginNode["source"]?["path"]?.GetValue<string>() ?? string.Empty;
        return AnarchyPathCanon.IsSupportedMarketplacePluginSourceRelativePath(pluginPath);
    }

    // Purpose: Builds the stable plugin identity written into plugin manifests, marketplace entries, and MCP names.
    // Expected input: Install scope and optional repo root retained for signature consistency.
    // Expected output: The public Anarchy-AI plugin name shared across install lanes.
    // Critical dependencies: GeneratedAnarchyPathCanon default plugin name.
    internal static string BuildPluginName(InstallScope installScope, string? repoRoot)
    {
        return GeneratedAnarchyPathCanon.DefaultPluginName;
    }

    // Purpose: Builds the install-lane-specific plugin directory name used for filesystem materialization.
    // Expected input: Install scope and optional repo root retained for signature consistency.
    // Expected output: The shared bundle directory name used beneath the selected install root.
    // Critical dependencies: GeneratedAnarchyPathCanon templates.
    internal static string BuildPluginDirectoryName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return GeneratedAnarchyPathCanon.DefaultPluginName;
        }

        return GeneratedAnarchyPathCanon.RepoScopedPluginDirectoryNameTemplate;
    }

    // Purpose: Builds the marketplace-relative source.path used to point at the selected plugin directory.
    // Expected input: Install scope and optional repo root context.
    // Expected output: A marketplace-relative source.path for repo-local or user-profile registration.
    // Critical dependencies: AnarchyPathCanon.BuildMarketplacePluginSourceRelativePath and BuildPluginDirectoryName.
    internal static string BuildPluginRelativePath(InstallScope installScope, string? repoRoot)
    {
        return AnarchyPathCanon.BuildMarketplacePluginSourceRelativePath(
            installScope == InstallScope.UserProfile,
            BuildPluginDirectoryName(installScope, repoRoot));
    }

    // Purpose: Returns the plugin-local MCP server name expected in .mcp.json.
    // Expected input: None.
    // Expected output: The stable Anarchy-AI MCP server name.
    // Critical dependencies: GeneratedAnarchyPathCanon default plugin name.
    private static string BuildMcpServerName()
    {
        return GeneratedAnarchyPathCanon.DefaultPluginName;
    }

    // Purpose: Builds the regex used to detect owned custom-MCP blocks in Codex config.
    // Expected input: None.
    // Expected output: One multiline regex that matches any current or legacy Anarchy-AI custom-MCP block.
    // Critical dependencies: AnarchyPathCanon.OwnedMcpServerNames and Regex.Escape.
    private static string BuildOwnedCodexCustomMcpServerBlockPattern()
    {
        var escapedNames = AnarchyPathCanon.OwnedMcpServerNames
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(Regex.Escape);
        var nameAlternation = string.Join("|", escapedNames);
        return $@"(?ms)^\[mcp_servers\.(?:{nameAlternation})\]\r?\n(?:.*?\r?\n)*(?=^\[|\z)";
    }

    // Purpose: Builds the marketplace identity name for the selected install lane.
    // Expected input: Install scope and optional repo root used for repo-scoped naming.
    // Expected output: The user-profile marketplace name or a repo-scoped marketplace name derived from the repo slug.
    // Critical dependencies: GeneratedAnarchyPathCanon templates and NormalizeMarketplaceSlug.
    internal static string BuildMarketplaceName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return GeneratedAnarchyPathCanon.UserProfileMarketplaceName;
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return GeneratedAnarchyPathCanon.RepoScopedMarketplaceNameTemplate;
        }

        var repoName = new DirectoryInfo(repoRoot).Name;
        var slug = NormalizeMarketplaceSlug(repoName);
        return GeneratedAnarchyPathCanon.RepoScopedMarketplaceNameTemplate
            .Replace("<repo-slug>", slug, StringComparison.Ordinal);
    }

    // Purpose: Builds the human-facing marketplace display name for the selected install lane.
    // Expected input: Install scope and optional repo root.
    // Expected output: A display name suitable for marketplace.json.
    // Critical dependencies: Repo directory naming and the current display-name contract.
    private static string BuildMarketplaceDisplayName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return AnarchyBranding.UserProfileMarketplaceDisplayName;
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return AnarchyBranding.BuildRepoLocalMarketplaceDisplayName(null);
        }

        var repoName = new DirectoryInfo(repoRoot).Name.Trim();
        if (string.IsNullOrWhiteSpace(repoName))
        {
            repoName = "Repo";
        }

        return AnarchyBranding.BuildRepoLocalMarketplaceDisplayName(repoName);
    }

    // Purpose: Resolves the absolute plugin root for the selected install lane.
    // Expected input: Install scope and repo root context.
    // Expected output: An absolute plugin root under the repo or user profile.
    // Critical dependencies: AnarchyPathCanon and BuildPluginDirectoryName.
    internal static string ResolvePluginRoot(InstallScope installScope, string repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? AnarchyPathCanon.ResolveUserProfilePluginRoot(GetUserProfileDirectory(), BuildPluginDirectoryName(installScope, repoRoot))
            : AnarchyPathCanon.ResolveRepoLocalPluginRoot(repoRoot, BuildPluginDirectoryName(installScope, repoRoot));
    }

    // Purpose: Detects the AI-Links repo-authored plugin bundle when setup is assessing the source repo itself.
    // Expected input: A workspace root that may be the AI-Links source repo.
    // Expected output: A bounded inspection of plugins/anarchy-ai without treating it as a consumer install target.
    // Critical dependencies: AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath and the current bundle-surface contract.
    internal static SourceAuthoringBundleInspection InspectSourceAuthoringBundle(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            return SourceAuthoringBundleInspection.Empty;
        }

        if (!LooksLikeAnarchySourceRepo(workspaceRoot))
        {
            return SourceAuthoringBundleInspection.Empty;
        }

        var sourcePluginRoot = AnarchyPathCanon.ResolveSourcePluginDirectory(workspaceRoot);
        if (!Directory.Exists(sourcePluginRoot))
        {
            return SourceAuthoringBundleInspection.Empty with { PluginRoot = sourcePluginRoot };
        }

        var pluginManifestExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(sourcePluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath));
        var mcpExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(sourcePluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath));
        var runtimeExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(sourcePluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath));
        var skillExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(sourcePluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath));
        var schemaManifestExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(sourcePluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath));
        var missingContracts = CoreContractFiles
            .Where(contractFile =>
            {
                var contractPath = AnarchyPathCanon.ResolveBundleFilePath(
                    sourcePluginRoot,
                    AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, contractFile));
                return !File.Exists(contractPath);
            })
            .ToArray();
        var experimentalContractExists = File.Exists(AnarchyPathCanon.ResolveBundleFilePath(
            sourcePluginRoot,
            AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, ExperimentalDirectionAssistContract)));
        var markerPresent = pluginManifestExists || mcpExists || runtimeExists || skillExists || schemaManifestExists || missingContracts.Length < CoreContractFiles.Length;

        return new SourceAuthoringBundleInspection(
            Present: markerPresent,
            PluginRoot: sourcePluginRoot,
            PluginManifestExists: pluginManifestExists,
            McpExists: mcpExists,
            RuntimeExists: runtimeExists,
            SkillExists: skillExists,
            SchemaManifestExists: schemaManifestExists,
            MissingCoreContracts: missingContracts,
            ExperimentalContractExists: experimentalContractExists);
    }

    // Purpose: Keeps source-authoring detection read-only so AI-Links does not become its own consumer install by accident.
    // Expected input: Setup options and source bundle inspection facts.
    // Expected output: True only for repo-local assess/status runs against a repo-authored source bundle.
    // Critical dependencies: the AI-Links authoring boundary in AGENTS.md and source-bundle path canon.
    private static bool ShouldInspectSourceAuthoringBundle(SetupOptions options, SourceAuthoringBundleInspection sourceAuthoringBundle)
    {
        return options.InstallScope == InstallScope.RepoLocal
            && (options.Mode == OperationMode.Assess || options.Mode == OperationMode.Status)
            && sourceAuthoringBundle.Present;
    }

    // Purpose: Blocks repo-local install/update from overwriting AI-Links' source-authored plugin bundle.
    // Expected input: Setup options and source-authoring detection facts.
    // Expected output: True when a write operation should return bounded guidance instead of mutating source truth.
    // Critical dependencies: LooksLikeAnarchySourceRepo and the plain repo-local plugins/anarchy-ai destination path.
    private static bool ShouldBlockSourceAuthoringConsumerWrite(SetupOptions options, SourceAuthoringBundleInspection sourceAuthoringBundle)
    {
        return options.InstallScope == InstallScope.RepoLocal
            && options.Mode is OperationMode.Install or OperationMode.Update or OperationMode.Underlay or OperationMode.Refresh
            && sourceAuthoringBundle.Present;
    }

    // Purpose: Distinguishes the AI-Links source repo from a consumer repo that has a plain plugins/anarchy-ai installed bundle.
    // Expected input: Candidate workspace root.
    // Expected output: True only when repo-authored harness/source markers are present.
    // Critical dependencies: source-authoring boundary and stable source tree markers.
    private static bool LooksLikeAnarchySourceRepo(string workspaceRoot)
    {
        return File.Exists(Path.Combine(workspaceRoot, "harness", "setup", "dotnet", "AnarchyAi.Setup.csproj"))
            && File.Exists(Path.Combine(workspaceRoot, "harness", "server", "dotnet", "AnarchyAi.Mcp.Server.csproj"))
            && File.Exists(Path.Combine(workspaceRoot, "docs", "README_ai_links.md"));
    }

    // Purpose: Resolves the marketplace file path for the selected install lane.
    // Expected input: Install scope and repo root context.
    // Expected output: The absolute repo-local or user-profile marketplace file path.
    // Critical dependencies: AnarchyPathCanon and GetUserProfileDirectory.
    private static string ResolveMarketplacePath(InstallScope installScope, string repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? AnarchyPathCanon.ResolveUserProfileMarketplaceFilePath(GetUserProfileDirectory())
            : AnarchyPathCanon.ResolveRepoLocalMarketplaceFilePath(repoRoot);
    }

    // Purpose: Returns the current Windows user-profile directory used for home-local installs.
    // Expected input: None.
    // Expected output: Absolute user-profile path for the current process.
    // Critical dependencies: Environment.SpecialFolder.UserProfile.
    private static string GetUserProfileDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    // Purpose: Reads Codex's host-owned plugin cache for the selected marketplace/plugin identity without treating it as install truth.
    // Expected input: Install scope, workspace root, and the setup-owned source plugin manifest path.
    // Expected output: Cache entries and findings that show whether Codex has materialized the source manifest version.
    // Critical dependencies: AnarchyPathCanon.UserProfilePluginCacheParentDirectoryRelativePath and current Codex cache layout.
    internal static CodexMaterializationReport InspectCodexMaterialization(
        InstallScope installScope,
        string workspaceRoot,
        string sourcePluginManifestPath)
    {
        var marketplaceName = BuildMarketplaceName(installScope, workspaceRoot);
        var pluginName = BuildPluginName(installScope, workspaceRoot);
        var configPluginKey = $"{pluginName}@{marketplaceName}";
        var codexConfigPath = AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory());
        var codexPluginEnabled = TryReadCodexPluginEnabled(codexConfigPath, configPluginKey);
        var pluginCacheParent = AnarchyPathCanon.ResolveRelativePath(
            GetUserProfileDirectory(),
            AnarchyPathCanon.UserProfilePluginCacheParentDirectoryRelativePath);
        var expectedCacheRoot = Path.Combine(
            pluginCacheParent,
            marketplaceName,
            pluginName);
        var sourceVersion = ReadPluginManifestVersion(sourcePluginManifestPath);
        var cacheEntries = Directory.Exists(expectedCacheRoot)
            ? Directory.EnumerateDirectories(expectedCacheRoot)
                .Select(Path.GetFileName)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Select(static name => name!)
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];
        var sourceVersionPresent = !string.IsNullOrWhiteSpace(sourceVersion)
            && cacheEntries.Any(entry => string.Equals(entry, sourceVersion, StringComparison.OrdinalIgnoreCase));
        var findings = new List<string>();

        if (!Directory.Exists(expectedCacheRoot))
        {
            findings.Add("codex_cache_root_missing");
        }
        else if (cacheEntries.Length == 0)
        {
            findings.Add("codex_cache_root_empty");
        }

        if (!string.IsNullOrWhiteSpace(sourceVersion) && !sourceVersionPresent)
        {
            findings.Add("source_plugin_version_not_materialized_in_codex_cache");
        }

        if (codexPluginEnabled is null)
        {
            findings.Add("codex_plugin_enable_state_missing");
        }
        else if (codexPluginEnabled == false)
        {
            findings.Add("codex_plugin_disabled");
        }

        return new CodexMaterializationReport
        {
            marketplace_name = marketplaceName,
            plugin_name = pluginName,
            codex_config_path = codexConfigPath,
            config_plugin_key = configPluginKey,
            codex_plugin_enabled = codexPluginEnabled,
            expected_cache_root = expectedCacheRoot,
            expected_cache_root_present = Directory.Exists(expectedCacheRoot),
            source_plugin_manifest_version = sourceVersion,
            cache_entries = cacheEntries,
            newest_cache_entry = cacheEntries.LastOrDefault(),
            source_version_present_in_cache = sourceVersionPresent,
            findings = findings.ToArray()
        };
    }

    // Purpose: Reads Codex's plugin enable-state for a marketplace/plugin key when Codex stores that state in config.toml.
    // Expected input: Absolute config.toml path and a key shaped as plugin@marketplace.
    // Expected output: True/false when the key is present with enabled value, otherwise null.
    // Critical dependencies: Current Codex config section shape: [plugins."plugin@marketplace"].
    private static bool? TryReadCodexPluginEnabled(string codexConfigPath, string pluginConfigKey)
    {
        if (!File.Exists(codexConfigPath))
        {
            return null;
        }

        var configText = File.ReadAllText(codexConfigPath);
        var sectionPattern = $@"(?ms)^\[plugins\.""{Regex.Escape(pluginConfigKey)}""\]\s*(?<body>.*?)(?=^\[|\z)";
        var sectionMatch = Regex.Match(configText, sectionPattern, RegexOptions.CultureInvariant);
        if (!sectionMatch.Success)
        {
            return null;
        }

        var enabledMatch = Regex.Match(
            sectionMatch.Groups["body"].Value,
            @"(?mi)^\s*enabled\s*=\s*(?<value>true|false)\s*$",
            RegexOptions.CultureInvariant);
        return enabledMatch.Success
            ? bool.Parse(enabledMatch.Groups["value"].Value)
            : null;
    }

    private static CodexDuplicateLaneResult UpdateCodexPrimaryLaneForSelectedInstall(
        SetupOptions options,
        string workspaceRoot,
        HashSet<string> actionsTaken)
    {
        var codexConfigPath = AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory());
        var selectedKey = $"{BuildPluginName(options.InstallScope, workspaceRoot)}@{BuildMarketplaceName(options.InstallScope, workspaceRoot)}";
        var configText = File.Exists(codexConfigPath)
            ? File.ReadAllText(codexConfigPath)
            : string.Empty;
        var (updatedText, disabledLanes, duplicateDetected, selectedLaneEnabled) = ReconcileAnarchyCodexLanesInConfigText(configText, selectedKey);

        if (!string.Equals(updatedText, configText, StringComparison.Ordinal))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(codexConfigPath)!);
            File.WriteAllText(codexConfigPath, updatedText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        if (selectedLaneEnabled)
        {
            actionsTaken.Add("enabled_selected_anarchy_codex_plugin_lane");
        }

        if (disabledLanes.Length > 0)
        {
            actionsTaken.Add("disabled_duplicate_anarchy_codex_plugin_lanes");
        }

        return new CodexDuplicateLaneResult(
            selectedKey,
            disabledLanes,
            duplicateDetected,
            selectedLaneEnabled);
    }

    internal static (string UpdatedText, string[] DisabledLanes, bool DuplicateDetected) DisableDuplicateCodexLanesInConfigText(
        string configText,
        string selectedKey)
    {
        var (updatedText, disabledLanes, duplicateDetected, _) = ReconcileAnarchyCodexLanesInConfigText(configText, selectedKey);
        return (updatedText, disabledLanes, duplicateDetected);
    }

    internal static (string UpdatedText, string[] DisabledLanes, bool DuplicateDetected, bool SelectedLaneEnabled) ReconcileAnarchyCodexLanesInConfigText(
        string configText,
        string selectedKey)
    {
        var disabled = new List<string>();
        var duplicateDetected = false;
        var selectedLaneEnabled = false;
        var selectedLaneSeen = false;
        var sectionPattern = @"(?ms)^\[plugins\.""(?<key>[^""]+)""\]\s*(?<body>.*?)(?=^\[|\z)";
        var updatedText = Regex.Replace(
            configText,
            sectionPattern,
            match =>
            {
                var key = match.Groups["key"].Value;
                if (!IsOwnedAnarchyCodexPluginConfigKey(key))
                {
                    return match.Value;
                }

                var body = match.Groups["body"].Value;
                var enabled = ReadEnabledFromPluginConfigSectionBody(body);
                if (string.Equals(key, selectedKey, StringComparison.Ordinal))
                {
                    selectedLaneSeen = true;
                    if (enabled == true)
                    {
                        return match.Value;
                    }

                    selectedLaneEnabled = true;
                    var selectedBody = SetEnabledInPluginConfigSectionBody(body, true);
                    return match.Value[..(match.Groups["body"].Index - match.Index)] + selectedBody;
                }

                if (enabled == false)
                {
                    return match.Value;
                }

                duplicateDetected = true;
                disabled.Add(key);
                var replacedBody = SetEnabledInPluginConfigSectionBody(body, false);
                return match.Value[..(match.Groups["body"].Index - match.Index)] + replacedBody;
            },
            RegexOptions.CultureInvariant);

        if (!selectedLaneSeen)
        {
            selectedLaneEnabled = true;
            var separator = string.IsNullOrWhiteSpace(updatedText)
                ? string.Empty
                : updatedText.EndsWith('\n') ? Environment.NewLine : Environment.NewLine + Environment.NewLine;
            updatedText += separator + $"[plugins.\"{selectedKey}\"]" + Environment.NewLine + "enabled = true" + Environment.NewLine;
        }

        return (updatedText, disabled.OrderBy(static value => value, StringComparer.Ordinal).ToArray(), duplicateDetected, selectedLaneEnabled);
    }

    private static string SetEnabledInPluginConfigSectionBody(string body, bool enabled)
    {
        var enabledText = enabled ? "true" : "false";
        var replacedBody = Regex.Replace(
            body,
            @"(?mi)^(?<prefix>\s*enabled\s*=\s*)(true|false)(?<suffix>\s*)$",
            "${prefix}" + enabledText + "${suffix}",
            RegexOptions.CultureInvariant);
        if (!string.Equals(replacedBody, body, StringComparison.Ordinal))
        {
            return replacedBody;
        }

        return body.EndsWith('\n')
            ? body + $"enabled = {enabledText}" + Environment.NewLine
            : body + Environment.NewLine + $"enabled = {enabledText}" + Environment.NewLine;
    }

    private static bool IsOwnedAnarchyCodexPluginConfigKey(string configKey)
    {
        var parts = configKey.Split('@', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        return AnarchyPathCanon.IsOwnedPluginName(parts[0])
            && AnarchyPathCanon.IsOwnedMarketplaceName(parts[1]);
    }

    private static bool? ReadEnabledFromPluginConfigSectionBody(string body)
    {
        var enabledMatch = Regex.Match(
            body,
            @"(?mi)^\s*enabled\s*=\s*(?<value>true|false)\s*$",
            RegexOptions.CultureInvariant);
        return enabledMatch.Success
            ? bool.Parse(enabledMatch.Groups["value"].Value)
            : null;
    }

    // Purpose: Reads the version field from a plugin manifest when the installed source bundle exposes one.
    // Expected input: Absolute path to .codex-plugin/plugin.json.
    // Expected output: Version string or null when unavailable/unparseable.
    // Critical dependencies: Codex plugin manifest JSON shape.
    private static string? ReadPluginManifestVersion(string pluginManifestPath)
    {
        if (!File.Exists(pluginManifestPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(pluginManifestPath));
            return document.RootElement.TryGetProperty("version", out var versionElement)
                ? versionElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    // Purpose: Builds the install-root label shown in setup help and disclosure text.
    // Expected input: Install scope.
    // Expected output: A home-local label for user-profile installs or a placeholder for repo-local selection.
    // Critical dependencies: AnarchyPathCanon label helpers and the current lane wording.
    internal static string BuildInstallRootLabel(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? AnarchyPathCanon.BuildHomeLabelPath(AnarchyPathCanon.UserProfileInstallRootDirectoryRelativePath)
            : "<selected repo>";
    }

    // Purpose: Builds the human-facing plugin-folder label for the selected install lane.
    // Expected input: Install scope and optional repo root context.
    // Expected output: A repo-relative or home-relative plugin-folder label.
    // Critical dependencies: BuildPluginDirectoryName and AnarchyPathCanon label builders.
    internal static string BuildPluginFolderLabel(InstallScope installScope, string? repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? AnarchyPathCanon.BuildHomeLabelPath(
                AnarchyPathCanon.CombineCanonRelativePath(
                    AnarchyPathCanon.UserProfilePluginParentDirectoryRelativePath,
                    BuildPluginDirectoryName(installScope, repoRoot)))
            : AnarchyPathCanon.BuildRepoLabelPath(
                AnarchyPathCanon.CombineCanonRelativePath(
                    AnarchyPathCanon.RepoLocalPluginParentDirectoryRelativePath,
                    BuildPluginDirectoryName(installScope, repoRoot)));
    }

    // Purpose: Builds the human-facing marketplace-path label for the selected install lane.
    // Expected input: Install scope and optional repo context.
    // Expected output: A repo-relative or home-relative marketplace-path label.
    // Critical dependencies: AnarchyPathCanon label builders and lane-specific marketplace constants.
    internal static string BuildMarketplacePathLabel(InstallScope installScope, string? repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? AnarchyPathCanon.BuildHomeLabelPath(AnarchyPathCanon.UserProfileMarketplaceFileRelativePath)
            : AnarchyPathCanon.BuildRepoLabelPath(AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath);
    }

    // Purpose: Classifies whether readiness should be described as plugin-marketplace or legacy custom-MCP fallback.
    // Expected input: Install scope, normalized host context, and legacy home-surface inspection results.
    // Expected output: A registration-mode string for setup JSON.
    // Critical dependencies: LegacyUserProfileInspection and the current Codex home-install model.
    internal static string DetermineRegistrationMode(
        InstallScope installScope,
        string normalizedHostContext,
        HostTargets hostTargets,
        LegacyUserProfileInspection legacyUserProfileInspection)
    {
        if (!hostTargets.HasFlag(HostTargets.Codex)
            && (hostTargets.HasFlag(HostTargets.ClaudeCode) || hostTargets.HasFlag(HostTargets.ClaudeDesktop)))
        {
            return "host_config";
        }

        if (installScope == InstallScope.UserProfile
            && string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal)
            && legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent
            && !legacyUserProfileInspection.NewPluginMarketplaceLaneReady)
        {
            return "custom_mcp_fallback";
        }

        return "plugin_marketplace";
    }

    // Purpose: Determines whether setup actions changed a host-owned configuration file.
    // Expected input: Actions taken during the setup run.
    // Expected output: True for write actions, false for no-op, skipped, or detection-only actions.
    // Critical dependencies: Claude lane action labels and setup result host_config_modified semantics.
    internal static bool DidModifyHostConfig(ISet<string> actionsTaken)
    {
        return actionsTaken.Contains("claude_code_user_scope_registration_added")
            || actionsTaken.Contains("claude_code_user_scope_registration_refreshed")
            || actionsTaken.Contains("claude_desktop_registration_added")
            || actionsTaken.Contains("claude_desktop_registration_refreshed");
    }

    // Purpose: Builds the install-lane-specific missing-marketplace finding string.
    // Expected input: Install scope.
    // Expected output: The lane-appropriate missing-marketplace finding code.
    // Critical dependencies: The current setup findings vocabulary.
    private static string BuildMarketplaceMissingFinding(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? "user_profile_marketplace_missing"
            : "repo_marketplace_missing";
    }

    // Purpose: Builds the install-lane-specific missing-plugins-array finding string.
    // Expected input: Install scope.
    // Expected output: The lane-appropriate missing-plugins-array finding code.
    // Critical dependencies: The current setup findings vocabulary.
    private static string BuildMarketplaceMissingPluginsArrayFinding(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? "user_profile_marketplace_missing_plugins_array"
            : "repo_marketplace_missing_plugins_array";
    }

    // Purpose: Resolves the setup-owned lifecycle state file inside the installed plugin bundle.
    // Expected input: Absolute plugin root.
    // Expected output: Absolute path to the versioned install-state JSON file.
    // Critical dependencies: InstallStateFileRelativePath and plugin-root resolution.
    internal static string ResolveInstallStatePath(string pluginRoot)
    {
        return AnarchyPathCanon.ResolveRelativePath(pluginRoot, InstallStateFileRelativePath);
    }

    private static InstallStateReport BuildRuntimeFreeInstallStateReport()
    {
        return new InstallStateReport
        {
            schema_version = InstallStateSchemaVersion,
            state_path = string.Empty,
            state_present = false,
            state_written = false,
            state_valid = true,
            findings = [],
            warnings = []
        };
    }

    // Purpose: Writes durable setup lifecycle state after an install or update materializes owned surfaces.
    // Expected input: Current setup options, resolved paths, and the mutable action log.
    // Expected output: No direct return value; writes install-state JSON and records an action marker.
    // Critical dependencies: ProgramJson, UTF-8 without BOM, and later InspectInstallState validation.
    private static void WriteInstallState(
        SetupOptions options,
        string normalizedHostContext,
        string workspaceRoot,
        string pluginRoot,
        string marketplacePath,
        string runtimePath,
        string updateSourceRoot,
        HashSet<string> actionsTaken)
    {
        var statePath = ResolveInstallStatePath(pluginRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        var installScopeLabel = BuildInstallScopeJsonLabel(options.InstallScope);
        var targetRoot = ResolveInstallTargetRoot(options.InstallScope, workspaceRoot);
        var workspaceSchemaTargeted = !string.IsNullOrWhiteSpace(workspaceRoot);

        var sourceKind = options.Mode == OperationMode.Update
            ? string.IsNullOrWhiteSpace(options.UpdateSourcePath)
                ? "public_update_source"
                : "local_update_source"
            : "embedded_payload";

        var target = new JsonObject
        {
            ["id"] = BuildInstallTargetId(options.InstallScope, normalizedHostContext),
            ["kind"] = options.InstallScope == InstallScope.UserProfile ? "home" : "project",
            ["root"] = targetRoot,
            ["install_state_path"] = statePath,
            ["plugin_root"] = pluginRoot,
            ["marketplace_path"] = marketplacePath,
            ["marketplace_plugin_source"] = BuildPluginRelativePath(options.InstallScope, workspaceRoot),
            ["mcp_server_name"] = BuildMcpServerName(),
            ["runtime_path"] = runtimePath,
            ["runtime_relative_path"] = AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath
        };

        var workspace = new JsonObject
        {
            ["root"] = workspaceRoot,
            ["schema_targeted"] = workspaceSchemaTargeted,
            ["schema_refresh_requested"] = options.RefreshPortableSchemaFamily,
            ["schema_refresh_applied"] = options.RefreshPortableSchemaFamily && options.ApplyChanges
        };

        var source = new JsonObject
        {
            ["kind"] = sourceKind,
            ["root"] = updateSourceRoot,
            ["schema_claim"] = "schemas_describe_route_shape_but_setup_records_materialized_state"
        };

        var document = new JsonObject
        {
            ["schema_version"] = InstallStateSchemaVersion,
            ["written_at_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["setup_operation"] = BuildSetupOperationLabel(options.Mode),
            ["install_scope"] = installScopeLabel,
            ["host_context"] = normalizedHostContext,
            ["host_targets"] = CreateJsonStringArray(HostTargetLabels.ToLabelArray(options.HostTargets)),
            ["target"] = target,
            ["workspace"] = workspace,
            ["source"] = source,
            ["managed_operations"] = BuildInstallStateManagedOperations(
                options,
                workspaceRoot,
                pluginRoot,
                marketplacePath,
                runtimePath,
                statePath),
            // Legacy flat fields remain for older readers, but v2 validation uses target/workspace separation.
            ["workspace_root"] = workspaceRoot,
            ["plugin_name"] = BuildPluginName(options.InstallScope, workspaceRoot),
            ["plugin_directory_name"] = BuildPluginDirectoryName(options.InstallScope, workspaceRoot),
            ["plugin_root"] = pluginRoot,
            ["marketplace_name"] = BuildMarketplaceName(options.InstallScope, workspaceRoot),
            ["marketplace_path"] = marketplacePath,
            ["marketplace_plugin_source"] = BuildPluginRelativePath(options.InstallScope, workspaceRoot),
            ["mcp_server_name"] = BuildMcpServerName(),
            ["runtime_path"] = runtimePath,
            ["runtime_relative_path"] = AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath,
            ["source_kind"] = sourceKind,
            ["source_root"] = updateSourceRoot,
            ["schema_claim"] = "schemas_describe_route_shape_but_setup_records_materialized_state"
        };

        File.WriteAllText(statePath, document.ToJsonString(ProgramJson.Options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        actionsTaken.Add("wrote_install_state");
    }

    // Purpose: Names the install target independently from any workspace/schema target.
    // Expected input: Install scope plus normalized host context.
    // Expected output: Stable target adapter-style id for lifecycle records.
    // Critical dependencies: The ECC-inspired target/workspace separation in install-state v2.
    private static string BuildInstallTargetId(InstallScope installScope, string normalizedHostContext)
    {
        return installScope == InstallScope.UserProfile
            ? $"{normalizedHostContext}-user-profile"
            : $"{normalizedHostContext}-repo-local";
    }

    // Purpose: Resolves the root whose identity owns the install state.
    // Expected input: Install scope and optional workspace root.
    // Expected output: User-profile root for home installs, workspace root for repo-local installs.
    // Critical dependencies: User-profile state must not become invalid merely because a different repo is being assessed.
    private static string ResolveInstallTargetRoot(InstallScope installScope, string workspaceRoot)
    {
        return installScope == InstallScope.UserProfile
            ? GetUserProfileDirectory()
            : workspaceRoot;
    }

    // Purpose: Records the surfaces setup owns so status/doctor/repair can become operation-based instead of guess-based.
    // Expected input: Resolved install paths and optional workspace target.
    // Expected output: JSON operation records modeled after ECC's managed-operation install-state discipline.
    // Critical dependencies: Plugin bundle surface constants, marketplace registration shape, and portable schema seeding rules.
    private static JsonArray BuildInstallStateManagedOperations(
        SetupOptions options,
        string workspaceRoot,
        string pluginRoot,
        string marketplacePath,
        string runtimePath,
        string statePath)
    {
        var operations = new JsonArray
        {
            CreateInstallStateOperation(
                "materialize_plugin_bundle",
                "plugin_bundle",
                pluginRoot,
                "copy_embedded_payload",
                "managed"),
            CreateInstallStateOperation(
                "write_plugin_manifest_identity",
                "codex_plugin_manifest",
                AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                "write_json",
                "managed"),
            CreateInstallStateOperation(
                "write_mcp_server_identity",
                "mcp_declaration",
                AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath),
                "write_json",
                "managed"),
            CreateInstallStateOperation(
                "materialize_runtime",
                "mcp_runtime",
                runtimePath,
                "copy_embedded_payload",
                "managed"),
            CreateInstallStateOperation(
                "write_install_state",
                "install_state",
                statePath,
                "write_json",
                "managed")
        };

        if (options.HostTargets.HasFlag(HostTargets.Codex))
        {
            operations.Add(CreateInstallStateOperation(
                "register_marketplace_entry",
                "marketplace_registration",
                marketplacePath,
                "merge_json",
                "managed"));
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var schemaRefreshApplied = options.RefreshPortableSchemaFamily && options.ApplyChanges;
            var ownership = schemaRefreshApplied ? "managed" : "seed_if_missing";
            foreach (var schemaFile in PortableSchemaFiles)
            {
                operations.Add(CreateInstallStateOperation(
                    schemaRefreshApplied ? "refresh_portable_schema" : "seed_portable_schema_if_missing",
                    "portable_schema_family",
                    Path.Combine(workspaceRoot, schemaFile),
                    schemaRefreshApplied ? "copy_embedded_payload" : "copy_if_missing",
                    ownership,
                    schemaFile));
            }
        }

        return operations;
    }

    // Purpose: Builds one managed-operation record for install-state v2.
    // Expected input: Operation role, destination path, strategy, ownership, and optional source-relative path.
    // Expected output: JSON operation record.
    // Critical dependencies: Install-state readers that count or later replay managed operations.
    private static JsonObject CreateInstallStateOperation(
        string kind,
        string surface,
        string destinationPath,
        string strategy,
        string ownership,
        string sourceRelativePath = "")
    {
        var operation = new JsonObject
        {
            ["kind"] = kind,
            ["surface"] = surface,
            ["destination_path"] = destinationPath,
            ["strategy"] = strategy,
            ["ownership"] = ownership
        };

        if (!string.IsNullOrWhiteSpace(sourceRelativePath))
        {
            operation["source_relative_path"] = sourceRelativePath;
        }

        return operation;
    }

    // Purpose: Reads setup lifecycle state and compares recorded intent against observed destination paths.
    // Expected input: Current setup lane and resolved destination facts.
    // Expected output: An InstallStateReport with bounded findings rather than inferred plugin-trust claims.
    // Critical dependencies: ResolveInstallStatePath, setup install-state JSON fields, and path comparison rules.
    internal static InstallStateReport InspectInstallState(
        SetupOptions options,
        string normalizedHostContext,
        string workspaceRoot,
        string pluginRoot,
        string marketplacePath,
        string runtimePath,
        bool stateWritten)
    {
        var statePath = ResolveInstallStatePath(pluginRoot);
        var findings = new List<string>();
        var warnings = new List<string>();

        if (!File.Exists(statePath))
        {
            findings.Add("install_state_missing");
            return new InstallStateReport
            {
                schema_version = InstallStateSchemaVersion,
                state_path = statePath,
                state_present = false,
                state_written = stateWritten,
                state_valid = false,
                findings = findings.ToArray(),
                warnings = warnings.ToArray()
            };
        }

        JsonObject? state;
        try
        {
            state = JsonNode.Parse(File.ReadAllText(statePath)) as JsonObject;
        }
        catch (JsonException)
        {
            findings.Add("install_state_invalid_json");
            return new InstallStateReport
            {
                schema_version = InstallStateSchemaVersion,
                state_path = statePath,
                state_present = true,
                state_written = stateWritten,
                state_valid = false,
                findings = findings.ToArray(),
                warnings = warnings.ToArray()
            };
        }

        if (state is null)
        {
            findings.Add("install_state_invalid_json");
            return new InstallStateReport
            {
                schema_version = InstallStateSchemaVersion,
                state_path = statePath,
                state_present = true,
                state_written = stateWritten,
                state_valid = false,
                findings = findings.ToArray(),
                warnings = warnings.ToArray()
            };
        }

        var schemaVersion = ReadJsonString(state, "schema_version");
        var recordedInstallScope = ReadJsonString(state, "install_scope");
        var recordedHostContext = ReadJsonString(state, "host_context");
        var recordedTargetId = ReadNestedJsonString(state, "target", "id");
        var recordedTargetKind = ReadNestedJsonString(state, "target", "kind");
        var recordedTargetRoot = ReadNestedJsonString(state, "target", "root");
        var recordedInstallStatePath = ReadNestedJsonString(state, "target", "install_state_path");
        var recordedWorkspaceRoot = ReadNestedJsonString(state, "workspace", "root");
        var recordedWorkspaceSchemaTargeted = ReadNestedJsonBool(state, "workspace", "schema_targeted");
        var recordedPluginRoot = ReadNestedJsonString(state, "target", "plugin_root");
        var recordedMarketplacePath = ReadNestedJsonString(state, "target", "marketplace_path");
        var recordedRuntimePath = ReadNestedJsonString(state, "target", "runtime_path");
        var recordedMcpServerName = ReadNestedJsonString(state, "target", "mcp_server_name");
        var recordedManagedOperationCount = state["managed_operations"] is JsonArray operations
            ? operations.Count
            : (int?)null;

        if (string.IsNullOrWhiteSpace(recordedWorkspaceRoot))
        {
            recordedWorkspaceRoot = ReadJsonString(state, "workspace_root");
        }
        if (string.IsNullOrWhiteSpace(recordedPluginRoot))
        {
            recordedPluginRoot = ReadJsonString(state, "plugin_root");
        }
        if (string.IsNullOrWhiteSpace(recordedMarketplacePath))
        {
            recordedMarketplacePath = ReadJsonString(state, "marketplace_path");
        }
        if (string.IsNullOrWhiteSpace(recordedRuntimePath))
        {
            recordedRuntimePath = ReadJsonString(state, "runtime_path");
        }
        if (string.IsNullOrWhiteSpace(recordedMcpServerName))
        {
            recordedMcpServerName = ReadJsonString(state, "mcp_server_name");
        }
        if (string.IsNullOrWhiteSpace(recordedTargetRoot))
        {
            recordedTargetRoot = options.InstallScope == InstallScope.UserProfile
                ? GetUserProfileDirectory()
                : recordedWorkspaceRoot;
        }
        if (string.IsNullOrWhiteSpace(recordedTargetKind))
        {
            recordedTargetKind = options.InstallScope == InstallScope.UserProfile ? "home" : "project";
        }
        if (string.IsNullOrWhiteSpace(recordedTargetId))
        {
            recordedTargetId = BuildInstallTargetId(
                options.InstallScope,
                string.IsNullOrWhiteSpace(recordedHostContext) ? normalizedHostContext : recordedHostContext);
        }
        if (string.IsNullOrWhiteSpace(recordedInstallStatePath))
        {
            recordedInstallStatePath = statePath;
        }

        if (!string.Equals(schemaVersion, InstallStateSchemaVersion, StringComparison.Ordinal))
        {
            findings.Add("install_state_schema_version_mismatch");
        }

        if (!string.Equals(recordedInstallScope, BuildInstallScopeJsonLabel(options.InstallScope), StringComparison.Ordinal))
        {
            findings.Add("install_state_scope_mismatch");
        }

        var expectedTargetRoot = ResolveInstallTargetRoot(options.InstallScope, workspaceRoot);
        if (!PathStringsMatch(recordedTargetRoot, expectedTargetRoot))
        {
            findings.Add("install_state_target_root_mismatch");
        }

        if (!PathStringsMatch(recordedInstallStatePath, statePath))
        {
            findings.Add("install_state_path_mismatch");
        }

        if (options.InstallScope == InstallScope.RepoLocal && !PathStringsMatch(recordedWorkspaceRoot, workspaceRoot))
        {
            findings.Add("install_state_workspace_root_mismatch");
        }
        else if (options.InstallScope == InstallScope.UserProfile
                 && !PathStringsMatch(recordedWorkspaceRoot, workspaceRoot))
        {
            warnings.Add("last_workspace_target_differs_from_current_request");
        }

        if (!PathStringsMatch(recordedPluginRoot, pluginRoot))
        {
            findings.Add("install_state_plugin_root_mismatch");
        }

        if (!PathStringsMatch(recordedMarketplacePath, marketplacePath))
        {
            findings.Add("install_state_marketplace_path_mismatch");
        }

        if (!PathStringsMatch(recordedRuntimePath, runtimePath))
        {
            findings.Add("install_state_runtime_path_mismatch");
        }

        if (!string.Equals(recordedMcpServerName, BuildMcpServerName(), StringComparison.Ordinal))
        {
            findings.Add("install_state_mcp_server_name_mismatch");
        }

        return new InstallStateReport
        {
            schema_version = string.IsNullOrWhiteSpace(schemaVersion) ? InstallStateSchemaVersion : schemaVersion,
            state_path = statePath,
            state_present = true,
            state_written = stateWritten,
            state_valid = findings.Count == 0,
            findings = findings.ToArray(),
            warnings = warnings.ToArray(),
            recorded_at_utc = ReadJsonString(state, "written_at_utc"),
            recorded_install_scope = recordedInstallScope,
            recorded_host_context = string.IsNullOrWhiteSpace(recordedHostContext) ? normalizedHostContext : recordedHostContext,
            recorded_host_targets = ReadJsonStringArray(state, "host_targets"),
            recorded_target_id = recordedTargetId,
            recorded_target_kind = recordedTargetKind,
            recorded_target_root = recordedTargetRoot,
            recorded_workspace_root = recordedWorkspaceRoot,
            recorded_workspace_schema_targeted = recordedWorkspaceSchemaTargeted,
            recorded_plugin_root = recordedPluginRoot,
            recorded_marketplace_path = recordedMarketplacePath,
            recorded_install_state_path = recordedInstallStatePath,
            recorded_runtime_path = recordedRuntimePath,
            recorded_mcp_server_name = recordedMcpServerName,
            recorded_managed_operation_count = recordedManagedOperationCount
        };
    }

    // Purpose: Converts setup operation enum values into stable JSON labels.
    // Expected input: OperationMode value.
    // Expected output: Lowercase operation label.
    // Critical dependencies: SetupResult.setup_operation and install-state document shape.
    private static string BuildSetupOperationLabel(OperationMode mode) => mode switch
    {
        OperationMode.Install => "install",
        OperationMode.Update => "update",
        OperationMode.Status => "status",
        OperationMode.Underlay => "underlay",
        OperationMode.Refresh => "refresh",
        _ => "assess"
    };

    // Purpose: Converts install-scope enum values into stable JSON labels.
    // Expected input: InstallScope value.
    // Expected output: repo_local or user_profile.
    // Critical dependencies: SetupResult.install_scope and install-state validation.
    private static string BuildInstallScopeJsonLabel(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile ? "user_profile" : "repo_local";
    }

    private static string BuildInstallScopeJsonLabel(SetupOptions options)
    {
        return options.Mode == OperationMode.Underlay ? "repo_underlay" : BuildInstallScopeJsonLabel(options.InstallScope);
    }

    // Purpose: Reads a string field from a JsonObject without throwing on absent or wrong-shaped fields.
    // Expected input: JSON object plus field key.
    // Expected output: Field value or empty string.
    // Critical dependencies: System.Text.Json.Nodes.
    private static string ReadJsonString(JsonObject state, string key)
    {
        return state[key] is JsonValue value && value.TryGetValue<string>(out var text)
            ? text
            : string.Empty;
    }

    // Purpose: Reads a string field from a nested JSON object without throwing on legacy or malformed state files.
    // Expected input: JSON object plus object key and nested field key.
    // Expected output: Field value or empty string.
    // Critical dependencies: install-state v2 target/workspace/source object shape.
    private static string ReadNestedJsonString(JsonObject state, string objectKey, string key)
    {
        return state[objectKey] is JsonObject nested
            ? ReadJsonString(nested, key)
            : string.Empty;
    }

    // Purpose: Reads a nullable bool from a nested JSON object without throwing on legacy or malformed state files.
    // Expected input: JSON object plus object key and nested field key.
    // Expected output: True/false when present and well-shaped; otherwise null.
    // Critical dependencies: install-state v2 workspace object shape.
    private static bool? ReadNestedJsonBool(JsonObject state, string objectKey, string key)
    {
        if (state[objectKey] is not JsonObject nested ||
            nested[key] is not JsonValue value ||
            !value.TryGetValue<bool>(out var booleanValue))
        {
            return null;
        }

        return booleanValue;
    }

    // Purpose: Reads a string-array field from a JsonObject without throwing on absent or wrong-shaped fields.
    // Expected input: JSON object plus field key.
    // Expected output: String array, empty when the field is absent or malformed.
    // Critical dependencies: System.Text.Json.Nodes.
    private static string[] ReadJsonStringArray(JsonObject state, string key)
    {
        if (state[key] is not JsonArray array)
        {
            return [];
        }

        return array
            .Select(node => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null)
            .Where(static text => !string.IsNullOrWhiteSpace(text))
            .Select(static text => text!)
            .ToArray();
    }

    // Purpose: Creates a JSON array from string values while avoiding caller-side JsonNode ceremony.
    // Expected input: Strings to include.
    // Expected output: JsonArray containing those strings.
    // Critical dependencies: System.Text.Json.Nodes.
    private static JsonArray CreateJsonStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    // Purpose: Compares path strings after normalizing absolute forms when possible.
    // Expected input: Recorded and expected path strings.
    // Expected output: True when both paths describe the same filesystem target, including two empty values.
    // Critical dependencies: Path.GetFullPath and Windows path comparison semantics.
    private static bool PathStringsMatch(string? actual, string? expected)
    {
        if (string.IsNullOrWhiteSpace(actual) && string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        try
        {
            return string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Purpose: Aggregates the origin, source, and destination path roles for setup output.
    // Expected input: Setup options plus resolved workspace, bundle, marketplace, and update-source paths.
    // Expected output: A PathRoleCollection aligned with the nested setup JSON contract.
    // Critical dependencies: BuildSetupOriginRoleReport, BuildSetupSourceRoleReport, BuildSetupDestinationRoleReport, and AnarchyPathCanon.
    private static PathRoleCollection BuildSetupResultPaths(
        SetupOptions options,
        string workspaceRoot,
        string pluginRoot,
        string marketplacePath,
        string runtimePath,
        string pluginManifestPath,
        string mcpPath,
        string skillPath,
        string schemaManifestPath,
        string updateSourceRoot)
    {
        var origin = BuildSetupOriginRoleReport(options, workspaceRoot, updateSourceRoot);
        var source = BuildSetupSourceRoleReport(options, workspaceRoot, pluginRoot, updateSourceRoot);
        var destination = BuildSetupDestinationRoleReport(
            options,
            workspaceRoot,
            pluginRoot,
            marketplacePath,
            runtimePath,
            pluginManifestPath,
            mcpPath,
            skillPath,
            schemaManifestPath);

        return AnarchyPathCanon.CreateRoleCollection(origin: origin, source: source, destination: destination);
    }

    // Purpose: Builds the origin role for setup output when an explicit update source was used.
    // Expected input: Setup options and the resolved update-source root.
    // Expected output: A path-role report for the repo-authored update source, or null when no update source applies.
    // Critical dependencies: AnarchyPathCanon source-path helpers and the update-source contract.
    private static PathRoleReport? BuildSetupOriginRoleReport(SetupOptions options, string workspaceRoot, string updateSourceRoot)
    {
        if (string.IsNullOrWhiteSpace(updateSourceRoot)
            && ShouldInspectSourceAuthoringBundle(options, InspectSourceAuthoringBundle(workspaceRoot)))
        {
            var workspaceSourcePluginDirectoryPath = AnarchyPathCanon.ResolveSourcePluginDirectory(workspaceRoot);
            return AnarchyPathCanon.CreateRoleReport(
                rootPath: workspaceRoot,
                directories:
                [
                    CreatePathEntry("plugin_source_directory_path", workspaceSourcePluginDirectoryPath)
                ],
                relative:
                [
                    CreatePathEntry("plugin_source_directory_relative_path", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath)
                ]);
        }

        if (string.IsNullOrWhiteSpace(updateSourceRoot))
        {
            return null;
        }

        var sourcePluginDirectoryPath = AnarchyPathCanon.ResolveSourcePluginDirectory(updateSourceRoot);
        return AnarchyPathCanon.CreateRoleReport(
            rootPath: updateSourceRoot,
            directories:
            [
                CreatePathEntry("plugin_source_directory_path", sourcePluginDirectoryPath)
            ],
            files:
            [
                CreatePathEntry("plugin_mcp_file_path", AnarchyPathCanon.ResolveRelativePath(updateSourceRoot, AnarchyPathCanon.RepoSourcePluginMcpFileRelativePath)),
                CreatePathEntry("setup_executable_file_path", AnarchyPathCanon.ResolveRelativePath(updateSourceRoot, AnarchyPathCanon.RepoSourceSetupExecutableFileRelativePath)),
                CreatePathEntry("plugin_readme_source_file_path", AnarchyPathCanon.ResolveRelativePath(updateSourceRoot, AnarchyPathCanon.RepoSourceGeneratedPluginReadmeSourceRelativePath))
            ],
            relative:
            [
                CreatePathEntry("plugin_source_directory_relative_path", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath),
                CreatePathEntry("plugin_mcp_file_relative_path", AnarchyPathCanon.RepoSourcePluginMcpFileRelativePath),
                CreatePathEntry("setup_executable_file_relative_path", AnarchyPathCanon.RepoSourceSetupExecutableFileRelativePath),
                CreatePathEntry("plugin_readme_source_file_relative_path", AnarchyPathCanon.RepoSourceGeneratedPluginReadmeSourceRelativePath)
            ]);
    }

    // Purpose: Builds the source role for setup output.
    // Expected input: Setup options, workspace root, plugin root, and optional update-source root.
    // Expected output: A path-role report describing either the explicit update source or the currently materialized bundle.
    // Critical dependencies: AnarchyPathCanon source and bundle path helpers.
    private static PathRoleReport BuildSetupSourceRoleReport(
        SetupOptions options,
        string workspaceRoot,
        string pluginRoot,
        string updateSourceRoot)
    {
        if (!string.IsNullOrWhiteSpace(updateSourceRoot))
        {
            return AnarchyPathCanon.CreateRoleReport(
                rootPath: updateSourceRoot,
                directories:
                [
                    CreatePathEntry("plugin_source_directory_path", AnarchyPathCanon.ResolveSourcePluginDirectory(updateSourceRoot))
                ],
                relative:
                [
                    CreatePathEntry("plugin_source_directory_relative_path", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath)
                ])!;
        }

        var sourceAuthoringBundle = InspectSourceAuthoringBundle(workspaceRoot);
        if (ShouldInspectSourceAuthoringBundle(options, sourceAuthoringBundle))
        {
            return AnarchyPathCanon.CreateRoleReport(
                rootPath: sourceAuthoringBundle.PluginRoot,
                directories:
                [
                    CreatePathEntry("contracts_directory_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleContractsDirectoryRelativePath)),
                    CreatePathEntry("runtime_directory_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleRuntimeDirectoryRelativePath)),
                    CreatePathEntry("schemas_directory_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleSchemasDirectoryRelativePath)),
                    CreatePathEntry("scripts_directory_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleScriptsDirectoryRelativePath)),
                    CreatePathEntry("skill_directory_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleSkillDirectoryRelativePath))
                ],
                files:
                [
                    CreatePathEntry("plugin_manifest_file_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath)),
                    CreatePathEntry("mcp_declaration_file_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath)),
                    CreatePathEntry("runtime_executable_file_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath)),
                    CreatePathEntry("schema_manifest_file_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath)),
                    CreatePathEntry("skill_file_path", AnarchyPathCanon.ResolveBundleFilePath(sourceAuthoringBundle.PluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath))
                ],
                relative:
                [
                    CreatePathEntry("plugin_source_directory_relative_path", AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath),
                    CreatePathEntry("plugin_manifest_file_relative_path", AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                    CreatePathEntry("mcp_declaration_file_relative_path", AnarchyPathCanon.BundleMcpFileRelativePath),
                    CreatePathEntry("runtime_executable_file_relative_path", AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath),
                    CreatePathEntry("schema_manifest_file_relative_path", AnarchyPathCanon.BundleSchemaManifestFileRelativePath),
                    CreatePathEntry("skill_file_relative_path", AnarchyPathCanon.BundleSkillFileRelativePath)
                ])!;
        }

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
                CreatePathEntry("skill_file_relative_path", AnarchyPathCanon.BundleSkillFileRelativePath),
                CreatePathEntry("marketplace_plugin_source_relative_path", BuildPluginRelativePath(options.InstallScope, workspaceRoot))
            ])!;
    }

    // Purpose: Builds the destination role for setup output.
    // Expected input: Setup options plus resolved bundle, runtime, marketplace, and workspace paths.
    // Expected output: A path-role report describing the install or assessment destination surfaces.
    // Critical dependencies: AnarchyPathCanon destination helpers and the selected install lane.
    private static PathRoleReport BuildSetupDestinationRoleReport(
        SetupOptions options,
        string workspaceRoot,
        string pluginRoot,
        string marketplacePath,
        string runtimePath,
        string pluginManifestPath,
        string mcpPath,
        string skillPath,
        string schemaManifestPath)
    {
        var destinationRoot = options.InstallScope == InstallScope.UserProfile
            ? GetUserProfileDirectory()
            : workspaceRoot;
        var codexSelected = options.HostTargets.HasFlag(HostTargets.Codex);
        var claudeDesktopConfigPath = options.HostTargets.HasFlag(HostTargets.ClaudeDesktop)
            ? ClaudeDesktopInstallDetector.ResolveActiveConfigPath(ClaudeDesktopInstallDetector.Detect())
            : null;

        return AnarchyPathCanon.CreateRoleReport(
            rootPath: destinationRoot,
            directories:
            [
                CreatePathEntry("plugin_root_directory_path", pluginRoot),
                CreatePathEntry(
                    "marketplace_directory_path",
                    codexSelected ? Path.GetDirectoryName(marketplacePath) : null),
                CreatePathEntry("schema_target_root_directory_path", workspaceRoot)
            ],
            files:
            [
                CreatePathEntry(
                    "marketplace_file_path",
                    codexSelected ? marketplacePath : null),
                CreatePathEntry("plugin_manifest_file_path", pluginManifestPath),
                CreatePathEntry("mcp_declaration_file_path", mcpPath),
                CreatePathEntry("runtime_executable_file_path", runtimePath),
                CreatePathEntry("skill_file_path", skillPath),
                CreatePathEntry("schema_manifest_file_path", schemaManifestPath),
                CreatePathEntry("install_state_file_path", ResolveInstallStatePath(pluginRoot)),
                CreatePathEntry(
                    "codex_config_file_path",
                    options.InstallScope == InstallScope.UserProfile && codexSelected
                        ? AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory())
                        : null),
                CreatePathEntry(
                    "claude_code_config_file_path",
                    options.HostTargets.HasFlag(HostTargets.ClaudeCode)
                        ? ClaudeCodeUserScopeLane.GetUserScopeConfigPath()
                        : null),
                CreatePathEntry("claude_desktop_config_file_path", claudeDesktopConfigPath)
            ],
            relative:
            [
                CreatePathEntry(
                    "marketplace_file_relative_path",
                    codexSelected
                        ? options.InstallScope == InstallScope.UserProfile
                            ? AnarchyPathCanon.UserProfileMarketplaceFileRelativePath
                            : AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath
                        : null),
                CreatePathEntry("plugin_source_relative_path", BuildPluginRelativePath(options.InstallScope, workspaceRoot)),
                CreatePathEntry("plugin_manifest_file_relative_path", AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                CreatePathEntry("mcp_declaration_file_relative_path", AnarchyPathCanon.BundleMcpFileRelativePath),
                CreatePathEntry("runtime_executable_file_relative_path", AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath),
                CreatePathEntry("schema_manifest_file_relative_path", AnarchyPathCanon.BundleSchemaManifestFileRelativePath),
                CreatePathEntry("install_state_file_relative_path", InstallStateFileRelativePath),
                CreatePathEntry("skill_file_relative_path", AnarchyPathCanon.BundleSkillFileRelativePath)
            ])!;
    }

    // Purpose: Creates a keyed path entry for nested path reporting.
    // Expected input: A path key and optional value.
    // Expected output: A key/value pair consumed by AnarchyPathCanon.CreateRoleReport.
    // Critical dependencies: The nested path-report key vocabulary.
    private static KeyValuePair<string, string?> CreatePathEntry(string key, string? value)
    {
        return new KeyValuePair<string, string?>(key, value);
    }

    // Purpose: Detects stale legacy home-local install surfaces that can block a clean user-profile ready state.
    // Expected input: Install scope, normalized host context, and whether the new marketplace lane is already ready.
    // Expected output: A LegacyUserProfileInspection record describing legacy plugin-root and custom-MCP evidence.
    // Critical dependencies: AnarchyPathCanon legacy helpers, TOML readers, and the Codex legacy cleanup model.
    private static LegacyUserProfileInspection InspectLegacyUserProfileSurfaces(InstallScope installScope, string normalizedHostContext, bool newPluginMarketplaceLaneReady)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return new LegacyUserProfileInspection(false, false, newPluginMarketplaceLaneReady, []);
        }

        var legacyPluginRoots = AnarchyPathCanon.OwnedPluginNameExact
            .Select(name => AnarchyPathCanon.ResolveLegacyUserProfilePluginRoot(GetUserProfileDirectory(), name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var legacyPluginRootPresent = legacyPluginRoots.Any(Directory.Exists);
        var legacyCodexCustomMcpEntryPresent = false;

        var codexConfigPath = AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory());
        if (File.Exists(codexConfigPath))
        {
            var content = File.ReadAllText(codexConfigPath);
            var blockMatch = Regex.Match(content, CodexCustomMcpServerBlockPattern);
            if (blockMatch.Success)
            {
                var command = TryReadTomlString(blockMatch.Value, "command");
                var cwd = TryReadTomlString(blockMatch.Value, "cwd");
                var legacyRuntimePaths = legacyPluginRoots
                    .Select(root => AnarchyPathCanon.ResolveBundleFilePath(root, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var legacyWorkingDirectories = legacyPluginRoots
                    .Select(Path.GetFullPath)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                legacyCodexCustomMcpEntryPresent =
                    (!string.IsNullOrWhiteSpace(command) && legacyRuntimePaths.Contains(command))
                    || (!string.IsNullOrWhiteSpace(cwd) && legacyWorkingDirectories.Contains(Path.GetFullPath(cwd)));
            }
        }

        var findings = new List<string>();
        if (legacyPluginRootPresent)
        {
            findings.Add("legacy_user_profile_plugin_root_present");
        }

        if (legacyCodexCustomMcpEntryPresent)
        {
            findings.Add("legacy_codex_custom_mcp_entry_present");
        }

        return new LegacyUserProfileInspection(
            legacyPluginRootPresent,
            legacyCodexCustomMcpEntryPresent,
            false,
            findings.ToArray());
    }

    // Purpose: Builds the user-facing label for the selected install scope.
    // Expected input: Install scope.
    // Expected output: "User-Profile" or "Repo-Local".
    // Critical dependencies: The current wording contract for disclosures and dialogs.
    private static string BuildInstallScopeLabel(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile ? "User-Profile" : "Repo-Local";
    }

    // Purpose: Removes the legacy Codex custom-MCP block when it still points at the deprecated home-local plugin root.
    // Expected input: Install scope, normalized host context, and mutable action log.
    // Expected output: No direct return value; rewrites the Codex config file when a stale legacy block is present.
    // Critical dependencies: AnarchyPathCanon legacy path helpers, TOML readers, and regex block removal.
    private static void RemoveLegacyCodexCustomMcpEntry(InstallScope installScope, string normalizedHostContext, HashSet<string> actionsTaken)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return;
        }

        var codexConfigPath = AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory());
        if (!File.Exists(codexConfigPath))
        {
            return;
        }

        var content = File.ReadAllText(codexConfigPath);
        var blockMatch = Regex.Match(content, CodexCustomMcpServerBlockPattern);
        if (!blockMatch.Success)
        {
            return;
        }

        var legacyPluginRoot = AnarchyPathCanon.ResolveLegacyUserProfilePluginRoot(
            GetUserProfileDirectory(),
            BuildPluginName(InstallScope.UserProfile, null));
        var legacyRuntimePath = AnarchyPathCanon.ResolveBundleFilePath(legacyPluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var command = TryReadTomlString(blockMatch.Value, "command");
        var cwd = TryReadTomlString(blockMatch.Value, "cwd");
        var staleLegacyBlock =
            string.Equals(command, legacyRuntimePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(cwd, Path.GetFullPath(legacyPluginRoot), StringComparison.OrdinalIgnoreCase);
        if (!staleLegacyBlock)
        {
            return;
        }

        var updatedContent = Regex.Replace(content, CodexCustomMcpServerBlockPattern, string.Empty, RegexOptions.Multiline);
        updatedContent = Regex.Replace(updatedContent, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
        updatedContent = updatedContent.TrimEnd('\r', '\n');
        if (!string.IsNullOrWhiteSpace(updatedContent))
        {
            updatedContent += Environment.NewLine;
        }

        File.WriteAllText(codexConfigPath, updatedContent);
        actionsTaken.Add("removed_stale_codex_custom_mcp_entry");
    }

    // Purpose: Detects the newline style to preserve when writing TOML back to disk.
    // Expected input: Existing file content.
    // Expected output: CRLF when present, otherwise LF.
    // Critical dependencies: Existing file content and the current newline-preservation rule.
    private static string DetectNewline(string content)
    {
        return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

    // Purpose: Reads a string value from a TOML block using the simple parser used by setup.
    // Expected input: TOML block text and the key to extract.
    // Expected output: The decoded string value or null when the key is missing.
    // Critical dependencies: Regex parsing and the current custom-MCP block format.
    private static string? TryReadTomlString(string block, string key)
    {
        var match = Regex.Match(block, $@"(?m)^\s*{Regex.Escape(key)}\s*=\s*(?<value>.+?)\s*$");
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups["value"].Value.Trim();
        if (raw.Length >= 2 && raw[0] == '\'' && raw[^1] == '\'')
        {
            return raw[1..^1].Replace("''", "'", StringComparison.Ordinal);
        }

        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            return raw[1..^1];
        }

        return raw;
    }

    // Purpose: Reads a boolean value from a TOML block using the simple parser used by setup.
    // Expected input: TOML block text and the key to extract.
    // Expected output: True, false, or null when the key is missing or not recognized.
    // Critical dependencies: Regex parsing and the current custom-MCP block format.
    private static bool? TryReadTomlBool(string block, string key)
    {
        var match = Regex.Match(block, $@"(?m)^\s*{Regex.Escape(key)}\s*=\s*(?<value>true|false)\s*$");
        if (!match.Success)
        {
            return null;
        }

        return string.Equals(match.Groups["value"].Value, "true", StringComparison.Ordinal);
    }

    // Purpose: Normalizes a repo or marketplace label into the slug format used for repo-scoped names.
    // Expected input: Raw label text.
    // Expected output: Lowercase dash-separated slug, falling back to `repo` when empty.
    // Critical dependencies: Repo-scoped naming templates and the current slug rules.
    private static string NormalizeMarketplaceSlug(string value)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "repo" : normalized;
    }

    // Purpose: Refreshes the installed bundle from an explicit local or remote update source.
    // Expected input: Plugin root, workspace root, setup options, mutable action log, and mutable update notes.
    // Expected output: The resolved source root actually used for the refresh.
    // Critical dependencies: ResolveUpdateSourceRoot, CopySurface, bundle-surface list, and schema refresh rules.
    private static string RefreshFromUpdateSource(
        string pluginRoot,
        string repoRoot,
        SetupOptions options,
        HashSet<string> actionsTaken,
        HashSet<string> updateNotes)
    {
        using var tempRoot = new TempDirectory();
        var sourceRoot = ResolveUpdateSourceRoot(options, tempRoot.Path, updateNotes);
        var sourcePluginRoot = AnarchyPathCanon.ResolveSourcePluginDirectory(sourceRoot);
        if (!Directory.Exists(sourcePluginRoot))
        {
            throw new DirectoryNotFoundException($"Update source did not contain {AnarchyPathCanon.RepoSourcePluginDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar)}.");
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
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                updateNotes.Add("portable_schema_family_refresh_skipped_without_workspace_root");
                actionsTaken.Add("portable_schema_family_not_targeted");
                return sourceRoot;
            }

            if (!options.ApplyChanges)
            {
                updateNotes.Add("portable_schema_family_refresh_plan_only_apply_required");
                actionsTaken.Add(string.IsNullOrWhiteSpace(options.UpdateSourcePath)
                    ? "planned_portable_schema_family_refresh_from_public_repo"
                    : "planned_portable_schema_family_refresh_from_local_update_source");
                return sourceRoot;
            }

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

        return sourceRoot;
    }

    // Purpose: Resolves the actual update source root from CLI options.
    // Expected input: Setup options, temporary working root, and mutable update notes.
    // Expected output: Absolute repo root extracted from a local path, local zip, or downloaded zip.
    // Critical dependencies: Zip download/extraction and ResolveFirstExtractedRoot.
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

    // Purpose: Finds the first extracted directory root produced by an update zip.
    // Expected input: Extraction directory path.
    // Expected output: Absolute path to the first extracted root directory.
    // Critical dependencies: Directory.GetDirectories and the archive layout convention.
    private static string ResolveFirstExtractedRoot(string extractPath)
    {
        var firstDirectory = Directory.GetDirectories(extractPath).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstDirectory))
        {
            throw new InvalidOperationException("Update archive did not produce an extractable repository root.");
        }

        return firstDirectory;
    }

    // Purpose: Copies one source file or directory into the target location, replacing any existing target first.
    // Expected input: Source path and destination path.
    // Expected output: No direct return value; materializes the target surface from the source surface.
    // Critical dependencies: File/Directory existence checks, recursive copy behavior, and filesystem write access.
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

// Purpose: Summarizes marketplace inspection results for setup assessment and repair logic.
// Expected input: Filesystem and JSON inspection facts collected by SetupEngine.
// Expected output: Immutable marketplace state record consumed by setup branching.
// Critical dependencies: Marketplace inspection helpers and the current marketplace findings vocabulary.
internal sealed record MarketplaceInspection(
    bool Exists,
    bool HasPluginsArray,
    bool HasAnarchyPluginEntry,
    bool InstalledByDefault,
    bool MarketplaceIdentityAligned,
    bool IsValidJson,
    string[] Findings);

// Purpose: Summarizes plugin-manifest inspection results for setup assessment and repair logic.
// Expected input: Filesystem and JSON inspection facts for the plugin manifest.
// Expected output: Immutable manifest state record consumed by setup branching.
// Critical dependencies: Plugin manifest inspection helpers and the current findings vocabulary.
internal sealed record PluginManifestInspection(
    bool Exists,
    bool IsValidJson,
    bool IdentityAligned,
    string[] Findings);

// Purpose: Summarizes .mcp.json inspection results for setup assessment and repair logic.
// Expected input: Filesystem and JSON inspection facts for the plugin-local MCP declaration.
// Expected output: Immutable MCP configuration state record consumed by setup branching.
// Critical dependencies: MCP declaration inspection helpers and the current findings vocabulary.
internal sealed record McpConfigurationInspection(
    bool Exists,
    bool IdentityAligned,
    bool IsValidJson,
    string[] Findings);

// Purpose: Summarizes selected host-config readiness for non-Codex MCP host targets.
// Expected input: Claude host config inspections for the selected host targets.
// Expected output: Immutable readiness, findings, and suggested safe repairs.
// Critical dependencies: Claude Code and Claude Desktop host-config inspectors.
internal sealed record HostConfigInspection(
    bool Ready,
    string[] Findings,
    string[] SafeRepairs)
{
    public static HostConfigInspection NotRequired { get; } = new(true, [], []);
    public static HostConfigInspection ReadyState { get; } = new(true, [], []);
}

// Purpose: Summarizes legacy home-local evidence discovered during user-profile inspection.
// Expected input: Legacy plugin-root and legacy custom-MCP detection facts.
// Expected output: Immutable legacy-state inspection record used to block false readiness.
// Critical dependencies: Legacy path detection and current Codex cleanup guidance.
internal sealed record LegacyUserProfileInspection(
    bool LegacyPluginRootPresent,
    bool LegacyCodexCustomMcpEntryPresent,
    bool NewPluginMarketplaceLaneReady,
    string[] Findings)
{
    public bool HasLegacySurface => LegacyPluginRootPresent || LegacyCodexCustomMcpEntryPresent;
}

// Purpose: Summarizes the repo-authored plugin bundle when the setup executable is run from the AI-Links source repo.
// Expected input: Filesystem evidence under plugins/anarchy-ai.
// Expected output: Immutable source-bundle facts that stay separate from consumer install-state evidence.
// Critical dependencies: source-authoring boundary and the current embedded plugin-bundle surface list.
internal sealed record SourceAuthoringBundleInspection(
    bool Present,
    string PluginRoot,
    bool PluginManifestExists,
    bool McpExists,
    bool RuntimeExists,
    bool SkillExists,
    bool SchemaManifestExists,
    string[] MissingCoreContracts,
    bool ExperimentalContractExists)
{
    public static SourceAuthoringBundleInspection Empty { get; } = new(
        Present: false,
        PluginRoot: string.Empty,
        PluginManifestExists: false,
        McpExists: false,
        RuntimeExists: false,
        SkillExists: false,
        SchemaManifestExists: false,
        MissingCoreContracts: [],
        ExperimentalContractExists: false);

    public bool IsComplete =>
        Present
        && PluginManifestExists
        && McpExists
        && RuntimeExists
        && SkillExists
        && SchemaManifestExists
        && MissingCoreContracts.Length == 0;

    public string State => IsComplete ? "complete" : Present ? "partial" : "absent";
}

internal sealed record RefreshSchemaResult(
    string[] RefreshedFiles,
    string[] UnchangedFiles,
    string[] BackupFiles)
{
    public static RefreshSchemaResult Empty { get; } = new([], [], []);
    public bool RefreshNeeded => RefreshedFiles.Length > 0;
}

internal sealed record CodexDuplicateLaneResult(
    string? SelectedPrimaryLane,
    string[] DisabledLanes,
    bool DuplicateLanesDetected,
    bool SelectedLaneEnabled)
{
    public static CodexDuplicateLaneResult Empty { get; } = new(null, [], false, false);
}

// Purpose: Exposes the embedded plugin bundle and portable-schema payload resources carried by the setup executable.
// Expected input: Resource-name requests from setup extraction code.
// Expected output: Enumerables of resource names and readable resource streams.
// Critical dependencies: Assembly manifest resources created by the setup publish process.
internal static class PayloadResources
{
    private static readonly string PluginPrefix = AnarchyPathCanon.BuildPluginPayloadResourcePrefix();
    private static readonly string PortableSchemaPrefix = AnarchyPathCanon.BuildPortableSchemaPayloadResourcePrefix();

    // Purpose: Enumerates the embedded plugin bundle resources carried by the setup executable.
    // Expected input: None.
    // Expected output: Resource names beneath the embedded plugin payload prefix.
    // Critical dependencies: Assembly manifest resources and AnarchyPathCanon.BuildPluginPayloadResourcePrefix.
    public static IEnumerable<string> GetPluginBundleResources()
    {
        return typeof(PayloadResources).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(PluginPrefix, StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal);
    }

    // Purpose: Enumerates the embedded portable schema resources carried by the setup executable.
    // Expected input: None.
    // Expected output: Resource names beneath the embedded portable-schema payload prefix.
    // Critical dependencies: Assembly manifest resources and AnarchyPathCanon.BuildPortableSchemaPayloadResourcePrefix.
    public static IEnumerable<string> GetPortableSchemaResources()
    {
        return typeof(PayloadResources).Assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(PortableSchemaPrefix, StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal);
    }

    // Purpose: Opens a manifest-resource stream from the setup executable.
    // Expected input: Full resource name to open.
    // Expected output: A readable resource stream.
    // Critical dependencies: Assembly manifest resources and a valid resource name.
    public static Stream OpenResource(string resourceName)
    {
        return typeof(PayloadResources).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded setup payload resource: {resourceName}");
    }
}

// Purpose: Creates and cleans up a temporary directory used during setup update flows.
// Expected input: Object construction and disposal lifetime.
// Expected output: A temp directory path that is removed on disposal when possible.
// Critical dependencies: Path.GetTempPath, Directory.CreateDirectory, and recursive delete permissions.
internal sealed class TempDirectory : IDisposable
{
    // Purpose: Creates a new unique temporary directory for setup work.
    // Expected input: None.
    // Expected output: A TempDirectory instance with a created directory path.
    // Critical dependencies: OS temp storage and directory creation permissions.
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "anarchy-ai-setup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    // Purpose: Removes the temporary directory created for this instance.
    // Expected input: None.
    // Expected output: No direct return value; deletes the temp directory when it still exists.
    // Critical dependencies: Recursive directory deletion and filesystem access.
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

// Purpose: Detects which Claude Desktop install (MSIX Store build or classic standalone) is present on the current user profile.
// Expected input: Current user profile filesystem state; no caller data.
// Expected output: A bounded InstallKind value plus the resolved config path for the detected install.
// Critical dependencies: Directory.Exists checks against the two documented Claude Desktop config roots. Detection intentionally
// avoids shelling out to Get-AppxPackage or touching the registry so the installer stays free of PowerShell and elevation assumptions.
internal static class ClaudeDesktopInstallDetector
{
    internal enum InstallKind
    {
        None,
        Classic,
        Msix,
        Both
    }

    // Purpose: Resolves the MSIX Claude Desktop LocalCache Roaming directory on the current user profile.
    // Expected input: None; reads the LocalApplicationData special folder.
    // Expected output: Absolute path to the MSIX LocalCache Roaming\Claude directory (whether or not it currently exists).
    // Critical dependencies: Environment.GetFolderPath and the fixed MSIX package family name Claude_pzs8sxrjxfjjc.
    public static string GetMsixClaudeDir()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Packages", "Claude_pzs8sxrjxfjjc", "LocalCache", "Roaming", "Claude");
    }

    // Purpose: Resolves the classic Claude Desktop config directory on the current user profile.
    // Expected input: None; reads the ApplicationData special folder.
    // Expected output: Absolute path to the %APPDATA%\Claude directory (whether or not it currently exists).
    // Critical dependencies: Environment.GetFolderPath and the classic Anthropic Claude install shape.
    public static string GetClassicClaudeDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Claude");
    }

    // Purpose: Returns the claude_desktop_config.json path beneath a Claude Desktop root directory.
    // Expected input: The directory returned by GetMsixClaudeDir or GetClassicClaudeDir.
    // Expected output: Full path ending in claude_desktop_config.json.
    // Critical dependencies: The documented Claude Desktop config file name.
    public static string GetConfigPath(string claudeDir) => Path.Combine(claudeDir, "claude_desktop_config.json");

    // Purpose: Detects which Claude Desktop install is present on the current user profile.
    // Expected input: None; inspects the two documented Claude Desktop directories.
    // Expected output: None, Classic, Msix, or Both per directory existence.
    // Critical dependencies: Directory.Exists against the MSIX LocalCache Roaming\Claude and classic %APPDATA%\Claude paths.
    public static InstallKind Detect()
    {
        var msix = Directory.Exists(GetMsixClaudeDir());
        var classic = Directory.Exists(GetClassicClaudeDir());
        if (msix && classic) { return InstallKind.Both; }
        if (msix) { return InstallKind.Msix; }
        if (classic) { return InstallKind.Classic; }
        return InstallKind.None;
    }

    // Purpose: Resolves the active Claude Desktop config path for the detected install kind.
    // Expected input: A InstallKind value.
    // Expected output: Absolute path of the claude_desktop_config.json the installer should write, or null when no install is detected.
    // Critical dependencies: GetMsixClaudeDir, GetClassicClaudeDir, and the documented MSIX-preferred-over-classic selection rule when both are present.
    public static string? ResolveActiveConfigPath(InstallKind kind)
    {
        return kind switch
        {
            InstallKind.Msix => GetConfigPath(GetMsixClaudeDir()),
            InstallKind.Classic => GetConfigPath(GetClassicClaudeDir()),
            // Both-present tie-break prefers the MSIX path because the current default Claude Desktop download is the MSIX build;
            // the classic %APPDATA%\Claude directory is often a stale seed or post-uninstall residue rather than an active install.
            InstallKind.Both => GetConfigPath(GetMsixClaudeDir()),
            _ => null
        };
    }

    // Purpose: Returns a short human-readable label for the detected install kind.
    // Expected input: A InstallKind value.
    // Expected output: A stable short label for JSON output and disclosure text.
    // Critical dependencies: SetupResult actions-taken contract and disclosure wording.
    public static string ToLabel(InstallKind kind) => kind switch
    {
        InstallKind.Msix => "msix",
        InstallKind.Classic => "classic",
        InstallKind.Both => "both_msix_preferred",
        _ => "none"
    };
}

// Purpose: Provides shared read-merge-write helpers for Claude Code and Claude Desktop JSON config edits.
// Expected input: Absolute paths to Claude host config files and the JSON object the installer wants to merge in.
// Expected output: Tolerant JSON parsing (comments, trailing commas, empty, missing), atomic writes, and first-time backups.
// Critical dependencies: System.Text.Json and the on-disk Claude host config contracts.
internal static class ClaudeHostConfigWriter
{
    // Purpose: Reads a Claude host config file and returns its root JsonObject, creating an empty one when missing or malformed.
    // Expected input: Absolute path to a claude_desktop_config.json or .claude.json file.
    // Expected output: A mutable JsonObject plus a boolean indicating whether the file existed and parsed as an object.
    // Critical dependencies: System.Text.Json with comment skipping and trailing-comma tolerance to match Claude Desktop user-edited files.
    public static (JsonObject Root, bool FileExistedAndParsed) ReadTolerant(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return (new JsonObject(), false);
        }

        string raw;
        try
        {
            raw = File.ReadAllText(configPath);
        }
        catch
        {
            return (new JsonObject(), false);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return (new JsonObject(), false);
        }

        try
        {
            var documentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            using var doc = JsonDocument.Parse(raw, documentOptions);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (new JsonObject(), false);
            }

            var rootNode = JsonNode.Parse(doc.RootElement.GetRawText());
            return rootNode is JsonObject obj ? (obj, true) : (new JsonObject(), false);
        }
        catch (JsonException)
        {
            return (new JsonObject(), false);
        }
    }

    // Purpose: Ensures an object-shaped child exists under the given key on the supplied parent.
    // Expected input: A parent JsonObject and the desired key.
    // Expected output: The existing JsonObject at that key, or a newly attached empty JsonObject when missing or wrong-typed.
    // Critical dependencies: System.Text.Json.Nodes.
    public static JsonObject EnsureObjectChild(JsonObject parent, string key)
    {
        if (parent[key] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[key] = created;
        return created;
    }

    // Purpose: Writes a Claude host config file atomically, creating a one-time .bak backup the first time the installer modifies it.
    // Expected input: Absolute config path, the mutated root JsonObject, and whether the original file existed.
    // Expected output: No return value; temp-file write plus File.Replace so a mid-write crash cannot corrupt the existing file.
    // Critical dependencies: File.Replace for atomic rename, UTF-8 without BOM encoding, and the documented Claude host config contract.
    public static void WriteAtomic(string configPath, JsonObject root, bool originalFileExisted)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var content = root.ToJsonString(serializerOptions);

        var backupPath = configPath + ".bak";
        if (originalFileExisted && !File.Exists(backupPath))
        {
            try
            {
                File.Copy(configPath, backupPath, overwrite: false);
            }
            catch
            {
                // Backup is best-effort. The atomic replace below still protects the live file from mid-write corruption.
            }
        }

        var tempPath = configPath + ".anarchy-tmp";
        File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (File.Exists(configPath))
        {
            // File.Replace requires the destination to exist; it performs an atomic swap on Windows.
            File.Replace(tempPath, configPath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(tempPath, configPath);
        }
    }

    // Purpose: Determines whether an existing mcpServers entry already describes the Anarchy runtime at the expected command path.
    // Expected input: The existing JsonObject entry and the command path the installer wants to register.
    // Expected output: True when the entry's command matches the expected path case-insensitively, otherwise false.
    // Critical dependencies: The documented Claude host stdio entry shape with a "command" string key.
    public static bool EntryMatchesCommand(JsonObject entry, string expectedCommandPath)
    {
        if (entry["command"] is not JsonValue commandValue)
        {
            return false;
        }

        return commandValue.TryGetValue<string>(out var existingCommand)
            && string.Equals(existingCommand, expectedCommandPath, StringComparison.OrdinalIgnoreCase);
    }

    // Purpose: Builds a fresh stdio entry JsonObject for a Claude host mcpServers map.
    // Expected input: The absolute command path and optional args/env maps.
    // Expected output: A JsonObject with "command", "args", and (when non-empty) "env" keys matching the documented stdio schema.
    // Critical dependencies: The Claude Code and Claude Desktop stdio entry contracts.
    public static JsonObject BuildStdioEntry(string commandPath, IReadOnlyList<string>? args, IReadOnlyDictionary<string, string>? env)
    {
        var entry = new JsonObject
        {
            ["command"] = commandPath
        };

        var argsArray = new JsonArray();
        if (args is not null)
        {
            foreach (var arg in args)
            {
                argsArray.Add(arg);
            }
        }
        entry["args"] = argsArray;

        if (env is not null && env.Count > 0)
        {
            var envObj = new JsonObject();
            foreach (var pair in env)
            {
                envObj[pair.Key] = pair.Value;
            }
            entry["env"] = envObj;
        }

        return entry;
    }
}

// Purpose: Registers Anarchy-AI with Claude Code at user scope by merging an mcpServers entry into ~/.claude.json.
// Expected input: The MCP runtime command path for this install plus the shared actions set from SetupEngine.Execute.
// Expected output: An updated ~/.claude.json with a top-level mcpServers[anarchy-ai] entry plus a stable action-taken marker.
// Critical dependencies: ClaudeHostConfigWriter for tolerant read-merge-write, and the documented Claude Code user-scope contract
// (claude mcp add --scope user <name> -- <command>) whose on-disk shape this lane writes directly.
internal static class ClaudeCodeUserScopeLane
{
    // Purpose: Returns the absolute path to ~/.claude.json on the current user profile.
    // Expected input: None; reads the UserProfile special folder.
    // Expected output: Absolute path ending in .claude.json.
    // Critical dependencies: Environment.GetFolderPath and the documented Claude Code user-scope config location.
    public static string GetUserScopeConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude.json");
    }

    // Purpose: Registers the Anarchy-AI MCP runtime with Claude Code at user scope.
    // Expected input: The MCP server name, absolute runtime command path, optional args/env, and the shared actions-taken set.
    // Expected output: No return value; mutates the config file and records an action label.
    // Critical dependencies: ClaudeHostConfigWriter helpers and the documented ~/.claude.json top-level mcpServers contract.
    public static void Register(
        string serverName,
        string commandPath,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        ISet<string> actionsTaken)
    {
        var configPath = GetUserScopeConfigPath();
        var (root, fileExisted) = ClaudeHostConfigWriter.ReadTolerant(configPath);
        var mcpServers = ClaudeHostConfigWriter.EnsureObjectChild(root, "mcpServers");

        if (mcpServers[serverName] is JsonObject existingEntry)
        {
            if (ClaudeHostConfigWriter.EntryMatchesCommand(existingEntry, commandPath))
            {
                actionsTaken.Add("claude_code_user_scope_registration_noop");
                return;
            }

            // Replace our own stale entry (matching name) with the refreshed command path.
            mcpServers[serverName] = ClaudeHostConfigWriter.BuildStdioEntry(commandPath, args, env);
            ClaudeHostConfigWriter.WriteAtomic(configPath, root, fileExisted);
            actionsTaken.Add("claude_code_user_scope_registration_refreshed");
            return;
        }

        mcpServers[serverName] = ClaudeHostConfigWriter.BuildStdioEntry(commandPath, args, env);
        ClaudeHostConfigWriter.WriteAtomic(configPath, root, fileExisted);
        actionsTaken.Add("claude_code_user_scope_registration_added");
    }
}

// Purpose: Registers Anarchy-AI with Claude Desktop by merging an mcpServers entry into the detected claude_desktop_config.json.
// Expected input: The MCP runtime command path for this install plus the shared actions set from SetupEngine.Execute.
// Expected output: An updated Claude Desktop config plus a stable action-taken marker; a clean no-op when no install is detected.
// Critical dependencies: ClaudeDesktopInstallDetector to pick the active config, ClaudeHostConfigWriter for safe read-merge-write,
// and the documented Claude Desktop mcpServers stdio contract.
internal static class ClaudeDesktopLane
{
    // Purpose: Registers the Anarchy-AI MCP runtime with Claude Desktop when an install is detected.
    // Expected input: The MCP server name, absolute runtime command path, optional args/env, and the shared actions-taken set.
    // Expected output: No return value; mutates the detected config file and records an action label per outcome.
    // Critical dependencies: ClaudeDesktopInstallDetector and ClaudeHostConfigWriter helpers.
    public static void Register(
        string serverName,
        string commandPath,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        ISet<string> actionsTaken)
    {
        var installKind = ClaudeDesktopInstallDetector.Detect();
        actionsTaken.Add($"claude_desktop_install_detected_{ClaudeDesktopInstallDetector.ToLabel(installKind)}");

        var configPath = ClaudeDesktopInstallDetector.ResolveActiveConfigPath(installKind);
        if (configPath is null)
        {
            actionsTaken.Add("claude_desktop_registration_skipped_no_install_detected");
            return;
        }

        var (root, fileExisted) = ClaudeHostConfigWriter.ReadTolerant(configPath);
        var mcpServers = ClaudeHostConfigWriter.EnsureObjectChild(root, "mcpServers");

        if (mcpServers[serverName] is JsonObject existingEntry)
        {
            if (ClaudeHostConfigWriter.EntryMatchesCommand(existingEntry, commandPath))
            {
                actionsTaken.Add("claude_desktop_registration_noop");
                return;
            }

            mcpServers[serverName] = ClaudeHostConfigWriter.BuildStdioEntry(commandPath, args, env);
            ClaudeHostConfigWriter.WriteAtomic(configPath, root, fileExisted);
            actionsTaken.Add("claude_desktop_registration_refreshed");
            return;
        }

        mcpServers[serverName] = ClaudeHostConfigWriter.BuildStdioEntry(commandPath, args, env);
        ClaudeHostConfigWriter.WriteAtomic(configPath, root, fileExisted);
        actionsTaken.Add("claude_desktop_registration_added");
    }
}
