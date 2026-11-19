# MYTLauncher.exe (Simple Win GUI)

Classic, simple Windows Forms launcher for MYT binaries:

- `mytd.exe`
- `myt-wallet-cli.exe`
- `myt-wallet-rpc.exe`

## Build on Windows

1. Install .NET 8 SDK.
2. Run:

```bat
build_launcher.bat
```

Output:

- `publish\MYTLauncher.exe`

## Use

Put `MYTLauncher.exe` in the same folder as:

- `mytd.exe`
- `myt-wallet-cli.exe`
- `myt-wallet-rpc.exe`

Then start `MYTLauncher.exe`.

## Note

`start_mining` against a remote restricted public node is blocked by design.
Mining should be done against a local node.


## Online wallet TLS

Enter the node hostname, RPC port and SHA-256 TLS certificate fingerprint.
Obtain the fingerprint from your node operator over an authenticated channel.
On the node, read it directly from the configured certificate with
`openssl x509 -in daemon-cert.pem -noout -fingerprint -sha256`. The launcher
requires TLS and opens the remote daemon in untrusted mode. Use the local
wallet and node mode when you do not have a verified remote fingerprint.
