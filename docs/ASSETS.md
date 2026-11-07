# Block Sauce: assets needed

Every asset still needed, in the order to make them. The top of the list is what the next phase builds against. Export settings, scale and naming are in `docs/PLAN.md` › Art pipeline. Drop finished files in `Assets/Art/_Incoming/` under any name; the agent integrates them, then removes the item from this list (or notes what still needs changing).

Guy clips go in the one animated `CH_Guy.fbx` (mesh, deform skeleton, all actions), animated in place, at least two keyframes per action. Game speeds below are real-world (Blender units ÷ 2.5).

## Arena (in: first graybox, 2026-11-05)

1. **A-008 Arena** (graybox in as `EN_Arena` and `EN_ArenaPieces`; Phase 7 built on it).
    - How it was read: **Block Sauce › Build Arena** uses `EN_Arena` for the layout and visuals (`displace_ground`, `exterior_wall`, `inner_circle`, `spawn_pillars`, `spawn_pillar_stairs`) and only `COL_stairs` from `EN_ArenaPieces`, fitted onto each stair by matching `MESH_stairs`. The rest of `EN_ArenaPieces` isn't used (its inner circle differs from the arranged one: 336 triangles and a deeper base). The ground, wall, ring and pillars collide as their render meshes, which is fine at these triangle counts (the ground's 32,768 are the most).
    - Spawns and block spots are measured from the pillar and ring meshes, so no empties were needed. If a re-export moves things, rerun Build Arena; for anything the measuring can't guess, add empties named `SPAWN_01`–`06`.
    - For a re-export: keep the object names; `COL_stairs` has collinear (zero-area) triangles that Quantum warns about at every bake (harmless; merge or dissolve them to quiet it); the stairs' top step is about 0.53 m below the pillar top, just over Guy's 0.5 m step, so players hop the last bit (raise the stairs or lower the pillars a little if that feels wrong).
    - Materials: everything uses one default material (`Lit`); separate materials for ground, wall, ring and pillars would let them read apart.

## Touch-ups: grab and match end

The agent builds these with placeholders until Jackson's designs arrive.

1. **A-025 Grab highlight and settle/stack highlight** (Jackson is writing the detailed design, 2026-11-06; the agent builds the shaders from it).
    - Grab highlight: the one block a Grab press would take right now (the block the camera looks at), for the local player only.
    - Settle/stack highlight: while holding a block, where it would land and whether it would stack and count there.
    - Things the design should cover, from the build side: reading on any player color, under a sauce coat and over the settle dither (A-007); not looking like the ownership highlight A-013 plans; what shows when no block is in reach.
2. **A-006 addition: match timer** (goes with the HUD mock-ups below). A 5-minute clock and a SUDDEN DEATH banner when it runs out. Draft built (2026-11-06): the clock leads the headline bar, top center ("4:32   STACK ALL 3 BLOCKS INTO ONE TOWER"); in sudden death the headline reads "SUDDEN DEATH 1:47   NEXT BLOCK WINS" and "SUDDEN DEATH!" shows big for 2.5 s; a timed-out tie shows DRAW. Sound: sudden death reuses the match-start sound until the bank has its own (A-014).

## Phase 6 (front end and bots)

The agent builds rough drafts of every screen in the style of the main menu mock-up; these mock-ups replace the drafts as they arrive. UI screens are 1920×1080; final art PNG at 2× with 9-slice margins noted.

1. **A-002 Menu and room UI** (main menu delivered 2026-11-03).
    - In: the main menu mock-up (`UI_Mockup_MainMenu`: logo and three buttons on the right, Start Game, Join Game, Settings, over a bot match seen from the spectator camera), `UI_Logo` (729×387; the sauce-splatter version, delivered 2026-11-05 as `mark.png`), `UI_MenuButton` (496×101) and the Jaro font in two optical sizes (`UI_Jaro_36pt` for headings, `UI_Jaro_9pt` for small text; OFL license alongside).
    - In since: the button's hover and pressed looks (`UI_MenuButton_Hover`, `_Pressed`; hover doubles as controller focus). The menu is built from these (`PH_MainMenu`), with Quit added under the three buttons and BACK on the same bar.
    - Most needed: mock-ups for Join Game (today: a code field on the button bar, Join, Join Any Room, Back), Settings (today: label left, control right, for volume, look speed, invert Y, fullscreen, resolution) and Pause in a room (today: the room code, Resume, Settings, Leave Room); looks for a slider, a checkbox, a dropdown and a text field (normal, focused), and a disabled button (shown while connecting). Today's drafts sit in the right-hand column under the logo, 357×72 per row at 1920×1080.
    - Then the room screen and the room code badge shown while playing (today top right, 380×64). Today's draft: a panel on the left (720×680, top-left at 160,160) with ROOM and the code, a hint line, and six slot rows (color swatch, PLAYER n / BOT n / OPEN, a YOU / HOST / BOT tag); the column on the right holds Start Match, Add Bot, Remove Bot, a Bots Fill Slots checkbox, Play in Lobby, Settings, Leave Room (100 px apart from y 300; no logo). Guests see "Waiting for the host" in place of the host's controls. Wanted: the slot row (with a bot marker and a host marker), the panel, and how an open slot looks.
    - Bot Enhancement adds a difficulty control to each bot's row: ◀ EASY / NORMAL / HARD / EXPERT ▶ at the row's right end (about 220×48), arrows for the host, plain text for guests. The draft uses the row's colors; a look for it (normal, focused) goes with the slot row.
    - The logo and button at 2× for sharp 4K (1458×774, 992×202).
