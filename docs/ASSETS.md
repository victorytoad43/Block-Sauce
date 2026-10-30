# Block Sauce: asset requests

The agent adds requests here at the start of every phase (plus look-ahead for the next phase and long-lead items). Jackson works from the tracker, drops files in `Assets/Art/_Incoming/`, and sets the status to `Delivered`. The agent integrates and sets `Integrated`, or `Needs changes` with a note.

**Statuses:** `Requested` → `In progress` (Jackson) → `Delivered` (Jackson) → `Integrated` | `Needs changes` (agent)
**Priority:** `Start now` (long lead, start immediately) · `Needed soon` (needed by the phase in progress) · `Next phase` · `Nice to have`

## Tracker

| ID | Asset | Needed by | Priority | Status | Placeholder in use |
|---|---|---|---|---|---|
| A-001 | Player color palette (8 colors) | Phase 2 | Needed soon | Requested | `PH_PlayerColors` |
| A-002 | Lobby + main menu UI mock-ups | Phase 1 | Next phase | Requested | Default UI |
| A-003 | Guy character model + rig | Phase 0 (import test) | Needed soon | Integrated | Guy on `PH_PlayerCapsule` (capsule collider stays) |
| A-004 | Guy animation set (first pass) | Phase 4 (walk needed soon for Phase 0) | Start now | In progress | None |
| A-005 | Block mesh | Draft for Phase 1; final by Phase 5 | Needed soon | Requested (draft seen) | `PH_Block` |
| A-006 | Match HUD + results screen mock-ups | Phase 2 | Needed soon | Requested | IMGUI text HUD (`MatchHud`) |
| A-007 | Settle/count effect art direction | Phase 2 | Needed soon | Requested | `PH_LockDither` shader |
| A-008 | Arena layout sketch | Phase 6 (graybox from Phase 0) | Nice to have | Requested | Graybox terrain |
| A-009 | Sauce vat model + sauce look references | Phase 5 | Nice to have | Requested | Cylinder |
| A-010 | Block Sauce logo / wordmark | Phase 6 | Nice to have | Requested | Text |
| A-011 | Movement feel targets (design input) | Phase 1 | Needed soon | Requested | Friendslop defaults |
| A-012 | Guy hold-block pose + throw timing | Phase 1 timings; Phase 4 clip | Start now | Requested | None |
| A-013 | Block owner tint and team readability | Phase 2 | Needed soon | Requested | Flat owner color on `PH_Block` |
| A-014 | SFX placeholder list | Phase 6 (temp SFX any time) | Nice to have | Requested | Silent |
| A-015 | Knockdown + get-up clips (slice of A-004) | Phase 3 | Needed soon | Requested | Snap to standing with a squash-and-stretch pop |
| A-016 | Knockdown readability (dazed indicator) | Phase 3 (final art Phase 6) | Nice to have | Requested | None |

## Request format (agent: use this for every new request)

```
### A-0XX: <name>
- Phase / needed by: <phase>, <milestone it unblocks>
- Priority: <Start now | Needed soon | Next phase | Nice to have>
- Purpose: <what it's for in the game, one or two lines>
- Spec: <dimensions, pivot, poly or texture budget, clip lengths, states, file format>
- Must match: <placeholder measurements, timings or code assumptions the art must fit>
- Placeholder: <PH_ asset name and path>
- Status: Requested
- Notes: <integration notes, change requests>
```

## Pipeline spec

**Drop zone:** `Assets/Art/_Incoming/` is the only place Jackson puts files, under any file name. The agent renames each file to the naming convention below, moves it to `Assets/Art/<Category>/`, and, for a re-export of something already in the game, copies it over the existing asset so every reference keeps working. Import settings are applied automatically by `Assets/Scripts/Editor/ArtImportPostprocessor.cs` for everything under `Assets/Art/`.

**Source files:** Jackson's Blender files live in `C:/Users/victo/Desktop/Games/BlockSauce/modelling/` (outside the repo). Only exported FBX/PNG files enter the project, through the drop zone.

