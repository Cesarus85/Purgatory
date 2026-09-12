#!/bin/bash
set -euo pipefail
quest_project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$quest_project_root"
# Only reproducible generated metadata, and only numbered copies with an original.
# Preserve every moved byte for inspection/recovery; no recursive deletion.
quest_quarantine=$(mktemp -d "$quest_project_root/Verification/StartupCrashV1920/typedb-quarantine.XXXXXX")
quest_count=0
while IFS= read -r quest_copy; do
  case "$quest_copy" in Library/BuildPlayerData/Player/*|Library/BuildPlayerData/Editor/*) ;; *) exit 1;; esac
  quest_original=$(printf '%s' "$quest_copy" | sed -E 's/ [0-9]+\.json$/.json/')
  test -f "$quest_original" || continue
  quest_relative=${quest_copy#Library/BuildPlayerData/}
  mkdir -p "$quest_quarantine/$(dirname "$quest_relative")"
  mv -n "$quest_copy" "$quest_quarantine/$quest_relative"
  quest_count=$((quest_count + 1))
done < <(rg --files Library/BuildPlayerData | rg ' [0-9]+\.json$' || true)
echo "QDMR_TYPEDB_QUARANTINED count=$quest_count path=$quest_quarantine"
