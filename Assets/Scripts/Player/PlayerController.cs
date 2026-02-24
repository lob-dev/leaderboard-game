using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Simple player interaction — for POC, just a tap/click to gain points.
    /// The core loop: tap -> gain score -> climb leaderboard.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int basePointsPerTap = 10;
        [SerializeField] private float comboWindow = 1.5f;
        [SerializeField] private int maxComboMultiplier = 5;

        [Header("UI")]
        [SerializeField] private Button tapButton;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI pointsPerTapText;
        [SerializeField] private TapFeedback tapFeedback;

        private int comboCount = 0;
        private float comboTimer = 0f;
        private bool buttonBound = false;

        private void Start()
        {
            TryBindButton();
        }

        private void Update()
        {
            // Late-bind: tapButton may be set via reflection after Start()
            if (!buttonBound)
            {
                TryBindButton();
            }

            // Handle keyboard input (Space key)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnTap();
            }

            // Combo decay
            if (comboCount > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0)
                {
                    comboCount = 0;
                    UpdateComboUI();
                }
            }
        }

        public void OnTap()
        {
            // Build combo
            comboCount = Mathf.Min(comboCount + 1, maxComboMultiplier);
            comboTimer = comboWindow;

            // Calculate points
            int points = basePointsPerTap * Mathf.Max(1, comboCount);

            // Add to leaderboard
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.AddPlayerScore(points);
            }

            UpdateComboUI();

            // Juicy tap feedback
            if (tapFeedback != null)
            {
                Vector3 tapPos = tapButton != null ? tapButton.transform.position : Input.mousePosition;
                tapFeedback.PlayTapFeedback(points, comboCount, tapPos);
            }
        }

        private void TryBindButton()
        {
            if (tapButton != null && !buttonBound)
            {
                tapButton.onClick.AddListener(OnTap);
                buttonBound = true;
            }
        }

        private void UpdateComboUI()
        {
            if (comboText != null)
            {
                comboText.text = comboCount > 1 ? $"x{comboCount} COMBO!" : "";
            }
            if (pointsPerTapText != null)
            {
                int pts = basePointsPerTap * Mathf.Max(1, comboCount);
                pointsPerTapText.text = $"+{pts}";
            }
        }
    }
}
