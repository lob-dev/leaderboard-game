# BRAINSTORM: Ludologist — Self-Referential Leaderboard Mechanics

## 1. Mechanical Loops: The Board IS the Game

### The Display-Action Loop
Your position on the leaderboard isn't just a score—it's your **physical location**. The board is the map. Players higher up can see further but have less cover. Players lower down are in "the noise"—crowded, chaotic, but hidden. Every action you take (attacking, building, trading) shifts your position, which shifts what actions are *available* to you.

**Key insight:** The leaderboard isn't a readout of the game state. It *is* the game state.

### Font Size as Power
Higher-ranked players render in larger font. This is literal: bigger font = bigger hitbox, more screen presence, more abilities. But also: you take up more space, you're harder to hide, and lower-ranked players can *read* you more easily (your stats, your cooldowns, your intentions are all displayed in your oversized entry).

### The Board Reshapes You
Your "character build" is determined by your leaderboard neighbors. Adjacent to an aggressive player? You gain defensive traits. Surrounded by hoarders? You become faster. The board is a constant draft—your identity is relational, not fixed.

---

## 2. Inverse Mechanics: The Curse of Climbing

### Visibility Tax
At rank 50+, nobody cares about you. At rank 10, you have a target painted on you. At rank 1, **every single player can see your actions in real-time**. The top of the board is a fishbowl.

- Ranks 100–50: Fog of war. You're anonymous. Safe. Boring.
- Ranks 50–10: Partial visibility. Some players can track you.
- Ranks 10–2: Fully visible. Your cooldowns, resources, next move—all public.
- Rank 1: Livestreamed. Every player gets a notification when you act. You can be targeted by anyone.

This creates a natural "king of the hill" dynamic where #1 is the hardest position to *hold*, not to reach.

### Gravity
The higher you climb, the stronger the pull downward. This is literal: actions cost more "energy" at higher ranks. A move that costs 1 energy at rank 80 costs 5 energy at rank 5. You have to be more efficient, more strategic, more ruthless—or you slide.

### Crown Burden
The top 3 players must complete "obligations"—visible quests that drain resources. Refuse, and you drop. Complete them, and you stay but weaker. This is the cost of fame: the board demands performance from its stars.

---

## 3. Leaderboard as Resource: Spend Your Rank

### Rank as Currency
You can *spend* rank positions to fuel abilities. Drop 5 ranks to launch a devastating attack. Drop 10 to build a permanent structure. Drop 20 to rewrite one rule of the game for 60 seconds.

**The tension:** climbing is hard and slow. Spending is instant and powerful. Do you hoard position or weaponize it?

### Position Trading
Two players can agree to swap positions. Why would someone ranked #3 trade with #30? Because #30 has resources that are only available in the lower ranks (anonymity, cheap actions, unique "underdog" abilities). This creates a market. Position becomes liquid.

### Rank Loans
Borrow rank from another player. You temporarily jump up; they temporarily drop. You owe them a return + interest (paid in rank). Default on the loan? The board marks you—a visible debt symbol next to your name that anyone can see and exploit.

### The Short Sell
Bet *against* another player. If they drop, you gain. If they rise, you lose. Pure social-financial warfare played out on the leaderboard itself.

---

## 4. Emergent Narratives Through Proximity

### Rivalry Zones
Players within 3 ranks of each other enter a "rivalry." Rivals get bonus rewards for actions against each other and *penalties* for ignoring each other. You can't just peacefully coexist with your neighbors. The board forces conflict at every tier.

### Entourages
A cluster of players who stay near each other for long enough form a visible "bloc" on the board—their entries share a background color, they gain group abilities. But if one member breaks away and climbs, the bloc fractures. Betrayal is structural.

### The Echo
When you overtake someone, they get a brief "vengeance" buff—extra power, but only usable against YOU. Every overtake creates a grudge mechanic. The board remembers, and it tells everyone.

### Name Proximity
Your display name on the board slowly morphs to include fragments of your rivals' names. Stay near someone long enough and the board starts merging your identities. You become defined by your rivalries.

---

## 5. Concrete Playable Ideas

