# MYT

MYT is a Monero-based Proof-of-Work privacy chain currently in testnet hardening.

## Current Status
- Custom chain parameters active
- Local/public-style testnet validated (multi-node)
- Release artifacts signed and ready

## Binaries
- `mytd`
- `myt-wallet-cli`
- `myt-wallet-rpc`

## Verify Releases
Each release includes:
- `myt-<version>-linux-x86_64.tar.gz`
- `SHA256SUMS.txt`
- `SHA256SUMS.txt.asc`
- `MYT_RELEASE_PUBKEY.asc`

Verification example:

```bash
sha256sum -c SHA256SUMS.txt
gpg --import MYT_RELEASE_PUBKEY.asc
gpg --verify SHA256SUMS.txt.asc SHA256SUMS.txt
```

## Attribution and License
MYT is a fork based on Monero code.

- Original upstream: Monero
- License: BSD-3-Clause (see `LICENSE`)
- MYT-specific modifications are included in this repository

## Repository Notes
This repository is focused on protocol and node software.
Public infrastructure (VPS nodes, explorer, faucet) is managed separately during rollout.
