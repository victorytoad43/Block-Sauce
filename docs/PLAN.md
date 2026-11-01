# Block Sauce: phased plan

Human-readable version with diagrams: https://claude.ai/artifact/3hSpV9BQ7hB3bYkNG2BBv7 (this file is the source of truth for the agent; keep the two in step when scope changes).

## Overview

The first playable target is an online match where 2–8 players each stack their own 3 blocks into a tower of 3, entered through a sandbox lobby where blocks can be played with freely.

Block Sauce is a third-person physics party game: build your tower, wreck everyone else's. There is no health and no death. Players only get knocked around, and stacks only fall when something hits them. Every feature should either help a player build or help them wreck; sauces tilt a block toward one or the other.

**Player verbs:** walk/run, jump, crouch (slouch), slide (run + crouch), grab, carry, hold and swing block, place/stack, throw, jumpshot (jump + throw). A held block is available in every movement state.

**Holding a block:** while a block is held, the right stick stops moving the camera and swings the block instead. The block orbits a pivot inside the player and stays oriented to the player, like something gripped at arm's length, rather than spinning in place. Left and right on the left stick always strafe. In the simulation the block chases a hand point on that orbit by velocity each tick (as Friendslop carries objects), so a swing physically hits players and towers.

**Stack:** Unity 6000.3.14f1 + URP 17.3 (view, animation, VFX), Photon Quantum 3.1 Preview (deterministic simulation and networking), Blender 5.1 (character model, rig, animation).

**Why Quantum 3.1:** the Friendslop reference sample is built on it, and 3.1 adds what this game depends on: `CharacterJoint3D` and the `QuantumRagdoll` component for ragdolls, plus a new default physics solver (`ProjectedGaussSeidel`) that the changelog says "greatly improves the stability when stacking rigid bodies". 3.1 is a preview SDK that Photon says isn't for released games yet. That's acceptable for development; pin one build and revisit when 3.1 goes stable.

## Architecture

Anything that decides who wins lives in the Quantum simulation; anything that only changes how it looks lives in the Unity view. Only player input crosses the network; Unity reads state and events and never writes back.

| Quantum simulation (deterministic, rolled back) | Unity view (local, cosmetic) |
|---|---|
| Player controller: physics capsule, control tiers, grab, swing, throw, knockback | Character animation: locomotion blend tree, upper-body carry layer |
| Blocks: bodies, ownership, stack detection, settle progress, counting | IK: foot grounder on uneven terrain, hands on the held block |
| Game mode: match flow, rules, win check, team IDs | Ragdoll visuals: limb views, get-up blend, cosmetic flinches |
| Sauce state: dip planes, sauce type, effect hooks | Shaders and VFX: settle dither, sauce coat, explosions, count pop |
| Knockdown: down state, timers, jointed-limb ragdoll that hits blocks | Camera, UI, audio driven by simulation events |

Reference: Quantum's [Friendslop sample](https://doc.photonengine.com/quantum/v3/game-samples/friendslop/overview) covers grab, carry and throw, deterministic ragdolls built from jointed limbs, and stacked physics objects.

### Deterministic ragdolls

A knockdown switches off the character's capsule and spawns a ragdoll of jointed limb entities in the character's pose. On get-up the ragdoll is destroyed and the character stands where the hips came to rest. The cost only exists while someone is down.

- **CPU under rollback:** every limb is a body plus a joint, re-simulated on every misprediction. Worst case: all 8 players down at once beside 24 blocks.
- **Limp only:** limbs are passive; stumbles and flinches stay view-side.
- **Fixed-point tuning:** Quantum physics is not PhysX; joints need their own tuning (Friendslop gives bridge planks custom inertia for stability).
- **Visible corrections:** remote ragdolls can snap when real inputs arrive; the view smooths limb positions.
- **Levers:** limb count, capsule colliders, a short knockdown timer, prediction culling around the local player, tick rate.
- **Payoff:** limbs knock towers over, bodies can be grabbed and thrown, every client sees the same tumble.

