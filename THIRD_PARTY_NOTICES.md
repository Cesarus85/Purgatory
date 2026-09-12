# Third-party art notices

## Enemy voices and flight/footstep audio (A6 / V18.8)

- Darsycho, [Monster snarls](https://opengameart.org/content/monster-snarls), **CC0**: growl/attack recordings edited into three variants each of idle, attack, hurt and death for four archetypes. Distinct processing of shared recordings, not four unrelated source packs.
- AntumDeluge, [Large Wings Flap](https://opengameart.org/content/large-wings-flap), **CC0**: three shortened, filtered wing-air transients. Archive README credits original chop foley by dave.des, https://freesound.org/s/127197/ .
- Existing Kenney [Impact Sounds](https://kenney.nl/assets/impact-sounds), **CC0**: three processed concrete-footstep recordings. No room-material recognition is claimed.
- Original downloads, hashes, README and reproducible processing: `ExternalSource/EnemyAudioV18/SOURCE.md`, `mix_enemies.py`. Previous A5 revolver/impact recordings are unchanged.

## The Free Firearm Sound Library / A5 combat audio (V18.7)

A6b / V18.9 derives a new bank from these retained sources: shared real-revolver attack, matched body dynamics, differentiated impact filtering and 38-ms audio-only contact predelay. Mechanical samples remain unchanged. No additional source packs or licenses. Exact processing and outputs: `ExternalSource/FirearmAudioV18/SOURCE-A6b.md`, `mix_a6b.py`.

- Authors: Ben Jaszczak, Brian Nelson, Kevin Heras, Matthew Nanney; hosted by OpenGameArt, submitted by bart.
- Source: https://opengameart.org/content/the-free-firearm-sound-library ; license: CC0.
- Used: Smith & Wesson 642 `V_27P.wav`, `V_22P.wav`, Ruger Single Six `S_14P.wav` recorded gun reports. Mono downmix, source segments, filtering, decay shaping and peak caps; no endorsement or weapon-brand affiliation implied for the fictional Ashwarden prop.
- Additional retained Kenney CC0 Impact Sounds supply layered mechanism/flesh/hard-surface/kill foley. These are designed cues, not recordings of the Ashwarden's fictional mechanism.
- Archive hash, exact segments and reproducible mixer: `ExternalSource/FirearmAudioV18/SOURCE.md`, `mix_revolver.py`. Original files remain outside Unity Resources. Previous sounds and their credits are retained.

## Original Ashwarden revolver V18.6

`BlenderSource/AshwardenRevolverV18_6.blend`, its FBX, baked patina atlas and `build_revolver_v18_6.py` are original work for this project. No external revolver model, brand replica, texture pack or purchased asset was incorporated. The new muzzle effect reuses the original V18.5 fire bake plus a code-authored smoke shader. Existing audio remains transitional until A5 and retains its credits below. Previous licensed pistol assets remain preserved; this replacement does not remove their notices.

## Kenney combat audio (V16)

- Author: Kenney (https://kenney.nl)
- Sources: https://kenney.nl/assets/sci-fi-sounds and https://kenney.nl/assets/impact-sounds
- License: Creative Commons Zero (CC0). Original notices in `THIRD_PARTY_LICENSES/KenneyAudioV16/`.
- Used source families: explosionCrunch, laserLarge, impactPunch_heavy, impactMetal_light and impactMetal_medium (variants 000–004).
- Changes: resampling/pitch adjustment, transient and tail shaping, layering, DC removal, fade-out and peak limiting. Twenty mono PCM variations for gunshots, flesh hits, hard-surface hits and kills.
- Reproduction: `ExternalSource/KenneyAudioV16/mix_combat.py`; original archives and source audio retained alongside it.


## Eldritch Portal

- Author: phantasmaCora
- Source: https://opengameart.org/content/eldritch-portal-model-with-pbr-textures
- License used: Creative Commons Attribution 4.0 International (CC BY 4.0)
- Changes: duplicated and mirrored into a full ring, reoriented, rescaled, decimated for Meta Quest, combined with original connector geometry, and assigned a Unity runtime PBR material.

## Original projectile effect bakes V18.5

`BlenderSource/InfernalFire.blend` and `IonPlasma.blend` are original procedural volume/filament scenes authored for this project. `build_projectiles_v18_5.py` bakes their animated RGBA atlases. No external images, textures, fire simulations or third-party projectile packages were incorporated. This entry does not change the licenses of the separately credited creature models.

## Stormhyde Demon

V18.4 derivative: `BlenderSource/RiftStalkerV18_4.blend` repairs facial/skull skin weights and replaces melee/cast animation ranges. Original source/license below remain unchanged. The production FBX retains its V12 resource path. Reproducible conversion: `BlenderSource/build_combat_v18_4.py`.

- Author: Hayden Barnett / Stormhyde
- Source: https://www.stormhyde.com/demon
- License statement at source: free for personal and commercial use.
- Changes: mesh narrowed and re-posed, new Blender armature and skin binding, five original animation sequences, Unity FBX export, and new material setup.

## Derringer Pistol

- Author: LonesomeDucky
- Source: https://opengameart.org/content/derringer-pistol
- License: CC0 1.0 Universal.
- Changes: low-poly PBR core combined with an original Hellcaster receiver, barrel sleeve, energy coils, sights, rails, grip armor, magazine/power cell, and emissive details.

## Sci-Fi Weapon - Challenger (V9 / V15 pistol)

V15 reuses the existing source: removed the incorrectly scaled inverted outline modifier, removed floating prototype accessories, shortened the rear stock, adjusted grip proportions/orientation, and added an authored muzzle socket. Reproducible Blender conversion: `BlenderSource/review_gun_v15.py`.

- Author: A1MERCet
- Source: https://a1mercet.itch.io/sci-fi-challenger
- License: CC0 1.0 Universal.
- Changes: removed the source presentation scene, normalized the weapon to Unity/Quest scale and +Z muzzle axis, consolidated the authored PBR body, added a demonic power core, slide, magazine base and muzzle prongs, and downscaled the packed PBR maps to 2K ASTC-ready textures.

## Infernal dimension artwork

- Original image generated for QuestDemonMR with OpenAI ImageGen on 2026-09-04.
- Changes: used through a custom animated oval portal shader with parallax offset, edge distortion, and emissive rift treatment.

## Infernal World V10

- Original 3D environment modeled procedurally in Blender for QuestDemonMR on 2026-09-04.
- Content: continuous ravine terrain, modeled lava river and waterfall, broken sacrificial bridge, three successive titan-rib arches, hanging soul cages, foreground parallax cliffs, floating islands, crystals, two moons and a multi-layer citadel.
- Runtime: rendered through dedicated left/right eye cameras into each portal, with head parallax, procedural flowing lava, rock fissures and depth embers; source objects are merged into seven material-batched meshes for Quest performance.

## Quest Portal Example technique reference

- Author: Bart Trzynadlowski.
- Source: https://github.com/trzy/Quest-Portal-Example
- License: MIT License.
- Use in this project: the stereo-camera concept was adapted for two runtime render textures, late per-eye pose/projection synchronization and a Quest single-pass-instanced portal surface. No source art or prefabs are included.
- Full license text: `THIRD_PARTY_LICENSES/Quest-Portal-Example-MIT.txt`.

## Abyssal Skull Portal V12

- Original procedural Blender model created for QuestDemonMR on 2026-09-04.
- Content: asymmetric obsidian segments, horned skull keystone, bone horns, emissive rune inlays, chains and wall-root tendrils.
- Runtime treatment: broken energy tendrils, wall fractures, smoke suction and the existing per-eye 3D portal cameras replace the former circular line rings.

## Ceiling Reaver V12

- Original Blender derivative created for QuestDemonMR on 2026-09-04 from the credited Stormhyde Demon base.
- Changes: additional chitin thorax and abdomen, four scythe appendages, mandibles, crown spikes, six-eye cluster, venom seams, ceiling-specific proportions and shared retargeted V12 animation set.

## Vampire Bat / Rift Bat V13

V18.4 derivative: `BlenderSource/InfernalBatV18_4.blend` adds a weighted abdominal/pelvic articulation and an authored talon-first U-shaped attack. Original author/license below remain unchanged. Production FBX keeps its V13 resource path for importer/GUID stability. Reproducible conversion: `BlenderSource/build_combat_v18_4.py`.

- Author: rubberduck.
- Source: https://opengameart.org/content/vampire-bat-animated
- License: CC0 1.0 Universal.
- Source content used: textured, rigged `Bat_LP_Anim` mesh and the authored `Bat_Flying`, `Bat_Attack`, `Bat_Idle` and `Bat_Die` actions.
- Changes: isolated and renamed the organic production rig, removed presentation/frost variants, converted and repacked textures, created charred infernal materials, exported four optimized Unity Legacy animation takes, and implemented original spatial flight, obstacle avoidance, orbit, swoop attack and falling-death behavior. No procedural replacement geometry is used for the creature.
- Editable derivative: `BlenderSource/InfernalBatV13.blend`; unmodified source and textures retained under `ExternalSource/VampireBatCC0`.
