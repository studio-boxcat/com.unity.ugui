using System.Runtime.CompilerServices;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    internal static class MultipleDisplayUtilities
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

        /// <summary>
        /// A version of Display.RelativeMouseAt that scales the position when the main display has a different rendering resolution to the system resolution.
        /// By default, the mouse position is relative to the main render area, we need to adjust this so it is relative to the system resolution
        /// in order to correctly determine the position on other displays.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RelativeMouseAtScaled(Vector2 position)
        {
            // Multi-display is not supported.
            return position;
        }
    }
}
