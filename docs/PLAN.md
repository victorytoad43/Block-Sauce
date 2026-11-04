# Block Sauce: plan

Source of truth for design, architecture, decisions and phases. Older plan versions live in git history.

## Overview

Block Sauce is a third-person online physics party game for 2–6 players: build your tower, wreck everyone else's. There is no health and no death. Players only get knocked around, and stacks only fall when something hits them. Every feature should either help a player build or help them wreck; sauces tilt a block toward one or the other.

**Player verbs:** walk/run, jump, sit, slide (sit while moving fast), grab, carry, hold and swing, place, throw, jumpshot (throw while airborne). A held block is available in every movement state.

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

- **Sit and slide:** the crouch button sits. A sitting character is stationary: no steering, turning or jumping, it only brakes (a shove still slides it). Pressing it while moving 4 m/s or faster slides instead (7.5 m/s for 0.75 s), ending in a sit while the button is held. The capsule is 1.30 m tall while sitting or sliding. The view plays the sit-down, blending into the slide clip while a sitting body moves faster than 2 m/s.
- **Control tiers:** in control (forces chase the stick's target velocity), staggered (no steering, light braking, drops the held block), knocked down (capsule off, ragdoll limbs take over and topple towers).
- The view reads speed, direction, grounded and state, and plays Blender clips on top. Root motion stays off; animation follows the body.
- Timed moves (slide length, throw release, get-up offset) are simulation numbers sized to the authored clips.
- Foot IK (feet planted per foot on statics and blocks, hips dropping up to 0.35 m) and hand IK live entirely in the view. A stagger plays a full-body flinch for the side that was hit.

### Holding a block

- While holding, look input (right stick or mouse) swings the block instead of turning the camera. The block orbits a pivot inside Guy at the `GrabBlock` clip's hold point (0.77 m up, 0.95 m out), level and square to him. Left/right on the left stick strafe.
- The block chases its hand point by velocity each tick, so a swing physically hits players and towers. A carrier never collides with, or is knocked by, the block it holds.
- The camera always moves with look input while holding, on a gradient: 35% of the input with the block in the middle of its swing, rising smoothly to all of it at the edges. Sideways, a swing past 45° turns the body and the camera turns with it. Vertically, past the top (60°) or bottom (−25°) of the swing, input only tilts the camera. Aiming tilts the same way with the throw angle.
- Holding Throw aims: the block rises to 30°, horizontal input turns the body, vertical input sets the throw angle, and a trajectory line and landing circle show the throw. Releasing throws (12 m/s; a jumpshot in the air is 1.3×). Grab while holding places.
- Aim and throw in the view: aiming holds frame 4 of `Throw` (the wind-up, block overhead) on the upper body, and the block's model rides in Guy's hands while the real block stays at the 30° aim point in front, colliding with nothing (it sits on the `AimedBlock` physics layer). The model moves between the two on an arc over Guy's head, both when aiming starts and on release. Releasing plays `Throw` on from frame 4, and the model catches up with the flying block within 0.08 s. For 0.15 s after a throw the block passes through its thrower and anything it was released inside. In the air it is the same over `JumpLoop`. The camera moves over the right shoulder while aiming so the raised block doesn't hide the aim line.

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

Each dip stores a plane in the block's local space; the shader turns those planes into a thick, lumpy, ragged coat that changes the block's silhouette as much as its color. The two sauces are opposites, both about grip: Stack is sticky, Slip is slippery. That leaves room for other kinds of sauce later.

**Vats:** Jackson's cup (`EN_SauceVat`) at 1.3× (1.94 m across, rim 0.61 m, above Guy's 0.5 m step), two at the middle of the Lobby (Stack at x −2.1, Slip at x +2.1). Collision follows the cup's shape, a flared paper sauce cup: 24 wall boxes leaning out 14° along the inner wall (0.71 m radius at the floor to 0.86 m at the rim) and a floor at the cup's inner bottom (0.05 m), all on the `Vat` layer. The hold's line-of-sight check ignores that layer, so a held block can be lifted over the rim and pushed down in. Sauce fills to 0.55 m; a block on the floor is about 60–70% dipped.

