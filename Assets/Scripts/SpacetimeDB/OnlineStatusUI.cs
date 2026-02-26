using UnityEngine;
using TMPro;
using System.Linq;

namespace LeaderboardGame
{
    /// <summary>
    /// Shows the online/offline connection status and player count in the UI.
    /// </summary>
    public class OnlineStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;

        private float updateTimer;
        private const float UPDATE_INTERVAL = 1f;

        private void Update()
        {
            if (statusText == null) return;

            updateTimer += Time.deltaTime;
            if (updateTimer < UPDATE_INTERVAL) return;
            updateTimer = 0f;

            if (SpacetimeDBManager.Instance != null && SpacetimeDBManager.Instance.IsConnected)
            {
                int onlineCount = 0;
                int totalCount = 0;
                if (LeaderboardManager.Instance != null)
                {
                    var entries = LeaderboardManager.Instance.GetEntries();
                    onlineCount = entries.Count(e => e.IsOnlinePlayer && e.IsOnlineConnected);
                    totalCount = entries.Count(e => e.IsOnlinePlayer);
                }
                statusText.text = $"<color=#00FF00>● ONLINE</color> <color=#AAAAAA>{onlineCount}/{totalCount} players</color>";
            }
            else
            {
                statusText.text = "<color=#FF6600>● OFFLINE</color> <color=#AAAAAA>solo mode</color>";
            }
        }
    }
}
