#!/bin/bash
# Recovery only after Unity has successfully generated the V18.3 IL2CPP player.
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_generated="$quest_project_root/Library/Bee/Android/Prj/IL2CPP/Gradle"
rg -q '^unity.versionName=0.18.3$' "$quest_generated/gradle.properties"
rg -q '^unity.versionCode=21$' "$quest_generated/gradle.properties"
quest_apk="$quest_project_root/Builds/QuestDemonMR-v18.3-spawn-fix.apk"
if [ -e "$quest_apk" ]; then echo "Refusing to overwrite existing V18.3 APK"; exit 1; fi
quest_stage=$(mktemp -d /private/tmp/qdmr-v183-gradle.XXXXXX)
rsync -a --exclude='.gradle/' --exclude='**/build/***' --exclude='* [0-9]*' "$quest_generated/" "$quest_stage/"
cd "$quest_stage/launcher"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
"$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" \
  org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_apk"
shasum -a 256 "$quest_apk"
echo "Retained recovery build directory: $quest_stage"
