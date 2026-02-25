# Click Charge System & Power-up Items — Design Spec

## Overview

The Click Charge System adds resource management to tapping. Players have a limited pool of charges that recharge over time. Power-up items modify charges, damage, or both — creating strategic moments and exciting pickups.

---

## 1. Click Charge Mechanics

| Property | Value |
|---|---|
| Max Charges | 10 |
| Recharge Rate | 1 charge/sec (flat) |
| Cost per Tap | 1 charge |
| Tap when empty | Blocked (button visually dims, no score) |
| Display | Charge meter shown on player's leaderboard line + HUD |

### Behavior
- Charges start full (10/10) on game start.
- Each tap consumes exactly 1 charge.
- Charges regenerate at a flat rate of 1/sec regardless of combo or rank.
- When charges hit 0, the TAP button greys out and taps are ignored until at least 1 charge is available.
- The charge meter is displayed as a small bar (or pip row) on every leaderboard entry line, so players can see opponents' charge state.

### Visual
- **HUD**: Large charge bar next to the TAP button area, showing `⚡ 7/10` with fill animation.
- **Leaderboard lines**: Thin horizontal bar under each player's score, colored by fill percentage (green → yellow → red as charges deplete).

---

## 2. Power-up Items

### Item Table

| # | Name | Icon/Emoji | Effect | Duration | Type |
|---|---|---|---|---|---|
| 1 | **Damage Boost** | 💥 | +50% damage (score per tap) | 10s | Buff |
| 2 | **Charge Rush** | ⚡ | 2x recharge rate (2 charges/sec) | 8s | Buff |
| 3 | **Max Capacity** | 🔋 | Max charges increased to 15 | 15s | Buff |
| 4 | **Instant Reload** | 🔄 | Instantly refill all charges to max | Instant | Instant |
| 5 | **Rapid Fire** | 🔥 | Taps cost 0 charges | 6s | Buff |
| 6 | **Overcharge** | ⚡⚡ | Fill to 20 charges (temporary max of 20) | 12s | Buff |
| 7 | **Score Bomb** | 💣 | +500 instant score | Instant | Instant |
| 8 | **Freeze** | ❄️ | Freeze all opponent score gains | 6s | Buff |

### Item Details

**Damage Boost** — Straightforward power spike. Multiplies final score-per-tap by 1.5x. Stacks multiplicatively with combo.

**Charge Rush** — Doubles recharge rate to 2/sec. Lets aggressive tappers sustain longer combos without running dry.

**Max Capacity** — Temporarily raises the charge cap to 15. Current charges stay; new charges can regen up to 15. When it expires, if charges > 10, they clamp back to 10.

**Instant Reload** — Immediately sets charges to current max. Simple, powerful, satisfying.

**Rapid Fire** — Taps consume 0 charges for the duration. Pure aggression mode. Short duration to balance.

**Overcharge** — Sets charges to 20 AND temporarily raises max to 20. When it expires, max returns to normal (10 or whatever current buff says) and charges clamp down.

**Score Bomb** — Carried over from existing system. +500 instant.

**Freeze** — Carried over from existing system. Opponents gain no score.

---

## 3. Item Spawn Mechanics

| Property | Value |
|---|---|
| Spawn interval | Random 8–15 seconds |
| Spawn location | Random position in upper screen area (normalized 0.15–0.85 X, 0.4–0.8 Y) |
| Uncollected lifetime | 5 seconds (blinks in last 30%, then disappears) |
| Pickup method | Click/tap the floating bubble |
| Buff stacking | Same buff refreshes duration (doesn't stack magnitude) |
| Visual on pickup | Full-screen notification banner (emoji + name, 1.5s fade) |

### Spawn Weights
All items have equal spawn weight. Future iteration could weight by game state (e.g., more Reloads when player is low on charges).

---

## 4. Item Pickup Visuals

- **Floating bubble**: Colored circle with emoji icon, bobs up/down, pulses scale.
- **On collect**: Brief full-width notification banner showing `⚡ CHARGE RUSH!` with item color.
- **Active indicator**: Horizontal bar below header showing active buffs with countdown timers.
- **Charge meter flash**: When Instant Reload or Overcharge triggers, the charge bar does a bright flash animation.

---

## 5. Implementation Plan

### Files Modified
- `PlayerController.cs` — Check charges before tap, consume 1 charge per tap
- `LeaderboardUIRuntime.cs` — Add charge meter bars to leaderboard entries
- `ItemDefinition.cs` — Add new ItemType enum values and definitions
- `ItemSystem.cs` — Handle new item effects (charge modifiers)
- `SceneBuilder.cs` — Wire up ChargeManager, build charge HUD

### Files Created
- `ChargeManager.cs` — Singleton managing charge pool, recharge, and item modifiers
- `CLICK_CHARGE_SYSTEM.md` — This document

---

*Designed for The Board — a leaderboard game where every tap matters.*
