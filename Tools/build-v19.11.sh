#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.11-scan-acceptance.apk" ]; then echo "V19.11 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/ScanAcceptance"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v1911-export.XXXXXX)
quest_build_log="$quest_project_root/Verification/ScanAcceptance/unity-export-$(basename "$QDMR_GRADLE_EXPORT").log"
echo "Unity log: $quest_build_log"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.ScanAcceptanceValidation.ValidateAndExport \
  -logFile "$quest_build_log"
rg -q '^QDMR_ACCEPTANCE_EXPORT_OK' "$quest_build_log"
if rg -q 'error CS[0-9]|Shader error|Exception:' "$quest_build_log"; then echo "Export diagnostics failed; APK packaging withheld"; exit 1; fi
bash "$quest_project_root/Tools/package-v19.11.sh" "$QDMR_GRADLE_EXPORT"
