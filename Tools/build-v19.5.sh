#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.5-room-profiles.apk" ]; then echo "V19.5 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/RoomProfiles"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v195-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.RoomProfilesValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/RoomProfiles/unity-export.log"
rg -q '^QDMR_PROFILE_EXPORT_OK' "$quest_project_root/Verification/RoomProfiles/unity-export.log"
bash "$quest_project_root/Tools/package-v19.5.sh" "$QDMR_GRADLE_EXPORT"
