# Block Sauce: plan

Source of truth for design, architecture, decisions and phases. Older plan versions live in git history.

## Overview

Block Sauce is a third-person online physics party game for 2–8 players: build your tower, wreck everyone else's. There is no health and no death. Players only get knocked around, and stacks only fall when something hits them. Every feature should either help a player build or help them wreck; sauces tilt a block toward one or the other.

**Player verbs:** walk/run, jump, crouch, slide (run + crouch), grab, carry, hold and swing, place, throw, jumpshot (throw while airborne). A held block is available in every movement state.

**Stack:** Unity 6000.3.14f1 + URP 17.3 (view, animation, VFX); Photon Quantum 3.1.0 Preview build 2189 (deterministic simulation and networking), pinned; Blender 5.1 (Guy's model, rig and animation).

## Architecture

Anything that decides who wins lives in the Quantum simulation; anything that only changes how it looks lives in the Unity view. Only player input crosses the network; Unity reads state and events and never writes back.

| Quantum simulation (deterministic, rolled back) | Unity view (local, cosmetic) |
|---|---|
| Player: force-driven capsule, control tiers, grab, swing, throw, knockback | Animation: locomotion blends, upper-body hold layer, IK |
| Blocks: bodies, ownership, stack detection, settle progress, counting | Ragdoll pose on Guy's bones, get-up blends, dazed indicator |
| Game mode: match flow, rules, win check, team IDs | Shaders and VFX: settle dither, sauce coat |
| Knockdown: 11-limb `QuantumRagdoll`, timers, get-up placement | Camera, HUD, audio driven by simulation events |
| Sauces (Phase 5): dip planes, sauce type, effect hooks | |

Reference implementation: Photon's Friendslop sample (grab, carry, throw, force-driven movement, ragdolls, stacked props). Port systems from it; never its first-person camera or input layout.

### Character

- **Control tiers:** in control (forces chase the stick's target velocity), staggered (no steering, light braking, drops the held block), knocked down (capsule off, ragdoll limbs take over and topple towers).
- The view reads speed, direction, grounded and state, and plays Blender clips on top. Root motion stays off; animation follows the body.
- Timed moves (slide length, throw release, get-up offset) are simulation numbers sized to the authored clips.
- Foot IK and hand IK live entirely in the view.

### Holding a block

- While holding, look input (right stick or mouse) swings the block instead of turning the camera. The block orbits a pivot inside Guy at the `GrabBlock` clip's hold point (0.77 m up, 0.95 m out), level and square to him. Left/right on the left stick strafe.
- The block chases its hand point by velocity each tick, so a swing physically hits players and towers. A carrier never collides with, or is knocked by, the block it holds.
- Sideways: a swing past 45° turns the body, and the camera follows behind. Vertically: once the block reaches the top (60°) or bottom (−25°) of its swing, further up or down input tilts the camera.
- Holding Throw aims: the block rises to 30°, horizontal input turns the body, vertical input sets the throw angle, and a trajectory line and landing circle show the throw. Releasing throws (12 m/s; a jumpshot in the air is 1.3×). Grab while holding places.

### Knockdown

- One knockback path (`KnockbackSystem`). A speed change of 2.5 m/s staggers; 6 m/s knocks down (a full throw or jumpshot, a hard swing, any fast block). A running body check on someone already staggered knocks them down.
- Down for 2.5 s; each Jump press takes 0.3 s off. Bodies can be carried (toggle on `PlayerRagdollConfig`) and break free after 2.5 s, shortened the same way; a body that broke free can't be grabbed again until it gets up.
- Get-up stands Guy at the hips (0.37 m ahead, to match the clips), on the nearest clear spot, with no grace period.

## Base game mode: Stack & Lock

A player wins the round when all three of their blocks stand stacked, and counted, in one tower.

1. Each player owns 3 blocks in their color. Towers can be built anywhere.
2. Blocks are always ordinary physics bodies: nothing is frozen or locked, and any block can be grabbed at any time.
3. A block is stacked when it rests level and squarely on another block of its owner, or is a ground block with one of its owner's blocks on top. A lone block on the ground isn't stacked.
4. A stacked block at rest settles: a dither in the owner's darker shade climbs it over 3 s, and any touch at 1 m/s or more restarts it. At the top it counts, for as long as it stays stacked.
5. A block that stops being stacked stops counting, and so does any block that was only stacked because of it.
6. The first player whose tower holds 3 counted blocks wins.

**Interference:** anyone can grab and throw any block and place blocks on a rival's tower. A foreign block on top is an obstacle the owner's next block can't count on until it is knocked off.

**Built as a baseplate for future modes:** match flow is a state machine (Lobby → Countdown → Playing → Round end → Results → Lobby); the lobby is a sandbox with the win rule off; a mode is a config asset (`GameModeConfig`) with rule hooks (`OnMatchStarted`, `OnBlockCounted`, `OnBlockUncounted`, `CanCount`, `CheckWinner`); every player and block carries a team ID (free-for-all is teams of one).

## Sauce system (Phase 5)

Each dip stores a plane in the block's local space; the shader turns those planes into a thick, ragged coat with drips.

**Simulation:**
- Dipping a block into a vat records the sauce surface as a plane in block-local space (normal + offset) plus the sauce type, up to about 3 dips per block.
- Strength = min(dipped fraction ÷ 0.5, 1), where dipped fraction is the share of the block's height below its deepest dip plane.
- **Stack sauce:** area knockback when the block is stacked. **Slip sauce:** low-friction physics material.

**View:**
- Coverage mask below each dip plane, with a ragged edge from 3D object-space noise seeded by the block's entity ID (identical on every client).
- Drips: noise stretched along the plane normal. Bulge: vertices pushed along normals by mask × thickness, thicker at the lip. Glossy, wet shading with a fake subsurface tint.
- About 1 s of drip growth and sag after a dip (view time).
- Needs the final block mesh with subdivision loops (A-005); URP has no tessellation.
- Player colors own hue, so each sauce also needs its own material identity and a particle cue.

## Phases

### Phases 0–3: done
- [x] **Phase 0, Foundations:** Quantum 3.1 upgrade, git + LFS, project hygiene, Guy import test, input scheme, Lobby and Arena_Graybox scenes.
- [x] **Phase 1, Sandbox lobby:** movement tiers, orbit camera, knockback, grab/hold/swing/place/throw/jumpshot, hold-to-aim, 8-player rooms. Approved after Jackson's play-test.
- [x] **Phase 2, Stack & Lock:** owned blocks, stack detection, settling and counting, match flow, HUD placeholder, win check.
- [x] **Phase 3, Knockdown:** knockdown tier, Guy's 11-limb ragdoll, get-up, carrying bodies, falling out, 8 players down at once profiled online (sim held 64 Hz).

### Phase 4: Character (next)
Already in: Idle, forward and backward walk/run blends, jump start/loop/land, `GrabBlock` hold layer with spine turn and two-bone hand IK on the block, `Throw` follow-through, ragdoll pose on Guy's bones, get-up blends, the grabbed block squaring to Guy's facing.
- [ ] Integrate the remaining clips (A-017 to A-023): strafe, stagger, crouch, slide, two-handed aim and jumpshot, place, win and lose
- [ ] Locomotion as a 2D blend (forward, back, strafe) from simulation velocity relative to facing; crouch and slide states; stagger flinch on the upper body
- [ ] Foot IK on uneven terrain
- [ ] Widen the physics capsule to Guy's measured 0.40 m radius (from 0.35 m) and retest grab reach, hold and stacking
- [ ] Fix hold visuals: hands slightly inside the block's faces; a block raised above about 40° reaching into Guy's chest
- [ ] Win and lose clips on Round end / Results
- **Done when:** Guy plays a full match on uneven terrain with no state left unanimated.

### Phase 5: Sauces
- [ ] Dip vats; dip planes and depth-based strength in the simulation
- [ ] Stack sauce and Slip sauce gameplay
- [ ] Sauce coat shader: noise edge, drips, bulge, wet shading, settle animation
- [ ] Final block mesh (A-005) and owner readability (A-013)
- **Done when:** dipping changes how a round plays and reads at a glance.

### Phase 6: Arena and polish
- [ ] Final arena with uneven terrain and room for towers; the match runs there (today it runs in the Lobby scene)
- [ ] Camera collision with walls; menu UI, HUD, audio
- [ ] Juice: shake, particles, squash and stretch, count pop
- [ ] Feel tuning pass (aim, sensitivity, throw speed, settle and knockdown numbers)
- **Done when:** a new player finishes a round without explanation.

### Phase 7: Expansion (ongoing)
- [ ] Team modes and roles, new win rules, more sauces
- [ ] Bots (Quantum Bot SDK), matchmaking polish
- Each new mode is a config plus rule hooks.

## Decisions

Current decisions only; how they were reached is in git history.

- **Engine:** Quantum 3.1.0 Preview build 2189, pinned; URP; Generic rigs (keeps bone scale for squash and stretch). 3.1 is a preview SDK; revisit when it goes stable.
- **Scale:** every FBX imports at scale factor 0.4 (Guy about 1.62 m, block 0.8 m); Jackson keeps authoring at the current Blender scale.
- **Guy's clips** arrive as one animated FBX (mesh, skeleton, all actions).
- **Towers** can be built anywhere; any hit during settling restarts it; rivals' blocks can be grabbed, thrown and placed on your tower.
- **Stacking:** no locking. Blocks stay physical and grabbable and only count for the score. Counted blocks at rest are put to sleep (performance only). Blocks keep a plain box collider with restitution 0. Placing over another block squares it to that block and lifts it out of any overlap (toggle: Settle Placed On Block Below on `PlayerGrabConfig`). Placement is otherwise free: no snapping.
- **Grab:** the nearest free block within 2 m in a 60° cone in front of the character; it turns level and square to Guy.
- **Holding and camera:** see Architecture › Holding a block. Vertical camera tilt at the swing's edge added 2026-11-02 at Jackson's request.
- **Throwing** is hold-to-aim with a trajectory line and landing circle; a jumpshot aims the same way at 1.3× speed. The throw clip releases on frame 5 of 7.
- **Movement input:** a gamepad stick's tilt sets speed from slow walk to full run; the keyboard walks, with Shift to run. The gamepad Start button starts a match; only Escape opens the menu.
- **Knockdown:** see Architecture › Knockdown. Numbers live on `PlayerKnockbackConfig` and `PlayerRagdollConfig` and stay open for tuning.
- **Feel numbers** (aim angle, sensitivity, throw speed) stay as built until the Phase 6 tuning pass.
- **Sauce strength:** 0–50% dipped maps to 0–100% of the effect.
- **Workflow:** the agent commits; Jackson pushes `main` to GitHub. `Assets/Art/_Incoming/` is Jackson's only drop-off, under any file name. Work pauses between phases while art catches up.

## Open questions

- Settle feel: 3 s to count; a 1 m/s touch restarts settling; stacked means within half a block of center and 12° of level. Right numbers? (Play-test.)
- Should a pickup swing that passes through your own tower be able to knock it over, or should the grab ignore your own blocks? (Play-test.)

## Art pipeline

**Drop zone:** `Assets/Art/_Incoming/`, any file name. The agent renames, moves the file to `Assets/Art/<Category>/`, and copies a re-export over the existing asset so references survive. `Assets/Scripts/Editor/ArtImportPostprocessor.cs` applies import settings to everything under `Assets/Art/`: scale 0.4, Generic rig, no cameras or lights, bone-scale curves kept, clip names stripped to the action name, and clips named Idle/Walk/Run/Loop/Pose/Hold looping.

**Source files:** `C:/Users/victo/Desktop/Games/BlockSauce/modelling/` (outside the repo). Only exported FBX/PNG files enter the project.

**Naming:** `<Prefix>_<Name>[_<Variant>]`. Prefixes: `CH_` character, `BL_` block, `EN_` environment, `UI_`, `FX_`, `SFX_`, `MUS_`. Placeholders are `PH_`.

**Models (Blender → FBX):** metric, unit scale 1.0, transforms applied, origin at the intended pivot, characters facing -Y in Blender. Export Forward -Z, Up Y, Apply Unit on, Apply Scalings FBX All. One material per logical surface, named as it should appear in Unity.

**Guy:**
- Generic rig with a `root` bone at the origin; 31 deform bones, which map onto the ragdoll (`hips`, `chest`, `head`, arm and leg segments). The `prop` bone marks the hold point. **Block Sauce › Build Guy Ragdoll** rebuilds `CH_Guy_Ragdoll` when his proportions change.
- Export with the collection exporter in `guy.blend`: `armature_guy` + `mesh_guy` only; Only Deform Bones on; Add Leaf Bones off; Apply Modifiers on; Bake Animation with All Actions, NLA Strips off, Force Start/End Keying on.
- Clips: in place (no root motion), clean loops (last frame matches the first), 24 fps is fine, at least two keyframes per action (a one-key action imports empty), a fake user on every action worth keeping. Leave the scene on idle or rest when exporting.

**Textures:** PNG, power-of-two. Color maps sRGB; masks, roughness and normals linear.

**UI:** mock-ups as PNG or a Figma link; final art PNG at 2× with 9-slice margins noted; fonts TTF/OTF with license confirmed.

**Audio:** WAV 48 kHz; mono for 3D sounds.
