using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Runtime scene builder - creates the full UI programmatically.
    /// Attach this to an empty GameObject in the scene and it builds everything on Start.
    /// This avoids needing to manually configure the scene in the editor.
    /// </summary>
    public class SceneBuilder : MonoBehaviour
    {
        [Header("Colors")]
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        public Color headerColor = new Color(0.12f, 0.12f, 0.18f);
        public Color entryColor = new Color(0.14f, 0.14f, 0.2f);
        public Color entryAltColor = new Color(0.12f, 0.12f, 0.17f);
        public Color playerEntryColor = new Color(0.25f, 0.2f, 0.05f);
        public Color accentColor = new Color(1f, 0.84f, 0f);
        public Color textColor = new Color(0.9f, 0.9f, 0.95f);
        public Color dimTextColor = new Color(0.5f, 0.5f, 0.6f);
        public Color top3Color = new Color(0.6f, 0.4f, 1f);

        private Canvas mainCanvas;
        private GameObject leaderboardContainer;
        private Transform entryParent;
        private TextMeshProUGUI playerRankText;
        private TextMeshProUGUI playerScoreText;
        private TextMeshProUGUI playerNameText;
        private TextMeshProUGUI comboText;
        private TextMeshProUGUI pointsPerTapText;
        private Button tapButton;
        private ScrollRect scrollRect;
        private GameObject entryPrefab;

        private void Start()
        {
            BuildScene();
        }

        private void BuildScene()
        {
            // Camera setup
            Camera.main.backgroundColor = backgroundColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;

            // Canvas
            var canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            Debug.Log($"[SceneBuilder] Screen: {Screen.width}x{Screen.height}, fullscreen={Screen.fullScreen}");
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 0;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem is required for UI interactions (button clicks, etc.)
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[SceneBuilder] Created EventSystem + StandaloneInputModule");
            }
            else
            {
                Debug.Log("[SceneBuilder] EventSystem already exists");
            }

            // Build entry prefab (hidden template)
            entryPrefab = BuildEntryPrefab(canvasObj.transform);

            // Header bar
            BuildHeader(canvasObj.transform);

            // Leaderboard scroll area
            BuildLeaderboardArea(canvasObj.transform);

            // Player info bar (bottom)
            BuildPlayerBar(canvasObj.transform);

            // Tap area
            BuildTapArea(canvasObj.transform);

            // Wire up managers
            WireUpManagers();
        }

        private GameObject BuildEntryPrefab(Transform parent)
        {
            var prefab = new GameObject("EntryPrefab");
            prefab.transform.SetParent(parent, false);

            var rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80);

            var layout = prefab.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 8);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            var bg = prefab.AddComponent<Image>();
            bg.color = entryColor;

            // LayoutElement so ContentSizeFitter can calculate preferred size
            var le = prefab.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            le.minHeight = 80;

            // Rank
            var rankObj = CreateText(prefab.transform, "Rank", "#1", 36, TextAlignmentOptions.Left, accentColor);
            rankObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);

            // Name
            var nameObj = CreateText(prefab.transform, "Name", "Player", 32, TextAlignmentOptions.Left, textColor);
            nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 60);
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1;

            // Score
            var scoreObj = CreateText(prefab.transform, "Score", "0", 32, TextAlignmentOptions.Right, dimTextColor);
            scoreObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);

            prefab.SetActive(false);
            return prefab;
        }

        private void BuildHeader(Transform parent)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent, false);

            var rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 140);
            rect.anchoredPosition = Vector2.zero;

            var bg = header.AddComponent<Image>();
            bg.color = headerColor;

            // Title
            var title = CreateText(header.transform, "Title", "LEADERBOARD", 52, TextAlignmentOptions.Center, accentColor);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 10);
            titleRect.offsetMax = new Vector2(-20, -30);
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Subtitle
            var sub = CreateText(header.transform, "Subtitle", "Tap to climb. Don't stop.", 24, TextAlignmentOptions.Center, dimTextColor);
            var subRect = sub.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0);
            subRect.anchorMax = new Vector2(1, 0.35f);
            subRect.offsetMin = new Vector2(20, 5);
            subRect.offsetMax = new Vector2(-20, -5);
        }

        private void BuildLeaderboardArea(Transform parent)
        {
            // Scroll view
            var scrollObj = new GameObject("LeaderboardScroll");
            scrollObj.transform.SetParent(parent, false);

            var scrollRectT = scrollObj.AddComponent<RectTransform>();
            scrollRectT.anchorMin = new Vector2(0, 0);
            scrollRectT.anchorMax = new Vector2(1, 1);
            scrollRectT.offsetMin = new Vector2(0, 280); // above player bar + tap area
            scrollRectT.offsetMax = new Vector2(0, -140); // below header

            scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 30f;

            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = backgroundColor;
            // Note: Don't add Mask here — the Viewport already has one. Double masking causes issues.

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;  // Must have alpha>0 for Mask stencil to work
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRect;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            entryParent = content.transform;
        }

        private void BuildPlayerBar(Transform parent)
        {
            var bar = new GameObject("PlayerBar");
            bar.transform.SetParent(parent, false);

            var rect = bar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, 120);
            rect.anchoredPosition = new Vector2(0, 160);

            var bg = bar.AddComponent<Image>();
            bg.color = playerEntryColor;

            // Rank
            playerRankText = CreateText(bar.transform, "PlayerRank", "#31", 48, TextAlignmentOptions.Left, accentColor).GetComponent<TextMeshProUGUI>();
            var prRect = playerRankText.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0, 0);
            prRect.anchorMax = new Vector2(0.2f, 1);
            prRect.offsetMin = new Vector2(20, 10);
            prRect.offsetMax = new Vector2(-5, -10);
            playerRankText.fontStyle = FontStyles.Bold;

            // Name
            playerNameText = CreateText(bar.transform, "PlayerName", "YOU", 36, TextAlignmentOptions.Left, textColor).GetComponent<TextMeshProUGUI>();
            var pnRect = playerNameText.GetComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0.2f, 0);
            pnRect.anchorMax = new Vector2(0.6f, 1);
            pnRect.offsetMin = new Vector2(10, 10);
            pnRect.offsetMax = new Vector2(-5, -10);
            playerNameText.fontStyle = FontStyles.Bold;

            // Score
            playerScoreText = CreateText(bar.transform, "PlayerScore", "0", 44, TextAlignmentOptions.Right, accentColor).GetComponent<TextMeshProUGUI>();
            var psRect = playerScoreText.GetComponent<RectTransform>();
            psRect.anchorMin = new Vector2(0.6f, 0);
            psRect.anchorMax = new Vector2(1, 1);
            psRect.offsetMin = new Vector2(5, 10);
            psRect.offsetMax = new Vector2(-20, -10);
        }

        private void BuildTapArea(Transform parent)
        {
            // Tap button area at bottom
            var tapArea = new GameObject("TapArea");
            tapArea.transform.SetParent(parent, false);

            var rect = tapArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, 160);
            rect.anchoredPosition = Vector2.zero;

            var bg = tapArea.AddComponent<Image>();
            bg.color = headerColor;

            // Tap button
            var btnObj = new GameObject("TapButton");
            btnObj.transform.SetParent(tapArea.transform, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.1f, 0.15f);
            btnRect.anchorMax = new Vector2(0.65f, 0.85f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnBg = btnObj.AddComponent<Image>();
            btnBg.color = accentColor;

            tapButton = btnObj.AddComponent<Button>();
            tapButton.targetGraphic = btnBg;

            var btnColors = tapButton.colors;
            btnColors.normalColor = accentColor;
            btnColors.highlightedColor = new Color(1f, 0.9f, 0.3f);
            btnColors.pressedColor = new Color(0.8f, 0.65f, 0f);
            tapButton.colors = btnColors;

            var btnText = CreateText(btnObj.transform, "BtnText", "TAP!", 40, TextAlignmentOptions.Center, new Color(0.1f, 0.1f, 0.1f));
            var btRect = btnText.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;
            btnText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Combo text
            comboText = CreateText(tapArea.transform, "ComboText", "", 32, TextAlignmentOptions.Center, new Color(1f, 0.5f, 0f)).GetComponent<TextMeshProUGUI>();
            var comboRect = comboText.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.65f, 0.15f);
            comboRect.anchorMax = new Vector2(0.95f, 0.55f);
            comboRect.offsetMin = Vector2.zero;
            comboRect.offsetMax = Vector2.zero;
            comboText.fontStyle = FontStyles.Bold;

            // Points per tap
            pointsPerTapText = CreateText(tapArea.transform, "PointsText", "+10", 28, TextAlignmentOptions.Center, dimTextColor).GetComponent<TextMeshProUGUI>();
            var ptsRect = pointsPerTapText.GetComponent<RectTransform>();
            ptsRect.anchorMin = new Vector2(0.65f, 0.55f);
            ptsRect.anchorMax = new Vector2(0.95f, 0.85f);
            ptsRect.offsetMin = Vector2.zero;
            ptsRect.offsetMax = Vector2.zero;
        }

        private void WireUpManagers()
        {
            // Add LeaderboardManager
            var managerObj = new GameObject("LeaderboardManager");
            var manager = managerObj.AddComponent<LeaderboardManager>();

            // Add LeaderboardUI and wire it up
            var uiObj = new GameObject("LeaderboardUI");
            var ui = uiObj.AddComponent<LeaderboardUIRuntime>();
            ui.Init(entryParent, entryPrefab, scrollRect, playerRankText, playerScoreText, playerNameText,
                    accentColor, top3Color, entryColor, entryAltColor, playerEntryColor, textColor, dimTextColor);

            // Add PlayerController and wire the button
            var playerObj = new GameObject("PlayerController");
            var player = playerObj.AddComponent<PlayerController>();
            // Use reflection to set button and texts
            SetPrivateField(player, "tapButton", tapButton);
            SetPrivateField(player, "comboText", comboText);
            SetPrivateField(player, "pointsPerTapText", pointsPerTapText);

            // Directly wire onClick since reflection + Start() timing is unreliable
            tapButton.onClick.AddListener(player.OnTap);
            Debug.Log($"[SceneBuilder] Button wired. tapButton={tapButton != null}, interactable={tapButton.interactable}, listeners={tapButton.onClick.GetPersistentEventCount()}+runtime");

            // Add TapFeedback for juicy tap effects
            var tapFeedback = playerObj.AddComponent<TapFeedback>();
            tapFeedback.Init(
                tapButton.GetComponent<RectTransform>(),
                mainCanvas.GetComponent<RectTransform>(),
                mainCanvas.transform
            );
            SetPrivateField(player, "tapFeedback", tapFeedback);

            // Add RankUpEffect for celebration animations
            var rankUpObj = new GameObject("RankUpEffect");
            var rankUpEffect = rankUpObj.AddComponent<RankUpEffect>();
            rankUpEffect.Init(mainCanvas.GetComponent<RectTransform>(), mainCanvas.transform);

            // Add LeaderboardAnimator for ambient polish
            var animatorObj = new GameObject("LeaderboardAnimator");
            var lbAnimator = animatorObj.AddComponent<LeaderboardAnimator>();
            // Find player bar background
            var playerBarObj = GameObject.Find("PlayerBar");
            if (playerBarObj != null)
                lbAnimator.Init(playerBarObj.GetComponent<Image>());

            // Add RankChangeDetector
            var detectorObj = new GameObject("RankChangeDetector");
            detectorObj.AddComponent<RankChangeDetector>();

            // Add AutoScreenshot
            var screenshotObj = new GameObject("AutoScreenshot");
            screenshotObj.AddComponent<AutoScreenshot>();
        }

        private GameObject CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            // Explicitly load default TMP font from Resources
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmp.font = font;
            tmp.text = content;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return obj;
        }

        private void SetPrivateField(Component comp, string fieldName, object value)
        {
            var field = comp.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(comp, value);
        }
    }
}
