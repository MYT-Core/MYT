using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
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
        public string OnlineTlsFingerprint = "";
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
    private readonly TextBox onlineTlsFingerprint = new();
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
        ClientSize = new Size(760, 632);

        binDir = AppDomain.CurrentDomain.BaseDirectory;

        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        profiles = new Dictionary<NetworkMode, NetworkProfile>
        {
            [NetworkMode.Testnet] = new NetworkProfile
            {
                OnlineNodeIp = "87.106.240.3",
                OnlineRpcPort = "38089",
                ExplorerUrl = "https://testnet-explorer.myt-coin.com/",
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
                ExplorerUrl = "https://explorer.myt-coin.com/",
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
            Bounds = new Rectangle(GroupLeft, 70, GroupWidth, 192)
        };
        Controls.Add(walletBox);

        AddLabeledTextBox(walletBox, "Online Node Host", onlineNodeIp, 22, 16, 130);
        AddLabeledTextBox(walletBox, "Online RPC Port", onlineRpcPort, 52, 16, 130);
        AddLabeledTextBox(walletBox, "Wallet Path", walletPath, 82, 16, 130);
        AddLabeledTextBox(walletBox, "Explorer URL", explorerUrl, 112, 16, 130);
        AddLabeledTextBox(walletBox, "RPC TLS SHA-256", onlineTlsFingerprint, 142, 16, 130);
        onlineTlsFingerprint.PlaceholderText = "Fingerprint supplied by your node operator";

        var bWallet = MakeButton("Open Wallet (Online)", ActionButtonX, 22, ActionButtonWidth, (_, _) => OpenWalletRemote());
        var bExplorer = MakeButton("Open Explorer", ActionButtonX, 66, ActionButtonWidth, (_, _) => OpenExplorer());
        var bCheckOnline = MakeButton("Check Online Node", ActionButtonX, 110, ActionButtonWidth, (_, _) => CheckOnlineNode());
        walletBox.Controls.AddRange([bWallet, bExplorer, bCheckOnline]);

        var miningBox = new GroupBox
        {
            Text = "2) Mining Mode (local node + wallet)",
            Bounds = new Rectangle(GroupLeft, 270, GroupWidth, 140)
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
            Bounds = new Rectangle(GroupLeft, 418, GroupWidth, 150)
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

        logBox.Bounds = new Rectangle(16, 576, 728, 48);
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
        p.OnlineTlsFingerprint = onlineTlsFingerprint.Text.Trim();
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
        onlineTlsFingerprint.Text = p.OnlineTlsFingerprint;
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

    private static List<string> GetChainArgs(NetworkMode mode) => mode == NetworkMode.Testnet ? ["--testnet"] : [];

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

        var host = onlineNodeIp.Text.Trim().Trim('[', ']');
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown ||
            !TryParsePort(onlineRpcPort.Text.Trim(), out var port))
        {
            MessageBox.Show("Enter a valid RPC hostname or IP address and port.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fingerprint = onlineTlsFingerprint.Text.Trim().Replace(":", "");
        if (!Regex.IsMatch(fingerprint, "\\A[0-9a-fA-F]{64}\\z"))
        {
            MessageBox.Show("Enter the verified SHA-256 TLS certificate fingerprint from your node operator.",
                "RPC certificate required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!CanConnectTcp(host, port, 2500, out var error))
        {
            Log("Online node check failed: " + error);
            MessageBox.Show("Online node is not reachable. Check the VPS daemon and firewall.",
                "Online node unreachable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var args = GetChainArgs(activeMode);
        args.AddRange(["--daemon-address", FormatEndpoint(host, port), "--untrusted-daemon",
            "--daemon-ssl", "enabled", "--daemon-ssl-allowed-fingerprints", fingerprint]);
        AddWalletFileArgs(args);
        StartProgram("myt-wallet-cli.exe", args);
    }

    private bool OpenWalletLocal()
    {
        if (!EnsureWalletDirectory())
            return false;

        var args = GetChainArgs(activeMode);
        args.AddRange(["--daemon-address", $"127.0.0.1:{GetLocalRpcPort(activeMode)}", "--trusted-daemon"]);
        AddWalletFileArgs(args);
        return StartProgram("myt-wallet-cli.exe", args);
    }

    private bool StartLocalNode()
    {
        if (!TryPriorityNodes(miningPriorityNode.Text, GetDefaultSeedP2PPort(activeMode), out var peers))
            return false;

        var dataDir = localDataDir.Text.Trim();
        Directory.CreateDirectory(dataDir);
        var args = GetChainArgs(activeMode);
        args.AddRange(["--data-dir", dataDir, "--p2p-bind-ip", "127.0.0.1",
            "--p2p-bind-port", GetLocalP2PPort(activeMode).ToString(), "--rpc-bind-ip", "127.0.0.1",
            "--rpc-bind-port", GetLocalRpcPort(activeMode).ToString(),
            "--disable-dns-checkpoints", "--check-updates", "disabled", "--no-igd",
            "--out-peers", "16", "--in-peers", activeMode == NetworkMode.Mainnet ? "0" : "32",
            "--log-level", "1"]);
        if (activeMode == NetworkMode.Mainnet)
            args.Add("--hide-my-port");
        args.AddRange(peers);
        return StartProgram("mytd.exe", args);
    }

    private void StartMiningMode()
    {
        if (!TryGetMiningThreads(out var threads))
        {
            MessageBox.Show("Mining Threads must be a number between 1 and 256.", "Input error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!StartLocalNode() || !OpenWalletLocal())
            return;

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
        var args = GetChainArgs(activeMode);

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

        if (!TryPriorityNodes(publicPriorityNode.Text, GetDefaultSeedP2PPort(activeMode), out var peers))
            return;

        Directory.CreateDirectory(dd);
        args.AddRange(["--data-dir", dd, "--p2p-bind-ip", "0.0.0.0", "--p2p-bind-port", p2p,
            "--rpc-bind-ip", "0.0.0.0", "--rpc-bind-port", rpc, "--confirm-external-bind",
            "--public-node", "--restricted-rpc", "--non-interactive", "--disable-dns-checkpoints",
            "--check-updates", "disabled", "--no-igd", "--out-peers", "32", "--in-peers", "64",
            "--log-level", "1"]);
        args.AddRange(peers);
        StartProgram("mytd.exe", args);
    }

    private static string FormatEndpoint(string host, int port)
    {
        var bareHost = host.Trim('[', ']');
        return Uri.CheckHostName(bareHost) == UriHostNameType.IPv6
            ? $"[{bareHost}]:{port}" : $"{bareHost}:{port}";
    }

    private static bool TryPriorityNodes(string value, int defaultPort, out List<string> args)
    {
        args = [];
        var parts = value.Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (IPAddress.TryParse(part, out var ip))
            {
                args.AddRange(["--add-priority-node", FormatEndpoint(ip.ToString(), defaultPort)]);
                continue;
            }
            if (!Uri.TryCreate("tcp://" + part, UriKind.Absolute, out var endpoint) ||
                endpoint.HostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6) ||
                endpoint.UserInfo.Length > 0 || endpoint.AbsolutePath is not ("" or "/") ||
                endpoint.Query.Length > 0 || endpoint.Fragment.Length > 0 || endpoint.Port == 0)
            {
                MessageBox.Show("Enter priority nodes as hostnames or IP addresses with optional ports.",
                    "Invalid priority node", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            args.AddRange(["--add-priority-node", FormatEndpoint(endpoint.Host,
                endpoint.Port < 0 ? defaultPort : endpoint.Port)]);
        }
        return true;
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

    private bool StartProgram(string exeName, IEnumerable<string> args)
    {
        var exe = Path.Combine(binDir, exeName);
        if (!File.Exists(exe))
        {
            Log($"Cannot start. Missing: {exeName}");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = binDir,
            UseShellExecute = false
        };
        foreach (var argument in args)
            psi.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                return false;
            Log($"Started: {exeName}");
            return true;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            Log($"Could not start {exeName}: {exception.Message}");
            return false;
        }
    }

    private void Log(string msg)
    {
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }

    private void AddWalletFileArgs(List<string> args)
    {
        var wallet = walletPath.Text.Trim();
        var exists = File.Exists(wallet) && File.Exists(wallet + ".keys");
        args.AddRange([exists ? "--wallet-file" : "--generate-new-wallet", wallet]);
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
