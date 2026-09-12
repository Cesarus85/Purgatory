#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v18.11-room-use.apk" ]; then echo "V18.11 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/RoomUse"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v1811-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.RoomUseValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/RoomUse/unity-export.log"
rg -q '^QDMR_ROOM_USE_EXPORT_OK' "$quest_project_root/Verification/RoomUse/unity-export.log"
bash "$quest_project_root/Tools/package-v18.11.sh" "$QDMR_GRADLE_EXPORT"
