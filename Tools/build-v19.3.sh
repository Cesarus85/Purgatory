#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.3-throwing-stars.apk" ]; then echo "V19.3 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/ThrowingStars"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v193-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.ThrowingStarValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/ThrowingStars/unity-export.log"
rg -q '^QDMR_STAR_EXPORT_OK' "$quest_project_root/Verification/ThrowingStars/unity-export.log"
bash "$quest_project_root/Tools/package-v19.3.sh" "$QDMR_GRADLE_EXPORT"