**Naming:** `<Prefix>_<Name>[_<Variant>]`. Prefixes: `CH_` character, `BL_` block, `EN_` environment, `UI_`, `FX_`, `SFX_`, `MUS_`. The character is `CH_Guy`; animation clips are `CH_Guy@<ClipName>.fbx` (Unity's `@` convention).

**Scale:** Jackson authors at about 2.5× real-world scale (Guy is 4.15 m tall in Blender; the draft block is 2 m). Physics must run at real-world scale, so every FBX from him is imported with **Scale Factor 0.4** (confirmed by Jackson 2026-10-28). Keep all Blender art at this same authoring scale so one factor covers everything. The import postprocessor sets this for everything under `Assets/Art/`.

**Models (Blender → FBX):**
- Metric units, unit scale 1.0, object transforms applied, object origin at the intended pivot.
- Characters face -Y in Blender (Guy does). Export with Forward: -Z, Up: Y, Apply Unit on, Apply Scalings: FBX All.
- One material per logical surface; name materials the way they should appear in Unity.

**Characters:**
- **Generic** rig. A `root` bone at the origin; keep bone scale animatable (squash and stretch).
- Export the armature and the character mesh only: **Only Deform Bones** on, **Add Leaf Bones** off, and leave out the `WGT_*` widget objects and any draft props in the file.
- Easiest setup (Blender 4.2+): put `armature_guy` and `mesh_guy` in their own collection, add an FBX exporter to it in Collection Properties > Exporters, point it at `Assets/Art/_Incoming/`, and set it once: Object Types Armature + Mesh; Apply Scalings: FBX All; Forward -Z, Up Y; Apply Modifiers on; Only Deform Bones on; Add Leaf Bones off; Bake Animation on with All Actions on, NLA Strips off, Force Start/End Keying on. The settings are saved in the .blend, and every later export is one click.
- Mesh, skeleton and all actions in one file (decided 2026-10-28). Action names like `Guy.WalkBasic` are fine; the import strips the prefixes. Clips whose names contain Idle, Walk, Run, Loop, Pose or Hold are set to loop.
- A single-pose action (aim pose, hold pose) needs **at least two keyframes** (the same pose on frames 0 and 1); a one-keyframe action exports with zero length and imports empty.
- No root motion in clips: animate in place. The simulation moves the body.
- Loops must loop cleanly; first and last frames match.
- Clips are authored at 24 fps; that's fine (Unity resamples, and the simulation runs at 64 Hz independently).
- Give every action you want to keep a fake user in Blender, so it isn't lost on save.

**Textures:** PNG, power-of-two sizes. Color maps sRGB; masks, roughness and normals linear (agent sets this on import).

**UI:** Mock-ups as PNG or a Figma link in the request's notes. Final UI art as PNG at 2× with 9-slice margins noted; fonts as TTF/OTF with license confirmed.

**Audio (Phase 6):** WAV 48 kHz; mono for 3D sounds.

## Requests

### A-001: Player color palette
- Phase / needed by: Phase 2, block ownership colors and the settle dither
- Priority: Next phase
- Purpose: Each player's blocks, character tint and HUD use one color. Sauces must stay readable on top of every one.
- Spec: 8 colors as hex, each with a darker shade for the settle dither and a lighter shade for highlights. Distinguishable for common color-vision deficiencies and on both light and dark terrain.
- Must match: Leaves room for sauce materials (A-009) to read by material, not hue.
- Placeholder: Unity default colors in `PH_PlayerColors` config
- Status: Requested

### A-002: Lobby + main menu UI mock-ups
- Phase / needed by: Phase 1 (sandbox lobby), final art Phase 6
- Priority: Next phase
- Purpose: Main menu (play, settings, quit), create/join room, lobby overlay (player list with colors, ready state, start).
- Spec: Mock-ups at 1920×1080; controller-navigable focus states shown.
- Must match: Agent lists screens and fields here once the lobby flow exists.
- Placeholder: Default UI
- Status: Requested

### A-003: Guy character model + rig
- Phase / needed by: Phase 0 import test; full use in Phase 4
- Priority: Needed soon
- Purpose: The single player character, tinted per player color.
- Current state (read from `modelling/guy/guy.blend`, Blender 5.1, 2026-10-28):
  - Mesh `Sphere`, 1,482 verts, subdivision surface on top, one material (`Material.001`), UVs present, 29 vertex groups. Rename to `CH_Guy` / `M_Guy` on export.
  - Armature: 100 bones, 31 deform bones (`root`, `hips`, `spine`, `chest`, `head`, `mouth`, `mouth_upper`, `jaw`, `eye.L/R`, `shoulder`/`upper_arm`/`forearm`/`hand` `.L/R`, `pelvis`/`thigh`/`shin`/`foot`/`heel`/`toe` `.L/R`, `prop`). Control rig separated into `CTRL_*` and `MCH_*` bones driving the deform bones: IK/FK arms and legs, foot roll, stretch, squash with maintain volume, look target, eye tracking. This already meets the spec below.
  - Height 4.15 m at authoring scale (about 1.66 m in game at factor 0.4).
- Spec: keep the deform hierarchy as is; it maps directly onto Quantum's ragdoll (`hips`, `chest`, `head`, arm and leg segments).
- Must match: the subdivision modifier should be applied or set to export at the level you want in game; budget is fine either way for 8 players. The `prop` bone is the hold point for blocks; keep it.
- Phase 0 export needed now: `CH_Guy.fbx` (mesh + deform skeleton, per the Characters spec above) dropped in `Assets/Art/_Incoming/`. Either Jackson exports it, or Jackson approves the agent running a headless Blender export that writes only the FBX and never saves `guy.blend`.
- Placeholder: `PH_PlayerCapsule` (agent sizes it from Guy's measurements in Phase 0). Until the import test, the capsule is 1.66 m tall with a 0.35 m radius, based on 4.15 m × 0.4.
- Status: Integrated
- 2026-10-29 re-export (collection exporter): integrated. Mesh 8,758 verts with subdivision applied, 31 deform bones, no leaf bones, scale 1 on every node. At game scale Guy is **1.62 m tall**; his body is about **0.40 m in radius** (0.41 at the belly, feet reach 0.49). Guy now renders on the player prefab, with the capsule and facing marker hidden, and walks in play mode. The physics capsule stays 0.35 m × 1.66 m for now, so Guy overlaps walls and blocks by about 5 cm; widening it changes the reach Phase 1 was tuned at, so it waits for Phase 4.
  - Small cleanups for the next export: the draft vat (`Circle`) and block (`Cube`) are in the exporter's collection and came along (the agent hides them in game). Move them to their own collection, ideally with their own exporters for A-005 and A-009. Nice to have: rename `Material.001` to `M_Guy`.
- 2026-10-29 first delivery (`guy.fbx`, now `Assets/Art/Characters/CH_Guy.fbx`): the rig imports correctly but **the mesh is missing** (the FBX holds only the armature). Re-export with `mesh_guy` included (Object Types: Armature + Mesh, and Selected Objects off or the mesh selected too). Also: the export has all 100 bones plus `_end` leaf bones; turn on Only Deform Bones and turn off Add Leaf Bones. Measured at game scale: faces +Z, `prop` bone 0.838 m up and 0.840 m forward of the root (matches the simulation's hold pivot), `head` bone 1.17 m. Full height and capsule radius get measured from the mesh on the re-export.
- Notes: Placeholder `Assets/Placeholders/PH_PlayerCapsule.prefab` is in use: height 1.66 m, radius 0.35 m, origin at the feet. The radius is a guess; the import test replaces it with Guy's measured width. (agent: record game-scale height, capsule radius and `prop` offset here after the import test.)

### A-004: Guy animation set (first pass)
- Phase / needed by: Phase 4; long lead, start now
- Priority: Start now
- Purpose: Locomotion and actions layered over the physics body.
- Spec (all in place, no root motion):
  - Locomotion: idle, walk and run (forward, back, strafe left, strafe right), crouch idle, crouch walk
  - Air: jump start, airborne loop, land
  - Slide: enter, loop, exit
  - Upper-body layer (used with an avatar mask): hold-block idle pose, grab, place/stack, throw, jumpshot throw
  - Reactions: stagger flinches (front, back, left, right), get-up from face-down and from face-up
  - Round end: win, lose
- Must match: Hands on the held block are IK, so the hold pose only needs to be close. Phase 1 timings (seconds at game speed):
  - Slide: 0.75 s, starting at 7.5 m/s.
  - Jump: about 0.4 s up and 0.25 s down (about 1 m peak).
  - Throw: no release delay yet (the block leaves on the press).
  - Stagger: 0.25–1.2 s depending on the hit.
  - Walk 3 m/s, run 6 m/s, crouch-walk 1.5 m/s. Locomotion cycles should match these speeds without foot sliding.
- Placeholder: None (capsule)
- Status: In progress
- Notes: the file has three actions named `Guy.WalkBasic`, `.001` and `.002`; only `.002` has keyframes (frames 0–96) and it has no fake user. 2026-10-28: Jackson says none of these is a real walk yet; animation work continues. **Delivery format: one animated FBX** (`CH_Guy.fbx` with mesh, deform skeleton and all actions, using clean action names). The Phase 0 import test (height, `prop` bone, squash-and-stretch bone scale, a clip playing in place) runs when the first export lands in `_Incoming`. Until then, Phase 0 uses the capsule at the confirmed 1.66 m.
- 2026-10-29 delivery, four actions in `CH_Guy.fbx`:
  - `WalkBasic` (48 frames, 2 s): imports and loops cleanly (seam 2.8°), animated in place, squash-and-stretch scale curves come through. Its planted foot moves backward at about 0.3 m/s at game scale, but the game walks at 3 m/s, so the feet would slide about 10×. For no sliding, a planted foot must move back about 0.31 m per frame in Blender units (3 m/s × 2.5 ÷ 24 fps) for walk, and 0.63 m per frame for run (6 m/s). If strides that long look wrong on Guy, the game's walk speed can come down instead; decide when tuning locomotion in Phase 4.
  - `Throw` (7 frames, 0.29 s): imports fine. The block leaves on **frame 5** (0.21 s), per Jackson.
  - `RunBasic` and `ThrowPose`: one keyframe each, so they export with zero length and import empty. Key them on at least two frames. (Still one keyframe in the 2026-10-29 re-export.)
  - In game (2026-10-29): `AC_Guy` plays `WalkBasic` whenever Guy moves, sped up to match ground speed (its stride matches about 0.3 m/s) up to 4×, and the rest pose when standing still. Per Jackson, the walk is for slow movement (low stick pressure) and a Run clip covers faster movement; the walk can be sped up if needed. Until `RunBasic` has keys, the walk plays at 4× above 1.2 m/s and the feet slide.

### A-005: Block mesh
- Phase / needed by: draft now (Phase 1 tunes grab, swing and stacking at its size); final topology by Phase 5 for sauce displacement
- Priority: Needed soon
- Purpose: The stackable block.
- Spec: Beveled cube with several even subdivision loops per face so the sauce shader can displace it; UVs unwrapped; pivot at the center.
- Must match: Draft block is 2 m at authoring scale (0.8 m in game). The agent records the final block size and mass here after Phase 1 tuning.
- Placeholder: Unity cube, `PH_Block`
- Status: Requested
- Phase 1 result: the game uses a 0.8 m block (2 m at authoring scale) with mass 2; grab range, hold radius and stacking were tuned at that size. Keep the final block at 2 m in Blender, pivot at the center.
- Notes: `guy.blend` contains a `Cube` (2 m, 104 verts, geometry nodes modifier) that looks like a block draft. If so, export it as `BL_Block.fbx` with the geometry nodes applied. An early draft export helps Phase 1: grab, swing and stacking get tuned at the block's real size, so a size change after Phase 1 means retuning.

### A-006: Match HUD + results screen mock-ups
- Phase / needed by: Phase 2
- Priority: Next phase
- Purpose: Countdown, each player's tower progress (3 slots: empty, settling, counted), round end and results.
- Spec: Mock-ups at 1920×1080 for 2 and 8 players.
- Must match (Phase 2 build): match states are Lobby (press Start / Enter to begin), Countdown (3 s), Playing, Round end (3 s, winner shown), Results (6 s), then back to Lobby. Per player the HUD shows their color and the 3 slots of their best stack, each empty, settling (0–100%) or counted.
- Placeholder: IMGUI text HUD, `Assets/Scripts/Match/MatchHud.cs`
- Status: Requested

### A-007: Settle/count effect art direction
- Phase / needed by: Phase 2
- Priority: Next phase
- Purpose: The dither overlay climbing a stacked block while it settles, and the "counted" pop when it reaches the top. The agent builds the shader; this is the look to aim for.
- Spec: A few frames or a short reference clip; dither pattern and scale; the pop (flash, ring, particles, sound idea).
- Must match (Phase 2 build, revised 2026-10-29): settle progress is a 0–1 value that fills over 3 s (mode setting) while a stacked block rests, and drops to 0 on any hit; the shader climbs bottom to top with it. A counted block shows the solid darker shade; nothing is frozen, so a counted block can be grabbed or knocked off at any moment and then falls back to the plain owner color. A lone block on the ground never settles.
- Placeholder: `Assets/Placeholders/PH_LockDither.shader` (4×4 screen-space dither in the owner's darker shade; counted = solid darker shade)
- Status: Requested

### A-008: Arena layout sketch
- Phase / needed by: Graybox from Phase 0, final Phase 6
- Priority: Nice to have
- Purpose: The first arena: uneven terrain, room for 8 towers anywhere, dip vats placement (Phase 5).
- Spec: Top-down sketch with rough size, elevation changes and landmarks.
- Placeholder: `Arena_Graybox` scene
- Status: Requested

### A-009: Sauce vat model + sauce look references
- Phase / needed by: Phase 5
- Priority: Nice to have
- Purpose: The pot a block is dipped into, and the look of Stack sauce and Slip sauce.
- Spec: Vat model (pivot at base center, sauce surface as a separate mesh at a known height). For each sauce: color, gloss level, and a material identity cue (bubbles, sparkle, etc.).
- Placeholder: Cylinder, `PH_SauceVat`
- Status: Requested
- Notes: `guy.blend` contains a `Circle` object (2.46 m wide, 1.18 m tall, subdivided) that may be a vat draft. Jackson to confirm.

### A-010: Block Sauce logo / wordmark
- Phase / needed by: Phase 6
- Priority: Nice to have
- Purpose: Title screen and menu header.
- Spec: SVG source plus PNG at 2×, light and dark variants.
- Placeholder: Text
- Status: Requested

### A-011: Movement feel targets (design input)
- Phase / needed by: Phase 1, movement and camera tuning
- Priority: Needed soon
- Purpose: The numbers that make the movement feel like Block Sauce, rather than the agent guessing at game feel.
- Spec: A short list (rough is fine): walk and run speed, how floaty the jump is (rough jump height), whether run is a held button or a stick push, slide length, how far a throw carries, and 1–3 reference games for feel. A sentence each is enough.
- Must match: Simulation timers (slide length, throw release) get sized to the clips in A-004, so state these in seconds if you have them.
- Placeholder: Friendslop's `MovementConfig` defaults, adapted to third person
- Status: Requested

### A-012: Guy hold-block pose + throw timing
- Phase / needed by: Phase 1 sets the hold point and throw timing in the simulation; the clip itself lands in Phase 4
- Priority: Start now (a long-lead slice of A-004)
- Purpose: Where the block sits relative to Guy while held, and when a throw lets go. The simulation's swing orbit and throw release timer are built around these.
- Spec: A static hold-block pose (one frame is enough for now) with the `prop` bone at the block's center. Plus a rough throw: the frame count from start to release and the total length at 24 fps. Blocking-out quality is fine.
- Must match: Block draft size from A-005 (0.8 m in game); the `prop` bone's rest position (about 0.84 m forward and 0.84 m up from the root at game scale) is the current default hold point.
- Phase 1 values in the simulation: the hold pivot is 0.84 m above the feet, and the block's center sits 0.9 m from it, so the hands meet the block about 0.5 m in front of the chest. Throwing is hold-to-aim (decided 2026-10-29). While Throw is held, the block is raised to a ready position (30° above the hold line, centered in front), and the character turns to aim. The block leaves on release at 12 m/s (a jumpshot at 15.6 m/s, while airborne). So the throw needs an aim/hold-ready pose (looping, any length) and a short release (the block leaves on its first frame, unless we add a release delay to match). The jumpshot needs the same pair, airborne.
- 2026-10-29: `ThrowPose` (aim pose) and `Throw` (release on frame 5 of 7, 0.21 s) delivered in `CH_Guy.fbx`; `ThrowPose` still needs a second keyframe to import. In the simulation the block leaves when Throw is released; Phase 4 lines the clip up so frame 5 lands on that moment.
- Placeholder: None (capsule; the block floats at the `prop` offset)
- Status: Requested

### A-013: Block owner tint and team readability
- Phase / needed by: Phase 2, when blocks get owners
- Priority: Next phase
- Purpose: Every block shows whose it is at a glance, from across the arena and while tumbling, and later under a sauce coat (A-009).
- Spec: How ownership shows on the block: a full tint, a colored edge or bevel, a stripe or icon, or a combination. One reference image or a quick paint-over of the A-005 draft is enough.
- Must match: The player palette in A-001. The settle dither (A-007) climbs the block in the owner's darker shade, so the base treatment must leave room for that overlay.
- Placeholder: `PH_Block` with a flat owner color (`PH_LockDither` shader, colors from `PH_PlayerColors`); unowned sandbox blocks stay gray
- Status: Requested

### A-014: SFX placeholder list
- Phase / needed by: final audio Phase 6; temporary sounds welcome any time
- Priority: Nice to have
- Purpose: The simulation raises events for jump, grab, drop, throw, block impact (with impact speed) and knockback. The view can play a sound on each as soon as one exists.
- Spec: WAV 48 kHz mono, short. For each event, one sound or a pointer to a free/CC0 pack you like. Impacts benefit from 2-3 variations, plus a light and a heavy version.
- Must match: Event names in code: `Jumped`, `Grabbed`, `Dropped`, `Thrown`, `ObjectImpact` (speed), `KnockedBack`. Phase 2 adds `BlockSettleStarted`, `BlockSettleReset`, `BlockCounted` (the count pop), `BlockUncounted`, and `MatchStateChanged` (countdown beeps, round start, win fanfare). Phase 3 adds a knockdown (thud plus a dazed loop while down), ragdoll limb impacts (soft and heavy), and a get-up.
- Placeholder: Silent
- Status: Requested

### A-015: Knockdown + get-up clips
- Phase / needed by: Phase 3, the get-up after a ragdoll; long lead
- Priority: Needed soon
- Purpose: The slice of A-004 that Phase 3 needs first. When a knocked-down player recovers, the ragdoll is removed and Guy stands up where his hips came to rest; the view blends from the ragdoll pose into a get-up clip.
- Spec: two clips, in place: get up from lying face down, and from lying face up. Start each from a relaxed lying pose (it doesn't have to match any particular ragdoll pose; the view blends into frame 0). About 0.6–1.0 s each at game speed. Same export as Guy (`CH_Guy.fbx`, two or more keyframes per action).
- Must match: the hips end where they started horizontally (no root motion); the last frame matches the idle/stand pose.
- Placeholder: the view snaps Guy back to standing with a short squash-and-stretch pop (Phase 3)
- Status: Requested
- Notes (Phase 3 prep): the ragdoll is built from the deform bones already in `CH_Guy.fbx` (hips, chest, head, upper arm, forearm, thigh, shin; hands and feet ride the last segments), so no new rig work is needed. The get-up starts wherever the ragdoll came to rest: the view will blend from the last ragdoll pose into frame 0 over about 0.15 s, so frame 0 should be a relaxed lying pose, not a precise one. Lying face down = chest facing the ground.

### A-016: Knockdown readability (dazed indicator)
- Phase / needed by: Phase 3 placeholder, final look Phase 6
- Priority: Nice to have
- Purpose: Players need to see at a glance who is down and roughly how long they have left, from across the arena and in a pile of bodies and blocks.
- Spec: a small effect over a downed player's head (circling stars, a swirl, sweat drops, whatever fits Guy). A sketch or reference is enough; the agent builds it. Optionally a way to show time left (shrinking ring, fewer stars).
- Must match: knockdown lasts a few seconds (first guess 2.5 s, Jackson to confirm); the effect follows the ragdoll's head while down and pops off on get-up.
- Placeholder: None until Phase 3 (then a plain ring in the player's color)
- Status: Requested
