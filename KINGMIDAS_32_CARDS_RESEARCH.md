# KingMidas 32 Cards Research

## Scope
This document summarizes the public rules and key parameters for the KingMidas “32 Cards” game and maps them to the current Unity project’s UI and scene flow to guide a clone implementation.

## Game Summary (Public Sources)
- KingMidas describes “32 Cards” as an Andar Bahar–inspired game where players bet on four options (8, 9, 10, 11) using cards from 6 to 13, aiming for the highest total; the table may include lightning bonuses to boost payouts. citeturn0search0
- The game uses a 32-card deck with ranks 6 through K; card values are 6–10 = pip value, J = 11, Q = 12, K = 13. citeturn0search1turn0search3
- Each round, a single card is dealt to each of the four betting spots labeled 8, 9, 10, 11. The total for a spot is its label plus the card value; the highest total wins. citeturn0search1turn0search3turn0search4
- Ties are resolved by dealing additional cards to the tied positions until a single winner emerges. citeturn0search1turn0search3turn0search4
- A common payout table (non-official, third-party) lists: spot 8 pays 11:1, spot 9 pays 4.5:1, spot 10 pays 2.2:1, spot 11 pays 1:1. citeturn0search1turn0search6

## Published Parameters (KingMidas)
- RTP: 97.00%
- Volatility: Medium
- Max win: 10x
These parameters are shown on the KingMidas game page and should be treated as the authoritative public specs for that platform. citeturn0search0

## Unity Project Mapping (Current Codebase)
- **Scenes:** The project currently uses `Assets/Scenes/MainScene.unity` as the primary scene.
- **Runtime flow:** `SocketIOManager` (in `Assets/Scripts/APIs`) connects to the backend, receives `initData`/`ResultData`, and exposes `initialData` (bets/multipliers/house edge) and `resultData` (win amount, crash point, win chance).
- **Betting flow:** `GameManager` (in `Assets/Scripts/Functionality`) drives bet selection, calls `SocketIOManager.AccumulateResult`, waits for `resultData`, updates balance, and animates the “crash point” multiplier and car movement.
- **UI:** `UiManager` (in `Assets/Scripts/UI`) owns menus, popups, coin selector, chip placement on the four bet positions, and undo/cancel/double controls. The four betting targets map to the “8/9/10/11” positions implied by the game rules.
- **Cards & dealer:** `DealerController`, `CardController`, and `ImageAnimation` coordinate shuffle/deal/reset animation frames and spawn card prefabs that land on the 8/9/10/11 positions.
- **Platform bridge:** WebGL/React Native hooks exist in `JSFunctCalls` and `JSHandler` for auth token retrieval and messaging.

## Gap Analysis for a Faithful Clone
- **Payout logic:** The public KingMidas page does not list a payout table; the project should confirm the intended paytable (or use backend-driven payouts) before hardcoding values. citeturn0search0turn0search1
- **Lightning bonuses:** The KingMidas page mentions lightning bonuses but the current Unity scripts do not show explicit bonus multipliers or visuals; these may need to be added to match the KingMidas presentation. citeturn0search0
- **Rules vs. current “crash point” UI:** The existing UI animates a “crash point” multiplier and car animation, which aligns more with a crash-style mechanic; if the target is a pure 32 Cards clone, the win display should be based on the highest total of four positions rather than a crash point. citeturn0search1turn0search3

## Recommended Next Steps
1. Confirm official KingMidas payout table and bonus mechanics (lightning/boosts) before mirroring in code. citeturn0search0
2. Decide whether the backend or Unity should calculate outcomes; the current code expects the backend to return `resultData` and uses it to drive UI/animations.
3. Update UI to emphasize four betting positions and a “highest total wins” reveal flow rather than a crash multiplier, if strict cloning is desired. citeturn0search1turn0search3
