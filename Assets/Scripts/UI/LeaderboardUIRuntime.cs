using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Runtime version of LeaderboardUI that gets wired up programmatically by SceneBuilder.
    /// </summary>
    public class LeaderboardUIRuntime : MonoBehaviour
    {
        private Transform entryContainer;
        private GameObject entryPrefab;
        private ScrollRect scrollRect;
        private TextMeshProUGUI playerRankText;
        private TextMeshProUGUI playerScoreText;
        private TextMeshProUGUI playerNameText;

        private Color accentColor;
        private Color top3Color;
        private Color entryColor;
        private Color entryAltColor;
        private Color playerEntryColor;
        private Color textColor;
        private Color dimTextColor;

        private List<GameObject> entryObjects = new List<GameObject>();
        private int lastPlayerRank = -1;

        public void Init(Transform container, GameObject prefab, ScrollRect scroll,
                         TextMeshProUGUI rankText, TextMeshProUGUI scoreText, TextMeshProUGUI nameText,
                         Color accent, Color top3, Color entry, Color entryAlt, Color playerEntry, Color text, Color dimText)
        {
            entryContainer = container;
            entryPrefab = prefab;
            scrollRect = scroll;
            playerRankText = rankText;
            playerScoreText = scoreText;
            playerNameText = nameText;
            accentColor = accent;
            top3Color = top3;
            entryColor = entry;
            entryAltColor = entryAlt;
            playerEntryColor = playerEntry;
            textColor = text;
            dimTextColor = dimText;
        }

        private void OnEnable()
        {
            StartCoroutine(WaitForManager());
        }

        private System.Collections.IEnumerator WaitForManager()
        {
            while (LeaderboardManager.Instance == null)
                yield return null;

            LeaderboardManager.Instance.OnLeaderboardUpdated.AddListener(RefreshUI);
            LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(OnRankChanged);

            // Trigger initial refresh (the first event may have fired before we subscribed)
            RefreshUI(LeaderboardManager.Instance.GetEntries());
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
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var obj = Instantiate(entryPrefab, entryContainer);
                obj.SetActive(true);

                var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{entry.Rank}";
                    texts[1].text = entry.PlayerName;
                    texts[2].text = entry.Score.ToString("N0");

                    if (entry.IsLocalPlayer)
                    {
                        texts[0].color = accentColor;
                        texts[1].color = accentColor;
                        texts[2].color = accentColor;
                    }
                    else if (entry.Rank <= 3)
                    {
                        texts[0].color = top3Color;
                        texts[1].color = textColor;
                        texts[2].color = dimTextColor;
                    }
                    else
                    {
                        texts[0].color = dimTextColor;
                        texts[1].color = textColor;
                        texts[2].color = dimTextColor;
                    }
                }

                var bg = obj.GetComponent<Image>();
                if (bg != null)
                {
                    if (entry.IsLocalPlayer)
                        bg.color = playerEntryColor;
                    else
                        bg.color = (i % 2 == 0) ? entryColor : entryAltColor;
                }

                entryObjects.Add(obj);
            }

            // Update player info bar
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            if (player != null)
            {
                playerRankText.text = $"#{player.Rank}";
                playerScoreText.text = player.Score.ToString("N0");
                playerNameText.text = player.PlayerName;

                // Auto-scroll to player position
                if (entries.Count > 0)
                {
                    float normalizedPos = 1f - ((float)(player.Rank - 1) / entries.Count);
                    scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, normalizedPos, Time.deltaTime * 5f);
                }
            }
        }

        private void OnRankChanged(int newRank)
        {
            if (lastPlayerRank != -1 && newRank < lastPlayerRank)
            {
                // Moved up! Could add screen effects here
                Debug.Log($"[Leaderboard] Climbed to #{newRank}!");
            }
            lastPlayerRank = newRank;
        }
    }
}
