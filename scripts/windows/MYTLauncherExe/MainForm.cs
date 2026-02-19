using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MYTLauncher;

public sealed class MainForm : Form
{
    private readonly TextBox vpsHost = new() { Text = "87.106.240.3" };
    private readonly TextBox p2pPort = new() { Text = "38080" };
    private readonly TextBox rpcPort = new() { Text = "38081" };
    private readonly TextBox walletPath = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "myt", "walletA") };
    private readonly TextBox dataDir = new() { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "myt", "local-node") };
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
        ClientSize = new Size(760, 500);

        binDir = AppDomain.CurrentDomain.BaseDirectory;
        BuildUi();
        ValidateBinaries();
    }

    private void BuildUi()
    {
        var info = new Label
        {
            Text = "Simple launcher for MYT testnet. Wallet-only uses VPS. Mining needs local node.",
            AutoSize = false,
            Bounds = new Rectangle(16, 12, 728, 36)
        };
        Controls.Add(info);

        var cfg = new GroupBox
        {
            Text = "Connection",
            Bounds = new Rectangle(16, 52, 728, 150)
        };
        Controls.Add(cfg);

        AddLabeledTextBox(cfg, "VPS Host/IP", vpsHost, 18);
        AddLabeledTextBox(cfg, "VPS P2P Port", p2pPort, 48);
        AddLabeledTextBox(cfg, "VPS RPC Port", rpcPort, 78);
        AddLabeledTextBox(cfg, "Wallet Path", walletPath, 108);

        var ops = new GroupBox
        {
            Text = "Actions",
            Bounds = new Rectangle(16, 210, 728, 130)
        };
        Controls.Add(ops);

        var b1 = MakeButton("Open Wallet (Remote VPS)", 16, 26, 220, (_, _) => OpenWalletRemote());
        var b2 = MakeButton("Start Local Node", 252, 26, 220, (_, _) => StartLocalNode());
        var b3 = MakeButton("Open Wallet (Local Node)", 488, 26, 220, (_, _) => OpenWalletLocal());
        var b4 = MakeButton("Start Wallet RPC (Remote)", 16, 70, 220, (_, _) => StartWalletRpcRemote());
        var b5 = MakeButton("Mining Hint", 252, 70, 220, (_, _) => ShowMiningHint());
        ops.Controls.AddRange([b1, b2, b3, b4, b5]);

        var local = new GroupBox
        {
            Text = "Local Node / Mining",
            Bounds = new Rectangle(16, 346, 728, 68)
        };
        Controls.Add(local);
        AddLabeledTextBox(local, "Local Data Dir", dataDir, 26);
        AddLabeledTextBox(local, "Threads", miningThreads, 26, 520, 70);

        logBox.Bounds = new Rectangle(16, 420, 728, 68);
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
        box.Bounds = new Rectangle(x + width + 8, y, 560 - (x - 16), 24);
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
        foreach (var exe in new[] { "mytd.exe", "myt-wallet-cli.exe", "myt-wallet-rpc.exe" })
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
        var daemon = $"{vpsHost.Text.Trim()}:{rpcPort.Text.Trim()}";
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            $"--testnet --daemon-address {daemon} --trusted-daemon");
        StartCmd("MYT Wallet (Remote VPS)", "myt-wallet-cli.exe", args);
    }

    private void OpenWalletLocal()
    {
        if (!EnsureWalletDirectory())
            return;
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            "--testnet --daemon-address 127.0.0.1:38081");
        StartCmd("MYT Wallet (Local Node)", "myt-wallet-cli.exe", args);
    }

    private void StartWalletRpcRemote()
    {
        if (!EnsureWalletDirectory())
            return;
        var daemon = $"{vpsHost.Text.Trim()}:{rpcPort.Text.Trim()}";
        var args = $"--testnet --daemon-address {daemon} --wallet-file \"{walletPath.Text.Trim()}\" --rpc-bind-port 38083";
        StartCmd("MYT Wallet RPC", "myt-wallet-rpc.exe", args);
    }

    private void StartLocalNode()
    {
        var host = vpsHost.Text.Trim();
        var p2p = p2pPort.Text.Trim();
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

    private void ShowMiningHint()
    {
        MessageBox.Show(
            "Mining via remote restricted VPS node is blocked by design.\n\n" +
            "Use local node first, then in wallet run:\n\n" +
            $"start_mining {miningThreads.Text.Trim()}",
            "Mining hint",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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
