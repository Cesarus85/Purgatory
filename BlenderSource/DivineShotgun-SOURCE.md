# User-supplied pump shotgun

Source supplied by user in `Downloads/Shotgun/Shotgun_VR_Asset_Teil1/game/PumpActionShotgun.glb`, with baked texture maps in Teil2. Original download files are unchanged. This note records provenance, not an independent ownership/license determination for public redistribution.

Inspected in Blender 5.2: separate Shotgun_Body and Shotgun_Pump, Grip at the wrist. Actual muzzle points original +X, contrary to the provided README's -X description. Master totals 57,676 triangles; V19.13 export totals 25,375, preserving separate pump, UVs and baked maps.

Adaptation: 88% grip-centred overall scale (~92 cm length), fore-end shifted 11 cm receiver-ward for ~39 cm free-controller reach; 7.5 cm travel. Unity +Z barrel direction, grip-centred origin, MuzzleSocket, PumpSocket, EjectionSocket. No reverse/mirrored scale for left-handed mode.

`prepare_shotgun_v1913.py` is the reproducible adaptation script; `DivineShotgunV19.blend` the adapted source. Runtime uses two mesh batches, 1K albedo/normal/AO/metal-smoothness maps with ASTC 6x6 Android compression. Original ORM.G roughness is inverted into Unity smoothness alpha; ORM.B metallic becomes red.
