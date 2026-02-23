using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Juicy tap feedback: button punch, screen shake, and particle-like effects.
    /// Attach to the same GameObject as PlayerController or wire up via SceneBuilder.
    /// </summary>
    public class TapFeedback : MonoBehaviour
    {
        private RectTransform tapButtonRect;
        private RectTransform canvasRect;
        private Transform popupParent;
        private Color accentColor = new Color(1f, 0.84f, 0f);

        private Vector3 buttonOriginalScale;
        private Coroutine punchCoroutine;

        public void Init(RectTransform buttonRect, RectTransform canvas, Transform popupContainer)
        {
            tapButtonRect = buttonRect;
            canvasRect = canvas;
            popupParent = popupContainer;
            if (tapButtonRect != null)
                buttonOriginalScale = tapButtonRect.localScale;
        }

        /// <summary>
        /// Call this on every tap for juicy feedback.
        /// </summary>
        public void PlayTapFeedback(int points, int comboCount, Vector3 tapPosition)
        {
            // Button punch
            if (tapButtonRect != null)
            {
                if (punchCoroutine != null) StopCoroutine(punchCoroutine);
                punchCoroutine = StartCoroutine(PunchScale(tapButtonRect, comboCount));
            }

            // Floating score popup
            SpawnScorePopup(points, tapPosition, comboCount > 1);

            // Screen shake on high combos
            if (comboCount >= 3 && canvasRect != null)
            {
                StartCoroutine(ScreenShake(comboCount));
            }
        }

        private IEnumerator PunchScale(RectTransform target, int intensity)
        {
            float punchAmount = Mathf.Lerp(0.15f, 0.3f, (float)intensity / 5f);
            Vector3 punchedScale = buttonOriginalScale * (1f + punchAmount);
            target.localScale = punchedScale;

            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Overshoot curve for snappy feel
                float ease = 1f - Mathf.Pow(1f - t, 3f) * Mathf.Cos(t * Mathf.PI * 0.5f);
                target.localScale = Vector3.LerpUnclamped(punchedScale, buttonOriginalScale, ease);
                yield return null;
            }

            target.localScale = buttonOriginalScale;
        }

        private IEnumerator ScreenShake(int intensity)
        {
            float magnitude = Mathf.Lerp(3f, 12f, (float)intensity / 5f);
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 originalPos = canvasRect.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / duration);
                float x = Random.Range(-1f, 1f) * magnitude * t;
                float y = Random.Range(-1f, 1f) * magnitude * t;
                canvasRect.localPosition = originalPos + new Vector3(x, y, 0);
                yield return null;
            }

            canvasRect.localPosition = originalPos;
        }

        private void SpawnScorePopup(int points, Vector3 position, bool isCombo)
        {
            if (popupParent == null) return;

            var popupObj = new GameObject("ScorePopup");
            popupObj.transform.SetParent(popupParent, false);

            var rect = popupObj.AddComponent<RectTransform>();
            rect.position = position + new Vector3(Random.Range(-30f, 30f), 20f, 0);

            var tmp = popupObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"+{points}";
            tmp.fontSize = isCombo ? 42 : 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isCombo ? new Color(1f, 0.5f, 0f) : accentColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;

            rect.sizeDelta = new Vector2(200, 60);

            var cg = popupObj.AddComponent<CanvasGroup>();

            StartCoroutine(AnimatePopup(rect, cg));
        }

        private IEnumerator AnimatePopup(RectTransform rect, CanvasGroup cg)
        {
            float lifetime = 0.8f;
            float elapsed = 0f;
            Vector3 startPos = rect.localPosition;
            float startScale = 0.5f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Float up
                rect.localPosition = startPos + Vector3.up * (120f * t);

                // Scale: pop in then shrink
                float scale = t < 0.15f
                    ? Mathf.Lerp(startScale, 1.2f, t / 0.15f)
                    : Mathf.Lerp(1.2f, 0.8f, (t - 0.15f) / 0.85f);
                rect.localScale = Vector3.one * scale;

                // Fade out
                cg.alpha = t < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);

                yield return null;
            }

            Destroy(rect.gameObject);
        }
    }
}
