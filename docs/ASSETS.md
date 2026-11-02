# Block Sauce: assets needed

Every asset still needed, in the order to make them. The top of the list is what the next phase builds against. Export settings, scale and naming are in `docs/PLAN.md` › Art pipeline. Drop finished files in `Assets/Art/_Incoming/` under any name; the agent integrates them, then removes the item from this list (or notes what still needs changing).

Guy clips go in the one animated `CH_Guy.fbx` (mesh, deform skeleton, all actions), animated in place, at least two keyframes per action. Game speeds below are real-world (Blender units ÷ 2.5).

1. **A-017 Guy strafe left and right (walk and run)**: Phase 4.
   - A carrier always strafes (the swing holds the facing), so this is the most-seen missing clip. Currently a sideways move plays the forward walk.
   - Loops, matching `WalkBasic`/`RunBasic` timing (24 frames). Walk is sped up from slow to 3.3 m/s, run covers 3.3 to 6 m/s; feet planted at those speeds avoid sliding.
2. **A-018 Guy stagger flinches (front, back, left, right)**: Phase 4.
   - Played on the upper body over locomotion when a hit staggers him; he keeps sliding on momentum and can't steer.
   - Short and readable: a stagger lasts 0.25 to 1.2 s depending on the hit, so a 0.3 to 0.5 s flinch that settles back to idle works for all of them.
3. **A-019 Guy crouch idle and crouch walk**: Phase 4.
   - The crouch capsule is 1.10 m tall (standing 1.66 m); crouch walk moves at 1.5 m/s. Loops.
4. **A-020 Guy slide (enter, loop, exit)**: Phase 4.
   - Run plus crouch: 7.5 m/s for 0.75 s, about 4 m of travel. Enter about 0.1 s, loop for the rest, exit back into crouch or run. Must fit inside the 1.10 m crouch height.
5. **A-021 Guy two-handed aim pose and jumpshot**: Phase 4.
   - Holding Throw raises the block, in both hands in front of him, to 30° above the hold line and keeps it there while he aims; releasing throws. The existing `ThrowPose` is one-handed overhead and goes unused.
   - Needed: an aim pose holding a 0.8 m block at that raised point (loop, any length), and an airborne version for the jumpshot. The block leaves on release; `Throw` already plays as the follow-through.
6. **A-022 Guy place**: Phase 4.
   - Grab while holding sets the block down where it is held. A short (about 0.3 s) upper-body set-down from the `GrabBlock` hold to empty hands.
7. **A-023 Guy win and lose**: Phase 4.
   - Played on Round end (3 s) and Results (6 s). Loops or holds, any length. Eyes, jaw and mouth bones can be keyed for expressions.
8. **A-005 Block, final mesh**: Phase 5 (the sauce shader needs it).
   - The draft (`BL_Block`, 0.8 m in game, 2 m in Blender) works for everything up to then; size and collider stay exactly as they are.
   - Final needs even subdivision loops on every face (the sauce coat displaces vertices; URP has no tessellation), UVs, and the pivot at the center (the draft's pivot is on the bottom face).
9. **A-013 Block owner readability**: Phase 5 (before sauce coats sit on top).
   - How a block shows its owner from across the arena and while tumbling: full tint, colored bevel, stripe, icon, or a mix. A paint-over of the block is enough.
   - Must leave room for the settle overlay (A-007) and a sauce coat. Players 5 to 8 of the palette (Seagrass, Dark Cyan, Blue Slate, Cerulean) sit close together, and red next to green is a color-blind pair, so ownership needs a cue beyond hue.
10. **A-009 Sauce vat and sauce looks**: Phase 5.
    - Vat model, pivot at base center, sauce surface as a separate mesh at a known height. A block is dipped into it, so the opening must take a 0.8 m block at an angle.
    - For Stack sauce and Slip sauce: color, gloss and a material cue (bubbles, sparkle) that reads without relying on hue, since player colors own hue.
11. **A-007 Settle and count effect direction**: Phase 6 (any time before).
    - The look of a stacked block filling up over 3 s (any hit restarts it), and the pop when it counts. A counted block can still be knocked off and goes back to its plain owner color. A few frames or a reference clip; the agent builds the shader. Placeholder: `PH_LockDither` (a 4×4 dither climbing in the owner's darker shade).
12. **A-008 Arena layout sketch**: Phase 6.
    - Top-down sketch with rough size, elevation changes, landmarks, room for 8 towers anywhere, and vat placement.
13. **A-006 Match HUD and results screen mock-ups**: Phase 6.
    - States: Lobby (press Start or Enter), Countdown 3 s, Playing, Round end 3 s with the winner, Results 6 s. Per player: their color and 3 slots for their best stack, each empty, settling (0 to 100%) or counted. Mock-ups at 1920×1080 for 2 and 8 players.
14. **A-002 Main menu and lobby UI mock-ups**: Phase 6.
    - Main menu (play, settings, quit), create or join room, lobby overlay (player list in colors, start). 1920×1080, controller focus states shown.
15. **A-014 Sound effects**: Phase 6; temporary sounds welcome any time.
    - WAV 48 kHz mono, short; one sound each, or a CC0 pack to pull from. Impacts want 2 to 3 variations and a light and heavy version.
    - Events: jump, grab, drop, place, throw, block impact (by speed), knockback, knockdown thud, dazed loop, limb impacts, get-up, settle start, settle reset, count pop, uncount, countdown beeps, round start, win.
16. **A-010 Logo and wordmark**: Phase 6.
    - SVG plus PNG at 2×, light and dark variants.
