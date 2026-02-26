# Multiplayer / Online Leaderboard Architecture

## Overview
The game uses **SpacetimeDB** as the backend for real-time multiplayer leaderboard sync. SpacetimeDB provides:
- Server-authoritative game state (no cheating by modifying client)
- Real-time subscriptions (all players see updates instantly)
- Identity-based authentication (no passwords needed)
- Persistent player data across sessions

## Backend (server/spacetimedb/Lib.cs)

### Player Table
| Field | Type | Purpose |
|-------|------|---------|
| Identity | Identity (PK) | SpacetimeDB cryptographic identity |
| Name | string | Display name (max 20 chars) |
| Score | long | Current score |
| Online | bool | Currently connected |
| TotalTaps | long | Lifetime tap count |
| HighestScore | long | All-time high (never decreases) |
| LastActionTimestampMs | long | Server-side timestamp of last action |
| ActionsInWindow | int | Actions in current rate window |
| WindowStartMs | long | Start of rate-limit window |
| JoinedAtMs | long | First connection timestamp |

### Reducers (Server RPCs)
- **SetName(name)** — Register/update display name (sanitized, capped at 20 chars)
- **AddScore(amount)** — Add score with anti-cheat validation
- **ClientConnected** — Auto-called on connect, marks player online
- **ClientDisconnected** — Auto-called on disconnect, marks player offline

### Anti-Cheat
1. **Score cap**: Max 500 points per AddScore call
2. **Rate limiting**: Max 30 actions per 1-second window
3. **Speed check**: Minimum 30ms between actions (prevents inhuman tap speeds)
4. **Server-authoritative**: All score changes happen on the server; client only sends intents

## Client Integration (Unity)

### SpacetimeDBManager.cs
- Singleton that manages the connection lifecycle
- Auto-connects on Start, auto-reconnects on disconnect (up to 10 attempts)
- Saves auth token in PlayerPrefs for persistent identity across sessions
- Subscribes to all Player table changes
- Syncs server state → LeaderboardManager on every change

### Authentication Flow
1. First launch: connects with no token → server assigns new Identity + token
2. Token saved to PlayerPrefs
3. Subsequent launches: connects with saved token → same Identity restored
4. `ResetAuth()` available for testing with fresh identity

### Offline Fallback
- If connection fails, game runs in offline mode with bots
- `fillWithBots` option adds bot entries alongside real players for fuller leaderboard
- Seamless transition: online players appear/disappear as they connect

## Deployment

### Prerequisites
- Install SpacetimeDB CLI: `curl -sSf https://install.spacetimedb.com | bash`
- Or on Windows: download from https://spacetimedb.com

### Local Development
```bash
cd server
spacetime start        # Start local SpacetimeDB instance
spacetime publish leaderboard-game --project-path spacetimedb  # Deploy module
spacetime generate --lang csharp --out-dir ../Assets/Scripts/SpacetimeDB/module_bindings --project-path spacetimedb
```

### Production (SpacetimeDB Cloud)
```bash
spacetime publish leaderboard-game --project-path spacetimedb --server maincloud
```
Then update `SpacetimeDBManager.host` to `https://maincloud.spacetimedb.com`.

## Future Enhancements
- [ ] Seasonal leaderboard resets
- [ ] Player badges/achievements stored server-side
- [ ] Spectator mode (subscribe without playing)
- [ ] Chat/emotes between players
- [ ] Server-side bot simulation for low-population times
