using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MYTLauncher;

public sealed class MainForm : Form
{
    private const int GroupLeft = 16;
    private const int GroupWidth = 728;
    private const int ActionButtonX = 520;
    private const int ActionButtonWidth = 190;
    private const int InputGapToButtons = 12;

    private readonly TextBox vpsHost = new() { Text = "87.106.240.3" };
    private readonly TextBox vpsRpcPort = new() { Text = "38081" };
    private readonly TextBox explorerUrl = new() { Text = "http://87.106.240.3:8081" };
    private readonly TextBox walletPath = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "myt", "walletA") };
    private readonly TextBox dataDir = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "myt", "local-node") };
    private readonly TextBox publicDataDir = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "myt", "public-node") };
    private readonly TextBox publicP2pPort = new() { Text = "38080" };
    private readonly TextBox publicRpcPort = new() { Text = "38081" };
    private readonly TextBox miningThreads = new() { Text = "1" };
    private readonly TextBox logBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

    private readonly string binDir;

    public MainForm()
    {
        Text = "MYT Launcher";
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 560);

        binDir = AppDomain.CurrentDomain.BaseDirectory;
        BuildUi();
        ValidateBinaries();
    }

    private void BuildUi()
    {
        var info = new Label
        {
            Text = "Simple MYT launcher: 1) Wallet (online node)  2) Mining mode (local node + wallet)  3) Public node.",
            AutoSize = false,
            Bounds = new Rectangle(16, 12, 728, 36)
        };
        Controls.Add(info);

        var walletBox = new GroupBox
        {
            Text = "1) Wallet erstellen / öffnen (Online Node)",
            Bounds = new Rectangle(GroupLeft, 52, GroupWidth, 150)
        };
        Controls.Add(walletBox);

        AddLabeledTextBox(walletBox, "Online Node IP", vpsHost, 22, 16, 130);
        AddLabeledTextBox(walletBox, "Online RPC Port", vpsRpcPort, 52, 16, 130);
        AddLabeledTextBox(walletBox, "Wallet Path", walletPath, 82, 16, 130);
        AddLabeledTextBox(walletBox, "Explorer URL", explorerUrl, 112, 16, 130);
        var bWallet = MakeButton("Open Wallet (Online Node)", ActionButtonX, 22, ActionButtonWidth, (_, _) => OpenWalletRemote());
        var bExplorer = MakeButton("Open Explorer", ActionButtonX, 66, ActionButtonWidth, (_, _) => OpenExplorer());
        walletBox.Controls.AddRange([bWallet, bExplorer]);

        var miningBox = new GroupBox
        {
            Text = "2) Mining starten (lokaler Node + Wallet)",
            Bounds = new Rectangle(GroupLeft, 210, GroupWidth, 120)
        };
        Controls.Add(miningBox);

        AddLabeledTextBox(miningBox, "Seed Node IP", vpsHost, 24, 16, 130);
        AddLabeledTextBox(miningBox, "Local Node Data", dataDir, 54, 16, 130);
        AddLabeledTextBox(miningBox, "Mining Threads", miningThreads, 84, 16, 130);
        var bMiningMode = MakeButton("Start Mining Mode", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartMiningMode());
        var bMiningHint = MakeButton("Mining Help", ActionButtonX, 68, ActionButtonWidth, (_, _) => ShowMiningHint());
        miningBox.Controls.AddRange([bMiningMode, bMiningHint]);

        var publicBox = new GroupBox
        {
            Text = "3) Run Public Node (advanced)",
            Bounds = new Rectangle(GroupLeft, 338, GroupWidth, 140)
        };
        Controls.Add(publicBox);
        AddLabeledTextBox(publicBox, "Public Data Dir", publicDataDir, 24, 16, 130);
        AddLabeledTextBox(publicBox, "Public P2P Port", publicP2pPort, 54, 16, 130);
        AddLabeledTextBox(publicBox, "Public RPC Port", publicRpcPort, 84, 16, 130);
        var bPublic = MakeButton("Start Public Node", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartPublicNode());
        var bPublicHelp = MakeButton("Ports Help", ActionButtonX, 68, ActionButtonWidth, (_, _) => ShowPublicNodeHint());
        publicBox.Controls.AddRange([bPublic, bPublicHelp]);

        logBox.Bounds = new Rectangle(16, 486, 728, 64);
        Controls.Add(logBox);
        Log("Launcher ready.");
    }

    private void AddLabeledTextBox(Control parent, string label, TextBox box, int y, int x = 16, int width = 120)
    {
        var l = new Label
        {
            Text = label,
            Bounds = new Rectangle(x, y + 3, width, 20)
        };
        var textX = x + width + 8;
        var textWidth = ActionButtonX - InputGapToButtons - textX;
        if (textWidth < 120)
            textWidth = 120;
        box.Bounds = new Rectangle(textX, y, textWidth, 24);
        parent.Controls.Add(l);
        parent.Controls.Add(box);
    }

    private Button MakeButton(string text, int x, int y, int w, EventHandler onClick)
    {
        var b = new Button { Text = text, Bounds = new Rectangle(x, y, w, 34) };
        b.Click += onClick;
        return b;
    }

    private void ValidateBinaries()
    {
        var missing = "";
        foreach (var exe in new[] { "mytd.exe", "myt-wallet-cli.exe" })
        {
            if (!File.Exists(Path.Combine(binDir, exe)))
                missing += $"{exe}\n";
        }
        if (!string.IsNullOrEmpty(missing))
        {
            Log("Missing files:\n" + missing.TrimEnd());
            MessageBox.Show("Put MYTLauncher.exe in the same folder as mytd.exe, myt-wallet-cli.exe and myt-wallet-rpc.exe.", "Missing binaries", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenWalletRemote()
    {
        if (!EnsureWalletDirectory())
            return;
        var daemon = $"{vpsHost.Text.Trim()}:{vpsRpcPort.Text.Trim()}";
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            $"--testnet --daemon-address {daemon} --trusted-daemon --daemon-ssl enabled --daemon-ssl-allow-any-cert");
        StartCmd("MYT Wallet (Online Node)", "myt-wallet-cli.exe", args);
    }

    private void OpenWalletLocal()
    {
        if (!EnsureWalletDirectory())
            return;
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            "--testnet --daemon-address 127.0.0.1:38081 --trusted-daemon");
        StartCmd("MYT Wallet (Local Node)", "myt-wallet-cli.exe", args);
    }

    private void StartLocalNode()
    {
        var host = vpsHost.Text.Trim();
        var p2p = "38080";
        var dd = dataDir.Text.Trim();
        Directory.CreateDirectory(dd);
        var args =
            $"--testnet --data-dir \"{dd}\" " +
            $"--add-priority-node {host}:{p2p} " +
            "--p2p-bind-ip 127.0.0.1 --p2p-bind-port 38080 " +
            "--rpc-bind-ip 127.0.0.1 --rpc-bind-port 38081 " +
            "--disable-dns-checkpoints --check-updates disabled " +
            "--no-igd --out-peers 16 --in-peers 32 --log-level 1";
        StartCmd("MYT Local Node", "mytd.exe", args);
    }

    private void StartMiningMode()
    {
        StartLocalNode();
        OpenWalletLocal();
    }

    private void StartPublicNode()
    {
        var dd = publicDataDir.Text.Trim();
        var p2p = publicP2pPort.Text.Trim();
        var rpc = publicRpcPort.Text.Trim();
        if (string.IsNullOrWhiteSpace(dd) || string.IsNullOrWhiteSpace(p2p) || string.IsNullOrWhiteSpace(rpc))
        {
            MessageBox.Show("Set Public Data Dir, Public P2P Port and Public RPC Port.");
            return;
        }
        Directory.CreateDirectory(dd);

        var args =
            $"--testnet --data-dir \"{dd}\" " +
            $"--p2p-bind-ip 0.0.0.0 --p2p-bind-port {p2p} " +
            $"--rpc-bind-ip 0.0.0.0 --rpc-bind-port {rpc} --confirm-external-bind " +
            "--public-node --restricted-rpc --non-interactive " +
            "--disable-dns-checkpoints --check-updates disabled " +
            "--no-igd --out-peers 32 --in-peers 64 --log-level 1";
        StartCmd("MYT Public Node", "mytd.exe", args);
    }

    private void ShowMiningHint()
    {
        MessageBox.Show(
            "Mining uses your LOCAL node.\n\n" +
            "Click 'Start Mining Mode', then in wallet run:\n\n" +
            $"start_mining {miningThreads.Text.Trim()}",
            "Mining hint",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowPublicNodeHint()
    {
        MessageBox.Show(
            "Public Node means your IP is visible to peers.\n\n" +
            "Open these firewall/router ports:\n" +
            $"- TCP {publicP2pPort.Text.Trim()} (P2P)\n" +
            $"- TCP {publicRpcPort.Text.Trim()} (Restricted RPC)\n\n" +
            "Use restricted RPC for safety.",
            "Public Node ports",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void OpenExplorer()
    {
        var url = explorerUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Set Explorer URL first.");
            return;
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "http://" + url;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Log("Opened Explorer: " + url);
        }
        catch (Exception ex)
        {
            Log("Failed to open Explorer URL: " + ex.Message);
            MessageBox.Show("Failed to open Explorer URL.");
        }
    }

    private void StartCmd(string title, string exeName, string args)
    {
        var exe = Path.Combine(binDir, exeName);
        if (!File.Exists(exe))
        {
            Log($"Cannot start. Missing: {exeName}");
            return;
        }

        // Use pushd/popd so UNC paths (e.g. \\wsl.localhost\...) are mapped to a temp drive in cmd.exe.
        var cmdLine = $"pushd \"{binDir}\" && title {title} && \"{exe}\" {args} && popd";
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k " + cmdLine,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            UseShellExecute = false
        };

        Process.Start(psi);
        Log($"Started: {exeName} {args}");
    }

    private void Log(string msg)
    {
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }

    private static string BuildWalletOpenOrCreateArgs(string walletFilePath, string commonArgsPrefix)
    {
        var wallet = walletFilePath.Trim();
        var keys = wallet + ".keys";
        var exists = File.Exists(wallet) && File.Exists(keys);
        var mode = exists ? "--wallet-file" : "--generate-new-wallet";
        return $"{commonArgsPrefix} {mode} \"{wallet}\"";
    }

    private bool EnsureWalletDirectory()
    {
        try
        {
            var wallet = walletPath.Text.Trim();
            var dir = Path.GetDirectoryName(wallet);
            if (string.IsNullOrWhiteSpace(dir))
                return true;
            Directory.CreateDirectory(dir);
            return true;
        }
        catch (Exception ex)
        {
            Log("Failed to create wallet directory: " + ex.Message);
            MessageBox.Show(
                "Cannot create wallet directory. Please choose another Wallet Path.",
                "Wallet path error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }
}
