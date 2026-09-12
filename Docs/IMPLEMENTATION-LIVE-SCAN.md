# V18 — live spatial reconstruction

User-authorized functional milestone, 5 September 2026. V17 long-run profiling is deferred, not passed. Preserve the V17.2 APK. Quality parity with Laser Tag is a device acceptance target, not a claim from compilation.

## Design

Independent implementation on the installed Meta Depth API, Unity 6 / Vulkan. No Laser Tag source is incorporated (current upstream license is PolyForm Noncommercial).

1. Capture depth texture and matching world-to-depth reprojection together. Discover observed chunks from a small GPU point readback. Integrate a bounded sparse projective signed-distance field on the GPU, including positively observed free space and repeated measurements. Unknown and behind-surface samples remain unknown.
2. Extract interpolated surfaces from confirmed samples, in small chunks. Mesh construction runs off the main thread; collider/renderer replacement is bounded on the main thread. Retain geometry outside the current view. Repeated new observations can remove moved furniture. No cloud upload, camera-image storage or room-scan persistence.
3. New live mode uses only reconstructed physical geometry: no automatic saved-room colliders or saved-room spawn vetoes. Unknown space cannot authorize a spawn. Derive ground support and ceilings from geometry. Keep the old build as recovery; do not silently fall back to an invented arena on hardware.
4. Connect navigation, flight, falling bodies, bullets, fireballs, ground and ceiling portals to the same physical map. Preview the actual collision mesh; expose preparation/coverage status and manual rescan while paused.
5. Compile, exercise geometry invariants and build a versioned APK. Full room quality, moved-furniture latency, headset lifecycle and thermal checks remain device gates. Do not call those passed without testing.

## Constraints

Bound voxel memory, pending GPU readbacks, mesh worker count and per-frame collider commits. Use sensor poses, not the current render-camera projection. Do not fuse invalid depth, outside-frustum space, or samples behind the measured surface. No boundary bypass. Tracking discontinuity invalidates the session map and pauses gameplay. Scanner may continue while the game is paused; app focus/pause stops acquisition.

## Primary references

- https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview/
- https://developers.meta.com/horizon/documentation/unity/unity-depthapi-api-reference/
- Installed SDK: EnvironmentDepthManager / EnvironmentDepthUtils and EnvironmentOcclusion.cginc, v205.
- https://github.com/anaglyphs/lasertag (architecture reference only; no source copied).
