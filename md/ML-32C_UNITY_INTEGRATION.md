# ML-32C Unity Integration Guide

## Overview
ML-32C is a multiplayer card game where 4 players (Player 8, 9, 10, 11) compete by receiving cards. Each player has a base value (8, 9, 10, 11) and their final score is calculated as: **baseValue + sum(cardValues)**. One player is randomly selected as the bonus player with a multiplier applied to winnings.

## Game Configuration

### Levels
- `casual`
- `novice`
- `expert`
- `high_roller`

### Bet Options
- `player_8`
- `player_9`
- `player_10`
- `player_11`

---

## Client → Server Actions

### 1. JOIN_LEVEL
Join a specific betting level.

**Payload:**
```typescript
{
  level: "casual" | "novice" | "expert" | "high_roller"
}
```

**Response:**
```typescript
{
  success: boolean;
  payload: {
    roomId: string;
    oldRoomId: string;
    playerCount: number;
    level: string;
    leaderboards: {
      richest: Array<{username: string, balance: number, rank: number}>;
      winners: Array<{username: string, totalWins: number, rank: number}>;
    };
    roundState?: {
      roundId: string;
      startedAt: number;
      bettingEndTime: number;
      serverTime: number;
      timeRemaining: number;
      phase: "betting" | "dealing";
      cardsDealt?: number;
    } | null;
  };
}
```

---

### 2. PLACE_BET
Place a bet on a player.

**Payload:**
```typescript
{
  amountIndex: number;        // Index of bet amount from config bets array
  betType: "main_bets";
  betOption: "player_8" | "player_9" | "player_10" | "player_11";
}
```

**Response:**
```typescript
{
  success: boolean;
  payload: {
    username: string;
    betId: string;
    totalBet: number;
    betOption: string;
    amount: number;
    balance: number;
    message?: string;  // Error message if success is false
  };
}
```

**Business Rules:**
- Users can bet on maximum **3 different players** per round (not all 4)
- Each bet option has a max_bet_limit per level
- Betting only allowed during betting phase

---

### 3. CANCEL_BET
Cancel all bets placed in the current round.

**Payload:** None

**Response:**
```typescript
{
  success: boolean;
  payload: {
    message: string;
    amount: number;       // Total refunded amount
    balance: number;
    bets: Array<{
      betId: string;
      betType: string;
      betOption: string;
      amount: number;
    }>;
  };
}
```

---

### 4. DOUBLE_BET
Double all bets placed in the current round.

**Payload:**
```typescript
{
  betId?: string;  // Optional, currently not used (doubles all bets)
}
```

**Response:**
```typescript
{
  success: boolean;
  payload: {
    message: string;
    balance: number;
    bets: Array<{
      betId: string;
      amount: number;      // Original amount
      delta: number;       // Amount added
      betType: string;
      betOption: string;
    }>;
    totalBet: number;      // New total bet amount
  };
}
```

**Business Rules:**
- Respects per-option max_bet_limit
- If limit reached, doubles partially up to the limit

---

### 5. REPEAT_BET
Repeat all bets from the previous round.

**Payload:** None

**Response:**
```typescript
{
  success: boolean;
  payload: {
    bets: Array<{
      betId: string;
      amount: number;
      betOption: string;
      betType: string;
    }>;
    totalBet: number;
    balance: number;
    message?: string;  // Error if no previous bets
  };
}
```

---

### 6. UNDO_BET
Undo the last bet placed.

**Payload:** None

**Response:**
```typescript
{
  success: boolean;
  payload: {
    message: string;
    refundAmount: number;
    balance: number;
    totalBet: number;
    bet: {
      betId: string;
      betType: string;
      betOption: string;
      amount: number;
    };
  };
}
```

---

### 7. HOME
Leave the current level and return to lobby.

**Payload:** None

**Response:**
```typescript
{
  success: boolean;
  payload: {
    message: string;
    roomId: string;
    oldRoomId: string;
    lobby: Record<Level, number>;  // Player counts per level
    balance: number;
  };
}
```

---

### 8. BET_HISTORY
Get paginated bet history.

**Payload:**
```typescript
{
  page?: number;  // Default: 1
}
```

