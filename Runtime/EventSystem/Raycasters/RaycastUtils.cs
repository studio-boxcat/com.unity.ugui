using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public static class RaycastUtils
    {
        public static bool TranslateScreenPosition(
            Vector2 screenPosition, Camera eventCamera, out Vector2 eventPosition)
        {
            var eventPosition3 = MultipleDisplayUtilities.RelativeMouseAtScaled(screenPosition);
            var targetDisplay = eventCamera.targetDisplay;

            if (!eventPosition3.Equals(default))
            {
                // We support multiple display and display identification based on event position.
                var eventDisplayIndex = (int) eventPosition3.z;

                // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                if (eventDisplayIndex != targetDisplay)
                {
                    eventPosition = default;
                    return false;
                }

                eventPosition = eventPosition3;
            }
            else
            {
#if UNITY_EDITOR
                if (Display.activeEditorGameViewTarget != targetDisplay)
                {
                    eventPosition = default;
                    return false;
                }
#endif

                // The multiple display system is not supported on all platforms, when it is not supported the returned position
                // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
                eventPosition = screenPosition;

                // We dont really know in which display the event occured. We will process the event assuming it occured in our display.
            }

            // Convert to view space
            Vector2 pos = eventCamera.ScreenToViewportPoint(eventPosition);

            // Check if the event is inside the camera's viewport.
            return pos.x is >= 0f and <= 1f && pos.y is >= 0f and <= 1f;
        }
    }
}