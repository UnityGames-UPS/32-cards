# Repository Guidelines

## Project Structure & Module Organization
- `Assets/` holds all game content: scenes (`Assets/Scenes`), scripts (`Assets/Scripts`), prefabs (`Assets/Prefabs`), animations (`Assets/Animations`), audio (`Assets/audio`), plugins (`Assets/Plugins`), and editor tooling (`Assets/Editor`).
- `Packages/` contains Unity package dependencies and manifests.
- `ProjectSettings/` defines Unity project configuration (quality, input, build settings).
- `UserSettings/`, `Library/`, and `Temp/` are local/Unity-generated; avoid editing or committing unless explicitly required.
- The main scene appears to be `Assets/Scenes/MainScene.unity`.

## Architecture Overview
- Runtime flow centers on `SocketIOManager` (`Assets/Scripts/APIs`) which connects to the realtime backend, receives `initData`/`ResultData`, and exposes `initialData`, `playerdata`, and `resultData`.
- `GameManager` (`Assets/Scripts/Functionality`) owns the betting lifecycle: updates bet/multiplier UI, starts a bet, triggers socket requests, animates the “crash point” number, and updates balance/win state.
- `UiManager` (`Assets/Scripts/UI`) drives menus, popups, coin/chip selection, and general screen state; it also triggers audio and exit flows.
- Dealer/card visuals are split across `DealerController` (orchestrates shuffle/deal/reset), `ImageAnimation` (sprite frame animation with keyframe hooks), and `CardController` (instantiates and moves card prefabs).
- Platform bridging for WebGL/React Native is in `JSFunctCalls` and `JSHandler`; keep these isolated from core gameplay logic.
- Audio is managed via `AudioManager`/`AudioController`. Use the existing one referenced by your scene objects.

## Build, Test, and Development Commands
- Open the project in Unity Hub and launch in the correct Unity Editor version.
- Play locally: press the Unity Editor Play button.
- Build: `File -> Build Settings...` in Unity, then select target (e.g., WebGL) and Build.
- Tests: `Window -> General -> Test Runner` in Unity (EditMode/PlayMode). No CLI test script is configured in this repo.

## Coding Style & Naming Conventions
- Language: C# for runtime and editor scripts.
- Indentation: 4 spaces; keep braces on the same line as declarations.
- Naming: classes and methods use `PascalCase`. Locals use `camelCase`.
- Follow existing field naming patterns in `Assets/Scripts` (many serialized fields use `PascalCase` with underscores, e.g., `TBetPlus_Button`). Maintain consistency within a file.
- One public class per `.cs` file; file name should match the main class.

## Testing Guidelines
- Unity Test Framework is included (`com.unity.test-framework`).
- If you add tests, create `Assets/Tests/` with asmdefs for EditMode/PlayMode, and name tests `*Tests.cs`.
- Prefer PlayMode tests for gameplay and UI flows; keep deterministic tests isolated from network dependencies.

## Commit & Pull Request Guidelines
- Recent commits use short, present-tense messages and sometimes a `type:` prefix (e.g., `feat: added betting chips animation`).
- Recommended format: `type: brief summary` where `type` is `feat`, `fix`, `chore`, or `refactor`.
- PRs should include a concise summary, list affected scenes/assets, and add screenshots or short clips for UI/animation changes.

## Asset & Scene Changes
- Keep large asset imports scoped; avoid unrelated asset reimports.
- When editing scenes or prefabs, verify changes in Play Mode and note any required project settings updates.
