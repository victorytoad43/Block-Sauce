# Block Sauce: progress log

The agent adds an entry at the top for each work session, and a phase report when a phase's done-when criteria are verified.

## Session entry format

```
## YYYY-MM-DD: Phase N, <short title>
- Did: <what was built or changed>
- Decided: <routine engineering decisions made without asking>
- Assets: <requests added, deliveries integrated>
- Blocked / questions for Jackson: <or "none">
- Next: <what the next session starts with>
```

## Phase report format

```
## Phase N report: <name>
- Built: <summary>
- Done-when verification: <each criterion and how it was checked>
- Known issues: <list>
- Placeholders still in use: <list with asset IDs>
- Ready for review: yes
```

---

## Phase 0 report: Foundations

- Built:
  - **Quantum:** 3.1.0 Preview build 2189 (from Friendslop), pinned. AppId preserved. 64 Hz, checksum interval 64, Friendslop's physics tuning.
  - **Git + LFS:** 4 commits on `main`.
  - **Hygiene:**
    - Template content removed; Run In Background on; company "Jackson Hill".
    - Multiplayer Play Mode 2.0.2 installed and enabled (Project Settings › Multiplayer › Play Mode local deployment; needed one editor restart).
    - Unity CLI connector (`com.unity.pipeline`).
  - **Friendslop study:** notes and porting verdicts above.
  - **Input:** the Quantum input definition covers every Phase 1 verb (move, camera, swing, jump, crouch, run, grab, throw). Unity-side bindings are in code.
  - **Simulation:** Player, PlayerLook, Team, GroundedTracker, camera-relative force-driven Movement.
  - **View:** orbit camera, input poller, local-player camera hookup.
  - **Content:**
    - `PH_PlayerCapsule` (1.66 m × 0.35 m radius).
    - `Lobby` and `Arena_Graybox` scenes with uneven `PH_` terrain, 8 spawn points, online start UI.
