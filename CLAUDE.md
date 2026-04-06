# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**God of Wealth - 32 Cards (ML-32C):** A real-time multiplayer card game built in Unity 6000.0.64f1, targeting WebGL embedded in a React Native app. Four players (labeled 8, 9, 10, 11) bet on who wins each round. A player's score = their base value (8/9/10/11) + sum of dealt card values (6–K). Highest score wins. Ties are broken by dealing additional cards to tied players only.

## Build & Test Commands

- **Play locally:** Press Play in the Unity Editor
- **Build (WebGL):** `File → Build Settings → WebGL → Build`
- **Run tests:** `Window → General → Test Runner` (EditMode / PlayMode; no CLI test script exists)
- **Unity version:** 6000.0.64f1 (open via Unity Hub)

## Architecture

### Module Responsibilities

| Script | Role |
|---|---|
| `Assets/Scripts/APIs/SocketIOManager.cs` | All backend communication: Socket.IO connection, auth, emitting player actions, dispatching server events to other components |
| `Assets/Scripts/UI/UIManager.cs` | Page/scene flow (Homepage ↔ GamePage ↔ LoadingPage), popups, menus, balance display |
| `Assets/Scripts/Functionality/DealerController.cs` | Dealer sprite-frame animations (shuffle, deal, reset); queues and sequences card deals |
| `Assets/Scripts/Functionality/CardController.cs` | Card prefab instantiation, flip animations, score tracking per player, win highlight |
| `Assets/Scripts/UI/BettingUI/BetPanelManager.cs` | Chip selection, bet spots for each player, Undo/Cancel/Double actions, opponent chip display |
| `Assets/Scripts/JS/JSFunctCalls.cs` | WebGL ↔ React Native bridge (auth token handshake, console logging) |
| `Assets/Scripts/Functionality/AudioManager.cs` | Music + SFX playback and muting |

### Game State Flow

```
game:init         → UIManager shows HomePage with leaderboards & balance
JOIN_LEVEL        → SocketIOManager emits → UIManager shows LoadingPage
game:round_start  → BetPanelManager opens betting interface
game:betting_timer→ Countdown displayed each second
PLACE_BET         → SocketIOManager emits → server validates → BetPanelManager updates chips
game:card_dealt   → DealerController queues deal → CardController flips & updates scores
game:round_end    → CardController highlights winner
game:cashout      → UIManager updates balance, DealerController resets cards → loop
```

**Mid-deal join:** When a player joins during the dealing phase, `OnJoinDuringDeal()` silently spawns all previously dealt cards first, then animates the current card normally. UIManager tracks a `pendingLevelEntry` state to handle this.

### Key Networking Details

- Socket.IO namespace: `/playground-multiplayer` (Tivadar.Best.SocketIO library)
- **Auth:** In WebGL builds, `JSFunctCalls.SendCustomMessage("authToken")` requests the token from React Native; it arrives via `ReceiveAuthToken()`. In the Editor, a hardcoded inspector token is used.
- **Ping/pong health checks:** 5 missed pongs = disconnect popup shown
- **Client actions emitted:** `PLACE_BET`, `CANCEL_BET`, `DOUBLE_BET`, `REPEAT_BET`, `UNDO_BET`, `JOIN_LEVEL`, `HOME`
- **Server broadcasts received:** `game:init`, `game:round_start`, `game:betting_timer`, `game:bonus`, `game:bet_placed`, `game:card_dealt`, `game:round_end`, `game:cashout`, `game:leaderboard_update`

Full payload schemas are in `ML-32C_UNITY_INTEGRATION.md`.

### Animation System

- `ImageAnimation` component drives sprite-frame animations with keyframe callbacks
- `DealerController` defines frame ranges per player (e.g., Player 8: frames 0–29, Player 11: frames 151–212) and uses a coroutine queue (`ProcessPendingDeals`) to prevent race conditions
- DoTween is used for UI transitions, chip animations, and menu slides

## Scope of Exploration

Only explore and read files under `Assets/Scripts/`. Never open, read, or explore prefabs (`.prefab`), scenes (`.unity`), images, sprites, or any other Unity assets unless the user explicitly asks. All implementation work is code-only — assume the user handles editor wiring.

Additional docs are in `md/` at the project root.

## Coding Style

- C# with 4-space indentation; braces on same line as declaration
- `PascalCase` for classes and methods; `camelCase` for locals
- Serialized fields often use `PascalCase` with underscores (e.g., `TBetPlus_Button`) — match the pattern within the file
- One public class per `.cs` file; filename matches class name

## Commit Format

`type: brief summary` — types: `feat`, `fix`, `chore`, `refactor`

PRs should list affected scenes/assets and include screenshots or clips for UI/animation changes.