**Implementation:** use Quantum 3.1's `QuantumRagdoll` component, as Friendslop does (`Simulation/Ragdoll`: `RagdollSystem`, `ImpactRagdollSystem`, `RagdollConfig`). It builds about 11 limbs: hips, chest, head, and upper and lower segments for each arm and leg. Start there and profile; merge segments only if the numbers say so. Guy's deform bones map directly: `hips`, `chest`, `head`, `upper_arm`/`forearm`/`hand`, and `thigh`/`shin`/`foot` (`.L`/`.R`). The rig is Generic, so assign these Transforms manually and leave the component's Animator field empty.

### Character movement: physics body, authored animation

The simulation owns a force-driven physics capsule; the view plays Blender animation on top of it.

1. **In control:** forces chase the stick's target velocity with tuned acceleration and braking, so movement feels tight while pushes and collisions stay physical. Friendslop's `MovementSystem` already works this way (`AddForce` toward a target velocity, a braking force, step-up and slope handling), so it is the starting point.
2. **Staggered:** a moderate hit cuts control force for a moment and the body slides on its momentum. The view layers an authored flinch over locomotion.
3. **Ragdoll:** past the knockdown threshold the capsule switches off and the limbs take over, free to topple towers.

- The view reads speed, direction, grounded and a state enum (slide, jump, holding, staggered). Root motion stays off; the animation follows the body.
- Timed moves (slide length, throw release, jumpshot) are simulation timers sized to the authored clips.
- Foot IK and hand IK stay entirely in the view.
- Guy's rig has a `prop` bone (rest position about 0.84 m in front of and 0.84 m above the root at game scale). Use it as the default hold point the simulation's swing orbit is built around; the view keeps the held block's model on the simulated block, with the hands IK'd to it.

## Base game mode: Stack & Lock

A player wins the round when all three of their blocks stand stacked, and counted, in one tower of 3.

1. Each player owns 3 blocks in their color. Towers can be built anywhere in the arena.
2. Blocks are always ordinary physics bodies: nothing is frozen or locked together, and any block can be grabbed at any time.
3. A block is stacked when it rests level and squarely on another block of its owner, or is a ground block with one of its owner's blocks on top. A lone block on the ground isn't stacked.
4. A stacked block at rest settles: a dither in the owner's darker shade climbs it over the settle time, and any hit restarts it. When it reaches the top, the block counts (shown solid) for as long as it stays stacked.
5. A block that stops being stacked (knocked off, grabbed, tipped over) stops counting, and so does any block that was only stacked because of it.
6. The first player whose tower holds 3 counted blocks wins.

**Interference:** any player can grab and throw any block, including rivals' blocks and blocks out of a tower, and can place blocks on a rival's tower. A block only counts in its owner's stack; a foreign block on top is an obstacle the owner's next block can't count on until it is knocked off.

**Stacking stability:** plain stacked blocks stand reliably under Quantum 3.1's default solver (Phase 1 test). The only physics change for stacking is a performance one: a counted block that comes to rest is put to sleep at once, so a finished stack leaves the solver until something touches it.

**Sim vs view:** stack detection and settle progress (0–1) live in the simulation; the dither shader only reads them. Detection uses what each block rests on (a ray straight down, its owner, the offset from the block below) and which way its faces point.

**Built as a baseplate for future modes:**

- Match flow as a state machine: Lobby → Countdown → Playing → Round end → Results → Lobby.
- The lobby is a mode with the win rule off: free blocks to throw around while players join.
- A mode is a config asset: blocks per player, tower height, settle time, stacking tolerances, team size, allowed sauces, win rule.
- Every player and block carries a team ID. Free-for-all is teams of one.
- Rules hook into named events (match started, block counted, block uncounted, win check). New modes override hooks instead of forking code.

## Sauce system

Each dip stores a plane in the block's local space, and the shader turns those planes into a thick, ragged coat with drips.

**Simulation:**

