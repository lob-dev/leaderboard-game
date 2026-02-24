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
            Debug.Log($"[PlayerController] OnTap called! Current score will increase by {basePointsPerTap}");
            // Build combo
            comboCount = Mathf.Min(comboCount + 1, maxComboMultiplier);
            comboTimer = comboWindow;

            // Calculate points (with item modifiers)
            int comboMult = Mathf.Max(1, comboCount);
            if (ItemSystem.Instance != null)
                comboMult *= ItemSystem.Instance.GetComboBonusMultiplier();
            int points = basePointsPerTap * comboMult;
            if (ItemSystem.Instance != null)
                points *= ItemSystem.Instance.GetScoreMultiplier();

            // === OBSERVER EFFECT: Cost scaling based on visibility ===
            // Higher rank = higher cost multiplier = points are worth less (harder to climb)
            // Lower rank = lower cost = points are worth more (easier to climb)
            if (NarrativeSystem.Instance != null)
            {
                float costMult = NarrativeSystem.Instance.GetLocalPlayerCostMultiplier();
                // Invert: high cost = less effective points
                points = Mathf.Max(1, Mathf.RoundToInt(points / costMult));
            }

            // === PROXIMITY RIVALRY: +25% damage vs rivals ===
            // When you have rivals nearby, your taps are more effective
            if (LeaderboardManager.Instance != null)
            {
                var player = LeaderboardManager.Instance.GetLocalPlayer();
                if (player != null && player.RivalCount > 0)
                {
                    float rivalBonus = 1.25f;
                    points = Mathf.RoundToInt(points * rivalBonus);
                }
            }

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
                int comboMult = Mathf.Max(1, comboCount);
                if (ItemSystem.Instance != null)
                    comboMult *= ItemSystem.Instance.GetComboBonusMultiplier();
                int pts = basePointsPerTap * comboMult;
                if (ItemSystem.Instance != null)
                    pts *= ItemSystem.Instance.GetScoreMultiplier();
                // Apply observer effect preview
                if (NarrativeSystem.Instance != null)
                {
                    float costMult = NarrativeSystem.Instance.GetLocalPlayerCostMultiplier();
                    pts = Mathf.Max(1, Mathf.RoundToInt(pts / costMult));
                }
                // Apply rival bonus preview
                if (LeaderboardManager.Instance != null)
                {
                    var player = LeaderboardManager.Instance.GetLocalPlayer();
                    if (player != null && player.RivalCount > 0)
                        pts = Mathf.RoundToInt(pts * 1.25f);
                }
                string suffix = "";
                if (NarrativeSystem.Instance != null)
                {
                    float cost = NarrativeSystem.Instance.GetLocalPlayerCostMultiplier();
                    if (cost > 1.5f) suffix = " ⚡HARD";
                    else if (cost < 0.9f) suffix = " ✦EASY";
                }
                if (LeaderboardManager.Instance != null)
                {
                    var p = LeaderboardManager.Instance.GetLocalPlayer();
                    if (p != null && p.RivalCount > 0)
                        suffix += " 🔴RIVAL";
                }
                pointsPerTapText.text = $"+{pts}{suffix}";
            }
        }
    }
}
