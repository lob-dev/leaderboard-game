using SpacetimeDB;

public static partial class Module
{
    /// <summary>
    /// Each connected player has a row in this table.
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
    }

    /// <summary>
    /// Register or update a player's name.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void SetName(ReducerContext ctx, string name)
    {
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
            ctx.Db.Player.Insert(new Player
            {
                Identity = identity,
                Name = name,
                Score = 0,
                Online = true,
            });
        }
    }

    /// <summary>
    /// Add score for the calling player. Server-authoritative: validates score increments.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void AddScore(ReducerContext ctx, long amount)
    {
        // Basic anti-cheat: cap per-call score to reasonable amount
        if (amount <= 0 || amount > 500)
        {
            Log.Warn($"Invalid score amount {amount} from {ctx.Sender}");
            return;
        }

        var existing = ctx.Db.Player.Identity.Find(ctx.Sender);
        if (existing is not null)
        {
            var updated = existing.Value;
            updated.Score += amount;
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
