using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Attaches to individual leaderboard rows to provide ambient animations:
    /// - Subtle breathing (alpha oscillation) for all rows
    /// - Gentle horizontal sway for ghost entries  
    /// - Pulsing border glow for rival entries
    /// - Score text shimmer for top-3 entries
    /// Each row gets a random phase offset so they don't all pulse in sync.
    /// </summary>
    public class RowAnimator : MonoBehaviour
    {
        public enum RowType { Normal, Top3, Rival, Ghost }

        private RowType rowType;
        private Image background;
        private Color baseColor;
        private TextMeshProUGUI scoreText;
        private Color baseScoreColor;
        private RectTransform rectTransform;
        private float phaseOffset;
        private float timer;

        // Ghost sway
        private Vector2 basePosition;
        private bool positionCaptured;

        // Rival glow
        private Image rivalBorderImage;

        public void Init(RowType type, Image bg, TextMeshProUGUI score, Image rivalBorder = null)
        {
            rowType = type;
            background = bg;
            baseColor = bg != null ? bg.color : Color.clear;
            scoreText = score;
            baseScoreColor = score != null ? score.color : Color.white;
            rectTransform = GetComponent<RectTransform>();
            rivalBorderImage = rivalBorder;

            // Random phase so rows don't breathe in unison
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = timer + phaseOffset;

            // Capture position after layout settles (one frame delay)
            if (!positionCaptured && rectTransform != null)
            {
                basePosition = rectTransform.anchoredPosition;
                positionCaptured = true;
            }

            switch (rowType)
            {
                case RowType.Normal:
                    AnimateNormal(t);
                    break;
                case RowType.Top3:
                    AnimateTop3(t);
                    break;
                case RowType.Rival:
                    AnimateRival(t);
                    break;
                case RowType.Ghost:
                    AnimateGhost(t);
                    break;
            }
        }

        private void AnimateNormal(float t)
        {
            // Very subtle alpha breathing
            if (background != null)
            {
                float breath = 1f + Mathf.Sin(t * 1.2f) * 0.02f;
                background.color = new Color(
                    baseColor.r * breath,
                    baseColor.g * breath,
                    baseColor.b * breath,
                    baseColor.a
                );
            }
        }

        private void AnimateTop3(float t)
        {
            // Score text shimmer — subtle brightness oscillation
            if (scoreText != null)
            {
                float shimmer = 1f + Mathf.Sin(t * 2.5f) * 0.15f;
                scoreText.color = new Color(
                    Mathf.Min(baseScoreColor.r * shimmer, 1f),
                    Mathf.Min(baseScoreColor.g * shimmer, 1f),
                    Mathf.Min(baseScoreColor.b * shimmer, 1f),
                    baseScoreColor.a
                );
            }

            // Background glow
            if (background != null)
            {
                float glow = 1f + Mathf.Sin(t * 1.8f) * 0.03f;
                background.color = new Color(
                    baseColor.r * glow,
                    baseColor.g * glow,
                    baseColor.b * glow,
                    baseColor.a
                );
            }
        }

        private void AnimateRival(float t)
        {
            // Pulsing background — more aggressive than normal
            if (background != null)
            {
                float pulse = 1f + Mathf.Sin(t * 3f) * 0.05f;
                background.color = new Color(
                    Mathf.Min(baseColor.r * pulse, 1f),
                    baseColor.g,
                    baseColor.b,
                    baseColor.a
                );
            }

            // Rival border alpha pulse
            if (rivalBorderImage != null)
            {
                float borderAlpha = 0.05f + Mathf.Sin(t * 2.5f) * 0.05f;
                Color c = rivalBorderImage.color;
                rivalBorderImage.color = new Color(c.r, c.g, c.b, Mathf.Max(0f, borderAlpha));
            }
        }

        private void AnimateGhost(float t)
        {
            // Ethereal alpha fade in/out
            if (background != null)
            {
                float fade = Mathf.Lerp(0.4f, 0.7f, (Mathf.Sin(t * 0.8f) + 1f) * 0.5f);
                background.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * fade);
            }

            // Gentle horizontal drift
            if (positionCaptured && rectTransform != null)
            {
                float drift = Mathf.Sin(t * 0.6f) * 2f;
                rectTransform.anchoredPosition = basePosition + new Vector2(drift, 0f);
            }
        }
    }
}
