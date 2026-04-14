using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AnarchyAi.Setup;

internal enum OperationMode
{
    Assess,
    Install,
    Update
}

internal enum InstallScope
{
    RepoLocal,
    UserProfile
}

internal sealed class SetupOptions
{
    public OperationMode Mode { get; init; } = OperationMode.Assess;
    public InstallScope InstallScope { get; init; } = InstallScope.RepoLocal;
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
    public required string registration_mode { get; init; }
    public required string host_context { get; init; }
    public required string install_scope { get; init; }
    public required string workspace_root { get; init; }
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
    private readonly Label _introLabel;
    private readonly Label _pathLabel;
    private readonly TextBox _repoPathTextBox;
    private readonly TextBox _resultTextBox;
    private readonly Label _statusLabel;
    private Label _subtitleLabel = null!;
    private readonly RadioButton _repoLocalRadioButton;
    private readonly RadioButton _userProfileRadioButton;
    private readonly Button _assessButton;
    private readonly Button _installButton;
    private readonly Button _browseButton;
    private readonly TableLayoutPanel _targetRepoPanel;
    private readonly TextBox _targetRepoTextBox;
    private readonly Button _targetRepoBrowseButton;
    private string _selectedRepoPath;
    private bool _updatingPathPresentation;

    public SetupForm()
    {
        Text = "Anarchy-AI Setup";
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
            RowCount = 6,
            Padding = new Padding(14)
        };
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

        rootPanel.Controls.Add(pathSectionPanel, 0, 3);

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
        rootPanel.Controls.Add(buttonPanel, 0, 4);

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
        rootPanel.Controls.Add(_resultTextBox, 0, 5);

