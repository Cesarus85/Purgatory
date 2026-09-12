#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_export="${1:?Provide completed V20.2 Unity export}"
case "$quest_export" in /private/tmp/qdmr-v202-export.*) ;; *) echo "Unexpected export path"; exit 1;; esac
rg -q '^unity.versionName=0.20.2$' "$quest_export/gradle.properties"
rg -q '^unity.versionCode=62$' "$quest_export/gradle.properties"
quest_apk="$quest_project_root/Builds/Purgatory-v20.2-katana-combat-polish.apk"
test ! -e "$quest_apk"
quest_stage=$(mktemp -d /private/tmp/qdmr-v202-package.XXXXXX)
rsync -a --exclude='* [0-9]*' "$quest_export/" "$quest_stage/"
test -x "$quest_stage/unityLibrary/src/main/Il2CppOutputProject/IL2CPP/build/deploy/il2cpp"
cd "$quest_stage/launcher"
export JAVA_HOME="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/OpenJDK"
"$JAVA_HOME/bin/java" -classpath "/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/Tools/gradle/lib/gradle-launcher-8.13.jar" \
  org.gradle.launcher.GradleMain --no-daemon --no-parallel --max-workers=2 --offline --console=plain assembleRelease
cp -n "$quest_stage/launcher/build/outputs/apk/release/launcher-release.apk" "$quest_apk"
shasum -a 256 "$quest_apk"
echo "Export retained: $quest_export"
echo "Packaging checkout: $quest_stage"
