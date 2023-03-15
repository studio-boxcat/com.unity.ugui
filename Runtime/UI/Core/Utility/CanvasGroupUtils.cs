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

                // if the parent group does not allow interaction
                // we need to break
                if (canvasGroup.enabled && !canvasGroup.interactable)
                    return false;

                // if this is a 'fresh' group, then break
                // as we should not consider parents
                if (canvasGroup.ignoreParentGroups)
                    return true;

                t = t.parent;
            }

            return true;
        }
    }
}