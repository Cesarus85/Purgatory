#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
test ! -e "$quest_project_root/Builds/Purgatory-v20.2-katana-combat-polish.apk"
mkdir -p "$quest_project_root/Verification/V20.2"
bash "$quest_project_root/Tools/quarantine-typedb-copies.sh"
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v202-export.XXXXXX)
quest_build_log="$quest_project_root/Verification/V20.2/unity-$(basename "$QDMR_GRADLE_EXPORT").log"
echo "Unity log: $quest_build_log"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.V202Validation.ValidateAndExport -logFile "$quest_build_log"
rg -q '^QDMR_V202_EXPORT_OK' "$quest_build_log"
if rg -q 'error CS[0-9]|Shader error|Exception:|TypeDB: Class' "$quest_build_log"; then echo "Export diagnostics failed"; exit 1; fi
bash "$quest_project_root/Tools/package-v20.2.sh" "$QDMR_GRADLE_EXPORT"
