# Block Sauce: progress

Where the build stands and what happens next. The agent adds a short entry at the top of the log for each work session, and a report when a phase's done-when is verified. Older entries and phase reports live in git history.

## Current state (2026-11-03)

**Phases 0–5 are done and reviewed. Phase 6 (Front end and bots) is in progress: steps 1–3 are in, and bots play and win matches on their own.**

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
- A-002 button focus states and the remaining screen mock-ups; A-006 HUD mock-ups (drafts stand in until they arrive).
- Stack glue strength, and whether it should glue to the ground (open question in the plan; play-test).
- The older open questions (settle numbers; pickup swings through your own tower).

### Placeholders in use
`PH_PlayerCapsule` (Guy's view on it), `PH_Block` (with the subdivided `BL_Block` mesh), `PH_LockDither` (A-007/A-013), `PH_SauceVat_Stack`/`_Slip` (the cup at 1.3× with placeholder sauce), `PH_SauceCoat` and `PH_SauceLooks` (A-009), `PH_PlayerColors` (holds the real palette), `PH_` terrain (A-008), the IMGUI `MatchHud` (A-006), the stock Quantum start UI (A-002), `PH_RagdollTest` (stress-test scene only).

## Next: Phase 6 (Front end and bots)

Goal: the whole game seen end to end before the plan review: open on the menu over a bot match, host or join a room, add bots, play a match, come back. See `docs/PLAN.md` › Phase 6 for the done-when.

**Build order** (each step ends in something playable, and is committed):
1. **Bot plumbing.** A `Bot` component on a character marks it as driven by the simulation. Bots take free player slots (`PlayerRef`s nobody holds), so ownership, teams, colors, scoring and the HUD treat them as players. `PlayerSystem` skips the network input copy for bot slots; the bot writes `Player.Input` itself, through the same fields a player's input fills. Commands to add and remove a bot (host only); a match start fills empty slots with bots when the room's setting is on. Check: a bot that stands still, then one that walks in a circle, joins a match and gets blocks.
2. **Bot actions** (our C#, deterministic): steer to a point (avoiding vats, towers and players), turn to face, grab a target block, carry, raise and lower (swing pitch), place on top of a tower, aim and throw at a target, dunk in a vat, recover from a knockdown. Each one testable alone with a dev command.
3. **Bot brain** (Bot SDK state machine, `HFSMAgent`; done): Waiting (outside a match) → Fetching (nearest loose own block; when all are stacked, a rival's, if a rival has a tower) → Using (own block: place on the tower, after a Stack dunk a quarter of the time; rival's block: throw at the leading rival's tower) → Fetching; a failed task pauses a second. Numbers on `BotConfig`. Bots should look like players, not play well.
4. **8-bot test match**, set up in a live room through the same add-bot and fill-at-start paths (plus a dev key that adds bots and starts, for scripted runs). Run it until 8 bots finish a match on their own; this is also the soak test for bots stuck on terrain or each other.
5. **Spectator camera** (view only): a director that picks a subject by interest (a tower near completion, a throw in flight, a knockdown, a scramble near a vat), holds each shot a few seconds, and cuts or eases to the next. Cinematic framing from above and to the side; no input.
6. **Menu scene.** A new `Menu` scene: loads the arena, starts an offline local game (`DeterministicGameMode.Local`) with 8 bots looping matches, and shows Jackson's menu over the spectator camera. Start Game: shuts the background game down and hosts an online room with a short room code. Join Game: a code field. Settings: volume, look sensitivity, invert Y, fullscreen and resolution, saved locally. uGUI with TextMeshPro and the Jaro font; controller and mouse navigation through the EventSystem. Escape or the gamepad's Back opens a pause menu in a room (leave room, settings). Replaces `BlockSauceStartUI` and the stock Quantum start UI.
7. **Room screen and HUD** (rough draft in Jackson's style): room code, player list in colors with bots marked, add/remove bot, bots-fill-slots toggle, start match; match HUD (countdown, each player's three tower slots, winner) and results. Replaces the IMGUI `MatchHud`.
8. **Done-when check:** menu over a bot match; host a room, add bots, play a full match, back to the menu; a second client (MPPM) joins by code; 8 bots finish a match alone. Two clients for the online parts; console clean.

**Risks:**
- Bot movement is the hard part, not the brain: Guy is a force-driven body with knockback, ragdolls, uneven ground and towers to path around. Start with straight-line steering and local avoidance; bake a Quantum NavMesh only if bots get stuck.
- Placing squarely on a tower needs fine control of the swing; bots may need a tolerance and retry, and may sometimes knock their own tower over (fine, players do too).
- Room codes: Photon room names are the code; a short random code needs a collision check (join fails, try another).
- Offline background game and online rooms in one process: the background session must shut down cleanly before connecting, and the menu must survive a failed connection.
- Bot SDK on a preview Quantum build: it compiles; runtime use is untested until step 3.
- The stock start UI's MPPM hooks (`QuantumStartUIMppm*`, our SDK patch) keep multi-client tests working; the new menu needs the same MPPM connect path.

**Answered (Jackson, 2026-11-03):**
1. Join Game: a room code, plus a "join any open room" button.
2. Bots: the host adds them by hand, and they fill empty slots at match start; the fill is a setting on the room screen, on by default. Jackson's next art drop includes room UI for it.
3. No solo offline mode: Start Game hosts online, and a room can be all bots. The 8-bot test is for testing only: set up in a live room (add 7 bots, or fill at match start), not a separate button.
4. One bot skill level, a bit clumsy; difficulty later.
5. The menu's background match runs in the Lobby until the arena exists.

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