**Response:**
```typescript
{
  success: boolean;
  payload: {
    history: Array<{
      round_id: string;
      bet_amount: number;
      win_amount: number;
      level: string;
      cards_dealt: number;
      match_side: string;
      created_at: Date;
      bets: Array<{
        bet_id: string;
        bet_type: string;
        bet_option: string;
        bet_amount: number;
        win_amount: number;
      }>;
    }>;
    meta: {
      total: number;
      page: number;
      limit: number;
      pages: number;
    };
  };
}
```

---

## Server → Client Broadcasts

### 1. 'game:lobby_count'
Broadcasted when player counts in levels change.

**Payload:**
```typescript
{
  lobby: {
    casual: number;
    novice: number;
    expert: number;
    high_roller: number;
  };
}
```

---

### 2. 'game:round_start'
Broadcasted when a new round begins.

**Payload:**
```typescript
{
  roundId: string;
  startedAt: number;          // Unix timestamp
  bettingEndTime: number;     // Unix timestamp
  serverTime: number;         // Unix timestamp
  playerCount: number;
}
```

---

### 3. 'game:betting_timer'
Periodic sync during betting phase (every 1 second).

**Payload:**
```typescript
{
  roundId: string;
  serverTime: number;         // Unix timestamp
  bettingEndTime: number;     // Unix timestamp
  timeRemaining: number;      // Milliseconds remaining
}
```

---

### 4. 'game:bonus'
Broadcasted after betting closes, announces bonus player.

**Payload:**
```typescript
{
  roundId: string;
  bonusPlayer: 8 | 9 | 10 | 11;
  bonusMultiplier: number;    // e.g., 1.5, 2, 3
}
```

**Timing:** Sent after betting closes, before cards are dealt.

---

### 5. 'game:bet_placed'
Broadcasted when any player places/cancels/doubles a bet.

**Payload:**
```typescript
{
  username: string;
  betId: string;
  betType: string;
  betOption: string;
  amount: number;             // Positive for placed, negative for cancelled
}
```

**Note:** This broadcasts all betting activity to all players in the room.

---

### 6. 'game:card_dealt'
Broadcasted for each card dealt (with cardInterval between cards).

**Payload:**
```typescript
{
  roundId: string;
  bonusPlayer: 8 | 9 | 10 | 11;
  bonusMultiplier: number;    // e.g., 1.5, 2, 3
  card: {
    rank: "6" | "7" | "8" | "9" | "10" | "J" | "Q" | "K";
    suit: "hearts" | "diamonds" | "clubs" | "spades";
  };
  player: 8 | 9 | 10 | 11;    // Which player received the card
  player8Cards: Array<Card>;   // All cards for reconnection
  player9Cards: Array<Card>;
  player10Cards: Array<Card>;
  player11Cards: Array<Card>;
  scores: {
    player_8: number;
    player_9: number;
    player_10: number;
    player_11: number;
  };
  cardsDealt: number;          // Total cards dealt so far
}
```

**Flow:**
- Cards dealt round-robin: Player 8 → 9 → 10 → 11 → 8...
- Continues until there's a clear winner (no ties)
- If tie detected, continues dealing only to tied players

---

### 7. 'game:round_end'
Broadcasted when the round winner is determined.

**Payload:**
```typescript
{
  roundId: string;
  matchSide: "player_8" | "player_9" | "player_10" | "player_11";
  winner: 8 | 9 | 10 | 11;
  scores: {
    player_8: number;
    player_9: number;
    player_10: number;
    player_11: number;
  };
  player8Cards: Array<Card>;
  player9Cards: Array<Card>;
  player10Cards: Array<Card>;
  player11Cards: Array<Card>;
}
```

---

### 8. 'game:cashout'
Broadcasted with payout results and updated leaderboards.

**Payload:**
```typescript
{
  leaderboards: {
    richest: Array<{username: string, balance: number, rank: number}>;
    winners: Array<{username: string, totalWins: number, rank: number}>;
  };
  payouts: Array<{
    win: number;
    balance: number;
    username: string;
    userId: string;
  }>;
}
```

**Note:** Only users who bet in the round appear in payouts array.

---

### 9. 'game:leaderboard_update'
Broadcasted when leaderboards change.

**Payload:**
```typescript
{
  leaderboards: {
    richest: Array<{username: string, balance: number, rank: number}>;
    winners: Array<{username: string, totalWins: number, rank: number}>;
  };
}
```

---

## Event Flow Diagram

