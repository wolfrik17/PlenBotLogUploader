#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PUBLISH_DIR="${1:-${ROOT_DIR}/artifacts/publish/linux-x64}"
OUTPUT_DIR="${2:-${ROOT_DIR}/artifacts/appimage}"
APP_DIR="${OUTPUT_DIR}/PlenBotLogUploader.AppDir"

echo "=== Building PlenBotLogUploader AppImage ==="
echo "Publish Dir: ${PUBLISH_DIR}"
echo "Output Dir:  ${OUTPUT_DIR}"

rm -rf "${APP_DIR}"
mkdir -p "${APP_DIR}/usr/bin"
mkdir -p "${APP_DIR}/usr/share/icons/hicolor/256x256/apps"
mkdir -p "${OUTPUT_DIR}"

# Copy published application files
cp -r "${PUBLISH_DIR}/." "${APP_DIR}/usr/bin/"
chmod +x "${APP_DIR}/usr/bin/PlenBotLogUploader"

# Copy AppRun and desktop file
cp "${SCRIPT_DIR}/AppRun" "${APP_DIR}/AppRun"
chmod +x "${APP_DIR}/AppRun"

cp "${SCRIPT_DIR}/plenbotloguploader.desktop" "${APP_DIR}/plenbotloguploader.desktop"

# Extract or copy icon
if [ -f "${ROOT_DIR}/Resources/Icon.png" ]; then
    cp "${ROOT_DIR}/Resources/Icon.png" "${APP_DIR}/plenbotloguploader.png"
    cp "${ROOT_DIR}/Resources/Icon.png" "${APP_DIR}/.DirIcon"
    cp "${ROOT_DIR}/Resources/Icon.png" "${APP_DIR}/usr/share/icons/hicolor/256x256/apps/plenbotloguploader.png"
elif command -v icotool >/dev/null 2>&1; then
    icotool -x -o "${APP_DIR}/" "${ROOT_DIR}/Resources/Icon.ico"
    # Find largest png
    LARGEST_ICON=$(ls -S "${APP_DIR}"/Icon_*.png 2>/dev/null | head -n 1)
    if [ -n "${LARGEST_ICON}" ]; then
        cp "${LARGEST_ICON}" "${APP_DIR}/plenbotloguploader.png"
        cp "${LARGEST_ICON}" "${APP_DIR}/.DirIcon"
        cp "${LARGEST_ICON}" "${APP_DIR}/usr/share/icons/hicolor/256x256/apps/plenbotloguploader.png"
        rm -f "${APP_DIR}"/Icon_*.png
    fi
fi

# Download appimagetool if not present
APPIMAGETOOL="${OUTPUT_DIR}/appimagetool-x86_64.AppImage"
if [ ! -f "${APPIMAGETOOL}" ]; then
    echo "Downloading appimagetool..."
    curl -sSL "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" -o "${APPIMAGETOOL}"
    chmod +x "${APPIMAGETOOL}"
fi

# Build AppImage
echo "Packaging AppImage with appimagetool..."
ARCH=x86_64 "${APPIMAGETOOL}" --appimage-extract-and-run "${APP_DIR}" "${OUTPUT_DIR}/PlenBotLogUploader-x86_64.AppImage"

echo "=== AppImage created successfully at ${OUTPUT_DIR}/PlenBotLogUploader-x86_64.AppImage ==="
