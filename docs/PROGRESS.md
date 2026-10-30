# Block Sauce: progress log

The agent adds an entry at the top for each work session, and a phase report when a phase's done-when criteria are verified.

## Session entry format

```
## YYYY-MM-DD: Phase N, <short title>
- Did: <what was built or changed>
- Decided: <routine engineering decisions made without asking>
- Assets: <requests added, deliveries integrated>
- Blocked / questions for Jackson: <or "none">
- Next: <what the next session starts with>
```

## Phase report format

```
## Phase N report: <name>
- Built: <summary>
- Done-when verification: <each criterion and how it was checked>
- Known issues: <list>
- Placeholders still in use: <list with asset IDs>
- Ready for review: yes
```

---

## 2026-10-29: Phase 3 prep (plan and asset pass; build starts when Jackson reprompts)

- Jackson moved on from Phase 2 and asked for Phase 3 prep while art catches up. Git workflow from now on: the agent commits; Jackson pushes at milestones.

### Phase 3 plan: Knockdown

Build order:
1. **Ragdoll tier in the simulation.**
   - Port Friendslop's `Ragdoll.qtn`, `RagdollSystem` and `Ragdoll.User.cs` as a third control tier after InControl and Staggered.
   - `Ragdollable` (config) on the player. `RagdollOwner` (timer, body, anchor) is added while down.
   - Knocking down disables the capsule's body and collider and spawns the ragdoll group in the character's pose with its velocity; the hidden character rides the hips.
   - Getting up destroys the group and stands the character where the hips lie.
   - `KnockbackSystem` picks the tier: a speed change below the stagger threshold just pushes, a mid one staggers (as now), and a big one knocks down. All of it stays on the one knockback path. A downed character drops what it holds and can't grab, move or throw.
2. **Guy's ragdoll.**
   - Build `CH_Guy_Ragdoll` from the deform skeleton already in `CH_Guy.fbx`, with Quantum's `QuantumRagdoll` component. Its Animator field stays empty and the bones are assigned by hand (Generic rig).
   - About 11 limbs: hips, chest, head, upper arm and forearm (with the hand), and thigh and shin (with the foot), each side.
   - Bake it into an entity prototype group. It replaces `PH_RagdollTest`.
3. **Limbs that sleep.** The Phase 1 stress test showed limbs never sleeping. Investigate in this order: the joint limits and solver settings that keep them twitching, limb damping, and, if still needed, the same trick counted blocks use (put the whole body to sleep once every limb is under the engine's sleep tolerances).
4. **Limbs vs blocks.** The limbs are ordinary bodies, so they already topple stacks and restart settling blocks through the Phase 2 code. Check this rather than build it.
5. **Get-up.**
   - Stand the character at the hips, facing the hips' forward direction on the ground plane.
   - Push the standing capsule out of any block it would overlap.
   - Add a short grace time after getting up, during which a hit can't knock the character down again (pending Jackson's answer).
   - Until the A-015 clips land, the view snaps Guy back to standing with a squash-and-stretch pop.
6. **View.**
   - While down, Guy's bones follow the simulated limbs (Quantum's ragdoll limb views), smoothed so remote ragdolls don't snap when corrected.
   - The camera follows the hips.
   - On get-up, the view blends from the last ragdoll pose into the get-up clip.
7. **Falling out.** Port Friendslop's `FallSystem` (characters return to their last grounded position, blocks to a safe spot) through a mode hook, since ragdolls can be launched over walls.
8. **Profile.**
   - Knock all 8 players down at once beside 24 blocks with 8 online clients (editor plus 7 development builds, as in Phase 1).
   - Record the cost per frame and the rollback cost.
   - Levers, in order: sleep, a shorter knockdown, fewer limbs, prediction culling around the local player.

Risks:
- Rollback cost: the Phase 1 stress test measured 3–4.4 ms per frame in the editor with 8 ragdolls that never slept.
- Joint stability with fixed-point physics.
- Get-up placement inside geometry.
- Remote ragdolls snapping on corrections.
- Knockdown thresholds are a feel call for Jackson (questions in PLAN.md).

Art: Phase 3 isn't blocked. The skeleton it needs is already delivered; only the get-up clips (A-015) are new art, with a placeholder pop until they arrive.

## 2026-10-29: Phase 2 revision after Jackson's play-test (stacking replaces locking)

- Jackson's play-test feedback:
  - Pressing Start to start a match also opened the menu.
  - Blocks that weren't stacked got locked.
  - Locked blocks were immovable.
  - What he asked for: no locking physics; blocks stay grabbable and only count for the score; the only physics change is a simplification for stacked blocks, for performance.
