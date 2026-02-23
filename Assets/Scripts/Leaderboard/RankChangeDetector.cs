using UnityEngine;
using UnityEngine.Events;

namespace LeaderboardGame
{
    /// <summary>
    /// Detects meaningful rank changes and triggers juice events.
    /// "You just overtook xXSlayerXx!" type moments.
    /// </summary>
    public class RankChangeDetector : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent<string> OnOvertake;        // player name you passed
        public UnityEvent<int> OnMilestoneReached;    // top 10, top 5, top 3, #1
        public UnityEvent<string> OnOvertakenBy;      // someone passed you

        private int lastRank = -1;
        private int lastScore = 0;

        private void OnEnable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(CheckRankChange);
            }
        }

        private void OnDisable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnPlayerRankChanged.RemoveListener(CheckRankChange);
            }
        }

        private void CheckRankChange(int newRank)
        {
            if (lastRank == -1)
            {
                lastRank = newRank;
                return;
            }

            if (newRank < lastRank)
            {
                // Moved up! Find who we overtook
                var entries = LeaderboardManager.Instance.GetEntries();
                if (newRank < entries.Count)
                {
                    var overtaken = entries[newRank]; // the one now below us
                    OnOvertake?.Invoke(overtaken.PlayerName);
                }

                // Milestones
                if (newRank <= 10 && lastRank > 10) OnMilestoneReached?.Invoke(10);
                if (newRank <= 5 && lastRank > 5) OnMilestoneReached?.Invoke(5);
                if (newRank <= 3 && lastRank > 3) OnMilestoneReached?.Invoke(3);
                if (newRank == 1 && lastRank > 1) OnMilestoneReached?.Invoke(1);
            }
            else if (newRank > lastRank)
            {
                // Someone overtook us
                var entries = LeaderboardManager.Instance.GetEntries();
                if (lastRank - 1 < entries.Count)
                {
                    var overtaker = entries[lastRank - 1];
                    OnOvertakenBy?.Invoke(overtaker.PlayerName);
                }
            }

            lastRank = newRank;
        }
    }
}
