# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports live in git history.

## Current state (2026-11-02)

**Phases 0–3 are done. Phase 4 (Character) is in progress, waiting on Guy's aim, place and win/lose clips.**

What plays today, online with up to 8 players, in the Lobby scene:
- Guy on a force-driven capsule (0.40 m radius, his real width): walk/run (analog on gamepad), jump, sit, slide, stagger.
- Grab, hold and swing (body turns past 45°; camera tilts at the swing's top and bottom), place with flush settling, hold-to-aim throw with trajectory preview, jumpshot.
- Stack & Lock: 3 owned blocks each, settle dither, counting, win, full match loop back to the lobby.
- Knockdown: Guy's 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips, dazed indicator, falling out of the world resets.
- Animation: idle; walk and run blending forward, back and strafe; jump; sit and slide; stagger flinches by hit side; GrabBlock hold with spine turn and hand IK; throw follow-through; foot IK on steps, ramps and blocks.

### Known issues
- Strafe feet slide: the strafe clips step about 0.16 m/s, so they're sped up 2–8× and still can't keep up (A-024).
- Sitting while holding a block keeps the block at the standing hold height; the arms reach up to it. If it reads badly, lower the hold pivot while sitting (simulation).
- With the hips dropped for a low foot, Guy's long arms can touch the ground beside him.
- Flinches were checked by playing each one through the view; a real stagger in play still needs a look (the event path is unchanged from the knockback system).
- The camera has no wall collision. Phase 6.
- A ragdoll that spawns half inside a block can launch it; the fall reset recovers it. If it shows up in play, spawn the ragdoll in a tucked pose.
- A body still airborne when its knockdown ends stands up mid-air and falls.
- A player who joins mid-match gets no blocks until the next round.
- Swing input is sent as per-tick deltas, so a repeated lost input repeats one tick of swing. Revisit only if swings feel jumpy online.
- The start menu has a stray white banner box (stock Quantum start UI, A-002).
- Matches only run in the Lobby scene; Arena_Graybox has no `Match` yet (Phase 6).

### Waiting on Jackson
- Play-test of Phase 3, the stacking/hold iteration, the vertical camera tilt, and today's sit, strafe, flinch and foot IK.
- The two open questions in the plan (settle numbers; pickup swings through your own tower).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the draft `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_PlayerColors` (holds the real palette), `PH_` terrain (A-008), the IMGUI `MatchHud` (A-006), the stock Quantum start UI (A-002), `PH_RagdollTest` (stress-test scene only).

## Phase 4 plan: what's left

1. **Aim and jumpshot (A-021):** play the two-handed aim pose on the upper-body layer while Throw is held, the airborne version while aiming in the air; hand IK keeps the hands on the raised block. Then `Throw` follows through on release as now.
2. **Place (A-022):** play the set-down on the upper-body layer on the `Placed` event, blending to empty hands.
3. **Win and lose (A-023):** play on Round end and Results from `MatchStateChanged`, winner and losers by the match's winner.
4. **Verify the done-when:** a full match on uneven terrain with 2+ clients through MPPM, cycling every state (idle, walk, run, strafe, jump, sit, slide, stagger, knockdown, get-up, grab, carry, aim, throw, jumpshot, place, win, lose); console clean; phase report.

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