- Dipping a block into a vat records the sauce surface as a plane in block-local space (normal + offset), plus the sauce type. Up to ~3 dips per block.
- Sauce type attaches gameplay hooks. **Stack sauce:** AoE knockback when the block is stacked. **Slip sauce:** low-friction physics material.
- **Dip depth sets strength:** strength = min(dipped fraction ÷ 0.5, 1), where dipped fraction is the share of the block's height below its deepest dip plane.

**View:**

- Coverage mask: everything below a dip plane is coated.
- Ragged edge: offset the plane test with 3D object-space noise, seeded from the block's entity ID so every client sees the same coat.
- Drips: noise stretched along the plane normal pushes streaks below the edge.
- Bulge: displace vertices along normals by mask × thickness, with a thicker lip at the edge; normals from the mask gradient.
- Shading: glossy, wet, fake subsurface tint in the sauce color.
- Settle animation: drips grow and the edge sags for ~1 s after the dip (view-side time).
- Mesh: the block needs bevels and subdivision loops for displacement (URP has no built-in tessellation).
- Readability: player colors own hue, so each sauce also needs its own material identity (gloss, bubbles, sparkle) plus a particle cue.

## Phases

Phases 0–2 build the whole game in gray-box before final art, so the first playable match proves the fun early. Art spikes (settle dither shader, sauce coat shader) can run any time. Guy's rig is already well along in Blender, so his import test moves up to Phase 0, and his real proportions set the physics capsule from the start.

### Phase 0: Foundations
- [x] **Upgrade Quantum to 3.1 Preview.** Default source: the SDK bundled in the Friendslop sample (3.1.0 Preview build 2189), which keeps exact parity with the reference code. Copy `Assets/Photon/Quantum`, `Assets/Photon/PhotonLibs` and `Assets/Photon/PhotonRealtime` with their `.meta` files; skip PhotonVoice, PhotonChat and QuantumDemoInput unless a later phase needs them. If Jackson has downloaded a newer 3.1 build instead, use that. Back up `PhotonServerSettings.asset` first, follow Photon's 3.1 migration guide, regenerate Quantum code, and confirm the AppId survived and the console is clean.
- [x] **Git:** `git init`, `git lfs install`, use the provided `.gitignore` and `.gitattributes`, and make the first commit after the upgrade compiles.
- [x] **Project hygiene:** remove the URP template's tutorial content (`Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene.unity`); set Player Settings › Run In Background on (it's off, and Quantum clients must keep simulating unfocused); set the company name; install Unity Multiplayer Play Mode for multi-client testing.
- [x] **Study Friendslop:** write notes in PROGRESS.md on its Player, Input, Movement, GroundedTracker, Grab, Attack, Ragdoll and Fall systems: what to port as-is, what to adapt, what to skip.
- [ ] **Guy import test:** export Guy from `modelling/guy/guy.blend` per the ASSETS.md spec (ask Jackson to do the export, or to approve you running the export headlessly). Import as Generic at the agreed scale factor and confirm: height matches the decision, squash-and-stretch bone scale plays, `prop` bone present, walk clip plays in place. Record Guy's game-scale height, width and `prop` offset in ASSETS.md A-003, and size the physics capsule from them.
- [x] **Input scheme:** move, strafe-always while holding, camera vs swing on the right stick, all buttons defined in Quantum input.
- [x] **Scenes:** Lobby and Arena_Graybox with uneven test terrain.
- **Done when:** two clients join a room and move capsules sized to Guy. **Verified 2026-10-28** (see the Phase 0 report in PROGRESS.md). Guy import test still open, waiting on his first FBX.

