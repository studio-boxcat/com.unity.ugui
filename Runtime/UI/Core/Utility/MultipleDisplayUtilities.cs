using System.Runtime.CompilerServices;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    static class MultipleDisplayUtilities
    {
        /// <summary>
        /// Converts the current drag position into a relative position for the display.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="position"></param>
        /// <returns>Returns true except when the drag operation is not on the same display as it originated</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetRelativeMousePositionForDrag(PointerEventData eventData, ref Vector2 position)
        {
            // Multi-display is not supported.
            position = eventData.position;
            return true;
        }
    }
}