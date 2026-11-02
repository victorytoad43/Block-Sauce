# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports live in git history.

## Current state (2026-11-02)

**Phases 0–3 are done. Phase 4 (Character) is next, holding for art.**

What plays today, online with up to 8 players, in the Lobby scene:
- Guy on a force-driven capsule: walk/run (analog on gamepad), jump, crouch, slide, stagger.
- Grab, hold and swing (body turns past 45°; camera tilts at the swing's top and bottom), place with flush settling, hold-to-aim throw with trajectory preview, jumpshot.
- Stack & Lock: 3 owned blocks each, settle dither, counting, win, full match loop back to the lobby.
- Knockdown: Guy's 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips, dazed indicator, falling out of the world resets.
- Animation: idle, forward/back walk and run, jump, GrabBlock hold with spine turn and hand IK, throw follow-through.

### Known issues
- The physics capsule (0.35 m) is narrower than Guy (0.40 m): he overlaps walls and blocks by about 5 cm. Phase 4.
- Hands sit a little inside the block's side faces; a block raised above about 40° can reach into Guy's chest (visual only). Phase 4.
- Sideways moves, crouch, slide and stagger play no matching clip yet (A-017 to A-020).
- The camera has no wall collision. Phase 6.
- A ragdoll that spawns half inside a block can launch it; the fall reset recovers it. If it shows up in play, spawn the ragdoll in a tucked pose.
- A body still airborne when its knockdown ends stands up mid-air and falls.
- A player who joins mid-match gets no blocks until the next round.
- Swing input is sent as per-tick deltas, so a repeated lost input repeats one tick of swing. Revisit only if swings feel jumpy online.
- The start menu has a stray white banner box (stock Quantum start UI, A-002).
- Matches only run in the Lobby scene; Arena_Graybox has no `Match` yet (Phase 6).

### Trailing items from before Phase 4
- **Jackson's play-test of Phase 3** and of the 2026-11-01 stacking and hold iteration, plus the two open questions in the plan (settle numbers; pickup swings through your own tower).
- **The vertical camera tilt (2026-11-02)** compiles clean but hasn't been played by hand yet; it belongs in that same play-test.
- **Phase 2 art direction** that never landed: A-013 (owner readability), A-007 (settle effect), A-006 (HUD). None blocks Phase 4; placeholders hold. They're scheduled for Phases 5–6 in `docs/ASSETS.md`.

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the draft `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_PlayerColors` (holds the real palette), `PH_` terrain (A-008), the IMGUI `MatchHud` (A-006), the stock Quantum start UI (A-002), `PH_RagdollTest` (stress-test scene only).

## Phase 4 plan: Character

**Intention:** finish Guy so no game state goes unanimated, then widen his capsule to his real size. The simulation barely changes in this phase; most of the work is the Animator, IK, and retesting reach after the capsule change.

Build order, once the clips land:
1. **Capsule to 0.40 m.** Retest grab reach, hold distance, stacking, slide and crouch headroom. Do this first, since everything after is tuned on the final body.
2. **Locomotion blend.** Replace the forward/back blends with a 2D blend on velocity relative to facing (forward, back, strafe left/right), walk and run tiers, rates matched to ground speed. Add crouch idle/walk and the slide (enter, loop, exit) as states driven by the simulation's movement state.
3. **Upper body.** Stagger flinch by hit direction (from the `KnockedBack` event); two-handed aim pose while Throw is held, airborne variant for the jumpshot; place clip on `Placed`.
4. **Foot IK** on uneven terrain: raycast each foot, pelvis offset, view only.
5. **Hold polish:** hands on the block's faces, and keep a raised block out of the chest (pull the hold back at high swing pitch, view only, or lower the pitch limit if that reads better).
6. **Round end:** win and lose clips on Round end and Results.
7. **Verify:** a full match on uneven terrain with 2+ clients, cycling through every state; console clean; report.

Risks: the capsule change shifts tuned reach and stacking feel; the 2D blend needs the strafe clips to match the forward cycles' timing to avoid foot sliding.

Can start before art: step 1 (capsule) and step 4 (foot IK) need no new clips. Holding them anyway until Jackson's play-test of Phase 3, so feedback lands before the body changes.

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

## 2026-11-02: Pre-Phase 4, vertical camera while holding; docs reset
- Did: while holding a block, up/down input past the top (60°) or bottom (−25°) of the block's swing now tilts the camera, so the view can be aimed vertically while carrying. Not while aiming a throw (vertical input sets the throw angle there). View only (`OrbitCamera`, `LocalPlayerView`). Docs rewritten to the current build: history removed, `ASSETS.md` is now one priority list, the art pipeline moved into `PLAN.md`.
- Verified: compiles clean in the open editor (`unity recompile`). Not yet played by hand.
- Decided: the tilt takes exactly the input the swing can't use, so it starts at the edge and stops the moment input stops, with no drift.
- Assets: list reordered for Phase 4; new requests A-017 to A-023 (strafe, stagger, crouch, slide, two-handed aim and jumpshot, place, win/lose). Integrated items removed.
- Questions for Jackson: the Phase 3 play-test and the two open questions in the plan.
- Next: hold for art and Jackson's review, then Phase 4 per the plan above.
