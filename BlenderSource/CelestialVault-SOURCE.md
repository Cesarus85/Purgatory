# Celestial Vault V19.14

Original procedural Blender model authored for Purgatory in this workspace.
No downloaded sky photograph, commercial environment or flattened background.

- Generator: `build_heaven_v1914.py`, deterministic seed 1914.
- Editable scene: `CelestialVaultV19.blend`.
- Runtime export: `Resources/Models/CelestialVaultV19.fbx`.
- Four fused/displaced cumulus banks at several heights; distant sun. 52,728
  exported triangles. The optional 768-triangle radial ornament is hidden at
  runtime, where atmospheric scattering supplies the light-source halo.
- Normals and geometric depth, plus code-authored cloud/atmosphere shaders.
- Blender 5.2.0 LTS, headless actual generation/export run on 2026-09-08.
- Unity uses two separately projected eye cameras; the mesh is not baked to an
  image. Native eye/lean previews are in `Verification/Immersion`.

The original V19.13 shotgun source model and its texture provenance are unchanged.