- Did:
  - **Start button:** `BlockSauceStartUI` (subclass of the SDK's `QuantumStartUI`, swapped in on Lobby and Arena_Graybox) toggles the menu with Escape only, so the gamepad Start button only starts a match. It's a subclass rather than an SDK edit, so SDK updates don't undo it.
  - **Stacking:** `BlockLockSystem` is replaced by `BlockStackSystem`. Each tick of play, every owned block that isn't held and has a face pointing up (within 12°) looks straight down. On static ground it's a ground block; on a block of the same owner, centered within half a block, it sits on that block. Following supports down gives each block a level and a base. A block is stacked at level 1 or above, or as a ground block with an owned block on top, so a lone block never counts.
  - **Counting:** a stacked block at rest settles over 3 s (the dither) and then counts. A touch at 1 m/s or more restarts a settling block. A block that stops being stacked stops counting. Nothing is frozen and every block can be grabbed. The only physics change: a counted block that's as still as the engine's own sleep check is put to sleep at once, skipping the sleep timer; any contact wakes it.
  - **Win:** the base mode's `CheckWinner` picks the first player with 3 counted blocks on one base. Mode hooks are now `OnBlockCounted`, `OnBlockUncounted` and `CanCount`, and the mode asset has `SettleSeconds`/`SettleResetSpeed` instead of the lock and unlock numbers.
  - **Removed:** the "placed" (`Handled`) rule. It's no longer needed, because lone blocks don't count.
  - **HUD:** the slots show each player's best stack. The dev stack command now stacks onto the best stack.
- Verified (play mode, Lobby, online):
  - The spawn row and a block placed alone on the ground stay Loose.
  - Two stacked blocks both settle and count, then sleep.
  - Grabbing the counted top block works, and both blocks go back to Loose.
  - A full match with dev stacking: 3 counted, then RoundEnd (player 1 wins), Results, and Lobby with the sandbox restored.
  - **Two clients:** a rival's block on top of player 1's 2-block stack doesn't count, player 1's two stay counted, and both clients agree.
  - The menu shows Start Game and connects with the new component.
  - Main console clean.
- Not verified: the gamepad Start button itself (no gamepad input can be sent to an unfocused editor). The change is that the menu now only listens to Escape.
- Test-tool note: dropping a 4th block with the dev command onto a slightly off-center rival block toppled the stack. That came from the dev tool teleporting a block onto a block that was still wobbling, not from the stacking rules.
- Next: stop for art (A-001, A-006, A-007, A-013, A-015, and the second keyframe on `RunBasic`/`ThrowPose`); Phase 3 after Jackson's play-test of this revision.

## Phase 2 report: Stack & Lock mode

(Superseded in part by the revision above: locking, freezing, unlock thresholds and the "placed" rule are gone.)

- Built:
  - **Mode config:** `GameModeConfig` (`StackAndLockMode.asset`) holds blocks per player 3, tower height 3, lock time 3 s, rest/tilt/offset tolerances, the unlock threshold, timers and spawn layout. Its virtual hooks are `OnMatchStarted`, `OnBlockLocked`, `OnBlockUnlocked`, `CanLock` and `CheckWinner`; a new mode subclasses it.
  - **Match flow:** a `Match` singleton on a map entity in the Lobby scene runs Lobby → Countdown (3 s, movement frozen, looking allowed) → Playing → RoundEnd (3 s) → Results (6 s) → Lobby. `CommandStartMatch` (Enter or gamepad Start) begins it.
  - **Match start:** the 18 sandbox blocks are removed (their home poses are kept), players move to their spawn points pushed out ×1.8, and each gets 3 blocks in their color and team. **Return:** match blocks go, the sandbox comes back, and players return to the lobby spawns.
  - **Locking (`BlockLockSystem`):** a block that has been placed (picked up since it spawned or was last knocked loose) and rests level, not held, in its owner's next slot fills its lock progress over 3 s. The first block goes on the ground; later ones go squarely on the owner's top locked block, one locking at a time. A hit at 1 m/s or more resets the progress to 0. Locked blocks are frozen (kinematic) and can't be grabbed.
  - **Unlocking:** a striker moving at 4 m/s or more into a locked block, with speed × mass of 8 or more, unlocks it and every locked block above it. That covers throws, swings, and running body checks; walking into a tower or leaning on it doesn't count. A knocked-loose block must be picked up again before it can relock.
  - **Foreign blocks:** a rival's block on top of a tower stops the owner's next block from locking there. It's emergent: the next block's support isn't the owner's top block.
  - **Win:** the base mode's `CheckWinner` picks the first player with 3 locked blocks.
  - **View:** `PH_LockDither` shader (owner color; a 4×4 dither in the darker shade climbs with progress; solid darker shade when locked). `BlockView` feeds it from the predicted frame, `PlayerColorView` tints Guy, and `PH_PlayerColors` is the placeholder palette.
  - **HUD:** placeholder IMGUI `MatchHud` shows the state and countdown, each player's color and 3 slots (empty, filling, locked), the winner, and the results timer.
  - **Dev tool:** `CommandDevStackTower` (F9 in editor and development builds; only debug simulation builds act on it) drops your next block onto a tower; it can target a rival's tower.
- Verified (play mode, Lobby, online):
  - **Solo, real input:** grab and place locked a base after 3 s, and a Grab press with only the locked block in reach grabbed nothing.
  - **Solo, hit reset:** picking up the neighbouring block swung it through a base at 75% and reset it to unlocked with 0 progress.
  - **Solo, throw unlock:** a throw (12 m/s) at a two-level locked tower unlocked both levels in one cascade.
  - **Full solo match (dev stacking):** Lobby → Countdown → Playing → 3 locks → RoundEnd with player 1 as winner → Results → Lobby, with the 18 sandbox blocks restored at their homes.
  - **Two clients (MPPM), foreign block:** player 1 locked a base, player 2 put their block on it, and player 1's next block landed on top of that. Neither locked, and both clients agreed on every block.
  - **Two clients, full match:** player 2 built a tower of 3 and won; both clients showed RoundEnd/Results with winner player 2 and returned to the Lobby, sandbox restored, players at the lobby spawns.
  - **Visual check:** a game-view capture shows Guy tinted per player, owner-colored blocks, the dither climbing a locking block, and the HUD slots.
  - **Console:** clean on the main client. The clone's only errors came from my test harness's own eval code, not the game.
- Known issues / play-test items:
  - Lock time 3 s, unlock speed 4 m/s and impulse 8, and reset speed 1 m/s are first guesses (feel items).
  - **Grab swings can hit your own tower:** picking up a block on the far side of your own tower swings it through the tower and can knock your base loose. It's physically correct, but may feel unfair.
  - Matches run in the Lobby scene; Arena_Graybox has no Match yet (Phase 6 arena).
  - One dev command sent from the clone was lost once and worked on retry. That's the test harness, not gameplay input.
  - A player who joins mid-match gets no blocks until the next round.
- Placeholders still in use: `PH_PlayerCapsule` (Guy view on the capsule collider), `PH_Block` with `PH_BlockLock`/`PH_LockDither`, `PH_PlayerColors`, the IMGUI `MatchHud`.
- Ready for review: yes

## 2026-10-29: Phase 2, plan

### Phase 2 plan: Stack & Lock mode

Build order:
1. **Mode config + match flow (simulation).** `GameModeConfig` asset (blocks per player, tower height, lock time, unlock threshold, timers, win rule) with virtual rule hooks; the base class plays Stack & Lock (`StackAndLockMode.asset`), and new modes subclass it. A `Match` singleton on a map entity in the Lobby scene holds the mode, the state (Lobby → Countdown → Playing → RoundEnd → Results → Lobby), a timer and the winner. A `StartMatch` command (Enter / gamepad Start) begins the countdown.
2. **Owned blocks.** `Block` gains an owner and lock state. At match start the sandbox blocks are cleared (their home poses are kept), players move to match spawns, and each gets 3 blocks in their team. Returning to the lobby removes match blocks and restores the sandbox ones.
3. **Locking.** A block locks when it rests, upright and not held, in its owner's next tower slot: on the ground for the first block, directly on the owner's top locked block after that. Progress fills over the lock time; any hit resets it. A locked block is frozen (kinematic) and can't be grabbed.
4. **Unlocking.** A hit on a locked block above the unlock threshold (striker speed × mass, with a minimum speed so walking into a tower doesn't count) makes it dynamic again, along with every locked block above it.
5. **Foreign blocks** need no extra code: a rival's block sitting on the top locked block stops the owner's next block from resting there.
6. **View.** `PH_LockDither` shader (owner color, dither climbing with progress, locked shade); a block view that feeds it; player tint; a placeholder `PH_PlayerColors` palette.
7. **HUD.** Placeholder IMGUI: state and countdown, each player's tower slots (empty / locking / locked), winner and results, and the start prompt in the lobby.
8. **Verify** with 2+ clients: a full match from lobby to a winner and back. A dev-only command that stacks a player's blocks speeds up the test; real placement and hits are tested by hand-scripted input.

Risks: kinematic locked blocks must still report collisions with dynamic bodies (needed for unlock and reset); spawning and destroying blocks mid-session under rollback; lock-time and unlock-threshold numbers are feel items for Jackson's play-test.

## 2026-10-29: Guy re-export integrated (Phase 0 import test done)

- Jackson set up a collection exporter in `guy.blend` and re-exported. The new file was copied over `CH_Guy.fbx`, so its GUID is kept, and removed from `_Incoming`.
- Did: Guy's model is now on `PH_PlayerCapsule`, under the entity view with the `Body` capsule and `FacingMarker` hidden. The draft `Circle`/`Cube` that came along in the export are hidden. New `AC_Guy` controller has two states: Idle (rest pose) and Walk (`WalkBasic`, rate from a parameter). It switches to Walk above 0.25 m/s and back below 0.2 m/s. New view component `CharacterAnimationView` sets Speed and WalkRate from the predicted `PhysicsBody3D` velocity. The walk plays at speed ÷ 0.3 m/s, capped at 4×.
- Verified in play mode (Lobby, online): stick at 0.2 → 0.59 m/s, Walk at 1.96×; stick at 0.6 → 1.79 m/s, Walk at the 4× cap; stopped → Idle; Guy faces the movement direction. A game-view capture shows him at the right scale next to the blocks. Console: only the expected zero-length-clip warnings for `RunBasic`/`ThrowPose`.
- Decided: the physics capsule stays 0.35 m (Guy measures about 0.40 m); widening it changes Phase 1 reach, so it's revisited in Phase 4. Player prefab keeps its `PH_` name until Phase 4 replaces the view setup.
- Assets: A-003 Integrated. A-004/A-012 notes updated (walk for slow movement, run for fast, throw releases on frame 5).
- Next: Phase 2 when Jackson says go.

## 2026-10-29: Phase 1 approved; first Guy FBX delivery (import test)

- Jackson play-tested Phase 1: hold-to-aim is good, minor tuning waits for the plan's polish phase. Phase 2 not started (waiting for his go).
- Did: `_Incoming/guy.fbx` moved to `Assets/Art/Characters/CH_Guy.fbx` (GUID kept). New `Assets/Scripts/Editor/ArtImportPostprocessor.cs` applies the pipeline import settings to every model under `Assets/Art/`: scale 0.4, Generic, no cameras/lights, bone-scale curves kept, clip names stripped to the action name (`armature_guy|Guy.WalkBasic` → `WalkBasic`), and clips named Idle/Walk/Run/Loop/Pose/Hold loop. This replaces the "import preset" the pipeline spec mentioned.
- Import test results (game scale): rig faces +Z; `prop` bone 0.838 m up and 0.840 m forward (matches the simulation's 0.84 m hold pivot); `head` bone at 1.17 m; 36 deform-bone scale curves per clip came through (squash and stretch works); no root motion (root travel 0, hips sway ≤ 0.10 m).
- Clips: `WalkBasic` 48 frames, loops (seam 2.8°), but its planted foot moves at about 0.3 m/s against the game's 3 m/s walk; `Throw` 7 frames (0.29 s); `RunBasic` and `ThrowPose` are single-keyframe actions and import empty (Unity: "length of 0 frames").
- Not integrated: the FBX has no mesh (only the armature, including all 100 control/mechanism bones plus `_end` leaf bones), so `PH_PlayerCapsule` stays. A-003 set to `Needs changes`.
- Decided: axis-conversion baking stays off (with Blender's default axes it turned Guy to face -Z). The FBX root keeps a -90° X rotation and ×100 scale from Blender's export; harmless, because the model will sit under the player view's own transform.
- Blocked / questions for Jackson: re-export with the mesh; which frame of `Throw` the block leaves on.
- Next: integrate the re-export (mesh measurements, swap the capsule's view for Guy, test a clip in play mode), then Phase 2 when Jackson says go.

## 2026-10-29: Phase 1 review follow-up, hold-to-aim throwing

- Jackson's answers to the four Phase 1 questions are in PLAN.md Decisions: turning while holding stays as built (tune in play-tests), free placement stays, throwing becomes hold-to-aim, grab targeting stays, and in Phase 4 a grabbed block will align with the character's facing.
- Did:
  - **Hold-to-aim throwing.**
    - Holding Throw while holding a block starts aiming. The block rises to a ready position (swing pitch 30°) centered in front.
    - Horizontal look input turns the body; vertical input raises or lowers the throw angle (−20° to 60°, starts at 15°, 0.5° per degree of input).
    - Releasing Throw throws. Released while airborne it's a jumpshot: the same aim, 1.3× speed. The jumpshot's own downward angle is gone.
    - Grab while aiming still places the block. A stagger drops it and ends the aim.
  - **The throw velocity lives in one shared function**, `ThrowHelper.GetThrowVelocity`. The grab system throws with it; the view's preview reads it.
  - **Aim preview** (`ThrowAimView`, local player only):
    - A yellow trajectory line, stepped at the simulation's 64 Hz with its gravity.
    - A circle lying on the surface it lands on, found with read-only Quantum raycasts that skip the thrower and the held block.
  - **Block linear drag set to 0**, so blocks fly ballistically and the preview is exact.
- Verified (single client, Lobby):
  - **Aim:** aiming raised the pitch from 15° to 33.8°, and the preview drew an 83-point arc ending on the south wall.
  - **Throw:** on release the block hit that wall 79 ticks later. The preview's arc reaches the wall face in about 81 ticks; the block's center arrives about 2.5 ticks early because of its 0.4 m half-size.
  - **Jumpshot:** released in the air at 15.97 m/s, against 15.6 expected (12 × 1.3), plus the jump's upward speed.
  - Console clean.
- Decided: block linear drag 0; aim starts at 15° and turns at 0.5° per degree of input (tune in play-tests).
- Assets: A-012 now also asks for an aim pose (block raised and held ready) and a release.
- Next: Jackson's play-test of Phase 1, then Phase 2.

## Phase 1 report: Sandbox lobby

- Built:
  - **Movement** (adapted from Friendslop's `MovementSystem`):
    - Camera-relative walk (3 m/s) and run (6 m/s). Jump: about 1 m peak, faster fall.
    - Crouch: a 1.10 m capsule at 1.5 m/s, standing back up only with headroom.
    - Slide: crouch while running and grounded. 7.5 m/s for 0.75 s, carries about 4 m.
    - Control tiers InControl and Staggered. Staggered has no steering and light braking, and drops the held block.
    - While holding a block the body doesn't turn to its move direction, so left/right strafes.
  - **Knockback:** one `ApplyKnockback` signal and `KnockbackSystem`.
    - Hits from moving blocks (swung, thrown, falling) and running body checks feed it. Strength is the striker's own speed toward the target, scaled by mass share.
    - Stagger at a 2.5 m/s speed change or more, lasting 0.12 s per m/s, clamped to 0.25–1.2 s.
  - **Blocks** (`PH_Block`): 0.8 m, mass 2, neutral team (255), grippy low-bounce material. 12 loose in each scene, plus two towers of 3 in the Lobby.
  - **Grab** (adapted from Friendslop's `GrabSystem`: velocity chase, swing limit, leash, carry load):
    - Grab: the nearest free block within 2 m in a 60° cone in front of the character.
    - Hold: the block orbits a pivot at Guy's `prop` offset (0.84 m up, 0.9 m out), driven by the right stick or mouse (`Input.Swing`).
    - Swing: the body turns to follow a swing past 45°; limits ±150° yaw, −25° to 60° pitch.
    - Place (Grab while holding): the block is set down where it is, square to the facing.
    - Throw: 12 m/s, 15° up. Jumpshot (throwing while airborne): ×1.3 speed, −10°.
    - Ported as-is: `GrabbableCollision`, `GrabJointCleanup`, `ObjectImpact`.
  - **Camera:** while holding, the look input becomes swing, and the camera eases round behind the character. It starts behind the spawn facing.
  - **Physics materials:** frictionless player (as Friendslop's), so speeds reach their targets exactly.
  - **Rooms:** hold 8 players (`#pragma max_players 8`).
  - **Test tooling:**
    - An input hook on the poller (editor and development builds only).
    - A dev launcher (`-autojoin ROOM`, `-autoplay`, development builds only).
    - A test-only `RagdollStressTestSystem` and the `Scenes/Tests/Test_RagdollStress` scene.
    - `PH_RagdollTest`: an empty-transform skeleton at Guy's scale, baked with `QuantumRagdoll` (11 limbs, bones assigned manually, no Animator).
- Done-when verification: "up to 8 online players toss blocks around and it feels fun".
  - **8 players online:** the main editor plus 7 development builds with `-autoplay`, all in one Photon room (region us, ping about 30 ms).
    - All 8 simulated together.
    - In about 65 s the autoplay players produced 9 grabs, 52 jumps, 29 knockbacks (some staggering), a placement and a jumpshot.
    - Console clean in the editor; no errors in any build log.
    - Simulation cost about 80–115 ms per second in the editor (1.3–1.8 ms per frame); worst update about 3 ms.
  - **Each mechanic, with two clients through MPPM** (both clients agreed on every position):
    - A swung block staggered the other player (2.8 m/s hit).
    - A running body check staggered the target (4.4 m/s) and not the runner.
    - Running into a resting held block no longer counts as a hit (fixed after the first test showed it did).
  - **Single client:**
    - Walk, run, crouch and slide hit their target speeds.
    - Grab, swing past the body-turn threshold, place, throw and jumpshot all behaved as specified and fired their events.
    - Measured from per-tick simulation logs.
  - **"Feels fun":** not something the agent can verify. Needs Jackson's play-test.
- Stacking test (3.1 solver, Friendslop's physics tuning):
  - Towers of 3 placed blocks stood perfectly still for 10 s.
  - A player walking into the base slid the whole tower 7 cm as one piece. It stayed upright (tilt 1.0000), went back to sleep, and didn't move by even a millimetre over the next 20 s.
  - **Plain towers stand unaided**, so Phase 2's locked state can be light: heavy damping rather than freezing, as the plan allows.
- Ragdoll stress test (8 ragdolls, 88 limbs, dropped onto the two towers; 2 clients through MPPM, ping about 30 ms, 2–3 predicted frames):
  - Simulation cost rose from about 45–50 ms per second (about 0.75 ms per frame) to about 180–280 ms per second (3.0–4.4 ms per frame). Worst update about 6 ms, with a 12 ms spike on the spawn frame. No desyncs.
  - The limbs knocked both towers down.
  - **The limbs never went to sleep** in 30 s, so the cost doesn't fall off after they land.
  - These are editor (Mono) numbers; a release build is typically several times faster. Rollback cost scales with ping, so at 100–150 ms ping expect roughly 3× the per-second cost.
  - Phase 3 levers: get limbs to sleep, a short knockdown timer, prediction culling, fewer limbs.
- Known issues:
  1. **Ragdoll limbs don't sleep** (stress test above). Phase 3.
  2. **Swing input is sent as per-tick deltas.** If the server repeats a lost input, a swing repeats one tick's worth. Harmless at normal loss rates; revisit if swings feel jumpy online.
  3. **The camera has no wall collision**, and controls are placeholder bindings (LMB/RB grab, RMB/RT throw, Shift/L3 run, Ctrl/B crouch, Space/A jump).
  4. **Automated tests can't use synthetic keyboard or mouse input** in an unfocused editor. They use the input hook instead; real play is unaffected.
  5. **Assets made by script must be force-reimported and Quantum's asset database refreshed** before they reach the simulation. Noted for future tooling.
  6. **Guy import test (Phase 0)** still waits on his FBX.
- Placeholders still in use:
  - `PH_PlayerCapsule` (A-003).
  - `PH_Block` and `PH_Block.mat` (A-005, A-013).
  - `PH_` terrain (A-008).
  - `PH_RagdollTest` (test only; Guy's rig replaces it in Phase 3/4).
  - The stock Quantum start UI (A-002).
- Defaults still awaiting Jackson's answers (questions in the Phase 1 plan below): turning while holding, place behaviour, jumpshot, grab targeting. One change since: grab targeting uses the character's facing, not the camera's.
- Ready for review: yes

## 2026-10-28: Phase 1, plan and asset pass

### Phase 1 plan

**Build order** (each step ends in a commit and a play-mode check):

1. **Movement tiers.**
   - Crouch: a shorter capsule and lower speed.
   - Slide: crouch while running and grounded. A forward impulse, low braking, no steering, and a timer (a simulation timer, sized to the clip later).
   - `ControlState` on the player: InControl or Staggered. Staggered cuts steering force for a timer, so the body slides on its momentum. Ragdoll arrives in Phase 3.
2. **Knockback through one impulse path.**
   - Every hit goes through a single `ApplyKnockback(target, impulse, source)` signal, handled by one `KnockbackSystem`.
   - It applies the impulse and, above a threshold, starts Staggered.
   - Block hits (thrown, swung or falling) and body checks all call it. In Phase 2, unlocking reads the same contact impulse.
3. **Blocks.**
   - The `PH_Block` entity: a 0.8 m box with `Team`, `Grabbable` and `Block`. 12 free blocks in the Lobby.
   - Grab is adapted from Friendslop's `GrabSystem`: the velocity chase toward a hand point, swing limit, leash, carry load and drop.
   - **Hold-and-swing:** the hand point orbits a pivot inside the player (Guy's `prop` offset) at a swing angle that the right stick drives. The block keeps its orientation relative to the player. Because the block is moved by velocity, a swing physically hits players and blocks.
   - **Place:** Grab while holding. Sets the block down gently, square to the player's facing.
   - **Throw:** an impulse along the aim plus the carried velocity.
   - **Jumpshot:** Throw while airborne. Its own speed and arc.
   - Ported as-is: `GrabbableCollisionSystem`, `GrabJointCleanupSystem`, `ObjectImpactSystem`.
4. **Camera.** While holding, the right stick and mouse feed `Input.Swing`, and the orbit camera stops taking look input and follows behind the character.
5. **Stacking test.** Towers of 3 plain blocks under the 3.1 solver (Friendslop's tuning). Record whether they stand unaided after being placed and after a nudge.
6. **Ragdoll stress test.** A minimal humanoid skeleton of empty transforms at Guy's proportions, baked with `QuantumRagdoll` (no Friendslop art; manual bone slots, Generic-style). Eight of them dropped onto block towers online. Read simulation time and rollback cost with Quantum's stats.
7. **Verification.** Up to 8 clients online (MPPM caps at 4 local players, plus extra standalone builds if needed). Report.

**Risks**

- Swing feel is the core mechanic and depends on design answers (questions below). Every number is config, so retuning doesn't mean rewriting.
- Block-on-block stacking under fixed-point physics may creep. The test in step 5 decides how heavy Phase 2's "locked" physics must be.
- 8 ragdolls under rollback may be costly. Step 6 measures it early, as the plan asks.
- "Feels fun" is Jackson's call. The agent can verify that the mechanics work, not that they're fun.

**Questions for Jackson (defaults the agent builds until answered)**

1. **Turning while holding.** The right stick swings the block and left/right on the left stick strafes, so what turns the character?
   - Default: horizontal swing moves the block around the player within ±150° of facing. The body turns toward the block whenever it's more than 45° off, and the camera follows behind. So a big swing also turns you.
2. **Place.** Default: Grab while holding sets the block down where it is, square to your facing. Do you want snapping to the tower below (aligned on top), or free placement?
3. **Jumpshot.** Default: Throw while airborne. 30% faster, and aimed down by up to 20° when you're above the target height, for "dunking" onto towers. Or did you have a different jumpshot in mind?
4. **Grab range and targeting.** Default: the nearest block within 2 m in a 60° cone in front of the character.

### Session log

- Did: Phase 1 plan; asset pass.
- Assets:
  - A-005 and A-011 are now *Needed soon*.
  - A-012 stays *Start now*.
  - A-001 moved to *Next phase*.
  - New: A-013 (block owner tint and readability, Phase 2 look-ahead) and A-014 (SFX placeholder list, *Nice to have*).
- Blocked / questions for Jackson: the four questions above (not blocking; defaults in use).
- Next: movement tiers.

### Session log (build)

- Did: everything in the Phase 1 report above.
- Decided:
  - Player linear drag 0 and a frictionless player material, so speeds reach their targets.
  - Slide deceleration 0.8 (2 bled the slide in 0.4 s).
  - Knockback strength uses the striker's own speed toward the target.
  - Grab cone follows the character's facing.
  - Block mass 2 and friction 0.8.
  - Rooms hold 8 players.
  - Dev launcher and input hook exist only in the editor and development builds.
- Assets: none delivered. A-003, A-004, A-005 and A-012 updated with the sizes and timings the art must match.
- Blocked / questions for Jackson: play-test for "feels fun"; the four design questions above.
- Next: Jackson's review of Phase 1, then Phase 2.

## Phase 0 report: Foundations

- Built:
  - **Quantum:** 3.1.0 Preview build 2189 (from Friendslop), pinned. AppId preserved. 64 Hz, checksum interval 64, Friendslop's physics tuning.
  - **Git + LFS:** 4 commits on `main`.
  - **Hygiene:**
    - Template content removed; Run In Background on; company "Jackson Hill".
    - Multiplayer Play Mode 2.0.2 installed and enabled (Project Settings › Multiplayer › Play Mode local deployment; needed one editor restart).
    - Unity CLI connector (`com.unity.pipeline`).
  - **Friendslop study:** notes and porting verdicts above.
  - **Input:** the Quantum input definition covers every Phase 1 verb (move, camera, swing, jump, crouch, run, grab, throw). Unity-side bindings are in code.
  - **Simulation:** Player, PlayerLook, Team, GroundedTracker, camera-relative force-driven Movement.
  - **View:** orbit camera, input poller, local-player camera hookup.
  - **Content:**
    - `PH_PlayerCapsule` (1.66 m × 0.35 m radius).
    - `Lobby` and `Arena_Graybox` scenes with uneven `PH_` terrain, 8 spawn points, online start UI.
- Done-when verification: "two clients join a room and move capsules sized to Guy".
  - **Setup:** main editor plus MPPM virtual Player 2, Lobby scene, Photon cloud, one room.
  - **Join:** each client saw 2 connected players, itself local and the other remote, on teams 0 and 1 at spawn points 0 and 1, each camera on its own capsule.
  - **Moving both:** gamepad left-stick input was held on each client in turn (keyboard events can't be injected into an unfocused editor, so the test used a virtual gamepad).
    - Main client: stick forward at camera yaw −138.9° moved player 0 by (−2.10, −2.41), exactly the camera's forward direction.
    - Clone: stick right at yaw −27.1° moved player 1 by (2.94, 1.38), along camera-right.
    - After each move, both clients reported identical positions for both players.
  - **Earlier single-client run:** forward + run reached the wall and stopped at the wall face minus the capsule radius; braked to zero on release.
  - **Console:** clean on the clone; one SDK warning on the main editor (known issue 1).
- Known issues:
  1. ~~SDK warning with MPPM~~ Fixed 2026-10-28 with Jackson's OK. **SDK patch:** `[System.Serializable]` added to `QuantumStartUIMppmConnectCommand` and `QuantumStartUIMppmDisconnectCommand` in `Assets/Photon/Quantum/Runtime/StartUI/QuantumStartUI.Common.cs`, each marked with a "Block Sauce SDK patch" comment. **Re-apply it after any SDK update.** Verified: a two-client join and disconnect leaves both consoles free of it.
  2. **Guy import test not done:** waiting for the first animated FBX (A-003/A-004). The capsule radius (0.35 m) is a guess until then.
  3. **Placeholder controls:** default bindings, and the camera has no collision with walls yet. Phase 1 work.
  4. **Synthetic keyboard input in automated tests:** an unfocused editor drops it. Automated tests use a virtual gamepad instead. Manual play is unaffected.
- Placeholders still in use:
  - `PH_PlayerCapsule` (A-003).
  - `PH_Gray`, `PH_GrayDark`, `PH_Ground`, `PH_Player` materials.
  - `PH_Terrain` in both scenes (A-008).
  - Quantum's stock start UI for the menu (A-002).
- Ready for review: yes

## 2026-10-28: Phase 0, upgrade and first online movement

- Did:
  - **Quantum 3.1.0 Preview build 2189** replaced 3.0.14 (Quantum, PhotonLibs, PhotonRealtime, copied from Friendslop). The AppId is intact (settings file byte-identical to the backup).
  - Codegen was run before reopening Unity with a small .NET runner around `Quantum.CodeGen.Qtn.dll`, so the editor never hit Safe Mode. Its output is byte-identical to the in-editor `Run Qtn CodeGen`.
  - Session config: 64 Hz, rollback window 64, checksum interval 64. Physics block copied from Friendslop (6 solver iterations, warm start, sleeping on, speculative callbacks).
  - Simulation:
    - `Input` (Move, Camera, Swing, HasCamera; Jump, Crouch, Run, Grab, Throw).
    - `Player` + `PlayerLook` + `PlayerSystem`/`PlayerLookSystem`: spawn at spawn point N, free-for-all team = player index.
    - `Team`.
    - `GroundedTracker` (ported as-is minus the ragdoll check).
    - `Movement`: Friendslop's minus Stamina/Attack/Launched; camera-relative; turns toward its move direction at 720°/s.
    - Systems are registered in `SystemSetup.User.cs`.
  - View:
    - `BlockSauceControls` (bindings in code).
    - `PlayerInputPoller`.
    - `OrbitCamera`: click locks the cursor, Esc frees it.
    - `LocalPlayerView`.
  - `PH_PlayerCapsule` prefab plus `PlayerMovementConfig`/`PlayerGroundedTrackerConfig`.
  - Scenes:
    - `Lobby`: 40 m floor, ramp, platform, 3 steps, walls.
    - `Arena_Graybox`: 60 m floor, 10°/20°/35° ramps, a stepped hill with side slopes, stairs at 0.2–0.8 m, rotated-cube bumps, walls.
    - Each has 8 spawn points, a start UI for online play, and a local debug runner (it stands down while the start UI is present).
  - Hygiene:
    - Company name "Jackson Hill"; template `QuantumGameScene` and its map removed.
    - The SDK start-UI prefab restored to stock: Friendslop had baked in its own connection component (with a `-RuhanDev` app-version postfix) and a nested settings panel that wasn't copied.
  - Verified online (one client, Photon cloud, Lobby):
    - The character spawns at spawn point 0 on team 0, grounded.
    - Holding W+Shift (simulated through the Input System) drove it along the camera's forward to the north wall, stopping at the wall's face minus the capsule radius; on release it braked to zero.
    - Camera follows. Console clean.
- Decided:
  - Capsule radius 0.35 m until Guy's FBX gives a real width.
  - Mass 1, drag 0.5, rotation frozen on X/Z, as Friendslop.
  - Spawn height offset 1 m.
  - Default bindings are placeholders: LMB/RB = grab, RMB/RT = throw, Shift/L3 = run, Ctrl/B = crouch, Space/A = jump.
  - The Swing input is sent as zero until holding exists (Phase 1).
  - Each scene's start-UI connection uses the per-scene app-version postfix the SDK sets, so Lobby and Arena rooms don't mix.
- Assets: none delivered. A-003 notes updated with the capsule placeholder size.
- Blocked / questions for Jackson: approve adding `com.unity.multiplayer.playmode@2.0.2` for the two-client done-when check. The Guy import test waits for his first animated FBX (that task stays open; it doesn't block the done-when, which only needs capsules sized to Guy).
- Next: add Multiplayer Play Mode, run 2 clients in one room, verify both move, then write the Phase 0 report.

## 2026-10-28: Phase 0, plan and setup

### Phase 0 plan

**Tasks, in build order**

1. **Tooling (done this session).** Unity CLI 1.0.0-beta.14 installed; `com.unity.pipeline` 0.9.1-exp.1 added. The CLI lists the open scene's hierarchy and reads the console, which is clean.
2. **Quantum 3.1 upgrade.** Back up `PhotonServerSettings.asset` outside `Assets`. With the Editor closed, replace `Assets/Photon/Quantum`, `PhotonLibs` and `PhotonRealtime` (plus their `.meta` files) with the Friendslop copies (3.1.0 Preview build 2189). Leave out PhotonVoice, PhotonChat and QuantumDemoInput. Reopen Unity, follow the 3.1 migration notes in the CHANGELOG, regenerate Quantum code, then confirm the AppId is intact and the console is clean.
3. **Git.** `git init`, `git lfs install`, use the provided `.gitignore`/`.gitattributes`, and make the first commit once the upgrade compiles.
4. **Hygiene.** Delete `TutorialInfo`, `Readme.asset` and `SampleScene`. Turn Run In Background on. Set the company name (waiting on Jackson). Install Multiplayer Play Mode, which needs Jackson's OK because it's a new package.
5. **Friendslop study.** Notes go below in this file.
6. **Input scheme.** Write the Quantum `Input` struct and a Unity Input System action map. Move; camera vs. swing on the right stick; buttons for jump, crouch, grab, throw and run.
7. **Scenes.** `Lobby` and `Arena_Graybox`, each with a Quantum map and uneven test terrain made from primitive slopes and steps (`PH_` materials).
8. **Player capsule.** A player entity with a `PhysicsBody3D` capsule sized to Guy, spawned per joining player, with minimal force-driven movement. Full movement comes in Phase 1.
9. **Guy import test.** Needs the FBX export (A-003/A-004) and the scale decision. Measure height, width and the `prop` offset, then resize the capsule to match.
10. **Done-when check.** Two clients, through Multiplayer Play Mode, join one room and move capsules sized to Guy. Console clean.

**Risks**

- **3.1 Preview API drift.** 3.0 → 3.1 changes commands, `SessionRunner.Arguments` and the sim rate. The project has no gameplay code yet, so the risk is mostly the generated code and the default config assets. Mitigation: regenerate code and recreate default configs if they fail to load.
- **Swapping the SDK with the Editor open** could leave a half-imported SDK, so the Editor is closed for the copy.
- **Guy's measurements block the final capsule size.** Until the export arrives, the capsule uses the proposed scale (1.66 m tall).
- **The Multiplayer Play Mode version** must match Unity 6000.3. Take the version the Package Manager offers for this editor.

### Friendslop study (Quantum 3.1.0 build 2189)

Paths are relative to `quantum-friendslop-3.1.0/Assets/`. Config numbers are the shipped asset values in `QuantumUser/Resources/Configs/`, not the C# defaults.

**Baseline settings**

- **Session:** 64 Hz, rollback window 64.
- **Physics:** gravity −10; solver type 1 (`ProjectedGaussSeidel`) with 6 velocity and 2 position iterations; penetration allowance 0.01 and correction 0.9; sleeping on (0.4 s); CCD off.
- **Player capsule** (`Prefabs/Characters/CharacterBase.prefab`): radius 0.5, height 2, origin at the feet, mass 1, rotation frozen on X and Z, collision callbacks on.
- **System order:** Player → PlayerLook → Stamina → GroundedTracker → Movement → GrabbableCollision → … → Fall → Grab → GrabJointCleanup → Ragdoll.

| System | What it does | Verdict for Block Sauce |
|---|---|---|
| **Player** (`Player.qtn`, `PlayerSystem`, `PlayerLookSystem`) | Copies the tick's input onto the entity. Spawns `RuntimePlayer.PlayerAvatar` at spawn point N. `PlayerLook` keeps absolute yaw and pitch. Keeps the last look when `HasLook` is false. | **Adapt.** Add `TeamId` and a `ControlState` enum (InControl/Staggered/Ragdoll); yaw becomes the orbit-camera yaw. |
| **Input** (`Input.qtn`, `Scripts/Input/PlayerInput.cs`, `LookInput.cs`) | Buttons `Jump`, `Sprint`, `Kick`, `Grab`, `Use`; `bool HasLook`; `InputDirectionMagnitude Move`; `InputPitchYaw Look`. Look is sent as **absolute** angles, so lost or repeated inputs don't drift. Polled via `CallbackPollInput` with `DeterministicInputFlags.Repeatable`. | **Adapt.** New struct below. |
| **Movement** (`MovementSystem`, `MovementConfig`) | Force toward a target velocity (accel 5, decel 5, walk 3, run 6), clamped only while there's input, so knockback survives. Jump impulse 5; fall gravity ×2.5. Holds on slopes. Step-up by raycast-driven up-velocity (max step 1). Snaps rotation to look yaw. Requires `Stamina` in its filter. | **Adapt.** Face the move direction (or the camera while holding). Drop Stamina. Add crouch, plus slide (run+crouch: impulse, low decel, timer). Gate on `ControlState`. |
| **GroundedTracker** | Grounded from contact normals (y ≥ 0.5) with 3 grace ticks, not raycasts. Banks a safe respawn position from static-only probes. | **Port as-is.** Standing on dynamic blocks works; towers aren't banked as respawn points, which is what we want. |
| **Grab** (`GrabSystem`, `GrabHelper`, `GrabConfig`) | Aim ray from the eye. The held body gets gravity 0 and no sleep, and a `DistanceJoint3D` leash to the shoulder. Each tick its velocity = carrier velocity + (hand − pos) × gain 20, capped at 50. The target is swing-limited (1080°/s around the shoulder). The carrier is slowed by 0.1 × mass. Auto-drops past 2.2 m or when a static blocks the line. **There is no throw:** grab toggles drop, and the kick pushes a held object along the aim. | **Adapt; most valuable code.** Replace the pitch-driven hand anchor with a swing angle integrated from `Input.Swing` (hand = shoulder + rotation(swing) × radius), anchored at Guy's `prop` offset. Keep chase, swing limit, leash and reaction. Add Throw (impulse along the camera aim + carried velocity), Jumpshot (airborne variant) and Place. Grab aims from the orbit camera ray or a forward cone. |
| **GrabbableCollision / GrabJointCleanup** | Ignore collisions between a carrier and the held object (including ragdoll limbs); remove the leash joint when released. | **Port as-is.** |
| **ObjectImpact** | `ObjectImpact` event on verified frames when a free object hits at ≥ 2 m/s. | **Port as-is** (audio/VFX hook). Phase 2's unlock check uses its own impulse path in the simulation. |
| **Attack** | Kick: lunge, sphere overlap at 0.25 s, push 10 with up-bias, ragdoll ×2, cooldown 0.6. | **Skip the kick.** Reuse its timer/overlap/push pattern for knockback (Phase 1) and the push-held-object path for Throw. |
| **Ragdoll** (`RagdollSystem`, `ImpactRagdollSystem`, `Ragdoll.User.cs`) | `SetRagdoll` disables the capsule and spawns an entity-prototype group baked by `QuantumRagdoll` (10 `CharacterJoint` joints; bones assigned manually, so Generic rigs work). Limbs are grabbable. Duration 8 s; jump "struggle" cuts 1 s. Impact ragdoll fires at **2 m/s** (asset value). A held object hitting someone at holder speed ≥ 6 disarms them instead. | **Adapt (Phase 3).** Add a Staggered tier below the knockdown threshold; raise the thresholds a lot (2 m/s would floor everyone hit by any block); shorter duration; team filter in a rule hook. |
| **Fall** | Below Y −15: characters return to the last grounded position; props are destroyed. | **Adapt.** Drop the enemy branch; blocks respawn instead of being destroyed; route through a game-mode hook. |
| **Stamina / JumpPad / Launched** | Sprint budget; launch pads. | **Skip.** The "marker component excludes Movement" pattern from `Launched` is reused for Staggered. |

Also worth copying: the `frame.GetConfig<T>(entity)` helper in `Frame.User.cs`.

**Block Sauce input struct (Phase 0)**

- `InputDirectionMagnitude Move`: left stick or WASD, relative to the camera yaw; left/right always strafes.
- `InputPitchYaw Camera` + `bool HasCamera`: absolute orbit yaw and pitch, the same no-drift scheme as Friendslop. Movement, aim and throw direction come from it.
- `FPVector2 Swing`: the raw right stick (or mouse delta) while holding a block. The view freezes the orbit camera locally while the sim drives the swing from this value.
- Buttons: `Jump`, `Crouch`, `Run`, `Grab` (grab/place/drop), `Throw` (with `Jump` airborne = jumpshot).

### Session log

- Did: created `.claude/settings.json`; installed the Unity CLI and the Pipeline package; confirmed the connector works; wrote the Phase 0 plan; did the asset pass; studied Friendslop (notes above); backed up `PhotonServerSettings.asset` to `BlockSauce-Unity/_backup/` (outside the repo); `git init` + `git lfs install` (no commit yet); deleted `TutorialInfo`, `Readme.asset`, `SampleScene`; turned Run In Background on.
- Decided: the Unity CLI and `com.unity.pipeline` count as Editor tooling (Jackson approved the install in chat).
- Assets: A-003/A-004 moved to *Needed soon* with export specifics; added A-011 (movement feel targets) and A-012 (hold-block pose, a long-lead slice of A-004); A-005 now asks for an early draft export for Phase 1.
- Blocked / questions for Jackson: close Unity for the SDK swap; approve adding `com.unity.multiplayer.playmode@2.0.2` (the agent's attempt was held for approval); game scale; which walk action is real; Guy export (Jackson, or a headless export by the agent); company name.
- Next: Quantum 3.1 upgrade, with the Editor closed.

---

## 2026-10-28: Pre-Phase 0 directory review (done in chat, not by the agent)

- **Unity project:** 6000.3.14f1, URP 17.3, Input System only (`activeInputHandler: 1`), product name "Block Sauce", company name still "DefaultCompany". Run In Background is **off**. URP template leftovers present (TutorialInfo, Readme.asset, SampleScene, InputSystem_Actions). No git repository yet.
- **Quantum in project:** 3.0.14 Stable (build 2241). Default `QuantumUser` scaffold and `QuantumGameScene`; no gameplay code yet. AppId set in `PhotonServerSettings.asset`.
- **Friendslop sample:** Unity 6000.3.13f1, Quantum **3.1.0 Preview build 2189**, plus Cinemachine 3.1.7, PhotonVoice, PhotonChat and several CC0 asset packs in `Assets/3rdParty`. Its ragdolls use 3.1-only features (`CharacterJoint3D`, `QuantumRagdoll`), so its code won't port onto 3.0.14. Hence the Phase 0 upgrade task.
- **Friendslop code worth porting:** `Movement/MovementSystem` (force toward target velocity, braking, slope and step-up), `Grab/GrabSystem` (velocity-driven hold point, `DistanceJoint3D` leash, `GrabHelper`), `Ragdoll/*` (`SetRagdoll` signal, `RagdollConfig`: knockdown duration, impact speed 20 m/s, impulse multiplier), `GroundedTracker`, `Fall`.
- **QuantumRagdoll and Generic rigs:** bone slots are plain serialized Transforms; auto-fill uses `Animator.GetBoneTransform` (Humanoid only). Leave the Animator field empty and assign bones manually.
- **Guy (`modelling/guy/guy.blend`, Blender 5.1):** mesh `Sphere` (1,482 verts, subsurf, 1 material), armature with 100 bones / 31 deform, a full control rig (IK/FK, foot roll, stretch, squash with maintain volume, look and eye targets) and a `prop` bone at the hold point. Height 4.15 m at authoring scale. 24 fps. Actions: `Guy.WalkBasic`, `.001`, `.002`; only `.002` has keys (0–96) and it has no fake user. Also in the file: `Cube` (2 m, geometry nodes; likely the block draft), `Circle` (2.46 × 1.18 m; possibly a vat draft), `WGT_*` control-shape widgets.
- **Open for Jackson:** confirm game scale (proposed import factor 0.4, Guy about 1.66 m); confirm which walk action is real.
- **Next:** Phase 0, starting with the Quantum 3.1 upgrade.
