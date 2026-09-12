#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.8-room-reliability.apk" ]; then echo "V19.8 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/RoomReliability"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v198-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.RoomReliabilityValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/RoomReliability/unity-export.log"
rg -q '^QDMR_ROOM_RELIABILITY_EXPORT_OK' "$quest_project_root/Verification/RoomReliability/unity-export.log"
if rg -q 'error CS[0-9]|Shader error|Exception:' "$quest_project_root/Verification/RoomReliability/unity-export.log"; then echo "Export diagnostics failed; APK packaging withheld"; exit 1; fi
bash "$quest_project_root/Tools/package-v19.8.sh" "$QDMR_GRADLE_EXPORT"
