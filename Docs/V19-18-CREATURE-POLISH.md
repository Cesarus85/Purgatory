# V19.18 — final bounded creature animation polish

Approved scope: authored narrower demon arm gait; three-joint tongue with occasional click; first seal-portal demon points at the player without delaying reinforcement; supple bat abdomen; eyes/head lead chest, directional shoulder response and per-creature variation. No new enemy type, wave rule or navigation rewrite. The user subsequently added the fifth-shot shotgun smoke return, detailed below; other weapon properties are unchanged.

Implementation sequence:
1. Preserve delivered FBX, refine existing Blender rig and gait while retaining all planted-foot trajectories and portal/combat clips.
2. Add bounded additive presentation with explicit state ownership: death/entry/leap/vault first, hit/attack next, gesture next, idle details last. Pause freezes clocks; offsets never accumulate.
3. Hook gesture only after first successful entry at an optional reinforcement portal. Gesture is interruptible, cannot alter the three-second seal clock, enemy quota or movement. Reinforcement gets a brief attention response, not an omniscient new route.
4. Add bounded low-priority mouth foley using existing shared enemy voice budget. Protect attack warnings.
5. Native pose/skin/contact/gesture/pause tests, rendered multi-pose review, existing full regression chain, separate 0.19.18/code57 APK. No installation without a new device request.

Acceptance: close-range gait from front and side without penguin arms or foot sliding; tongue visible during occasional mouth movement but no detached teeth; a seal-portal first demon points while continuing pursuit and reinforcement timing is unchanged; bat hindquarters flex in straight flight and trail turns without twitching. Check both hands/weapons, hit interruptions, portal thresholds, pause/resume and sustained Quest frame timing. Automated checks do not constitute worn-headset acceptance.

## Implementation details

- `BlenderSource/polish_creatures_v1918.py` opens the delivered V19.16 scene, reauthors only upper-body gait keys using explicit elbow/wrist targets in model space, checks unchanged foot positions in every edited frame, and preserves timeline 1–770. New source: `RiftStalkerV19_18.blend`. Existing 102-vertex tongue insert is weighted across Tongue1–3, parented to FaceJaw; no extra triangles or morph targets. Rig has 24 bones. Original FBX is preserved in `Verification/CreaturePolish/baseline-EmberfiendAnimatedV12.fbx`.
- `DemonAgent.Expression` layers a bounded 0.85-second pointing pose onto locomotion. Optional portal, successful first entry, living ground enemy and clear view are required. The target is recorded on that encounter; a later reinforcement acknowledges that direction for 0.65 seconds. No quota, seal timeout, navigation or movement change. Hits, attacks and special traversal interrupt the gesture; interruption cannot replay on landing.
- Tongue joints follow the jaw with a slight independent sway and occasional curl, roughly 5.5–10 seconds apart after a staggered initial delay. A short procedural mouth click uses three prewarmed mono variants and the existing eight-voice enemy bus. Rally uses an existing archetype-specific guttural voice, below attack-warning priority. Generated clip lifetime survives scene cleanup, and the bank reloads destroyed entries safely.
- Bat PelvicCurl now flexes with an offset from wing phase, reduced during glide, with damped vertical/turn follow-through. Existing U-attack and portal/dive poses keep priority. No new physical displacement or collider.
- Slower chest follow relative to head, archetype posture differences, independent gait start phases, small hand follow-through and left/right shoulder reactions from the actual contact position. Rapid hits still interrupt damage delivery but do not repeatedly restart alternate full-body flinch clips.
- No new textures, enemies, combat rules or graphics passes. Three extra deform bones on ground enemies and a small additive pose layer; hardware frame timing remains an explicit acceptance check.

## Verification notes

