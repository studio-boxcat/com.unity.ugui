namespace UnityEngine.UI
{
    public struct InteractabilityResolver
    {
        bool _valid;
        bool _interactable;

        public void SetDirty() => _valid = false;

        public bool IsInteractable(Component component)
        {
            if (_valid) return _interactable;
            Reevaluate(component.transform);
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
                var canvasGroup = ComponentSearch.SearchEnabledParentOrSelfComponent<CanvasGroup>(t);
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