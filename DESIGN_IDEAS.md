# Design Ideas — Leaderboard Game

## Current State Analysis

**What the game is today:** A tap-to-climb leaderboard game. You tap a button, gain points (with combo multiplier), and watch yourself rise through a 30-player leaderboard. SpacetimeDB handles real-time multiplayer sync. Items spawn periodically (Double Points, Auto Tap, Freeze Opponents, Combo Booster, Score Bomb).

### What Feels Good
- **Combo system** — The 1.5s window with escalating multiplier (up to x5) creates a rhythmic urgency. You *feel* the combo building.
- **Rank change juice** — The overtake notifications ("You just passed xXSlayerXx!"), flash overlays, and milestone celebrations (top 10, top 3, #1) give genuine dopamine hits. The screen shake at high combos is satisfying.
- **Bot pressure** — Bots gaining score at random intervals creates real anxiety. You can't just stop tapping.
- **Score popups** — The floating "+points" with punch scale and fade feels snappy and responsive.
- **Items as disruption** — Freeze Opponents is the most interesting item because it's the only one that affects *other players*. That's the seed of something bigger.

### What's Missing
- **Strategic depth** — It's pure APM (actions per minute). No decisions beyond "tap faster."
- **Player interaction** — You can't *do* anything to other players. The leaderboard is a passive scoreboard you're climbing, not a space you're playing in.
- **Session structure** — No rounds, no win condition, no arc. Score just goes up forever.
- **Identity on the board** — Players are just names + numbers. No personality, no threat assessment, no rivalries.

---

## Design Ideas

### 1. 🎯 Rank Zones — The Board Is the Arena (⭐ TOP PICK)

**The leaderboard is divided into colored zones (e.g., Bronze 20-30, Silver 10-19, Gold 5-9, Diamond 1-4) and each zone has different rules.** In Bronze, you just tap. In Silver, items spawn more frequently but can also hurt you. In Gold, you can spend score to "attack" nearby players (knock them down 1-3 ranks). In Diamond, every player can see a shared countdown — when it hits zero, the #1 player wins the round and everyone resets.

This makes rank position *meaningful* beyond vanity. Climbing into Gold fundamentally changes your gameplay. You stop mindlessly tapping and start making decisions: do I spend 200 points to knock the person above me down, or save up? The leaderboard becomes a territory with different biomes.

**Why ship this first:** It layers on top of existing mechanics with no server schema changes needed (zones are client-side rules, attacks are just negative AddScore calls with a new reducer). It gives the game an actual win condition and round structure.

### 2. ⚔️ Sabotage Items — Attack the Board, Not Just Your Score

**New item category: offensive items that target other players on the leaderboard.** "Score Drain" steals 10% of the nearest player above you. "Swap" switches your position with an adjacent player. "Shield" blocks the next incoming attack. "Spotlight" makes a random player's score visible as a countdown — if they don't tap fast enough, they lose points.

Currently, FreezeOpponents is the only item that touches other players, and it's passive. Sabotage items create direct player-vs-player tension. You'd see someone rising fast and think "I need to Shield up" or "time to Drain them before they pass me." The leaderboard stops being parallel play and becomes adversarial.

**Implementation:** New ItemTypes, a `TargetPlayer` reducer on the server that validates and applies effects. The server already tracks all players — targeting is just "find the player at rank N±1."

### 3. 🏰 King of the Hill — #1 Is a Job, Not a Prize

**The player at #1 doesn't just sit there — they enter a special "defense mode" where all other players' taps generate slightly more points, and the #1 player's tap button changes to a "Defend" button that costs score to maintain position.** Every second at #1 drains a small amount of score. You're spending to hold the crown. Meanwhile, everyone else gets a visible "DETHRONE" bonus multiplier that increases the longer someone holds #1.

This creates a natural rubber-banding mechanic and a thrilling push-pull at the top. Being #1 is exciting but *costly*. It encourages players to strategically time their push for the top rather than just grinding. It also gives lower-ranked players hope — the king is always bleeding.

**Implementation:** Server-side timer on the #1 player (reduce score by N per tick). Client-side UI changes for the #1 player. A broadcast "dethrone multiplier" value synced to all clients.

### 4. 🤝 Alliances — Temporary Pacts on the Board

**Players can tap on nearby players' names on the leaderboard to propose a 30-second "alliance."** Allied players share a score multiplier (1.5x) but their scores are averaged every 5 seconds — so the higher player pulls the lower one up, but also gets dragged down slightly. Alliances auto-break if one player attacks the other or if they drift more than 5 ranks apart.

This adds a social/strategic layer: do you ally with someone just below you to boost your multiplier, knowing they'll close the gap? Do you ally with someone above you to leech their score? The averaging mechanic means alliances are always a calculated gamble, not a free boost.

**Implementation:** New `Alliance` table in SpacetimeDB with two player identities and an expiry timestamp. Modified score calculation that checks for active alliances. UI overlay showing alliance status.

### 5. 🎰 Score Gambling — Risk It for the Biscuit

**A "Wager" button appears periodically. Tap it to bet a percentage of your current score (25%, 50%, or ALL IN) on a 5-second mini-challenge** — e.g., "tap 20 times in 5 seconds" or "don't tap for 5 seconds while your score counts down." Win: double your wager. Lose: lose it. Other players can see when someone is wagering (their row on the leaderboard pulses/glows), creating spectacle and tension.

This adds risk/reward decisions and creates dramatic moments visible to everyone. Seeing someone go ALL IN and either rocket up or crash down is great spectator gameplay. It breaks the monotony of pure grinding and creates stories ("I was #3, went all in, and dropped to #15").

**Implementation:** Client-side mini-game system triggered by a new event. Server reducer for `PlaceWager` that locks score and `ResolveWager` that pays out or deducts. Leaderboard UI gets a "wagering" visual state per entry.

---

## Recommendation

**Ship Rank Zones first.** It's the idea that most directly fulfills the game's thesis — "the leaderboard IS the game" — by making your *position* on the board change how you play. It adds strategic depth, a win condition, and player interaction without requiring major server changes. The zone system also creates a natural framework for layering in the other ideas later (sabotage items become Gold-zone-only; King of the Hill becomes the Diamond-zone endgame; gambling becomes a Silver-zone feature).

The current game is a solid clicker with great juice. What it needs is *reasons to think*, not just tap. Rank Zones give you that.
