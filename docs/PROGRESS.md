# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports (Phases 0–7, the plan review, Bot Enhancement's log, report and full soak review) live in git history: `git log -- docs/PROGRESS.md`.

## Current state (2026-11-06)

**Phases 0–7, the plan review and Bot Enhancement are done and reviewed. Bots are set aside. The Blocks phase (stacking that behaves) is built and waits for Jackson's hand check (Blocks report). Touch-ups is half built: grab what the camera looks at, Guy's head looking at what he'd grab, and the 5-minute clock with sudden death are in; the grab highlight and the settle/stack highlight wait on Jackson's design. Then Phase 8 feel and juice and Phase 9 playtest build.**

What plays today, online with 2–6 players, in the Arena scene (the Lobby is a test scene with the sauce vats):
- **Front end:** the game opens on Jackson's menu over an offline all-bot demo match watched by the spectator director. Start Game hosts a room with a 5-letter code; Join Game joins by code or any open room. The room screen lists the players; the host adds and removes bots, sets each bot's difficulty (`< NORMAL >`), sets the bot fill and starts the match. The HUD shows the countdown, everyone's tower slots, the winner and the results, then the room screen comes back. Settings: volume, look speed, invert Y, fullscreen, resolution. Placeholder sounds throughout.
- **Arena:** a walled bowl with uneven ground, six spawn pillars with stairs, a raised inner ring and a dip in the middle; each player starts on their pillar with their home block, their second block on the ring across the middle, the third in a pile of six in the middle. Camera collision with the arena.
- **Guy:** looks (head, chest, eyes) at the block a grab would take; force-driven capsule; walk/run (analog on gamepad), jump, sit, slide, stagger; grab, hold and swing, place with flush settling, hold-to-aim throw with trajectory preview, jumpshot; knockdown into an 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips. Animation for every state, foot and hand IK.
- **Stack & Lock:** 3 owned blocks each, settle dither, counting, win, a 5-minute clock with sudden death (the HUD headline shows it), full match loop. Grab takes the block the camera looks at. Stacks hold still: a block let go squarely enough to count stays where it was put (the rest assist, `BlockRestSystem`), and towers still fall when hit.
- **Sauces** (Lobby only): Stack (sticky, glues) and Slip (slides); a lumpy coat shader. Dev keys F8 / Shift+F8 coat blocks.
- **Bots:** navmesh pathfinding with jump links, slide tackles, four skill levels, three hidden personalities (Wrecker, Builder, Klutz with mistakes), a jump-grab for blocks lying on towers. Dev builds show each bot's personality and skill on the spectator label and the room screen, and `BotSoakLog` writes a `[BotSoak]` line per match.

