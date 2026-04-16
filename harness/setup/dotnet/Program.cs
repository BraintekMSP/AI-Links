using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    Update
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
    public required string[] actions_taken { get; init; }
    public required string[] missing_components { get; init; }
    public required string[] safe_repairs { get; init; }
    public required string next_action { get; init; }
    public required PathRoleCollection paths { get; init; }
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
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

// Purpose: Provides the Windows Forms front end for bounded setup assess/install actions.
// Expected input: User-selected install lane, optional repo path, and button-click actions.
// Expected output: GUI state updates plus serialized setup results displayed to the user.
// Critical dependencies: SetupEngine, ProgramJson, Windows Forms controls, and the shared path canon.
internal sealed class SetupForm : Form
{
    private readonly Label _introLabel;
    private readonly Label _pathLabel;
    private readonly TextBox _repoPathTextBox;
    private readonly TextBox _resultTextBox;
    private readonly Label _statusLabel;
    private Label _subtitleLabel = null!;
    private readonly RadioButton _repoLocalRadioButton;
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
            Text = "Install Lane",
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
        _repoLocalRadioButton = new RadioButton
        {
            AutoSize = true,
            Text = "Repo-local install",
            Checked = true,
            Margin = new Padding(0, 2, 0, 4)
        };
        _repoLocalRadioButton.CheckedChanged += (_, _) => UpdateActionButtons();
        installLaneFlow.Controls.Add(_repoLocalRadioButton);

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
        hostTargetFlow.Controls.Add(_codexHostCheckBox);

        _claudeCodeHostCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Claude Code (user scope; unverified on this machine)",
            Checked = false,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 4)
        };
        hostTargetFlow.Controls.Add(_claudeCodeHostCheckBox);

        _claudeDesktopHostCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Claude Desktop (auto-detected install; unverified on this machine)",
            Checked = false,
            Enabled = true,
            Margin = new Padding(0, 2, 0, 0)
        };
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
        pathSectionPanel.Controls.Add(_targetRepoPanel, 0, 1);

        rootPanel.Controls.Add(pathSectionPanel, 0, 4);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };

        _assessButton = new Button { Text = "Assess", AutoSize = false, Width = 220, Height = 36 };
        _assessButton.Click += (_, _) => Execute(OperationMode.Assess);
        buttonPanel.Controls.Add(_assessButton);

        _installButton = new Button { Text = "Install", AutoSize = false, Width = 220, Height = 36 };
        _installButton.Click += (_, _) => Execute(OperationMode.Install);
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
    // Critical dependencies: Current install-lane selection and path-presentation update guards.
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

    // Purpose: Runs an assess or install action from the GUI and displays the resulting JSON state.
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
            if (mode == OperationMode.Install)
            {
                var disclosure = new InstallDisclosureForm(
                    effectiveRepoPath,
                    SetupEngine.BuildInstallDisclosure(effectiveRepoPath, installScope, selectedHostTargets),
                    installScope,
                    selectedHostTargets);

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
                InstallScope = installScope,
                HostContext = "codex",
                HostTargets = selectedHostTargets,
                RepoPath = effectiveRepoPath
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

    // Purpose: Resolves the install scope selected by the radio-button group.
    // Expected input: Current radio-button state.
    // Expected output: RepoLocal or UserProfile.
    // Critical dependencies: The setup form radio controls.
    private InstallScope GetSelectedInstallScope()
    {
        return _userProfileRadioButton.Checked ? InstallScope.UserProfile : InstallScope.RepoLocal;
    }

    // Purpose: Resolves the selected host targets from the host-target checkbox group.
    // Expected input: Current checkbox state of Codex, Claude Code, and Claude Desktop host toggles.
    // Expected output: A HostTargets bitmask describing every host lane the current GUI run should register.
    // Critical dependencies: The host-target checkbox controls and the default-to-Codex fallback when all are unchecked.
    private HostTargets GetSelectedHostTargets()
    {
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
            : "Assess Repo-Local";
        _installButton.Text = selectedScope == InstallScope.UserProfile
            ? "Install User-Profile"
            : "Install Repo-Local";
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
            _subtitleLabel.Text = "User-profile harness install and assessment";
            _introLabel.Text = $"Install or assess {AnarchyBranding.BrandDisplayName} through the current user profile. User-profile install keeps the harness under the current user profile instead of attaching it to one repo.";
            return;
        }

        _subtitleLabel.Text = "Repo-local harness install and assessment";
        _introLabel.Text = $"Install or assess {AnarchyBranding.BrandDisplayName} for a selected repo. Repo-local install keeps the harness inside that repo so the delivery surface travels with the workspace.";
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
                _pathLabel.Text = "Install Root:";
                _repoPathTextBox.Text = AnarchyPathCanon.ResolveUserProfilePluginRoot(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    GeneratedAnarchyPathCanon.DefaultPluginName);
                _repoPathTextBox.ReadOnly = true;
                _repoPathTextBox.BackColor = System.Drawing.SystemColors.Control;
                _browseButton.Visible = false;
                _browseButton.Enabled = false;
                _targetRepoPanel.Visible = false;
                return;
            }

            _pathLabel.Text = "Repo Path:";
            _repoPathTextBox.Text = _selectedRepoPath;
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
}

