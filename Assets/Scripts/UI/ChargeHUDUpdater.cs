using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Simple updater that syncs the charge HUD bar with ChargeManager state.
    /// </summary>
    public class ChargeHUDUpdater : MonoBehaviour
    {
        private RectTransform fillRect;
        private Image fillImage;
        private TextMeshProUGUI label;

        public void Init(Image fill, TextMeshProUGUI text)
        {
            fillImage = fill;
            fillRect = fill != null ? fill.GetComponent<RectTransform>() : null;
            label = text;
        }

        private void Update()
        {
            if (ChargeManager.Instance == null || fillRect == null || label == null) return;

            var cm = ChargeManager.Instance;
            float pct = cm.FillPercent;
            // Use anchor scaling instead of fillAmount (fillAmount needs a sprite to work)
            fillRect.anchorMax = new Vector2(pct, 1f);
            if (fillImage != null)
                fillImage.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.4f), pct);
            label.text = $"\u26a1 {cm.CurrentCharges}/{cm.MaxCharges}";

            // Dim label when empty
            label.color = cm.HasCharge
                ? new Color(1f, 0.84f, 0f)
                : new Color(1f, 0.3f, 0.3f);
        }
    }
}