### Known issues
- **Stacking (Blocks phase, waiting on Jackson's hand check):** the bench says stacks hold; how stacking three feels by hand isn't checked yet. A held block carried in low enough to ram the top block's side still knocks it (that's a hit, not a slip). Bots still land only about half their placings (their line-up, not the physics).
- **Touch-ups, waiting on Jackson:** no grab highlight yet (Guy's head look shows the target meanwhile); the camera-aim grab and sudden death are unplayed by a person. A sudden-death winner is listed first on the results even with fewer blocks. Sudden death reuses the match-start sound.
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
- A play-test of the Touch-ups so far: grabbing by looking (with the head look as the only cue until the highlight), the 5-minute clock and sudden death (the rule is my proposal), and whether the head look reads.
- A hand check of the Blocks fix: stack three in the Lobby or a match. Is the difficulty now the reach, not sliding? Is the third block too easy now?
- Detailed design for the grab highlight and the settle/stack highlight (A-025).
- A-002 button focus states and the remaining screen mock-ups; A-006 HUD mock-ups (now with the 5-minute clock).
- Open questions in the plan (sudden-death rule, settle numbers, swings through your own tower, Stack glue).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip`, `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain in the Lobby test scene, `PH_MainMenu` (Jackson's main menu art; other screens are drafts, A-002), `PH_MatchHud` (A-006), the IMGUI spectator bar (A-006), `PH_RagdollTest` (stress-test scene only), `PH_SoundBank` (A-014).

## Next: Touch-ups: the two highlights

Design in `docs/PLAN.md` › Touch-ups; items 1 and 4 are built. Items 2 and 3 (the grab highlight and the settle/stack highlight) wait on Jackson's detailed design (A-025); both are view only and read the simulation (`GrabSystem.PeekGrabTarget` for the grab target, the same query `HeadLookView` uses; the place spot from `GrabSystem.LimitToRestOnBlockBelow` and `TryGetBlockBelow` with `BlockStackSystem`'s center and level tests). Then Jackson's play-test of the whole phase and its report.

What was checked for items 1 and 4 (2026-11-06):
- Grab by aim: a 39-minute all-bot Arena soak (35 matches, 0 errors) with bots looking at their target blocks: placings land 51% (52% before), fetches 33 a minute (31), raids 10.5 (8.7), match length the same (median 48 s). Not played by a person yet.
- Head look: close-up renders of bots in the demo show the head and eyes turned toward the block they're about to grab.
- Clock: with the clock shortened to 20 s and sudden death to 15 s (in memory, not saved), 48 all-bot matches all went to sudden death and ended within 35 s: most by the next block to count, 4 at sudden death's time-out (2 of them draws). In an online room with bots, screenshots show the clock in the headline, "SUDDEN DEATH!" as it starts, and the round end and results (headline shortened after the sudden-death one overflowed its bar). At the normal 5 minutes, no all-bot match reached sudden death (towers stand now; the longest ran 185 s).

## Blocks report (2026-11-06)
- Built: the block bench (`Assets/Scenes/Dev/BlockBench.unity`, **Block Sauce › Build Block Bench**; `BlockBenchSystem` runs `BlockBenchTrial`s through the real place code, `GrabSystem.SetDown` and `DetachHeld`, and the hold's chase for carried blocks; **Block Sauce › Block Bench › Run Placing / Carry / Hit / Sauce Panel**; `BlockBenchStarter` starts the scene locally, with retries). Fixes: counted blocks no longer sleep early (`GameModeConfig.SleepCountedBlocks`, off); the rest assist (`BlockRestSystem`, `GameModeConfig.RestAssist`: half of any movement under 0.25 m/s and 1 rad/s relative to what a block rests on is taken off each tick; Slip-sauced blocks skipped); a held block over a block rests on it instead of being pressed in (`PlayerGrabConfig.HoldRestsOnBlockBelow`).
- What the bench found (a two-block tower, the third block let go 2 cm above it, 15 s recorded; slide is the most any block moved sideways):
  - Before: a centered third block never slid, asleep or awake, dropped from up to 30 cm, turned up to 45°, squared or not. Off center it slipped: 2 cm off slid the tower 18 cm and tilted it 5°; 10 cm off fell after 5–15 s; a fourth block 5 cm off brought all four down. A second block on one stayed put up to 15 cm off. Pressing a carried block 15 cm into the tower shoved it apart before the let-go.
  - Suspects, one at a time: keeping blocks awake cut the slides to 2–9 cm with no falls (the main cause); doubling solver iterations helped a little, only with sleep on; lower penetration correction was much worse; Baumgarte, angular drag and friction changed nothing useful. With sleep off, a slow creep was left that grew over 15 s and could still topple a tower: that's what the rest assist damps.
  - After (placing panel): every placing 0–30 cm off center, diagonal, dropped 10 cm, on an offset base, on an awake tower, a fourth block, a second block: at most 2 cm of slide and 2.6° of tilt, no falls, all counted. Carried and pressed in up to 15 cm, at 1.5 and 3 m/s, onto three- and four-block towers: 1 cm, no falls. Pressed 30–50 cm (the block rams the top block's side while carried in low): still knocked over.
- Feel kept: hits (60 per setup: 4, 6, 8 and 12 m/s, each level, five points along the face) toppled the tower in 20 of 60 (40 blocks off) after, against 21 of 60 (37 blocks) before. Sauce: Slip towers fell to 3–4 m/s hits on the top block that plain and Stack towers stood through. Jump-placing the third block is unchanged (the fix only acts on resting blocks and the hold's height over a block).
- In matches (a 49-minute all-bot Arena soak, 49 matches, 0 errors): matches got much shorter (median 47 s, from 104 s; the longest 171 s, none over 5 minutes) because towers that are built stay up; bots still land 52% of their placings (their line-up, unchanged); raids 8.7, throws 5.8, tackles 4.0, knockdowns 3.3 a minute (about as before). Builders won 31% of their matches, Wreckers 8%.
- Done-when: on the bench, met (every placing within the stack tolerance moved at most 2 cm, no falls, asleep or awake; hits as before). Jackson's hand check: waiting.
- Known issues: see Current state.
- Placeholders still in use: none new.
- Questions for Jackson: (1) stack three by hand: is the difficulty now the reach? (2) is the third block too easy now, or is the jump-place enough? (3) a block held low over a tower now sits on top instead of pushing into it (slamming one down onto a rival's tower lands on top): any case where you wanted to press into a tower?
- Ready for review: yes

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

## 2026-11-06: Touch-ups: grab by aim, head look, clock and sudden death
- Did: `GrabSystem.FindAimedTarget` (the camera's center line through the camera pivot, first block hit or nearest within 0.6 m, else the old nearest-in-cone rule; `PlayerGrabConfig.GrabWhereAimed`, `AimPivotHeight`, `AimMaxMiss`, `AimFallbackToNearest`); bots look at their target block within 3.5 m (their move input turned to match); `HeadLookView` on `PH_PlayerCapsule` (Jackson's ask mid-session); `GameModeConfig.MatchSeconds` (300) and `SuddenDeathSeconds` (120), `Match.SuddenDeath`, event `SuddenDeathStarted`, `MatchSystem.UpdateClock`; the HUD clock, SUDDEN DEATH! and DRAW; `BotSoakLog` adds `suddenDeath=`; the bench mode has no clock.
- Verified: see Next › What was checked.
- Decided: a near-miss tolerance and the old rule as fallback, so a grab still works when the camera isn't on a block (the highlight will show which); the head look uses the grab's own query, so it always shows the block a press would take; the sudden-death winner is the player whose counted blocks go above the fewest they had since sudden death began (a rebuilt block counts as the next block).
- Questions for Jackson: in Waiting on Jackson.
- Next: the two highlights when Jackson's design arrives; his play-test.

## 2026-11-06: Blocks: bench, causes, fix
- Did: the block bench (scene, builder, `BlockBenchSystem`, panels, starter); `GrabSystem.SetDown`, `DetachHeld` and `LimitToRestOnBlockBelow` made public for it; the three fixes (counted-block sleep off, `BlockRestSystem`, the hold resting on the block below). Docs: plan (Blocks phase, the Stacking decision, a pace question), progress (Blocks report, Touch-ups next), `CLAUDE.md` (Block bench under Testing).
- Verified: the bench panels before and after (Blocks report), the hit and sauce panels, a 49-minute Arena bot soak (0 errors).
- Decided: a rest assist over solver settings (iterations, penetration correction and Baumgarte didn't fix it, and they change ragdolls and players too); its numbers (0.25 m/s, 1 rad/s, half per tick) from the bench (a fifth per tick left 2–3 cm and didn't stop a pressed-in block); counted blocks stay awake (the engine's own sleep still applies, so a still tower sleeps after 0.4 s).
- Questions for Jackson: in the Blocks report.
- Next: Jackson's hand check; Touch-ups items 1 and 2 can start.

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
