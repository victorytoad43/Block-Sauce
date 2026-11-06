# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports live in git history.

## Current state (2026-11-05)

**Phases 0–5 are done and reviewed. Phase 6 (Front end and bots) and the plan review are done. Phase 7 (Arena) is done and reviewed: matches, the menu and rooms now run in Jackson's graybox arena with his start layout, the camera stays out of walls, and placeholder sounds play. Next: Bot Enhancement (now with navmesh pathfinding and slide tackles), then touch-ups, Phase 8 feel and juice, Phase 9 playtest build. The per-tick exception after collision knockdowns is fixed. The frame-rate review is pinned: the game itself is light (about 1 ms of work a frame in a build); Jackson's editor runs at 20–33 fps and the standalone build far faster, so performance is judged in builds. The game opens on Jackson's menu over a bot match; Start Game hosts a room with a code, Join Game joins by code or any open room. In a room, the room screen lists the players and lets the host add and remove bots, set the bot fill and start the match; the HUD shows the countdown, everyone's tower slots, the winner and the results, then the room screen comes back.**

What plays today, online with up to 6 players, in the Arena scene (the Lobby is now a test scene with the vats):
- The arena: a walled bowl with uneven ground, six spawn pillars with stairs, a raised inner ring and a dip in the middle. Each player starts on their pillar with their home block in front of them; their second block is on the inner ring across the middle; the third is in a pile of six in the middle.
- Guy on a force-driven capsule (0.40 m radius, his real width): walk/run (analog on gamepad), jump, sit, slide, stagger.
- Grab (the block keeps whichever face was up), hold and swing (body turns past 45°; the camera follows the swing on a gradient and never locks), place with flush settling, hold-to-aim throw with trajectory preview, jumpshot.
- Stack & Lock: 3 owned blocks each, settle dither, counting, win, full match loop back to the lobby.
- Knockdown: Guy's 11-limb ragdoll, Jump to recover faster, carrying bodies, get-up clips, dazed indicator, falling out of the world resets.
- Animation: idle; walk and run blending forward, back and strafe; jump; sit and slide; stagger flinches by hit side; GrabBlock hold with spine turn and hand IK; aim pose (`Throw` frame 4) and follow-through, grounded and airborne; foot IK on steps, ramps and blocks; win and lose placeholders after a round.
- Sauces (Lobby only; the arena has no cups for now): two vats (Jackson's cup) in the middle of the Lobby. Dunk a held block (or throw one in) to coat it; how deep sets the strength. Stack sauce makes a block sticky: high friction, and it glues to the blocks and ground it rests against until a throw-hard hit or a grab tears it free; Slip sauce makes a block slide (7.9 m after a throw against 1.1 m plain), so a Slip tower comes apart when pushed. The coat is a thick, lumpy, glossy shell that changes the block's silhouette, with bubbles (Stack) or glints (Slip); drips are off for now. Dev keys: F8 coats your held block or all your blocks with Stack sauce, Shift+F8 with Slip.

### Known issues
- The editor runs a match at 20–33 fps on Jackson's PC (main and render threads both waiting); a build runs far faster. A build can still stall for 100 or 200 ms now and then on presentation. A small hiccup on knockdowns (Phase 9 profiling).
- Sitting while holding a block keeps the block at the standing hold height; the arms reach up to it. If it reads badly, lower the hold pivot while sitting (simulation).
- A block dunked hard against the cup's side can stick at the bottom for a moment before the hold lifts it out.
- Sauce particles emit over the whole block, not only the coated part.
- A block let go off-center in a cup rests against the sloped wall, a little tilted (the cup is 1.43 m across at the floor).
- With the hips dropped for a low foot, Guy's long arms can touch the ground beside him.
- MPPM: starting the `TwoClients` scenario can take several minutes, or hang the main editor while the clone launches; stopping and starting the scenario again recovers it.
- Quantum's view updater sometimes asserts (`group.Buffer != default`) for about 3 s while creating a ragdoll body's view; seen twice in about an hour of all-bot matches. View only; the simulation is unaffected. Watch for a ragdoll that shows up late or not at all.
- Bots now and then wedge against each other or a wall for a few seconds; the detour or the task's timeout frees them.
- A ragdoll that spawns half inside a block can launch it; the fall reset recovers it. If it shows up in play, spawn the ragdoll in a tucked pose.
- A body still airborne when its knockdown ends stands up mid-air and falls.
- A player who joins mid-match gets no blocks until the next round.
- Swing input is sent as per-tick deltas, so a repeated lost input repeats one tick of swing. Revisit only if swings feel jumpy online.
- Quantum's stats overlay (frame, ping) shows over the menu. Photon logs an error line when a typed room code doesn't exist (the menu shows its own message).
- Bots that filled empty slots at a match start stay in the room afterwards; the host removes them with Remove Bot.
- The room's host is whoever holds the lowest slot among the connected players (read from the verified frame); any player's room commands are still accepted by the simulation, the screen only hides the controls from the others.
- The settings controls (sliders, toggles, the resolution list) and the code field are drafts in the button's colors until A-002's screens arrive; buttons look the same when disabled (while connecting).
- `Arena_Graybox` is retired (still in the build list, unused); it has uncommitted changes from outside the agent's work, so it is left alone until Jackson says what to do with it.
- Bots don't use the stairs: they jump onto pillars and the ring from wherever they arrive, which works but looks hoppy.
- Blocks sometimes rest tilted against a pillar's rim or the ring's edge and so don't count until moved.
- The placeholder sounds are unheard picks by file name; some may be wrong for their moment or too loud.

### Waiting on Jackson
- A-002 button focus states and the remaining screen mock-ups; A-006 HUD mock-ups (drafts stand in until they arrive).
- Stack glue strength, and whether it should glue to the ground (open question in the plan; play-test).
- The older open questions (settle numbers; pickup swings through your own tower).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip` (the cup at 1.3× with placeholder sauce), `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain in the Lobby test scene, `PH_MainMenu` (Jackson's main menu art; the Join, Settings, Pause and room screens are drafts, A-002), `PH_MatchHud` (draft HUD and results, A-006), the IMGUI spectator bar (A-006), `PH_RagdollTest` (stress-test scene only), `PH_SoundBank` (CC0 placeholder sounds, A-014).

## Next: Bot Enhancement

Design in `docs/PLAN.md` › Bot Enhancement: navmesh pathfinding, slide tackles, four skill levels set per bot, three hidden personalities (Wrecker, Builder, Klutz).

Build order:
1. **Navmesh.** (Done 2026-11-05.) A `NavMeshSurface` on the arena (agent: radius 0.4, height 1.8, step 0.5, slope 35°) and a `QuantumMapNavMeshUnity` beside the map data, so Quantum's map bake imports it (`QuantumMapDataBakeFlags.BakeNavMesh | ImportUnityNavMesh | BakeUnityNavMesh`). Links from the builder: jump links every couple of meters round the ring's edge and each pillar's rim, and on each stair (walk links once a stair's top step is within 0.5 m). Check the link import in Quantum 3.1 first (a small test map with one ledge) before building on it. **Block Sauce › Bake Arena NavMesh** for a rebake on its own; Build Arena calls it.
2. **Path following.** (Done 2026-11-05, as `BotNavigation` with a direct path lookup instead of a `NavMeshPathfinder`; see the log.) A `NavMeshPathfinder` on each bot (no steering agent: bots still drive themselves through input). `BotActions.Steer` asks for a path when the target moves more than a meter or every half second, aims at the next waypoint, and presses Jump at a jump link's start; arriving and the task logic stay as they are. The probes keep dodging players and blocks; the detour-when-stuck stays as the last resort. No navmesh on the map: today's steering. Then re-soak the arena (8 or more matches) against the Phase 7 numbers: detours, match length, stuck time.
3. **Slides.** (Done 2026-11-05; see the log.) `BotIntent.Slide` (the crouch press while running over 4 m/s). Measure first that a bot's slide into a carrier staggers them and knocks the block loose (7.5 m/s against the 4.5 m/s body-check and 2.5 m/s stagger thresholds; expect yes). A `SlideTackle` task: path to a rival, lead them, slide from 3 to 4 m out, give up after a timeout; travel slides on long straight paths. Normal bots tackle a rival carrying one of their own blocks, or one at their tower; the personalities change this in step 5.
4. **Skill levels.** (Done 2026-11-05; see the log.) `BotSkill` assets (`Resources/Configs/Bots/BotSkill_Easy`, `_Normal`, `_Hard`, `_Expert`) listed on `BotConfig`; `Bot.Skill` (byte, the level). The knobs that differ by level move off `BotConfig` onto `BotSkill`: hesitation (`ThinkSecondsMin`/`Max`), aim tolerance and a random aim error per throw (yaw and pitch, degrees), a random placement error (the point it lets go over, meters), how long it holds an aim (`MinAimSeconds`), whether it runs while carrying, grab and place attempts, `RaidAtCount`. Normal keeps today's numbers exactly, so the arena soak (Phase 7 report) is the Normal baseline. First guesses: Easy hesitates 0.8–2 s, ±6° aim, 0.25 m off on placing, walks everywhere; Expert hesitates 0.1–0.3 s, ±0.5°, 0.05 m, runs with a block.
5. **Room screen.** (Done 2026-11-05; see the log.) A difficulty control on each bot's row (◀ NORMAL ▶, host only; guests see the level as text). `CommandSetBotSkill { Slot, Level }` (lobby only), which also sets `Match.NewBotSkill`, the level new bots and fill bots take. The demo match rolls each bot's level at random. Built by **Block Sauce › Build Main Menu** as before.
6. **Personalities.** (Done 2026-11-05; see the log.) `BotPersonality` assets (`Wrecker`, `Builder`, `Klutz`) listed on `BotConfig`; `Bot.Personality` rolled with `frame.RNG` in `MakeBot`. `BotChoices` turns into a scored choice: each option (build with an own block, raid, guard, dunk, tackle, clear the top) gets a score from the situation times the personality's weight, highest wins, ties broken by the RNG. New tasks in `BotActions`: tackles are the slide tackle from step 3 (Wreckers use it on any carrier and at towers, Builders on rivals near their tower), and `ClearTop` (fetch a foreign block off its own tower and throw or drop it away; only when the top is in reach). New decision in the brain for interrupting a task to guard (a rival near the tower), rebuilt by **Block Sauce › Build Bot Brain**.
7. **Klutz mistakes.** (Done 2026-11-05; see the log.) Chances per second or per task on the personality: trip while running (a knockdown through the same path as `CommandDevKnockdown`), slide into walls or off ledges, drop the carried block (a Grab press mid-carry), let go off center (a large placement error), throw wild (a large aim error).
8. **Dev view and soak stats.** Dev builds only: the spectator label and the room screen's bot rows add the personality; a `BotSoakLog` (view, dev only) counts per bot from simulation events: wins, placements, misses, throws, tackles, guards, knockdowns caused and taken, trips, drops; it writes a line per match to the log. Events added where missing (`BotTaskEnded` with the task and result).
9. **Tuning and done-when.** Head-to-head soaks (2 bots, one level each; 10 matches per pair), then 6-bot mixed matches for the personalities; tune until the done-when holds; then Jackson's play-test.

Risks:
- Quantum's navmesh is flat-ish by design (heights per triangle, paths in 2D); the arena's bumpy ground and stacked levels (pillars over ground) should import fine as separate areas joined by links, but the link import is the part to prove first (step 1).
- The ground's 32,768 triangles make a detailed navmesh; Unity's bake simplifies it, but path queries on six bots should stay well under a millisecond. Measure in the soak.
- Blocks and towers aren't in the navmesh (they move); bots route through a tower and dodge it with the probes, as today. Carving blocks out is possible later if bots keep bumping towers.
- Slide tackles could make bots feel mean to new players; how often bots tackle lives on the skill and personality assets, and Jackson's play-test decides.
- Expert stops being fun to play against; it is capped by `BotSkill` numbers, and Jackson's play-test decides.
- Tackling and guarding could feel like griefing, especially with a Wrecker on a player who is trying to build; tackle cooldowns live on the personality.
- Taking a foreign block off a tall tower needs a jump-grab the bots can't do yet; until then a Builder leaves an out-of-reach top alone.
- Hidden personalities make tuning hard to see; the dev label and soak log are there for that.
- More decisions per tick: scoring loops over players and blocks; keep it to a few times a second per bot (the hesitation already spaces decisions).

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

## Phase 7 report: Arena
- Built: Jackson's graybox as the game's arena (**Block Sauce › Build Arena** makes the `Arena` scene from the Lobby: colliders, stair colliders fitted per stair, spawns on the six pillars, block markers, its own map and mode, first in the build). Match blocks now come from map markers (`BlockSpawnPoint`): home block on the pillar, one on the inner ring across the middle, one in the center pile; a bot's tower starts on its home block. Bots measure "on the ground" from the ground under a block and jump onto ledges. Camera collision (sweep against `CameraBlock` colliders, eased back out). Placeholder audio (`GameAudio`, `PH_SoundBank`, **Block Sauce › Build Sound Bank**).
- Done-when verification:
  - A full 6-player match in the arena: a 15-minute all-bot soak of the demo (6 bots) played 8 matches to a winner, 52 to 140 s each (about 97 s on average; Lobby matches took about 2 minutes), with 0 errors (listening on every thread) and no block lost off the map. An online room (host plus 5 fill bots) started and played a match in the arena with sound and no errors. Not checked with 6 human clients (MPPM); the arena changes no networking.
  - The camera never looks through a wall: at the spawns the camera's usual place stays clear of the wall; looking up from a pillar (the camera's usual place is under the ground) pulls it in from 4.5 to 3.8 m, above the ground, and it eases back to 4.5 m within 2 s. The sweep covers the ground, wall, ring, pillars and stairs, the only geometry there is.
