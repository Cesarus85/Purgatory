#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
if [ -e "$quest_project_root/Builds/QuestDemonMR-v18.5-projectiles.apk" ]; then echo "V18.5 APK already exists"; exit 1; fi
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v185-export.XXXXXX)
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.ProjectilePolishValidation.ValidateAndExport \
  -logFile "$quest_project_root/Verification/ProjectilePolish/unity-export.log"
rg -q '^QDMR_PROJECTILE_EXPORT_OK' "$quest_project_root/Verification/ProjectilePolish/unity-export.log"
bash "$quest_project_root/Tools/package-v18.5.sh" "$QDMR_GRADLE_EXPORT"
