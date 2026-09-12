#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v19.1-rhythm-life.apk" ]; then echo "V19.1 APK already exists"; exit 1; fi
mkdir -p "$quest_project_root/Verification/RhythmLife"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v191-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.RhythmLifeValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/RhythmLife/unity-export.log"
rg -q '^QDMR_LIFE_EXPORT_OK' "$quest_project_root/Verification/RhythmLife/unity-export.log"
bash "$quest_project_root/Tools/package-v19.1.sh" "$QDMR_GRADLE_EXPORT"