**Simulation (`SauceSystem`, tuning on `SauceConfig`, referenced by the game mode):**
- A block reaching into a vat's sauce volume is dipped, held or loose: the surface is recorded as a plane in block-local space (normal + offset) with the sauce type, keeping the deepest the block went. A dip ends 0.25 s after the block leaves (so an edge bobbing across the surface stays one dip). Up to 3 dips per block; a fourth replaces the shallowest. Dips under 5% leave nothing.
- Strength per sauce = its deepest dip's fraction ÷ 0.5, capped at 1. Fraction = the share of the block's height, along the plane normal, below the plane.
- **Stack sauce:** the block takes a high-friction physics material in four steps by strength (friction 1.5, 2.25, 3, 4, combined by multiplying; plain blocks 0.8, the ground 0.6), and it glues itself to whatever it touches while nearly at rest (under 0.5 m/s against it): to each touching block, and to the ground or any static surface. Glue is a stiff joint (a character joint bending at most 2°) at the shared face, or at the block's center for the ground. It tears when either glued block is hit at 3 m/s into the contact at the lowest strength to 7 m/s at full (a throw is 12, a jumpshot 15.6; walking, running and a flying ragdoll stay under it), when either block is grabbed, or when it is pulled more than 10 cm apart. Torn blocks glue again wherever they come to rest. A glued Stack tower stands up to shoves and ragdoll slams; a throw breaks it. Where a block has both sauces, the stronger wins by the difference.
- **Slip sauce:** the block takes a low-friction physics material in four steps by strength (friction 0.6, 0.4, 0.2, 0.03; plain blocks 0.8), so it slides, and what is stacked on it slides off.
- New blocks every match, so sauce never carries over.

**View:**
- Coverage mask below each dip plane, with a ragged edge from 3D object-space noise seeded by the block's entity ID (identical on every client).
- The coat is drawn on its own evenly divided rounded-box shell the block's size (28 quads per face edge, edges rounded 4 cm), not the block's mesh, so it has the vertices for volume however the block is modelled. Under the sauce the shell is pushed out along its smooth normals by 1.9 cm plus soft lumps of up to 3 cm, ramping up over 6 cm from the edge to a rounded bead; lighting uses the pushed-out surface's normals. URP has no tessellation.
- Drips are off for now (`_DripLength` 0 on `PH_SauceCoat`), to be reconsidered once the coat's volume is settled (Jackson, 2026-11-03).
- Player colors own hue, so the sauces differ in value and cue: Stack is dark and warm with rising bubbles, Slip pale and glossy with glints (`PH_SauceLooks`). A dip splashes the vat's sauce.

## Phases

### Phases 0–3: done
- [x] **Phase 0, Foundations:** Quantum 3.1 upgrade, git + LFS, project hygiene, Guy import test, input scheme, Lobby and Arena_Graybox scenes.
- [x] **Phase 1, Sandbox lobby:** movement tiers, orbit camera, knockback, grab/hold/swing/place/throw/jumpshot, hold-to-aim, 8-player rooms. Approved after Jackson's play-test.
- [x] **Phase 2, Stack & Lock:** owned blocks, stack detection, settling and counting, match flow, HUD placeholder, win check.
- [x] **Phase 3, Knockdown:** knockdown tier, Guy's 11-limb ragdoll, get-up, carrying bodies, falling out, 8 players down at once profiled online (sim held 64 Hz).

### Phase 4: Character (done 2026-11-02, reviewed)
In: Idle; walk and run as 2D blends (forward, back, strafe); jump start/loop/land; sit and slide; stagger flinches by hit side; `GrabBlock` hold layer with spine turn and two-bone hand IK on the block; aim pose and `Throw` follow-through, grounded and airborne; foot IK on uneven ground and blocks; ragdoll pose on Guy's bones; get-up blends; the grabbed block squaring to Guy's facing; capsule at Guy's 0.40 m radius.
- [x] Strafe, stagger, sit and slide clips integrated (2026-11-02)
- [x] Crouch replaced by sit (stationary, slides above 4 m/s)
- [x] Capsule widened to 0.40 m; grab reach and flush placing retested
- [x] Foot IK on uneven terrain
- [x] Hold visuals: hands on the block's faces, no chest clipping from 0° to 60° (checked 2026-11-02)
- [x] Two-handed aim pose and jumpshot from `Throw` frame 4 (2026-11-02)
- [x] Place: no clip; letting go blends the upper body back out of the hold
- [x] Camera keeps moving with look input while holding (gradient instead of a locked middle)
- [x] Done-when verified: a full match with 2 clients (MPPM `TwoClients` scenario), every state cycled; win and lose play `Idle` until A-023
- **Done when:** Guy plays a full match on uneven terrain with no state left unanimated.

