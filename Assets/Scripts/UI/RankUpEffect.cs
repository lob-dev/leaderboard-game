using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Celebratory effect when the player climbs ranks.
    /// Flash overlay, rank text animation, and streak tracking.
    /// </summary>
    public class RankUpEffect : MonoBehaviour
    {
        private RectTransform canvasRect;
        private Transform effectParent;
        private Color accentColor = new Color(1f, 0.84f, 0f);

        private int lastRank = -1;
        private int climbStreak = 0;

        public void Init(RectTransform canvas, Transform parent)
        {
            canvasRect = canvas;
            effectParent = parent;
        }

        private void OnEnable()
        {
            StartCoroutine(WaitForManager());
        }

        private System.Collections.IEnumerator WaitForManager()
        {
            while (LeaderboardManager.Instance == null)
                yield return null;

            LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(OnRankChanged);
            lastRank = LeaderboardManager.Instance.GetPlayerRank();
        }

        private void OnDisable()
        {
            if (LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.OnPlayerRankChanged.RemoveListener(OnRankChanged);
        }

        private void OnRankChanged(int newRank)
        {
            if (lastRank == -1)
            {
                lastRank = newRank;
                return;
            }

            if (newRank < lastRank)
            {
                // Moved up!
                int positions = lastRank - newRank;
                climbStreak += positions;

                SpawnRankUpBanner(newRank, positions);

                // Big celebration for entering top 10
                if (newRank <= 10 && lastRank > 10)
                    StartCoroutine(FlashOverlay(new Color(1f, 0.84f, 0f, 0.3f), 0.4f));
                // Top 3 entry is even bigger
                else if (newRank <= 3 && lastRank > 3)
                    StartCoroutine(FlashOverlay(new Color(0.6f, 0.4f, 1f, 0.4f), 0.6f));
                else
                    StartCoroutine(FlashOverlay(new Color(0.2f, 1f, 0.4f, 0.15f), 0.25f));
            }
            else if (newRank > lastRank)
            {
                // Dropped — reset streak
                climbStreak = 0;
                StartCoroutine(FlashOverlay(new Color(1f, 0.2f, 0.2f, 0.1f), 0.2f));
            }

            lastRank = newRank;
        }

        private void SpawnRankUpBanner(int newRank, int positionsGained)
        {
            if (effectParent == null) return;

            var bannerObj = new GameObject("RankUpBanner");
            bannerObj.transform.SetParent(effectParent, false);

            var rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.45f);
            rect.anchorMax = new Vector2(0.9f, 0.55f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = bannerObj.AddComponent<TextMeshProUGUI>();
            
            if (newRank == 1)
                tmp.text = "👑 #1 — YOU'RE ON TOP!";
            else if (newRank <= 3)
                tmp.text = $"🔥 #{newRank} — TOP 3!";
            else if (positionsGained >= 5)
                tmp.text = $"🚀 #{newRank} — {positionsGained} RANKS UP!";
            else
                tmp.text = $"↑ #{newRank}";

            tmp.fontSize = positionsGained >= 3 ? 52 : 40;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = accentColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;

            var cg = bannerObj.AddComponent<CanvasGroup>();
            StartCoroutine(AnimateBanner(rect, cg));
        }

        private IEnumerator AnimateBanner(RectTransform rect, CanvasGroup cg)
        {
            float lifetime = 1.2f;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one * 0.3f;
            Vector3 peakScale = Vector3.one * 1.15f;
            Vector3 normalScale = Vector3.one;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Scale: pop in, settle, then fade
                if (t < 0.15f)
                    rect.localScale = Vector3.Lerp(startScale, peakScale, t / 0.15f);
                else if (t < 0.3f)
                    rect.localScale = Vector3.Lerp(peakScale, normalScale, (t - 0.15f) / 0.15f);
                else
                    rect.localScale = normalScale;

                // Fade: full opacity, then fade out
                cg.alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);

                yield return null;
            }

            Destroy(rect.gameObject);
        }

        private IEnumerator FlashOverlay(Color flashColor, float duration)
        {
            if (effectParent == null) yield break;

            var flashObj = new GameObject("FlashOverlay");
            flashObj.transform.SetParent(effectParent, false);

            var rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = flashObj.AddComponent<Image>();
            img.color = flashColor;
            img.raycastTarget = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                img.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1f - t));
                yield return null;
            }

            Destroy(flashObj);
        }
    }
}