- How the layout played (8 matches): towers that won stood on the ground beside a pillar (3), in the middle (2), on a pillar (2) and on the ring (1). Every win took the full trip; no player won from the pillar alone. Knockdowns were rare (0 to 2 a match; bots throw little), and bots made 4 to 31 short detours a match round each other and the ledges. Uneven ground didn't stop towers settling; blocks that tipped against the pillar rims and the ring's edge did.
- Known issues: see Current state (bots hop rather than use stairs; blocks tilted against rims; unheard sounds; `Arena_Graybox` left alone).
- Placeholders still in use: see Current state; new: `PH_SoundBank` (A-014).
- Ready for review: yes

## 2026-11-05: Bot Enhancement, step 7: Klutz mistakes
- Did: mistake chances on `BotPersonality` (Mistakes): per second of running, `TripChance` (the bot goes down as a ragdoll at its run's speed, dropping what it holds; applied after the bot loop, since a knockdown adds components) and `WildSlideChance` (a slide with no look ahead, so into walls or off ledges); per second of carrying, `DropChance` (a Grab press); per placing, `OffCenterChance` (lets go up to `OffCenterError`, 0.45 m, off center); per throw, `WildThrowChance` (up to `WildThrowError`, 12°, off in yaw and pitch, on top of the skill's error). Event `BotMistake { Bot, Type }` for the soak stats (step 8). Only the Klutz has them: trip 0.03 a second, wild slide 0.04, drop 0.03, off-center a quarter of placings, wild throw 30% of throws. The skill still applies, so a Hard Klutz plays well with funny mistakes.
- Verified: 20-minute soak of the demo, Normal skill, personalities rotated two each per match, 0 errors, 5 matches (94 to 272 s). Klutzes (10 bot-matches): 3 trips, 5 wild slides, 7 dropped blocks, 56 off-center placings (counted per placing attempt, including retries), 5 wild throws; Wreckers and Builders none. Klutz still won 3 of the 5 (Builder 2, Wrecker 0); its placements landed 39 of 81 against Builder's 30 of 52.
- Found: the mistakes are mild at these numbers (a trip or wild slide every few minutes per Klutz); they're easy to raise on the asset once Jackson has watched one.
- Next: step 8, dev view and soak stats.

