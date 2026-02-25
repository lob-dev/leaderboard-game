using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Runtime scene builder - creates the full UI programmatically.
    /// Now with narrative layer: board voice, subtitles, alive presence.
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
        private TextMeshProUGUI playerTitleText;
        private TextMeshProUGUI comboText;
        private TextMeshProUGUI pointsPerTapText;
        private Button tapButton;
        private ScrollRect scrollRect;
        private GameObject entryPrefab;

        // Narrative UI
        private TextMeshProUGUI boardVoiceText;
        private CanvasGroup boardVoiceCG;

        // Synthesis UI
        private Image visibilityFill;
        private TextMeshProUGUI visibilityLabel;
        private TextMeshProUGUI costLabel;
        private TextMeshProUGUI rivalCountLabel;
        private TextMeshProUGUI becomingLabel;

        private void Start()
        {
            BuildScene();
        }

        private void BuildScene()
        {
            Camera.main.backgroundColor = backgroundColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;

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

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            entryPrefab = BuildEntryPrefab(canvasObj.transform);
            BuildHeader(canvasObj.transform);
            BuildBoardVoice(canvasObj.transform);
            BuildSynthesisHUD(canvasObj.transform);
            BuildLeaderboardArea(canvasObj.transform);
            BuildPlayerBar(canvasObj.transform);
            BuildTapArea(canvasObj.transform);
            WireUpManagers();
        }

        private GameObject BuildEntryPrefab(Transform parent)
        {
            var prefab = new GameObject("EntryPrefab");
            prefab.transform.SetParent(parent, false);

            var rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 90); // slightly taller for subtitle

            var layout = prefab.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 0;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var bg = prefab.AddComponent<Image>();
            bg.color = entryColor;

            var le = prefab.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            le.minHeight = 90;

            // Main row (rank, name, score) in a horizontal group
            var mainRow = new GameObject("MainRow");
            mainRow.transform.SetParent(prefab.transform, false);
            var mainRowRect = mainRow.AddComponent<RectTransform>();
            mainRowRect.sizeDelta = new Vector2(0, 54);
            var mainRowLayout = mainRow.AddComponent<HorizontalLayoutGroup>();
            mainRowLayout.padding = new RectOffset(12, 12, 0, 0);
            mainRowLayout.spacing = 15;
            mainRowLayout.childAlignment = TextAnchor.MiddleLeft;
            mainRowLayout.childControlWidth = false;
            mainRowLayout.childForceExpandWidth = false;
            var mainRowLE = mainRow.AddComponent<LayoutElement>();
            mainRowLE.preferredHeight = 54;

            // Rank
            var rankObj = CreateText(mainRow.transform, "Rank", "#1", 34, TextAlignmentOptions.Left, accentColor);
            rankObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

            // Name
            var nameObj = CreateText(mainRow.transform, "Name", "Player", 30, TextAlignmentOptions.Left, textColor);
            nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 50);
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1;

            // Score
            var scoreObj = CreateText(mainRow.transform, "Score", "0", 30, TextAlignmentOptions.Right, dimTextColor);
            scoreObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            // Subtitle row (playstyle title or last words)
            var subtitleObj = CreateText(prefab.transform, "Subtitle", "", 20, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.6f, 0.6f));
            var subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(0, 28);
            var subLE = subtitleObj.AddComponent<LayoutElement>();
            subLE.preferredHeight = 28;
            var subTMP = subtitleObj.GetComponent<TextMeshProUGUI>();
            subTMP.fontStyle = FontStyles.Italic;
            subTMP.margin = new Vector4(32, 0, 12, 0); // indent to align under name

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
            rect.sizeDelta = new Vector2(0, 120);
            rect.anchoredPosition = Vector2.zero;

            var bg = header.AddComponent<Image>();
            bg.color = headerColor;

            var title = CreateText(header.transform, "Title", "THE BOARD", 48, TextAlignmentOptions.Center, accentColor);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.3f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, -20);
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var sub = CreateText(header.transform, "Subtitle", "You are a name and a number. Climb.", 22, TextAlignmentOptions.Center, dimTextColor);
            var subRect = sub.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0);
            subRect.anchorMax = new Vector2(1, 0.35f);
            subRect.offsetMin = new Vector2(20, 5);
            subRect.offsetMax = new Vector2(-20, -5);
            sub.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        }

        private void BuildBoardVoice(Transform parent)
        {
            // Board voice — self-aware text that floats below header
            var voiceObj = new GameObject("BoardVoice");
            voiceObj.transform.SetParent(parent, false);

            var rect = voiceObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 50);
            rect.anchoredPosition = new Vector2(0, -120); // just below header

            boardVoiceCG = voiceObj.AddComponent<CanvasGroup>();
            boardVoiceCG.alpha = 0f;
            boardVoiceCG.blocksRaycasts = false;

            boardVoiceText = CreateText(voiceObj.transform, "VoiceText", "", 22, TextAlignmentOptions.Center, new Color(0.65f, 0.6f, 0.75f)).GetComponent<TextMeshProUGUI>();
            var vtRect = boardVoiceText.GetComponent<RectTransform>();
            vtRect.anchorMin = Vector2.zero;
            vtRect.anchorMax = Vector2.one;
            vtRect.offsetMin = new Vector2(30, 0);
            vtRect.offsetMax = new Vector2(-30, 0);
            boardVoiceText.fontStyle = FontStyles.Italic;
        }

        private void BuildSynthesisHUD(Transform parent)
        {
            // Synthesis HUD — sits between board voice and leaderboard
            var hud = new GameObject("SynthesisHUD");
            hud.transform.SetParent(parent, false);

            var rect = hud.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 70);
            rect.anchoredPosition = new Vector2(0, -170); // below board voice

            var bg = hud.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.9f);

            // Left section: Visibility meter
            var visContainer = new GameObject("VisibilityMeter");
            visContainer.transform.SetParent(hud.transform, false);
            var visRect = visContainer.AddComponent<RectTransform>();
            visRect.anchorMin = new Vector2(0, 0.15f);
            visRect.anchorMax = new Vector2(0.32f, 0.85f);
            visRect.offsetMin = new Vector2(15, 0);
            visRect.offsetMax = new Vector2(-5, 0);

            // Visibility bar background
            var visBg = new GameObject("VisBg");
            visBg.transform.SetParent(visContainer.transform, false);
            var visBgRect = visBg.AddComponent<RectTransform>();
            visBgRect.anchorMin = new Vector2(0, 0);
            visBgRect.anchorMax = new Vector2(1, 0.5f);
            visBgRect.offsetMin = Vector2.zero;
            visBgRect.offsetMax = Vector2.zero;
            var visBgImg = visBg.AddComponent<Image>();
            visBgImg.color = new Color(0.15f, 0.15f, 0.2f);

            // Visibility bar fill
            var visFillObj = new GameObject("VisFill");
            visFillObj.transform.SetParent(visBg.transform, false);
            var visFillRect = visFillObj.AddComponent<RectTransform>();
            visFillRect.anchorMin = Vector2.zero;
            visFillRect.anchorMax = Vector2.one;
            visFillRect.offsetMin = Vector2.zero;
            visFillRect.offsetMax = Vector2.zero;
            visibilityFill = visFillObj.AddComponent<Image>();
            visibilityFill.color = new Color(0.2f, 0.8f, 0.3f);
            visibilityFill.type = Image.Type.Filled;
            visibilityFill.fillMethod = Image.FillMethod.Horizontal;
            visibilityFill.fillAmount = 0f;

            // Visibility label
            visibilityLabel = CreateText(visContainer.transform, "VisLabel", "👁 0%", 20, TextAlignmentOptions.Left, dimTextColor).GetComponent<TextMeshProUGUI>();
            var visLabelRect = visibilityLabel.GetComponent<RectTransform>();
            visLabelRect.anchorMin = new Vector2(0, 0.5f);
            visLabelRect.anchorMax = new Vector2(1, 1);
            visLabelRect.offsetMin = Vector2.zero;
            visLabelRect.offsetMax = Vector2.zero;

            // Cost label
            costLabel = CreateText(visContainer.transform, "CostLabel", "COST: 1.0x", 16, TextAlignmentOptions.Left, dimTextColor).GetComponent<TextMeshProUGUI>();
            var costRect = costLabel.GetComponent<RectTransform>();
            // Place below the bar background area - need separate positioning
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(1, 0.15f);
            costRect.offsetMin = Vector2.zero;
            costRect.offsetMax = Vector2.zero;

            // Middle section: Rival count
            rivalCountLabel = CreateText(hud.transform, "RivalCount", "No rivals nearby", 20, TextAlignmentOptions.Center, dimTextColor).GetComponent<TextMeshProUGUI>();
            var rivalRect = rivalCountLabel.GetComponent<RectTransform>();
            rivalRect.anchorMin = new Vector2(0.32f, 0.15f);
            rivalRect.anchorMax = new Vector2(0.68f, 0.85f);
            rivalRect.offsetMin = new Vector2(5, 0);
            rivalRect.offsetMax = new Vector2(-5, 0);

            // Right section: Becoming
            becomingLabel = CreateText(hud.transform, "Becoming", "TAP TO DISCOVER WHO YOU ARE", 18, TextAlignmentOptions.Right, dimTextColor).GetComponent<TextMeshProUGUI>();
            var becRect = becomingLabel.GetComponent<RectTransform>();
            becRect.anchorMin = new Vector2(0.68f, 0.15f);
            becRect.anchorMax = new Vector2(1, 0.85f);
            becRect.offsetMin = new Vector2(5, 0);
            becRect.offsetMax = new Vector2(-15, 0);
            becomingLabel.fontStyle = FontStyles.Bold;
        }

        private void BuildLeaderboardArea(Transform parent)
        {
            var scrollObj = new GameObject("LeaderboardScroll");
            scrollObj.transform.SetParent(parent, false);

            var scrollRectT = scrollObj.AddComponent<RectTransform>();
            scrollRectT.anchorMin = new Vector2(0, 0);
            scrollRectT.anchorMax = new Vector2(1, 1);
            scrollRectT.offsetMin = new Vector2(0, 280);
            scrollRectT.offsetMax = new Vector2(0, -240); // extra space for board voice + synthesis HUD

            scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 30f;

            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = backgroundColor;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRect;

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
            playerRankText = CreateText(bar.transform, "PlayerRank", "#31", 44, TextAlignmentOptions.Left, accentColor).GetComponent<TextMeshProUGUI>();
            var prRect = playerRankText.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0, 0.35f);
            prRect.anchorMax = new Vector2(0.18f, 1);
            prRect.offsetMin = new Vector2(20, 0);
            prRect.offsetMax = new Vector2(-5, -10);
            playerRankText.fontStyle = FontStyles.Bold;

            // Name
            playerNameText = CreateText(bar.transform, "PlayerName", "YOU", 32, TextAlignmentOptions.Left, textColor).GetComponent<TextMeshProUGUI>();
            var pnRect = playerNameText.GetComponent<RectTransform>();
            pnRect.anchorMin = new Vector2(0.18f, 0.5f);
            pnRect.anchorMax = new Vector2(0.6f, 1);
            pnRect.offsetMin = new Vector2(10, 0);
            pnRect.offsetMax = new Vector2(-5, -10);
            playerNameText.fontStyle = FontStyles.Bold;

            // Playstyle title under name
            playerTitleText = CreateText(bar.transform, "PlayerTitle", "", 22, TextAlignmentOptions.Left, new Color(1f, 0.75f, 0.3f, 0.8f)).GetComponent<TextMeshProUGUI>();
            var ptRect = playerTitleText.GetComponent<RectTransform>();
            ptRect.anchorMin = new Vector2(0.18f, 0);
            ptRect.anchorMax = new Vector2(0.6f, 0.5f);
            ptRect.offsetMin = new Vector2(10, 10);
            ptRect.offsetMax = new Vector2(-5, 0);
            playerTitleText.fontStyle = FontStyles.Italic;

            // Score
            playerScoreText = CreateText(bar.transform, "PlayerScore", "0", 40, TextAlignmentOptions.Right, accentColor).GetComponent<TextMeshProUGUI>();
            var psRect = playerScoreText.GetComponent<RectTransform>();
            psRect.anchorMin = new Vector2(0.6f, 0);
            psRect.anchorMax = new Vector2(1, 1);
            psRect.offsetMin = new Vector2(5, 10);
            psRect.offsetMax = new Vector2(-20, -10);
        }

        private void BuildTapArea(Transform parent)
        {
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

            comboText = CreateText(tapArea.transform, "ComboText", "", 32, TextAlignmentOptions.Center, new Color(1f, 0.5f, 0f)).GetComponent<TextMeshProUGUI>();
            var comboRect = comboText.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.65f, 0.15f);
            comboRect.anchorMax = new Vector2(0.95f, 0.55f);
            comboRect.offsetMin = Vector2.zero;
            comboRect.offsetMax = Vector2.zero;
            comboText.fontStyle = FontStyles.Bold;

            pointsPerTapText = CreateText(tapArea.transform, "PointsText", "+10", 28, TextAlignmentOptions.Center, dimTextColor).GetComponent<TextMeshProUGUI>();
            var ptsRect = pointsPerTapText.GetComponent<RectTransform>();
            ptsRect.anchorMin = new Vector2(0.65f, 0.55f);
            ptsRect.anchorMax = new Vector2(0.95f, 0.85f);
            ptsRect.offsetMin = Vector2.zero;
            ptsRect.offsetMax = Vector2.zero;
        }

        private TextMeshProUGUI chargeText;
        private Image chargeFillImage;

        private void BuildChargeHUD(Transform parent)
        {
            // Charge display sits above the tap area
            var hud = new GameObject("ChargeHUD");
            hud.transform.SetParent(parent, false);

            var rect = hud.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, 40);
            rect.anchoredPosition = new Vector2(0, 155); // just above player bar

            // Bar background
            var barBg = new GameObject("ChargeBarBg");
            barBg.transform.SetParent(hud.transform, false);
            var barBgRect = barBg.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.05f, 0.2f);
            barBgRect.anchorMax = new Vector2(0.7f, 0.8f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.12f, 0.12f, 0.18f);

            // Bar fill
            var barFill = new GameObject("ChargeBarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            chargeFillImage = barFill.AddComponent<Image>();
            chargeFillImage.color = new Color(0.2f, 1f, 0.4f);
            chargeFillImage.type = Image.Type.Filled;
            chargeFillImage.fillMethod = Image.FillMethod.Horizontal;
            chargeFillImage.fillAmount = 1f;

            // Text label
            chargeText = CreateText(hud.transform, "ChargeLabel", "\u26a1 10/10", 26, TextAlignmentOptions.Right, accentColor).GetComponent<TextMeshProUGUI>();
            var ctRect = chargeText.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.7f, 0);
            ctRect.anchorMax = new Vector2(0.98f, 1);
            ctRect.offsetMin = Vector2.zero;
            ctRect.offsetMax = Vector2.zero;
            chargeText.fontStyle = FontStyles.Bold;

            // Update loop via a simple MonoBehaviour
            var updater = hud.AddComponent<ChargeHUDUpdater>();
            updater.Init(chargeFillImage, chargeText);
        }

        private void WireUpManagers()
        {
            // LeaderboardManager
            var managerObj = new GameObject("LeaderboardManager");
            managerObj.AddComponent<LeaderboardManager>();

            // NarrativeSystem
            var narrativeObj = new GameObject("NarrativeSystem");
            var narrative = narrativeObj.AddComponent<NarrativeSystem>();
            narrative.Init(boardVoiceText, boardVoiceCG);

            // LeaderboardUI with narrative support
            var uiObj = new GameObject("LeaderboardUI");
            var ui = uiObj.AddComponent<LeaderboardUIRuntime>();
            ui.Init(entryParent, entryPrefab, scrollRect, playerRankText, playerScoreText, playerNameText, playerTitleText,
                    accentColor, top3Color, entryColor, entryAltColor, playerEntryColor, textColor, dimTextColor);
            ui.SetSynthesisUI(visibilityFill, visibilityLabel, costLabel, rivalCountLabel, becomingLabel);

            // PlayerController
            var playerObj = new GameObject("PlayerController");
            var player = playerObj.AddComponent<PlayerController>();
            SetPrivateField(player, "tapButton", tapButton);
            SetPrivateField(player, "comboText", comboText);
            SetPrivateField(player, "pointsPerTapText", pointsPerTapText);

            tapButton.onClick.AddListener(player.OnTap);
            Debug.Log($"[SceneBuilder] Button wired. tapButton={tapButton != null}, interactable={tapButton.interactable}");

            // TapFeedback
            var tapFeedback = playerObj.AddComponent<TapFeedback>();
            tapFeedback.Init(
                tapButton.GetComponent<RectTransform>(),
                mainCanvas.GetComponent<RectTransform>(),
                mainCanvas.transform
            );
            SetPrivateField(player, "tapFeedback", tapFeedback);

            // RankUpEffect
            var rankUpObj = new GameObject("RankUpEffect");
            var rankUpEffect = rankUpObj.AddComponent<RankUpEffect>();
            rankUpEffect.Init(mainCanvas.GetComponent<RectTransform>(), mainCanvas.transform);

            // LeaderboardAnimator
            var animatorObj = new GameObject("LeaderboardAnimator");
            var lbAnimator = animatorObj.AddComponent<LeaderboardAnimator>();
            var playerBarObj = GameObject.Find("PlayerBar");
            if (playerBarObj != null)
                lbAnimator.Init(playerBarObj.GetComponent<Image>());

            // ChargeManager
            var chargeObj = new GameObject("ChargeManager");
            chargeObj.AddComponent<ChargeManager>();

            // Charge HUD (in tap area)
            BuildChargeHUD(mainCanvas.transform);

            // ItemSystem
            var itemSystemObj = new GameObject("ItemSystem");
            itemSystemObj.AddComponent<ItemSystem>();

            // ItemUI
            var itemUIObj = new GameObject("ItemUI");
            var itemUI = itemUIObj.AddComponent<ItemUI>();
            itemUI.Init(mainCanvas.GetComponent<RectTransform>(), mainCanvas.transform, dimTextColor);

            // RankChangeDetector
            var detectorObj = new GameObject("RankChangeDetector");
            detectorObj.AddComponent<RankChangeDetector>();

            // AutoScreenshot
            var screenshotObj = new GameObject("AutoScreenshot");
            screenshotObj.AddComponent<AutoScreenshot>();

            // SpacetimeDB
            var stdbObj = new GameObject("SpacetimeDB");
            stdbObj.AddComponent<SpacetimeDBManager>();
            stdbObj.AddComponent<SpacetimeDB.SpacetimeDBNetworkManager>();

            // Online status
            var statusObj = new GameObject("OnlineStatus");
            var statusUI = statusObj.AddComponent<OnlineStatusUI>();
            var statusTextObj = CreateText(mainCanvas.transform, "StatusText", "● OFFLINE", 24, TextAlignmentOptions.Right, dimTextColor);
            var statusRect = statusTextObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(1, 1);
            statusRect.anchoredPosition = new Vector2(-20, -20);
            statusRect.sizeDelta = new Vector2(200, 40);
            SetPrivateField(statusUI, "statusText", statusTextObj.GetComponent<TextMeshProUGUI>());
        }

        private GameObject CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
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
