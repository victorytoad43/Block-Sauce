# Block Sauce: agent playbook

You are building **Block Sauce**, a third-person online physics party game, in Unity, phase by phase. Jackson (the owner) makes the art in Blender while you build the game. Your second job is keeping `docs/ASSETS.md` an accurate, priority-ordered list of what he should make next, so art is always ahead of the code that needs it.

## Source files (read these first, every session)

| File | What it is | Who edits it |
|---|---|---|
| `docs/PLAN.md` | Design, architecture, decisions, phases with done-when criteria, art pipeline. Source of truth. | You (status, checkboxes, decisions); Jackson (scope) |
| `docs/ASSETS.md` | One list of every asset still needed, in the order to make them | You |
| `docs/PROGRESS.md` | Current state, known issues, the next phase's plan, and a session log | You |

If code contradicts `docs/PLAN.md`, the plan wins. If the plan seems wrong, ask Jackson; don't silently diverge. Record every design change you agree with him in the plan's Decisions section, in its current form (replace, don't append history). Keep the docs about the current build: git holds the past.

## Where things are

This repo is the Unity project: `C:/Users/victo/Desktop/Games/BlockSauce/BlockSauce-Unity/Block Sauce`. `.claude/settings.json` grants read access to two sibling folders:

| Folder | What it is | Rule |
|---|---|---|
| `C:/Users/victo/Desktop/Games/BlockSauce/quantum-friendslop-3.1.0` | Photon's **Friendslop** sample: the reference for grab, carry, throw, force-driven movement, ragdolls and stacked props. | Read-only. Port code and adapt it. Never open it as the working project, never copy its `Assets/3rdParty`. |
| `C:/Users/victo/Desktop/Games/BlockSauce/modelling` | Jackson's Blender sources (`guy/guy.blend`). | Read-only. Inspect for measurements; never save or edit. Art enters the game only as exports through `Assets/Art/_Incoming/`. |

Friendslop docs: https://doc.photonengine.com/quantum/v3/game-samples/friendslop/architecture

## Stack

- Unity **6000.3.14f1**, URP **17.3**. Don't upgrade the editor or any package without asking.
- **Photon Quantum 3.1.0 Preview build 2189**, pinned. Never update the SDK without asking.
- The Quantum AppId is in `Assets/QuantumUser/Resources/PhotonServerSettings.asset`. Never print it in logs or docs.
- Characters use **Generic** rigs exported from Blender as FBX.
- Unity is usually open while you work. Drive it with the Unity CLI (`unity status`, `unity recompile`, `unity cmd ...`) rather than hand-editing `.unity` / `.prefab` YAML. Batch mode can't open the project while the editor has it open.

## Architecture rules (non-negotiable)

1. **Anything that decides who wins lives in the Quantum simulation** (`Assets/QuantumUser/Simulation`). Anything that only changes how it looks lives in the Unity view (`Assets/Scripts`).
2. Simulation code is deterministic: Quantum fixed-point math (`FP`, `FPVector3`) only. No `float`, no `UnityEngine` types, no `System.Random`, no wall-clock time. Use Quantum's RNG and frame timers.
3. The view only reads simulation state and reacts to simulation events. It never writes back into the simulation.
4. Input is the only thing that crosses the network. New gameplay input means changing `Input.qtn`.
5. Every player and block carries a **team ID** (free-for-all = teams of one).
6. Game modes are config assets plus rule hooks. Don't hardcode Stack & Lock rules into core systems.

## Known gotchas

- **Quantum 3.1 API:** commands go through `QuantumGame.AddCommand` and are read with `frame.GetPlayerCommands<T>`; `SessionRunner.Arguments` needs an explicit `TaskRunner`; the sim runs at 64 Hz. Use the 3.1 changelog in `Assets/Photon/Quantum/CHANGELOG.md`, not 3.0 docs.
- **SDK patch:** `[System.Serializable]` is added to `QuantumStartUIMppmConnectCommand` and `QuantumStartUIMppmDisconnectCommand` in `Assets/Photon/Quantum/Runtime/StartUI/QuantumStartUI.Common.cs` (marked "Block Sauce SDK patch"). Re-apply it after any SDK update, or MPPM logs a warning.
- **Ragdolls with a Generic rig:** leave `QuantumRagdoll`'s `_animator` field **empty** and assign bone Transforms by hand; if it's set, `BuildRagdoll` overwrites the slots with nulls. **Block Sauce › Build Guy Ragdoll** does this for Guy.
- **Friendslop is first-person;** Block Sauce is third-person with an orbit camera, and look input swings a held block. Port systems, not its camera or input layout.
- **Art scale:** Jackson's art is about 2.5× real-world. `ArtImportPostprocessor` imports everything under `Assets/Art/` at 0.4.
- **Assets made by script** must be force-reimported and Quantum's asset database refreshed before the simulation sees them.
- **Automated tests:** an unfocused editor drops synthetic keyboard and mouse input. Drive tests through `PlayerInputPoller.TestInputOverride` or a virtual gamepad.
- Don't save the Lobby scene after a play session.

## Testing

- Multiple clients: Unity Multiplayer Play Mode (installed), plus development builds launched with `-autojoin ROOM` and `-autoplay` (random bot input) for up to 8 players.
- Dev tools (editor and development builds only): F9 stacks your next block onto a tower (`CommandDevStackTower`), F10 knocks you down (`CommandDevKnockdown`), `CommandDevBuildColumn` builds a tall column for stacking tests.
- Measure from the verified frame, not just the predicted one, when checking anything that could be rolled back.
- In-editor checks: `unity cmd script eval_file --file <cs>` runs C# in the open editor (fully qualify `UnityEditor.Animations` types; `using` lines don't compile). For timed input, set `PlayerInputPoller.TestInputOverride` to a lambda that scripts input by elapsed time and logs samples to `AppDomain.CurrentDomain.SetData`, then read the log back in a second eval. Clear the override afterwards.
- `unity cmd capture game_view --save_path Temp/x.png` actually writes `Assets/Temp/x.png`: move the image out and delete `Assets/Temp` and its `.meta`.

## How to work through a phase

1. **Start of phase:** read the phase in `docs/PLAN.md`; update the phase plan in `docs/PROGRESS.md` (tasks, risks, build order).
2. **Asset pass, before writing code:** make sure `docs/ASSETS.md` lists everything this phase and the next need, in priority order, with the sizes, timings and code assumptions the art must match. Long-lead items (animation) go near the top. Tell Jackson in one short message which items matter most.
3. **Build with placeholders. Never block on art.** Unity primitives, gray materials and default UI, all prefixed `PH_`. Record any dimension, pivot or timing the art must match in its `ASSETS.md` item.
4. **Integrate deliveries.** At the start of every session, check `Assets/Art/_Incoming/`. For each file: rename and move it per the pipeline in `docs/PLAN.md` (copy re-exports over the existing asset to keep its GUID), swap out the placeholder, test it in play mode, then remove the item from `ASSETS.md`, or leave it with a precise note of what still needs changing. Don't edit art files yourself.
5. **Commit** after each working milestone, with a message naming the phase and task. Jackson pushes; never push.
6. **End of phase:** verify every done-when criterion in play mode (several clients for online criteria), check the console is clean, write a phase report in `docs/PROGRESS.md`, update its Current state, and tick the phase in `docs/PLAN.md`. Then **stop and ask Jackson to review** before starting the next phase.

## Asking Jackson

Ask instead of guessing when a choice is about game feel, art direction, scope, or anything under Open questions in the plan. Don't ask about routine engineering choices: decide, note it in the session log, and keep going. Batch questions into one message.

## Folder conventions

```
Assets/
  QuantumUser/Simulation/<Feature>/   # deterministic code, one folder per feature (.qtn, systems, configs)
  QuantumUser/Resources/Configs/      # config assets (PlayerGrabConfig, PlayerRagdollConfig, StackAndLockMode, ...)
  Scripts/<Feature>/                  # view code, same feature names as the simulation
  Art/_Incoming/                      # Jackson's drop zone; you move files out of here
  Art/Characters/  Art/Blocks/  Art/Environment/  Art/UI/  Art/VFX/  Art/Audio/
  Placeholders/                       # all PH_ assets
  Scenes/                             # Lobby (matches run here), Arena_Graybox, Tests/
docs/  PLAN.md  ASSETS.md  PROGRESS.md
```
