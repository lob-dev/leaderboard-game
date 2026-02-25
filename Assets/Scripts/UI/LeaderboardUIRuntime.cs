using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace LeaderboardGame
{
    /// <summary>
    /// Runtime version of LeaderboardUI — now with narrative flavor.
    /// Ghost entries, subtitles, auras, rival highlights, alive presence.
    /// </summary>
    public class LeaderboardUIRuntime : MonoBehaviour
    {
        private Transform entryContainer;
        private GameObject entryPrefab;
        private ScrollRect scrollRect;
        private TextMeshProUGUI playerRankText;
        private TextMeshProUGUI playerScoreText;
        private TextMeshProUGUI playerNameText;
        private TextMeshProUGUI playerTitleText; // Playstyle subtitle

        private Color accentColor;
        private Color top3Color;
        private Color entryColor;
        private Color entryAltColor;
        private Color playerEntryColor;
        private Color textColor;
        private Color dimTextColor;

        // Ghost colors
        private Color ghostBgColor = new Color(0.08f, 0.08f, 0.1f, 0.6f);
        private Color ghostTextColor = new Color(0.35f, 0.35f, 0.4f, 0.7f);
        private Color ghostNameColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);
        private Color rivalBorderColor = new Color(1f, 0.25f, 0.3f, 0.8f);
        private Color lastWordsColor = new Color(0.45f, 0.45f, 0.55f, 0.6f);

        // Synthesis UI references
        private Image visibilityFill;
        private TextMeshProUGUI visibilityLabel;
        private TextMeshProUGUI costLabel;
        private TextMeshProUGUI rivalCountLabel;
        private TextMeshProUGUI becomingLabel;

        private List<GameObject> entryObjects = new List<GameObject>();
        private RectTransform localPlayerChargeFillRect; // live-updated charge meter for local player
        private Image localPlayerChargeFillImg;
        private int lastPlayerRank = -1;

        // Auto-scroll to player
        private RectTransform localPlayerEntryRect;
        private float lastUserScrollTime = -999f;
        private float autoScrollIdleDelay = 3f; // seconds of no scrolling before auto-centering
        private bool userIsDragging;
        private float autoScrollSpeed = 5f;

        public void Init(Transform container, GameObject prefab, ScrollRect scroll,
                         TextMeshProUGUI rankText, TextMeshProUGUI scoreText, TextMeshProUGUI nameText,
                         TextMeshProUGUI titleText,
                         Color accent, Color top3, Color entry, Color entryAlt, Color playerEntry, Color text, Color dimText)
        {
            entryContainer = container;
            entryPrefab = prefab;
            scrollRect = scroll;
            playerRankText = rankText;
            playerScoreText = scoreText;
            playerNameText = nameText;
            playerTitleText = titleText;
            accentColor = accent;
            top3Color = top3;
            entryColor = entry;
            entryAltColor = entryAlt;
            playerEntryColor = playerEntry;
            textColor = text;
            dimTextColor = dimText;

            // Listen for user scroll interactions
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
                // Attach drag detector to detect begin/end drag
                var dragDetector = scrollRect.gameObject.GetComponent<ScrollDragDetector>();
                if (dragDetector == null)
                    dragDetector = scrollRect.gameObject.AddComponent<ScrollDragDetector>();
                dragDetector.onBeginDrag = () => { userIsDragging = true; lastUserScrollTime = Time.time; };
                dragDetector.onEndDrag = () => { userIsDragging = false; lastUserScrollTime = Time.time; };
            }

            StartCoroutine(WaitForManager());
        }

        public void SetSynthesisUI(Image visFill, TextMeshProUGUI visLabel, TextMeshProUGUI costLbl, TextMeshProUGUI rivalLbl, TextMeshProUGUI becomingLbl)
        {
            visibilityFill = visFill;
            visibilityLabel = visLabel;
            costLabel = costLbl;
            rivalCountLabel = rivalLbl;
            becomingLabel = becomingLbl;
        }

        private System.Collections.IEnumerator WaitForManager()
        {
            while (LeaderboardManager.Instance == null)
                yield return null;

            LeaderboardManager.Instance.OnLeaderboardUpdated.AddListener(RefreshUI);
            LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(OnRankChanged);
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

        private void OnScrollValueChanged(Vector2 _)
        {
            if (userIsDragging)
                lastUserScrollTime = Time.time;
        }

        private void RefreshUI(List<LeaderboardEntry> entries)
        {
            foreach (var obj in entryObjects)
                DestroyImmediate(obj);
            entryObjects.Clear();
            localPlayerChargeFillRect = null;
            localPlayerChargeFillImg = null;
            localPlayerEntryRect = null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var obj = Instantiate(entryPrefab, entryContainer);
                obj.SetActive(true);

                var texts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
                // texts[0]=Rank, [1]=Name, [2]=Score, [3]=Subtitle (if exists)

                if (entry.IsGhost)
                    SetupGhostEntry(obj, entry, texts);
                else
                    SetupAliveEntry(obj, entry, texts, i);

                // Aura glow (left border accent)
                if (entry.AuraColor.a > 0.01f)
                    AddAuraGlow(obj, entry.AuraColor);

                // Rival border highlight
                if (entry.IsRival)
                    AddRivalHighlight(obj);

                // Add charge meter bar to all entries
                AddChargeMeter(obj, entry);

                // Attach ambient row animation (skip local player — handled by LeaderboardAnimator)
                if (!entry.IsLocalPlayer)
                    AttachRowAnimator(obj, entry);

                // Track local player entry for auto-scroll
                if (entry.IsLocalPlayer)
                    localPlayerEntryRect = obj.GetComponent<RectTransform>();

                entryObjects.Add(obj);
            }

            // Force layout rebuild
            var contentRect = entryContainer.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            // Update player info bar
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            if (player != null)
            {
                playerRankText.text = $"#{player.Rank}";
                playerScoreText.text = player.Score.ToString("N0");
                playerNameText.text = player.PlayerName;
                
                // Show playstyle title prominently ("You Are Becoming")
                if (playerTitleText != null)
                {
                    if (!string.IsNullOrEmpty(player.PlaystyleTitle))
                        playerTitleText.text = $"⟐ {player.PlaystyleTitle} ⟐";
                    else
                        playerTitleText.text = "";
                }

                // === SYNTHESIS UI UPDATES ===
                
                // Visibility meter (Observer Effect) — use anchor scaling (fillAmount needs a sprite)
                if (visibilityFill != null)
                {
                    var visRect = visibilityFill.GetComponent<RectTransform>();
                    if (visRect != null)
                        visRect.anchorMax = new Vector2(player.Visibility, 1f);
                    // Color: green at low vis, yellow mid, red high
                    visibilityFill.color = Color.Lerp(
                        new Color(0.2f, 0.8f, 0.3f),
                        new Color(1f, 0.2f, 0.2f),
                        player.Visibility
                    );
                }
                if (visibilityLabel != null)
                    visibilityLabel.text = $"👁 {Mathf.RoundToInt(player.Visibility * 100)}%";
                if (costLabel != null)
                {
                    string costStr = player.CostMultiplier > 1.5f ? $"COST: {player.CostMultiplier:F1}x ⚡" :
                                     player.CostMultiplier < 0.9f ? $"COST: {player.CostMultiplier:F1}x ✦" :
                                     $"COST: {player.CostMultiplier:F1}x";
                    costLabel.text = costStr;
                    costLabel.color = player.CostMultiplier > 1.5f ? new Color(1f, 0.3f, 0.3f) :
                                     player.CostMultiplier < 0.9f ? new Color(0.3f, 1f, 0.5f) :
                                     dimTextColor;
                }

                // Rival count
                if (rivalCountLabel != null)
                {
                    if (player.RivalCount > 0)
                    {
                        rivalCountLabel.text = $"🔴 {player.RivalCount} RIVAL{(player.RivalCount > 1 ? "S" : "")} (+25%)";
                        rivalCountLabel.color = new Color(1f, 0.4f, 0.4f);
                    }
                    else
                    {
                        rivalCountLabel.text = "No rivals nearby";
                        rivalCountLabel.color = dimTextColor;
                    }
                }

                // Becoming label
                if (becomingLabel != null)
                {
                    if (!string.IsNullOrEmpty(player.PlaystyleTitle))
                    {
                        becomingLabel.text = $"YOU ARE BECOMING: {player.PlaystyleTitle.ToUpper()}";
                        if (player.PlaystyleTitle == "Striker")
                            becomingLabel.color = new Color(1f, 0.4f, 0.2f);
                        else if (player.PlaystyleTitle == "Stalwart")
                            becomingLabel.color = new Color(0.3f, 0.7f, 1f);
                        else if (player.PlaystyleTitle == "Nomad")
                            becomingLabel.color = new Color(0.5f, 1f, 0.4f);
                        else
                            becomingLabel.color = accentColor;
                    }
                    else
                    {
                        becomingLabel.text = "TAP TO DISCOVER WHO YOU ARE";
                        becomingLabel.color = dimTextColor;
                    }
                }

                // Only scroll to top on first load, not on every refresh
                // (prevents hijacking user's scroll position)
                if (lastPlayerRank == -1)
                    scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static readonly Color[] avatarPalette = new Color[]
        {
            new Color(0.90f, 0.30f, 0.30f), // Red
            new Color(0.20f, 0.65f, 0.90f), // Blue
            new Color(0.30f, 0.80f, 0.40f), // Green
            new Color(0.85f, 0.55f, 0.20f), // Orange
            new Color(0.65f, 0.40f, 0.90f), // Purple
            new Color(0.90f, 0.75f, 0.20f), // Yellow
            new Color(0.20f, 0.80f, 0.75f), // Teal
            new Color(0.85f, 0.35f, 0.70f), // Pink
        };

        private Color GetAvatarColor(string playerId, bool isLocal)
        {
            if (isLocal) return accentColor;
            int hash = 0;
            if (!string.IsNullOrEmpty(playerId))
                foreach (char c in playerId) hash = hash * 31 + c;
            return avatarPalette[Mathf.Abs(hash) % avatarPalette.Length];
        }

        private void SetupAvatar(GameObject obj, LeaderboardEntry entry)
        {
            var avatarTransform = obj.transform.Find("MainRow/Avatar");
            if (avatarTransform == null) return;

            var avatarBg = avatarTransform.GetComponent<Image>();
            if (avatarBg != null)
                avatarBg.color = entry.IsGhost ? ghostBgColor : GetAvatarColor(entry.PlayerId, entry.IsLocalPlayer);

            var initialTransform = avatarTransform.Find("Initial");
            if (initialTransform != null)
            {
                var tmp = initialTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    string initial = !string.IsNullOrEmpty(entry.PlayerName) ? entry.PlayerName.Substring(0, 1).ToUpper() : "?";
                    tmp.text = initial;
                    tmp.color = entry.IsGhost ? ghostTextColor : Color.white;
                    if (entry.IsGhost) tmp.fontStyle = FontStyles.Italic;
                }
            }
        }

        private void SetupAliveEntry(GameObject obj, LeaderboardEntry entry, TextMeshProUGUI[] texts, int index)
        {
            SetupAvatar(obj, entry);

            if (texts.Length >= 3)
            {
                texts[0].text = $"#{entry.Rank}";

                // Show powerup icon next to name if they have an active powerup
                if (entry.ActivePowerup.HasValue)
                {
                    var pwData = ItemDefinitions.Get(entry.ActivePowerup.Value);
                    texts[1].text = $"{entry.PlayerName} <color=#{ColorUtility.ToHtmlStringRGB(pwData.Color)}>[{pwData.Emoji}]</color>";
                }
                else
                {
                    texts[1].text = entry.PlayerName;
                }

                texts[2].text = entry.Score.ToString("N0");

                if (entry.IsLocalPlayer)
                {
                    texts[0].color = accentColor;
                    texts[1].color = accentColor;
                    texts[2].color = accentColor;
                }
                else if (entry.IsRival)
                {
                    texts[0].color = new Color(1f, 0.4f, 0.4f);
                    texts[1].color = new Color(1f, 0.5f, 0.5f);
                    texts[2].color = new Color(1f, 0.4f, 0.4f);
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

            // Subtitle: playstyle title + visibility for rivals
            if (texts.Length >= 4)
            {
                string subtitle = "";
                if (!string.IsNullOrEmpty(entry.PlaystyleTitle))
                    subtitle = entry.PlaystyleTitle;
                if (entry.IsRival)
                    subtitle = (subtitle.Length > 0 ? subtitle + " • " : "") + $"RIVAL (👁{Mathf.RoundToInt(entry.Visibility * 100)}%)";
                else if (entry.Visibility > 0.7f && !entry.IsLocalPlayer)
                    subtitle = (subtitle.Length > 0 ? subtitle + " • " : "") + $"👁{Mathf.RoundToInt(entry.Visibility * 100)}%";

                if (!string.IsNullOrEmpty(subtitle))
                {
                    texts[3].text = subtitle;
                    texts[3].color = entry.IsLocalPlayer ? new Color(1f, 0.75f, 0.3f, 0.8f) :
                                     entry.IsRival ? new Color(1f, 0.4f, 0.4f, 0.8f) :
                                     new Color(0.5f, 0.5f, 0.6f, 0.6f);
                    texts[3].gameObject.SetActive(true);
                }
                else
                {
                    texts[3].gameObject.SetActive(false);
                }
            }

            var bg = obj.GetComponent<Image>();
            if (bg != null)
            {
                if (entry.IsLocalPlayer)
                    bg.color = playerEntryColor;
                else if (entry.IsRival)
                    bg.color = new Color(0.2f, 0.1f, 0.1f, 0.9f);
                else
                    bg.color = (index % 2 == 0) ? entryColor : entryAltColor;
            }
        }

        private void SetupGhostEntry(GameObject obj, LeaderboardEntry entry, TextMeshProUGUI[] texts)
        {
            SetupAvatar(obj, entry);

            if (texts.Length >= 3)
            {
                texts[0].text = $"#{entry.Rank}";
                texts[0].color = ghostTextColor;
                texts[0].fontStyle = FontStyles.Italic;

                // Ghost name with strikethrough effect
                texts[1].text = entry.PlayerName;
                texts[1].color = ghostNameColor;
                texts[1].fontStyle = FontStyles.Italic | FontStyles.Strikethrough;

                texts[2].text = entry.Score.ToString("N0");
                texts[2].color = ghostTextColor;
                texts[2].fontStyle = FontStyles.Italic;
            }

            // Show "Last Words" as subtitle
            if (texts.Length >= 4 && !string.IsNullOrEmpty(entry.LastWords))
            {
                texts[3].text = $"\"{entry.LastWords}\"";
                texts[3].color = lastWordsColor;
                texts[3].fontStyle = FontStyles.Italic;
                texts[3].gameObject.SetActive(true);
            }
            else if (texts.Length >= 4)
            {
                texts[3].gameObject.SetActive(false);
            }

            var bg = obj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = ghostBgColor;
            }
        }

        private void AddAuraGlow(GameObject entryObj, Color auraColor)
        {
            // Add a thin colored bar on the left as an "aura"
            var aura = new GameObject("Aura");
            aura.transform.SetParent(entryObj.transform, false);

            var rect = aura.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(4, 0);
            rect.anchoredPosition = Vector2.zero;

            var img = aura.AddComponent<Image>();
            img.color = auraColor;
            img.raycastTarget = false;

            // Set as first child so it renders behind text
            aura.transform.SetAsFirstSibling();
        }

        private void AddRivalHighlight(GameObject entryObj)
        {
            // Pulsing border effect for rival
            var border = new GameObject("RivalBorder");
            border.transform.SetParent(entryObj.transform, false);

            var rect = border.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(-2, -2);
            rect.offsetMax = new Vector2(2, 2);

            var outline = border.AddComponent<Outline>();
            // Outline component is on Text — instead use Image
            Destroy(outline);

            // Use a slightly transparent overlay
            var img = border.AddComponent<Image>();
            img.color = new Color(1f, 0.2f, 0.3f, 0.08f);
            img.raycastTarget = false;
            border.transform.SetAsFirstSibling();
        }

        private void AddChargeMeter(GameObject entryObj, LeaderboardEntry entry)
        {
            // Thin charge bar at the bottom of each leaderboard entry
            var meterObj = new GameObject("ChargeMeter");
            meterObj.transform.SetParent(entryObj.transform, false);

            var rect = meterObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(0, 4);
            rect.anchoredPosition = Vector2.zero;

            // LayoutElement so VerticalLayoutGroup doesn't mess with positioning
            var le = meterObj.AddComponent<LayoutElement>();
            le.preferredHeight = 4;
            le.minHeight = 4;

            // Background
            var bgImg = meterObj.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            bgImg.raycastTarget = false;

            // Fill — use RectTransform anchor scaling instead of Image.fillAmount
            // (fillAmount requires a sprite; without one Unity ignores it and draws full rect)
            var fillObj = new GameObject("ChargeFill");
            fillObj.transform.SetParent(meterObj.transform, false);

            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImg = fillObj.AddComponent<Image>();
            fillImg.raycastTarget = false;

            if (entry.IsLocalPlayer && ChargeManager.Instance != null)
            {
                // Store references for live updates in Update()
                localPlayerChargeFillRect = fillRect;
                localPlayerChargeFillImg = fillImg;
                float pct = ChargeManager.Instance.FillPercent;
                fillRect.anchorMax = new Vector2(pct, 1f);
                fillImg.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.4f), pct);
            }
            else if (entry.IsGhost)
            {
                // Ghosts: empty meter
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillImg.color = new Color(0.3f, 0.3f, 0.35f, 0.4f);
            }
            else
            {
                // Bots/other players: simulate a semi-random charge level based on activity
                float fakeFill = 0.3f + 0.7f * Mathf.PerlinNoise(entry.Score * 0.01f, Time.time * 0.5f);
                fillRect.anchorMax = new Vector2(fakeFill, 1f);
                fillImg.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.4f), fakeFill);
            }
        }

        private void AttachRowAnimator(GameObject obj, LeaderboardEntry entry)
        {
            var animator = obj.AddComponent<RowAnimator>();
            var bg = obj.GetComponent<Image>();
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI scoreText = texts.Length >= 3 ? texts[2] : null;

            // Find rival border image if present
            Image rivalBorder = null;
            var borderTransform = obj.transform.Find("RivalBorder");
            if (borderTransform != null)
                rivalBorder = borderTransform.GetComponent<Image>();

            RowAnimator.RowType type;
            if (entry.IsGhost)
                type = RowAnimator.RowType.Ghost;
            else if (entry.IsRival)
                type = RowAnimator.RowType.Rival;
            else if (entry.Rank <= 3)
                type = RowAnimator.RowType.Top3;
            else
                type = RowAnimator.RowType.Normal;

            animator.Init(type, bg, scoreText, rivalBorder);
        }

        private void Update()
        {
            // Live-update the local player's charge meter every frame
            if (localPlayerChargeFillRect != null && ChargeManager.Instance != null)
            {
                float pct = ChargeManager.Instance.FillPercent;
                localPlayerChargeFillRect.anchorMax = new Vector2(pct, 1f);
                if (localPlayerChargeFillImg != null)
                    localPlayerChargeFillImg.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.4f), pct);
            }

            // Auto-scroll to player's position when not actively scrolling
            AutoScrollToPlayer();
        }

        private void AutoScrollToPlayer()
        {
            if (scrollRect == null || localPlayerEntryRect == null || userIsDragging)
                return;

            // Wait for idle delay after last user interaction
            if (Time.time - lastUserScrollTime < autoScrollIdleDelay)
                return;

            var content = scrollRect.content;
            var viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
            if (content == null || viewport == null) return;

            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            if (contentHeight <= viewportHeight) return; // no scrolling needed

            // Calculate target scroll position to center the player entry
            float playerLocalY = -localPlayerEntryRect.anchoredPosition.y; // positive distance from top
            float entryHeight = localPlayerEntryRect.rect.height;
            float centerOffset = playerLocalY - (viewportHeight * 0.5f) + (entryHeight * 0.5f);
            float maxScroll = contentHeight - viewportHeight;
            float targetNormalized = 1f - Mathf.Clamp01(centerOffset / maxScroll);

            // Smoothly lerp toward target
            float current = scrollRect.verticalNormalizedPosition;
            if (Mathf.Abs(current - targetNormalized) > 0.001f)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(current, targetNormalized, Time.deltaTime * autoScrollSpeed);
            }
        }

        private void OnRankChanged(int newRank)
        {
            if (lastPlayerRank != -1 && newRank < lastPlayerRank)
            {
                Debug.Log($"[Leaderboard] Climbed to #{newRank}!");
            }
            lastPlayerRank = newRank;
        }
    }
}
