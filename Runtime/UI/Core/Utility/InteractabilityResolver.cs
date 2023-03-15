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
            _interactable = CanvasGroupUtils.IsInteractionAllowed(component.transform);
            _valid = true;
            return _interactable;
        }
    }
}