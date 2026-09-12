#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$quest_project_root"
test ! -e Builds/Purgatory-v20.6-contact-portals.apk
bash Tools/quarantine-typedb-copies.sh
export QDMR_GRADLE_EXPORT
QDMR_GRADLE_EXPORT=$(mktemp -d /private/tmp/qdmr-v206-export.XXXXXX)
quest_log="$QDMR_GRADLE_EXPORT.unity.log"
echo "Unity log: $quest_log"
"/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity" -batchmode -force-metal -quit -projectPath "$quest_project_root" -executeMethod QuestDemonMR.Editor.V206Validation.ValidateAndExport -logFile "$quest_log"
rg -q '^QDMR_V206_EXPORT_OK' "$quest_log"
if rg -q 'error CS[0-9]|Shader error|Exception:|TypeDB: Class' "$quest_log"; then exit 1; fi
quest_stage=$(mktemp -d /private/tmp/qdmr-v206-package.XXXXXX)
rsync -a --exclude='* [0-9]*' "$QDMR_GRADLE_EXPORT/" "$quest_stage/"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
echo "Gradle log: $quest_stage/gradle.log"
cd "$quest_stage/launcher"
if ! "$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease > "$quest_stage/gradle.log" 2>&1; then tail -80 "$quest_stage/gradle.log";exit 1;fi
rg '^BUILD SUCCESSFUL|actionable tasks:' "$quest_stage/gradle.log"
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_project_root/Builds/Purgatory-v20.6-contact-portals.apk"
echo "EXPORT=$QDMR_GRADLE_EXPORT STAGE=$quest_stage"
