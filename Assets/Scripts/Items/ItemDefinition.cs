namespace LeaderboardGame
{
    /// <summary>
    /// Types of special items that modify tapping behavior.
    /// </summary>
    public enum ItemType
    {
        /// <summary>Double points per tap for a duration.</summary>
        DoublePoints,
        /// <summary>Auto-taps N times per second for a duration.</summary>
        AutoTap,
        /// <summary>Freezes all opponent score gains for a duration.</summary>
        FreezeOpponents,
        /// <summary>Each tap scores a bonus based on current combo (combo booster).</summary>
        ComboBooster,
        /// <summary>Instantly grants a large score bonus.</summary>
        ScoreBomb,
        /// <summary>+50% damage (score per tap) for 10s.</summary>
        DamageBoost,
        /// <summary>2x recharge rate for 8s.</summary>
        ChargeRush,
        /// <summary>Max charges increased to 15 for 15s.</summary>
        MaxCapacity,
        /// <summary>Instantly refill all charges.</summary>
        InstantReload,
        /// <summary>Taps cost 0 charges for 6s.</summary>
        RapidFire,
        /// <summary>Fill to 20 charges with temporary max of 20 for 12s.</summary>
        Overcharge
    }

    /// <summary>
    /// Static data describing each item's properties.
    /// </summary>
    public static class ItemDefinitions
    {
        public struct ItemData
        {
            public string Name;
            public string Emoji;
            public string Description;
            public float Duration; // 0 = instant
            public UnityEngine.Color Color;
        }

        public static ItemData Get(ItemType type)
        {
            switch (type)
            {
                case ItemType.DoublePoints:
                    return new ItemData
                    {
                        Name = "Double Points",
                        Emoji = "x2",
                        Description = "2x points per tap!",
                        Duration = 8f,
                        Color = new UnityEngine.Color(0.2f, 0.8f, 0.2f)
                    };
                case ItemType.AutoTap:
                    return new ItemData
                    {
                        Name = "Auto Tap",
                        Emoji = "\u26a1",
                        Description = "Auto-taps for you!",
                        Duration = 5f,
                        Color = new UnityEngine.Color(0.3f, 0.6f, 1f)
                    };
                case ItemType.FreezeOpponents:
                    return new ItemData
                    {
                        Name = "Freeze",
                        Emoji = "\u2744",
                        Description = "Opponents frozen!",
                        Duration = 6f,
                        Color = new UnityEngine.Color(0.4f, 0.9f, 1f)
                    };
                case ItemType.ComboBooster:
                    return new ItemData
                    {
                        Name = "Combo Boost",
                        Emoji = "\ud83d\udd25",
                        Description = "Combo x2 bonus!",
                        Duration = 10f,
                        Color = new UnityEngine.Color(1f, 0.4f, 0.1f)
                    };
                case ItemType.ScoreBomb:
                    return new ItemData
                    {
                        Name = "Score Bomb",
                        Emoji = "\ud83d\udca3",
                        Description = "+500 instant!",
                        Duration = 0f,
                        Color = new UnityEngine.Color(1f, 0.2f, 0.2f)
                    };
                case ItemType.DamageBoost:
                    return new ItemData
                    {
                        Name = "Damage Boost",
                        Emoji = "\ud83d\udca5",
                        Description = "+50% damage!",
                        Duration = 10f,
                        Color = new UnityEngine.Color(1f, 0.6f, 0.1f)
                    };
                case ItemType.ChargeRush:
                    return new ItemData
                    {
                        Name = "Charge Rush",
                        Emoji = "\u26a1",
                        Description = "2x recharge speed!",
                        Duration = 8f,
                        Color = new UnityEngine.Color(0.3f, 0.9f, 0.5f)
                    };
                case ItemType.MaxCapacity:
                    return new ItemData
                    {
                        Name = "Max Capacity",
                        Emoji = "\ud83d\udd0b",
                        Description = "Hold 15 charges!",
                        Duration = 15f,
                        Color = new UnityEngine.Color(0.2f, 0.6f, 1f)
                    };
                case ItemType.InstantReload:
                    return new ItemData
                    {
                        Name = "Instant Reload",
                        Emoji = "\ud83d\udd04",
                        Description = "Charges refilled!",
                        Duration = 0f,
                        Color = new UnityEngine.Color(0.4f, 1f, 0.4f)
                    };
                case ItemType.RapidFire:
                    return new ItemData
                    {
                        Name = "Rapid Fire",
                        Emoji = "\ud83d\udd25",
                        Description = "Free taps!",
                        Duration = 6f,
                        Color = new UnityEngine.Color(1f, 0.3f, 0.1f)
                    };
                case ItemType.Overcharge:
                    return new ItemData
                    {
                        Name = "Overcharge",
                        Emoji = "\u26a1\u26a1",
                        Description = "20 charges!",
                        Duration = 12f,
                        Color = new UnityEngine.Color(0.9f, 0.9f, 0.2f)
                    };
                default:
                    return new ItemData { Name = "Unknown", Duration = 0f, Color = UnityEngine.Color.white };
            }
        }

        public static int Count => System.Enum.GetValues(typeof(ItemType)).Length;
    }
}
