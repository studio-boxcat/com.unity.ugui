namespace UnityEngine.EventSystems
{
    public static class RaycastUtils
    {
        public static bool IsInside(Camera camera, Vector2 screenPosition)
        {
            // Convert to view space
            Vector2 pos = camera.ScreenToViewportPoint(screenPosition);

            // Check if the event is inside the camera's viewport.
            return pos.x is >= 0f and <= 1f && pos.y is >= 0f and <= 1f;
        }
    }
}