# Leaderboard Game - Test Report

**Tester:** Lobster 🦞 (AI Agent)  
**Date:** 2026-02-24  
**Build:** LeaderboardGame.exe (Unity 6000.3.9f1)  
**Method:** Automated run with `--screenshot` flag + full code review

---

## ✅ What Works

- **Game launches and runs** — no crashes, clean startup
- **UI renders correctly** — header, leaderboard entries, player bar, tap button all display properly
- **Leaderboard sorting** — ranks update correctly as scores change
- **Combo system** — combo counter increments and resets as expected (1.5s window, max x5)
- **Score calculation** — 20 taps produced exactly 900 points (matching combo math perfectly)
- **Bot AI simulation** — bots gain points over time creating competitive pressure
- **Rank change detection** — log shows "Climbed to #29!", "#28!", "#27!" correctly
- **Visual effects** — tap feedback, score popups, rank-up banners, flash overlays all present in code
- **EventSystem** — properly created at runtime for UI interaction
- **Screenshot mode** — `--screenshot` flag works perfectly for automated testing

---

## 🐛 Bugs Found

### BUG-1: Double Tap Registration (HIGH)
**File:** `SceneBuilder.cs` (WireUpManagers) + `PlayerController.cs` (TryBindButton)

SceneBuilder explicitly adds a click listener:
```csharp
tapButton.onClick.AddListener(player.OnTap);
```
Then sets the private `tapButton` field via reflection. On the next frame, `PlayerController.Update()` calls `TryBindButton()` which adds **another** listener since `buttonBound` is still false (it was set to true only inside TryBindButton, but the first listener was added externally).

**Impact:** Every button click fires `OnTap()` twice, doubling score gain for mouse/touch input. Space key input is unaffected.

**Fix:** Either remove the `tapButton.onClick.AddListener(player.OnTap)` line from SceneBuilder (let PlayerController handle it), or set `buttonBound` via reflection too.

---

### BUG-2: Scroll Position Always Resets to Top (MEDIUM)
**File:** `LeaderboardUIRuntime.cs` (RefreshUI, line ~120)

```csharp
scrollRect.verticalNormalizedPosition = 1f;
```

This runs every 0.5s (on every leaderboard update). The player can never scroll down to see their own entry or lower-ranked players — the scroll snaps back to top constantly.

**Impact:** With 31 entries and only ~10 visible, players can't see entries below rank ~10. The player bar at the bottom shows rank/score, but you can't see your actual row in the leaderboard.

**Fix:** Only force scroll on initial load, or auto-scroll to keep the player's entry visible instead.

---

### BUG-3: Performance - Full UI Rebuild Every 0.5s (MEDIUM)
**File:** `LeaderboardUIRuntime.cs` (RefreshUI)

Every leaderboard update (every 0.5s) destroys ALL entry GameObjects with `DestroyImmediate()` and recreates them from scratch. With 31 entries, that's 62 object lifecycle operations per second.

**Impact:** Unnecessary GC pressure and potential frame hitches, especially on mobile. `DestroyImmediate` is also not recommended during gameplay (Unity docs suggest `Destroy` instead).

**Fix:** Use object pooling — reuse existing entries and just update text/colors.

---

### BUG-4: Score Tie-Breaking Causes Rank Flickering (LOW)
**File:** `LeaderboardEntry.cs` (CompareTo)

When two players have identical scores, `CompareTo` returns 0. Since C#'s sort is unstable, their relative order can change randomly each sort, causing visual flickering in the UI.

**Impact:** Occasional visual glitch where entries at the same score swap positions every 0.5s.

**Fix:** Add secondary sort by PlayerName or PlayerId for stable ordering:
```csharp
int cmp = other.Score.CompareTo(Score);
return cmp != 0 ? cmp : PlayerId.CompareTo(other.PlayerId);
```

---

### BUG-5: RankChangeDetector Events Are Never Wired (LOW)
**File:** `RankChangeDetector.cs`

`OnOvertake`, `OnMilestoneReached`, and `OnOvertakenBy` UnityEvents are declared but never initialized (no `new` in Awake/constructor). Since the component is added at runtime via `AddComponent`, they're always null. The `?.Invoke()` prevents crashes but the events silently do nothing.

**Impact:** The overtake/milestone system is dead code — no "You overtook xXSlayerXx!" moments.

**Fix:** Initialize events in Awake or when declared.

---

### BUG-6: Entrance Animation Never Plays (LOW)
**File:** `LeaderboardAnimator.cs`

`PlayEntranceAnimation()` exists but is never called by anyone. The staggered slide-in effect for entries on first load doesn't happen.

**Impact:** Missing visual polish on game start.

---

## 📋 Design Notes (Not Bugs)

- **Balancing:** Bots start at 50-5000 score. With 30% chance per bot per 0.5s gaining 1-15 points, bots collectively gain ~67 points/second. A player tapping at max combo (50 pts/tap) needs to tap about 1.3x/second just to keep pace. Feels challenging but fair.
- **No end state:** Game runs forever. Could use a win condition (reach #1) or time limit.
- **Player entry not highlighted in scroll list:** The player's row in the leaderboard has `playerEntryColor` background but the auto-scroll-to-top prevents seeing it when ranked low.

---

## Verdict

**Game is functional and playable** but has one significant gameplay bug (double-tap on button clicks) and several quality issues. Recommend fixing BUG-1 and BUG-2 before shipping, as they directly affect gameplay. Moving card to **Testing**.