### Idea A: **BOARD**
**Genre:** Async multiplayer strategy (mobile-friendly)
**Pitch:** 100 players. You ARE a line on the leaderboard. The game screen is literally just a leaderboard. Each "turn" (every 10 minutes), you pick one action: Push (attack player above), Guard (defend position), Siphon (steal resources from player below), or Invest (gain resources but risk dropping). Actions resolve simultaneously. Your "score" is just your rank, and your rank is your life.

**Success condition:** Hold rank #1 for 24 consecutive hours.

**Self-referential hook:** The entire UI is the leaderboard. No map, no avatar, no world. Just names, ranks, and the tension between them.

---

### Idea B: **TALL POPPY**
**Genre:** Battle royale meets social deduction
**Pitch:** 50 players start in a shuffled leaderboard. Every 30 seconds, the bottom player is eliminated. You climb by completing micro-tasks (puzzles, reflex challenges). BUT: the top 5 players are visible to everyone and can be "voted down" by any 3 players acting together. The meta-game is staying in the safe middle—high enough to survive elimination, low enough to avoid the mob.

**Success condition:** Be the last player standing.

**Self-referential hook:** The optimal strategy is to be *mediocre on purpose*. The leaderboard punishes excellence.

---

### Idea C: **RANK ECONOMY**
**Genre:** Multiplayer economic sim
**Pitch:** Your rank IS your currency. 200 players on a board. You can spend rank to buy abilities, build alliances, or sabotage others. The twist: the game generates "interest"—every hour, the top 10% gain +1 rank (others are pushed down). This creates inflation at the top and desperation at the bottom. Players can form "banks" (rank-pooling alliances) and issue "rank loans."

**Success condition:** Accumulate and hold rank #1 while having at least 3 active loans out (you must be both powerful AND systemically important).

**Self-referential hook:** The leaderboard becomes a stock ticker. Position is money, money is position, and crashes are spectacular.

---

### Idea D: **ECHO CHAMBER**
**Genre:** Narrative multiplayer / ARG-like
**Pitch:** Each player's leaderboard entry contains a short text blurb (think: a status message). Your rank determines how many characters you get—rank 1 gets 280 characters, rank 100 gets 3. Actions include: writing/editing your blurb, reading others' blurbs, "amplifying" a blurb (boosting that player's rank), or "muting" (dropping them). A story emerges from the collective text of the leaderboard, read top to bottom.

**Success condition:** Get your message to rank #1 and have it "amplified" by 10+ players (consensus that your message matters most).

**Self-referential hook:** The leaderboard is literally a document. The game is about who gets to speak the loudest, and the leaderboard is the medium AND the message.

---

### Idea E: **GLASS THRONE**
**Genre:** Real-time tactics / king-of-the-hill
**Pitch:** 20 players, real-time, 10-minute rounds. The board is visible to all. You have 3 resources: Attack, Defend, Cloak. Attack pushes a target down. Defend blocks one attack. Cloak hides your next action. Here's the twist: **your available resources depend on your rank.** Rank 1 gets 3 Attack, 0 Defend, 0 Cloak (all offense, no protection). Rank 20 gets 0 Attack, 0 Defend, 3 Cloak (invisible but powerless). Middle ranks get balanced loadouts. The throne is made of glass—you can take it, but you can't hold it without help.

**Success condition:** Accumulate the most "time at #1" across the round. Every second at #1 scores a point.

**Self-referential hook:** Your rank literally changes your abilities. The game is a constant negotiation between *wanting* to be #1 and *being able to survive* at #1.

---

## Cross-Cutting Themes

| Theme | Why It Works |
|---|---|
| **The board is the world** | Eliminates the gap between "game" and "meta-game"—there IS no meta, just the board |
| **Rank = identity** | Players don't have characters; they ARE their position. Loss is personal. |
| **Visibility as danger** | Inverts the usual power fantasy. Winning makes you vulnerable. |
| **Social physics** | Proximity on the board creates forced relationships—rivalries, alliances, betrayals |
| **Legibility vs. power** | The more powerful you are, the more readable you become. Information asymmetry flips at the top. |

---

*— Ludologist, Feb 2026*
