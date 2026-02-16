# MYT Testnet Parameter Freeze

This document freezes the consensus and network-critical parameters for public testnet launch.
Any change in this list requires a coordinated chain reset and a new release tag.

## Freeze Source

- File: `src/cryptonote_config.h`
- Freeze date: 2026-02-15

## Consensus Parameters (Frozen)

- Money supply: `MONEY_SUPPLY = 21000000000000000` (21,000,000 MYT with 9 decimals)
- Halving interval (height-based): `MYT_HALVING_INTERVAL_BLOCKS = 2102400`
- Block target: `DIFFICULTY_TARGET_V1 = 60`, `DIFFICULTY_TARGET_V2 = 60`

## Network Parameters (Frozen)

Mainnet:
- Address prefix: `55`
- Integrated prefix: `56`
- Subaddress prefix: `57`
- P2P/RPC/ZMQ ports: `28080 / 28081 / 28082`
- Network ID: `db1e1199-35bb-4bbc-b1de-6a138dd0c477`
- Genesis nonce: `0x4D595454`

Testnet:
- Address prefix: `155`
- Integrated prefix: `156`
- Subaddress prefix: `157`
- P2P/RPC/ZMQ ports: `38080 / 38081 / 38082`
- Network ID: `bfe865fb-d1a8-4321-9b86-8e24a5666ecc`
- Genesis nonce: `0x4D595454`

Stagenet:
- Address prefix: `255`
- Integrated prefix: `256`
- Subaddress prefix: `257`
- P2P/RPC/ZMQ ports: `48080 / 48081 / 48082`
- Network ID: `63aff2b5-0ac1-44f2-8bc0-7cf73cf7049b`
- Genesis nonce: `0x4D595454`

## Change Policy

Allowed without reset:
- Website text
- Documentation
- Scripts and tooling
- Explorer UI-only changes

Not allowed without reset:
- Genesis tx/nonce
- Network IDs
- Address prefixes
- Consensus emission/halving/difficulty constants
- Hardfork schedule affecting consensus

## Release Rule

Public testnet launch must use binaries built from a commit that matches this freeze document.
