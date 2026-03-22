# MYT Local Release Preparation

This guide prepares launch artifacts locally before any VPS is purchased.

## Local Data-Dir Layout (Linux, under /home/max)

Use explicit per-network data directories to avoid accidental cross-network reuse:

- Mainnet node data: `$HOME/.myt/mainnet`
- Testnet node data: `$HOME/.myt/testnet`
- Mainnet wallet files: `$HOME/.myt/wallets/mainnet/`
- Testnet wallet files: `$HOME/.myt/wallets/testnet/`

Recommended startup examples:

```bash
# Mainnet
./mytd --data-dir $HOME/.myt/mainnet

# Testnet
./mytd --testnet --data-dir $HOME/.myt/testnet
```

## 1) Build release artifacts

```bash
cd $HOME/projects/crypto/myt-base/myt
./scripts/release/build_release.sh v1.0.0-testnet.1
```

Output:
- `release/myt-v1.0.0-testnet.1-linux-x86_64/`
- `release/myt-v1.0.0-testnet.1-linux-x86_64.tar.gz`

## 2) Generate checksums

```bash
cd $HOME/projects/crypto/myt-base/myt
./scripts/release/make_checksums.sh ./release
```

Output:
- `release/SHA256SUMS.txt`

## 3) Sign checksums

With default GPG key:

```bash
cd $HOME/projects/crypto/myt-base/myt
./scripts/release/sign_release.sh ./release
```

With specific GPG key id:

```bash
cd $HOME/projects/crypto/myt-base/myt
./scripts/release/sign_release.sh ./release <YOUR_GPG_KEY_ID>
```

Output:
- `release/SHA256SUMS.txt.asc` (GPG)
- or `release/SHA256SUMS.txt.minisig` (if minisign is used)

## 4) Verify locally before publishing

```bash
cd $HOME/projects/crypto/myt-base/myt/release
sha256sum -c SHA256SUMS.txt
```

For GPG signature:

```bash
gpg --verify SHA256SUMS.txt.asc SHA256SUMS.txt
```

## 5) Publish package checklist

- Tarball: `myt-<version>-linux-x86_64.tar.gz`
- `SHA256SUMS.txt`
- Signature file (`.asc` or `.minisig`)
- Public key / fingerprint
- Release notes (what changed, known limitations)
