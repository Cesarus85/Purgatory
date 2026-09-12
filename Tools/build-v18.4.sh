#!/bin/bash
# Reproducible A1/A2 validation, Unity export and isolated Gradle packaging.
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_apk="$quest_project_root/Builds/QuestDemonMR-v18.4-combat-animation.apk"
if [ -e "$quest_apk" ]; then echo "Refusing to overwrite existing V18.4 APK"; exit 1; fi
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v184-export.XXXXXX)
quest_editor="/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity"
"$quest_editor" -batchmode -force-metal -quit -projectPath "$quest_project_root" \
  -executeMethod QuestDemonMR.Editor.CombatPolishValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/CombatPolish/unity-export.log"
rg -q '^QDMR_COMBAT_EXPORT_OK' "$quest_project_root/Verification/CombatPolish/unity-export.log"
bash "$quest_project_root/Tools/package-v18.4.sh" "$QDMR_GRADLE_EXPORT"
