using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace LeaderboardGame
{
    /// <summary>
    /// Makes the leaderboard feel alive with ambient animations:
    /// - Smooth row sliding when ranks change
    /// - Subtle pulsing on the player's row
    /// - Score counter animation (rolls up like a slot machine)
    /// - Staggered entry appearance on first load
    /// </summary>
    public class LeaderboardAnimator : MonoBehaviour
    {
        private Color playerEntryColor = new Color(0.25f, 0.2f, 0.05f);
        private Color accentColor = new Color(1f, 0.84f, 0f);
        private Image playerBarBg;
        private float pulseTimer;
        private bool initialized;

        public void Init(Image playerBar)
        {
            playerBarBg = playerBar;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || playerBarBg == null) return;

            // Subtle pulse on player bar
            pulseTimer += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(pulseTimer * 2f) * 0.03f;
            float alpha = Mathf.Lerp(0.9f, 1f, (Mathf.Sin(pulseTimer * 1.5f) + 1f) * 0.5f);

            playerBarBg.color = new Color(
                playerEntryColor.r * pulse,
                playerEntryColor.g * pulse,
                playerEntryColor.b * pulse,
                alpha
            );
        }

        /// <summary>
        /// Animate entries appearing with a staggered slide-in.
        /// Call this after populating the leaderboard for the first time.
        /// </summary>
        public void PlayEntranceAnimation(List<GameObject> entries)
        {
            StartCoroutine(StaggeredEntrance(entries));
        }

        private IEnumerator StaggeredEntrance(List<GameObject> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;

                var cg = entry.GetComponent<CanvasGroup>();
                if (cg == null) cg = entry.AddComponent<CanvasGroup>();

                var rect = entry.GetComponent<RectTransform>();
                if (rect == null) continue;

                cg.alpha = 0f;
                Vector2 originalPos = rect.anchoredPosition;
                rect.anchoredPosition = originalPos + new Vector2(-80f, 0);

                StartCoroutine(SlideIn(rect, cg, originalPos, 0.3f));

                yield return new WaitForSeconds(0.03f); // stagger delay
            }
        }

        private IEnumerator SlideIn(RectTransform rect, CanvasGroup cg, Vector2 targetPos, float duration)
        {
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                cg.alpha = ease;

                yield return null;
            }

            rect.anchoredPosition = targetPos;
            cg.alpha = 1f;
        }

        /// <summary>
        /// Flash a row green (rank up) or red (rank down).
        /// </summary>
        public void FlashRow(GameObject row, bool movedUp)
        {
            if (row == null) return;
            StartCoroutine(RowFlash(row, movedUp));
        }

        private IEnumerator RowFlash(GameObject row, bool movedUp)
        {
            var bg = row.GetComponent<Image>();
            if (bg == null) yield break;

            Color originalColor = bg.color;
            Color flashColor = movedUp
                ? new Color(0.2f, 1f, 0.4f, 0.4f)
                : new Color(1f, 0.2f, 0.2f, 0.25f);

            bg.color = flashColor;

            float duration = 0.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bg.color = Color.Lerp(flashColor, originalColor, elapsed / duration);
                yield return null;
            }

            bg.color = originalColor;
        }
    }
}
