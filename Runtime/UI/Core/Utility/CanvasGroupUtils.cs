using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public static class CanvasGroupUtils
    {
        public static bool IsInteractionAllowed(Transform t)
        {
            while (t is not null)
            {
                var canvasGroup = ComponentSearch.SearchActiveAndEnabledParentOrSelfComponent<CanvasGroup>(t);
                if (canvasGroup is null)
                    return true;

                // Interaction is not allowed if the group is not interactable.
                if (canvasGroup.interactable == false)
                    return false;

                // If ignoreParentGroups is true, we should not consider the parent groups.
                if (canvasGroup.ignoreParentGroups)
                    return true;

                t = t.parent;
            }

            return true;
        }
    }
}