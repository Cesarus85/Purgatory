#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$quest_project_root"
test ! -e Builds/Purgatory-v20.4-katana-seal-polish.apk
mkdir -p Verification/V20.4
bash Tools/quarantine-typedb-copies.sh
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v204-export.XXXXXX)
quest_build_log="$QDMR_GRADLE_EXPORT.unity.log"
echo "Unity log: $quest_build_log"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.V204Validation.ValidateAndExport -logFile "$quest_build_log"
rg -q '^QDMR_V204_EXPORT_OK' "$quest_build_log"
if rg -q 'error CS[0-9]|Shader error|Exception:|TypeDB: Class' "$quest_build_log"; then echo 'Export diagnostics failed'; exit 1; fi
bash Tools/package-v20.4.sh "$QDMR_GRADLE_EXPORT"
python3 Tools/verify-v20.4.py "$QDMR_GRADLE_EXPORT" "$quest_build_log"