// Purpose: Presents the install disclosure that the user must review before a GUI install proceeds.
// Expected input: Repo path context, generated disclosure text, and the chosen install scope.
// Expected output: A modal dialog with continue/back actions.
// Critical dependencies: SetupEngine.BuildInstallDisclosure and Windows Forms controls.
internal sealed class InstallDisclosureForm : Form
{
    // Purpose: Builds the modal disclosure dialog for a pending install action.
    // Expected input: Repo-path context, disclosure text, chosen install scope, and chosen host targets.
    // Expected output: A ready-to-show modal form instance.
    // Critical dependencies: Generated disclosure text, SetupWindowIcon, and Windows Forms layout controls.
    public InstallDisclosureForm(string repoPath, string disclosureText, InstallScope installScope, HostTargets hostTargets)
    {
        Text = "Install Disclosure";
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
            Text = $"Target repo:{Environment.NewLine}{repoPath}{Environment.NewLine}Install lane:{Environment.NewLine}{(installScope == InstallScope.UserProfile ? "User-profile" : "Repo-local")}{Environment.NewLine}Host targets:{Environment.NewLine}{HostTargetLabels.ToDisplayString(hostTargets)}",
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
            Text = "Continue Install",
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
                case "repolocal":
                    installScope = InstallScope.RepoLocal;
                    break;
                case "userprofile":
                    installScope = InstallScope.UserProfile;
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

        return new SetupOptions
        {
            Mode = mode,
            InstallScope = installScope,
            HostContext = hostContext,
            HostTargets = hostTargets,
            Silent = silent,
            JsonOutput = jsonOutput,
            RefreshPortableSchemaFamily = refreshPortableSchemaFamily,
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
        var disclosureLines = new List<string>
        {
            $"Responsible disclosure for {BuildInstallScopeLabel(installScope).ToLowerInvariant()} {AnarchyBranding.BrandDisplayName} install.",
            "All carried schema, contract, and install surfaces remain authored in this repo and are published into the standalone installer payload.",
            $"Install root: {installRoot}",
            installScope == InstallScope.UserProfile
                ? $"Workspace target: {targetRepo}"
                : $"Target repo: {targetRepo}",
            "Install impact:",
            $"- Adds {pluginFolder}\\ with {PluginSurfaces.Count} bundled surfaces.",
            $"- Creates or updates {marketplacePath}.",
            installScope == InstallScope.UserProfile
                ? $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the current user profile marketplace."
                : $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the target repo.",
            installScope == InstallScope.UserProfile
                ? $"- Uses the Codex-native plugin marketplace lane; it does not require a custom mcp_servers.{BuildMcpServerName()} block to count as ready."
                : $"- Leaves {AnarchyPathCanon.BuildHomeLabelPath(AnarchyPathCanon.UserProfileCodexConfigFileRelativePath)} untouched in the repo-local lane.",
            "- Current GUI install does not rewrite AGENTS.md."
        };

        if (installScope == InstallScope.RepoLocal && hostTargets.HasFlag(HostTargets.Codex))
        {
            disclosureLines.Add("- Repo-local Codex caveat: the installer writes the Codex-documented repo-local shape (marketplace.json + plugins/ bundle), but this lane has not yet been observed producing a callable plugin in Codex's plugin surface. The direct MCP server surface is the only observed working path, and cross-machine / cross-session verification is stale. Treat repo-local as unproven until a promotion test lands in the Environment Truth Matrix. The bundled runtime is also per-machine, so a committed marketplace entry does not carry a working runtime to collaborators.");
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
            "  AnarchyAi.Setup.exe /install [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /update [/repolocal|/userprofile] [/repo <path>] [/codex] [/claudecode] [/claudedesktop] [/allhosts] [/sourcepath <path>] [/sourceurl <url>] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /? | -? | /h | -h | /help | -help | --help | --?",
            string.Empty,
            "Availability:",
            $"  {availabilityLead}",
            "  Installing it would give the target repo preflight, gap assessment, and schema reality checks.",
            "  It also exposes active-work compilation and gov2gov reconciliation through the same harness surface.",
            $"  Target repo: {targetRepo}",
            string.Empty,
            "Here's what changes:",
            $"- /repolocal (home-local runtime + repo-local marketplace, Codex) adds {BuildPluginFolderLabel(InstallScope.RepoLocal, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.RepoLocal, resolvedRepo)}.",
            $"- /userprofile (home-local runtime + home-local marketplace, Codex) adds {BuildPluginFolderLabel(InstallScope.UserProfile, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.UserProfile, resolvedRepo)}.",
            $"- /repolocal registers {BuildPluginName(InstallScope.RepoLocal, resolvedRepo)} for the selected repo.",
            $"- /userprofile registers {BuildPluginName(InstallScope.UserProfile, resolvedRepo)} for the current user profile.",
            $"- /userprofile uses the Codex-native plugin marketplace lane rather than requiring a custom mcp_servers.{BuildMcpServerName()} block.",
            "- /repolocal places the runtime binary under the target repo's plugins folder on this machine; collaborators need their own install to get a working runtime even when the marketplace entry is committed.",
            "- Host targets default to Codex when no /codex|/claudecode|/claudedesktop|/allhosts flag is passed; multiple host flags combine (e.g. /codex /claudecode).",
            $"- /claudecode adds an mcpServers.{BuildMcpServerName()} entry to ~/.claude.json at user scope (read-merge-write; creates a .bak on first modification). Requires a Claude Code restart. Unverified on this machine -- promotion pending.",
            $"- /claudedesktop auto-detects MSIX vs classic Claude Desktop and merges mcpServers.{BuildMcpServerName()} into the active claude_desktop_config.json (read-merge-write; creates a .bak on first modification; no-op when no install is detected). Requires a full app restart; older MSIX builds may ignore mcpServers (upstream issue) -- update and retry. Unverified on this machine -- promotion pending.",
            $"- Makes {CoreToolNames.Length} core + {ExperimentalToolNames.Length} test harness tool available to supported hosts.",
            "- Does not rewrite AGENTS.md.",
            schemaSeedingLine,
            "- Leaves existing root schema files in place unless /refreshschemas is passed.",
            string.Empty,
            "Flags:",
            "  /repolocal             Install or assess through the selected repo root.",
            "  /userprofile           Install or assess through the current user profile.",
            "  /repo <path>            Override repo auto-detection.",
            "  /sourcepath <path>      Refresh from a local AI-Links source path.",
            "  /sourceurl <url>        Refresh from a zip source URL.",
            "  /refreshschemas         Force-refresh the portable schema family into repo root.",
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
        var runtimePath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath);
        var pluginManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundlePluginManifestFileRelativePath);
        var mcpPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleMcpFileRelativePath);
        var skillPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSkillFileRelativePath);
        var schemaManifestPath = AnarchyPathCanon.ResolveBundleFilePath(pluginRoot, AnarchyPathCanon.BundleSchemaManifestFileRelativePath);
        var updateSourceRoot = string.Empty;

        var actionsTaken = new HashSet<string>(StringComparer.Ordinal);
        var missingComponents = new HashSet<string>(StringComparer.Ordinal);
        var safeRepairs = new HashSet<string>(StringComparer.Ordinal);
        var updateNotes = new HashSet<string>(StringComparer.Ordinal);

        var updateRequested = options.Mode == OperationMode.Update;
        var updateState = updateRequested ? "in_progress" : "not_requested";

        if (options.Mode == OperationMode.Install)
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
                    ExtractEmbeddedPortableSchemaFamily(workspaceRoot, actionsTaken);
                }
                else
                {
                    SeedMissingEmbeddedPortableSchemaFamily(workspaceRoot, actionsTaken);
                }
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

        foreach (var contractFile in CoreContractFiles)
        {
            var contractPath = AnarchyPathCanon.ResolveBundleFilePath(
                pluginRoot,
                AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, contractFile));
            if (!File.Exists(contractPath))
            {
                missingComponents.Add($"missing_contract:{contractFile}");
            }
        }

        var experimentalDirectionAssistContractPath = AnarchyPathCanon.ResolveBundleFilePath(
            pluginRoot,
            AnarchyPathCanon.CombineCanonRelativePath(AnarchyPathCanon.BundleContractsDirectoryRelativePath, ExperimentalDirectionAssistContract));
        if (!File.Exists(experimentalDirectionAssistContractPath))
        {
            actionsTaken.Add("experimental_direction_assist_contract_missing_non_blocking");
        }

        var pluginManifestInspection = InspectPluginManifest(pluginManifestPath, options.InstallScope, workspaceRoot);
        missingComponents.UnionWith(pluginManifestInspection.Findings);
        var marketplaceInspection = InspectMarketplace(marketplacePath, options.InstallScope, workspaceRoot);
        missingComponents.UnionWith(marketplaceInspection.Findings);
        var mcpInspection = InspectMcpConfiguration(mcpPath);
        missingComponents.UnionWith(mcpInspection.Findings);
        var marketplaceMissingFinding = BuildMarketplaceMissingFinding(options.InstallScope);
        var marketplaceMissingPluginsArrayFinding = BuildMarketplaceMissingPluginsArrayFinding(options.InstallScope);
        if (!marketplaceInspection.Exists)
        {
            missingComponents.Add(marketplaceMissingFinding);
        }
        if (marketplaceInspection.Exists && !marketplaceInspection.HasPluginsArray)
        {
            missingComponents.Add(marketplaceMissingPluginsArrayFinding);
        }
        if (marketplaceInspection.Exists && !marketplaceInspection.IsValidJson)
        {
            missingComponents.Add("marketplace_json_invalid");
        }

        var marketplaceRegistrationReady = runtimeExists
            && marketplaceInspection.HasAnarchyPluginEntry
            && marketplaceInspection.InstalledByDefault
            && pluginManifestInspection.IdentityAligned
            && marketplaceInspection.MarketplaceIdentityAligned
            && mcpInspection.IdentityAligned;
        var legacyUserProfileInspection = InspectLegacyUserProfileSurfaces(options.InstallScope, normalizedHostContext, marketplaceRegistrationReady);
        missingComponents.UnionWith(legacyUserProfileInspection.Findings);

        if (!runtimeExists) { safeRepairs.Add("publish_or_restore_bundled_runtime"); }
        if (!marketplaceInspection.HasAnarchyPluginEntry || !marketplaceInspection.InstalledByDefault)
        {
            safeRepairs.Add(options.InstallScope == InstallScope.UserProfile
                ? "run_user_profile_harness_install"
                : "run_bootstrap_harness_install");
        }
        if (!pluginManifestInspection.IdentityAligned)
        {
            safeRepairs.Add(options.InstallScope == InstallScope.UserProfile
                ? "refresh_user_profile_plugin_identity"
                : "refresh_repo_plugin_identity");
        }
        if (!marketplaceInspection.MarketplaceIdentityAligned)
        {
            safeRepairs.Add(options.InstallScope == InstallScope.UserProfile
                ? "refresh_user_profile_marketplace_identity"
                : "refresh_repo_marketplace_identity");
        }
        if (!mcpInspection.IdentityAligned)
        {
            safeRepairs.Add("refresh_mcp_server_identity");
        }
        if (legacyUserProfileInspection.LegacyPluginRootPresent)
        {
            safeRepairs.Add("inventory_and_manually_quarantine_legacy_user_profile_plugin_root");
        }
        if (legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent)
        {
            safeRepairs.Add("inventory_and_remove_stale_codex_custom_mcp_entry");
        }

        var hasLockedBundleSurfaceSkip = missingComponents.Contains("locked_bundle_surface_write_skipped");
        var hasBlockingLegacySurface = legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent;
        var bootstrapState = !hasLockedBundleSurfaceSkip
            && marketplaceRegistrationReady
            && !hasBlockingLegacySurface
            ? "ready"
        : runtimeExists && marketplaceInspection.HasAnarchyPluginEntry && marketplaceInspection.InstalledByDefault
            ? "registration_refresh_needed"
                : runtimeExists && (pluginManifestExists || mcpExists)
                    ? "repo_bundle_present_unregistered"
                : runtimeExists
                    ? "runtime_only"
                    : "bootstrap_needed";
        var registrationMode = DetermineRegistrationMode(options.InstallScope, normalizedHostContext, legacyUserProfileInspection);

        var nextAction = hasLockedBundleSurfaceSkip
            ? "release_runtime_lock_and_retry_install"
            : hasBlockingLegacySurface
                ? "inventory_legacy_home_install_and_run_user_profile_install"
            : bootstrapState switch
        {
            "ready" => "use_preflight_session",
            "registration_refresh_needed" => "refresh_plugin_registration",
            "repo_bundle_present_unregistered" => "register_plugin_in_marketplace",
            "runtime_only" => "materialize_repo_plugin_bundle",
            _ => "restore_runtime_or_complete_bundle"
        };

        var resultPaths = BuildSetupResultPaths(
            options,
            workspaceRoot,
            pluginRoot,
            marketplacePath,
            runtimePath,
            pluginManifestPath,
            mcpPath,
            skillPath,
            schemaManifestPath,
            updateSourceRoot);

        return new SetupResult
        {
            bootstrap_state = bootstrapState,
            registration_mode = registrationMode,
            host_context = normalizedHostContext,
            host_targets = HostTargetLabels.ToLabelArray(options.HostTargets),
            install_scope = options.InstallScope == InstallScope.UserProfile ? "user_profile" : "repo_local",
            update_requested = updateRequested,
            update_state = updateState,
            update_source_zip_url = options.UpdateSourceZipUrl,
            update_notes = updateNotes.ToArray(),
            runtime_present = runtimeExists,
            marketplace_registered = marketplaceInspection.HasAnarchyPluginEntry,
            installed_by_default = marketplaceInspection.InstalledByDefault,
            actions_taken = actionsTaken.ToArray(),
            missing_components = missingComponents.ToArray(),
            safe_repairs = safeRepairs.ToArray(),
            next_action = nextAction,
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
    // Expected input: Install scope and optional repo root used for repo-local directory scoping.
    // Expected output: The shared user-profile directory name or a repo-scoped local directory name with a stable path hash.
    // Critical dependencies: GeneratedAnarchyPathCanon templates, NormalizeMarketplaceSlug, and SHA256 hashing.
    internal static string BuildPluginDirectoryName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return GeneratedAnarchyPathCanon.DefaultPluginName;
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return GeneratedAnarchyPathCanon.RepoScopedPluginDirectoryNameTemplate;
        }

        var repoName = new DirectoryInfo(repoRoot).Name;
        var slug = NormalizeMarketplaceSlug(repoName);
        var repoPathHash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(repoRoot).ToLowerInvariant()));
        var suffix = Convert.ToHexString(repoPathHash.AsSpan(0, 4)).ToLowerInvariant();
        return GeneratedAnarchyPathCanon.RepoScopedPluginDirectoryNameTemplate
            .Replace("<repo-slug>", slug, StringComparison.Ordinal)
            .Replace("<stable-path-hash>", suffix, StringComparison.Ordinal);
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
    internal static string DetermineRegistrationMode(InstallScope installScope, string normalizedHostContext, LegacyUserProfileInspection legacyUserProfileInspection)
    {
        if (installScope == InstallScope.UserProfile
            && string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal)
            && legacyUserProfileInspection.LegacyCodexCustomMcpEntryPresent
            && !legacyUserProfileInspection.NewPluginMarketplaceLaneReady)
        {
            return "custom_mcp_fallback";
        }

        return "plugin_marketplace";
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
        var origin = BuildSetupOriginRoleReport(options, updateSourceRoot);
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
    private static PathRoleReport? BuildSetupOriginRoleReport(SetupOptions options, string updateSourceRoot)
    {
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

        return AnarchyPathCanon.CreateRoleReport(
            rootPath: destinationRoot,
            directories:
            [
                CreatePathEntry("plugin_root_directory_path", pluginRoot),
                CreatePathEntry("marketplace_directory_path", Path.GetDirectoryName(marketplacePath)),
                CreatePathEntry("schema_target_root_directory_path", workspaceRoot)
            ],
            files:
            [
                CreatePathEntry("marketplace_file_path", marketplacePath),
                CreatePathEntry("plugin_manifest_file_path", pluginManifestPath),
                CreatePathEntry("mcp_declaration_file_path", mcpPath),
                CreatePathEntry("runtime_executable_file_path", runtimePath),
                CreatePathEntry("skill_file_path", skillPath),
                CreatePathEntry("schema_manifest_file_path", schemaManifestPath),
                CreatePathEntry(
                    "codex_config_file_path",
                    options.InstallScope == InstallScope.UserProfile
                        ? AnarchyPathCanon.ResolveUserProfileCodexConfigFilePath(GetUserProfileDirectory())
                        : null)
            ],
            relative:
            [
                CreatePathEntry(
                    "marketplace_file_relative_path",
                    options.InstallScope == InstallScope.UserProfile
                        ? AnarchyPathCanon.UserProfileMarketplaceFileRelativePath
                        : AnarchyPathCanon.RepoLocalMarketplaceFileRelativePath),
                CreatePathEntry("plugin_source_relative_path", BuildPluginRelativePath(options.InstallScope, workspaceRoot)),
                CreatePathEntry("plugin_manifest_file_relative_path", AnarchyPathCanon.BundlePluginManifestFileRelativePath),
                CreatePathEntry("mcp_declaration_file_relative_path", AnarchyPathCanon.BundleMcpFileRelativePath),
                CreatePathEntry("runtime_executable_file_relative_path", AnarchyPathCanon.BundleRuntimeExecutableFileRelativePath),
                CreatePathEntry("schema_manifest_file_relative_path", AnarchyPathCanon.BundleSchemaManifestFileRelativePath),
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