### Phase 1: Sandbox lobby
- [x] Physics-capsule movement adapted from Friendslop's `MovementSystem`: run, jump, crouch, slide; control tiers (in control, staggered)
- [x] Third-person orbit camera; right stick switches to swing while holding
- [x] Knockback on hit through one impulse path
- [x] Blocks: grab, carry, hold-and-swing, place, throw, jumpshot with plain cubes (adapt Friendslop's `GrabSystem`)
- [x] Stacking test: towers of 3 plain blocks under the 3.1 solver; note in PROGRESS.md whether they stand unaided
- [x] Early stress test: 8 Quantum ragdolls (Friendslop's prefab is fine) piling onto block towers, to read rollback cost early
- **Done when:** up to 8 online players toss blocks around and it feels fun. **Mechanics verified 2026-10-28 with 8 online clients (see the Phase 1 report in PROGRESS.md); "feels fun" waits on Jackson's play-test.**

### Phase 2: Stack & Lock mode
- [x] 3 owned blocks per player; settle progress, dither shader, reset on hit
- [x] Stack detection and counting; blocks stay fully physical and always grabbable; counted blocks sleep at rest (revised 2026-10-29, replacing locked/frozen blocks and unlock thresholds)
- [x] Foreign blocks as obstacles on rival towers
- [x] Match flow state machine; mode config asset; team IDs; win check
- [x] Minimal HUD: tower progress per player, countdown, results
- **Done when:** a gray-box match runs from lobby to a winner and back to the lobby. **Verified 2026-10-29 with 2 online clients (see the Phase 2 report in PROGRESS.md); waiting on Jackson's play-test.**

### Phase 3: Knockdown
- [x] Knockdown thresholds and staggered tier tuning (first numbers per Jackson; tuning stays open)
- [x] Deterministic ragdoll built with `QuantumRagdoll` from Guy's bones (11 limbs); limbs collide with and topple blocks
- [x] Get-up: ragdoll destroyed, character stands at the hips; view blends to a get-up clip
- [x] Profile 8 players down at once beside 24 blocks; trim limbs only if needed (not needed)
- **Done when:** a tumbling body topples a tower, and 8 players down at once holds frame rate online. **Verified 2026-10-31 (see the Phase 3 report in PROGRESS.md); waiting on Jackson's play-test.**

### Phase 4: Character
- [ ] Integrate Guy's full clip set (deform skeleton exported from Jackson's control rig, Generic)
- [ ] Locomotion blend tree driven by simulation speed, direction and state; upper-body carry layer (avatar mask)
- [ ] Foot IK on uneven terrain; hand IK on the held block, anchored at the `prop` bone
- [ ] A grabbed block turns to align straight with the character's facing (decided 2026-10-29)
- [ ] Ragdoll limb views mapped to Guy; get-up blends; eyes and jaw driven for expressions if clips use them
- **Done when:** Guy plays a full match on uneven test terrain.

### Phase 5: Sauces
- [ ] Dip vats; dip planes and depth-based strength in the simulation
- [ ] Stack sauce and Slip sauce gameplay
- [ ] Sauce coat shader: noise edge, drips, bulge, wet shading, settle animation
- **Done when:** dipping changes how a round plays and reads at a glance.

### Phase 6: Arena and polish
- [ ] Final arena with uneven terrain and room for towers
- [ ] Camera, menu UI, HUD, audio
- [ ] Juice: shake, particles, squash and stretch, count pop
- **Done when:** a new player finishes a round without explanation.

### Phase 7: Expansion (ongoing)
- [ ] Team modes and roles, new win rules, more sauces
- [ ] Bots (Quantum Bot SDK), matchmaking polish
- Each new mode is a config plus rule hooks.

## Decisions

- Towers can be built anywhere.
- A hit during the dither restarts the block's settle progress.
- Players can grab and throw rivals' blocks and place blocks on rivals' towers; a foreign block is an obstacle until knocked off.
- Holding a block: the right stick swings it instead of turning the camera; left/right on the left stick always strafe.
- Characters: force-driven physics capsule with three control tiers; authored animation layered in the view.
- Ragdolls: deterministic, built with `QuantumRagdoll` (about 11 limbs), so tumbling bodies can topple towers.
- Rig type: Generic (keeps bone scale for squash and stretch).
- Render pipeline: URP.
- Dip depth drives strength: 0–50% dipped maps to 0–100% of the effect.
- Quantum 3.1 Preview, pinned to one build; the Friendslop sample's SDK (build 2189) by default.
- The character is Guy (`modelling/guy/guy.blend`).
- Game scale: every FBX from Jackson imports at scale factor 0.4 (Guy about 1.66 m, block about 0.8 m); Jackson keeps authoring at the current Blender scale. (Confirmed 2026-10-28.)
- Guy's clips arrive as one animated FBX (mesh, skeleton and actions together) rather than one file per clip. (Confirmed 2026-10-28.)
- Turning while holding: a big swing turns the body (past 45°), the camera follows behind. Final numbers come from play-testing. (Jackson, 2026-10-29.)
- Placing: free placement where the block is held, square to the facing; no snapping to the tower below. (Jackson, 2026-10-29.)
- Throwing is hold-to-aim: holding Throw shows a trajectory line and a landing circle, releasing throws. A jumpshot aims the same way and is a bit more powerful. (Jackson, 2026-10-29.)
- Grab targeting: the nearest free block within 2 m in front of the character. Once Guy has arms (Phase 4), a grabbed block should turn to align straight with the character's facing instead of keeping its current rotation. (Jackson, 2026-10-29.)
- Phase 1 approved after play-testing. Feel numbers (aim angle, sensitivity, throw speed) stay as built until the plan's polish phase. (Jackson, 2026-10-29.)
- Locomotion animation: the walk clip covers slow movement (low stick pressure) and a run clip covers faster movement; the walk may be sped up to fit. The throw clip releases on frame 5 of 7. (Jackson, 2026-10-29.)
- Stacking, revised after the Phase 2 play-test: no locking. Blocks stay physical and always grabbable; stacked blocks only count for the score (a lone ground block doesn't), and the only physics change is putting counted blocks to sleep for performance. The gamepad Start button starts a match and no longer opens the menu (Escape still does). (Jackson, 2026-10-29.)
- After Phase 2, work pauses so art can catch up before Phase 3. (Jackson, 2026-10-29.)
- Git workflow: the agent commits; Jackson pushes `main` to GitHub at milestones. (Jackson, 2026-10-29.)
- Art delivery: `Assets/Art/_Incoming/` is Jackson's only drop-off, under any file name; the agent handles naming, placement and import settings. (Jackson, 2026-10-29.)
- Knockdown: a big hit knocks you down (a full-speed throw or jumpshot to the body, a hard swing, a running body check on someone already staggered); smaller hits stagger. Fast-moving blocks in general can stagger or knock down too. (Jackson, 2026-10-31.)
- Knockdown duration: 2.5 s, each Jump press takes 0.3 s off. The numbers stay open for tuning and live on `PlayerRagdollConfig`; the knockdown threshold (6 m/s) lives on `PlayerKnockbackConfig`. (Jackson, 2026-10-31.)
- Grabbing downed bodies: an editor toggle (Allow Grabbing Bodies on `PlayerRagdollConfig`), so both ways can be tried; on by default. (Jackson, 2026-10-31.)
- No knockdown grace after getting up: you can be knocked down again straight away. (Jackson, 2026-10-31.)
- Bodies stay carryable. A carried body breaks free after 2.5 s, and Jump presses shorten that the same way they shorten the knockdown (0.3 s each); once it has broken free it can't be picked up again until it has got up. (Jackson, 2026-10-31.)
- Speed on a gamepad comes from how far the stick is pushed, from a slow walk up to a full run; the stick-click run button is gone. The keyboard keeps walk, with Shift to run. (Jackson, 2026-10-31.)

## Open questions

- (Agent: add questions here as they come up; move them to Decisions once Jackson answers.)
- Settle feel: 3 s to count; a touch at 1 m/s or more restarts a settling block; a block stays stacked within half a block of center on the one below and within 12° of level. Right numbers? (Phase 2 play-test.)
- Should picking up a block be able to knock your own tower over when the pickup swing passes through it, or should a block's own pickup be ignored? (Phase 2 play-test.)
