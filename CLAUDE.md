# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.3.22f1** 2D project (URP 2D renderer, Cinemachine 3, new Input System only: `activeInputHandler: 1`). All game code is in `Assets/PopJet/` and builds a prototype called **Pop Jet**. In this portrait (9:16) physics puzzle, the player holds on the balloon, drags to aim a needle and releases. That pokes a hole, and the escaping air pushes the balloon the opposite way. The goal is to reach the goal zone before the air runs out. `Assets/PopJet/README.md` (in Vietnamese) is the setup doc. It has the per-script table, scene setup, the tuning table of starting values, and the smoke-test checklist. Read it before changing gameplay numbers.

`Assets/PopJet/Docs/GDD.md` (Vietnamese) is the full mobile game design. Its §20 records the user's decisions (F2P, age 13+, art 100% AI-generated in a papercraft style, English-only game, "pinch" experiment deferred, tactical leaks kept). Phase 1 of its roadmap (§16.1) is implemented. **All in-game text is English** and lives in `GameText`; code comments, tooltips and docs stay Vietnamese.

Code comments, tooltips and log messages are written in **Vietnamese**. Keep that convention. Every number in `[SerializeField]` defaults is a playtest "starting value", not a spec.

## Commands

There is no CLI build or lint setup. Game code has no `.asmdef` files, so everything compiles into `Assembly-CSharp` / `Assembly-CSharp-Editor`. Unity is installed at `C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe`. Batchmode only works when the Editor is **closed** on this project (it locks `Temp/UnityLockfile`); check before running.

- **Regenerate the sample scene:** menu `Tools > PopJet > Build Sample Scene`. Headless equivalent:
  ```
  Unity.exe -batchmode -quit -projectPath "E:/Unity/my/Test-BB" -executeMethod PopJet.EditorTools.PopJetSceneBuilder.BuildFromCommandLine -logFile -
  ```
- **Add Phase 1 features to the currently open scene** (keeps manual edits, idempotent): `Tools > PopJet > Add Phase 1 Features To Open Scene`. Pop/burst only: `Add Balloon Pop To Open Scene`. Player Settings portrait lock: `Apply Mobile Player Settings`.
- Play-test scene: `Assets/PopJet/Scenes/PopJet_Sample.unity` (build index 0).
- **Play Mode smoke test** (`Assets/PopJet/Tests/PlayMode/`, its own `PopJet.SmokeTests.asmdef`): loads build index 0 and drives aim, vent, leak, patch, `Pop` and auto-restart. The asmdef can't reference `Assembly-CSharp`, so it calls components by name and reflection; renaming those fields/methods breaks it. Run headless:
  ```
  Unity.exe -batchmode -projectPath "E:/Unity/my/Test-BB" -runTests -testPlatform PlayMode -testResults results.xml -logFile -
  ```
- Quick compile check without opening Unity: run Unity's bundled `csc` (`Editor\Data\DotNetSdkRoslyn\csc.dll` via `Editor\Data\NetCoreRuntime\dotnet.exe`) with the references and defines from the generated `Assembly-CSharp*.csproj` files. Drop `UNITY_EDITOR*` defines to also check player-only `#if` branches (Android/iOS haptics).

## Architecture

**`BalloonController` is the hub.** It owns all balloon state, and every other script either calls its public API or subscribes to its C# events. No script talks to another gameplay script directly, except `ButtonSwitch` → `Door`.

