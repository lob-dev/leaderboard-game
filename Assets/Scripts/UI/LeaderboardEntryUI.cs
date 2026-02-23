using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace LeaderboardGame
{
    /// <summary>
    /// Individual leaderboard row. Handles its own animations.
    /// </summary>
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image background;
        [SerializeField] private Image rankBadge;

        [Header("Animation")]
        [SerializeField] private float slideSpeed = 8f;
        [SerializeField] private float highlightDuration = 0.5f;

        private int lastRank = -1;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetData(LeaderboardEntry entry, Color bgColor)
        {
            rankText.text = $"#{entry.Rank}";
            nameText.text = entry.PlayerName;
            scoreText.text = entry.Score.ToString("N0");
            background.color = bgColor;

            // Rank badge for top 3
            if (rankBadge != null)
            {
                rankBadge.gameObject.SetActive(entry.Rank <= 3);
                if (entry.Rank == 1) rankBadge.color = new Color(1f, 0.84f, 0f);      // Gold
                else if (entry.Rank == 2) rankBadge.color = new Color(0.75f, 0.75f, 0.75f); // Silver
                else if (entry.Rank == 3) rankBadge.color = new Color(0.8f, 0.5f, 0.2f);    // Bronze
            }

            // Detect rank change for animation
            if (lastRank != -1 && entry.Rank != lastRank)
            {
                StartCoroutine(RankChangeFlash(entry.Rank < lastRank));
            }
            lastRank = entry.Rank;
        }

        private IEnumerator RankChangeFlash(bool movedUp)
        {
            Color flashColor = movedUp ? new Color(0.2f, 1f, 0.2f, 0.5f) : new Color(1f, 0.2f, 0.2f, 0.3f);
            Color originalColor = background.color;

            background.color = flashColor;

            float elapsed = 0f;
            while (elapsed < highlightDuration)
            {
                elapsed += Time.deltaTime;
                background.color = Color.Lerp(flashColor, originalColor, elapsed / highlightDuration);
                yield return null;
            }

            background.color = originalColor;
        }
    }
}