### Complete Round Flow
```
1. ROUND_START
   ↓ (during betting period)
2. BETTING_TIMER (every 1 second)
   ↓ (players place bets)
3. BET_PLACED (for each bet)
   ↓ (betting closes)
4. BONUS (announce bonus player)
   ↓ (wait bonusInterval ms)
5. CARD_DEALT (repeated for each card with cardInterval)
   ↓ (continue until winner determined)
6. ROUND_END (announce winner)
   ↓
7. CASHOUT (payouts and leaderboards)
   ↓ (wait cashoutInterval ms)
8. Back to ROUND_START
```

---

## Initial Data Structure

When connecting and joining a level, the client receives:

```typescript
{
  id: string;                 // Game ID
  gameData: {
    betOptions: ["player_8", "player_9", "player_10", "player_11"];
    roundInterval: number;    // Betting duration (ms)
    cardLimit: number;        // Not used (32-card deck fixed)
    statsLimit: number;       // Stats history limit
    bets: {
      casual: number[];
      novice: number[];
      expert: number[];
      high_roller: number[];
    };
    levels: string[];
    wagers: {
      main_bets: {
        player_8: { payout: number[], max_bet_limit: Record<Level, number> };
        player_9: { payout: number[], max_bet_limit: Record<Level, number> };
        player_10: { payout: number[], max_bet_limit: Record<Level, number> };
        player_11: { payout: number[], max_bet_limit: Record<Level, number> };
      };
    };
    lobby: Record<Level, number>;
    leaderboards: {
      richest: Array<{username: string, balance: number, rank: number}>;
      winners: Array<{username: string, totalWins: number, rank: number}>;
    };
    stats: Array<{matchSide: string}>;
    bonusMultipliers: {
      player_8: number;
      player_9: number;
      player_10: number;
      player_11: number;
    };
  };
  player: {
    balance: number;
    username: string;
  };
}
```

---

## Scoring System

### Card Values
- `6` = 6
- `7` = 7
- `8` = 8
- `9` = 9
- `10` = 10
- `J` = 11
- `Q` = 12
- `K` = 13

### Score Calculation
```
Player Score = Base Value + Sum(Card Values)

Example:
- Player 8 receives: [6♥, 10♦, K♠]
- Score = 8 + (6 + 10 + 13) = 37
```

### Winner Determination
- Highest score wins
- If tie, continue dealing to tied players only
- If still tied after all 32 cards dealt, first tied player wins

---

## Payout Calculation

### Base Payout
```
winAmount = betAmount × baseMultiplier
```

### With Bonus Multiplier
If user bet on the bonus player AND that player won:
```
winAmount = betAmount × baseMultiplier × bonusMultiplier
```

**Example:**
- Bet: 100 on Player 8
- Base payout multiplier: 2x
- Player 8 is bonus with 1.5x multiplier
- Player 8 wins
- **Payout: 100 × 2 × 1.5 = 300**

---

## Configuration Intervals

| Interval | Purpose | Typical Value |
|----------|---------|---------------|
| `roundInterval` | Betting duration | 15000ms (15s) |
| `bonusInterval` | Wait after bonus announcement | 2000ms (2s) |
| `cardInterval` | Wait between cards | 1000ms (1s) |
| `cashoutInterval` | Wait before next round | 5000ms (5s) |
| `bettingTimerSyncInterval` | Timer sync frequency | 1000ms (1s) |

---

## Error Handling

Common error responses:

### Betting Closed
```typescript
{ success: false, payload: { message: "Betting closed" } }
```

### Invalid Level
```typescript
{ success: false, payload: { message: "Invalid level or level not selected" } }
```

### Limit Reached
```typescript
{ success: false, payload: { message: "Limit reached" } }
```

### Cannot Bet on 4 Players
```typescript
{ success: false, payload: { message: "Cannot bet on more than 3 players" } }
```

### Insufficient Balance
```typescript
{ success: false, payload: { message: "Insufficient balance" } }
```

---


## Testing Checklist

- [ ] Join each level successfully
- [ ] Place bet on each player option
- [ ] Cancel bets before betting closes
- [ ] Double bets within limits
- [ ] Repeat bets from previous round
- [ ] Undo last bet
- [ ] Try betting on 4 different players (should fail)
- [ ] Try betting after betting closes (should fail)
- [ ] Test reconnection during betting phase
- [ ] Test reconnection during dealing phase
- [ ] Verify bonus multiplier applied correctly
- [ ] Verify tie-breaking logic
- [ ] Check leaderboard updates
- [ ] Verify balance updates after wins
- [ ] Test history pagination
