#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <version> [source_dir] [depends_jobs] [build_jobs]"
  echo "Example: $0 v1.0.0-testnet.2 /home/max/projects/crypto/myt-base/myt"
  exit 1
fi

VERSION="$1"
SOURCE_DIR="${2:-$(pwd)}"
DEPENDS_JOBS="${3:-4}"
BUILD_JOBS="${4:-4}"

HOST="x86_64-w64-mingw32"
DEPENDS_DIR="${SOURCE_DIR}/contrib/depends"
BUILD_DIR="${SOURCE_DIR}/build-win64-release"
OUTPUT_DIR="${SOURCE_DIR}/release-win"
ARTIFACT_ROOT="${OUTPUT_DIR}/myt-${VERSION}-win64"
BIN_DIR="${ARTIFACT_ROOT}/bin"

mkdir -p "${OUTPUT_DIR}" "${BIN_DIR}"

cd "${DEPENDS_DIR}"
chmod +x config.guess config.sub
make HOST="${HOST}" -j"${DEPENDS_JOBS}"

cd "${SOURCE_DIR}"
cmake -S . -B "${BUILD_DIR}" \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_TOOLCHAIN_FILE="${DEPENDS_DIR}/${HOST}/share/toolchain.cmake"

cmake --build "${BUILD_DIR}" --target daemon simplewallet wallet_rpc_server -j"${BUILD_JOBS}"

for f in mytd.exe myt-wallet-cli.exe myt-wallet-rpc.exe; do
  if [[ ! -f "${BUILD_DIR}/bin/${f}" ]]; then
    echo "Missing expected binary: ${BUILD_DIR}/bin/${f}"
    exit 1
  fi
done

cp "${BUILD_DIR}/bin/mytd.exe" "${BIN_DIR}/"
cp "${BUILD_DIR}/bin/myt-wallet-cli.exe" "${BIN_DIR}/"
cp "${BUILD_DIR}/bin/myt-wallet-rpc.exe" "${BIN_DIR}/"

LAUNCHER_EXE="${SOURCE_DIR}/scripts/windows/MYTLauncherExe/publish/MYTLauncher.exe"
if [[ -f "${LAUNCHER_EXE}" ]]; then
  cp "${LAUNCHER_EXE}" "${BIN_DIR}/MYTLauncher.exe"
  echo "Included launcher: ${LAUNCHER_EXE}"
else
  echo "Launcher not found at ${LAUNCHER_EXE} (skipping)."
fi

cat > "${ARTIFACT_ROOT}/README.txt" <<README
MYT ${VERSION} Windows x64 release artifacts.

Binaries:
- mytd.exe
- myt-wallet-cli.exe
- myt-wallet-rpc.exe
- MYTLauncher.exe (if available)

Verify integrity using SHA256SUMS.txt and signature file.
README

(
  cd "${OUTPUT_DIR}"
  zip -r "myt-${VERSION}-win64.zip" "myt-${VERSION}-win64" >/dev/null
)

echo "Windows release artifacts created in: ${OUTPUT_DIR}"
echo "Zip: ${OUTPUT_DIR}/myt-${VERSION}-win64.zip"
