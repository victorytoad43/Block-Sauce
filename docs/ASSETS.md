# Block Sauce: asset requests

The agent adds requests here at the start of every phase (plus look-ahead for the next phase and long-lead items). Jackson works from the tracker, drops files in `Assets/Art/_Incoming/`, and sets the status to `Delivered`. The agent integrates and sets `Integrated`, or `Needs changes` with a note.

**Statuses:** `Requested` → `In progress` (Jackson) → `Delivered` (Jackson) → `Integrated` | `Needs changes` (agent)
**Priority:** `Start now` (long lead, start immediately) · `Needed soon` (needed by the phase in progress) · `Next phase` · `Nice to have`

## Tracker

| ID | Asset | Needed by | Priority | Status | Placeholder in use |
|---|---|---|---|---|---|
| A-001 | Player color palette (8 colors) | Phase 2 | Next phase | Requested | Unity default colors |
| A-002 | Lobby + main menu UI mock-ups | Phase 1 | Next phase | Requested | Default UI |
| A-003 | Guy character model + rig | Phase 0 (import test) | Needed soon | In progress | `PH_PlayerCapsule` |
| A-004 | Guy animation set (first pass) | Phase 4 (walk needed soon for Phase 0) | Start now | In progress | None |
| A-005 | Block mesh | Draft for Phase 1; final by Phase 5 | Needed soon | Requested (draft seen) | `PH_Block` |
| A-006 | Match HUD + results screen mock-ups | Phase 2 | Next phase | Requested | Default UI |
| A-007 | Lock effect art direction | Phase 2 | Next phase | Requested | Agent's shader defaults |
| A-008 | Arena layout sketch | Phase 6 (graybox from Phase 0) | Nice to have | Requested | Graybox terrain |
| A-009 | Sauce vat model + sauce look references | Phase 5 | Nice to have | Requested | Cylinder |
| A-010 | Block Sauce logo / wordmark | Phase 6 | Nice to have | Requested | Text |
| A-011 | Movement feel targets (design input) | Phase 1 | Needed soon | Requested | Friendslop defaults |
| A-012 | Guy hold-block pose + throw timing | Phase 1 timings; Phase 4 clip | Start now | Requested | None |
| A-013 | Block owner tint and team readability | Phase 2 | Next phase | Requested | Flat team color on `PH_Block` |
| A-014 | SFX placeholder list | Phase 6 (temp SFX any time) | Nice to have | Requested | Silent |

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

**Drop zone:** `Assets/Art/_Incoming/`. The agent moves files to `Assets/Art/<Category>/` on integration.

**Source files:** Jackson's Blender files live in `C:/Users/victo/Desktop/Games/BlockSauce/modelling/` (outside the repo). Only exported FBX/PNG files enter the project, through the drop zone.

