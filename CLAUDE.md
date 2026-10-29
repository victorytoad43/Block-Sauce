# Block Sauce: agent playbook

You are building **Block Sauce**, a third-person physics party game, in Unity, phase by phase. Jackson (the owner) makes the art in Blender while you build the game. Your second job is to keep him supplied with clear, prioritized **asset requests**, so art is always being made ahead of when the code needs it.

## Source files (read these first, every session)

| File | What it is | Who edits it |
|---|---|---|
| `docs/PLAN.md` | Game design, architecture, decisions, and the phase plan with done-when criteria. Source of truth. | You (status, checkboxes); Jackson (scope) |
| `docs/ASSETS.md` | Art pipeline spec and the asset request tracker | You add and update requests; Jackson marks deliveries |
| `docs/PROGRESS.md` | Running log: one entry per work session and a report per finished phase. Read the latest entries first. | You |

If code contradicts `docs/PLAN.md`, the plan wins. If the plan seems wrong, ask Jackson; don't silently diverge. Record every design change you agree with him in the plan's Decisions section.

## Where things are

This repo is the Unity project: `C:/Users/victo/Desktop/Games/BlockSauce/BlockSauce-Unity/Block Sauce`. Two sibling folders outside it are references you need to read. If `.claude/settings.json` doesn't exist yet, create it on your first run (Jackson approves the write) with exactly:

```json
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "additionalDirectories": [
      "C:/Users/victo/Desktop/Games/BlockSauce/quantum-friendslop-3.1.0",
      "C:/Users/victo/Desktop/Games/BlockSauce/modelling"
    ]
  }
}
```

| Folder | What it is | Rule |
|---|---|---|
| `C:/Users/victo/Desktop/Games/BlockSauce/quantum-friendslop-3.1.0` | Photon's **Friendslop** sample, a separate Unity project. The reference implementation for grab, carry, throw, force-driven movement, deterministic ragdolls and stacked physics props. | Read-only. Port code into this project and adapt it. Never open it as the working project, and never copy its `Assets/3rdParty` content. |
| `C:/Users/victo/Desktop/Games/BlockSauce/modelling` | Jackson's Blender source files (`guy/guy.blend`: the Guy character and rig). | Read-only. Inspect if you need measurements; never save or edit. Art reaches the game as FBX through `Assets/Art/_Incoming/`. |

Friendslop docs: https://doc.photonengine.com/quantum/v3/game-samples/friendslop/architecture

## Stack

- Unity **6000.3.14f1** with **URP 17.3**. Don't upgrade the editor or any package without asking.
- **Photon Quantum 3.1 Preview** for the deterministic simulation and networking. The project currently has 3.0.14 Stable; the first Phase 0 task upgrades it (see the plan). Once upgraded, pin that build: never update the SDK without asking.
- The Quantum AppId is already set in `Assets/QuantumUser/Resources/PhotonServerSettings.asset`. Preserve it through the SDK upgrade, and never print it in logs or docs.
- Characters use **Generic** rigs (not Humanoid), exported from Blender as FBX.
- Use the Unity connector/CLI to create and inspect scenes, GameObjects, prefabs and the console, and to enter play mode. Prefer it over hand-editing `.unity` / `.prefab` YAML. Unity is often open while you work; let it finish importing after file changes before you inspect anything.

## Architecture rules (non-negotiable)

1. **Anything that decides who wins lives in the Quantum simulation** (`Assets/QuantumUser/Simulation`). Anything that only changes how it looks lives in the Unity view (`Assets/Scripts`).
2. Simulation code is deterministic: Quantum fixed-point math (`FP`, `FPVector3`) only. No `float`, no `UnityEngine` types, no `System.Random`, no wall-clock time. Use Quantum's RNG and frame timers.
3. The view only reads simulation state and reacts to simulation events. It never writes back into the simulation.
4. Input is the only thing that crosses the network. New gameplay input means changing the Quantum input definition.
5. Every player and block carries a **team ID** from day one (free-for-all = teams of one).
6. Game modes are config assets plus rule hooks (block placed, locked, unlocked, win check). Don't hardcode Stack & Lock rules into core systems.

## Known gotchas

