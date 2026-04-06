# Dealer Animation Layers & Sprite Export Guide

## ImageAnimation Components (6 total)

| # | Field | Purpose |
|---|---|---|
| 1 | `DealerImageAnim_IA` | Main dealer body (torso/arms) |
| 2 | `BoxAnim_IA` | Card box open/close |
| 3 | `LeftHandAnim_IA` | Left hand (deal state only) |
| 4 | `RightHandAnim_IA` | Right hand (deal state only) |
| 5 | `BothHandAnim_IA` | Both hands together (shuffle & reset states) |
| 6 | `TopDownHandsAnim_IA` | Top-down hands overlay (deal & reset states) |

## Sprite Arrays That Get Swapped (9 total)

| # | Sprite List | Used By | During |
|---|---|---|---|
| 1 | `DealerShuffle_Sprites` | `DealerImageAnim_IA` | Shuffle |
| 2 | `DealerReset_Sprites` | `DealerImageAnim_IA` | Reset |
| 3 | `DealerDeal_Sprites` | `DealerImageAnim_IA` | Deal |
| 4 | `BothhandsShuffle_Sprites` | `BothHandAnim_IA` | Shuffle |
| 5 | `BothHandsReset_Sprites` | `BothHandAnim_IA` | Reset |
| 6 | `TopDownHandsReset_Sprites` | `TopDownHandsAnim_IA` | Reset |
| 7 | `TopDownHandsDeal_Sprites` | `TopDownHandsAnim_IA` | Deal |
| 8 | `BoxOpen_Sprites` | `BoxAnim_IA` | Box open |
| 9 | `BoxClose_Sprites` | `BoxAnim_IA` | Box close |

## Animation States Breakdown

### 1. Shuffle (`ShuffleCardsAnimation`)

Triggered during betting phase (2s delay after betting starts, only if >8s remaining).

| Component | Sprites | Speed | Active |
|---|---|---|---|
| `DealerImageAnim_IA` | `DealerShuffle_Sprites` | 90 | Yes |
| `BothHandAnim_IA` | `BothhandsShuffle_Sprites` | 90 | Yes |
| `LeftHandAnim_IA` | - | - | **No** |
| `RightHandAnim_IA` | - | - | **No** |
| `TopDownHandsAnim_IA` | - | - | **No** |
| `BoxAnim_IA` | triggered mid-shuffle | 20 | Yes |

**Keyframe callbacks** (driven by `ImageAnimation.ShufffleController` on `DealerImageAnim_IA`):
- Frame 2: `BoxOpenAnimation()` -> swaps `BoxAnim_IA` to `BoxOpen_Sprites`
- Frame 3: Both hands layered above box
- Frame 80: `BoxCloseAnimation()` -> swaps `BoxAnim_IA` to `BoxClose_Sprites`
- Frame 95: Both hands layered back to default
- Frame 107: Shuffle ends -> `SwitchToRest()`

**Layers to export for Shuffle:**
1. Dealer body shuffle sprites
2. Both hands shuffle sprites
3. Box open sprites
4. Box close sprites

---

### 2. Reset (`resetCardsAnimation`)

Triggered on cashout (after `CardResetDelayOnCashout` = 9s delay).

| Component | Sprites | Speed | Active |
|---|---|---|---|
| `DealerImageAnim_IA` | `DealerReset_Sprites` | 15 | Yes |
| `BothHandAnim_IA` | `BothHandsReset_Sprites` | 15 | Yes |
| `TopDownHandsAnim_IA` | `TopDownHandsReset_Sprites` | 6.5 | Yes |
| `LeftHandAnim_IA` | - | - | **No** |
| `RightHandAnim_IA` | - | - | **No** |
| `BoxAnim_IA` | - | - | unchanged |

**Keyframe callbacks** (driven by `ImageAnimation.ResetController` on `DealerImageAnim_IA`):
- Frame 4: Both hands layered above box
- Frame 17: Both hands layered back to default
- Frame 19: Reset ends -> `SwitchToRest()`

