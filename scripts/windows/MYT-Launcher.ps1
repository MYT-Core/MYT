Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$binDir = $scriptDir

if (-not (Test-Path (Join-Path $binDir "mytd.exe"))) {
    $candidate = Join-Path (Split-Path -Parent $scriptDir) "bin"
    if (Test-Path (Join-Path $candidate "mytd.exe")) {
        $binDir = $candidate
    }
}

function Start-CmdWindow {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$CommandLine
    )

    $cmd = "title $Title && $CommandLine"
    Start-Process -FilePath "cmd.exe" -ArgumentList @("/k", $cmd) -WorkingDirectory $binDir
}

function Q {
    param([string]$s)
    return '"' + $s + '"'
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "MYT Simple Launcher"
$form.Size = New-Object System.Drawing.Size(760, 470)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

$font = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Font = $font

$y = 20

$lblInfo = New-Object System.Windows.Forms.Label
$lblInfo.Location = New-Object System.Drawing.Point(20, $y)
$lblInfo.Size = New-Object System.Drawing.Size(710, 44)
$lblInfo.Text = "Wallet-Only: connect directly to VPS node. Mining requires your own local mytd node (restricted public nodes block start_mining)."
$form.Controls.Add($lblInfo)
$y += 52

function Add-LabelBox {
    param(
        [string]$label,
        [string]$value
    )
    $script:y += 2
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Location = New-Object System.Drawing.Point(20, $script:y)
    $lbl.Size = New-Object System.Drawing.Size(220, 22)
    $lbl.Text = $label
    $form.Controls.Add($lbl)

    $tb = New-Object System.Windows.Forms.TextBox
    $tb.Location = New-Object System.Drawing.Point(250, $script:y - 2)
    $tb.Size = New-Object System.Drawing.Size(480, 24)
    $tb.Text = $value
    $form.Controls.Add($tb)
    $script:y += 30
    return $tb
}

$tbNodeHost = Add-LabelBox "VPS Node Host/IP" "87.106.240.3"
$tbP2pPort = Add-LabelBox "VPS P2P Port" "38080"
$tbRpcPort = Add-LabelBox "VPS RPC Port" "38081"
$tbWalletPath = Add-LabelBox "Wallet Path" "$env:USERPROFILE\myt\walletA"
$tbDataDir = Add-LabelBox "Local Node Data Dir" "$env:ProgramData\myt\local-node"
$tbThreads = Add-LabelBox "Mining Threads (for wallet cmd)" "1"

$y += 8

$btnWalletRemote = New-Object System.Windows.Forms.Button
$btnWalletRemote.Location = New-Object System.Drawing.Point(20, $y)
$btnWalletRemote.Size = New-Object System.Drawing.Size(220, 34)
$btnWalletRemote.Text = "Open Wallet (Remote VPS)"
$btnWalletRemote.Add_Click({
    $wallet = $tbWalletPath.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($wallet)) {
        [System.Windows.Forms.MessageBox]::Show("Please set Wallet Path.")
        return
    }
    $daemon = "$($tbNodeHost.Text.Trim()):$($tbRpcPort.Text.Trim())"
    $exe = Join-Path $binDir "myt-wallet-cli.exe"
    $cmd = "$(Q $exe) --testnet --daemon-address $daemon --trusted-daemon --wallet-file $(Q $wallet)"
    Start-CmdWindow -Title "MYT Wallet (Remote VPS)" -CommandLine $cmd
})
$form.Controls.Add($btnWalletRemote)

