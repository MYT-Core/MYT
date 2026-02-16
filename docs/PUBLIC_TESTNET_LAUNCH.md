# MYT Public Testnet Launch Runbook

This runbook is for launch day after binaries are published.

## Prerequisites

- Frozen parameters documented in `docs/TESTNET_PARAMETER_FREEZE.md`
- Signed release artifacts prepared (`docs/RELEASE_LOCAL_PREP.md`)
- At least 3 servers planned:
  - Seed Node A
  - Full Node B
  - Full/Mining Node C
- DNS names prepared (optional but recommended)

## Variables

Set these values before launch:

- `<SEED_A_IP>`
- `<NODE_B_IP>`
- `<NODE_C_IP>`
- `<PUBLIC_RPC_HOST>`
- `<EXPLORER_HOST>`

## Step 1: Start Seed Node A

```bash
BIN=/opt/myt/bin
DATA=/var/lib/myt/seed-a

$BIN/mytd \
  --testnet \
  --data-dir "$DATA" \
  --p2p-bind-ip 0.0.0.0 --p2p-bind-port 38080 \
  --rpc-bind-ip 127.0.0.1 --rpc-bind-port 38081 \
  --out-peers 64 --in-peers 128 \
  --add-exclusive-node <NODE_B_IP>:38080 \
  --add-exclusive-node <NODE_C_IP>:38080 \
  --no-igd --no-dns \
  --log-level 1
```

## Step 2: Start Node B

```bash
BIN=/opt/myt/bin
DATA=/var/lib/myt/node-b

$BIN/mytd \
  --testnet \
  --data-dir "$DATA" \
  --p2p-bind-ip 0.0.0.0 --p2p-bind-port 38080 \
  --rpc-bind-ip 127.0.0.1 --rpc-bind-port 38081 \
  --add-priority-node <SEED_A_IP>:38080 \
  --out-peers 32 --in-peers 64 \
  --no-igd --no-dns \
  --log-level 1
```

## Step 3: Start Node C (optional mining)

```bash
BIN=/opt/myt/bin
DATA=/var/lib/myt/node-c
WALLET_ADDR=<MINER_WALLET_ADDRESS>

$BIN/mytd \
  --testnet \
  --data-dir "$DATA" \
  --p2p-bind-ip 0.0.0.0 --p2p-bind-port 38080 \
  --rpc-bind-ip 127.0.0.1 --rpc-bind-port 38081 \
  --add-priority-node <SEED_A_IP>:38080 \
  --start-mining "$WALLET_ADDR" \
  --mining-threads 2 \
  --no-igd --no-dns \
  --log-level 1
```

## Step 4: Enable public RPC on one node

Use a dedicated node or reverse proxy in front of localhost RPC.
Do not expose unrestricted admin RPC methods publicly.

Recommended public endpoint target for website:

- `<PUBLIC_RPC_HOST>:38081`

## Step 5: Start Explorer

```bash
cd /opt/myt-explorer/build

./xmrblocks \
  --testnet \
  --bc-path /var/lib/myt/seed-a/testnet/lmdb \
  --daemon-url 127.0.0.1:38081 \
  --port 8081 \
  --bindaddr 127.0.0.1 \
  --enable-json-api=1 \
  --enable-pusher=1 \
  --enable-emission-monitor=1
```

Publish explorer URL as:
- `https://<EXPLORER_HOST>/`

## Step 6: Smoke tests

- `status` on all nodes shows peers > 0 after bootstrap
- `sync_info` on all nodes reaches same height
- Wallet test:
  - create wallet
  - `set_daemon <PUBLIC_RPC_HOST>:38081`
  - `refresh`
  - receive + send small tx
- Explorer shows latest block and tx

## Step 7: Update website values

Replace placeholders in website:
- `<PUBLIC_RPC_HOST>`
- `<SEED1_IP>`
- Explorer URL
- Download links for release artifacts
- SHA256SUMS and signature links

## Rollback plan

- If consensus mismatch is detected, stop all public nodes
- Revert to last known-good release artifacts
- Announce reset window and exact new bootstrap instructions
