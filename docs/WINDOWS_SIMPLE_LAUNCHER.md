# Windows Simple Launcher

This launcher provides a basic GUI for:

- opening `myt-wallet-cli` against a remote VPS node
- starting a local `mytd` (for local mining)
- opening `myt-wallet-cli` against local node
- starting `myt-wallet-rpc`

## Files

- `scripts/windows/MYT-Launcher.ps1`
- `scripts/windows/MYT-Launcher.bat`

## Quick use

1. Copy both launcher files into your Windows release `bin` folder (same folder as `mytd.exe`).
2. Double-click `MYT-Launcher.bat`.
3. Use:
   - `Open Wallet (Remote VPS)` for wallet-only users.
   - `Start Local Node (for mining)` + `Open Wallet (Local Node)` for users who want mining.

## Notes

- Mining through a restricted public VPS RPC is blocked by design.
- For mining, users need a local node or trusted admin RPC access.
