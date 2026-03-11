using System;
using System.Collections.Generic;
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

    private enum NetworkMode
    {
        Testnet,
        Mainnet
    }

    private sealed class NetworkProfile
    {
        public string OnlineNodeIp = "";
        public string OnlineRpcPort = "";
        public string ExplorerUrl = "";
        public string WalletPath = "";
        public string MiningPriorityNode = "";
        public string LocalDataDir = "";
        public string MiningThreads = "1";
        public string PublicDataDir = "";
        public string PublicPriorityNode = "";
        public string PublicP2pPort = "";
        public string PublicRpcPort = "";
    }

    private readonly TabControl networkTabs = new();

    private readonly TextBox onlineNodeIp = new();
    private readonly TextBox onlineRpcPort = new();
    private readonly TextBox explorerUrl = new();
    private readonly TextBox walletPath = new();

    private readonly TextBox miningPriorityNode = new();
    private readonly TextBox localDataDir = new();
    private readonly TextBox miningThreads = new();

    private readonly TextBox publicDataDir = new();
    private readonly TextBox publicPriorityNode = new();
    private readonly TextBox publicP2pPort = new();
    private readonly TextBox publicRpcPort = new();

    private readonly TextBox logBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };

    private readonly string binDir;
    private readonly Dictionary<NetworkMode, NetworkProfile> profiles;
    private NetworkMode activeMode = NetworkMode.Testnet;

    public MainForm()
    {
        Text = "MYT Launcher";
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 600);

        binDir = AppDomain.CurrentDomain.BaseDirectory;

        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        profiles = new Dictionary<NetworkMode, NetworkProfile>
        {
            [NetworkMode.Testnet] = new NetworkProfile
            {
                OnlineNodeIp = "87.106.240.3",
                OnlineRpcPort = "38089",
                ExplorerUrl = "http://87.106.240.3:8081",
                WalletPath = Path.Combine(user, "myt", "wallet-testnet"),
                MiningPriorityNode = "87.106.240.3",
                LocalDataDir = Path.Combine(programData, "myt", "local-node-testnet"),
                MiningThreads = "1",
                PublicDataDir = Path.Combine(programData, "myt", "public-node-testnet"),
                PublicPriorityNode = "87.106.240.3",
                PublicP2pPort = "38080",
                PublicRpcPort = "38089"
            },
            [NetworkMode.Mainnet] = new NetworkProfile
            {
                OnlineNodeIp = "87.106.240.3",
                OnlineRpcPort = "39089",
                ExplorerUrl = "http://87.106.240.3:9081",
                WalletPath = Path.Combine(user, "myt", "wallet-mainnet"),
                MiningPriorityNode = "87.106.240.3,67.217.244.109",
                LocalDataDir = Path.Combine(programData, "myt", "local-node-mainnet"),
                MiningThreads = "1",
                PublicDataDir = Path.Combine(programData, "myt", "public-node-mainnet"),
                PublicPriorityNode = "87.106.240.3,67.217.244.109",
                PublicP2pPort = "39080",
                PublicRpcPort = "39089"
            }
        };

        BuildUi();
        LoadProfile(activeMode);
        ValidateBinaries();
    }

    private void BuildUi()
    {
        var info = new Label
        {
            Text = "Simple launcher: 1) Wallet/Explorer (online node, no mining)  2) Mining Mode (local node)  3) Optional public node.",
            AutoSize = false,
            Bounds = new Rectangle(16, 10, 728, 22)
        };
        Controls.Add(info);

        networkTabs.Bounds = new Rectangle(16, 34, 300, 30);
        networkTabs.TabPages.Add("Testnet");
        networkTabs.TabPages.Add("Mainnet");
        networkTabs.SelectedIndexChanged += (_, _) => OnNetworkChanged();
        Controls.Add(networkTabs);

        var walletBox = new GroupBox
        {
            Text = "1) Wallet + Explorer (online node, no mining)",
            Bounds = new Rectangle(GroupLeft, 70, GroupWidth, 160)
        };
        Controls.Add(walletBox);

        AddLabeledTextBox(walletBox, "Online Node IP", onlineNodeIp, 22, 16, 130);
        AddLabeledTextBox(walletBox, "Online RPC Port", onlineRpcPort, 52, 16, 130);
        AddLabeledTextBox(walletBox, "Wallet Path", walletPath, 82, 16, 130);
        AddLabeledTextBox(walletBox, "Explorer URL", explorerUrl, 112, 16, 130);

        var bWallet = MakeButton("Open Wallet (Online)", ActionButtonX, 22, ActionButtonWidth, (_, _) => OpenWalletRemote());
        var bExplorer = MakeButton("Open Explorer", ActionButtonX, 66, ActionButtonWidth, (_, _) => OpenExplorer());
        var bCheckOnline = MakeButton("Check Online Node", ActionButtonX, 110, ActionButtonWidth, (_, _) => CheckOnlineNode());
        walletBox.Controls.AddRange([bWallet, bExplorer, bCheckOnline]);

        var miningBox = new GroupBox
        {
            Text = "2) Mining Mode (local node + wallet)",
            Bounds = new Rectangle(GroupLeft, 238, GroupWidth, 140)
        };
        Controls.Add(miningBox);

        AddLabeledTextBox(miningBox, "Add Priority Node", miningPriorityNode, 24, 16, 130);
        AddLabeledTextBox(miningBox, "Local Node Data", localDataDir, 54, 16, 130);
        AddLabeledTextBox(miningBox, "Mining Threads", miningThreads, 84, 16, 130);
        miningPriorityNode.PlaceholderText = "optional, e.g. 87.106.240.3,67.217.244.109";

        var bMiningMode = MakeButton("Start Mining Mode", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartMiningMode());
        var bCopyMining = MakeButton("Copy Mining Command", ActionButtonX, 68, ActionButtonWidth, (_, _) => CopyMiningCommand());
        miningBox.Controls.AddRange([bMiningMode, bCopyMining]);

        var publicBox = new GroupBox
        {
            Text = "3) Public Node (optional, advanced)",
            Bounds = new Rectangle(GroupLeft, 386, GroupWidth, 150)
        };
        Controls.Add(publicBox);

        AddLabeledTextBox(publicBox, "Public Data Dir", publicDataDir, 24, 16, 130);
        AddLabeledTextBox(publicBox, "Add Priority Node", publicPriorityNode, 54, 16, 130);
        AddLabeledTextBox(publicBox, "Public P2P Port", publicP2pPort, 84, 16, 130);
        AddLabeledTextBox(publicBox, "Public RPC Port", publicRpcPort, 114, 16, 130);
        publicPriorityNode.PlaceholderText = "optional, e.g. 87.106.240.3,67.217.244.109";

        var bPublic = MakeButton("Start Public Node", ActionButtonX, 24, ActionButtonWidth, (_, _) => StartPublicNode());
        var bPublicHelp = MakeButton("Ports + Safety Help", ActionButtonX, 68, ActionButtonWidth, (_, _) => ShowPublicNodeHint());
        publicBox.Controls.AddRange([bPublic, bPublicHelp]);

        logBox.Bounds = new Rectangle(16, 544, 728, 48);
        Controls.Add(logBox);
        Log("Launcher ready.");
    }

    private void OnNetworkChanged()
    {
        SaveProfile(activeMode);
        activeMode = networkTabs.SelectedIndex == 0 ? NetworkMode.Testnet : NetworkMode.Mainnet;
        LoadProfile(activeMode);
        Log($"Switched to {GetModeName(activeMode)} profile.");
    }

    private void SaveProfile(NetworkMode mode)
    {
        var p = profiles[mode];
        p.OnlineNodeIp = onlineNodeIp.Text.Trim();
        p.OnlineRpcPort = onlineRpcPort.Text.Trim();
        p.ExplorerUrl = explorerUrl.Text.Trim();
        p.WalletPath = walletPath.Text.Trim();
        p.MiningPriorityNode = miningPriorityNode.Text.Trim();
        p.LocalDataDir = localDataDir.Text.Trim();
        p.MiningThreads = miningThreads.Text.Trim();
        p.PublicDataDir = publicDataDir.Text.Trim();
        p.PublicPriorityNode = publicPriorityNode.Text.Trim();
        p.PublicP2pPort = publicP2pPort.Text.Trim();
        p.PublicRpcPort = publicRpcPort.Text.Trim();
    }

    private void LoadProfile(NetworkMode mode)
    {
        var p = profiles[mode];
        onlineNodeIp.Text = p.OnlineNodeIp;
        onlineRpcPort.Text = p.OnlineRpcPort;
        explorerUrl.Text = p.ExplorerUrl;
        walletPath.Text = p.WalletPath;
        miningPriorityNode.Text = p.MiningPriorityNode;
        localDataDir.Text = p.LocalDataDir;
        miningThreads.Text = p.MiningThreads;
        publicDataDir.Text = p.PublicDataDir;
        publicPriorityNode.Text = p.PublicPriorityNode;
        publicP2pPort.Text = p.PublicP2pPort;
        publicRpcPort.Text = p.PublicRpcPort;
    }

    private static string GetModeName(NetworkMode mode) => mode == NetworkMode.Testnet ? "testnet" : "mainnet";

    private static string GetChainArg(NetworkMode mode) => mode == NetworkMode.Testnet ? "--testnet " : "";

    private static int GetDefaultSeedP2PPort(NetworkMode mode) => mode == NetworkMode.Testnet ? 38080 : 39080;

    private static int GetLocalP2PPort(NetworkMode mode) => mode == NetworkMode.Testnet ? 38080 : 39080;

    private static int GetLocalRpcPort(NetworkMode mode) => mode == NetworkMode.Testnet ? 38081 : 39081;

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
            MessageBox.Show(
                "Put MYTLauncher.exe in the same folder as mytd.exe, myt-wallet-cli.exe and myt-wallet-rpc.exe.",
                "Missing binaries",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OpenWalletRemote()
    {
        if (!EnsureWalletDirectory())
            return;

        var host = onlineNodeIp.Text.Trim();
        var port = onlineRpcPort.Text.Trim();
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

        var chainArg = GetChainArg(activeMode);
        var daemon = $"{host}:{port}";
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            $"{chainArg}--daemon-address {daemon} --trusted-daemon --daemon-ssl enabled --daemon-ssl-allow-any-cert");

        StartCmd($"MYT Wallet ({GetModeName(activeMode)})", "myt-wallet-cli.exe", args);
        Log("Online wallet opened. Mining is disabled in this mode.");
    }

    private void OpenWalletLocal()
    {
        if (!EnsureWalletDirectory())
            return;

        var chainArg = GetChainArg(activeMode);
        var localRpc = GetLocalRpcPort(activeMode);
        var args = BuildWalletOpenOrCreateArgs(
            walletPath.Text.Trim(),
            $"{chainArg}--daemon-address 127.0.0.1:{localRpc} --trusted-daemon");

        StartCmd($"MYT Wallet (Local {GetModeName(activeMode)})", "myt-wallet-cli.exe", args);
    }

    private void StartLocalNode()
    {
        var chainArg = GetChainArg(activeMode);
        var dd = localDataDir.Text.Trim();
        Directory.CreateDirectory(dd);

        var localP2P = GetLocalP2PPort(activeMode);
        var localRpc = GetLocalRpcPort(activeMode);
        var addPriority = BuildPriorityNodesArg(miningPriorityNode.Text.Trim(), GetDefaultSeedP2PPort(activeMode));

        var peerArgs = activeMode == NetworkMode.Mainnet
            ? "--no-igd --out-peers 16 --in-peers 0 --hide-my-port --log-level 1"
            : "--no-igd --out-peers 16 --in-peers 32 --log-level 1";

        var args =
            $"{chainArg}--data-dir \"{dd}\" " +
            addPriority +
            $"--p2p-bind-ip 127.0.0.1 --p2p-bind-port {localP2P} " +
            $"--rpc-bind-ip 127.0.0.1 --rpc-bind-port {localRpc} " +
            "--disable-dns-checkpoints --check-updates disabled " +
            peerArgs;

        StartCmd($"MYT Local Node ({GetModeName(activeMode)})", "mytd.exe", args);
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
            "In the wallet window run:\n" +
            cmd + "\n\n" +
            "(Command copied to clipboard.)",
            "Next step: start mining",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void StartPublicNode()
    {
        var chainArg = GetChainArg(activeMode);

        var dd = publicDataDir.Text.Trim();
        var p2p = publicP2pPort.Text.Trim();
        var rpc = publicRpcPort.Text.Trim();

        if (string.IsNullOrWhiteSpace(dd) || string.IsNullOrWhiteSpace(p2p) || string.IsNullOrWhiteSpace(rpc))
        {
            MessageBox.Show("Set Public Data Dir, Public P2P Port and Public RPC Port.");
            return;
        }

        if (!TryParsePort(p2p, out _) || !TryParsePort(rpc, out _))
        {
            MessageBox.Show("Public node ports are invalid.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(dd);

        var addPriority = BuildPriorityNodesArg(publicPriorityNode.Text.Trim(), GetDefaultSeedP2PPort(activeMode));

        var args =
            $"{chainArg}--data-dir \"{dd}\" " +
            addPriority +
            $"--p2p-bind-ip 0.0.0.0 --p2p-bind-port {p2p} " +
            $"--rpc-bind-ip 0.0.0.0 --rpc-bind-port {rpc} --confirm-external-bind " +
            "--public-node --restricted-rpc --non-interactive " +
            "--disable-dns-checkpoints --check-updates disabled " +
            "--no-igd --out-peers 32 --in-peers 64 --log-level 1";

        StartCmd($"MYT Public Node ({GetModeName(activeMode)})", "mytd.exe", args);
    }

    private static string BuildPriorityNodesArg(string value, int defaultPort)
    {
        var raw = value.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var parts = raw.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "";

        var args = "";
        foreach (var part in parts)
        {
            var v = part.Trim();
            if (string.IsNullOrWhiteSpace(v))
                continue;

            if (!v.Contains(':'))
                v = $"{v}:{defaultPort}";

            args += $"--add-priority-node {v} ";
        }

        return args;
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
        var host = onlineNodeIp.Text.Trim();
        var port = onlineRpcPort.Text.Trim();
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
}
