# Purgatory V19.17 — Portal presence and weapon surface detail

## Requested scope

- Make portal-opening room dimming clearly stronger and longer.
- Sharpen both weapons without changing their accepted scale, grip, pump, sound or gameplay.

## Implemented

Portal atmosphere now lasts 2.6 seconds (previously 1.05). Relative Meta passthrough brightness dips by up to 0.38 API units (previously 0.115), holds briefly, partially recovers, then has a slow secondary dip and recovery. These values are not physical brightness percentages. The existing four-second onset cooldown prevents overlapping portals from stacking or constantly restarting the effect. Portals opening together share one atmospheric event. Brightness is never driven below -0.55 by this effect; an already darker custom baseline is left unchanged. Pause, disable, focus loss, encounter cleanup and the KLANG → PORTAL-DUNKEL switch restore the original style. Custom LUT/grayscale mappings remain untouched. No rapid strobe or white flash.

Shotgun: restore the user's original 2048×2048 base-color, normal, occlusion and metallic/roughness sources. The previous pipeline had reduced them to 1024×1024. Repack native ORM channels to Unity metallic-R/smoothness-A without resizing. New runtime resource directory avoids the legacy 1K-only shotgun importer. Body and pump retain separate texture sets.

Revolver: reopen the original authored Blender scene and bake its procedural materials at 4096×4096 onto the existing UVs, rather than upscaling the old 2K image. Bake a separate 2048×2048 tangent-space normal atlas from subtle procedural metal and walnut surface relief. Existing production FBX, dimensions, pivots, sockets and animations are untouched.

Both: targeted Android ASTC 4×4 compression, trilinear mipmaps, anisotropy 8 and mild -0.2 mip bias. No readable CPU texture copies. More texture memory is a deliberate near-hand quality tradeoff; this is not a free performance improvement. Expected new compressed maps including mips total about 69 MiB versus about 7 MiB for previous active weapon maps. Old resources are retained for rollback and remain in the package, but are no longer selected by the live weapon materials.

Reproducible source preparation: `BlenderSource/bake_weapons_v1917.py`. New Blender source: `BlenderSource/AshwardenRevolverV19_17.blend`. Explicit import utility: `WeaponDetailImport.Configure`; no new global texture postprocessor.

## Verification and delivery

Focused validation checks actual runtime materials, native dimensions, Android settings, mip/filter settings, and shader compilation. Atmosphere regression checks include sustained darkening, baseline floor, no stacking, cooldown, restoration and the saved comfort toggle. Full existing gameplay/room/weapon regression chain runs before the Android export.

Build entry: `bash Tools/build-v19.17.sh`. Completed APK: `Builds/Purgatory-v19.17-weapons-portal.apk`, version 0.19.17 / code 56, 134,310,182 bytes. All 39 suite entries passed (3,724 logged checks including nested tests and curve samples); focused checks: 39 weapon / 343 atmosphere. Unity export and native Gradle build succeeded. No C# or shader errors or exceptions in the successful build log. ZIP integrity, package/version, ARM64 payload and matching APK v2 signer verified. SHA-256: `c45d968e3dc731ce48f5a11a95e47b19d61309fc661be22897b0012769601cb8`. Previous V19.16 APK checksum is unchanged. Machine-readable evidence: `Verification/WeaponDetail/delivery.json`.

Fresh native renders of both weapons inspected and preserved in `Verification/WeaponDetail`. The first attempt stopped on an overly broad new material assertion that included muzzle-flame renderers; the corrected test checks the five authored revolver surface types and the two shotgun meshes. The subsequent full build passed. No headset installation or worn-headset acceptance performed.

## Worn-headset acceptance still required

1. Portal opening in a normally lit room: clearly visible dimming and slow recovery, room remains usable; multiple simultaneous portals do not compound it.
2. Pause during a dip, resume and toggle PORTAL-DUNKEL off/on: no stuck dark passthrough.
3. Inspect both weapons close up and at oblique angles: walnut, metal patina, engravings and pump grooves; watch for mip shimmer.
4. Fight with several portals/enemies, shotgun and stars: check sustained headset frame timing and texture-memory pressure. Desktop renders and APK validation are not proof of Quest visual quality or frame rate.
