#!/bin/bash
# Accept only the explicit temporary export and expected application version.
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_export="${1:?Provide the completed Unity export directory}"
case "$quest_export" in /private/tmp/qdmr-v184-export.*) ;; *) echo "Unexpected export directory"; exit 1;; esac
rg -q '^unity.versionName=0.18.4$' "$quest_export/gradle.properties"
rg -q '^unity.versionCode=22$' "$quest_export/gradle.properties"
quest_apk="$quest_project_root/Builds/QuestDemonMR-v18.4-combat-animation.apk"
if [ -e "$quest_apk" ]; then echo "Refusing to overwrite existing V18.4 APK"; exit 1; fi
quest_stage=$(mktemp -d /private/tmp/qdmr-v184-package.XXXXXX)
# Do not exclude arbitrary **/build: Unity ships the required IL2CPP compiler
# itself under Il2CppOutputProject/IL2CPP/build/deploy in an exported project.
rsync -a --exclude='.gradle/' --exclude='/build/' --exclude='/launcher/build/' \
  --exclude='/unityLibrary/build/' --exclude='/unityLibrary/xrmanifest.androidlib/build/' \
  --exclude='* [0-9]*' "$quest_export/" "$quest_stage/"
cd "$quest_stage/launcher"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
"$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" \
  org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_apk"
shasum -a 256 "$quest_apk"
echo "Retained export: $quest_export"
echo "Retained packaging: $quest_stage"
