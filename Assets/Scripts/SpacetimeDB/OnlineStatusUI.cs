using UnityEngine;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// Shows the online/offline connection status in the UI.
    /// </summary>
    public class OnlineStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;

        private void Update()
        {
            if (statusText == null) return;

            if (SpacetimeDBManager.Instance != null && SpacetimeDBManager.Instance.IsConnected)
            {
                statusText.text = "<color=#00FF00>* ONLINE</color>";
            }
            else
            {
                statusText.text = "<color=#FF6600>* OFFLINE</color>";
            }
        }
    }
}