## 2026-11-05: New logo
- Assets: Jackson's new logo (`_Incoming/mark.png`, the sauce-splatter version, 729×387) copied over `UI_Logo` (its GUID kept); the menu's logo box widened to 693×368 so it keeps its height; **Build Main Menu** rerun. Checked on the menu over the demo.

## 2026-11-05: Bot Enhancement, step 6: personalities
- Did: `BotPersonality` (simulation asset) and `BotPersonalityType` (Wrecker, Builder, Klutz); `BotConfig.Personalities` lists one per type; `Bot.Personality` is rolled with the simulation's RNG in `MakeBot`. **Block Sauce › Build Bot Personalities** creates any missing one with first-guess numbers; existing assets are left for tuning in the Inspector. `BotChoices.ChooseBlockToFetch` now scores its options, each weight times how much the situation calls for it plus a random 0 to 0.4, highest wins: Build (own loose block, 1), Raid (a rival's block, when a rival tower is worth raiding: that rival's counted blocks over 2), ClearTop (a rival's block resting on its own tower's top, low enough to grab, 1.5). A rival's block in hand is thrown at a nearby carrier now and then (Wreckers), else at the tower worth raiding, else set down 3 m away from its own tower. Throws at characters aim at their middle. Who a bot tackles comes from its personality (`TackleCarriers`: its own blocks' carriers or any carrier; `GuardRadius` round its tower, carrying or, for a guard, anyone; `TackleBuilders`: rivals at their own towers), with a `TackleChance` (a tackle passed up waits the cooldown) and a cooldown scale; dunking scales too. Guarding needs no new brain state: the Tackling state already interrupts fetching and pausing. Test switch `BotConfig.TestPersonalityBySlot`.
- Personalities (first guesses, tuned once): Wrecker raids from a rival's first counted block (sometimes before its own tower is done, mostly once a rival has two), throws at carriers 30% of the time, tackles anyone carrying a block and rivals at their towers on 60% of chances, dunks half as often. Builder raids only a rival one block from finishing and only with nothing of its own to fetch, clears its top first, dunks twice as often, guards anyone within 5 m of its tower on 80% of chances. Klutz chooses like the bot before personalities (its mistakes are step 7).
- Verified: 30-minute soaks of the demo, all bots Normal skill, personalities rotated two per type across the slots each match, 0 errors. First guesses made matches far too long (about 280 s on average, one 638 s): Builders raided more than the old bot (a 3-block tower makes "one from finishing" the default raid count) and Wreckers raided nearly every time, so towers rarely finished. Retuned (Builder raid weight 0.5, Wrecker 1.4, Wrecker throws at carriers 30%): 11 matches, about 141 s on average (56 to 317). Per type over 22 bot-matches: wins Wrecker 0, Builder 6, Klutz 5; blocks at the end 0.68, 1.36, 1.14; rival blocks fetched 301, 77, 69; throws 75 (47 at players), 16, 11; tackles started 136, 108, 99, of them at rivals with empty hands 42, 49, 0; placements landed 33 of 39, 78 of 113, 77 of 116.
- Found: the personalities show in the numbers (Wreckers raid, throw and tackle most; Builders guard and build best). Wreckers never won: they spend their time breaking towers. Two Wreckers a match (as in these soaks) still make matches about half again as long as before. No bot cleared a rival block off its top in either soak: thrown blocks rarely stay on a tower, so ClearTop is untested in play.
- Questions for Jackson: is a Wrecker that never wins fine (it makes the room harder for everyone), or should it build more? Are 2–5 minute matches with Wreckers in the room too long?
- Next: step 7, Klutz mistakes.

## 2026-11-05: Bot Enhancement, step 5: room screen difficulty
- Did: each bot's row on the room screen shows its level (`< NORMAL >`) where the BOT tag was; the host gets the arrows in the lobby (they stop at Easy and Expert), guests see the level as text. `CommandSetBotSkill { Slot, Level }` (lobby only, like the other room commands) sets that bot's level and `Match.NewBotSkill`, the level the next bots take, including those that fill the empty slots at the start (Normal when the map starts). The menu's demo rolls every bot's level at random at each match start (`BotSystem.RollSkills`). Built by **Block Sauce › Build Main Menu**, which now reopens the scene that was open before it (it used to leave the Lobby open, so the next play session started there). `MppmReportCommand` reports each bot row's level.
- Verified: as host (one client): two bots added at Normal; three presses up on one took it to Expert (the third did nothing, the arrow went dim), two down on the other to Easy; a third bot came in at Easy, and the match's fill bots too; 0 errors. Two clients (MPPM): the guest's room screen showed `BOT 6 HARD` with no arrows after the host raised it, and the Waiting text. Demo: a match started with levels Hard, Normal, Expert, Easy, Expert, Expert.
- Found: starting the two-client scenario with the Lobby left changed (the builder had left it open, and play sessions and Quantum's map bake mark the open scene changed) stopped on Unity's "save changes?" prompt; Jackson cleared it. Before each play session the open scenes are now reloaded from disk if changed, and the builder no longer leaves the Lobby open (see CLAUDE.md).
- Next: step 6, personalities.

## 2026-11-05: Bot Enhancement, step 4: skill levels
- Did: `BotSkill` (simulation asset) with the knobs that differ by level, moved off `BotConfig`: hesitation (`ThinkSecondsMin`/`Max`), `RaidAtCount`, `RunWhileCarrying`, `GrabAttempts`, `PlaceAttempts`, `PlaceError` (meters; the point it lets go over, picked in a disc per placing), `AimTolerance`, `MinAimSeconds`, `AimError` (degrees off in yaw and pitch, picked per throw), and tackling (`TackleSlideDistance`, `TackleAlignDegrees`, `TackleLead`). `BotConfig.Skills` lists one per `BotSkillLevel` (Easy, Normal, Hard, Expert); `Bot.Skill` picks it, Normal for every new bot until step 5's room screen sets it. A task's errors are rolled with the simulation's RNG when it starts (`Bot.AimError`, `Bot.PlaceError`). **Block Sauce › Build Bot Skills** creates any missing level with first-guess numbers and lists them on `BotConfig`; it leaves existing assets alone, so tune them in the Inspector. Normal is the Phase 6 bot's numbers exactly. Test switch `BotConfig.TestSkillBySlot`.
- First guesses: Easy hesitates 0.8–2 s, raids at 3 blocks, places up to 0.25 m off, aims ±6° with 4° tolerance, 2 place and 4 grab tries, slides at 5 m out, lined up within 35°, at where the rival is (no lead). Hard 0.2–0.6 s, runs with a block, 1.5° tolerance, 4 and 8 tries, 15° line-up. Expert 0.1–0.3 s, runs with a block, 1° tolerance, 0.3 s shortest aim, 5 and 10 tries, slides at 3.2 m, 10° line-up. Easy still runs with empty hands (the plan said it walks everywhere, but a walking bot can't slide and never tackles; how much Easy runs can be a knob later).
- Verified: 30-minute soak of the demo, 6 bots, the four levels rotated across the slots each match, 17 matches, 0 errors. Per level (about 25 bot-matches each): wins Easy 4, Normal 4, Hard 3, Expert 6; blocks at the end 1.00, 0.77, 0.92, 1.16 a bot; fetch 3.6, 3.3, 3.1, 2.5 s; placements landed 43%, 56%, 67%, 66%; throws landed 0 of 0 (Easy never raids), 10 of 11, 14 of 17, 20 of 24; tackle slides hit 12 of 16, 6 of 8, 6 of 11, 11 of 13.
- Found: the levels show in how bots play (Expert fetches fastest, places and throws best) more than in who wins: in six-bot matches who gets raided and who gets tackled decides more than skill, and Easy loses little by never raiding. Easy's slides at where the rival is hit as often as leading ones (12 of 16), so leading isn't clearly better. Head-to-head soaks (two bots, step 9) are the real test of the levels; tuning waits for them.
- Next: step 5, the room screen difficulty control.

## 2026-11-05: Bot Enhancement, step 3: slides
- Did: bots slide. `BotIntent.Crouch` (pressed while running fast it starts the player's slide; released, the slide ends standing). A `SlideTackle` task (`BotActions`): run at the rival, aiming where it will be (its velocity times the time to get there, up to 1 s), and once within 3.5 m, running at 5 m/s or more on the ground, lined up within 20° and with no wall between, slide; Done when the rival is staggered or knocked down (by the slide, or by the bot running into it first), Failed when the slide misses or the rival stops being worth it. Who: `BotChoices.ChooseTackleTarget`, for a bot with empty hands, the nearest rival within 12 m and about level (1 m) who is on its feet and carrying one of the bot's blocks, or carrying anything within 3 m of the bot's tower; 5 s between tackles, 6 s to land one. The brain (**Block Sauce › Build Bot Brain**) gains a Tackling state, entered from Fetching and Pausing when there's a rival to tackle; a hit goes straight back to fetching (the loose block may be the bot's own), a miss pauses. Travel slides: while running empty-handed toward something over 10 m away, with the path's next corner and the probes clear for 6 m and the run lined up, a 15% chance a second (4 s apart). Event `BotTackleEnded` (slid, hit, dropped) for the soak stats in step 8. Test switch `BotChoices.TestDisableTackles`.
- Measured first, as planned: a bot's slide into a carrier staggers it and knocks the block loose, as the numbers said (7.5 m/s, over the 4.5 m/s body check; even a running body check staggers).
- Verified: A/B soak, 25 minutes alternating tackles on and off each match, 6 bots, 0 errors. With tackles: 23 tackle slides, 10 hit; 4 more rivals body-checked on the run before the slide; 12 of those rivals lost their block. Matches 92 s against 105 s without; staggers 5.0 against 4.0 a minute; slides 2.2 against 0.7 a minute (the 0.7 is travel slides, which run in both); knockdowns 0.3 against 0.6 a minute; fetches 3.2 against 3.6 s; no walking bot stood still over 4.6 s. About 16 tackles a 25 minutes ended before a slide, mostly when the rival threw or placed the block first.
- Found: slides hit less than half the time (10 of 23): the slide goes where the bot is running, and a rival who turns or stops in the 0.5 s it takes gets missed. Fine for Normal; skill levels (step 4) set how well it's timed and aimed.
- Decided: tackles live in the brain as their own state rather than as a scored choice, until personalities (step 6) score them; a running body check that staggers the rival counts as a landed tackle.
- Questions for Jackson: do tackles feel right at this rate (about one every 30 s across six bots) when you play against them, or too mean for new players? Tackles target human players the same as bots.
- Next: step 4, skill levels.

## 2026-11-05: Bot Enhancement, step 1 and 2: navmesh and path following
- Did: the arena navmesh. **Block Sauce › Build Arena** (and **Bake Arena NavMesh** on its own) bakes Unity's navmesh from the arena's CameraBlock colliders for Guy's size (radius 0.4, height 1.8, slope 35°, climb 0.45 so ledges split the areas), skips the wall's top, adds 70 two-way jump links (the inner ring's outer and inner edges every 20°; each pillar's rim every 45° except over the stair and on the tower side, ending just inside the rim; one from each stair onto its pillar), and Quantum imports it into `ArenaMap` (`ArenaMap_ArenaNavMesh`). `BotNavigation` (simulation): a bot walking somewhere looks up a path a few times a second (or when its target moves a meter), heads for the next corner, and at a link's start heads for its far end and jumps; the existing probes still dodge players and blocks. No navmesh on the map (the Lobby): straight as before. `Bot` gains the path fields.
- Verified: the import (2 areas, every link mapped onto the navmesh), drawn top-down: the pillar tops and the raised half of the ring are their own areas, joined by the links. Path lookups cost about 27 µs (1,000 across the arena in 27 ms), so six bots at 4 a second cost well under a millisecond a second. A/B soaks, alternating navmesh and straight walking each match, 25 minutes each, 0 errors: first run, navmesh worse (matches 128 against 87 s, detours 16 against 9 a minute) because the pillars' inward links ended on the home block, so bots walked into their towers; after moving the pillar links clear of the tower side, about even (matches 90 against 108 s; detours 12 against 8.5 a minute; fetches 3.6 against 3.3 s; places 5.1 against 4.7 s). Match lengths swing too much between runs to read much from them. No walking bot stood still over 6.6 s.
- Found: in this open arena straight walking plus a hop is close to the best path already, so the navmesh doesn't win much here; it can't see blocks and towers, which move, so its paths lead into them and the probes then detour (most of the extra detours). It pays off when a map has walls or drops to go round.
- Decided: ledges are separate areas joined by links (a 1 m climb made the whole arena one flat area, paths straight through everything); the path is looked up directly (`Navigation.FindPath`) and kept on the bot rather than through a `NavMeshPathfinder`, so bots keep driving through input and lookups stay few; a test-only switch (`BotNavigation.TestDisable`) compares with straight walking.
- Questions for Jackson: keep the navmesh on in the arena (it costs nothing and is ready for busier maps), and is reducing the extra detours (teaching the probes to trust the path, or keeping paths clear of towers) worth doing now or after slides?
- Next: step 3, slides.

## 2026-11-05: Phase 7 reviewed; navigation and slides added to Bot Enhancement
- Did: Jackson's review note into the plan: bots pathfind on a navmesh baked into the map (rebake from a menu), and use slides, mainly slide tackles to make a carrier drop their block. Both go into Bot Enhancement, navigation first; build order and risks in Next.
- Verified: nothing to run. AI Navigation 2.0.12 is already in the project; Quantum 3.1 imports a Unity navmesh with its links at map bake.
- Decided: Unity bakes, Quantum imports (deterministic pathfinding in the simulation); bots keep driving through input and only take waypoints from the navmesh; ledges are jump links rather than a reshaped map; a slide at 7.5 m/s is over the body-check and stagger thresholds, so a slide tackle should already knock a block loose (measured in step 3).
- Questions for Jackson: none.
- Next: step 1, the arena navmesh.

## 2026-11-05: Phase 7, arena
- Did: Jackson's graybox (`arena_map.fbx`, `arena_map_pieces.fbx` to `EN_Arena`, `EN_ArenaPieces`) and his start layout; the Arena scene and builder; block markers in the simulation (`BlockSpawnPoint`, `PlayerSystem.GetSpawnIndex`); bots on uneven ground (ground height under a block, ledge jumps, the knee probe ignores static ledges); camera collision; the CC0 sound pack (`FreeSFX`) moved to `Assets/Art/Audio/_Pack_FreeSFX` (git-ignored, 164 MB) with 18 picks copied in as `SFX_*` and `MUS_*` (32 MB, LFS); `GameAudio` and `PH_SoundBank`.
- Verified: see the Phase 7 report.
- Decided: the arena scene starts as a copy of the Lobby, so the menu, HUD, bots and dev keys come along unchanged; the Lobby stays as the sauce test scene. Collision uses the render meshes (low enough counts) and Jackson's stair collider; spawns are measured from the meshes rather than empties. Camera collision uses Unity colliders on their own layer, not the Quantum statics (view only). Rooms on the arena use the app version postfix `-Arena`, so they don't mix with Lobby builds. The bot changes for uneven ground change Normal bots slightly ahead of Bot Enhancement (jumping onto ledges, ground height); the arena soak is the new Normal baseline.
- Assets: A-008 graybox in (notes for a re-export in `docs/ASSETS.md`); A-014 placeholders in.
- Questions for Jackson: see the review message.
- Next: Jackson's review; then Bot Enhancement.

## 2026-11-04: Bot Enhancement planned
- Did: Jackson's answers into the plan: four skill levels (Easy, Normal, Hard, Expert; today's bot is Normal), set per bot by the host; three personalities (Wrecker, Builder, Klutz), random and hidden. The phase moves ahead of Phase 7, which waits on the graybox. Build order and risks in Next; A-002 gains the difficulty control on a bot's row.
- Verified: nothing to run.
- Decided: personalities weight a scored choice in `BotChoices` rather than the Bot SDK's utility AI (its documents can't be built from a script, like the state machine's, and the choices are few); utility AI stays planned for team modes. New bots and fill bots take the difficulty the host set last. The demo match mixes levels. Dev builds show personalities; players never see them.
- Assets: A-002 gains the bot row's difficulty control.
- Questions for Jackson: none.
- Next: step 1, skill levels.

## 2026-11-04: Plan review pinned, Phase 7 prepared
- Did: Jackson's retest into the plan: his editor runs 20–33 fps (Unity Stats: main and render thread both 30–50 ms, so waiting), Quantum's simulation 2.75 ms a frame online, and the standalone build far faster; the Meta Virtual Monitor made no clear difference. The review is pinned: performance is judged in builds, and the knockdown hiccup and triangle counts go to Phase 9's profiling. The PowerShell form of the `-logframes` command into CLAUDE.md (a quoted path needs `&`). Phase 7's build order and risks written up (Next).
- Found: blocks are 3,168 triangles, Guy 15,532; 4 shadow cascades at 50 m.
- Questions for Jackson: none.
- Next: Phase 7 when the graybox arrives.

## 2026-11-04: Plan review, frame rate in a build
- Did: a Windows development build (`Builds/BlockSauce_Dev`, 188 MB). New dev-only `FrameLog` (`-logframes`, `-fps N`; see CLAUDE.md › Testing). Frame timing stats on in the player settings (so `FrameTimingManager` reports in builds). Vsync on in both quality levels: it was off, so the build ran at about 900 fps.
- Verified: the build on the menu's 6-bot demo: about 1 ms of main-thread work and 0.3 ms of simulation a frame; 165 fps with vsync on this 165 Hz screen; capped at 60, a steady 60 (worst frame 17–20 ms). Uncapped or at 165, single frames stall for about 103 or 205 ms in bursts; the recorder shows the stall is all present wait (103 ms frame: 102 ms waiting for the last presentation). Not GC (heap steady at about 22 MB; only the two startup hitches had a collection). No exceptions in the build.
- Decided: vsync on by default; a frame-rate setting can come with the settings polish if needed.
- Questions for Jackson: retest with the Meta Virtual Monitor off (see Next).
- Next: Jackson's retest; Phase 7 when the graybox arrives.

## 2026-11-04: Plan review, answers and fonts
- Did: Jackson's answers into the plan (Phases 7 to 9, Bot Enhancement and Steam later, fonts, 6 players). DynaPuff (the logo's second line, OFL) in as `UI_DynaPuff_Bold` and `UI_DynaPuff_Regular`; the menu builder uses it for buttons, names, codes, prompts and hints, and Jaro for the countdown, RESULTS and the winner. Rebuilt `PH_MainMenu`, `PH_MatchHud` and the Lobby's front end. New `JointValidator` (simulation test helper): lists joints whose owner or connected entity is gone or has no body.
- Verified: every character the UI uses exists in both DynaPuff weights. A 7-minute demo soak listening on every thread (Quantum logs physics exceptions from worker threads, which the first soak missed): 10 knockdowns, 0 errors, no bad joints. Online room with 6 bots: about 8 ms of work a frame including 7 simulation steps; forced knockdowns of everyone showed no hitch.
- Found: the 30 ms frames in the agent's sessions are display pacing (the same with every camera and canvas off; the GPU is about 2% busy). Two earlier sessions in the editor log threw solver exceptions throughout; Jackson's session and every one since threw none.
- Questions for Jackson: his frame numbers during a knockdown (see Next).
- Next: a standalone development build to compare frame rates.

## 2026-11-04: Plan review, optimization pass
- Did: profiled the demo match (6 bots) in play mode. The one large cost was an exception every tick ("pointer is not properly aligned" in the physics callbacks' list) after a character was knocked down by a collision: the knockdown created the ragdoll and added components inside `OnCollisionEnter3D`, which moved the character's data under the physics engine. About 53 ms a frame in stack traces, and it left the demo stuck on its results. Fixed: the collision callback records the tick's strongest hit on `Knockbackable` (`PendingImpulse`, `PendingSource`) and `KnockbackSystem.Update` applies it after the physics step. `SauceSystem.GlueTouchingBlocks` now copies the block's transform and velocity before gluing, which adds a joints component.
- Verified: 8-minute soak of the demo: 7 matches to results, 7 knockdowns and 28 staggers from hits, 0 errors. Profile of 600 frames during play: about 5 ms main thread (Quantum's update 1.8 ms including the simulation; no game system over 0.1 ms), rendering about 0.9 ms, GPU 1.6 ms. The frame time read 31–34 ms only because the unfocused editor throttles presentation.
- Decided: a collision hit is applied one step later (same tick, after physics) and only the strongest hit per tick counts.
- Questions for Jackson: in Next.
- Next: Jackson's answers, then write the chosen phases into the plan.

## Phase 6 report: Front end and bots
- Built: bots (a `Bot` takes a free player slot and writes its input; actions in our code, decisions in a Bot SDK state machine; the host adds and removes them, and they fill empty slots at a match start, on by default); the F6 all-bot test match; the spectator director camera with viewer controls; 6 players (the two blues cut); Jackson's main menu over an offline bot match, with Start Game (hosts an online room under a 5-letter code), Join Game (by code or any open room), Settings and Pause; the room screen (code, slots in color with bots, you and the host marked, the host's controls); the uGUI match HUD and results. Replaces the stock Quantum start UI and the IMGUI HUD in the Lobby.
- Done-when verification ("the game opens on the menu over a bot match; a player can start a room, add bots, play a full match against them and return to the menu; a second player joins that room by code; 6 bots finish a match on their own"), two clients through MPPM `TwoClients`, everything pressed through the screens' own buttons:
  - Menu: both clients opened on the menu over the demo match, 6 bots playing, the spectator director on.
  - Host and join by code: A pressed Start Game and got room CDRVV; B joined it by the code through the same path as the Join Game screen. A saw PLAYER 1 (YOU · HOST) and PLAYER 2 with Start Match, Add Bot, Remove Bot and Bots Fill Slots; B saw PLAYER 1 (HOST) and PLAYER 2 (YOU) with "Waiting for the host" and no controls.
  - Add bots, full match: A added two bots (BOT 5 and BOT 6 on both clients) and started; bots filled the last two slots; both got the countdown and the HUD. A handed its character to a bot (F6's command) and B stood still; BOT 5 won after about 2 minutes, BOT 5 WINS! on both, then ranked results (BOT 5 3 / 3, the rest 0).
  - Back: the room screen came back on both after the results, the bots still in it; A pressed Leave Room and both clients went back to the menu and the demo.
  - 6 bots alone: the demo match behind the menu (6 bots, no one connected) played to its results and started the next match by itself. The F6 all-bot match in a room was soak-tested in step 4.
  - Consoles on both clients: no game errors.
- Fixed during verification: every client thought it was the host and showed the host's controls, because on predicted frames the other clients' slots look unconnected; the room screen and the HUD now read who is connected from the verified frame. Room codes no longer use V, which the Jaro font draws like U (CDRVV read as CDRUU).
- Not checked: real mouse and controller input by hand (the screens were driven through their buttons in code); the guest's screen was checked through its log and a screenshot, not by hand.
- Known issues: see Current state (the stats overlay over the menu; Arena_Graybox still on the old UI; bots that filled slots stay after the match; bots sometimes wedge for a few seconds).
- Placeholders still in use: `PH_MainMenu` (drafts of the Join, Settings, Pause and room screens, A-002), `PH_MatchHud` (A-006), the IMGUI spectator bar (A-006), `PH_PlayerCapsule`, `PH_Block`, `PH_LockDither` (A-007/A-013), the sauce placeholders (A-009), `PH_` terrain (A-008).
- Ready for review: yes

## 2026-11-04: Phase 6, step 8: done-when check
- Did: the done-when with two clients (report above). MPPM clones now join the main editor's room through `JoinRoom` (by its code, never creating one), the Join Game screen's path. New editor-only `MppmReportCommand`: sent from the main editor, each clone writes what its screen shows to its own Editor.log and can save a screenshot.
- Fixed: the host check and the bot names read who is connected from the verified frame (every client thought it was the host); room codes skip V.
- Questions for Jackson: in the review (see Next).
- Next: Jackson's review, then the plan review.

## 2026-11-04: Phase 6, step 7: room screen and HUD
- Did: the room screen (`RoomScreen`, in the menu prefab): the room code to share; one row per slot in its color (PLAYER n, BOT n or OPEN; YOU and HOST marked); for the host Start Match, Add Bot, Remove Bot and Bots Fill Slots (the existing room commands); for everyone Play in Lobby, Settings and Leave Room. It is the in-room menu between matches: it opens on entering a room and after each match's results, closes as a match starts, and Escape or Select reopens it in the lobby (during a match they open Pause). The match HUD (`MatchHud`, now uGUI in `PH_MatchHud`, replacing the IMGUI one): the lobby prompt (Enter / Start to start for the host, waiting for the host for the others), the countdown big in the middle, GO!, a headline bar, every player's three tower slots top left in slot order (filling while settling, solid when counted), the winner in their color, and the results (winner, everyone ranked by counted blocks, seconds until the room screen). Dev keys moved from the HUD to `DevKeys`. Both built by **Block Sauce › Build Main Menu**, which now also swaps the Lobby's IMGUI HUD for `PH_MatchHud` and `DevKeys`.
- Verified (one client, driven through the screens' own buttons): host → room screen up with Start Match selected and the HUD hidden; two Add Bot → BOT 5 and BOT 6 rows, Remove Bot → BOT 6 gone; the fill toggle off and on, the simulation following each; Play in Lobby → the prompt and room badge; reopen → Start Match → countdown 3 with GET READY, the room screen closed, bots filled to 6; GO!; towers filling; Pause during the match shows Pause, not the room screen; a bot won after about 2 min: BOT 2 WINS! in orange, results ranked with the countdown to the room; back in the lobby the room screen reopened; leave → menu, HUD hidden. Console clean.
- Fixed while testing: the prompt's bar stayed up with its text gone; the first selection missed Start Match (the host isn't known on the room's first frame); tower rows came in entity order.
- Decided: the room screen is the in-room menu between matches, not a separate screen to open; the host is the connected player in the lowest slot (every client works it out the same way, and it passes on if the host leaves); bots that filled slots stay after the match.
- Not yet checked: a second client seeing the room screen as a guest (step 8, with MPPM); real mouse and controller input by hand.
- Assets: A-002 and A-006 updated with the drafts' layouts.
- Questions for Jackson: none.
- Next: step 8, the done-when check.

## 2026-11-03: Phase 6, step 6: menu
- Did: the game opens on Jackson's main menu (A-002) over a bot match. `GameFlow` runs the front end: the demo match (an offline local game with nobody in it; `RuntimeConfig.DemoMatch` has bots fill every slot and the lobby start each match by itself), Start Game (hosts an online room under a 5-letter code of consonants), Join Game (a code, which only ever joins, never creates; or any open room), leaving or losing a room (back to the menu and the demo), one change at a time. `GameConnection` (from the SDK's start UI connection, keeping its settings) starts and stops the games; MPPM clones follow the main editor into and out of rooms (`GameFlowMppmJoinCommand`/`LeaveCommand`). `MainMenu` screens: Main (Start Game, Join Game, Settings, plus Quit), Join (code field, Join, Join Any Room, Back), Settings (volume, look speed, invert Y, fullscreen, resolution; `GameSettings`, saved in PlayerPrefs; the orbit camera applies look speed and invert Y), Pause in a room (room code, Resume, Settings, Leave Room), a connecting notice, the error under the column, and the room code top right while playing. Mouse and controller: each screen selects its first control, Escape or B goes back, Escape or Select pauses in a room (Start still starts the match), and the selected control shows (hover look on buttons, orange on the rest). While a menu is up the player's input and the IMGUI HUD stay out. Jackson's art: the re-exported button over `UI_MenuButton`, its new hover and pressed looks (`UI_MenuButton_Hover`/`_Pressed`, 9-slice ends), TextMeshPro font assets from both Jaro fonts. Built by **Block Sauce › Build Main Menu** (`MainMenuBuilder`) into `Assets/Placeholders/PH_MainMenu.prefab` and the Lobby (stock start UI removed, local debug runner off).
- Verified (one client, driven through `GameFlow` since an unfocused editor drops clicks): menu over a 6-bot local match with the director on and Start Game selected; host → room GNBQP, alone in its lobby, orbit camera on the player, the code top right; pause → Resume selected, input blocked; leave → menu and demo back; a made-up code → "No room with the code ZZZZZ."; any room with none open → "No open rooms right now. Start a game instead?"; Join and Settings screens captured with focus showing. Console: only Photon's line for the missing room.
- Fixed: the demo's first start timed out while the editor hitched loading play mode; the start timeout is 30 s and the demo retries.
- Decided: the menu is an overlay in the Lobby scene rather than its own scene, since the match runs there until the arena exists; the demo is a runtime-config flag so it works in release builds; Quit added under Jackson's three buttons; the 8-bot "one-button" test (F6) stays a dev key.
- Not yet checked: a second client joining by code (step 8, with MPPM), and real mouse and controller input by hand.
- Assets: A-002 hover and pressed looks in; A-002 updated (Join, Settings, Pause screens; slider, toggle, dropdown and text field looks; a disabled look).
- Questions for Jackson: none.
- Next: step 7, the room screen and HUD.

## 2026-11-03: Phase 6, 6 players; lighter spectator camera
- Did (Jackson: about 40 fps with the full game): players cut from 8 to 6 (`max_players 6` in `Input.qtn`, which also sizes the Photon room); the two blues left the palette (`PH_PlayerColors` keeps red, orange, yellow, light green, green and teal). The Lobby's ring of 8 spawn points is shared evenly among the 6 slots in code (`PlayerSystem.FindSpawnTransform`), leaving the scene alone. The spectator director now rescores what to watch 10 times a second instead of every frame (the tower count it uses loops over all blocks for every block, per player), moves only the current shot with its subject in between, builds its label text only when the shot changes (IMGUI asked for it several times a frame), and no longer allocates a lambda each frame for the overview.
- Verified (one client, all-bot match): 6 slots (the player in 0, bots in 1–5) standing at 0°, 45°, 135°, 180°, 225° and 315°; the spectator modes as before (follow next, wrap, previous, overview, auto), the camera 7.3–7.6 m from a followed player; console clean. Frame rate not measured here (an unfocused editor throttles); Jackson's machine is the check.
- Decided: the frame-rate drop is more likely the bots and physics than the camera; the plan review now opens with a profiling and optimization pass (see the plan).
- Assets: A-006 mock-ups for 2 and 6 players.
- Questions for Jackson: none.
- Next: step 6, the menu scene.

## 2026-11-03: Phase 6, spectator controls
- Did (Jackson's request): the spectator director has three modes. Auto is the director as before; Follow keeps the camera on one player (7 m back, drifting round them), previous or next in slot order, wrapping; Overview holds the wide shot of everyone. If a followed player leaves, it goes back to Auto. `SpectatorControls` (placeholder IMGUI bar at the bottom, until A-006): what the camera shows ("Auto · Player 3's tower", "Following Player 8", "Overview"), ◀ ▶ to follow the previous or next player, Overview and Auto buttons (the active one highlighted). Keys: ← → follow, ↑ overview, ↓ auto; the gamepad's d-pad the same (both were unbound). F5 now brings up the controls with the director; the orbit camera leaves the cursor free while it directs. The menu's background match will use the director without the controls.
- Verified (one client, F6 all-bot match, modes driven through the director's methods): Auto on Player 3's tower; next followed Player 8, next wrapped round to Player 1, previous back to Player 8, the camera 6.4–7.6 m from the followed player each time; Overview held the wide shot; Auto went back to the director. Screenshots show the bar and each view. Console clean. The keys and the buttons' clicks weren't driven (an unfocused editor drops input); they call the same methods.
- Decided: following a player starts from whoever the current shot is about (a tower's owner, a knocked-down player), so ▶ during an Auto shot picks up that player.
- Assets: A-006 (HUD) should include the spectator bar: the current view's label, previous / next player, Overview, Auto.
- Questions for Jackson: none.
- Next: step 6, the menu scene (with Jackson's new menu art).

## 2026-11-03: Phase 6, step 5: spectator director camera
- Did: `SpectatorDirector` (view only, on the main camera, after the orbit camera): each frame it scores a wide shot of everyone (1), each player's best tower (1 + 1.5 per counted block, + 1.5 per level while its top settles), a block in flight for 2 s after a throw (6), a knockdown for 3 s (5), and two or more players within 3 m of a vat (1 + players). It holds a shot at least 3 s, switches early only for something 2 points more interesting (or when the subject is gone), and moves on to the next best after 7 s. A subject within 8 m of the current focus is eased to over 1.2 s; further away, cut to. Framing: 35° down, 6–14 m away by shot (the wide shot pulls back to fit everyone, up to 26 m), drifting 5°/s round the subject, from the arena's middle side so walls stay behind, kept inside ±18 m and at least 3 m up. Positions come from the entity views, so it moves smoothly between ticks. Dev key F5 switches the camera to it and back; the menu scene (step 6) will use it with no orbit camera.
- Verified (one client, F6 all-bot match with the director on): over a 40 s match it changed shot 13 times on its own: tower shots as stacks grew, throws, and the wide shot in the lobby; screenshots at 5 s intervals all framed players and towers from above. Console clean.
- Decided: interest scores and timings live on the component, for tuning by eye in the menu scene.
- Assets: none.
- Questions for Jackson: none yet; judge the camera's feel once it runs behind the menu.
- Next: step 6, the menu scene (with Jackson's new menu art).

## 2026-11-03: Phase 6, step 4: 8-bot test match
- Did: dev key F6 (`CommandDevBotSelf`): in the lobby, your character is handed to a bot and the match starts with bots filling the other slots; F6 again takes it back. `BotSystem.MakeBot`/`ReleaseBot` shared with new bots and with a player taking over a bot's slot. Brain fixes from the soak: a bot hesitates 0.3–1 s before each task; bots raid only a rival with at least 2 counted blocks (`RaidAtCount`); a bot with no home carries its block back to where its blocks started and sets it down there; bots don't chase a block another bot is fetching, leave a block they failed to fetch alone for 20 s, may take a block lying on another one, and set down anything that isn't a block (a knocked-down player); `MatchSystem.TryGetBlockRowCenter`.
- Verified (one client, its own character a bot, all 8 slots bots, matches started back to back): five matches, each won by a bot, in 37, 18, 125, 158 and 66 s; 10 short stuck moments (a few seconds against another bot or a wall), all freed by the detour or the task timeout; F6 at the end gave slot 0 back to the player. Console: the view updater assert above, once, in the lobby after the run.
- Fixed (found by the soak): with raids at any stack, misses piled up in one corner and every bot wedged there (no winner in 15 minutes); a bot that grabbed a knocked-down player threw an exception; blocks lying on other blocks counted as out of reach, which left every bot idle once blocks were scattered.
- Decided: the 8-bot test is your own character as a bot, so it runs in any live room with no extra slot.
- Assets: Jackson's new menu art is in `_Incoming` (button bar normal, hover and press; a mark; a menu mock-up); integrated in step 6.
- Questions for Jackson: undisturbed, a bot stacks its three blocks (spawned 1.2 m apart) in about 8 s of play, so matches swing between 18 s and 2.5 minutes depending on how much raiding happens. Should bots be slower or clumsier (say, a longer hesitation, slower walking with a block, a missed placement now and then)? Easy to tune on `BotConfig`.
- Next: step 5, the spectator director camera.

## 2026-11-03: Phase 6, step 3: bot brain
- Did: the bots' brain is a Bot SDK state machine (`HFSMRoot` with four states: Waiting, Fetching, Using, Pausing), initialized when a bot is added and updated by `BotSystem` each tick before it writes the bot's input. Our actions and decisions for it: `BotFetchAction`, `BotUseBlockAction`, `BotStopAction`; `BotMatchPlayingDecision`, `BotTaskEndedDecision`, `BotWaitedDecision`. The choices behind them are in `BotChoices`: fetch the nearest loose own block (not the home block), or, when all its blocks are stacked and a rival has a tower, a rival's; own block in hand goes on the tower (a Stack dunk first, 25% of the time, if uncoated); with no block on the ground it's set down to become the home block; a rival's block is thrown at the leading rival's tower. New `Drop` task. The graph is built by **Block Sauce › Build Bot Brain** (`BotBrainBuilder`) into `Resources/Bots/Brain`, because the SDK's visual editor can't create its documents from a script; the actions and decisions appear in the visual editor too, so the graph can be redrawn there.
- Verified (one client standing still, 7 bots filling the match at start): two matches won by a bot, a locked 3-block tower in 65 s and in 33 s; each bot cycled fetch, place, dunk and throw (first match: 40 fetches, 19 placements and 9 misses, 4 dunks, 8 throws, 2 drops); console clean.
- Fixed: the action and decision classes shared one file, so Unity saved their assets without a script and the simulation loaded them as null (each now has a file of its own name; the builder replaces assets that lost their script). A bot that dropped its own block to start a tower fetched it straight back, and a bot raided blocks with no tower to throw them at; both looped (138 drops in 150 s, no winner).
- Decided: the brain's numbers stay on `BotConfig` rather than the SDK's `AIConfig`, next to the action tuning. Bots raid only when every own block is stacked.
- Assets: none.
- Questions for Jackson: none.
- Next: step 4, the 8-bot test match in a live room (and the dev key that adds bots and starts).

## 2026-11-03: Phase 6, step 2: bot actions
- Did: `BotActions` carries out a bot's task (`Bot.Task`: GoTo, Fetch, PlaceOn, ThrowAt, Dunk; `Status` Running/Done/Failed, with a per-task timeout) as a `BotIntent` that `BotSystem` turns into the player's `Input` (buttons through `Button.Update`, so presses and releases register like a player's). Walking: straight at the target, probing rays at knee and chest height for the nearest clear angle, and a sideways detour when it hasn't moved for a second. Fetch: walk up, creep so the body faces the block, press grab. PlaceOn, low tower: the lowest swing pitch that clears the top (reaching furthest out), stop at 0.95 m so the body doesn't shove it, let go over the center. PlaceOn, tall tower (top above 1 m): block raised to 60° and close to the body, line up at 1.3 m, jump straight up, and once the block clears the top lower it to 30° to reach out over the tower and let go; up to 3 jumps. ThrowAt: walk into 3–8 m, aim (turning the body through the swing) along the low ballistic arc for the 12 m/s throw, release. Dunk: the scripted dunk's moves (raise, walk into the cup, lower, soak, lift, back off). Knocked down: tap Jump. `SetTask` for the brain; test command `CommandDevBotTask` (picks an obvious target when none is given). Tuning on `BotConfig`.
- Verified (one client, lobby sandbox, a bot driven through each action): fetch 2–3 s; place on a ground block 1.6–1.9 s, three times out of three; jump-place onto its own 2-block tower 1.7 s on the first jump, three times out of three, each landing within 0.4 m of center; dunk to full Stack strength in 8–11 s; throw at a player 0.6–2.7 s; up again 1.0 s after a knockdown; a 4th block on a 3-high stack (out of reach) fails cleanly after 15 s; console clean.
- Decided: placing on a tall tower is a jump, as for players; the block is raised close to the body until it clears the top, because walking or jumping in with it held out knocked towers over.
- Assets: none.
- Questions for Jackson: none.
- Next: step 3, the bot brain (Bot SDK state machine).

## 2026-11-03: Phase 6, step 1: bot plumbing
- Did: `Bot` component, `BotConfig` (avatar; referenced by the mode as `Bots`), `BotSystem` (runs before `PlayerSystem`): bots take the highest free player slots and write `Player.Input` themselves; `PlayerSystem` skips the network copy for them, and a client given a bot's slot takes over its character. Commands `CommandAddBot`, `CommandRemoveBot` (highest slot), `CommandSetBotFill` (lobby only); a match start fills the empty slots unless `Match.LeaveSlotsEmpty`. `PlayerSystem.SpawnCharacter`/`TryGetCharacter` shared with bots. Placeholder brain: walk forward, turning slowly. Dev key F7 adds a bot in the lobby, Shift+F7 removes one.
- Verified (one client): 3 bots added took slots 7, 6, 5 and wandered at walking speed; Remove took slot 7; starting the match filled slots 1–4 and 7, all 8 on their match spawns with 3 blocks each, still through the countdown, then wandering; console clean.
- Not yet checked: a second client joining a room full of bots takes over a bot's slot (done-when, two clients).
- Decided: room commands are accepted from any player for now; the room screen shows them to the host only.
- Assets: none.
- Questions for Jackson: none.
- Next: step 2, bot actions.

## 2026-11-03: Phase 6 planned
- Did: replanned the phases with Jackson: Phase 6 is the front end and bots (menu and UI draft, spectator director camera, bots, one-button 8-bot test), then a plan review; the arena, juice, audio and tuning wait for the review. Wrote the Phase 6 build order, risks and questions; reordered `ASSETS.md` around it.
- Verified: nothing to run.
- Decided: bots take free player slots and write `Player.Input` (every system already reads input there, and ownership and scoring key on player slots); UI in uGUI with TextMeshPro (in Unity 6's uGUI); the menu's background match runs offline on the local machine.
- Assets: none new.
- Questions for Jackson: the five Phase 6 questions in Next (answered the same day).
- Next: step 1 (bot plumbing).

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
