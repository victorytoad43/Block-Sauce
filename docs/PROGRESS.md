# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports (Phases 0–7, the plan review, Bot Enhancement's log, report and full soak review) live in git history: `git log -- docs/PROGRESS.md`.

## Current state (2026-11-06)

**Phases 0–7, the plan review and Bot Enhancement are done and reviewed (Jackson, 2026-11-06: "the current bots are at a great spot"). Bots are set aside. Next is the Blocks phase (stacking that behaves), then Touch-ups (grab what the camera looks at, the two highlights, a 5-minute timer with sudden death), then Phase 8 feel and juice and Phase 9 playtest build.**

What plays today, online with 2–6 players, in the Arena scene (the Lobby is a test scene with the sauce vats):
- **Front end:** the game opens on Jackson's menu over an offline all-bot demo match watched by the spectator director. Start Game hosts a room with a 5-letter code; Join Game joins by code or any open room. The room screen lists the players; the host adds and removes bots, sets each bot's difficulty (`< NORMAL >`), sets the bot fill and starts the match. The HUD shows the countdown, everyone's tower slots, the winner and the results, then the room screen comes back. Settings: volume, look speed, invert Y, fullscreen, resolution. Placeholder sounds throughout.
- **Arena:** a walled bowl with uneven ground, six spawn pillars with stairs, a raised inner ring and a dip in the middle; each player starts on their pillar with their home block, their second block on the ring across the middle, the third in a pile of six in the middle. Camera collision with the arena.
- **Guy:** force-driven capsule; walk/run (analog on gamepad), jump, sit, slide, stagger; grab, hold and swing, place with flush settling, hold-to-aim throw with trajectory preview, jumpshot; knockdown into an 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips. Animation for every state, foot and hand IK.
- **Stack & Lock:** 3 owned blocks each, settle dither, counting, win, full match loop.
- **Sauces** (Lobby only): Stack (sticky, glues) and Slip (slides); a lumpy coat shader. Dev keys F8 / Shift+F8 coat blocks.
- **Bots:** navmesh pathfinding with jump links, slide tackles, four skill levels, three hidden personalities (Wrecker, Builder, Klutz with mistakes), a jump-grab for blocks lying on towers. Dev builds show each bot's personality and skill on the spectator label and the room screen, and `BotSoakLog` writes a `[BotSoak]` line per match.

