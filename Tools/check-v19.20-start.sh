#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
quest_adb="/Applications/Unity/Hub/Editor/6000.3.2f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
quest_serial="${1:?Pass the verified connected Quest serial}"
quest_attempt="${2:?Pass a unique attempt label}"
case "$quest_attempt" in *[!a-zA-Z0-9_-]*|'') exit 1;; esac
quest_package=de.stefanmaier.questdemonmr
quest_log="$quest_project_root/Verification/StartupCrashV1920/start-$quest_attempt.log"
test ! -e "$quest_log"
"$quest_adb" -s "$quest_serial" shell dumpsys package "$quest_package" | rg 'versionName=0.19.20'
# Requires explicit user authorization for device startup tests. No data clearing.
"$quest_adb" -s "$quest_serial" shell am force-stop "$quest_package"
"$quest_adb" -s "$quest_serial" shell am start -W -n "$quest_package/com.unity3d.player.UnityPlayerGameActivity"
quest_pid=$("$quest_adb" -s "$quest_serial" shell pidof "$quest_package" | tr -d '\r')
test -n "$quest_pid"
echo "QDMR_START_TEST_BEGIN attempt=$quest_attempt pid=$quest_pid"
quest_ready=false
for quest_tick in $(seq 1 30); do
  "$quest_adb" -s "$quest_serial" logcat -d --pid="$quest_pid" -v threadtime Unity:V libc:F '*:S' > "$quest_log"
  quest_current=$("$quest_adb" -s "$quest_serial" shell pidof "$quest_package" | tr -d '\r' || true)
  if test "$quest_current" != "$quest_pid"; then echo "QDMR_START_TEST_FAIL process_exited pid=$quest_pid"; exit 1; fi
  if rg -q 'QDMR_START_PHASE scan content_ready=true' "$quest_log"; then quest_ready=true; break; fi
  sleep 2
done
if test "$quest_ready" != true; then echo "QDMR_START_TEST_INCOMPLETE initialization_not_observed check_headset_presence log=$quest_log"; exit 2; fi
sleep 20
"$quest_adb" -s "$quest_serial" logcat -d --pid="$quest_pid" -v threadtime Unity:V libc:F '*:S' > "$quest_log"
quest_current=$("$quest_adb" -s "$quest_serial" shell pidof "$quest_package" | tr -d '\r' || true)
test "$quest_current" = "$quest_pid"
if rg -q 'Fatal signal|FATAL EXCEPTION|Exception:|CachedReader::OutOfBounds' "$quest_log"; then echo "QDMR_START_TEST_FAIL runtime_error log=$quest_log"; exit 1; fi
rg 'QDMR_GUN_READY|QDMR_CONTENT_READY|QDMR_START_PHASE' "$quest_log"
rg -q 'QDMR_CONTENT_READY version=0.19.20.*missing=0' "$quest_log"
echo "QDMR_START_TEST_PASS attempt=$quest_attempt pid=$quest_pid initialized=true stable_after_init_seconds=20 log=$quest_log"