$btnNodeLocal = New-Object System.Windows.Forms.Button
$btnNodeLocal.Location = New-Object System.Drawing.Point(260, $y)
$btnNodeLocal.Size = New-Object System.Drawing.Size(220, 34)
$btnNodeLocal.Text = "Start Local Node (for mining)"
$btnNodeLocal.Add_Click({
    $host = $tbNodeHost.Text.Trim()
    $p2p = $tbP2pPort.Text.Trim()
    $data = $tbDataDir.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($host) -or [string]::IsNullOrWhiteSpace($p2p)) {
        [System.Windows.Forms.MessageBox]::Show("Please set VPS host and P2P port.")
        return
    }
    if ([string]::IsNullOrWhiteSpace($data)) {
        [System.Windows.Forms.MessageBox]::Show("Please set Local Node Data Dir.")
        return
    }
    $exe = Join-Path $binDir "mytd.exe"
    $cmd = "$(Q $exe) --testnet --data-dir $(Q $data) --add-priority-node $host`:$p2p --p2p-bind-ip 127.0.0.1 --p2p-bind-port 38080 --rpc-bind-ip 127.0.0.1 --rpc-bind-port 38081 --disable-dns-checkpoints --check-updates disabled --no-igd --out-peers 16 --in-peers 32 --log-level 1"
    Start-CmdWindow -Title "MYT Local Node" -CommandLine $cmd
})
$form.Controls.Add($btnNodeLocal)

$btnWalletLocal = New-Object System.Windows.Forms.Button
$btnWalletLocal.Location = New-Object System.Drawing.Point(510, $y)
$btnWalletLocal.Size = New-Object System.Drawing.Size(220, 34)
$btnWalletLocal.Text = "Open Wallet (Local Node)"
$btnWalletLocal.Add_Click({
    $wallet = $tbWalletPath.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($wallet)) {
        [System.Windows.Forms.MessageBox]::Show("Please set Wallet Path.")
        return
    }
    $exe = Join-Path $binDir "myt-wallet-cli.exe"
    $cmd = "$(Q $exe) --testnet --daemon-address 127.0.0.1:38081 --wallet-file $(Q $wallet)"
    Start-CmdWindow -Title "MYT Wallet (Local Node)" -CommandLine $cmd
})
$form.Controls.Add($btnWalletLocal)

$y += 44

$btnWalletRpc = New-Object System.Windows.Forms.Button
$btnWalletRpc.Location = New-Object System.Drawing.Point(20, $y)
$btnWalletRpc.Size = New-Object System.Drawing.Size(220, 34)
$btnWalletRpc.Text = "Start wallet-rpc (Remote VPS)"
$btnWalletRpc.Add_Click({
    $wallet = $tbWalletPath.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($wallet)) {
        [System.Windows.Forms.MessageBox]::Show("Please set Wallet Path.")
        return
    }
    $daemon = "$($tbNodeHost.Text.Trim()):$($tbRpcPort.Text.Trim())"
    $exe = Join-Path $binDir "myt-wallet-rpc.exe"
    $cmd = "$(Q $exe) --testnet --daemon-address $daemon --wallet-file $(Q $wallet) --rpc-bind-port 38083"
    Start-CmdWindow -Title "MYT Wallet RPC" -CommandLine $cmd
})
$form.Controls.Add($btnWalletRpc)

$btnHelp = New-Object System.Windows.Forms.Button
$btnHelp.Location = New-Object System.Drawing.Point(260, $y)
$btnHelp.Size = New-Object System.Drawing.Size(470, 34)
$btnHelp.Text = "Show mining hint"
$btnHelp.Add_Click({
    $threads = $tbThreads.Text.Trim()
    if ([string]::IsNullOrWhiteSpace($threads)) { $threads = "1" }
    [System.Windows.Forms.MessageBox]::Show(
        "In wallet connected to LOCAL node run:`n`nstart_mining $threads`n`nIf wallet is connected to remote restricted VPS node, start_mining is blocked by design.",
        "Mining Hint",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    )
})
$form.Controls.Add($btnHelp)

$y += 54
$lblFooter = New-Object System.Windows.Forms.Label
$lblFooter.Location = New-Object System.Drawing.Point(20, $y)
$lblFooter.Size = New-Object System.Drawing.Size(710, 38)
$lblFooter.Text = "Tip: place this launcher in the same folder as mytd.exe, myt-wallet-cli.exe and myt-wallet-rpc.exe."
$form.Controls.Add($lblFooter)

[void]$form.ShowDialog()
