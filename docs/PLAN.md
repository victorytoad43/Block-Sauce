# Block Sauce: plan

Source of truth for design, architecture, decisions and phases. Older plan versions, finished phase checklists and how decisions were reached live in git history.

## Overview

Block Sauce is a third-person online physics party game for 2–6 players: build your tower, wreck everyone else's. There is no health and no death. Players only get knocked around, and stacks only fall when something hits them. Every feature should either help a player build or help them wreck; sauces tilt a block toward one or the other.

**Player verbs:** walk/run, jump, sit, slide (sit while moving fast), grab, carry, hold and swing, place, throw, jumpshot (throw while airborne). A held block is available in every movement state.

**Stack:** Unity 6000.3.14f1 + URP 17.3 (view, animation, VFX); Photon Quantum 3.1.0 Preview build 2189 (deterministic simulation and networking), pinned; Quantum Bot SDK 3.10.6, pinned; Blender 5.1 (Guy's model, rig and animation).

## Architecture

Anything that decides who wins lives in the Quantum simulation; anything that only changes how it looks lives in the Unity view. Only player input crosses the network; Unity reads state and events and never writes back.

| Quantum simulation (deterministic, rolled back) | Unity view (local, cosmetic) |
|---|---|
| Player: force-driven capsule, control tiers, grab, swing, throw, knockback | Animation: locomotion blends, upper-body hold layer, IK |
| Blocks: bodies, ownership, stack detection, settle progress, counting | Ragdoll pose on Guy's bones, get-up blends, dazed indicator |
| Game mode: match flow, rules, win check, team IDs | Shaders and VFX: settle dither, sauce coat |
| Knockdown: 11-limb `QuantumRagdoll`, timers, get-up placement | Camera (orbit, spectator director), HUD, menus, audio from simulation events |
| Sauces: dip planes, sauce type, friction and glue | Dev only: bot labels, `BotSoakLog` |
| Bots: navmesh paths, actions, Bot SDK state machine, skill and personality | |

Reference implementation: Photon's Friendslop sample (grab, carry, throw, force-driven movement, ragdolls, stacked props). Port systems from it; never its first-person camera or input layout.

### Character

- **Sit and slide:** the crouch button sits. A sitting character is stationary: no steering, turning or jumping, it only brakes (a shove still slides it). Pressing it while moving 4 m/s or faster slides instead (7.5 m/s for 0.75 s), ending in a sit while the button is held. The capsule is 1.30 m tall while sitting or sliding.
- **Control tiers:** in control (forces chase the stick's target velocity), staggered (no steering, light braking, drops the held block), knocked down (capsule off, ragdoll limbs take over and topple towers).
- The view reads speed, direction, grounded and state, and plays Blender clips on top. Root motion stays off; animation follows the body. Timed moves (slide length, throw release, get-up offset) are simulation numbers sized to the authored clips.
- Foot IK (feet planted per foot on statics and blocks, hips dropping up to 0.35 m), hand IK and a head look live entirely in the view. The head look (`HeadLookView`, Jackson 2026-11-06): with empty hands, Guy's head (a little of his chest, and his eyes) turns toward the block a Grab press would take right now, so everyone can see what a player or bot is about to pick up. A stagger plays a full-body flinch for the side that was hit.

### Holding a block

- **Grab** takes the free block within 2 m of a pivot inside Guy (0.77 m up) that the camera looks at: the first block on the camera's center line (which runs through the camera's pivot 1.4 m above Guy's feet, along the look's pitch and yaw), or else the block in reach whose center lies nearest that line, within 0.6 m; with nothing near the line, the nearest free block in a 60° cone in front (Jackson, 2026-11-06; the nearest-block rule stays behind `PlayerGrabConfig.GrabWhereAimed`). It turns the least it can to sit level and square to Guy. Bots look at the block they mean to grab.
- While holding, look input swings the block instead of turning the camera. The block orbits the pivot at the `GrabBlock` clip's hold point (0.95 m out), level and square to him. Left/right on the left stick strafe. The block chases its hand point by velocity each tick, so a swing physically hits players and towers. A carrier never collides with, or is knocked by, the block it holds.
- The camera always moves with look input while holding, on a gradient: 35% of the input with the block in the middle of its swing, rising to all of it at the edges. Past 45° sideways the body turns; past the top (60°) or bottom (−25°) of the swing, input only tilts the camera.
- Holding Throw aims: the block rises to 30°, horizontal input turns the body, vertical input sets the throw angle, and a trajectory line and landing circle show the throw. Releasing throws (12 m/s; a jumpshot is 1.3×). Grab while holding places. While aiming the real block collides with nothing (`AimedBlock` layer) and its model rides in Guy's hands (`Throw` frame 4), arcing back to the block on release.

### Knockdown

- One knockback path (`KnockbackSystem`). A speed change of 2.5 m/s staggers; 6 m/s knocks down (a full throw or jumpshot, a hard swing, any fast block). A running body check (4.5 m/s) on someone staggers them; on someone already staggered it knocks them down.
- Down for 2.5 s; each Jump press takes 0.3 s off. Bodies can be carried and break free after 2.5 s. Get-up stands Guy at the hips, on the nearest clear spot.

## Base game mode: Stack & Lock

A player wins the round when all three of their blocks stand stacked, and counted, in one tower.

1. Each player owns 3 blocks in their color. Towers can be built anywhere.
2. Blocks are always ordinary physics bodies: nothing is frozen or locked, and any block can be grabbed at any time.
3. A block is stacked when it rests level and squarely on another block of its owner (within half a block of center and 12° of level), or is a ground block with one of its owner's blocks on top. A lone block on the ground isn't stacked.
4. A stacked block at rest settles: a dither in the owner's darker shade climbs it over 3 s, and any touch at 1 m/s or more restarts it. At the top it counts, for as long as it stays stacked.
5. A block that stops being stacked stops counting, and so does any block that was only stacked because of it.
6. The first player whose tower holds 3 counted blocks wins. A match has a 5-minute clock (`GameModeConfig.MatchSeconds`); when it runs out, sudden death: the next block to count wins. If none counts within 2 more minutes (`SuddenDeathSeconds`), the player with the most counted blocks in one tower wins, and a tie is a draw. The HUD shows the clock in the headline.

**What stacking should feel like** (Jackson, 2026-11-06): two blocks stack easily; the third is hard because you can't reach up to it (a jump-place). The difficulty is reach and timing, never friction: a block placed squarely stays put, and towers fall when something hits them, not by sliding on their own.

**Interference:** anyone can grab and throw any block and place blocks on a rival's tower. A foreign block on top is an obstacle the owner's next block can't count on until it is knocked off.

**Arena start layout:** each player starts on their pillar with one block in front of them (their home block); their second block sits on the inner ring across the middle; the third is in a pile of six in the middle. Map markers (`BlockSpawnPoint`) set it; a map without markers gives each player a row of blocks.

**Built as a baseplate for future modes:** match flow is a state machine (Lobby → Countdown → Playing → Round end → Results → Lobby); the lobby is a sandbox with the win rule off; a mode is a config asset (`GameModeConfig`) with rule hooks (`OnMatchStarted`, `OnBlockCounted`, `OnBlockUncounted`, `CanCount`, `CheckWinner`); every player and block carries a team ID (free-for-all is teams of one).

## Sauce system

Each dip stores a plane in the block's local space; the shader turns those planes into a thick, lumpy coat that changes the block's silhouette as much as its color. The two sauces are opposites, both about grip: Stack is sticky, Slip is slippery. Sauces live in the Lobby test scene for now (the arena has no cups, Jackson 2026-11-05).

- **Vats:** Jackson's cup (`EN_SauceVat`) at 1.3× (1.94 m across, rim 0.61 m, above Guy's 0.5 m step). Collision follows the cup (24 leaning wall boxes and a floor on the `Vat` layer, which the hold's line-of-sight ignores, so a held block can be pushed in). Sauce fills to 0.55 m.
- **Simulation (`SauceSystem`, tuning on `SauceConfig`):** a block reaching into the sauce records the surface as a plane in block-local space with the sauce type (up to 3 dips; a fourth replaces the shallowest; under 5% leaves nothing). Strength = deepest dip's fraction ÷ 0.5, capped at 1.
- **Stack sauce:** high friction in four steps by strength (1.5 to 4; plain 0.8, ground 0.6), and it glues to whatever it touches while nearly at rest (a stiff joint bending at most 2°). Glue tears when hit at 3 m/s (lowest strength) to 7 m/s (full) into the contact, when either block is grabbed, or when pulled 10 cm apart; torn blocks glue again where they rest. A glued tower stands up to shoves and ragdolls; a throw breaks it.
- **Slip sauce:** low friction in four steps (0.6 to 0.03), so the block slides and what is stacked on it slides off.
- **View:** coverage below each dip plane with a ragged noise edge seeded by the entity ID; the coat is its own rounded-box shell pushed out 1.9 cm plus lumps up to 3 cm. Stack is dark and warm with bubbles, Slip pale and glossy with glints (`PH_SauceLooks`). Drips off for now.

## Bots

A bot takes a free player slot and its brain writes that slot's input, so every system treats it as a player. The host adds and removes bots on the room screen; bots fill empty slots at a match start (on by default). The menu's demo match is all bots.

- **Navigation:** a navmesh baked into each map (Unity bakes for Guy's size, Quantum imports it at map bake), with jump links onto ledges (ring edges, pillar rims, stairs). Bots look up a path a few times a second, head for the next corner, jump at links, and dodge players and blocks with probes. No navmesh (the Lobby): straight-line steering. **Block Sauce › Bake Arena NavMesh** rebakes it.
- **Actions** (`BotActions`, our code): go to, fetch (including a jump-grab for a block lying on a tower), carry, place on a tower (a jump for tall ones), throw at a target, dunk, slide tackle, get up. **Decisions:** a Bot SDK state machine (`BotBrainBuilder`: fetching, building, raiding, dunking, tackling, recovering) around scored choices in `BotChoices` (build, raid, clear its top, what to throw at, whom to tackle).
- **Skill** (host sets it per bot on the room screen; new and fill bots take the last level set; the demo rolls it every match): `BotSkill_Easy`, `_Normal`, `_Hard`, `_Expert` assets: hesitation, raid count, running with a block, grab and place attempts, placement error, aim tolerance and error, tackle distance, line-up and lead.
- **Personality** (rolled when a bot is added, and every match in the demo; hidden from players, shown in dev builds): `BotPersonality_Wrecker` (raids early, throws at carriers, tackles anyone carrying and rivals at their towers), `_Builder` (raids only a rival about to win, clears its top, guards its tower, dunks in Stack more), `_Klutz` (plays like the default bot and makes mistakes: trips, wild slides, drops, off-center placings, wild throws; raised 2026-11-06).
- **Purpose** (Jackson, 2026-11-06): bots add variety to a room that's missing players. They don't need to be balanced against each other; balance work is for play with humans.
- **Dev tools:** the spectator label and the room screen add a bot's personality and skill (editor and development builds only); `BotSoakLog` writes a `[BotSoak]` line per match with per-player stats; test switches (`BotSystem.TestBotCount`, `BotConfig.TestSkillBySlot`, `TestPersonalityBySlot`, `BotChoices.TestDisableTackles`, `BotNavigation.TestDisable`) drive soaks from editor scripts.

## Phases

Done and reviewed: **0** Foundations, **1** Sandbox lobby, **2** Stack & Lock, **3** Knockdown, **4** Character, **5** Sauces, **6** Front end and bots, the plan review (optimization pass, frame rate pinned: judge performance in builds), **7** Arena, **Bot Enhancement** (navmesh, slide tackles, skill levels, personalities, dev view and soak stats, the bot-soak review; reviewed 2026-11-06). Their checklists and reports are in git history. Bots are set aside for now (Jackson, 2026-11-06): no bot work in the next phases unless a change elsewhere breaks them.

### Blocks: stacking that behaves (built 2026-11-06, waiting for Jackson's hand check)
Jackson (2026-11-06): adding a third block to a two-block tower often makes all three slip against each other, slide around and topple. It's hard to predict and makes stacking three tedious instead of suspenseful. The goal is the feel in Stack & Lock › What stacking should feel like: keep the easy two and the hard reach for three, and remove the inconsistent friction and random sliding.
- [x] **Block bench:** `Assets/Scenes/Dev/BlockBench` (Block Sauce › Build Block Bench) and `BlockBenchSystem` run scripted placings through the real place code; Block Sauce › Block Bench › Run … panels for placing, carrying, hits and sauce (`CLAUDE.md` › Testing).
- [x] **Causes found** (numbers in `docs/PROGRESS.md` › Blocks report): (1) counted blocks were put to sleep the moment they were still, and a block landing on a sleeping tower woke it with no contact history, so the tower slipped; (2) even awake, a slightly uneven stack of three boxes creeps and rocks until it slides apart, and nothing in the solver damps it; (3) a held block pressed into a tower is driven into it at up to the hold's follow speed and shoves it apart before it's let go. Not the cause: the release (centered placings never slid), the squaring, drop height, turn, the solver's iterations, penetration correction or Baumgarte (no better, or worse), angular drag.
- [x] **Fixes:** counted blocks no longer sleep early (`GameModeConfig.SleepCountedBlocks` off; the engine's own sleep stays); a rest assist (`BlockRestSystem`, `GameModeConfig.RestAssist`) takes half of any slow creep or rocking (under 0.25 m/s and 1 rad/s relative to what a block rests on) off each tick, skipping Slip-sauced blocks; a held block's hand point stops at the height it would rest on a block below it (`PlayerGrabConfig.HoldRestsOnBlockBelow`). No locking, snapping or joints.
- [x] **Feel kept (bench and soak):** hits topple towers as often as before; Slip towers fall to light hits that plain and Stack towers shrug off; jump-placing the third block is untouched.
- [ ] Jackson stacks three by hand (Lobby or a match) and the difficulty comes from reach, not sliding.
- **Done when:** on the bench, a block let go within the stack tolerance on a two-block tower stays put (under 2 cm of slide, never a fall) in every repeated trial, asleep or awake, and towers hit by a throw or a ragdoll still fall as before; Jackson stacks three by hand and the difficulty comes from reach, not sliding.

### Touch-ups: grab and match end
Jackson's picks (2026-11-06). The two highlights wait on Jackson's detailed design.
1. [x] **Grab what the camera looks at** (2026-11-06): `GrabSystem.FindGrabTarget`; bots aim their camera at their target block; the old rule behind `PlayerGrabConfig.GrabWhereAimed`. Plus Jackson's ask: Guy's head looks at the block a grab would take (`HeadLookView`).
2. **Grab highlight** (Jackson's design): the block Grab would take, shown for the local player only (view, reads `PeekGrabTarget`).
3. **Settle and stack highlight** (Jackson's design): where the held block will land, and whether it would stack and count there (view only, from `BlockStackSystem`'s center and level tests; it predicts and never decides).
4. [x] **Match timer and sudden death** (2026-11-06): as in Stack & Lock rule 6; the HUD's headline shows the clock, "SUDDEN DEATH!" shows big as it starts, a timed-out tie shows DRAW. The sudden-death rule is a proposal for Jackson's play-test.
- **Done when:** a player lifts a block off any tower they can reach with one press by looking at it; both highlights follow Jackson's design; no match runs past sudden death by more than a minute in soaks; Jackson plays a room and signs off the grab rule.

### Phase 8: Feel and juice
- [ ] Tuning pass in play with humans (bots fill the room): aim, sensitivity, throw speed, settle, knockdown and glue numbers, and the open questions below
- [ ] Juice: shake, particles, squash and stretch, count pop
- [ ] Owner readability (A-013), settle look (A-007), sauce looks (A-009)
- [ ] Audio: listen to the placeholder picks; Jackson's sounds as they arrive (A-014)
- **Done when:** a new player finishes a round without explanation.

### Phase 9: Playtest build
- [ ] A Windows standalone build, profiled on its own (`-logframes`); the distributable later goes to Steam for private playtests
- [ ] The small hiccup when someone is knocked down (most likely creating the ragdoll's entities and view; pool them if so); a lighter block mesh or fewer shadow cascades if lower-end PCs struggle
- [ ] Every match saves a replay (Quantum's replay file), viewable later through the spectator camera; spectator free flight if it fits
- [ ] Mouse and controller checked by hand; Quantum's stats overlay off
- **Done when:** people outside the project play a round from the build, and their matches can be replayed.

### Later: Steam
- [ ] Steam integration (store page, Steamworks), once the build has been playtested

### Later: Expansion (candidates from the review; Jackson picks)
- **Wrecking that pays** (for human players; the bots' Wrecker is fine as it is, Jackson 2026-11-06). In free-for-all, knocking a leader down helps everyone else as much as you. Ideas: a block you knock off a counted tower becomes yours to place (a capture); or knocking blocks fills a meter for a sauce or a power throw; or a round score across several rounds (towers built plus towers wrecked).
- **Sauce in the arena:** cups back in the middle as a contested spot (the plan's original idea), or sauce pickups; sauces are the game's build-or-wreck choice and the arena doesn't have them.
- **More maps:** a new layout brings new strategies (Jackson, 2026-11-06, on towers ending up beside the pillars); the arena's builder and navmesh bake already make a map a data change.
- **Two-player rooms:** head-to-heads hardly interact; fewer blocks, one shared pile, or a smaller map for 2–3 players.
- **Team modes and roles, new win rules, more sauces;** bot tactics for them (Bot SDK utility AI); matchmaking polish. Each new mode is a config plus rule hooks.

## Decisions

Current decisions only; how they were reached is in git history.

- **Engine:** Quantum 3.1.0 Preview build 2189, pinned; URP; Generic rigs (keeps bone scale for squash and stretch). 3.1 is a preview SDK; revisit when it goes stable.
- **Bots** use Photon's Quantum Bot SDK 3.10.6 (`Assets/Photon/QuantumAddons/QuantumBotSDK`, pinned): its decision layer drives our own action code, and bots send the same input a player does (Jackson, 2026-11-03). Its compiled AI assets go to `Assets/QuantumUser/Resources/DB`.
- **Bot skill and personality** (Jackson, 2026-11-04): four skill levels (Normal is the Phase 6 bot), set per bot by the host; three personalities, rolled at random and never shown to players. Personalities weight choices scored in our own code (`BotChoices`); the Bot SDK's utility AI waits for team modes.
- **Bot navigation** (Jackson, 2026-11-05): a navmesh baked into the map, rebaked from a menu when the map changes. **Bot slides** (Jackson, 2026-11-05): slide tackles to make a carrier drop their block are a key move.
- **Bots after review** (Jackson, 2026-11-06): the current tackle rate, the navmesh and long matches with Wreckers are fine; bots are for variety, not balance; Klutz mistakes raised; bots set aside while the core loop is worked on.
- **Scale:** every FBX imports at scale factor 0.4 (Guy about 1.62 m, block 0.8 m); Jackson keeps authoring at the current Blender scale. **Guy's clips** arrive as one animated FBX.
- **Towers** can be built anywhere; any hit during settling restarts it; rivals' blocks can be grabbed, thrown and placed on your tower.
- **Stacking:** no locking. Blocks stay physical and grabbable and only count for the score. Blocks keep a plain box collider with restitution 0. Placing over another block squares it to that block and lifts it out of any overlap, and a held block over a block rests on it rather than being pressed in (toggles on `PlayerGrabConfig`). No snapping. A rest assist damps slow creep and rocking of resting blocks (not Slip ones), and counted blocks aren't put to sleep early (toggles on the mode config; block bench, 2026-11-06).
- **Grab:** today the nearest free block in range and cone; the face nearest up stays on top, so you choose the top face by how the block lies when you pick it up (Jackson, 2026-11-03). A rotate button waits until this has been played. Next: the block the camera looks at, with a grab highlight and a settle and stack highlight to Jackson's detailed design (Jackson, 2026-11-06).
- **Match end:** a 5-minute timer, then sudden death (Jackson, 2026-11-06).
- **Arena start layout** stays: towers ending up beside the pillars is welcome emergence; new maps will bring new strategies (Jackson, 2026-11-06).
- **Holding and camera:** the camera never locks while holding; it follows the swing on a gradient (Jackson, 2026-11-02).
- **Throwing** is hold-to-aim with a trajectory line and landing circle; a jumpshot aims the same way at 1.3×. One clip, `Throw`: frame 4 is the aim pose. No place clip. While aiming, the real block collides with nothing (Jackson, 2026-11-02).
- **Win and lose:** after a round the winner plays `Win.Jump` and everyone else `Sitdown`, through round end and results (Jackson's placeholders).
- **Block ownership readability** is rethought at the top level in the visual polish, likely a personal UI that highlights your own blocks (Jackson, 2026-11-02).
- **Menu and rooms** (2026-11-03): the menu is an overlay in the scene the match runs in (the Arena), over an offline demo match watched by the spectator director. Start Game hosts a public room under a 5-letter consonant code; Join Game by code only joins; Join Any Room joins a random open room. The room screen is the menu between matches. The host is the connected player in the lowest slot; only the host sees the room's controls. Bots that filled empty slots stay in the room after the match.
- **Players: 2–6** (Jackson, 2026-11-03; kept 2026-11-04). `max_players` in `Input.qtn` sets the session and room size.
- **UI fonts** (Jackson, 2026-11-04): Jaro for big display text; DynaPuff for everything read closely. Jaro's V reads as U.
- **Phase order** (Jackson, 2026-11-05; updated 2026-11-06): Blocks (stacking that behaves, built), Touch-ups (grab and match end), then Phase 8 feel and juice, Phase 9 playtest build (with replays), then Steam.
- **Arena** (Jackson, 2026-11-05): Jackson's graybox as delivered, with the pillar / ring / center-pile start layout; no sauce cups for now (sauces stay in the Lobby test scene and on the dev keys).
- **Audio** (Jackson, 2026-11-05): placeholder sounds from a CC0 pack (FreeSFX), picked by file name; real sounds replace them in the sound bank.
- **Movement input:** a gamepad stick's tilt sets speed from slow walk to full run; the keyboard walks, with Shift to run. The gamepad Start button starts a match; only Escape opens the menu.
- **Sit replaces crouch** (Jackson, 2026-11-02): no movement while sitting except sliding.
- **Knockdown, feel and sauce numbers** live on their config assets (`PlayerKnockbackConfig`, `PlayerRagdollConfig`, `PlayerGrabConfig`, `SauceConfig`) and stay open for the Phase 8 tuning pass. Sauce strength: 0–50% dipped maps to 0–100% of the effect.
- **Performance** is judged in builds, not Jackson's editor (2026-11-04).
- **Workflow:** the agent commits; Jackson pushes `main` to GitHub. `Assets/Art/_Incoming/` is Jackson's only drop-off, under any file name. Work pauses between phases while art catches up.

## Open questions

- Sudden death: next block to count wins, then most counted blocks after 2 more minutes? (Play-test.) A sudden-death winner can have fewer counted blocks than others, and the results list them first anyway; say it on the results ("WINS IN SUDDEN DEATH")?
- Pace: with towers that stand, all-bot matches got much shorter (median 47 s, from 104 s). Is the third block now too easy for players, or is the jump-place enough? (Jackson's hand check.)
- Settle feel: 3 s to count; a 1 m/s touch restarts settling; stacked means within half a block of center and 12° of level. Right numbers? (Play-test, after the Blocks phase.)
- Should a pickup swing that passes through your own tower be able to knock it over, or should the grab ignore your own blocks? (Play-test; the camera-aim grab may settle it.)
- Stack glue: how strong, and should Stack blocks glue to the ground or only to other blocks?

## Art pipeline

**Drop zone:** `Assets/Art/_Incoming/`, any file name. The agent renames, moves the file to `Assets/Art/<Category>/`, and copies a re-export over the existing asset so references survive. `Assets/Scripts/Editor/ArtImportPostprocessor.cs` applies import settings to everything under `Assets/Art/`: scale 0.4, Generic rig, no cameras or lights, bone-scale curves kept, clip names stripped to the action name, and clips named Idle/Walk/Run/Loop/Pose/Hold looping.

**Source files:** `C:/Users/victo/Desktop/Games/BlockSauce/modelling/` (outside the repo). Only exported FBX/PNG files enter the project.

**Naming:** `<Prefix>_<Name>[_<Variant>]`. Prefixes: `CH_` character, `BL_` block, `EN_` environment, `UI_`, `FX_`, `SFX_`, `MUS_`. Placeholders are `PH_`.

**Models (Blender → FBX):** metric, unit scale 1.0, transforms applied, origin at the intended pivot, characters facing -Y in Blender. Export Forward -Z, Up Y, Apply Unit on, Apply Scalings FBX All. One material per logical surface, named as it should appear in Unity.

**Guy:**
- Generic rig with a `root` bone at the origin; 31 deform bones, which map onto the ragdoll (`hips`, `chest`, `head`, arm and leg segments). The `prop` bone marks the hold point. **Block Sauce › Build Guy Ragdoll** rebuilds `CH_Guy_Ragdoll` when his proportions change.
- Export with the collection exporter in `guy.blend`: `armature_guy` + `mesh_guy` only; Only Deform Bones on; Add Leaf Bones off; Apply Modifiers on; Bake Animation with All Actions, NLA Strips off, Force Start/End Keying on.
- Clips: in place (no root motion), clean loops, 24 fps is fine, at least two keyframes per action, a fake user on every action worth keeping. Leave the scene on idle or rest when exporting.

**Textures:** PNG, power-of-two. Color maps sRGB; masks, roughness and normals linear.

**UI:** mock-ups as PNG or a Figma link; final art PNG at 2× with 9-slice margins noted; fonts TTF/OTF with license confirmed.

**Audio:** WAV 48 kHz; mono for 3D sounds.