2. **A-006 Match HUD and results screen mock-ups**.
    - States: Lobby (press Start or Enter), Countdown 3 s, Playing, Round end 3 s with the winner, Results 6 s. Per player: their color and 3 slots for their best stack, each empty, settling (0 to 100%) or counted. Mock-ups at 1920×1080 for 2 and 6 players. Bots show in the same list, marked as bots.
    - Today's draft (`PH_MatchHud`): a headline bar top center (900×72, Jackson's button shape in the dark shade) for GET READY and the goal; the countdown, GO! and the winner big in the middle (up to 200 px, the winner in their color); the tower list top left (500×380: swatch, name, three 46×40 slots, bottom of the tower on the left); the lobby prompt bar bottom center (1100×64); results in a centered 820×700 panel (RESULTS, the winner, six ranked rows with n / 3, the seconds until the room). Wanted: the slot looks (empty, settling, counted), a countdown and GO! treatment, the winner banner and the results panel.
    - Spectator bar (shown while spectating in a room): a label for what the camera shows ("Auto · Player 3's tower", "Following Player 8", "Overview"), previous / next player, Overview and Auto, the active mode marked, and a hint line for the keys (arrows / d-pad). Today's placeholder sits bottom center, about 640×78.
3. **A-010 Logo variants** (the logo itself is in as `UI_Logo`).
    - A small version for the in-game HUD corner and the window icon (256×256), if the full logo doesn't shrink well.

## After the plan review

4. **A-009 Sauce looks** (vat delivered).
   - The cup (`EN_SauceVat`, re-exported 2026-11-03) is in, scaled 1.3× in its prefab so the rim (0.61 m) is above Guy's 0.5 m step and players can't walk in. Its collision now follows the art (24 walls leaning out 14° along the inner wall, and its floor), so no re-export is needed.
   - Still needed for Stack sauce and Slip sauce: color, gloss and a cue that reads without hue (player colors own hue). Placeholders: Stack dark brown with rising bubbles, Slip pale silver with glints (`PH_SauceLooks`). A sauce surface mesh for the cup (a disc at 0.55 m in game, 0.42 m in Blender before the 1.3×) is optional; a flat disc stands in.
5. **A-013 Block owner readability** (visual polish).
   - Jackson is rethinking ownership at the top level; likely a personal UI that highlights your own blocks. Must leave room for the settle overlay (A-007) and a sauce coat.
6. **A-007 Settle and count effect direction**.
    - The look of a stacked block filling up over 3 s (any hit restarts it), and the pop when it counts. A counted block can still be knocked off and goes back to its plain owner color. A few frames or a reference clip; the agent builds the shader. Placeholder: `PH_LockDither` (a 4×4 dither climbing in the owner's darker shade).
7. **A-023 Guy win and lose, final**.
   - Placeholders are in: `Win.Jump` (0.33 s, plays once and holds) and `Sitdown` (1 s) for lose, played through round end (3 s) and results (6 s). The lose action is named `Guy,Lose.Sitdown` (a comma, not a dot); it imports fine as `Sitdown`. Final clips can loop or hold, any length.
8. **A-024 Guy slide polish** (nice to have).
   - Strafes are fixed: the planted foot now moves about 0.5 m/s, so they play at 1.1× walking and 2.25× running.
   - `Slide` holds the sit pose; any motion in it (lean, arms out, dust-kicking feet) plays while a sitting Guy moves faster than 2 m/s.
9. **A-014 Sound effects** (placeholders in, 2026-11-05: 16 sounds and 2 music loops picked by name from Jackson's CC0 FreeSFX pack, in `PH_SoundBank`; the whole pack stays in `Assets/Art/Audio/_Pack_FreeSFX`, kept out of git). Swap any pick in **Block Sauce › Build Sound Bank** or the bank asset.
    - WAV 48 kHz mono, short; one sound each, or a CC0 pack to pull from. Impacts want 2 to 3 variations and a light and heavy version.
    - Events: menu move, select and back; jump, grab, drop, place, throw, block impact (by speed), knockback, knockdown thud, dazed loop, limb impacts, get-up, settle start, settle reset, count pop, uncount, countdown beeps, round start, win; sauce dip splash, Stack sauce sticky landing, Slip sauce slide.
