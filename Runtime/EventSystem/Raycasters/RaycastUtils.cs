using System.Collections.Generic;

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


        static readonly List<ICanvasRaycastFilter> _raycastFilterBuf = new();

        /// <summary>
        /// Is the given RectTransform valid for a raycast event?
        /// For instance, the masked areas should not be valid for a raycast.
        /// </summary>
        /// <param name="sp">Screen point being tested</param>
        /// <param name="eventCamera">Camera that is being used for the testing.</param>
        /// <returns>True if the provided point is a valid location for GraphicRaycaster raycasts.</returns>
        public static bool IsEligibleForRaycast(Transform transform, Vector2 sp, Camera eventCamera)
        {
            var t = transform;
            var ignoreParentGroups = false;
            while (t is not null)
            {
                // For most cases, there's no ICanvasRaycastFilter so we can avoid the GetComponents call.
                if (t.TryGetComponent(out ICanvasRaycastFilter _))
                {
                    t.GetComponents(_raycastFilterBuf);
                    foreach (var filter in _raycastFilterBuf)
                    {
                        // If the filter is disabled, skip it.
                        if (filter is Behaviour {enabled: false})
                            continue;

                        // Skip if we've set ignoreParentGroups to true.
                        if (filter is CanvasGroup group)
                        {
                            if (ignoreParentGroups)
                                continue;
                            if (group.ignoreParentGroups)
                                ignoreParentGroups = true;
                        }

                        // If any filter says it's not valid, return false.
                        if (filter.IsRaycastLocationValid(sp, eventCamera) == false)
                            return false;
                    }
                }


                // If canvas.overrideSorting is set, raycast traversal should stop at the canvas.
                if (t.TryGetComponent(out Canvas canvas) && canvas.overrideSorting)
                    break;


                // Go up the hierarchy.
                t = t.parent;
            }

            return true;
        }

    }
}