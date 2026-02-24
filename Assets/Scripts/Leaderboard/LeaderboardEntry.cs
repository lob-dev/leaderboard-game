using System;
using UnityEngine;

namespace LeaderboardGame
{
    [Serializable]
    public class LeaderboardEntry : IComparable<LeaderboardEntry>
    {
        public string PlayerId;
        public string PlayerName;
        public int Score;
        public int Rank;
        public bool IsLocalPlayer;
        public bool IsOnlinePlayer;

        // === Narrativist fields ===
        public bool IsGhost;              // Inactive/dead entry
        public string LastWords;          // Final message before going inactive
        public string PlaystyleTitle;     // "The Hammer", "The Scholar", etc.
        public float LastActiveTime;      // Time.time of last activity
        public Color AuraColor;           // Visual aura based on history
        public bool IsRival;              // Currently highlighted as rival
        public int PreviousRank;          // For detecting rank changes
        public float RankChangeTime;      // When rank last changed
        public bool JustRankedUp;         // Flash state for rank-up animation
        public bool JustRankedDown;       // Flash state for rank-down

        // Bot narrative flavor
        public string GhostReason;        // Why they went inactive (flavor)

        public LeaderboardEntry(string id, string name, int score, bool isLocal = false)
        {
            PlayerId = id;
            PlayerName = name;
            Score = score;
            IsLocalPlayer = isLocal;
            IsOnlinePlayer = false;
            IsGhost = false;
            LastWords = "";
            PlaystyleTitle = "";
            LastActiveTime = Time.time;
            AuraColor = Color.clear;
            IsRival = false;
            PreviousRank = -1;
            RankChangeTime = 0f;
            JustRankedUp = false;
            JustRankedDown = false;
            GhostReason = "";
        }

        public int CompareTo(LeaderboardEntry other)
        {
            // Ghosts always sink to bottom
            if (IsGhost && !other.IsGhost) return 1;
            if (!IsGhost && other.IsGhost) return -1;
            // Higher score = better rank (sort descending)
            return other.Score.CompareTo(Score);
        }
    }
}
