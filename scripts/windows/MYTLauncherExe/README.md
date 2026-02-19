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