### Phase 5: Sauces (done 2026-11-03, reviewed)
- [x] Dip vats (Jackson's cup); dip planes and depth-based strength in the simulation (2026-11-02)
- [x] Stack sauce and Slip sauce gameplay; Stack reworked from a shove to sticky friction and glue (2026-11-03)
- [x] Sauce coat shader: noise edge, lumpy volume on its own shell, wet shading (drips off for now, 2026-11-03)
- [x] Subdivided block mesh (A-005) in; owner readability (A-013) moved to the visual polish after the plan review
- [x] Aimed block collides with nothing; its model arcs over the head
- [x] Done-when verified with two clients (2026-11-02): Slip tower comes apart when pushed (the Stack shove it also checked is gone)
- **Done when:** dipping changes how a round plays and reads at a glance.

### Phase 6: Front end and bots (done 2026-11-04)
A rough draft of the menu and UI, the spectator camera and bots, so the game can be seen whole before a plan review (Jackson, 2026-11-03). Art for the UI is Jackson's mock-up and pieces (A-002); everything without art is a rough draft in his style, not final.
- [x] Bots: a bot takes a free player slot and its brain writes that slot's input, so every system treats it as a player (owner, team, color, score). Actions in our code (go to, grab, carry, place on a tower, throw at a target, dunk, get up); decisions in a Bot SDK state machine (fetch, build, raid, dunk, recover). The host adds and removes bots in the room; bots fill empty slots when a match starts, a room-screen setting that is on by default. One skill level, a bit clumsy (Jackson, 2026-11-03).
- [x] Spectator camera: a director that follows the action (who is building, who is fighting), cutting and easing between players; non-interactive behind the menu. In a room, a viewer can take over: follow the previous or next player, hold the overview (wide shot of everyone), or hand back to the director (Jackson, 2026-11-03). Free flight comes later.
- [x] Menu: Jackson's main menu (logo and Start Game, Join Game, Settings on the right) over a live bot match seen from the spectator camera, running offline on this machine. Start Game hosts an online room and Join Game joins one by room code or any open room; both land in the sandbox room. No offline solo mode: a room can be all bots. Settings: volume, look sensitivity, invert Y, fullscreen and resolution. Controller and mouse both drive it. Replaces the stock Quantum start UI.
- [x] In-room UI and HUD, rough draft in the Jaro font and Jackson's style: room code, player list in colors with bots marked, add/remove bots, start match; match HUD (countdown, each player's tower slots) and results. Replaces the IMGUI `MatchHud`.
- [x] All-bot test match: set up in a live room (add bots or fill at start), 6 bots play Stack & Lock to a win, watched with the spectator camera. Testing only, not a player feature.
- [x] Done-when verified with 2 clients (MPPM `TwoClients`): menu over the demo match, host, guest by code, bots added, a full match, back to the menu; the demo's 6 bots finish matches alone
- **Done when:** the game opens on the menu over a bot match; a player can start a room, add bots, play a full match against them and return to the menu; a second player joins that room by code; 6 bots finish a match on their own.

### Plan review (in progress)
After Phase 6, Jackson and the agent replan what follows from playing the whole game. It includes an optimization pass (Jackson): profile the full game with 6 players and bots, CPU and GPU, then fix the top costs.
- [x] Optimization pass (2026-11-04): the one big cost was a bug, an exception thrown every tick after a knockdown from a collision (about 53 ms a frame; it also left the demo stuck on its results). Fixed. With 6 bots playing, a frame costs about 5 ms on the main thread offline and about 8 ms online (7 simulation steps a frame with rollbacks), and 1.5 ms on the GPU, measured in the agent's unfocused editor.
- [ ] Jackson still sees about 30 fps in his editor, dropping to about 9 fps on knockdowns, with no errors logged; not reproduced by the agent. Next check: a standalone development build, and the frame numbers from Jackson's session.
- [x] Replan agreed (Jackson, 2026-11-04): the phases below.

