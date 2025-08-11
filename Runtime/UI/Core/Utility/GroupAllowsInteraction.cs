namespace UnityEngine.UI
{
    /// only checks the CanvasGroup allows the interaction or not.
    public struct GroupAllowsInteraction
    {
        private bool _valid;
        private bool _interactable;

        public void SetDirty() => _valid = false;

        public bool IsInteractable(Component component)
        {
            if (_valid) return _interactable;
            Reevaluate(component);
            return _interactable;
        }

        public bool Reevaluate(Component component)
        {
            _interactable = IsInteractionAllowed(component.transform);
            _valid = true;
            return _interactable;
        }

        private static bool IsInteractionAllowed(Transform t)
        {
            while (t is not null)
            {
                // includes inactive GameObject too, as we only cares about whether the group allows interaction or not.
                // only checks the CanvasGroup component is enabled or not.
                var canvasGroup = ComponentSearch.NearestUpwards_GOAnyAndCompEnabled<CanvasGroup>(t);
                if (canvasGroup is null)
                    return true;

                // Interaction is not allowed if the group is not interactable.
                if (canvasGroup.interactable is false)
                    return false;

                // If ignoreParentGroups is true, we can safely skip the parent groups.
                if (canvasGroup.ignoreParentGroups)
                    return true;

                t = t.parent;
            }

            return true;
        }
    }
}