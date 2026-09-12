#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.4-hand-roles.apk" ]; then echo "V19.4 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/HandRoles"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v194-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.HandRolesValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/HandRoles/unity-export.log"
rg -q '^QDMR_HAND_EXPORT_OK' "$quest_project_root/Verification/HandRoles/unity-export.log"
bash "$quest_project_root/Tools/package-v19.4.sh" "$QDMR_GRADLE_EXPORT"
