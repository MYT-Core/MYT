using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
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
    private readonly TextBox seedNodeHost = new() { Text = "87.106.240.3" };
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
            Text = "Simple MYT launcher for beginners. Step 1: Wallet/Explorer. Step 2: Mining Mode (local node). Step 3: Optional public node.",
            AutoSize = false,
            Bounds = new Rectangle(16, 12, 728, 36)
        };
        Controls.Add(info);

        var walletBox = new GroupBox
        {
            Text = "1) Wallet + Explorer (online node, no mining)",
            Bounds = new Rectangle(GroupLeft, 52, GroupWidth, 150)
        };
        Controls.Add(walletBox);

        AddLabeledTextBox(walletBox, "Online Node IP", vpsHost, 22, 16, 130);
        AddLabeledTextBox(walletBox, "Online RPC Port", vpsRpcPort, 52, 16, 130);
        AddLabeledTextBox(walletBox, "Wallet Path", walletPath, 82, 16, 130);
        AddLabeledTextBox(walletBox, "Explorer URL", explorerUrl, 112, 16, 130);
        var bWallet = MakeButton("Open Wallet (Online)", ActionButtonX, 22, ActionButtonWidth, (_, _) => OpenWalletRemote());
        var bExplorer = MakeButton("Open Explorer", ActionButtonX, 66, ActionButtonWidth, (_, _) => OpenExplorer());
        var bCheckOnline = MakeButton("Check Online Node", ActionButtonX, 110, ActionButtonWidth, (_, _) => CheckOnlineNode());
        walletBox.Controls.AddRange([bWallet, bExplorer, bCheckOnline]);

        var miningBox = new GroupBox
        {
            Text = "2) Mining Mode (local node + wallet)",
            Bounds = new Rectangle(GroupLeft, 210, GroupWidth, 130)
        };
        Controls.Add(miningBox);

        AddLabeledTextBox(miningBox, "Seed Node IP", seedNodeHost, 24, 16, 130);
        AddLabeledTextBox(miningBox, "Local Node Data", dataDir, 54, 16, 130);
        AddLabeledTextBox(miningBox, "Mining Threads", miningThreads, 84, 16, 130);
        var bMiningMode = MakeButton("Start Mining Mode", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartMiningMode());
        var bCopyMining = MakeButton("Copy Mining Command", ActionButtonX, 68, ActionButtonWidth, (_, _) => CopyMiningCommand());
        miningBox.Controls.AddRange([bMiningMode, bCopyMining]);

        var publicBox = new GroupBox
        {
            Text = "3) Public Node (optional, advanced)",
            Bounds = new Rectangle(GroupLeft, 348, GroupWidth, 130)
        };
        Controls.Add(publicBox);
        AddLabeledTextBox(publicBox, "Public Data Dir", publicDataDir, 24, 16, 130);
        AddLabeledTextBox(publicBox, "Public P2P Port", publicP2pPort, 54, 16, 130);
        AddLabeledTextBox(publicBox, "Public RPC Port", publicRpcPort, 84, 16, 130);
        var bPublic = MakeButton("Start Public Node", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartPublicNode());
        var bPublicHelp = MakeButton("Ports + Safety Help", ActionButtonX, 68, ActionButtonWidth, (_, _) => ShowPublicNodeHint());
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

        var host = vpsHost.Text.Trim();
        var port = vpsRpcPort.Text.Trim();
        if (!TryParsePort(port, out var portNum))
        {
            MessageBox.Show("Online RPC Port is invalid.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!CanConnectTcp(host, portNum, 2500, out var err))
        {
            Log("Online node check failed: " + err);
            MessageBox.Show(
                "Online node is not reachable.\n\n" +
                $"Tried: {host}:{port}\n" +
                "Check VPS daemon/firewall and try again.",
                "Online node unreachable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var daemon = $"{host}:{port}";
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            $"--testnet --daemon-address {daemon} --trusted-daemon --daemon-ssl enabled --daemon-ssl-allow-any-cert");
        StartCmd("MYT Wallet (Online Node)", "myt-wallet-cli.exe", args);
        Log("Online wallet opened. Mining is disabled in this mode.");
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
        var host = seedNodeHost.Text.Trim();
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
        if (!TryGetMiningThreads(out var threads))
        {
            MessageBox.Show("Mining Threads must be a number between 1 and 256.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        StartLocalNode();
        OpenWalletLocal();

        var cmd = $"start_mining {threads}";
        TryCopyToClipboard(cmd);
        MessageBox.Show(
            "Mining Mode started.\n\n" +
            "In the wallet window run this command:\n" +
            cmd + "\n\n" +
            "(Command was copied to clipboard.)",
            "Next step: start mining",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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

    private void CopyMiningCommand()
    {
        if (!TryGetMiningThreads(out var threads))
        {
            MessageBox.Show("Mining Threads must be a number between 1 and 256.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cmd = $"start_mining {threads}";
        TryCopyToClipboard(cmd);
        MessageBox.Show(
            "Mining uses your LOCAL node.\n\n" +
            "After opening wallet in mining mode, run:\n\n" +
            cmd,
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

    private void CheckOnlineNode()
    {
        var host = vpsHost.Text.Trim();
        var port = vpsRpcPort.Text.Trim();
        if (!TryParsePort(port, out var portNum))
        {
            MessageBox.Show("Online RPC Port is invalid.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (CanConnectTcp(host, portNum, 2500, out _))
        {
            Log($"Online node reachable: {host}:{port}");
            MessageBox.Show("Online node is reachable.", "Node check", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Log($"Online node NOT reachable: {host}:{port}");
        MessageBox.Show(
            $"Online node not reachable: {host}:{port}\n" +
            "Check VPS daemon/firewall.",
            "Node check",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
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

    private static bool TryParsePort(string text, out int port)
    {
        return int.TryParse(text, out port) && port > 0 && port <= 65535;
    }

    private bool TryGetMiningThreads(out int threads)
    {
        return int.TryParse(miningThreads.Text.Trim(), out threads) && threads >= 1 && threads <= 256;
    }

    private static bool CanConnectTcp(string host, int port, int timeoutMs, out string error)
    {
        try
        {
            using var client = new TcpClient();
            var ar = client.BeginConnect(host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
            {
                error = "timeout";
                return false;
            }

            client.EndConnect(ar);
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void TryCopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            Log("Copied to clipboard: " + text);
        }
        catch
        {
            Log("Clipboard copy failed.");
        }
    }
}
