using UnityEngine;
using TMPro;
using System.Collections;

namespace LeaderboardGame
{
    /// <summary>
    /// Floating "+points" text that appears on tap and floats upward.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float floatSpeed = 100f;
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 0.2f, 1.2f);

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        public void Show(int points, Vector3 worldPos, bool isCombo = false)
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            text.text = $"+{points}";
            if (isCombo)
            {
                text.color = new Color(1f, 0.5f, 0f); // Orange for combos
                text.fontSize *= 1.3f;
            }

            rectTransform.position = worldPos;
            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            float elapsed = 0f;
            Vector3 startPos = rectTransform.position;

            while (elapsed < lifetime)
            {
                float t = elapsed / lifetime;
                rectTransform.position = startPos + Vector3.up * (floatSpeed * elapsed);
                canvasGroup.alpha = fadeCurve.Evaluate(t);
                transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
