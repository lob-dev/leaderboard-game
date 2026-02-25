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

        private static readonly Color[] avatarPalette = new Color[]
        {
            new Color(0.90f, 0.30f, 0.30f),
            new Color(0.20f, 0.65f, 0.90f),
            new Color(0.30f, 0.80f, 0.40f),
            new Color(0.85f, 0.55f, 0.20f),
            new Color(0.65f, 0.40f, 0.90f),
            new Color(0.90f, 0.75f, 0.20f),
            new Color(0.20f, 0.80f, 0.75f),
            new Color(0.85f, 0.35f, 0.70f),
        };

        private void SetupEntryRow(GameObject obj, LeaderboardEntry entry)
        {
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
            // DFS order: [0]=Rank, [1]=Avatar/Initial, [2]=Name, [3]=Score, [4]=Subtitle
            // Skip index 1 (avatar initial) — handled separately below.
            if (texts.Length >= 4)
            {
                texts[0].text = $"#{entry.Rank}";
                texts[2].text = entry.PlayerName;
                texts[3].text = entry.Score.ToString("N0");
            }

            // Set avatar
            var avatarTransform = obj.transform.Find("MainRow/Avatar");
            if (avatarTransform != null)
            {
                var avatarBg = avatarTransform.GetComponent<Image>();
                var initialTransform = avatarTransform.Find("Initial");

                // Try to load avatar image from DiceBear
                if (avatarBg != null && !string.IsNullOrEmpty(entry.PlayerId))
                {
                    bool cached = AvatarLoader.LoadAvatar(entry.PlayerId, avatarBg, this);
                    if (cached)
                    {
                        // Avatar loaded — hide initial text
                        if (initialTransform != null)
                            initialTransform.gameObject.SetActive(false);
                    }
                    else
                    {
                        // Fallback to colored circle with initial while loading
                        if (entry.IsLocalPlayer)
                            avatarBg.color = localPlayerColor;
                        else
                        {
                            int hash = 0;
                            if (!string.IsNullOrEmpty(entry.PlayerId))
                                foreach (char c in entry.PlayerId) hash = hash * 31 + c;
                            avatarBg.color = avatarPalette[Mathf.Abs(hash) % avatarPalette.Length];
                        }
                        if (initialTransform != null)
                        {
                            initialTransform.gameObject.SetActive(true);
                            var tmp = initialTransform.GetComponent<TextMeshProUGUI>();
                            if (tmp != null)
                                tmp.text = !string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerName.Substring(0, 1).ToUpper() : "?";
                        }
                    }
                }
                else if (avatarBg != null)
                {
                    // No player ID — fallback to initial
                    if (entry.IsLocalPlayer)
                        avatarBg.color = localPlayerColor;
                    else
                    {
                        int hash = 0;
                        if (!string.IsNullOrEmpty(entry.PlayerId))
                            foreach (char c in entry.PlayerId) hash = hash * 31 + c;
                        avatarBg.color = avatarPalette[Mathf.Abs(hash) % avatarPalette.Length];
                    }
                    if (initialTransform != null)
                    {
                        var tmp = initialTransform.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                            tmp.text = !string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerName.Substring(0, 1).ToUpper() : "?";
                    }
                }
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
