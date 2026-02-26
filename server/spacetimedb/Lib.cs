using SpacetimeDB;

public static partial class Module
{
    /// <summary>
    /// Each connected player has a row in this table.
    /// Stores identity, display info, score, online status, and anti-cheat metadata.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "Player", Public = true)]
    public partial struct Player
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.Unique]
        public Identity Identity;

        public string Name;
        public long Score;
        public bool Online;

        // Anti-cheat & stats
        public long TotalTaps;
        public long LastActionTimestampMs;  // server-side epoch ms
        public int ActionsInWindow;         // actions in current rate-limit window
        public long WindowStartMs;          // start of current rate-limit window
        public long JoinedAtMs;             // first connection time
        public long HighestScore;           // all-time high (never decreases)
    }

    // --- Configuration ---
    private const int MAX_SCORE_PER_ACTION = 500;
    private const int MAX_ACTIONS_PER_WINDOW = 30;  // max 30 taps per window
    private const long RATE_WINDOW_MS = 1000;        // 1-second window
    private const int MIN_ACTION_INTERVAL_MS = 30;   // minimum 30ms between actions (prevents inhuman speeds)

    /// <summary>
    /// Get current server timestamp in milliseconds.
    /// </summary>
    private static long NowMs(ReducerContext ctx)
    {
        return (long)(ctx.Timestamp.MicrosecondsSinceUnixEpoch / 1000);
    }

    /// <summary>
    /// Register or update a player's name.
    /// Names are sanitized and length-capped.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void SetName(ReducerContext ctx, string name)
    {
        // Sanitize name
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Player";
        }
        // Cap length
        if (name.Length > 20)
        {
            name = name.Substring(0, 20);
        }
        // Strip control characters
        name = name.Trim();

        var identity = ctx.Sender;
        var existing = ctx.Db.Player.Identity.Find(identity);
        if (existing is not null)
        {
            var updated = existing.Value;
            updated.Name = name;
            ctx.Db.Player.Identity.Update(updated);
        }
        else
        {
            long now = NowMs(ctx);
            ctx.Db.Player.Insert(new Player
            {
                Identity = identity,
                Name = name,
                Score = 0,
                Online = true,
                TotalTaps = 0,
                LastActionTimestampMs = 0,
                ActionsInWindow = 0,
                WindowStartMs = now,
                JoinedAtMs = now,
                HighestScore = 0,
            });
        }
    }

    /// <summary>
    /// Add score for the calling player. Server-authoritative with rate limiting.
    /// Anti-cheat:
    ///   - Score amount capped per call (1-500)
    ///   - Rate limited: max 30 actions per second
    ///   - Minimum interval between actions: 30ms
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void AddScore(ReducerContext ctx, long amount)
    {
        // Validate score range
        if (amount <= 0 || amount > MAX_SCORE_PER_ACTION)
        {
            Log.Warn($"Invalid score amount {amount} from {ctx.Sender}");
            return;
        }

        long now = NowMs(ctx);

        var existing = ctx.Db.Player.Identity.Find(ctx.Sender);
        if (existing is not null)
        {
            var updated = existing.Value;

            // --- Rate limiting ---
            // Check minimum interval between actions
            if (updated.LastActionTimestampMs > 0)
            {
                long elapsed = now - updated.LastActionTimestampMs;
                if (elapsed < MIN_ACTION_INTERVAL_MS)
                {
                    Log.Warn($"Rate limit: too fast ({elapsed}ms) from {ctx.Sender}");
                    return;  // silently drop — client shouldn't be this fast
                }
            }

            // Check actions-per-window rate limit
            if (now - updated.WindowStartMs < RATE_WINDOW_MS)
            {
                if (updated.ActionsInWindow >= MAX_ACTIONS_PER_WINDOW)
                {
                    Log.Warn($"Rate limit: {updated.ActionsInWindow} actions in window from {ctx.Sender}");
                    return;
                }
                updated.ActionsInWindow++;
            }
            else
            {
                // New window
                updated.WindowStartMs = now;
                updated.ActionsInWindow = 1;
            }

            // Apply score
            updated.Score += amount;
            updated.TotalTaps++;
            updated.LastActionTimestampMs = now;

            // Track all-time high
            if (updated.Score > updated.HighestScore)
            {
                updated.HighestScore = updated.Score;
            }

            ctx.Db.Player.Identity.Update(updated);
        }
        else
        {
            // Auto-register player if they don't exist
            ctx.Db.Player.Insert(new Player
            {
                Identity = ctx.Sender,
                Name = "Player",
                Score = amount,
                Online = true,
                TotalTaps = 1,
                LastActionTimestampMs = now,
                ActionsInWindow = 1,
                WindowStartMs = now,
                JoinedAtMs = now,
                HighestScore = amount,
            });
        }
    }

    /// <summary>
    /// Mark player online when they connect.
    /// </summary>
    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void ClientConnected(ReducerContext ctx)
    {
        var existing = ctx.Db.Player.Identity.Find(ctx.Sender);
        if (existing is not null)
        {
            var updated = existing.Value;
            updated.Online = true;
            ctx.Db.Player.Identity.Update(updated);
        }
        else
        {
            // First connection — create player record
            long now = NowMs(ctx);
            ctx.Db.Player.Insert(new Player
            {
                Identity = ctx.Sender,
                Name = "Player",
                Score = 0,
                Online = true,
                TotalTaps = 0,
                LastActionTimestampMs = 0,
                ActionsInWindow = 0,
                WindowStartMs = now,
                JoinedAtMs = now,
                HighestScore = 0,
            });
        }
    }

    /// <summary>
    /// Mark player offline when they disconnect.
    /// </summary>
    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void ClientDisconnected(ReducerContext ctx)
    {
        var existing = ctx.Db.Player.Identity.Find(ctx.Sender);
        if (existing is not null)
        {
            var updated = existing.Value;
            updated.Online = false;
            ctx.Db.Player.Identity.Update(updated);
        }
    }
}