The first focused run passed movement/gesture checks but caught a destroyed generated mouth clip after scene cleanup. The corrected run passes 148 focused checks, including bounded gait width, actual player-pointing direction, unchanged foot/root positions and seal time, hit/pause/leap interruption, tongue and bat flex, pose restoration, foley variants and audio priorities. Full existing sparse skinning, entry, room and combat regression is required before delivery. Review captures use freshly baked posed meshes because repeated edit-mode Camera.Render calls can reuse the first frame's GPU skinning result; this is a review-only workaround, not runtime mesh baking.

## Added during implementation: shotgun return

User additionally requested smoke dissolution after the fifth shot. `ShotgunDeparture` now sweeps the entire weapon from stock to muzzle in 0.85 seconds, using the existing depth-aware material clipping plane covered by soft pale smoke. Both body and pump participate, status text disappears during dissolution. World-space smoke remains briefly after the revolver returns. A bounded 96-particle rig is prepared with the gun; no new shader/pass, runtime asset download or per-frame object creation. The return timer and smoke freeze during pause. Spent shotgun cannot fire or grab its pump; original revolver ammo is preserved. Reset/player death clear effects and restore material clipping for the next bonus. Focused tests cover the real fifth-shot trigger, intermediate visibility, pause, delayed return and cleanup.

First full creature regression passed all suites, but export failed reading `Library/Bee/1300b0aP-inputdata.json` with `ERROR_NOT_SUPPORTED`. Filesystem flags showed `compressed,dataless`: the generated file was not locally resident. With Unity stopped, it was renamed to `1300b0aP-inputdata.v1918-recoverable.json` in the same cache directory so Unity can regenerate the input. No source, room save or prior APK was removed. A fresh full build includes the smoke addition and the corrected close-up framing.

## Headset acceptance checklist (still open)

1. Watch several ground enemies from the front and side: arms stay nearer the torso, gait differs slightly, head/chest look less synchronous. Look for a brief tongue curl at close range and verify teeth/eyes stay attached.
2. Leave an optional seal portal intact: the first eligible ground enemy points a claw toward the player while moving; a later reinforcement briefly looks that way. Combat/hits interrupt the pose; the portal must not wait for the gesture.
3. Watch straight flight, turns and the existing U-shaped bat attack: abdomen and hindquarters follow through without moving hit surfaces away from the mesh.
4. Fire all five shotgun rounds: the stock-to-muzzle smoke sweep lasts 0.85 seconds, includes the pump and hides its label. The revolver returns with its previous ammunition; there must be no sixth shotgun shot.
5. Pause mid-sweep and resume; then repeat the bonus, including left-handed play. Check that no clipped weapon surfaces, stuck smoke, extra pump grabs or lost throwing stars remain.
6. Compare sustained Quest frame timing and nearby multi-enemy combat against V19.17. Editor captures and automated checks do not verify headset performance or visual quality.

## Delivery — 2026-09-08

Final full validation/export succeeded in `Verification/CreaturePolish/unity-qdmr-v1918-export.nN7qfX.log`, followed by successful ARM64 IL2CPP/Gradle packaging. The focused creature/smoke suite now passes **156 checks** (the 148 above describes the earlier creature-only iteration). All existing validation suites in the export chain also passed; the log contains 43 `*_OK checks=` entries, including nested/repeated suite invocations. No C# errors, shader errors or exceptions were found in this final export log. Resource/shader collection was unusually slow but completed without further cache intervention.

- APK: `Builds/Purgatory-v19.18-creature-polish.apk`, 134,319,918 bytes.
- Package/version: `de.stefanmaier.questdemonmr`, `0.19.18`, code `57`, `arm64-v8a`.
- SHA-256: `c973e272b51a743cdda001f6d1793a3bd30bfb0467a1e67cd6c253e6bc29c860`.
- ZIP integrity and APK v2 signature verified; signing certificate unchanged from V19.17. Detailed hashes in `Verification/CreaturePolish/delivery.json`.
- V19.17 APK is unchanged (SHA-256 `c45d968e3dc731ce48f5a11a95e47b19d61309fc661be22897b0012769601cb8`). No previous delivery or user room data was deleted.
- Not installed this turn. Worn-headset acceptance and frame-time measurements remain open.
