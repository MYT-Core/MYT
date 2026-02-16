# Contributing to MYT

MYT is a Monero-based fork focused on a stable, reproducible protocol rollout.
Contributions are welcome, but consensus-critical changes are handled with extra care.

## Scope and Priorities

Current priority order:

1. Consensus safety and determinism
2. Network stability and node interoperability
3. Wallet reliability (sync, transfer, refresh)
4. Tooling and operational documentation
5. Performance optimizations

## Ground Rules

- Keep patches minimal and focused on one problem.
- Avoid unrelated refactors in the same commit.
- Preserve existing license headers and notices.
- Do not introduce breaking consensus changes without explicit discussion.
- Include tests for behavioral changes whenever possible.

## Branch and PR Workflow

- Branch from `main`.
- Use descriptive branch names, e.g. `fix/p2p-bootstrap-timeout`.
- Open a pull request with:
  - Problem statement
  - Root cause
  - Proposed fix
  - Risk assessment
  - Test evidence

## Commit Message Style

Use:

`<area>: <short description>`

Examples:

- `cryptonote_core: disable remote update checks`
- `p2p: remove hardcoded seed defaults`
- `wallet: improve testnet output selection handling`

## Consensus-Critical Changes

The following require explicit maintainer approval and rollout notes:

- Genesis parameters
- Network IDs and address prefixes
- Emission/halving logic
- Hardfork schedule/rules
- Transaction/block validation rules

For such changes, include:

- Migration impact
- Reset requirement (if any)
- Reproducible test procedure

## Testing Expectations

Before requesting merge:

- Build succeeds on target platform(s)
- Local multi-node testnet starts and peers cleanly
- Wallet A -> Wallet B transfer test passes
- No unexpected regressions in daemon/wallet startup

## Security and Disclosure

Do not publish exploitable details for active vulnerabilities.
For sensitive issues, contact maintainers privately first.

## Attribution and License

MYT is based on Monero and keeps upstream license obligations.
By contributing, you agree your patch is licensed under the repository license terms.
