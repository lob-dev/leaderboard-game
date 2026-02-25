using UnityEngine;
using UnityEngine.EventSystems;

namespace LeaderboardGame
{
    /// <summary>
    /// Detects begin/end drag on a ScrollRect to track user scroll interaction.
    /// </summary>
    public class ScrollDragDetector : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public System.Action onBeginDrag;
        public System.Action onEndDrag;

        public void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDrag?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDrag?.Invoke();
        }
    }
}
