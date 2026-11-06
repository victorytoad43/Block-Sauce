# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports (Phases 0–7, the plan review, Bot Enhancement's step-by-step log) live in git history: `git log -- docs/PROGRESS.md`.

## Current state (2026-11-06)

**Phases 0–7 and the plan review are done and reviewed. Bot Enhancement is done and waits for Jackson's review; its done-when holds except the head-to-head skill order (see its report). A wide review of the game from bot soaks (below) sets up the next phase, Touch-ups: core loop, then Phase 8 feel and juice and Phase 9 playtest build.**

What plays today, online with 2–6 players, in the Arena scene (the Lobby is a test scene with the sauce vats):
- **Front end:** the game opens on Jackson's menu over an offline all-bot demo match watched by the spectator director. Start Game hosts a room with a 5-letter code; Join Game joins by code or any open room. The room screen lists the players; the host adds and removes bots, sets each bot's difficulty (`< NORMAL >`), sets the bot fill and starts the match. The HUD shows the countdown, everyone's tower slots, the winner and the results, then the room screen comes back. Settings: volume, look speed, invert Y, fullscreen, resolution. Placeholder sounds throughout.
- **Arena:** a walled bowl with uneven ground, six spawn pillars with stairs, a raised inner ring and a dip in the middle; each player starts on their pillar with their home block, their second block on the ring across the middle, the third in a pile of six in the middle. Camera collision with the arena.
- **Guy:** force-driven capsule; walk/run (analog on gamepad), jump, sit, slide, stagger; grab, hold and swing, place with flush settling, hold-to-aim throw with trajectory preview, jumpshot; knockdown into an 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips. Animation for every state, foot and hand IK.
- **Stack & Lock:** 3 owned blocks each, settle dither, counting, win, full match loop.
- **Sauces** (Lobby only): Stack (sticky, glues) and Slip (slides); a lumpy coat shader. Dev keys F8 / Shift+F8 coat blocks.
- **Bots:** navmesh pathfinding with jump links, slide tackles, four skill levels, three hidden personalities (Wrecker, Builder, Klutz with mistakes), a jump-grab for blocks lying on towers. Dev builds show each bot's personality and skill on the spectator label and the room screen, and `BotSoakLog` writes a `[BotSoak]` line per match.

