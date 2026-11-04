# Block Sauce: assets needed

Every asset still needed, in the order to make them. The top of the list is what the next phase builds against. Export settings, scale and naming are in `docs/PLAN.md` › Art pipeline. Drop finished files in `Assets/Art/_Incoming/` under any name; the agent integrates them, then removes the item from this list (or notes what still needs changing).

Guy clips go in the one animated `CH_Guy.fbx` (mesh, deform skeleton, all actions), animated in place, at least two keyframes per action. Game speeds below are real-world (Blender units ÷ 2.5).

## Phase 6 (front end and bots)

The agent builds rough drafts of every screen in the style of the main menu mock-up; these mock-ups replace the drafts as they arrive. UI screens are 1920×1080; final art PNG at 2× with 9-slice margins noted.

1. **A-002 Menu and room UI** (main menu delivered 2026-11-03).
    - In: the main menu mock-up (`UI_Mockup_MainMenu`: logo and three buttons on the right, Start Game, Join Game, Settings, over a bot match seen from the spectator camera), `UI_Logo` (669×387), `UI_MenuButton` (496×101) and the Jaro font in two optical sizes (`UI_Jaro_36pt` for headings, `UI_Jaro_9pt` for small text; OFL license alongside).
    - Most needed: the button's highlighted and pressed looks (controller focus shows which button is selected), and a back button.
    - Then mock-ups for: Join Game (type a room code; maybe a list of open rooms), Settings (volume, look sensitivity, invert Y, fullscreen, resolution), and the room screen (room code to share, player list in their colors with bots marked, add and remove bot buttons, start match).
    - The logo and button at 2× for sharp 4K (1338×774, 992×202).
2. **A-006 Match HUD and results screen mock-ups**.
    - States: Lobby (press Start or Enter), Countdown 3 s, Playing, Round end 3 s with the winner, Results 6 s. Per player: their color and 3 slots for their best stack, each empty, settling (0 to 100%) or counted. Mock-ups at 1920×1080 for 2 and 8 players. Bots show in the same list, marked as bots.
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
7. **A-008 Arena layout sketch**.
    - Top-down sketch with rough size, elevation changes, landmarks, room for 8 towers anywhere, and vat placement (the Lobby has two vats in the middle, 4.2 m apart). Bot matches behind the menu run wherever matches run, so the arena is also the menu's backdrop.
8. **A-023 Guy win and lose, final**.
   - Placeholders are in: `Win.Jump` (0.33 s, plays once and holds) and `Sitdown` (1 s) for lose, played through round end (3 s) and results (6 s). The lose action is named `Guy,Lose.Sitdown` (a comma, not a dot); it imports fine as `Sitdown`. Final clips can loop or hold, any length.
9. **A-024 Guy slide polish** (nice to have).
   - Strafes are fixed: the planted foot now moves about 0.5 m/s, so they play at 1.1× walking and 2.25× running.
   - `Slide` holds the sit pose; any motion in it (lean, arms out, dust-kicking feet) plays while a sitting Guy moves faster than 2 m/s.
10. **A-014 Sound effects**; temporary sounds welcome any time (menu clicks and a menu music loop would help the Phase 6 draft).
    - WAV 48 kHz mono, short; one sound each, or a CC0 pack to pull from. Impacts want 2 to 3 variations and a light and heavy version.
    - Events: menu move, select and back; jump, grab, drop, place, throw, block impact (by speed), knockback, knockdown thud, dazed loop, limb impacts, get-up, settle start, settle reset, count pop, uncount, countdown beeps, round start, win; sauce dip splash, Stack sauce sticky landing, Slip sauce slide.
