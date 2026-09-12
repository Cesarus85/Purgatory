#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_export="${1:?Provide the completed V19.4 Unity export}"
case "$quest_export" in /private/tmp/qdmr-v194-export.*) ;; *) echo "Unexpected export path"; exit 1;; esac
rg -q '^unity.versionName=0.19.4$' "$quest_export/gradle.properties"
rg -q '^unity.versionCode=43$' "$quest_export/gradle.properties"
quest_apk="$quest_project_root/Builds/QuestDemonMR-v19.4-hand-roles.apk"
if [ -e "$quest_apk" ]; then echo "Refusing to overwrite V19.4 delivery APK"; exit 1; fi
quest_stage="/private/tmp/qdmr-v184-package.SE7n42"
if [ ! -f "$quest_stage/gradle.properties" ]; then
  quest_stage=$(mktemp -d /private/tmp/qdmr-v194-package.XXXXXX)
else
  rg -q '^unity.versionName=0.19.[34]$' "$quest_stage/gradle.properties"
fi
# Explicit task-owned temporary checkout; keep only native tools and build caches.
rsync -a --delete --exclude='.gradle/' --exclude='/build/' --exclude='/launcher/build/' \
  --exclude='/unityLibrary/build/' --exclude='/unityLibrary/xrmanifest.androidlib/build/' \
  --exclude='* [0-9]*' "$quest_export/" "$quest_stage/"
test -x "$quest_stage/unityLibrary/src/main/Il2CppOutputProject/IL2CPP/build/deploy/il2cpp"
cd "$quest_stage/launcher"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
"$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" \
  org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_apk"
shasum -a 256 "$quest_apk"
echo "Export retained: $quest_export"
echo "Temporary packaging checkout: $quest_stage"
