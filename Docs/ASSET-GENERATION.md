# Asset Generation Record

Tool modes: built-in ImageGen (V1/V2) and Blender 5.2 LTS (V3)

## V20 — Katana, Bestiarium und Schnittspuren (2026-09-08)

- Blender 5.2.0 LTS tatsächlich ausgeführt: `BlenderSource/build_katana_v20.py`. Nutzerquelle `Downloads/Katana_VR_Asset/Katana_Szene.blend`, SHA-256 `2d45ed14ae79e199d60b17f289cfb167bc44091ee2315916f802039b7f03ae9e`, unverändert erhalten. Die gelieferten schwarzen Maps wurden nicht übernommen. Aus den prozeduralen Originalmaterialien neu gebacken: 2048² BaseColor, Tangent-Normal, AO, Metallic/Smoothness. Scheide/Präsentationsbühne entfernt, 19.000 Dreiecke, ein Material, Griffursprung null, 70,39 cm Klinge. Änderbare Projektkopie `BlenderSource/MercyKatanaV20.blend`, FBX `Resources/Models/MercyKatanaV20.fbx`. Keine öffentliche Weiterveröffentlichungslizenz für das Nutzeroriginal behauptet.
- `BlenderSource/build_bestiary_v20.py`: CrownedEmberfiendV20 (23.516 Dreiecke, ein SkinnedMesh), ChainPenitentV20 (29.348 Dreiecke, zwei SkinnedMeshes, 24 Knochen), RaggedRiftBatV20 (7.732 Dreiecke, ein SkinnedMesh, 33 Knochen). Eigene Horn-/Rüstungs-/Fesselgeometrie wird am vorhandenen Rig gewichtet; keine unkontrollierten Physikketten. Ausgangsmodelle bleiben unverändert. Bat-Variante vom aktuell gelieferten FBX, einschließlich InvertedBurst, nicht vom veralteten Vier-Clip-Blenderstand. Deren bestehende CC0-Provenienz bleibt in ExternalSource/THIRD_PARTY_LICENSES erhalten.
- Anschließend `BlenderSource/bake_penitent_v20.py`: Rüstung mit eigenem UV-Layout, gebackener Oxid-/Rostfarbe, Oberflächennormalen und AO in 1024². Bei erneuter Bestiarium-Erzeugung diesen zweiten Schritt wieder ausführen. Metallfarbe beim Katana als Emission der tatsächlichen BaseColor backen, damit metallische Flächen nicht durch einen reinen Diffuse-Bake schwarz werden. Blender-Studiobilder und native Unity-Materialansichten wurden separat erzeugt und gesichtet.
- `Tools/render-v20-audio.py`: sechs eigene deterministisch synthetisierte Katana-Klänge; zwölf Kettenbüßer-Stimmvarianten aus dem bestehenden CinderBrute-Bank mit verändertem Timing/Kettenpartialen. Grundlage des Stimm-Banks: Darsycho, Monster snarls, CC0, siehe `ExternalSource/EnemyAudioV18/SOURCE.md`. Kein neuer Sprecher/keine neue unabhängige Monsteraufnahme behauptet. Vorhandene Revolver-/Shotgunaufnahmen und deren Lautstärken unverändert.
- `KatanaCutV20.shader`: keine Bitmap-/ImageGen-Platzhalter, sondern gerichtete unregelmäßige Schnittdarstellung auf kopierten tatsächlichen Hautdreiecken. Sparse Skinning hält sie an der Animation/Ragdoll; maximal drei pro Oberfläche, 18 insgesamt, acht Sekunden oder Körper-Lebensdauer. Vorhandenes begrenztes Blutablagerungssystem wiederverwendet. Keine Körperzerteilung.
- Maschinenlesbare Blender-/Audio-Nachweise unter `Verification/V20`. Native Unity-Bilder und Geräteabnahme getrennt ausweisen; diese Zahlen sind keine Quest-FPS-Messung.

## V19.0 / remaining A10 art and portal seals

