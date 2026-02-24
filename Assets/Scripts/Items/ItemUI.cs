using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Handles item UI: floating collectible bubbles and active-item status bar.
    /// Created by SceneBuilder; driven by ItemSystem events.
    /// </summary>
    public class ItemUI : MonoBehaviour
    {
        private RectTransform canvasRect;
        private Transform canvasTransform;
        private RectTransform activeBarRect; // horizontal bar showing active items
        private List<GameObject> activeIndicators = new List<GameObject>();
        private Color dimTextColor;

        public void Init(RectTransform canvas, Transform canvasTrans, Color dimText)
        {
            canvasRect = canvas;
            canvasTransform = canvasTrans;
            dimTextColor = dimText;

            // Build the active-items status bar (sits just below the header)
            BuildActiveBar();

            // Subscribe to ItemSystem events
            if (ItemSystem.Instance != null)
            {
                ItemSystem.Instance.OnItemSpawned.AddListener(OnItemSpawned);
                ItemSystem.Instance.OnItemCollected.AddListener(OnItemCollected);
                ItemSystem.Instance.OnItemExpired.AddListener(OnItemExpired);
            }
        }

        private void Update()
        {
            // Update active item timer displays
            if (ItemSystem.Instance == null) return;
            var active = ItemSystem.Instance.GetActiveItems();

            // Clean up old indicators for expired items
            for (int i = activeIndicators.Count - 1; i >= 0; i--)
            {
                if (activeIndicators[i] == null)
                {
                    activeIndicators.RemoveAt(i);
                    continue;
                }
                var tag = activeIndicators[i].GetComponent<ItemIndicatorTag>();
                if (tag != null && !active.ContainsKey(tag.Type))
                {
                    Destroy(activeIndicators[i]);
                    activeIndicators.RemoveAt(i);
                }
            }

            // Update remaining time text
            foreach (var indicator in activeIndicators)
            {
                if (indicator == null) continue;
                var tag = indicator.GetComponent<ItemIndicatorTag>();
                if (tag == null) continue;
                var timerText = tag.TimerText;
                if (timerText != null && active.ContainsKey(tag.Type))
                {
                    timerText.text = $"{active[tag.Type]:F1}s";
                }
            }
        }

        private void BuildActiveBar()
        {
            var bar = new GameObject("ActiveItemsBar");
            bar.transform.SetParent(canvasTransform, false);

            activeBarRect = bar.AddComponent<RectTransform>();
            activeBarRect.anchorMin = new Vector2(0, 1);
            activeBarRect.anchorMax = new Vector2(1, 1);
            activeBarRect.pivot = new Vector2(0.5f, 1);
            activeBarRect.sizeDelta = new Vector2(0, 50);
            activeBarRect.anchoredPosition = new Vector2(0, -140); // just below header

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(15, 15, 5, 5);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
        }

        private void OnItemSpawned(ItemType type, float lifetime, Vector2 normalizedPos)
        {
            StartCoroutine(SpawnCollectibleBubble(type, lifetime, normalizedPos));
        }

        private IEnumerator SpawnCollectibleBubble(ItemType type, float lifetime, Vector2 normalizedPos)
        {
            var data = ItemDefinitions.Get(type);

            // Create bubble
            var bubble = new GameObject($"Item_{type}");
            bubble.transform.SetParent(canvasTransform, false);

            var rect = bubble.AddComponent<RectTransform>();
            // Position from normalized coords
            float canvasW = canvasRect.rect.width;
            float canvasH = canvasRect.rect.height;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                (normalizedPos.x - 0.5f) * canvasW,
                (normalizedPos.y - 0.5f) * canvasH
            );
            rect.sizeDelta = new Vector2(90, 90);

            // Background circle
            var bg = bubble.AddComponent<Image>();
            bg.color = new Color(data.Color.r, data.Color.g, data.Color.b, 0.9f);

            // Make it a button for collection
            var btn = bubble.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() =>
            {
                if (ItemSystem.Instance != null)
                    ItemSystem.Instance.CollectItem(type);
                Destroy(bubble);
            });

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(bubble.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = data.Emoji;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            // Animate: bob up and down, then fade and destroy
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            float bobSpeed = 2f;
            float bobAmount = 15f;

            while (elapsed < lifetime && bubble != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Bob
                rect.anchoredPosition = startPos + Vector2.up * (Mathf.Sin(elapsed * bobSpeed) * bobAmount);

                // Pulse scale
                float scale = 1f + 0.1f * Mathf.Sin(elapsed * 3f);
                rect.localScale = Vector3.one * scale;

                // Fade in last second
                if (t > 0.7f)
                {
                    float fade = 1f - ((t - 0.7f) / 0.3f);
                    bg.color = new Color(data.Color.r, data.Color.g, data.Color.b, 0.9f * fade);
                    // Blink effect
                    bubble.SetActive(Mathf.Sin(elapsed * 12f) > 0f);
                }

                yield return null;
            }

            if (bubble != null)
                Destroy(bubble);
        }

        private void OnItemCollected(ItemType type)
        {
            var data = ItemDefinitions.Get(type);

            // Instant items: just show a flash notification
            if (data.Duration <= 0f)
            {
                StartCoroutine(ShowNotification(data));
                return;
            }

            // Duration items: add to active bar if not already there
            foreach (var ind in activeIndicators)
            {
                if (ind == null) continue;
                var tag = ind.GetComponent<ItemIndicatorTag>();
                if (tag != null && tag.Type == type) return; // already showing, timer refreshed in Update
            }

            AddActiveIndicator(type, data);
        }

        private void AddActiveIndicator(ItemType type, ItemDefinitions.ItemData data)
        {
            var indicator = new GameObject($"Active_{type}");
            indicator.transform.SetParent(activeBarRect, false);

            var rect = indicator.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);

            var bg = indicator.AddComponent<Image>();
            bg.color = new Color(data.Color.r, data.Color.g, data.Color.b, 0.6f);

            // Layout
            var layout = indicator.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            // Emoji label
            var emojiObj = new GameObject("Emoji");
            emojiObj.transform.SetParent(indicator.transform, false);
            var emojiRect = emojiObj.AddComponent<RectTransform>();
            emojiRect.sizeDelta = new Vector2(35, 36);
            var emojiTmp = emojiObj.AddComponent<TextMeshProUGUI>();
            emojiTmp.text = data.Emoji;
            emojiTmp.fontSize = 22;
            emojiTmp.alignment = TextAlignmentOptions.Center;
            emojiTmp.color = Color.white;

            // Timer text
            var timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(indicator.transform, false);
            var timerRect = timerObj.AddComponent<RectTransform>();
            timerRect.sizeDelta = new Vector2(60, 36);
            var timerTmp = timerObj.AddComponent<TextMeshProUGUI>();
            timerTmp.text = $"{data.Duration:F1}s";
            timerTmp.fontSize = 20;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;

            // Tag component for tracking
            var tag = indicator.AddComponent<ItemIndicatorTag>();
            tag.Type = type;
            tag.TimerText = timerTmp;

            activeIndicators.Add(indicator);
        }

        private IEnumerator ShowNotification(ItemDefinitions.ItemData data)
        {
            var notify = new GameObject("ItemNotification");
            notify.transform.SetParent(canvasTransform, false);

            var rect = notify.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.45f);
            rect.anchorMax = new Vector2(0.9f, 0.55f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = notify.AddComponent<Image>();
            bg.color = new Color(data.Color.r, data.Color.g, data.Color.b, 0.85f);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(notify.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{data.Emoji} {data.Description}";
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            var cg = notify.AddComponent<CanvasGroup>();

            // Animate in and out
            float showTime = 1.5f;
            float elapsed = 0f;
            while (elapsed < showTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / showTime;
                if (t < 0.15f) cg.alpha = t / 0.15f;
                else if (t > 0.7f) cg.alpha = 1f - ((t - 0.7f) / 0.3f);
                else cg.alpha = 1f;

                rect.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(elapsed * 5f));
                yield return null;
            }

            Destroy(notify);
        }

        private void OnItemExpired(ItemType type)
        {
            // Indicator cleanup happens in Update
        }
    }

    /// <summary>Tag component to track active item indicators.</summary>
    public class ItemIndicatorTag : MonoBehaviour
    {
        public ItemType Type;
        public TextMeshProUGUI TimerText;
    }
}
