using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class ComponentSearch
    {
        public static T SearchActiveAndEnabledParentOrSelfComponent<T>(Component root) where T : Behaviour
        {
            while (true)
            {
                var comp = root.GetComponentInParent<T>(false);

                // Reached top of hierarchy, break.
                if (comp is null)
                    return null;

                // Found valid component, break.
                if (comp.isActiveAndEnabled)
                    return comp;

                // Go up hierarchy.
                root = comp.transform.parent;
                if (root is null) // Reached top of hierarchy, break.
                    return null;
            }
        }

        // Dedicated component buffer for ValidController().
        static readonly List<Component> _compBuf = new();

        public static bool AnyActiveAndEnabledComponent<T>(Transform target)
        {
            // Before get entire component list, check if the target has any component.

            // When there is no component at all.
            if (target.TryGetComponent(typeof(T), out var found) == false)
                return false;

            // When the target has a component and it is enabled.
            if (((Behaviour) found).isActiveAndEnabled)
                return true;

            // Get all components, and check if there is any enabled one.
            target.GetComponents(typeof(T), _compBuf);

            // If there is only one component found, it must be the one we already checked.
            if (_compBuf.Count == 1)
                return false;

            foreach (var cur in _compBuf)
            {
                // Skip the one we already checked.
                if (ReferenceEquals(cur, found))
                    continue;
                if (((Behaviour) cur).isActiveAndEnabled)
                    return true;
            }

            return false;
        }
    }
}