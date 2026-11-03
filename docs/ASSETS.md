# Block Sauce: assets needed

Every asset still needed, in the order to make them. The top of the list is what the next phase builds against. Export settings, scale and naming are in `docs/PLAN.md` › Art pipeline. Drop finished files in `Assets/Art/_Incoming/` under any name; the agent integrates them, then removes the item from this list (or notes what still needs changing).

Guy clips go in the one animated `CH_Guy.fbx` (mesh, deform skeleton, all actions), animated in place, at least two keyframes per action. Game speeds below are real-world (Blender units ÷ 2.5).

1. **A-009 Sauce looks** (vat delivered): Phase 5/6.
   - The cup (`EN_SauceVat`, re-exported 2026-11-03) is in, scaled 1.3× in its prefab so the rim (0.61 m) is above Guy's 0.5 m step and players can't walk in. Its collision now follows the art (24 walls leaning out 14° along the inner wall, and its floor), so no re-export is needed.
   - Still needed for Stack sauce and Slip sauce: color, gloss and a cue that reads without hue (player colors own hue). Placeholders: Stack dark brown with rising bubbles, Slip pale silver with glints (`PH_SauceLooks`). A sauce surface mesh for the cup (a disc at 0.55 m in game, 0.42 m in Blender before the 1.3×) is optional; a flat disc stands in.
2. **A-013 Block owner readability**: Phase 6 (visual polish).
   - Jackson is rethinking ownership at the top level; likely a personal UI that highlights your own blocks. Must leave room for the settle overlay (A-007) and a sauce coat.
3. **A-007 Settle and count effect direction**: Phase 6 (any time before).
    - The look of a stacked block filling up over 3 s (any hit restarts it), and the pop when it counts. A counted block can still be knocked off and goes back to its plain owner color. A few frames or a reference clip; the agent builds the shader. Placeholder: `PH_LockDither` (a 4×4 dither climbing in the owner's darker shade).
4. **A-008 Arena layout sketch**: Phase 6.
    - Top-down sketch with rough size, elevation changes, landmarks, room for 8 towers anywhere, and vat placement (the Lobby has two vats in the middle, 4.2 m apart).
5. **A-006 Match HUD and results screen mock-ups**: Phase 6.
    - States: Lobby (press Start or Enter), Countdown 3 s, Playing, Round end 3 s with the winner, Results 6 s. Per player: their color and 3 slots for their best stack, each empty, settling (0 to 100%) or counted. Mock-ups at 1920×1080 for 2 and 8 players.
6. **A-002 Main menu and lobby UI mock-ups** (main menu delivered 2026-11-03): Phase 6.
    - In: the main menu mock-up (`UI_Mockup_MainMenu`: logo and three buttons on the right, Start Game, Join Game, Settings, over a bot match seen from the spectator camera), `UI_Logo` (669×387), `UI_MenuButton` (496×101) and the Jaro font in two optical sizes (`UI_Jaro_36pt` for headings, `UI_Jaro_9pt` for small text; OFL license alongside).
    - Still needed: the logo and button at 2× for sharp 4K (1338×774, 992×202), the button's highlighted and pressed looks (controller focus), and mock-ups for Join Game (room code or list), Settings, and the in-room lobby overlay (player list in colors, start).
7. **A-023 Guy win and lose, final**: Phase 6.
   - Placeholders are in: `Win.Jump` (0.33 s, plays once and holds) and `Sitdown` (1 s) for lose, played through round end (3 s) and results (6 s). The lose action is named `Guy,Lose.Sitdown` (a comma, not a dot); it imports fine as `Sitdown`. Final clips can loop or hold, any length.
8. **A-024 Guy slide polish**: Phase 6 (nice to have).
   - Strafes are fixed: the planted foot now moves about 0.5 m/s, so they play at 1.1× walking and 2.25× running.
   - `Slide` holds the sit pose; any motion in it (lean, arms out, dust-kicking feet) plays while a sitting Guy moves faster than 2 m/s.
9. **A-014 Sound effects**: Phase 6; temporary sounds welcome any time.
    - WAV 48 kHz mono, short; one sound each, or a CC0 pack to pull from. Impacts want 2 to 3 variations and a light and heavy version.
    - Events: jump, grab, drop, place, throw, block impact (by speed), knockback, knockdown thud, dazed loop, limb impacts, get-up, settle start, settle reset, count pop, uncount, countdown beeps, round start, win; sauce dip splash, Stack sauce sticky landing, Slip sauce slide.
10. **A-010 Logo and wordmark**: Phase 6.
    - SVG plus PNG at 2×, light and dark variants.