- Done-when verification: "two clients join a room and move capsules sized to Guy".
  - **Setup:** main editor plus MPPM virtual Player 2, Lobby scene, Photon cloud, one room.
  - **Join:** each client saw 2 connected players, itself local and the other remote, on teams 0 and 1 at spawn points 0 and 1, each camera on its own capsule.
  - **Moving both:** gamepad left-stick input was held on each client in turn (keyboard events can't be injected into an unfocused editor, so the test used a virtual gamepad).
    - Main client: stick forward at camera yaw −138.9° moved player 0 by (−2.10, −2.41), exactly the camera's forward direction.
    - Clone: stick right at yaw −27.1° moved player 1 by (2.94, 1.38), along camera-right.
    - After each move, both clients reported identical positions for both players.
  - **Earlier single-client run:** forward + run reached the wall and stopped at the wall face minus the capsule radius; braked to zero on release.
  - **Console:** clean on the clone; one SDK warning on the main editor (known issue 1).
- Known issues:
  1. ~~SDK warning with MPPM~~ Fixed 2026-10-28 with Jackson's OK. **SDK patch:** `[System.Serializable]` added to `QuantumStartUIMppmConnectCommand` and `QuantumStartUIMppmDisconnectCommand` in `Assets/Photon/Quantum/Runtime/StartUI/QuantumStartUI.Common.cs`, each marked with a "Block Sauce SDK patch" comment. **Re-apply it after any SDK update.** Verified: a two-client join and disconnect leaves both consoles free of it.
  2. **Guy import test not done:** waiting for the first animated FBX (A-003/A-004). The capsule radius (0.35 m) is a guess until then.
  3. **Placeholder controls:** default bindings, and the camera has no collision with walls yet. Phase 1 work.
  4. **Synthetic keyboard input in automated tests:** an unfocused editor drops it. Automated tests use a virtual gamepad instead. Manual play is unaffected.
- Placeholders still in use:
  - `PH_PlayerCapsule` (A-003).
  - `PH_Gray`, `PH_GrayDark`, `PH_Ground`, `PH_Player` materials.
  - `PH_Terrain` in both scenes (A-008).
  - Quantum's stock start UI for the menu (A-002).
- Ready for review: yes

## 2026-10-28: Phase 0, upgrade and first online movement

- Did:
  - **Quantum 3.1.0 Preview build 2189** replaced 3.0.14 (Quantum, PhotonLibs, PhotonRealtime, copied from Friendslop). The AppId is intact (settings file byte-identical to the backup).
  - Codegen was run before reopening Unity with a small .NET runner around `Quantum.CodeGen.Qtn.dll`, so the editor never hit Safe Mode. Its output is byte-identical to the in-editor `Run Qtn CodeGen`.
  - Session config: 64 Hz, rollback window 64, checksum interval 64. Physics block copied from Friendslop (6 solver iterations, warm start, sleeping on, speculative callbacks).
  - Simulation:
    - `Input` (Move, Camera, Swing, HasCamera; Jump, Crouch, Run, Grab, Throw).
    - `Player` + `PlayerLook` + `PlayerSystem`/`PlayerLookSystem`: spawn at spawn point N, free-for-all team = player index.
    - `Team`.
    - `GroundedTracker` (ported as-is minus the ragdoll check).
    - `Movement`: Friendslop's minus Stamina/Attack/Launched; camera-relative; turns toward its move direction at 720°/s.
    - Systems are registered in `SystemSetup.User.cs`.
  - View:
    - `BlockSauceControls` (bindings in code).
    - `PlayerInputPoller`.
    - `OrbitCamera`: click locks the cursor, Esc frees it.
    - `LocalPlayerView`.
  - `PH_PlayerCapsule` prefab plus `PlayerMovementConfig`/`PlayerGroundedTrackerConfig`.
  - Scenes:
    - `Lobby`: 40 m floor, ramp, platform, 3 steps, walls.
    - `Arena_Graybox`: 60 m floor, 10°/20°/35° ramps, a stepped hill with side slopes, stairs at 0.2–0.8 m, rotated-cube bumps, walls.
    - Each has 8 spawn points, a start UI for online play, and a local debug runner (it stands down while the start UI is present).
  - Hygiene:
    - Company name "Jackson Hill"; template `QuantumGameScene` and its map removed.
    - The SDK start-UI prefab restored to stock: Friendslop had baked in its own connection component (with a `-RuhanDev` app-version postfix) and a nested settings panel that wasn't copied.
  - Verified online (one client, Photon cloud, Lobby):
    - The character spawns at spawn point 0 on team 0, grounded.
    - Holding W+Shift (simulated through the Input System) drove it along the camera's forward to the north wall, stopping at the wall's face minus the capsule radius; on release it braked to zero.
    - Camera follows. Console clean.
- Decided:
  - Capsule radius 0.35 m until Guy's FBX gives a real width.
  - Mass 1, drag 0.5, rotation frozen on X/Z, as Friendslop.
  - Spawn height offset 1 m.
  - Default bindings are placeholders: LMB/RB = grab, RMB/RT = throw, Shift/L3 = run, Ctrl/B = crouch, Space/A = jump.
  - The Swing input is sent as zero until holding exists (Phase 1).
  - Each scene's start-UI connection uses the per-scene app-version postfix the SDK sets, so Lobby and Arena rooms don't mix.
- Assets: none delivered. A-003 notes updated with the capsule placeholder size.
- Blocked / questions for Jackson: approve adding `com.unity.multiplayer.playmode@2.0.2` for the two-client done-when check. The Guy import test waits for his first animated FBX (that task stays open; it doesn't block the done-when, which only needs capsules sized to Guy).
- Next: add Multiplayer Play Mode, run 2 clients in one room, verify both move, then write the Phase 0 report.

## 2026-10-28: Phase 0, plan and setup

### Phase 0 plan

**Tasks, in build order**

1. **Tooling (done this session).** Unity CLI 1.0.0-beta.14 installed; `com.unity.pipeline` 0.9.1-exp.1 added. The CLI lists the open scene's hierarchy and reads the console, which is clean.
2. **Quantum 3.1 upgrade.** Back up `PhotonServerSettings.asset` outside `Assets`. With the Editor closed, replace `Assets/Photon/Quantum`, `PhotonLibs` and `PhotonRealtime` (plus their `.meta` files) with the Friendslop copies (3.1.0 Preview build 2189). Leave out PhotonVoice, PhotonChat and QuantumDemoInput. Reopen Unity, follow the 3.1 migration notes in the CHANGELOG, regenerate Quantum code, then confirm the AppId is intact and the console is clean.
3. **Git.** `git init`, `git lfs install`, use the provided `.gitignore`/`.gitattributes`, and make the first commit once the upgrade compiles.
4. **Hygiene.** Delete `TutorialInfo`, `Readme.asset` and `SampleScene`. Turn Run In Background on. Set the company name (waiting on Jackson). Install Multiplayer Play Mode, which needs Jackson's OK because it's a new package.
5. **Friendslop study.** Notes go below in this file.
6. **Input scheme.** Write the Quantum `Input` struct and a Unity Input System action map. Move; camera vs. swing on the right stick; buttons for jump, crouch, grab, throw and run.
7. **Scenes.** `Lobby` and `Arena_Graybox`, each with a Quantum map and uneven test terrain made from primitive slopes and steps (`PH_` materials).
8. **Player capsule.** A player entity with a `PhysicsBody3D` capsule sized to Guy, spawned per joining player, with minimal force-driven movement. Full movement comes in Phase 1.
9. **Guy import test.** Needs the FBX export (A-003/A-004) and the scale decision. Measure height, width and the `prop` offset, then resize the capsule to match.
10. **Done-when check.** Two clients, through Multiplayer Play Mode, join one room and move capsules sized to Guy. Console clean.

**Risks**

- **3.1 Preview API drift.** 3.0 → 3.1 changes commands, `SessionRunner.Arguments` and the sim rate. The project has no gameplay code yet, so the risk is mostly the generated code and the default config assets. Mitigation: regenerate code and recreate default configs if they fail to load.
- **Swapping the SDK with the Editor open** could leave a half-imported SDK, so the Editor is closed for the copy.
- **Guy's measurements block the final capsule size.** Until the export arrives, the capsule uses the proposed scale (1.66 m tall).
- **The Multiplayer Play Mode version** must match Unity 6000.3. Take the version the Package Manager offers for this editor.

### Friendslop study (Quantum 3.1.0 build 2189)

Paths are relative to `quantum-friendslop-3.1.0/Assets/`. Config numbers are the shipped asset values in `QuantumUser/Resources/Configs/`, not the C# defaults.

**Baseline settings**

- **Session:** 64 Hz, rollback window 64.
- **Physics:** gravity −10; solver type 1 (`ProjectedGaussSeidel`) with 6 velocity and 2 position iterations; penetration allowance 0.01 and correction 0.9; sleeping on (0.4 s); CCD off.
- **Player capsule** (`Prefabs/Characters/CharacterBase.prefab`): radius 0.5, height 2, origin at the feet, mass 1, rotation frozen on X and Z, collision callbacks on.
- **System order:** Player → PlayerLook → Stamina → GroundedTracker → Movement → GrabbableCollision → … → Fall → Grab → GrabJointCleanup → Ragdoll.

| System | What it does | Verdict for Block Sauce |
|---|---|---|
| **Player** (`Player.qtn`, `PlayerSystem`, `PlayerLookSystem`) | Copies the tick's input onto the entity. Spawns `RuntimePlayer.PlayerAvatar` at spawn point N. `PlayerLook` keeps absolute yaw and pitch. Keeps the last look when `HasLook` is false. | **Adapt.** Add `TeamId` and a `ControlState` enum (InControl/Staggered/Ragdoll); yaw becomes the orbit-camera yaw. |
| **Input** (`Input.qtn`, `Scripts/Input/PlayerInput.cs`, `LookInput.cs`) | Buttons `Jump`, `Sprint`, `Kick`, `Grab`, `Use`; `bool HasLook`; `InputDirectionMagnitude Move`; `InputPitchYaw Look`. Look is sent as **absolute** angles, so lost or repeated inputs don't drift. Polled via `CallbackPollInput` with `DeterministicInputFlags.Repeatable`. | **Adapt.** New struct below. |
| **Movement** (`MovementSystem`, `MovementConfig`) | Force toward a target velocity (accel 5, decel 5, walk 3, run 6), clamped only while there's input, so knockback survives. Jump impulse 5; fall gravity ×2.5. Holds on slopes. Step-up by raycast-driven up-velocity (max step 1). Snaps rotation to look yaw. Requires `Stamina` in its filter. | **Adapt.** Face the move direction (or the camera while holding). Drop Stamina. Add crouch, plus slide (run+crouch: impulse, low decel, timer). Gate on `ControlState`. |
| **GroundedTracker** | Grounded from contact normals (y ≥ 0.5) with 3 grace ticks, not raycasts. Banks a safe respawn position from static-only probes. | **Port as-is.** Standing on dynamic blocks works; towers aren't banked as respawn points, which is what we want. |
| **Grab** (`GrabSystem`, `GrabHelper`, `GrabConfig`) | Aim ray from the eye. The held body gets gravity 0 and no sleep, and a `DistanceJoint3D` leash to the shoulder. Each tick its velocity = carrier velocity + (hand − pos) × gain 20, capped at 50. The target is swing-limited (1080°/s around the shoulder). The carrier is slowed by 0.1 × mass. Auto-drops past 2.2 m or when a static blocks the line. **There is no throw:** grab toggles drop, and the kick pushes a held object along the aim. | **Adapt; most valuable code.** Replace the pitch-driven hand anchor with a swing angle integrated from `Input.Swing` (hand = shoulder + rotation(swing) × radius), anchored at Guy's `prop` offset. Keep chase, swing limit, leash and reaction. Add Throw (impulse along the camera aim + carried velocity), Jumpshot (airborne variant) and Place. Grab aims from the orbit camera ray or a forward cone. |
| **GrabbableCollision / GrabJointCleanup** | Ignore collisions between a carrier and the held object (including ragdoll limbs); remove the leash joint when released. | **Port as-is.** |
| **ObjectImpact** | `ObjectImpact` event on verified frames when a free object hits at ≥ 2 m/s. | **Port as-is** (audio/VFX hook). Phase 2's unlock check uses its own impulse path in the simulation. |
| **Attack** | Kick: lunge, sphere overlap at 0.25 s, push 10 with up-bias, ragdoll ×2, cooldown 0.6. | **Skip the kick.** Reuse its timer/overlap/push pattern for knockback (Phase 1) and the push-held-object path for Throw. |
| **Ragdoll** (`RagdollSystem`, `ImpactRagdollSystem`, `Ragdoll.User.cs`) | `SetRagdoll` disables the capsule and spawns an entity-prototype group baked by `QuantumRagdoll` (10 `CharacterJoint` joints; bones assigned manually, so Generic rigs work). Limbs are grabbable. Duration 8 s; jump "struggle" cuts 1 s. Impact ragdoll fires at **2 m/s** (asset value). A held object hitting someone at holder speed ≥ 6 disarms them instead. | **Adapt (Phase 3).** Add a Staggered tier below the knockdown threshold; raise the thresholds a lot (2 m/s would floor everyone hit by any block); shorter duration; team filter in a rule hook. |
| **Fall** | Below Y −15: characters return to the last grounded position; props are destroyed. | **Adapt.** Drop the enemy branch; blocks respawn instead of being destroyed; route through a game-mode hook. |
| **Stamina / JumpPad / Launched** | Sprint budget; launch pads. | **Skip.** The "marker component excludes Movement" pattern from `Launched` is reused for Staggered. |

Also worth copying: the `frame.GetConfig<T>(entity)` helper in `Frame.User.cs`.

**Block Sauce input struct (Phase 0)**

- `InputDirectionMagnitude Move`: left stick or WASD, relative to the camera yaw; left/right always strafes.
- `InputPitchYaw Camera` + `bool HasCamera`: absolute orbit yaw and pitch, the same no-drift scheme as Friendslop. Movement, aim and throw direction come from it.
- `FPVector2 Swing`: the raw right stick (or mouse delta) while holding a block. The view freezes the orbit camera locally while the sim drives the swing from this value.
- Buttons: `Jump`, `Crouch`, `Run`, `Grab` (grab/place/drop), `Throw` (with `Jump` airborne = jumpshot).

### Session log

- Did: created `.claude/settings.json`; installed the Unity CLI and the Pipeline package; confirmed the connector works; wrote the Phase 0 plan; did the asset pass; studied Friendslop (notes above); backed up `PhotonServerSettings.asset` to `BlockSauce-Unity/_backup/` (outside the repo); `git init` + `git lfs install` (no commit yet); deleted `TutorialInfo`, `Readme.asset`, `SampleScene`; turned Run In Background on.
- Decided: the Unity CLI and `com.unity.pipeline` count as Editor tooling (Jackson approved the install in chat).
- Assets: A-003/A-004 moved to *Needed soon* with export specifics; added A-011 (movement feel targets) and A-012 (hold-block pose, a long-lead slice of A-004); A-005 now asks for an early draft export for Phase 1.
- Blocked / questions for Jackson: close Unity for the SDK swap; approve adding `com.unity.multiplayer.playmode@2.0.2` (the agent's attempt was held for approval); game scale; which walk action is real; Guy export (Jackson, or a headless export by the agent); company name.
- Next: Quantum 3.1 upgrade, with the Editor closed.

---

## 2026-10-28: Pre-Phase 0 directory review (done in chat, not by the agent)

- **Unity project:** 6000.3.14f1, URP 17.3, Input System only (`activeInputHandler: 1`), product name "Block Sauce", company name still "DefaultCompany". Run In Background is **off**. URP template leftovers present (TutorialInfo, Readme.asset, SampleScene, InputSystem_Actions). No git repository yet.
- **Quantum in project:** 3.0.14 Stable (build 2241). Default `QuantumUser` scaffold and `QuantumGameScene`; no gameplay code yet. AppId set in `PhotonServerSettings.asset`.
- **Friendslop sample:** Unity 6000.3.13f1, Quantum **3.1.0 Preview build 2189**, plus Cinemachine 3.1.7, PhotonVoice, PhotonChat and several CC0 asset packs in `Assets/3rdParty`. Its ragdolls use 3.1-only features (`CharacterJoint3D`, `QuantumRagdoll`), so its code won't port onto 3.0.14. Hence the Phase 0 upgrade task.
- **Friendslop code worth porting:** `Movement/MovementSystem` (force toward target velocity, braking, slope and step-up), `Grab/GrabSystem` (velocity-driven hold point, `DistanceJoint3D` leash, `GrabHelper`), `Ragdoll/*` (`SetRagdoll` signal, `RagdollConfig`: knockdown duration, impact speed 20 m/s, impulse multiplier), `GroundedTracker`, `Fall`.
- **QuantumRagdoll and Generic rigs:** bone slots are plain serialized Transforms; auto-fill uses `Animator.GetBoneTransform` (Humanoid only). Leave the Animator field empty and assign bones manually.
- **Guy (`modelling/guy/guy.blend`, Blender 5.1):** mesh `Sphere` (1,482 verts, subsurf, 1 material), armature with 100 bones / 31 deform, a full control rig (IK/FK, foot roll, stretch, squash with maintain volume, look and eye targets) and a `prop` bone at the hold point. Height 4.15 m at authoring scale. 24 fps. Actions: `Guy.WalkBasic`, `.001`, `.002`; only `.002` has keys (0–96) and it has no fake user. Also in the file: `Cube` (2 m, geometry nodes; likely the block draft), `Circle` (2.46 × 1.18 m; possibly a vat draft), `WGT_*` control-shape widgets.
- **Open for Jackson:** confirm game scale (proposed import factor 0.4, Guy about 1.66 m); confirm which walk action is real.
- **Next:** Phase 0, starting with the Quantum 3.1 upgrade.