**Naming:** `<Prefix>_<Name>[_<Variant>]`. Prefixes: `CH_` character, `BL_` block, `EN_` environment, `UI_`, `FX_`, `SFX_`, `MUS_`. The character is `CH_Guy`; animation clips are `CH_Guy@<ClipName>.fbx` (Unity's `@` convention).

**Scale:** Jackson authors at about 2.5× real-world scale (Guy is 4.15 m tall in Blender; the draft block is 2 m). Physics must run at real-world scale, so every FBX from him is imported with **Scale Factor 0.4** (confirmed by Jackson 2026-10-28). Keep all Blender art at this same authoring scale so one factor covers everything. The agent sets this in a Unity import preset for `Assets/Art/`.

**Models (Blender → FBX):**
- Metric units, unit scale 1.0, object transforms applied, object origin at the intended pivot.
- Characters face -Y in Blender (Guy does). Export with Forward: -Z, Up: Y, Apply Unit on, Apply Scalings: FBX All.
- One material per logical surface; name materials the way they should appear in Unity.

**Characters:**
- **Generic** rig. A `root` bone at the origin; keep bone scale animatable (squash and stretch).
- Export the armature and the character mesh only: **Only Deform Bones** on, **Add Leaf Bones** off, and leave out the `WGT_*` widget objects and any draft props in the file.
- Mesh + skeleton in `CH_Guy.fbx`; one clip per file as `CH_Guy@Clip.fbx`, or all actions in one file with clean action names. Pick one and note it in A-004.
- No root motion in clips: animate in place. The simulation moves the body.
- Loops must loop cleanly; first and last frames match.
- Clips are authored at 24 fps; that's fine (Unity resamples, and the simulation runs at 64 Hz independently).
- Give every action you want to keep a fake user in Blender, so it isn't lost on save.

**Textures:** PNG, power-of-two sizes. Color maps sRGB; masks, roughness and normals linear (agent sets this on import).

**UI:** Mock-ups as PNG or a Figma link in the request's notes. Final UI art as PNG at 2× with 9-slice margins noted; fonts as TTF/OTF with license confirmed.

**Audio (Phase 6):** WAV 48 kHz; mono for 3D sounds.

## Requests

### A-001: Player color palette
- Phase / needed by: Phase 2, block ownership colors and the lock dither
- Priority: Next phase
- Purpose: Each player's blocks, character tint and HUD use one color. Sauces must stay readable on top of every one.
- Spec: 8 colors as hex, each with a darker shade for the lock dither and a lighter shade for highlights. Distinguishable for common color-vision deficiencies and on both light and dark terrain.
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
- Status: In progress
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
- Must match: The agent records throw release frame, slide duration and jump timings here once the simulation timers are tuned in Phase 1. Hands on the held block are IK, so the hold pose only needs to be close.
- Placeholder: None (capsule)
- Status: In progress
- Notes: the file has three actions named `Guy.WalkBasic`, `.001` and `.002`; only `.002` has keyframes (frames 0–96) and it has no fake user. 2026-10-28: Jackson says none of these is a real walk yet; animation work continues. **Delivery format: one animated FBX** (`CH_Guy.fbx` with mesh, deform skeleton and all actions, using clean action names). The Phase 0 import test (height, `prop` bone, squash-and-stretch bone scale, a clip playing in place) runs when the first export lands in `_Incoming`. Until then, Phase 0 uses the capsule at the confirmed 1.66 m.

### A-005: Block mesh
- Phase / needed by: draft now (Phase 1 tunes grab, swing and stacking at its size); final topology by Phase 5 for sauce displacement
- Priority: Needed soon
- Purpose: The stackable block.
- Spec: Beveled cube with several even subdivision loops per face so the sauce shader can displace it; UVs unwrapped; pivot at the center.
- Must match: Draft block is 2 m at authoring scale (0.8 m in game). The agent records the final block size and mass here after Phase 1 tuning.
- Placeholder: Unity cube, `PH_Block`
- Status: Requested
- Notes: `guy.blend` contains a `Cube` (2 m, 104 verts, geometry nodes modifier) that looks like a block draft. If so, export it as `BL_Block.fbx` with the geometry nodes applied. An early draft export helps Phase 1: grab, swing and stacking get tuned at the block's real size, so a size change after Phase 1 means retuning.

### A-006: Match HUD + results screen mock-ups
- Phase / needed by: Phase 2
- Priority: Next phase
- Purpose: Countdown, each player's tower progress (3 slots: empty, locking, locked), round end and results.
- Spec: Mock-ups at 1920×1080 for 2 and 8 players.
- Placeholder: Default UI
- Status: Requested

### A-007: Lock effect art direction
- Phase / needed by: Phase 2
- Priority: Next phase
- Purpose: The dither overlay climbing a placed block, and the "locked" pop when it reaches the top. The agent builds the shader; this is the look to aim for.
- Spec: A few frames or a short reference clip; dither pattern and scale; the pop (flash, ring, particles, sound idea).
- Placeholder: Agent's default shader
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
- Placeholder: None (capsule; the block floats at the `prop` offset)
- Status: Requested

### A-013: Block owner tint and team readability
- Phase / needed by: Phase 2, when blocks get owners
- Priority: Next phase
- Purpose: Every block shows whose it is at a glance, from across the arena and while tumbling, and later under a sauce coat (A-009).
- Spec: How ownership shows on the block: a full tint, a colored edge or bevel, a stripe or icon, or a combination. One reference image or a quick paint-over of the A-005 draft is enough.
- Must match: The player palette in A-001. The lock dither (A-007) climbs the block in the owner's darker shade, so the base treatment must leave room for that overlay.
- Placeholder: `PH_Block` with a flat team color
- Status: Requested

### A-014: SFX placeholder list
- Phase / needed by: final audio Phase 6; temporary sounds welcome any time
- Priority: Nice to have
- Purpose: The simulation raises events for jump, grab, drop, throw, block impact (with impact speed) and knockback. The view can play a sound on each as soon as one exists.
- Spec: WAV 48 kHz mono, short. For each event, one sound or a pointer to a free/CC0 pack you like. Impacts benefit from 2-3 variations, plus a light and a heavy version.
- Must match: Event names in code: `Jumped`, `Grabbed`, `Dropped`, `Thrown`, `ObjectImpact` (speed), `KnockedBack`.
- Placeholder: Silent
- Status: Requested