The candidates, from the old Phase 6:
- Final arena with uneven terrain and room for towers; the match runs there (today it runs in the Lobby scene)
- Camera collision with walls; audio
- Juice: shake, particles, squash and stretch, count pop; block owner readability (A-013); settle look (A-007); sauce looks (A-009)
- Feel tuning pass (aim, sensitivity, throw speed, settle, knockdown and glue numbers), now with bots to test against
- Spectator: join any live room without a player and fly freely; Quantum replays through the same camera for promo capture
- **Done when** (old Phase 6): a new player finishes a round without explanation.

### Phase 7: Arena (waits on Jackson's graybox, A-008)
- [ ] Import Jackson's graybox: visual meshes, simple `COL_` collision meshes as Quantum static colliders, spawn and vat markers
- [ ] Matches move from the Lobby to the arena: menu, HUD, dev keys, vats, spawns; the demo match behind the menu plays there
- [ ] Camera collision with walls
- **Done when:** a full 6-player match plays in the arena, and the camera never looks through a wall.

### Phase 8: Feel and juice
- [ ] Tuning pass with bots: aim, sensitivity, throw speed, settle, knockdown and glue numbers, and the open questions below
- [ ] Juice: shake, particles, squash and stretch, count pop
- [ ] Owner readability (A-013), settle look (A-007), sauce looks (A-009)
- [ ] Audio from a temporary CC0 pack (Jackson picks it, A-014)
- **Done when:** a new player finishes a round without explanation.

