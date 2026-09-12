#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
runtime=/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/Resources/Scripting/NetCoreRuntime/dotnet
compiler=/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll
# Cached SDK reference compilation only. This does not start Unity or build an APK.
"$runtime" "$compiler" @Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.rsp \
  -out:Verification/V17/Assembly-CSharp.dll -refout:Verification/V17/Assembly-CSharp.ref.dll
"$runtime" "$compiler" @Verification/V17/editor.rsp Assets/QuestDemonMR/Editor/SparseWoundValidation.cs \
  Assets/QuestDemonMR/Editor/StartupValidation.cs Assets/QuestDemonMR/Editor/GradleDuplicateGuard.cs
"$runtime" "$compiler" @Verification/V17/core-tests.rsp
"$runtime" Verification/V17/CoreTests.dll
python3 Tools/test_analyze_v17.py