- Blender 5.2 LTS actually executed `BlenderSource/build_cathedral_v19.py`: original Gothic hall, pointed crossing ribs, clustered columns, recessed chapels, ritual altar and rose tracery. `CathedralHallV19.blend` / FBX, 28,350 triangles, four materials, 2048² diffuse/AO bake. Uses the established forge pipeline without rebuilding or overwriting forge/bridge geometry.
- `BlenderSource/build_portal_details_v19.py` derives three original relief frames from the existing original ObsidianRift source: `ForgeRiftV19`, `BridgeRiftV19`, `CathedralRiftV19`. Distinct staples/rivets, broken links and lancet tracery; 27,100 / 28,900 / 27,932 triangles, three batches each. Existing rift color/normal textures reused; validated aperture/outer envelope unchanged.
- Same script exports an original carved `PortalSealV19.blend` / FBX with bronze filigree and cut gemstone. Runtime pulse, piece breakup and materials are bounded; no decal on target triggers. No new downloaded/licensed third-party assets or ImageGen pictures.
- Cathedral ambience reuses the existing curved-petal mesh recipe with a 24-particle cap, not the pickup-award logic. No new realtime lights. Native Unity images and geometry reports: `Verification/PortalSealing`; source/rendered detail and physical headset acceptance remain separate.

## V18.14 / A9 – original relics

- Blender 5.2 LTS, `BlenderSource/build_relics_v18_14.py` and editable `RelicsV18_14.blend`: forged six-cartridge crescent and organic bone-clasped life vessel. Exported `AmmoRelicV18_14.fbx` (5,694 triangles) and `HealthRelicV18_14.fbx` (3,208 triangles), four material slots each, one shared 1024² color-only baked `RelicsV18_14_Albedo.png`. Original geometry/procedural material work; no new third-party model download.
- FBX roundtrip geometry and bounds recorded in `Verification/Relics/art-geometry.json`; Blender previews there are separate from the native Unity previews and headset acceptance.
- Runtime pickup fragments are tiny forged tetrahedra and curved soul petals generated as reusable meshes. No bitmap sprite, ImageGen or stretched blue beam used for A9 collection effects. Stereo and Meta environment-depth shader variants retained.
- Ammo pickup reuses the existing licensed V18.9 revolver-load bank documented in `THIRD_PARTY_NOTICES.md`. Life pickup is an original short deterministic breath/resonance signal generated once during preload by `RelicEffects.HealthSound`; it does not use a combat hit sound.

## V16 portal depth and combat audio

Current changes: [REVIEW-V16.md](REVIEW-V16.md).

- Blender 5.2 LTS: new `PortalThresholdV16.blend` / FBX, with fractured stone slabs, inlaid runes, forged posts and individually linked hanging chains. Three material batches, 6804 source polygons; reproducible `build_portal_threshold_v16.py`.
- Blender repair: `InfernalWorldV16.blend` / FBX fixes reversed winding on terrain, lava river and waterfall (6490 polygons). Seven material batches retained; reproducible `repair_world_v16.py`.
- Twenty combat WAVs mixed from Kenney CC0 source assets by `ExternalSource/KenneyAudioV16/mix_combat.py`; source notices in `THIRD_PARTY_LICENSES/KenneyAudioV16/`.
- Ten Unity/Metal verification images in `Previews/V16/`, including separate eye textures and a closer side view into the portal. No headset-stereo quality claim from desktop pictures.

## V15 review and corrected runtime assets

Previous changes and verification: [REVIEW-V15.md](REVIEW-V15.md).

- Blender repair/conversion: `BlenderSource/review_gun_v15.py`, producing `HellcasterChallengerV15.blend` and `Models/HellcasterPistolV15.fbx`. Removes the inflated outline and floating accessories, shortens the stock, aligns the grip and forward muzzle socket.
- Original Bat color/detail/normal textures now explicitly loaded from `Resources/Art/BatV15`; visual orientation corrected so its head leads the flight.
- MR PBR shader preserves normal mapping, metal/AO textures and emission. Surface-bound wounds follow exact animated vertices instead of bone colliders.
- Seven real Unity verification images: `Previews/V15/`. These are desktop renders, not headset screenshots.

Older entries below document previous asset iterations, not current verification status.

## V13 Rift Bat

- `Assets/QuestDemonMR/Resources/Models/InfernalBatAnimatedV13.fbx` – Quest-ready flying enemy with separate `Idle`, `Fly`, `Attack` and `Death` takes
- `BlenderSource/InfernalBatV13.blend` – editable Blender derivative
- `BlenderSource/build_infernal_bat_v13.py` – reproducible conversion, material, preview and FBX-export script
- `ExternalSource/VampireBatCC0` – retained CC0 source model and authored textures
- `Previews/infernal-bat-v13.png` – Blender verification rendering