        Controls.Add(rootPanel);
        UpdateActionButtons();
    }

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
            Text = "Anarchy-AI Setup",
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

    private static PictureBox? BuildLogoPictureBox()
    {
        var image = ResourceImageLoader.TryLoadPng("SetupPayload/plugins/anarchy-ai/assets/anarchy-ai.png");
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

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        if (GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return;
        }

        BrowseForRepoSelection(_repoPathTextBox.Text, path => _repoPathTextBox.Text = path);
    }

    private void TargetRepoBrowseButton_Click(object? sender, EventArgs e)
    {
        BrowseForRepoSelection(_targetRepoTextBox.Text, path => _targetRepoTextBox.Text = path);
    }

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

    private void RepoPathTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_updatingPathPresentation || GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return;
        }

        _selectedRepoPath = _repoPathTextBox.Text;
    }

    private void TargetRepoTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_updatingPathPresentation)
        {
            return;
        }

        _selectedRepoPath = _targetRepoTextBox.Text;
    }

    private void Execute(OperationMode mode)
    {
        try
        {
            var installScope = GetSelectedInstallScope();
            var effectiveRepoPath = GetEffectiveRepoPath();
            if (mode == OperationMode.Install)
            {
                var disclosure = new InstallDisclosureForm(
                    effectiveRepoPath,
                    SetupEngine.BuildInstallDisclosure(effectiveRepoPath, installScope),
                    installScope);

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

    private InstallScope GetSelectedInstallScope()
    {
        return _userProfileRadioButton.Checked ? InstallScope.UserProfile : InstallScope.RepoLocal;
    }

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

    private void UpdateHeaderCopy(InstallScope installScope)
    {
        if (installScope == InstallScope.UserProfile)
        {
            _subtitleLabel.Text = "User-profile harness install and assessment";
            _introLabel.Text = "Install or assess Anarchy-AI through the current user profile. User-profile install keeps the harness under the current user profile instead of attaching it to one repo.";
            return;
        }

        _subtitleLabel.Text = "Repo-local harness install and assessment";
        _introLabel.Text = "Install or assess Anarchy-AI for a selected repo. Repo-local install keeps the harness inside that repo so the delivery surface travels with the workspace.";
    }

    private void UpdatePathPresentation(InstallScope installScope)
    {
        _updatingPathPresentation = true;
        try
        {
            if (installScope == InstallScope.UserProfile)
            {
                _pathLabel.Text = "Install Root:";
                _repoPathTextBox.Text = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "plugins",
                    "anarchy-ai");
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

    private string GetEffectiveRepoPath()
    {
        if (GetSelectedInstallScope() == InstallScope.UserProfile)
        {
            return string.Empty;
        }

        return _selectedRepoPath.Trim();
    }
}

internal sealed class InstallDisclosureForm : Form
{
    public InstallDisclosureForm(string repoPath, string disclosureText, InstallScope installScope)
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
            Text = $"Target repo:{Environment.NewLine}{repoPath}{Environment.NewLine}Install lane:{Environment.NewLine}{(installScope == InstallScope.UserProfile ? "User-profile" : "Repo-local")}",
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

internal static class SetupWindowIcon
{
    public static System.Drawing.Icon? Create()
    {
        return ResourceIconLoader.TryLoadIcon("SetupPayload/plugins/anarchy-ai/assets/anarchy-ai.ico")
            ?? SafeExtractExecutableIcon();
    }

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

internal static class ResourceImageLoader
{
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

    private static Stream? TryOpenResourceStream(string logicalSuffix)
    {
        var normalizedSuffix = logicalSuffix.Replace('\\', '/');
        var resourceName = PayloadResources.GetPluginBundleResources()
            .FirstOrDefault(name =>
                name.Replace('\\', '/').EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase));

        return resourceName is null ? null : PayloadResources.OpenResource(resourceName);
    }
}

internal static class ResourceIconLoader
{
    public static System.Drawing.Icon? TryLoadIcon(string logicalSuffix)
    {
        using var stream = TryOpenResourceStream(logicalSuffix);
        return stream is null ? null : new System.Drawing.Icon(stream);
    }

    private static Stream? TryOpenResourceStream(string logicalSuffix)
    {
        var normalizedSuffix = logicalSuffix.Replace('\\', '/');
        var resourceName = PayloadResources.GetPluginBundleResources()
            .FirstOrDefault(name =>
                name.Replace('\\', '/').EndsWith(normalizedSuffix, StringComparison.OrdinalIgnoreCase));

        return resourceName is null ? null : PayloadResources.OpenResource(resourceName);
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
        var installScope = InstallScope.RepoLocal;
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
            InstallScope = installScope,
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
    private const string CodexCustomMcpServerBlockPattern = @"(?ms)^\[mcp_servers\.anarchy-ai\]\r?\n(?:.*?\r?\n)*(?=^\[|\z)";

    private static readonly string[] CoreContractFiles =
    [
        "active-work-state.contract.json",
        "schema-reality.contract.json",
        "gov2gov-migration.contract.json",
        "preflight-session.contract.json",
        "harness-gap-state.contract.json"
    ];

    private const string ExperimentalDirectionAssistContract = "direction-assist-test.contract.json";

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

    public static string BuildInstallDisclosure(string repoPath, InstallScope installScope)
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
            $"Responsible disclosure for {BuildInstallScopeLabel(installScope).ToLowerInvariant()} Anarchy-AI install.",
            "All carried schema, contract, and install surfaces remain authored in this repo and are published into the standalone installer payload.",
            $"Install root: {installRoot}",
            installScope == InstallScope.UserProfile
                ? $"Workspace target: {targetRepo}"
                : $"Target repo: {targetRepo}",
            "Install impact:",
            $"- Adds {pluginFolder}\\ with {PluginSurfaces.Length} bundled surfaces.",
            $"- Creates or updates {marketplacePath}.",
            installScope == InstallScope.UserProfile
                ? $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the current user profile marketplace."
                : $"- Registers {pluginName} as INSTALLED_BY_DEFAULT in the target repo.",
            installScope == InstallScope.UserProfile
                ? "- Uses the Codex-native plugin marketplace lane; it does not require a custom mcp_servers.anarchy-ai block to count as ready."
                : "- Leaves ~/.codex/config.toml untouched in the repo-local lane.",
            "- Current GUI install does not rewrite AGENTS.md."
        };

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
                ? "- Makes Anarchy-AI available through the current user profile for supported hosts."
                : "- Makes Anarchy-AI available by default to agents in this repo.",
            "- Strengthens startup/control surfaces; it does not rewrite project code by itself.",
            "Back out now if this repo should remain unchanged."
        ]);

        return string.Join(Environment.NewLine, disclosureLines);
    }

    public static string BuildCommandLineHelp(string? repoPath)
    {
        var resolvedRepo = string.IsNullOrWhiteSpace(repoPath)
            ? TryResolveDefaultRepoRoot()
            : Path.GetFullPath(repoPath);
        var workspaceTargeted = !string.IsNullOrWhiteSpace(resolvedRepo);
        var targetRepo = workspaceTargeted ? resolvedRepo! : "(repo path unresolved)";
        var availabilityLead = workspaceTargeted
            ? "This repo has Anarchy-AI available."
            : "Anarchy-AI can be installed into a target repo.";
        var schemaSeedingLine = workspaceTargeted
            ? "- Seeds missing portable root schema files during install."
            : "- Seeds missing portable root schema files only when a workspace root is targeted (/repolocal or /userprofile with /repo).";
        var lines = new[]
        {
            "Anarchy-AI Setup",
            string.Empty,
            "Usage:",
            "  AnarchyAi.Setup.exe /assess [/repolocal|/userprofile] [/repo <path>] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /install [/repolocal|/userprofile] [/repo <path>] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /update [/repolocal|/userprofile] [/repo <path>] [/sourcepath <path>] [/sourceurl <url>] [/refreshschemas] [/json] [/silent]",
            "  AnarchyAi.Setup.exe /? | -? | /h | -h | /help | -help | --help | --?",
            string.Empty,
            "Availability:",
            $"  {availabilityLead}",
            "  Installing it would give the target repo preflight, gap assessment, and schema reality checks.",
            "  It also exposes active-work compilation and gov2gov reconciliation through the same harness surface.",
            $"  Target repo: {targetRepo}",
            string.Empty,
            "Here's what changes:",
            $"- /repolocal adds {BuildPluginFolderLabel(InstallScope.RepoLocal, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.RepoLocal, resolvedRepo)}.",
            $"- /userprofile adds {BuildPluginFolderLabel(InstallScope.UserProfile, resolvedRepo)}\\ and updates {BuildMarketplacePathLabel(InstallScope.UserProfile, resolvedRepo)}.",
            $"- /repolocal registers {BuildPluginName(InstallScope.RepoLocal, resolvedRepo)} for the selected repo.",
            $"- /userprofile registers {BuildPluginName(InstallScope.UserProfile, resolvedRepo)} for the current user profile.",
            "- /userprofile uses the Codex-native plugin marketplace lane rather than requiring a custom mcp_servers.anarchy-ai block.",
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
            "  /host <name>            Carry host context such as codex, claude, cursor, or generic."
        };

        return string.Join(Environment.NewLine, lines);
    }

    public SetupResult Execute(SetupOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var normalizedHostContext = NormalizeHostContext(options.HostContext);
        var workspaceRoot = ResolveWorkspaceRoot(options.InstallScope, options.RepoPath);
        var pluginRoot = ResolvePluginRoot(options.InstallScope, workspaceRoot);
        var marketplacePath = ResolveMarketplacePath(options.InstallScope, workspaceRoot);
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
            try
            {
                ExtractEmbeddedPluginBundle(pluginRoot, actionsTaken);
                EnsurePluginManifestIdentity(pluginManifestPath, options.InstallScope, workspaceRoot, actionsTaken);
                EnsurePluginMcpConfiguration(mcpPath, actionsTaken);
                EnsureMarketplaceRegistration(marketplacePath, options.InstallScope, workspaceRoot, actionsTaken);

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
                RefreshFromUpdateSource(pluginRoot, workspaceRoot, options, actionsTaken, updateNotes);
                EnsurePluginManifestIdentity(pluginManifestPath, options.InstallScope, workspaceRoot, actionsTaken);
                EnsurePluginMcpConfiguration(mcpPath, actionsTaken);
                EnsureMarketplaceRegistration(marketplacePath, options.InstallScope, workspaceRoot, actionsTaken);
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
            var contractPath = Path.Combine(pluginRoot, "contracts", contractFile);
            if (!File.Exists(contractPath))
            {
                missingComponents.Add($"missing_contract:{contractFile}");
            }
        }

        var experimentalDirectionAssistContractPath = Path.Combine(pluginRoot, "contracts", ExperimentalDirectionAssistContract);
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

        return new SetupResult
        {
            bootstrap_state = bootstrapState,
            registration_mode = registrationMode,
            host_context = normalizedHostContext,
            install_scope = options.InstallScope == InstallScope.UserProfile ? "user_profile" : "repo_local",
            workspace_root = workspaceRoot,
            update_requested = updateRequested,
            update_state = updateState,
            update_source_zip_url = options.UpdateSourceZipUrl,
            update_source_path = options.UpdateSourcePath,
            update_notes = updateNotes.ToArray(),
            repo_root = workspaceRoot,
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

    private static void EnsureCodexCustomMcpRegistration(InstallScope installScope, string normalizedHostContext, string pluginRoot, HashSet<string> actionsTaken)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return;
        }

        var codexConfigPath = Path.Combine(GetUserProfileDirectory(), ".codex", "config.toml");
        var codexConfigDirectory = Path.GetDirectoryName(codexConfigPath)!;
        if (!Directory.Exists(codexConfigDirectory))
        {
            Directory.CreateDirectory(codexConfigDirectory);
            actionsTaken.Add("created_codex_config_directory");
        }

        var content = File.Exists(codexConfigPath) ? File.ReadAllText(codexConfigPath) : string.Empty;
        if (!File.Exists(codexConfigPath))
        {
            actionsTaken.Add("created_codex_config_file");
        }

        var expectedServerBlock = BuildExpectedCodexMcpServerBlock(pluginRoot, DetectNewline(content));
        if (UpsertTomlServerBlock(ref content, expectedServerBlock))
        {
            File.WriteAllText(codexConfigPath, content);
            actionsTaken.Add("updated_codex_custom_mcp_server_entry");
        }
        else
        {
            actionsTaken.Add("codex_custom_mcp_server_already_registered");
        }
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
        // Auto-detection should only trust an actual repo marker.
        // Generic parent folders can also contain "plugins" or ".agents",
        // which makes those signals too weak for safe default resolution.
        return Directory.Exists(Path.Combine(path, ".git")) ||
               File.Exists(Path.Combine(path, ".git"));
    }

    private static bool IsRuntimeLockException(IOException ex)
    {
        return ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("access is denied", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("access to the path", StringComparison.OrdinalIgnoreCase);
    }

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

    private static void ExtractEmbeddedPluginBundle(string pluginRoot, HashSet<string> actionsTaken)
    {
        var retainedLockedSurfaceWithoutDrift = false;
        var skippedLockedSurfaceWithUnknownDrift = false;

        Directory.CreateDirectory(pluginRoot);
        foreach (var resource in PayloadResources.GetPluginBundleResources())
        {
            var relativePath = resource["SetupPayload/plugins/anarchy-ai/".Length..]
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

    private static void SeedMissingEmbeddedPortableSchemaFamily(string repoRoot, HashSet<string> actionsTaken)
    {
        var copiedAny = false;

        foreach (var resource in PayloadResources.GetPortableSchemaResources())
        {
            var fileName = resource["SetupPayload/portable-schema/".Length..]
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

        if (!string.Equals(existingServer["command"]?.GetValue<string>(), ".\\runtime\\win-x64\\AnarchyAi.Mcp.Server.exe", StringComparison.Ordinal) ||
            existingServer["args"] is not JsonArray ||
            !string.Equals(existingServer["cwd"]?.GetValue<string>(), ".", StringComparison.Ordinal))
        {
            updatedMcpIdentity = true;
        }

        existingServer["command"] = ".\\runtime\\win-x64\\AnarchyAi.Mcp.Server.exe";
        existingServer["args"] = new JsonArray();
        existingServer["cwd"] = ".";

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

    private static CodexCustomMcpInspection InspectCodexCustomMcpConfiguration(InstallScope installScope, string normalizedHostContext, string pluginRoot)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return new CodexCustomMcpInspection(false, true, true, []);
        }

        var codexConfigPath = Path.Combine(GetUserProfileDirectory(), ".codex", "config.toml");
        if (!File.Exists(codexConfigPath))
        {
            return new CodexCustomMcpInspection(true, false, false, ["codex_config_missing"]);
        }

        var content = File.ReadAllText(codexConfigPath);
        var blockMatch = Regex.Match(content, @"(?ms)^\[mcp_servers\.anarchy-ai\]\r?\n(?:.*?\r?\n)*(?=^\[|\z)");
        if (!blockMatch.Success)
        {
            return new CodexCustomMcpInspection(true, false, false, ["codex_mcp_server_entry_missing"]);
        }

        var expectedCommand = Path.GetFullPath(Path.Combine(pluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe"));
        var expectedCwd = Path.GetFullPath(pluginRoot);
        var command = TryReadTomlString(blockMatch.Value, "command");
        var cwd = TryReadTomlString(blockMatch.Value, "cwd");
        var enabled = TryReadTomlBool(blockMatch.Value, "enabled");
        var identityAligned =
            string.Equals(command, expectedCommand, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(cwd, expectedCwd, StringComparison.OrdinalIgnoreCase) &&
            enabled == true;

        return identityAligned
            ? new CodexCustomMcpInspection(true, true, true, [])
            : new CodexCustomMcpInspection(true, true, false, ["codex_mcp_server_entry_outdated"]);
    }

    private static bool IsAnarchyPluginEntry(JsonObject? pluginNode)
    {
        if (pluginNode is null)
        {
            return false;
        }

        var pluginName = pluginNode["name"]?.GetValue<string>() ?? string.Empty;
        if (pluginName.StartsWith("anarchy-ai", StringComparison.Ordinal))
        {
            return true;
        }

        var pluginPath = pluginNode["source"]?["path"]?.GetValue<string>() ?? string.Empty;
        return pluginPath.StartsWith("./plugins/anarchy-ai", StringComparison.Ordinal);
    }

    internal static string BuildPluginName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return "anarchy-ai";
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return "anarchy-ai-<repo-slug>-<stable-path-hash>";
        }

        var repoName = new DirectoryInfo(repoRoot).Name;
        var slug = NormalizeMarketplaceSlug(repoName);
        var repoPathHash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(repoRoot).ToLowerInvariant()));
        var suffix = Convert.ToHexString(repoPathHash.AsSpan(0, 4)).ToLowerInvariant();
        return $"anarchy-ai-{slug}-{suffix}";
    }

    internal static string BuildPluginRelativePath(InstallScope installScope, string? repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? $"./.codex/plugins/{BuildPluginName(installScope, repoRoot)}"
            : $"./plugins/{BuildPluginName(installScope, repoRoot)}";
    }

    private static string BuildMcpServerName()
    {
        return "anarchy-ai";
    }

    private static string BuildMarketplaceName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return "anarchy-user-profile";
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return "anarchy-local-<repo-slug>-<stable-path-hash>";
        }

        var repoName = new DirectoryInfo(repoRoot).Name;
        var slug = NormalizeMarketplaceSlug(repoName);
        var repoPathHash = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(repoRoot).ToLowerInvariant()));
        var suffix = Convert.ToHexString(repoPathHash.AsSpan(0, 4)).ToLowerInvariant();
        return $"anarchy-local-{slug}-{suffix}";
    }

    private static string BuildMarketplaceDisplayName(InstallScope installScope, string? repoRoot)
    {
        if (installScope == InstallScope.UserProfile)
        {
            return "Anarchy-AI User Profile";
        }

        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return "Anarchy-AI Local (Repo)";
        }

        var repoName = new DirectoryInfo(repoRoot).Name.Trim();
        if (string.IsNullOrWhiteSpace(repoName))
        {
            repoName = "Repo";
        }

        return $"Anarchy-AI Local ({repoName})";
    }

    internal static string ResolvePluginRoot(InstallScope installScope, string repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? Path.Combine(GetUserProfileDirectory(), ".codex", "plugins", BuildPluginName(installScope, repoRoot))
            : Path.Combine(repoRoot, "plugins", BuildPluginName(installScope, repoRoot));
    }

    private static string ResolveMarketplacePath(InstallScope installScope, string repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? Path.Combine(GetUserProfileDirectory(), ".agents", "plugins", "marketplace.json")
            : Path.Combine(repoRoot, ".agents", "plugins", "marketplace.json");
    }

    private static string GetUserProfileDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    internal static string BuildInstallRootLabel(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? Path.Combine(GetUserProfileDirectory(), ".codex")
            : "<selected repo>";
    }

    internal static string BuildPluginFolderLabel(InstallScope installScope, string? repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? Path.Combine("~", ".codex", "plugins", BuildPluginName(installScope, repoRoot))
            : Path.Combine("plugins", BuildPluginName(installScope, repoRoot));
    }

    internal static string BuildMarketplacePathLabel(InstallScope installScope, string? repoRoot)
    {
        return installScope == InstallScope.UserProfile
            ? Path.Combine("~", ".agents", "plugins", "marketplace.json")
            : Path.Combine(".agents", "plugins", "marketplace.json");
    }

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

    private static string BuildMarketplaceMissingFinding(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? "user_profile_marketplace_missing"
            : "repo_marketplace_missing";
    }

    private static string BuildMarketplaceMissingPluginsArrayFinding(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile
            ? "user_profile_marketplace_missing_plugins_array"
            : "repo_marketplace_missing_plugins_array";
    }

    private static LegacyUserProfileInspection InspectLegacyUserProfileSurfaces(InstallScope installScope, string normalizedHostContext, bool newPluginMarketplaceLaneReady)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return new LegacyUserProfileInspection(false, false, newPluginMarketplaceLaneReady, []);
        }

        var legacyPluginRoot = Path.Combine(GetUserProfileDirectory(), "plugins", BuildPluginName(InstallScope.UserProfile, null));
        var legacyPluginRootPresent = Directory.Exists(legacyPluginRoot);
        var legacyCodexCustomMcpEntryPresent = false;

        var codexConfigPath = Path.Combine(GetUserProfileDirectory(), ".codex", "config.toml");
        if (File.Exists(codexConfigPath))
        {
            var content = File.ReadAllText(codexConfigPath);
            var blockMatch = Regex.Match(content, CodexCustomMcpServerBlockPattern);
            if (blockMatch.Success)
            {
                var command = TryReadTomlString(blockMatch.Value, "command");
                var cwd = TryReadTomlString(blockMatch.Value, "cwd");
                var legacyRuntimePath = Path.GetFullPath(Path.Combine(legacyPluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe"));
                legacyCodexCustomMcpEntryPresent =
                    string.Equals(command, legacyRuntimePath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cwd, Path.GetFullPath(legacyPluginRoot), StringComparison.OrdinalIgnoreCase);
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

    private static string BuildInstallScopeLabel(InstallScope installScope)
    {
        return installScope == InstallScope.UserProfile ? "User-Profile" : "Repo-Local";
    }

    private static string BuildExpectedCodexMcpServerBlock(string pluginRoot, string newline)
    {
        var runtimePath = Path.GetFullPath(Path.Combine(pluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe"));
        var normalizedPluginRoot = Path.GetFullPath(pluginRoot);
        return string.Join(newline, new[]
        {
            "[mcp_servers.anarchy-ai]",
            $"command = '{ToTomlLiteral(runtimePath)}'",
            $"cwd = '{ToTomlLiteral(normalizedPluginRoot)}'",
            "enabled = true"
        });
    }

    private static bool UpsertTomlServerBlock(ref string content, string expectedBlock)
    {
        var existingMatch = Regex.Match(content, CodexCustomMcpServerBlockPattern);
        if (existingMatch.Success)
        {
            var existingNormalized = existingMatch.Value.TrimEnd('\r', '\n');
            var expectedNormalized = expectedBlock.TrimEnd('\r', '\n');
            if (string.Equals(existingNormalized, expectedNormalized, StringComparison.Ordinal))
            {
                return false;
            }

            var sectionRegex = new Regex(CodexCustomMcpServerBlockPattern, RegexOptions.Multiline);
            content = sectionRegex.Replace(content, expectedBlock + DetectNewline(content), 1);
            return true;
        }

        var newline = DetectNewline(content);
        if (!string.IsNullOrWhiteSpace(content) && !content.EndsWith(newline, StringComparison.Ordinal))
        {
            content += newline;
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            content += newline;
        }

        content += expectedBlock + newline;
        return true;
    }

    private static string ToTomlLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static void RemoveLegacyCodexCustomMcpEntry(InstallScope installScope, string normalizedHostContext, HashSet<string> actionsTaken)
    {
        if (installScope != InstallScope.UserProfile || !string.Equals(normalizedHostContext, "codex", StringComparison.Ordinal))
        {
            return;
        }

        var codexConfigPath = Path.Combine(GetUserProfileDirectory(), ".codex", "config.toml");
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

        var legacyPluginRoot = Path.Combine(GetUserProfileDirectory(), "plugins", BuildPluginName(InstallScope.UserProfile, null));
        var legacyRuntimePath = Path.GetFullPath(Path.Combine(legacyPluginRoot, "runtime", "win-x64", "AnarchyAi.Mcp.Server.exe"));
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

    private static string DetectNewline(string content)
    {
        return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

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

    private static bool? TryReadTomlBool(string block, string key)
    {
        var match = Regex.Match(block, $@"(?m)^\s*{Regex.Escape(key)}\s*=\s*(?<value>true|false)\s*$");
        if (!match.Success)
        {
            return null;
        }

        return string.Equals(match.Groups["value"].Value, "true", StringComparison.Ordinal);
    }

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
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                updateNotes.Add("portable_schema_family_refresh_skipped_without_workspace_root");
                actionsTaken.Add("portable_schema_family_not_targeted");
                return;
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
    bool MarketplaceIdentityAligned,
    bool IsValidJson,
    string[] Findings);

internal sealed record PluginManifestInspection(
    bool Exists,
    bool IsValidJson,
    bool IdentityAligned,
    string[] Findings);

internal sealed record McpConfigurationInspection(
    bool Exists,
    bool IdentityAligned,
    bool IsValidJson,
    string[] Findings);

internal sealed record CodexCustomMcpInspection(
    bool Required,
    bool EntryPresent,
    bool IdentityAligned,
    string[] Findings);

internal sealed record LegacyUserProfileInspection(
    bool LegacyPluginRootPresent,
    bool LegacyCodexCustomMcpEntryPresent,
    bool NewPluginMarketplaceLaneReady,
    string[] Findings)
{
    public bool HasLegacySurface => LegacyPluginRootPresent || LegacyCodexCustomMcpEntryPresent;
}

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
