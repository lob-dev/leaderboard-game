using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Renders the leaderboard as the main game view.
    /// The leaderboard is ALWAYS visible — it's the game.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI playerNameText;

        [Header("Colors")]
        [SerializeField] private Color localPlayerColor = new Color(1f, 0.84f, 0f); // Gold
        [SerializeField] private Color top3Color = new Color(0.8f, 0.6f, 1f);       // Purple
        [SerializeField] private Color normalColor = new Color(0.9f, 0.9f, 0.9f);

        private List<GameObject> entryObjects = new List<GameObject>();

        private void OnEnable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated.AddListener(RefreshUI);
                LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(OnRankChanged);
            }
        }

        private void OnDisable()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.OnLeaderboardUpdated.RemoveListener(RefreshUI);
                LeaderboardManager.Instance.OnPlayerRankChanged.RemoveListener(OnRankChanged);
            }
        }

        private void RefreshUI(List<LeaderboardEntry> entries)
        {
            // Clear existing
            foreach (var obj in entryObjects)
            {
                Destroy(obj);
            }
            entryObjects.Clear();

            // Create entry rows
            foreach (var entry in entries)
            {
                var obj = Instantiate(entryPrefab, entryContainer);
                SetupEntryRow(obj, entry);
                entryObjects.Add(obj);
            }

            // Update player info bar
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            if (player != null)
            {
                playerRankText.text = $"#{player.Rank}";
                playerScoreText.text = player.Score.ToString("N0");
                playerNameText.text = player.PlayerName;
            }
        }

        private void SetupEntryRow(GameObject obj, LeaderboardEntry entry)
        {
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[0].text = $"#{entry.Rank}";
                texts[1].text = entry.PlayerName;
                texts[2].text = entry.Score.ToString("N0");
            }

            // Color coding
            var bg = obj.GetComponent<Image>();
            if (bg != null)
            {
                if (entry.IsLocalPlayer)
                    bg.color = localPlayerColor;
                else if (entry.Rank <= 3)
                    bg.color = top3Color;
                else
                    bg.color = normalColor;
            }
        }

        private void OnRankChanged(int newRank)
        {
            // TODO: Rank change animation, screen shake, celebration
            Debug.Log($"[Leaderboard] Player rank changed to #{newRank}!");
        }
    }
}