The organic base mesh, UVs, armature and four animations are from rubberduck's CC0 “Vampire Bat (Animated)” asset. Blender isolates the animated production mesh, creates the infernal surface treatment and bone-attached details, and exports the actions without replacing them with procedural rigid-part motion. Unity supplies 3D room-aware steering, animated orbit flight, evasive altitude changes, swoop attacks, per-bone shot hitboxes and a tumbling fall on death.

Outputs:

- `Assets/QuestDemonMR/Art/Concept/emberfiend-concept-v1.png` – frühe Designreferenz
- `Assets/QuestDemonMR/Resources/Art/emberfiend-sprite-v2.png` – aktuelles Produktionssprite
- `Assets/QuestDemonMR/Resources/Models/EmberfiendAnimated.fbx` – aktuelles geriggtes Produktionsmodell
- `Assets/QuestDemonMR/Resources/Models/HellcasterPistol.fbx` – aktuelles Pistolenmodell
- `BlenderSource/EmberfiendAnimated.blend` und `BlenderSource/HellcasterPistol.blend` – editierbare Quellen
- `Previews/emberfiend-v3.png` und `Previews/hellcaster-pistol-v3.png` – geprüfte Blender-Renderings

V3 lädt die beiden FBX-Modelle. Das Emberfiend-Rig enthält eine Master-Timeline, die Unity in `Idle` (1–30), `Walk` (31–70), `Attack` (71–100) und `Death` (101–140) aufteilt. V2 und `PixelArtFactory.cs` bleiben nur als Lade-Fallback erhalten.

## Final prompt

### Production sprite V2

```text
Use case: stylized-concept
Asset type: final in-game billboard sprite for a Meta Quest 3 mixed-reality shooter
Primary request: Create one completely original, intimidating pixel-art demon called the Emberfiend, readable at 2 to 6 meters in a headset. This is a finished production sprite, not concept art, and must not resemble any DOOM monster or other copyrighted character.
Scene/backdrop: genuine transparent alpha background everywhere outside the character; no colored rectangle, vignette, floor, shadow, smoke, scenery, aura, or background glow
Subject: full-body front-facing bipedal volcanic gargoyle; tall broad silhouette; charcoal-black cracked stone armor/skin; hot orange magma fissures; angular obsidian crown horns sweeping outward; luminous cyan eyes; heavy forearms with four-clawed hands; digitigrade legs; aggressive combat-ready stance; mouth closed; no weapon
Style/medium: premium hand-authored 1990s shooter pixel art upgraded for modern XR; chunky deliberate pixels; strong clusters and crisp stepped edges; detailed 128x192 sprite aesthetic; limited 20-color palette; hard alpha-cut silhouette; no antialiasing, no gradients, no painterly brushwork
Composition/framing: exactly one character centered; orthographic front view; full body fully visible; symmetric readable pose with slight organic asymmetry; tight but safe transparent padding; portrait canvas
Lighting/mood: high contrast internal ember light only; sinister and powerful; silhouette remains readable against both bright and dark real rooms
Constraints: actual transparent PNG alpha outside the silhouette; exactly one pose and one character; no sprite-sheet grid; no text; no logo; no watermark; no gore; original design only; every visible nontransparent pixel belongs to the character
Avoid: DOOM likeness, imp likeness, cacodemon likeness, horns or face copied from existing games, photorealism, 3D render, smooth illustration, soft edges, bloom outside silhouette, ground plane, decorative frame, background of any kind
```

### Concept V1

```text
Use case: stylized-concept
Asset type: production game enemy sprite for a Meta Quest mixed-reality shooter
Primary request: Create one completely original pixel-art demon enemy, suitable as a front-facing billboard sprite in a 1990s-inspired shooter. It must not depict or closely resemble any existing DOOM monster or other copyrighted game character.
Scene/backdrop: fully transparent background, no floor, no shadow, no environment
Subject: a stocky ash-black and ember-orange imp with asymmetrical stone horns, glowing cyan eyes, broad readable silhouette, both arms visible, standing attack-ready and facing straight toward camera
Style/medium: crisp hand-crafted low-resolution pixel art, chunky 64x96-era sprite aesthetic, hard pixel edges, limited 12-color palette, no antialiasing, no gradients
Composition/framing: single full-body character centered, generous transparent padding, orthographic front view
Lighting/mood: sinister but readable, ember cracks as highlights
Constraints: genuinely transparent alpha background; exactly one character; full body entirely visible; no text; no logo; no watermark; no gore; original design only
Avoid: DOOM character likeness, photorealism, smooth painting, 3D render, ground plane, scenery, weapons, multiple poses or sprite-sheet grid
```