Also calls `cardManager.FadeOutScoreTexts()` at the start.

**Layers to export for Reset:**
1. Dealer body reset sprites
2. Both hands reset sprites
3. Top-down hands reset sprites

---

### 3. Deal (`PrepareDealAnimationState` + `PlayDealSegment`)

Triggered per card dealt. Cards are queued and played one at a time via `ProcessPendingDeals`.

| Component | Sprites | Speed | Active |
|---|---|---|---|
| `DealerImageAnim_IA` | `DealerDeal_Sprites` | 215 | Yes |
| `TopDownHandsAnim_IA` | `TopDownHandsDeal_Sprites` | 215 | Yes |
| `LeftHandAnim_IA` | **not swapped** (uses inspector sprites) | 215 (via PlaySegment) | Yes |
| `RightHandAnim_IA` | **not swapped** (uses inspector sprites) | 215 (via PlaySegment) | Yes |
| `BothHandAnim_IA` | - | - | **No** |
| `BoxAnim_IA` | - | - | unchanged |

**Per-player frame segments** (configured in `DealSegments` list in inspector):
Each player gets a segment range. All 4 IAs play the same segment range simultaneously.

**Card spawn frames** (when the card prefab appears):
- Player 8: frame 29
- Player 9: frame 91
- Player 10: frame 150
- Player 11: frame 212

**On segment complete:** Left/right hands return to default layer, `SwitchToRest()` is called.

**Layers to export for Deal:**
1. Dealer body deal sprites
2. Top-down hands deal sprites
3. Left hand sprites (already in inspector, not swapped at runtime)
4. Right hand sprites (already in inspector, not swapped at runtime)

---

### 4. Box Open/Close

Not standalone states -- triggered as part of the shuffle animation via keyframe callbacks.

| Animation | Sprites | Speed |
|---|---|---|
| `BoxOpenAnimation()` | `BoxOpen_Sprites` | 20 |
| `BoxCloseAnimation()` | `BoxClose_Sprites` | 20 |

---

### 5. Rest (Idle)

`DealerRest_Object` is a plain **GameObject** (likely just a static Image). It is **not** an `ImageAnimation` component. No `StartAnimation()` is ever called on it.

**How it works:** `SwitchToRest()` simply toggles GameObjects:
- `DealerMoving_Object` -> **off**
- `DealerRest_Object` -> **on**
- All hand objects -> **off**
- `TopDownHandsParent_Object` -> **off**

The rest state is the default idle pose -- just a static sprite, no animation.

**Layers to export for Rest:**
1. Single static dealer rest image (on `DealerRest_Object`)

---

## Summary: Export Checklist

| Layer | Sprite List | States Used In |
|---|---|---|
| Dealer Body - Shuffle | `DealerShuffle_Sprites` | Shuffle |
| Dealer Body - Reset | `DealerReset_Sprites` | Reset |
| Dealer Body - Deal | `DealerDeal_Sprites` | Deal |
| Dealer Body - Rest | Static image on `DealerRest_Object` | Idle |
| Both Hands - Shuffle | `BothhandsShuffle_Sprites` | Shuffle |
| Both Hands - Reset | `BothHandsReset_Sprites` | Reset |
| Top-Down Hands - Reset | `TopDownHandsReset_Sprites` | Reset |
| Top-Down Hands - Deal | `TopDownHandsDeal_Sprites` | Deal |
| Left Hand - Deal | Inspector sprites on `LeftHandAnim_IA` | Deal |
| Right Hand - Deal | Inspector sprites on `RightHandAnim_IA` | Deal |
| Box - Open | `BoxOpen_Sprites` | Shuffle (mid-anim) |
| Box - Close | `BoxClose_Sprites` | Shuffle (mid-anim) |

**Total distinct animation layers: 10 animated + 1 static rest = 11 layers**

Note: `LeftHandAnim_IA` and `RightHandAnim_IA` sprites are never swapped in code -- they keep whatever is assigned in the Unity inspector. They only have one set of sprites (deal).