### Known issues
- **Stacking (the Blocks phase):** a third block placed on a two-block tower often sets all three slipping against each other until they topple (Jackson's play-test); about half of all placings land, for players and bots alike, and most towers that fall are knocked by their own builder.
- **Grab and match end (Touch-ups):** grab takes the nearest block, so blocks on a tower's top need a jump-grab and a grab at a rival's short tower yanks its base out; there is no match time limit.
- Bots don't use the stairs (they hop onto pillars and the ring); now and then they wedge against each other or a wall for a few seconds.
- Blocks sometimes rest tilted against a pillar's rim or the ring's edge and don't count until moved.
- The editor runs a match at 20–33 fps on Jackson's PC (waiting on presentation); a build runs far faster. A small hiccup on knockdowns (Phase 9 profiling).
- Quantum's view updater sometimes asserts (`group.Buffer != default`) for about 3 s while creating a ragdoll body's view (view only).
- A ragdoll that spawns half inside a block can launch it; a body still airborne when its knockdown ends stands up mid-air and falls.
- Sitting while holding keeps the block at the standing hold height. A block dunked hard can stick at a cup's bottom for a moment; sauce particles emit over the whole block; a block let go off-center in a cup rests tilted.
- With the hips dropped for a low foot, Guy's long arms can touch the ground.
- A player who joins mid-match gets no blocks until the next round. Swing input is per-tick deltas (a repeated lost input repeats one tick of swing).
- Quantum's stats overlay shows over the menu. Photon logs an error line when a typed room code doesn't exist. "Session start timed out" is logged once when the offline demo starts in play mode (it retries and runs).
- The room's host is whoever holds the lowest connected slot; any player's room commands are still accepted by the simulation (the screen hides the controls).
- MPPM: starting `TwoClients` can take minutes or hang until restarted.
- The settings controls and the code field are drafts until A-002's screens arrive.
- `Arena_Graybox` is retired (still in the build list); it has uncommitted changes from outside the agent's work and is left alone until Jackson says what to do with it.
- The placeholder sounds are unheard picks by file name.

### Waiting on Jackson
- Detailed design for the grab highlight and the settle/stack highlight (A-025).
- A-002 button focus states and the remaining screen mock-ups; A-006 HUD mock-ups (now with the 5-minute clock).
- Open questions in the plan (sudden-death rule, settle numbers, swings through your own tower, Stack glue).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip`, `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain in the Lobby test scene, `PH_MainMenu` (Jackson's main menu art; other screens are drafts, A-002), `PH_MatchHud` (A-006), the IMGUI spectator bar (A-006), `PH_RagdollTest` (stress-test scene only), `PH_SoundBank` (A-014).

## Next: Blocks: stacking that behaves

Design and done-when in `docs/PLAN.md` › Blocks. No feel calls are needed to start; Jackson's hand check closes it.

Starting point:
- Code: placing is `GrabSystem` (the release and `SettlePlacedOnBlockBelow`, around line 341, with `TryGetBlockBelow`); stacking, counting and the instant sleep are `BlockStackSystem` (`SleepIfStill`); the hold is a velocity chase in `GrabSystem`. Tuning: `PlayerGrabConfig`, `QuantumDefaultConfigs` › Physics (solver 6 + 2 iterations, penetration correction 0.9, Baumgarte 0.5, sleep thresholds), `SessionConfig` (64 Hz), the block body on `PH_Block` (mass 2, angular drag 0.5), `Resources/PhysicsMaterials` (`BlockPhysicsMaterial` friction 0.8, the Stack and Slip steps).
- Tools: the eval and soak workflow in `CLAUDE.md` › Testing. The bench is new: a scene under `Assets/Scenes/Dev/` that is never in a match (built by a builder script, like the Arena), and an editor eval that drives trials through the simulation without a player (spawn blocks, place them with the same code path a release uses, record per tick).

Build order:
1. **Bench first, no fixes:** build the scene and the trial runner; reproduce Jackson's slip (a third block on two) and measure how often and how far blocks slide per case (asleep or awake, offset, release speed, squaring on or off, plain or sauced). Write the numbers into the log.
2. **One suspect at a time** (the list in the plan): change it in the bench only, keep what moves the numbers, revert what doesn't.
3. **Apply the fix to the game,** then check that hits still topple towers (throw, swing, body check, ragdoll), Stack glue and Slip still work, and jump-placing the third block is still the hard part. A bot soak afterwards measures placings landing (a side effect, not a target).
4. **Jackson's hand check** of stacking three, then the phase report.

Risks:
- Physics settings are global: more solver iterations or different sleep rules change ragdolls, players and throws too, and cost CPU (check the build's frame time, not the editor's).
- Making stacks too steady makes wrecking weak: keep a hit-to-topple check in every bench run.
- The editor runs slowly; judge physics by simulation ticks on the bench, never by eye at a low frame rate.

## Bot Enhancement: closing notes (2026-11-06)
The full report and the soak review are in git history (`e160987`). What still matters for the next phases:
- Placings hold about half the time at every bot skill level, and 60% of the blocks knocked off stacks came off while their owner was placing: likely the same stacking behavior Jackson saw by hand (the Blocks phase).
- The nearest-block grab rule makes lifting off a tower need a jump-grab (the Touch-ups grab rule replaces it).
- Matches run a median 104 s, a few over 5 minutes with Wreckers (the timer will cap them).
- Jackson's answers: grab what the camera looks at, with highlights to his design; a 5-minute timer with sudden death; Wreckers are fine (bots are for variety, not balance; balance is for play with humans); raise Klutz mistakes (done); the pillar start stays, and new maps are welcome; tackle rate, navmesh and long Wrecker matches are fine.

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

## 2026-11-06: Bot Enhancement reviewed; Klutz mistakes raised; plan set for Blocks
- Did: Jackson reviewed Bot Enhancement and answered the batched questions (in Bot Enhancement: closing notes). Klutz mistake chances raised (`BotPersonality_Klutz`: trip 0.03 → 0.075 a second while running, wild slide 0.04 → 0.1, drop 0.03 → 0.07, off-center placing 0.25 → 0.4, wild throw 0.3 → 0.5). Plan rewritten: bots set aside; a new Blocks phase (stacking that behaves: a block bench, the suspects, fixes, done-when) next, from Jackson's play-test of three-block slipping; Touch-ups now holds the camera-aim grab, the two highlights (Jackson's design), and the 5-minute timer with sudden death. ASSETS A-025 now waits on Jackson's design.
- Verified: a 53-minute rolled demo soak (12 matches): Klutzes now make 0.65 trips, 0.79 wild slides, 0.93 drops, 0.26 wild throws and 2.7 off-center placings a minute (about 2.6 visible mistakes a minute, up from about one). No new errors (the known "Session start timed out" line once).
- Decided: the sudden-death rule proposal (next block to count wins; most counted after 2 more minutes; a tie is a draw); the Blocks phase before the Touch-ups, since the highlights wait on Jackson's design and stacking needs nothing from him to start.
- Questions for Jackson: none new (the highlight designs, and the sudden-death rule at his play-test).
- Next: Blocks step 1, the bench.

## 2026-11-06: Bot Enhancement, dev view, soak stats, tuning, review
- Did: dev labels (`BotDevInfo.Describe`: " · Wrecker, Hard" in the editor and development builds, empty in release) on the spectator label (`SpectatorDirector.ShownPlayer`) and the room screen's bot rows; `BotSoakLog` (view, dev only; a `[BotSoak]` line per match); event `BotTaskEnded`; `BotSystem.TestBotCount` for head-to-heads. Fixes: home block re-read from the best tower; `FindBlockOnTop` (a rival's block, or its own unstacked one, on any of its top blocks, up to 2.4 m); `IsOnTopOfAnother` and a jump-grab in `Fetch` (jump, and grab once `PeekGrabTarget` is the target); stepping round when the grab would take a different block; backing off a target block it stands too close to (or on); the demo rolls personality as well as skill each match (`BotSystem.RollBots`). Docs: plan and progress rewritten around the review, with the old history left to git; ASSETS gains A-025 and the match timer.
- Verified: the soaks in the review and the report above; 0 errors in every soak; console clean after.
- Decided: the jump-grab instead of changing the grab rule for everyone (that is Jackson's call); the head-to-head gap documented rather than tuned by guesswork, since placing is the cause and is the next phase's first item.
- Questions for Jackson: in the Bot Enhancement report.
- Next: Jackson's review; then Touch-ups item 1 (bots place carefully), which needs no feel call.