- *Jets model:* each vent (player poke) or leak (spike hit) is a `Jet` in a list with a direction, thrust, duration and air cost. In `FixedUpdate`, thrust decays linearly to 0. Air drains at `airCost * 2 * k * dt / duration`, so each jet costs exactly `airCost` in total. Low air scales thrust down (`minThrustWhenLow`), and `transform.localScale` shows the air level.
- *State* (`Ready → Floating/Venting/Aiming → Empty | Goal`) is derived from flags, not stored. The balloon has zero gravity until the first vent (`GameStarted`). After that it uses `playGravityScale`. When empty, it pops if `popWhenEmpty` (sets `rb.simulated = false`) or deflates and falls.
- *Events:* `VentStarted`, `LeakAdded` (hole position + fly direction), `PatchedUp`, `BecameEmpty`, `GoalReached`, `GameStarted`, `Poked(count)`. Hard hazards call `Pop(FailReason)`, which always bursts; `Burst` and `LastFailReason` describe the last death. `GetJets(List<JetInfo>)` exposes running vents/leaks without new events (used by `TrajectoryPreview` and `LeakStreamFX`).
- *Subscribers:* `BalloonFeedback` (audio + particle prefabs, pop burst tinted with the balloon color, `onPopped` UnityEvent → Cinemachine impulse), `LevelManager` (play timer, soft fail → auto `Restart` after a delay or tap-to-retry, stuck detection → `StuckChanged`, win → optional `LoadNext`), `PopJetHud`, `BalloonFace`, `BalloonString`, `LeakStreamFX`, `BalloonHaptics`. `BalloonInput` raises `AimStarted`/`AimEnded(vented)` (used by `TimeFreezeAudio`, `BalloonHaptics`), and `BalloonBounce` raises `Bounced(strength)`.
- *Callers:* `BalloonInput` (`SetAiming`, `TryVent`), `Spike` (`AddLeak`, rate-limited by `leakCooldown`/`maxLeaks` inside the controller), `PatchPickup` (`PatchAll` + `AddAir`), `GoalZone` (`ReachGoal`). `Fan` applies force to the attached `Rigidbody2D` directly.
- *Readers that poll instead of subscribing:* `TrajectoryPreview` (re-integrates the jet model with the same Box2D-style damping, `CircleCast` until the first hit), `AirRing`, `AirBarUI`.

Cross-cutting rules that are easy to break:

- **Aiming freezes the world with `Time.timeScale = 0`** (`BalloonInput.freezeWorldWhileAiming`). Anything that must keep running while aiming has to use unscaled time; `LevelManager.PlayTime` does. `BalloonInput.OnDisable` and `LevelManager.Restart`/`LoadNext` reset `timeScale` to 1, so any new scene-transition or teardown path must do the same.
- **The root transform scale shows the air level.** Visual-only deformation (`BalloonBounce` squash/stretch) must go on the child `Visual` transform, never the root.
- Interactables find the balloon with `GetComponentInParent<BalloonController>()` on the hit collider. Trigger-type interactables (`GoalZone`, `Fan`, `ButtonSwitch`, `PatchPickup`) force `isTrigger = true` in `Awake`. `Spike` works as either a solid collider or a trigger.
- **Aim/UI visuals run on unscaled time** (`AirRing` blink, `BalloonFace`, `PopJetHud` animations, `TimeFreezeAudio`); world things (`BalloonString`, `LeakStreamFX` fades, particles) use scaled time so they freeze while aiming.
- `BalloonFace` is a child of `Visual`, which `BalloonBounce` rotates; the face counter-rotates in `LateUpdate` (`[DefaultExecutionOrder(100)]`). The face parts are built at runtime in `Awake`, so they don't show in edit mode.
- `BalloonInput` has its own `PointerHelper`, which uses the Input System `Pointer.current` (with a legacy-input fallback under `#if`). `Poll()` is per-frame idempotent so `LevelManager` can call it too. Touches that start on a UI raycast target are ignored (`PointerHelper.IsOverUI`). It does not use the `InputSystem_Actions` asset.
- The HUD needs an `EventSystem` with `InputSystemUIInputModule` (never `StandaloneInputModule`). It uses legacy `UI.Text` with the built-in `LegacyRuntime.ttf` because TMP Essentials are not imported.
- Unity 6 API differences are guarded with `#if UNITY_6000_0_OR_NEWER` (`linearDamping` vs `drag`).

**`Editor/PopJetSceneBuilder.cs`** (a `partial` class; Phase 1 setup lives in `Editor/PopJetPhase1Builder.cs`) builds the whole sample scene in code. It starts from `Assets/Scenes/SampleScene.unity` as the template. It procedurally generates the placeholder sprites in `Art/`, the WAV clips in `Audio/`, the particle prefabs in `Prefabs/` and the materials, and puts the scene first in Build Settings. Two consequences:
- Rerunning it **overwrites** `PopJet_Sample.unity` and the generated art, audio and prefab assets, so manual edits to those are lost.
- It wires component references through `SerializedObject` using **field-name strings** (`SetRefs(lm, ("balloon", balloon))`, `SetFloat`, `SetArray`, …). If you rename a `[SerializeField]` field in a runtime script, update the matching string in the builder, or add `[FormerlySerializedAs]` so existing scenes keep their values.

`PopJetDebug` is an `OnGUI` tuning overlay (bottom-left) showing state, air, pokes left, vents/leaks, time and paused status. Its `OnGUI` only compiles in the Editor and Development Builds.
