#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -force-metal -quit -projectPath "$quest_project_root" \
  -executeMethod QuestDemonMR.Editor.PortalRevisionValidation.ValidateAndBuild \
  -logFile "$quest_project_root/Logs/v18.2-portals-build.log"
