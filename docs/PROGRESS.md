# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports live in git history.

## Current state (2026-11-03)

**Phases 0–5 are done. Phase 5 (Sauces) has Jackson's first round of tweaks in (Stack as sticky friction and glue, grab keeps the top face, cup-shaped vat collision, a coat with volume) and waits for his review.**

What plays today, online with up to 8 players, in the Lobby scene:
- Guy on a force-driven capsule (0.40 m radius, his real width): walk/run (analog on gamepad), jump, sit, slide, stagger.
- Grab (the block keeps whichever face was up), hold and swing (body turns past 45°; the camera follows the swing on a gradient and never locks), place with flush settling, hold-to-aim throw with trajectory preview, jumpshot.
- Stack & Lock: 3 owned blocks each, settle dither, counting, win, full match loop back to the lobby.
- Knockdown: Guy's 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips, dazed indicator, falling out of the world resets.
- Animation: idle; walk and run blending forward, back and strafe; jump; sit and slide; stagger flinches by hit side; GrabBlock hold with spine turn and hand IK; aim pose (`Throw` frame 4) and follow-through, grounded and airborne; foot IK on steps, ramps and blocks; win and lose placeholders after a round.
- Sauces: two vats (Jackson's cup) in the middle of the Lobby. Dunk a held block (or throw one in) to coat it; how deep sets the strength. Stack sauce makes a block sticky: high friction, and it glues to the blocks and ground it rests against until a throw-hard hit or a grab tears it free; Slip sauce makes a block slide (7.9 m after a throw against 1.1 m plain), so a Slip tower comes apart when pushed. The coat is a thick, lumpy, glossy shell that changes the block's silhouette, with bubbles (Stack) or glints (Slip); drips are off for now. Dev keys: F8 coats your held block or all your blocks with Stack sauce, Shift+F8 with Slip.

### Known issues
- Sitting while holding a block keeps the block at the standing hold height; the arms reach up to it. If it reads badly, lower the hold pivot while sitting (simulation).
- A block dunked hard against the cup's side can stick at the bottom for a moment before the hold lifts it out.
- Sauce particles emit over the whole block, not only the coated part.
- A block let go off-center in a cup rests against the sloped wall, a little tilted (the cup is 1.43 m across at the floor).
- With the hips dropped for a low foot, Guy's long arms can touch the ground beside him.
- MPPM: starting the `TwoClients` scenario sometimes hangs the main editor while the clone launches; stopping and starting the scenario again recovers it.
- The camera has no wall collision. Phase 6.
- A ragdoll that spawns half inside a block can launch it; the fall reset recovers it. If it shows up in play, spawn the ragdoll in a tucked pose.
- A body still airborne when its knockdown ends stands up mid-air and falls.
- A player who joins mid-match gets no blocks until the next round.
- Swing input is sent as per-tick deltas, so a repeated lost input repeats one tick of swing. Revisit only if swings feel jumpy online.
- The start menu has a stray white banner box (stock Quantum start UI, A-002).
- Matches only run in the Lobby scene; Arena_Graybox has no `Match` yet (Phase 6).

### Waiting on Jackson
- Review of Phase 5 and its tweaks: a play-test of the sauces and of grabbing a block on its side, and the new open question in the plan (Stack glue strength, and whether it should glue to the ground).
- The older open questions (settle numbers; pickup swings through your own tower).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip` (the cup at 1.3× with placeholder sauce), `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain (A-008), the IMGUI `MatchHud` (A-006), the stock Quantum start UI (A-002), `PH_RagdollTest` (stress-test scene only).

## Next: Phase 6 (Arena and polish), after Jackson's review of Phase 5

## Log

Entry format:

```
## YYYY-MM-DD: Phase N, <short title>
- Did: <what was built or changed>
- Verified: <how>
- Decided: <routine engineering decisions made without asking>
- Assets: <deliveries integrated, list changes>
- Questions for Jackson: <or "none">
- Next: <what the next session starts with>
```

Phase report format:

```
## Phase N report: <name>
- Built: <summary>
- Done-when verification: <each criterion and how it was checked>
- Known issues: <list>
- Placeholders still in use: <list with asset IDs>
- Ready for review: yes
```

## Phase 5 report: Sauces
- Since this report (2026-11-03): the Stack shove was replaced by sticky friction, and the coat and vat collision were reworked (see the log).
- Built: two dip vats (Jackson's cup at 1.3×) in the Lobby. A block dipped in a vat records the sauce surface as a plane in its own space, up to three dips; the deepest dip sets the sauce's strength. Stack sauce: a stacked block shoves rivals within 2.5 m (stagger, drop what they hold), then waits 2 s. Slip sauce: the block takes a low-friction material in four steps. Coat shader (ragged edge, drips growing after the dip, lip bulge, wet shading), bubbles and glints by strength, dip splash, Stack pulse ring. Dev keys F8 / Shift+F8. Also this phase: the aimed block collides with nothing and its model arcs over the head; thrown blocks clear their thrower; Win and Lose placeholders after a round; the subdivided block.
- Done-when verification ("dipping changes how a round plays and reads at a glance"), two clients through MPPM `TwoClients` in one online room:
  - Stack: A started a match, coated its blocks with Stack sauce and stacked three. B (scripted) ran at A's tower twice. Each approach ended in one shove at 2.5 m, staggered, and B was thrown back out of range; A logged one pulse each time. On B, each shove arrived as one event.
  - Slip: A ran into the base of its own three-block tower. Plain, the tower moved 0.15 m and still counted. Slip-sauced, it came apart: all three blocks fell to the ground and slid 2.9, 4.4 and 6.8 m.
  - Dipping: a block dunked in a cup came out 69% coated at full Stack strength, the player outside the cup throughout. A Slip-thrown block slid 7.9 m after landing against 1.1 m plain.
  - Reads at a glance: the coat (dark and warm for Stack, pale and glossy for Slip), bubbles or glints, the splash and the shove ring, checked in a capture. Jackson's play-test is the real check.
  - Round end, seen on B: A (winner) in Win, B in Lose.
  - Consoles on both clients: no game errors.
- Fixed during verification: a three-block Stack tower shoved a rival three times in one tick (now once per player per tick, with the tower firing and cooling down together). The shoved client flinched twice per shove because the knockback event's impulse differed between the predicted and the verified frame; the impulse and the pulse position are now `nothashed`.
- Known issues: see Current state (sauce particles cover the whole block; the cup art is wider than its collision; a hard dunk can stick for a moment; MPPM scenario start sometimes hangs).
- Placeholders still in use: `PH_SauceVat_Stack`/`_Slip` and `PH_SauceCoat`/`PH_SauceLooks` (A-009), `PH_PlayerCapsule`, `PH_Block`, `PH_LockDither` (A-007/A-013), `PH_` terrain (A-008), IMGUI `MatchHud` (A-006), stock start UI (A-002); Win and Lose clips are Jackson's placeholders (A-023).
- Ready for review: yes

## 2026-11-03: Bot SDK check
- Did: checked Jackson's import of the Quantum Bot SDK 3.10.6 (Oct 16, 2026) against our pinned Quantum 3.1.0 Preview 2189. Its simulation and view code compile into our own assemblies through asmrefs, with its `.qtn` types in our codegen; the visual editor is a prebuilt DLL.
- Verified: a clean full recompile has no errors or warnings (the errors right after the import were from before codegen ran); all agent, blackboard and config types load; the editor menu (Tools › Quantum Bot SDK) and window open without errors; a match still plays (glued Stack tower push: same result as before, 0.00–0.04 m); console clean. No Quantum update needed.
- Decided: bots on the Bot SDK (Jackson).
- Assets: main menu mock-up, logo, menu button and Jaro font filed under `Assets/Art/UI` (A-002).
- Questions for Jackson: the next phase's shape (menu, spectator and bots before a plan review).
- Next: replan the next phase with Jackson.

## 2026-11-03: Stack glue; block pivot
- Did: Stack sauce glues a block to each block it touches and to the ground (character joints at the shared face, 2° of bend), torn by a hit of 3–7 m/s into the contact by strength, a grab, or 10 cm of stretch; `SauceSystem.ReleaseGlue` frees a block before a dev command or the fall reset moves it. Removed the block model's 0.4 m offset in `PH_Block` now that `BL_Block`'s pivot is at its center.
- Verified (one client): a lone Stack block walked into didn't move (0.00 m, plain 0.29); a 3-block Stack tower walked into moved at most 0.04 m and under a ragdoll slam at most 0.14 m, glue intact; with the break speed lowered to 2 m/s the same slam tore it and the tower fell, then the blocks re-glued where they landed; grabbing a ground-glued block freed it. Block model sits on its collider (render). Console clean.
- Decided: glue is to the ground too, so a Stack tower can't be tipped over whole (without it the glued tower fell over as one piece under the slam).
- Assets: none new (the centered block came with this morning's export).
- Questions for Jackson: glue strength, and glue to the ground or only to blocks.
- Next: Jackson's play-test and review of Phase 5.

## 2026-11-03: Phase 5 tweaks from Jackson's review
- Did: Stack sauce reworked from a shove to sticky friction (four high-friction materials, the opposite of Slip; the shove, its cooldown, event and pulse ring are gone). Grab keeps the block's most-upward face on top (nearest of a cube's 24 square orientations) and placing keeps it. Vat collision reshaped to the cup: 24 walls leaning out 14° along the inner wall, plus its floor; Lobby map rebaked. Sauce coat drawn on its own evenly divided rounded-box shell with lumpy, bead-edged volume and lit by the displaced surface; drips off.
- Verified (one client): a block landed on its side (local +Z up) stayed +Z up when grabbed, held and placed. Dunking still works (68% dipped, full Stack strength); a block let go in the cup rests on the floor; a player running at the cup stops at the wall (1.17 m from center) without riding up. Stack and Slip towers under a ragdoll slam: plain and Stack stood (blocks moved about 0.4–0.6 m), Slip fell apart. Coat checked in close-up renders. Console clean. Not rechecked with two clients: the changes are simulation-side FP math and physics materials, with no new input or events.
- Tried and dropped: extra weight on resting Stack blocks (to resist tipping) made Stack towers shed their top block under hits.
- Decided: a block with both sauces takes the stronger one by the difference; drips wait until the coat's volume is settled (Jackson).
- Assets: `block.fbx` (lower subdivision) over `BL_Block`; `sauce_cup.fbx` over `EN_SauceVat`. A-009 no longer needs a cup re-export.
- Questions for Jackson: is grip enough for Stack sauce, or should Stack blocks also glue to the blocks they touch? (Glue next.)
- Next: Jackson's play-test and review of Phase 5, then Phase 6.

## 2026-11-02: Phase 5, done-when verified
- Did: Stack shove once per player per tick; knockback and pulse events no longer fire twice on a predicting client (`nothashed` fields).
- Verified: the Phase 5 done-when with two clients (report above).
- Decided: a tower's sauced blocks fire and cool down together; a player takes one shove per tick.
- Assets: none.
- Questions for Jackson: as in the entry below.
- Next: Jackson's review of Phase 5, then Phase 6.

## 2026-11-02: Phase 5, sauces; aim collisions; Guy and block deliveries
- Did: aimed block on an `AimedBlock` layer that collides with nothing, model arcs over the head (runtime trigger toggling broke Quantum's broadphase; a disabled collider froze the body). Thrown blocks pass through their thrower and whatever they were released inside for 0.15 s. Sauce simulation (`SauceSystem`, `SauceConfig`): vats, dip planes, strength, Stack shove, Slip friction. Coat shader and view, bubbles, glints, splash, Stack pulse ring. Dev command `CommandDevSauce` (F8 / Shift+F8). Win and lose states in the Animator.
- Verified: aim (model clears the head at 2.1 m, no errors); first throw out of a stack at full speed; dunk into a cup (one dip, 69%, Stack strength 1.0, lifted out cleanly, player stays outside); Slip throw slides 7.9 m against 1.1 m plain; winner plays Win through round end and results; coat rendered in a capture.
- Decided: vats are entities (`SauceVat`) with static wall colliders; the cup scaled 1.3× so players can't step in, collision as 12 vertical boxes on a `Vat` layer the hold reaches over; Stack sauce shoves rivals within 2.5 m every 2 s while stacked (3–5.5 m/s, never a knockdown); Slip in four friction steps; dips under 5% dropped, dips end 0.25 s after leaving the sauce; strafe timescales 1.1× walk, 2.25× run.
- Assets: Guy re-export (the first one had no motion in any clip; the second is in): `Win.Jump` and `Sitdown` placeholders, longer strafes. `BL_Block` subdivided (4488 vertices). `EN_SauceVat` (the cup). A-005 removed; A-013 moved to Phase 6; A-009 now covers sauce looks; A-024 down to the slide; A-014 gains sauce sounds.
- Questions for Jackson: Stack sauce shove every 2 s, or once when stacked? Drips away from the coat (glaze) or down with gravity? Re-export the cup at 1.3× with straighter outer walls?
- Next: two-client done-when check, phase report.

## Phase 4 report: Character
- Built: every player state animated on Guy. Idle; walk and run blending forward, back and strafe; jump start, loop and land; sit and slide; stagger flinches by hit side; `GrabBlock` hold with spine turn and hand IK; the aim pose (`Throw` frame 4) and follow-through, grounded and airborne; knockdown ragdoll on Guy's bones and get-up blends; foot IK on steps, ramps and blocks; capsule at Guy's 0.40 m radius. Camera: never locks while holding (gradient), moves over the shoulder while aiming.
- Done-when verification ("Guy plays a full match on uneven terrain with no state left unanimated"): two clients through MPPM (`Assets/Settings/PlayMode/TwoClients.asset`, main editor plus one clone) in one online room. Player A ran a scripted match: start, walk, run, slide into a sit, jump, grab, carry with strafes and back-steps, swing past the turn threshold, aim and throw, aim in the air and jumpshot, place, knockdown and get-up, then stood still while player B (scripted) ran into it for a real stagger, then stacked three blocks to win. The match went Lobby → Countdown → Playing → RoundEnd → Results → Lobby. A logged every Animator state each character's view reached, as seen on A:
  - A (local): Idle, Walk, Walk back, Walk strafe, Run, Run back, Run strafe, JumpStart, JumpLoop, JumpLand, Sit, Sit with the slide clip, GrabBlock, Aim (on the ground and in the air), Throw, StruckBack, StruckLeft, ragdoll, GetUpFaceDown, foot IK planted; tiers InControl and Staggered.
  - B (remote, interpolated): Idle, Walk, Run, Run strafe, JumpLoop, JumpLand, foot IK planted.
  - No state outside the controller's known ones, on any layer.
  - Win and lose: Idle during RoundEnd and Results, as decided.
  - Uneven terrain: foot IK on the Lobby's step and ramps (checked earlier today; planted in this run too).
  - A separate replay of the jumpshot: aiming on the ground, airborne at 0.61 s, thrown in the air at 1.01 s.
  - Console on both clients: no game errors.
- Known issues: see Current state (aim block offset, strafe foot slide, block height while sitting, long arms on dropped hips).
- Placeholders still in use: `PH_PlayerCapsule`, `PH_Block` (draft `BL_Block`, A-005), `PH_LockDither` (A-007/A-013), `PH_` terrain (A-008), IMGUI `MatchHud` (A-006), stock start UI (A-002); win and lose on `Idle` (A-023).
- Ready for review: yes

## 2026-11-02: Phase 4, holding camera gradient; done-when verified
- Did: the orbit camera no longer locks behind the body while holding. It turns toward the swing at 35% of the input in the middle, rising smoothly (cubic) to all of it at the 45° body-turn edge, then turns with the body; vertically it tilts the same way toward the ends of the swing pitch or throw angle and fully past them. View only (`OrbitCamera`, `LocalPlayerView`). Added the `TwoClients` MPPM scenario. Ran the done-when (report above).
- Verified: swinging right at 60°/s, the camera turned 3° → 7° → 16° while the block swung 8° → 25° → 40°, then followed the turning body; raising the block tilted the camera from 15° to −28° throughout; raising and lowering the throw aim tilted it 15° → 5° → 21°.
- Decided: the camera yaw is a function of the swing (no drift: swinging back returns it); the middle share is a tunable (`Swing Camera Share`). The Lobby scene now stores the camera's three new fields at their defaults.
- Assets: none.
- Questions for Jackson: does the gradient feel right (0.35 in the middle)?
- Next: Phase 4 review, then Phase 5.

## 2026-11-02: Phase 4, aim pose and jumpshot from Throw frame 4
- Did:
  - **Guy re-export** with the two-handed `Throw` (12 frames): integrated.
  - **Aim** (Jackson): a frozen `Aim` state on the upper-body layer holds `Throw` frame 4, the wind-up with the block overhead. The block's model rides between Guy's hands there (`HeldBlockPoseView`), while the real block stays at the 30° aim point; the torso turn and hand IK give way to the pose while aiming. Releasing plays `Throw` on from frame 4 and hands the model back to the flying block over 0.08 s.
  - **Jumpshot** uses the same pose and clip over the jump; the upper body fades back to `JumpLoop`.
  - **Aim camera:** the orbit camera moves 0.9 m right and 0.3 m up while aiming. The raised block hid the aim line and landing circle from the usual spot.
  - **Place and win/lose:** no place clip (letting go already blends out of the hold); win and lose stand on `Idle`. A-021 and A-022 removed from the asset list.
  - **Spectator and repeatable test match:** assessed and placed in Phase 6 and Phase 7 (see the plan's Decisions).
- Verified (play mode, one online client, scripted input):
  - Grounded: Aim holds at normalized time 0.333 (frame 4) for the whole hold; the block's model sits 0.72 m behind and 1.67 m above his feet, his hands on its sides. On release `Throw` runs on from 0.36, and the model closes on the real block 1.20 → 0.77 → 0.50 → 0.26 → 0.05 m over 5 ticks, then tracks it within 0.02 m.
  - Airborne: the same, not grounded throughout, jumpshot speed.
  - Captures: from the usual camera spot the raised block hid the aim; from the shoulder the line and circle are clear.
  - Console: no game errors.
- Decided: the block's model, not the simulation's block, goes overhead, so nothing in the simulation changes and a throw still launches from in front (see Known issues).
- Assets: A-021 delivered as the reworked `Throw`; A-022 dropped; A-023 stays (in development).
- Questions for Jackson: does the shoulder camera feel right while aiming?
- Next: the Phase 4 done-when with 2+ clients through MPPM.

## 2026-11-02: Phase 4, sit, strafe, flinch, foot IK, capsule
- Did:
  - **Guy re-export** (22 actions): integrated. New `Crouch` (the sit-down), `Slide`, `StrafeLeft`/`StrafeRight`, `StruckFront`/`Back`/`Left`/`Right`. Strafe and Slide now loop on import.
  - **Sit replaces crouch** (Jackson): the crouch button sits. Sitting is stationary (no steering, turning or jumping; it only brakes). Pressed while moving 4 m/s or faster it slides instead (the Run button is no longer required), ending in a sit while held. Capsule 1.30 m while sitting or sliding (his sitting head height), was 1.10. Config fields renamed: `SitHeight`, `SlideEndSpeed`; `MovementData.IsSitting`.
  - **Capsule radius 0.40 m** (was 0.35), matching Guy; collider, config and the hidden body visual updated.
  - **Animator:** Walk and Run are 2D directional blends on the move direction relative to facing (forward, back, strafe left, strafe right; strafes play 2× in Walk and 4× in Run). A Sit state blends `Crouch` into `Slide` by ground speed, entered from any state while sitting. A full-body Flinch layer plays the `Struck` clip for the side that was hit on a stagger, then fades out; skipped while sitting or down.
  - **Foot IK** (`FootIKView`): raycasts the predicted frame under each foot (statics and blocks), drops the hips for the lower foot, bends each leg to keep the foot's animated lift, tilts the foot to the slope. Only while idle, walking or running on the ground. Runs before the hold pose. The arm IK is now a shared `TwoBoneIK` helper.
- Verified (play mode, one online client, scripted input):
  - Sit: capsule 1.30 m, Sit state, zero movement under full stick, stands on release.
  - Slide: running 6 m/s → crouch → slide at 7.3 m/s → ends after 0.75 s → sits and brakes to 0 in 0.7 s → stands on release. Sit state throughout.
  - Strafe while carrying: MoveX +1.0 walking right, −1.0 running left, Walk→Run on speed.
  - Flinches: each hit side plays its clip at full weight, then the layer returns to 0.
  - Foot IK straddling the 0.30 m step: hips dropped 0.30 m, the floor-side ankle 0.08 m above the floor, the step-side one 0.06 m above the step. Captures with IK off and on show the floating foot planted.
  - Hold: wrists 0.05 m outside the block's side faces; the block stays clear of the chest from 0° to 60° of swing.
  - With the 0.40 m capsule: grabbed a block 1.25 m away; placed one on a ground block: settled at y 1.204 (1.2 exact), 3 cm off center, asleep within 0.5 s.
  - Console: no game errors (only the agent's test-tool errors).
- Decided: the slide clip plays above 2 m/s while sitting; flinches are full body (the clips step) and play once at normal speed whatever the stagger length.
- Assets: A-017 to A-020 delivered and integrated, removed from the list; A-024 added (strafe stride, slide motion; nice to have).
- Questions for Jackson: none new.
- Next: integrate A-021 (aim, jumpshot) when it lands, then A-022 and A-023, then the Phase 4 done-when.

## 2026-11-02: Pre-Phase 4, vertical camera while holding; docs reset
- Did: while holding a block, up/down input past the top (60°) or bottom (−25°) of the block's swing now tilts the camera, so the view can be aimed vertically while carrying. Not while aiming a throw (vertical input sets the throw angle there). View only (`OrbitCamera`, `LocalPlayerView`). Docs rewritten to the current build: history removed, `ASSETS.md` is now one priority list, the art pipeline moved into `PLAN.md`.
- Verified: compiles clean in the open editor (`unity recompile`). Not yet played by hand.
- Decided: the tilt takes exactly the input the swing can't use, so it starts at the edge and stops the moment input stops, with no drift.
- Assets: list reordered for Phase 4; new requests A-017 to A-023. Integrated items removed.
- Questions for Jackson: the Phase 3 play-test and the two open questions in the plan.
- Next: hold for art and Jackson's review, then Phase 4.
