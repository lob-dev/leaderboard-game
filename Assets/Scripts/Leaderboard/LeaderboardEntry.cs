using System;

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

        public LeaderboardEntry(string id, string name, int score, bool isLocal = false)
        {
            PlayerId = id;
            PlayerName = name;
            Score = score;
            IsLocalPlayer = isLocal;
        }

        public int CompareTo(LeaderboardEntry other)
        {
            // Higher score = better rank (sort descending)
            return other.Score.CompareTo(Score);
        }
    }
}