- **Quantum 3.1 API differences from 3.0:** commands go through `QuantumGame.AddCommand` and are read with `frame.GetPlayerCommands<T>`; `SessionRunner.Arguments` needs an explicit `TaskRunner`; the default sim rate is 64 Hz. Follow the 3.1 migration guide and the 3.1 changelog in `Assets/Photon/Quantum/CHANGELOG.md`, not 3.0 docs.
- **Ragdolls with a Generic rig:** `QuantumRagdoll` can auto-fill bones only from a Humanoid `Animator`. Leave its `_animator` field **empty** and assign the bone Transforms manually; if the field is set, `BuildRagdoll` overwrites the slots with nulls.
- **Friendslop is first-person;** Block Sauce is third-person with an orbit camera, and the right stick swings a held block. Port systems, not its camera or input layout.
- **Art scale:** Jackson's Blender art is about 2.5× real-world scale. Import every FBX from him with the scale factor in `docs/ASSETS.md` so physics runs at real-world scale.

## How to work through a phase

1. **Start of phase:** read the phase in `docs/PLAN.md`. Write a short plan for it in `docs/PROGRESS.md`: tasks, risks, build order.
2. **Asset pass, before writing code:** list every art, UI, audio or design-input asset the phase needs or would benefit from. Add each to `docs/ASSETS.md` in the request format there, and update existing requests with anything you've learned. Also add **look-ahead** requests for the next phase and any long-lead items (animation takes Jackson the longest). Then tell Jackson in one short message which requests matter most and when they're needed.
3. **Build with placeholders. Never block on art.** Use Unity primitives, gray materials and default UI, all prefixed `PH_` and named in the request's *Placeholder* field. Record any dimension, pivot, or timing the art must match in the request, so Jackson builds to it.
4. **Integrate deliveries.** Jackson drops FBX/PNG files in `Assets/Art/_Incoming/` and sets the request to `Delivered`. At the start of every session, check for deliveries: move each file to its final folder, apply the import settings in `docs/ASSETS.md`, swap out the placeholder, test it in play mode, and set the request to `Integrated`. If something doesn't fit the spec, set it to `Needs changes` with a precise note. Don't edit art files yourself.
5. **Commit** after each working milestone, with a message naming the phase and task.
6. **End of phase:** verify every done-when criterion in play mode. For online criteria, run several clients with Unity Multiplayer Play Mode (Quantum supports it). Check the console is clean. Write a phase report in `docs/PROGRESS.md`: what was built, how each done-when was verified, known issues, placeholders still in use. Tick the phase in `docs/PLAN.md`. Then **stop and ask Jackson to review** before starting the next phase.

## Asking Jackson

Ask instead of guessing when a choice is about game feel, art direction, scope, or anything under Open questions in the plan. Don't ask about routine engineering choices: decide, note it in `docs/PROGRESS.md`, and keep going. Batch questions into one message.

## Setup status

- [x] Unity project created with the URP template (6000.3.14f1, URP 17.3)
- [x] Photon Quantum SDK imported (3.0.14 Stable; upgrading to 3.1 is Phase 0 task 1)
- [x] Quantum AppId set in PhotonServerSettings
- [x] Friendslop sample downloaded (3.1.0 Preview build 2189)
- [x] Agent kit placed at the repo root
- [ ] `.claude/settings.json` for the two reference folders: you create it on first run (see "Where things are")
- [ ] Git repository: you initialize it in Phase 0 (`.gitignore` and `.gitattributes` are already here)
- [ ] Unity connector working in this session: confirm you can list the open scene's hierarchy before starting. If you can't, stop and tell Jackson.

## Folder conventions

```
Assets/
  QuantumUser/Simulation/<Feature>/   # deterministic code, one folder per feature (.qtn, systems, configs)
  Scripts/<Feature>/                  # view code, same feature names as the simulation
  Art/_Incoming/                      # Jackson's drop zone; you move files out of here
  Art/Characters/  Art/Blocks/  Art/Environment/  Art/UI/  Art/VFX/  Art/Audio/
  Placeholders/                       # all PH_ assets
  Scenes/                             # Lobby, Arena_Graybox, test scenes
docs/  PLAN.md  ASSETS.md  PROGRESS.md
```
