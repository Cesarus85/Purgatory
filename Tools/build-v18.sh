#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_editor_path="/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity"
# GPU-backed synthetic geometry checks, then versioned Android packaging.
# This does not install, launch or run a long headset benchmark.
"$quest_editor_path" -batchmode -force-metal -quit \
  -projectPath "$quest_project_root" \
  -executeMethod QuestDemonMR.Editor.LiveScanValidation.ValidateAndBuild \
  -logFile "$quest_project_root/Logs/v18-livescan-build.log"