### Known issues
- **Gameplay (the next phase's work):** grabbing takes the nearest block, so blocks on a tower's top need a jump-grab and a grab at a rival's short tower yanks its base out; about half of all placings land and most towers that fall are knocked by their own builder; there is no match time limit, so matches with Wreckers can run 5–11 minutes.
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
- Review of Bot Enhancement and the review below; the batched questions at the end of the Bot Enhancement report.
- A-002 button focus states and the remaining screen mock-ups; A-006 HUD mock-ups; A-025 direction for the grab highlight and place preview.
- Open questions in the plan (grab rule, match end, Wreckers, settle numbers, swings through your own tower, Stack glue).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip`, `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain in the Lobby test scene, `PH_MainMenu` (Jackson's main menu art; other screens are drafts, A-002), `PH_MatchHud` (A-006), the IMGUI spectator bar (A-006), `PH_RagdollTest` (stress-test scene only), `PH_SoundBank` (A-014).

## Next: Touch-ups: core loop

Design in `docs/PLAN.md` › Touch-ups. Items 1 and 3 wait on Jackson's pick; the rest can start at once.

Build order:
1. **Bots place carefully** (no feel call needed). Measure first which placing step fails and how (`BotTaskEnded` with the step; the collapse classifier from this session: owner placing 0–3, owner walking past). Then: path round the bot's own tower when leaving it (a temporary obstacle or a detour point on the far side), back off a step after letting go before turning, let go only when the held block's speed is under a skill knob (`PlaceSteadiness`), and a slower final approach on lower skill. Re-run the head-to-head soak (10 matches a pair, sides swapped) until Expert beats Easy 8 of 10 and each level beats the one below.
2. **Grab targeting** (after Jackson picks): change `GrabSystem.FindGrabTarget` (one function; bots read it through `PeekGrabTarget`, so they follow). With rule (a) or (b), the bots' jump-grab is only needed when the target is out of reach. Then the grab highlight (view only, reads `PeekGrabTarget` for the local player) and its art direction (A-025).
3. **Place preview** (view only): the held block's landing spot on the top below it, squared like `Settle Placed On Block Below`, tinted by whether it would stack (`BlockStackSystem`'s center and level tests). A pure view of simulation state, so it predicts but never decides.
4. **Match timer and sudden death** (after Jackson picks): numbers on `GameModeConfig` (`MatchSeconds`, sudden-death rule), a `CheckWinner` path for time out; HUD clock (A-006 addition). Re-soak: no match past the timer by more than a minute.
5. **Bots, smaller:** stairs as walk links when the top step is within reach (or a stair-following waypoint), 1-v-1 raiding (raid weight up when only one rival), Builders guard more.
6. **Layout check with Jackson:** the review's tower positions; then the done-when with a mixed room of bots and Jackson's play-test.

Risks:
- A grab rule change touches every player interaction; it needs Jackson's play, not just soaks. Keep the old rule behind a toggle on `PlayerGrabConfig` until he signs off.
- Careful placing can make bots slow; the skill levels must still feel fast at Expert.
- A timer changes the game's pacing for humans more than for bots: try it in Jackson's play-test before tuning the length.
- The place preview predicts locally; in an online match a block can land differently from its ghost after a rollback. It only shows where the block would go, never snaps it.

## Review: the game from bot soaks (2026-11-06)

What was played: 137 six-bot demo matches (about 7.4 hours of simulated play: 80 with a fixed set of four Klutzes, one Builder and one Wrecker, then 57 with skill and personality rolled every match), 40 head-to-head matches (two bots, 10 per pair of levels, sides swapped every match, the personality rotated), 0 errors throughout. Every match was logged per player by `BotSoakLog` (counted blocks and where the best tower stood, fetches, raids, placings landed and tried, throws, tackles and hits, staggers and knockdowns caused and taken, slides, mistakes), and every grab was classified by where its block was.

**How matches go (57 rolled six-bot matches):**
- Length: median 104 s, mean 166 s; 7 of 57 over 5 minutes, the longest 688 s. With the fixed set (one Wrecker) the median was 100 s and the mean 119 s. Wreckers make matches long: every one of them raids, so towers keep coming down.
- A match minute holds about 13 placings landed, 11 raids (rival blocks fetched), 8 throws, 5 tackles, 3 staggers and 3 knockdowns. A tower needs two placings that hold, so most placings are rebuilding.
- Winning towers stood on the ground beside a pillar in 33 of 57 matches, on a pillar top in 14, and on the ring, in the middle or between in 10. Home blocks start on the pillars, but the pillar's edge is where they end up: pushed off by the builder's own approach or by raiders, and rebuilt on the ground below. The ring and the middle pile are crossed for blocks, not built on.

**Successful strategies:**
- Building wins: Builders won 24% of their matches (an even share is 17%), Klutzes 16%, Wreckers 10%. Winners made the most placings in 22 of 57 matches.
- Skill shows in six-bot rooms: Easy won 5%, Normal 19%, Hard 23%, Expert 20%. Easy loses mainly by hesitating (fetches 6 a minute against Expert's 9) and never raiding.
- Raiding is common but not decisive: 44 of 57 winners raided at least once, and the winner was among the least-hit players in only 14. Wrecking pays off for the rest of the room, not the Wrecker: Wreckers raided 12.6 blocks and threw 9.4 a match, and finished with the fewest counted blocks (0.67).
- In head-to-heads (two bots), there is almost no interaction (raids, throws and tackles under 0.4 a minute each): each match is a building race decided by how many placings hold, which is about half at every level (Easy 42%, Normal 54%, Hard 52%, Expert 54%).

**Interesting interactions:**
- The grab rule shapes play more than intended. Because the nearest block wins, a block resting on a tower can't be lifted from the ground: the block under it is always nearer. A rival's block dropped on your tower, or your own block landing off center on it, needs a jump-grab. Grabbing at a rival's two-block tower from the ground takes a block out from under another (about 35 such grabs of rival blocks in the 7 hours, and about 40 of the bots' own). Bots now jump-grab (about 50 lifts off towers in the 7 hours, where there were none before this session).
- Towers mostly fall to their own builder: of about 420 blocks knocked off stacks in 36 minutes of two Normal Builders, 60% came off while the owner was placing (approach, line-up, jump, letting go) and 25% while the owner was walking near its tower empty-handed; only 4% had nobody near.
- Tackles: about 5 a match minute, hitting about half the time; a hit makes the carrier drop its block, which is often the tackler's own and goes straight back to its tower.
- Klutz mistakes read as mild: per match, 0.5 trips, 0.5 wild slides, 0.9 drops, 4 off-center placings, 0.2 wild throws.
- Stalls: before this session's fixes, a head-to-head ran over 10 minutes with both bots stuck (one standing on its own target block, the other unable to fetch its third block lying off center on its tower). Both fixed; matches can still run long because nothing ends them.

**Opportunities (laid out as phases in `docs/PLAN.md`):**
- Touch-ups: grab targeting and a grab highlight; placing that holds (bots) and a place preview (players); a match timer with sudden death; bots on stairs, raiding in 1-v-1, guarding more; a layout check.
- Phase 8: the grab highlight's and place preview's art, plus the planned juice and readability.
- Expansion candidates: wrecking that pays (captured blocks, a meter, multi-round scores); sauce back in the arena as a contested spot; a better 2–3 player game (fewer blocks, a shared pile, a smaller map).

## Bot Enhancement report
- Built: navmesh pathfinding with jump links (**Block Sauce › Bake Arena NavMesh**), slide tackles and travel slides, four skill levels (`BotSkill` assets) set per bot on the room screen, three hidden personalities (`BotPersonality` assets) scoring the bot's choices, Klutz mistakes, a jump-grab for blocks lying on towers (and a Builder clearing a rival's block off its top), the demo rolling skill and personality every match; dev labels (`BotDevInfo`: the spectator label and the room screen's bot rows, editor and development builds only) and `BotSoakLog`; events `BotTaskEnded`, `BotTackleEnded`, `BotMistake`; test switches for soaks. Fixes from the soaks: a stale home block, grabbing its own tower's top while fetching (the bot now steps round to where the grab takes the right block: `GrabSystem.PeekGrabTarget`), standing on its target block, its own block lying off center on its tower.
- Done-when verification:
  - Bots reach pillar tops and the ring by path: 70 jump links imported and used; path lookups about 27 µs. No bot stuck over 10 s after the fixes (a 600 s stall check ran through 40 head-to-heads without firing). The Phase 7 soak's detours (4 to 31 a match) are about even with straight walking in this open arena (see git history, 2026-11-05).
  - Slide tackles make carriers drop their blocks: about half of tackle slides hit, and a hit drops the block.
  - The host sets each bot's difficulty on the room screen: checked with one client and with two (MPPM): arrows for the host, the level as text for the guest; new bots take the last level picked.
  - Head-to-head (two bots, 10 matches a pair, sides swapped every match): Easy v Normal 2–8, Normal v Hard 6–4, Hard v Expert 4–6, Easy v Expert 3–7. **Not met:** Expert beats Easy 7 of 10 (8 wanted) and Hard loses to Normal. Placings hold about half the time at every level, so head-to-heads are decided by which tower falls; making placing better at higher skill is the Touch-ups' first item. In six-bot rooms the levels do separate (wins Easy 5%, Normal 19%, Hard 23%, Expert 20%).
  - Personalities in the soak stats (57 rolled matches): Wreckers throw (9.4 a match against 0.5–0.7) and tackle (3.4 against 1.4–1.7) most; Klutzes make all the mistakes (about 6 a match); Builders win most (24%) and tackle more than Klutzes (guarding), though only a handful of top-clears happened, since a rival block rarely stays on a tower.
  - Jackson plays a mixed room and can tell the bots apart: waiting on his play-test (the dev label shows what each bot is; checked through `BotDevInfo` on the demo's six bots, not by eye on screen).
- Known issues: see Current state.
- Placeholders still in use: none new.
- Questions for Jackson (batched):
  1. Grab targeting: keep nearest-block, prefer blocks with nothing on them, or aim with the camera? A grab highlight either way?
  2. Match end: a timer (about 4 minutes) and sudden death, or something else?
  3. Wreckers win about 10% (an even share is 17%): fine, or should wrecking pay (Expansion ideas in the plan)?
  4. Klutz mistakes are mild (about one funny mistake a minute): raise them after you watch a Klutz?
  5. The pillar start: towers mostly end up on the ground beside the pillars. Is that the layout you wanted?
  6. Still open from earlier: tackles against human players at this rate (one every 10–20 s across six bots), keep the navmesh on in the arena, 2–5 minute matches with Wreckers in the room.
- Ready for review: yes

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

## 2026-11-06: Bot Enhancement, dev view, soak stats, tuning, review
- Did: dev labels (`BotDevInfo.Describe`: " · Wrecker, Hard" in the editor and development builds, empty in release) on the spectator label (`SpectatorDirector.ShownPlayer`) and the room screen's bot rows; `BotSoakLog` (view, dev only; a `[BotSoak]` line per match); event `BotTaskEnded`; `BotSystem.TestBotCount` for head-to-heads. Fixes: home block re-read from the best tower; `FindBlockOnTop` (a rival's block, or its own unstacked one, on any of its top blocks, up to 2.4 m); `IsOnTopOfAnother` and a jump-grab in `Fetch` (jump, and grab once `PeekGrabTarget` is the target); stepping round when the grab would take a different block; backing off a target block it stands too close to (or on); the demo rolls personality as well as skill each match (`BotSystem.RollBots`). Docs: plan and progress rewritten around the review, with the old history left to git; ASSETS gains A-025 and the match timer.
- Verified: the soaks in the review and the report above; 0 errors in every soak; console clean after.
- Decided: the jump-grab instead of changing the grab rule for everyone (that is Jackson's call); the head-to-head gap documented rather than tuned by guesswork, since placing is the cause and is the next phase's first item.
- Questions for Jackson: in the Bot Enhancement report.
- Next: Jackson's review; then Touch-ups item 1 (bots place carefully), which needs no feel call.
