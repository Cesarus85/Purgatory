#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_export="${1:?Provide completed V20.4 Unity export}"
case "$quest_export" in /private/tmp/qdmr-v204-export.*) ;; *) echo 'Unexpected export path'; exit 1;; esac
rg -q '^unity.versionName=0.20.4$' "$quest_export/gradle.properties"
rg -q '^unity.versionCode=64$' "$quest_export/gradle.properties"
quest_apk="$quest_project_root/Builds/Purgatory-v20.4-katana-seal-polish.apk"
test ! -e "$quest_apk"
quest_stage=$(mktemp -d /private/tmp/qdmr-v204-package.XXXXXX)
rsync -a --exclude='* [0-9]*' "$quest_export/" "$quest_stage/"
cd "$quest_stage/launcher"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
echo "Gradle log: $quest_stage/gradle.log"
if ! "$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" \
  org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease > "$quest_stage/gradle.log" 2>&1; then
  tail -80 "$quest_stage/gradle.log"; exit 1
fi
rg '^BUILD SUCCESSFUL|actionable tasks:' "$quest_stage/gradle.log"
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_apk"
shasum -a 256 "$quest_apk"
echo "Packaging checkout: $quest_stage"
