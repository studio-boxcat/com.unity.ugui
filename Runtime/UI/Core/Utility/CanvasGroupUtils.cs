namespace UnityEngine.UI
{
    public static class CanvasGroupUtils
    {
        public static bool IsInteractionAllowed(Transform t)
        {
            while (t is not null)
            {
                if (t.TryGetComponent(out CanvasGroup canvasGroup) == false)
                {
                    t = t.parent;
                    continue;
                }

                // If the group is not enabled, we just ignore it.
                if (canvasGroup.enabled == false)
                {
                    t = t.parent;
                    continue;
                }

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