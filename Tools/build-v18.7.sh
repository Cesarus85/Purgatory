#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v18.7-audio.apk" ]; then echo "V18.7 APK already exists"; exit 1; fi
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v187-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.AudioPolishValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/AudioPolish/unity-export.log"
rg -q '^QDMR_AUDIO_EXPORT_OK' "$quest_project_root/Verification/AudioPolish/unity-export.log"
bash "$quest_project_root/Tools/package-v18.7.sh" "$QDMR_GRADLE_EXPORT"
