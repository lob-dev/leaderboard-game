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
        private Image fillImage;
        private TextMeshProUGUI label;

        public void Init(Image fill, TextMeshProUGUI text)
        {
            fillImage = fill;
            label = text;
        }

        private void Update()
        {
            if (ChargeManager.Instance == null || fillImage == null || label == null) return;

            var cm = ChargeManager.Instance;
            float pct = cm.FillPercent;
            fillImage.fillAmount = pct;
            fillImage.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.4f), pct);
            label.text = $"\u26a1 {cm.CurrentCharges}/{cm.MaxCharges}";

            // Dim label when empty
            label.color = cm.HasCharge
                ? new Color(1f, 0.84f, 0f)
                : new Color(1f, 0.3f, 0.3f);
        }
    }
}