### Phase 9: Playtest build
- [ ] A Windows standalone build, profiled on its own; the distributable later goes to Steam for private playtests
- [ ] Every match saves a replay (Quantum's replay file), viewable later through the spectator camera; spectator free flight if it fits
- [ ] Mouse and controller checked by hand; Quantum's stats overlay off
- **Done when:** people outside the project play a round from the build, and their matches can be replayed.

### Later: Bot Enhancement
- [ ] Skill levels (a difficulty setting) and personalities: aggressive, defensive, clumsy, and others (Jackson, 2026-11-04)

### Later: Steam
- [ ] Steam integration (store page, Steamworks), once the build has been playtested

### Later: Expansion (ongoing)
- [ ] Team modes and roles, new win rules, more sauces; bot tactics for them (Bot SDK utility AI)
- [ ] Matchmaking polish
- Each new mode is a config plus rule hooks.

## Decisions

Current decisions only; how they were reached is in git history.

- **Engine:** Quantum 3.1.0 Preview build 2189, pinned; URP; Generic rigs (keeps bone scale for squash and stretch). 3.1 is a preview SDK; revisit when it goes stable.
- **Bots** use Photon's Quantum Bot SDK 3.10.6 (`Assets/Photon/QuantumAddons/QuantumBotSDK`, pinned like Quantum): its decision layer (state machine first, utility AI once there are real tactical choices) drives our own action code, and bots send the same input a player does (Jackson, 2026-11-03). Its compiled AI assets go to `Assets/QuantumUser/Resources/DB`.
- **Scale:** every FBX imports at scale factor 0.4 (Guy about 1.62 m, block 0.8 m); Jackson keeps authoring at the current Blender scale.
- **Guy's clips** arrive as one animated FBX (mesh, skeleton, all actions).
- **Towers** can be built anywhere; any hit during settling restarts it; rivals' blocks can be grabbed, thrown and placed on your tower.
- **Stacking:** no locking. Blocks stay physical and grabbable and only count for the score. Counted blocks at rest are put to sleep (performance only). Blocks keep a plain box collider with restitution 0. Placing over another block squares it to that block and lifts it out of any overlap (toggle: Settle Placed On Block Below on `PlayerGrabConfig`). Placement is otherwise free: no snapping.
- **Grab:** the nearest free block within 2 m in a 60° cone in front of the character. It turns the least it can to sit level and square to Guy: the face nearest facing up stays on top, and of the faces beside it, the one nearest Guy faces him (one of a cube's 24 square orientations). Placing levels it the same way, onto whichever face is down, so you choose the top face by how the block lies when you pick it up (Jackson, 2026-11-03). A rotate button waits until this has been played.
- **Holding and camera:** see Architecture › Holding a block. The camera never locks while holding: it follows the swing on a gradient (Jackson, 2026-11-02).
- **Throwing** is hold-to-aim with a trajectory line and landing circle; a jumpshot aims the same way at 1.3× speed. One clip, `Throw`, covers both: frame 4 is the aim pose, the rest is the follow-through (Jackson, 2026-11-02). No place clip. While aiming, the real block collides with nothing and its model arcs over the head (Jackson, 2026-11-02).
- **Win and lose:** after a round the winner plays `Win.Jump` and everyone else `Sitdown`, through round end and results (Jackson's placeholders, 2026-11-02).
- **Block ownership readability** is rethought at the top level in the visual polish after the plan review, likely a personal UI that highlights your own blocks (Jackson, 2026-11-02).
- **Menu and rooms** (2026-11-03): the menu is an overlay in the scene the match runs in (the Lobby until the arena exists), over an offline demo match (`RuntimeConfig.DemoMatch`: bots in every slot, matches start by themselves) watched by the spectator director. Start Game hosts a public room under a 5-letter consonant code; Join Game by code only joins (never creates); Join Any Room joins a random open room. In a room, the room screen is the menu between matches: it opens on entering and after each match's results, and closes as a match starts; Escape or the gamepad's Select opens it in the lobby, or Pause during a match (Start stays the match start). The host is the connected player in the lowest slot; only the host sees the room's controls (start, bots, fill). Bots that filled empty slots stay in the room after the match.
- **Players: 2–6** (Jackson, 2026-11-03; kept 2026-11-04): cut from 8 when 8 ran at about 40 fps, most likely because of the per-tick exception fixed on 2026-11-04, so 8 is technically possible again; the two blues left the palette. `max_players` in `Input.qtn` sets the session and room size. The Lobby's ring of 8 spawn points is shared out evenly among the 6 slots.
- **Optimization** (Jackson, 2026-11-03): a profiling and optimization pass is part of the plan review that follows the menu build.
- **UI fonts** (Jackson, 2026-11-04): Jaro, the logo's first line, for big display text (the countdown, RESULTS, the winner); DynaPuff, the logo's second line, for everything read closely (buttons, names, room codes, prompts, hints). Jaro's V reads as U.
- **Bots** play one skill level for now; skill levels and personalities come in the later Bot Enhancement phase (Jackson, 2026-11-04).
- **Phase order** (Jackson, 2026-11-04): Phase 7 arena, Phase 8 feel and juice, Phase 9 playtest build (with replays), then Bot Enhancement and Steam. Phase 7 waits on Jackson's graybox map.
- **Movement input:** a gamepad stick's tilt sets speed from slow walk to full run; the keyboard walks, with Shift to run. The gamepad Start button starts a match; only Escape opens the menu.
- **Sit replaces crouch** (Jackson, 2026-11-02): no movement while sitting except sliding; a sitting body moving fast plays the slide.
- **Knockdown:** see Architecture › Knockdown. Numbers live on `PlayerKnockbackConfig` and `PlayerRagdollConfig` and stay open for tuning.
- **Feel numbers** (aim angle, sensitivity, throw speed) stay as built until the tuning pass after the plan review.
- **Sauce strength:** 0–50% dipped maps to 0–100% of the effect. Sauce visuals beyond readability wait for the visual polish after the plan review (Jackson, 2026-11-02).
- **Vats** sit in the middle of the arena, contested by everyone.
- **Workflow:** the agent commits; Jackson pushes `main` to GitHub. `Assets/Art/_Incoming/` is Jackson's only drop-off, under any file name. Work pauses between phases while art catches up.

## Open questions

- Settle feel: 3 s to count; a 1 m/s touch restarts settling; stacked means within half a block of center and 12° of level. Right numbers? (Play-test.)
- Should a pickup swing that passes through your own tower be able to knock it over, or should the grab ignore your own blocks? (Play-test.)
- Stack glue: how strong? Built so walking, running and ragdolls can't break a full-strength Stack tower and a throw can (`SauceConfig` glue speeds). Should Stack blocks also glue to the ground, as built, or only to other blocks?

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
