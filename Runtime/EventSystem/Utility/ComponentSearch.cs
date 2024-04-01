using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.EventSystems
{
    public static class ComponentSearch
    {
        // Dedicated component buffer for ValidController().
        static readonly List<Component> _compBuf = new();

        public static bool AnyActiveAndEnabledComponent<T>(GameObject target)
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

                // When component is a Behaviour, we need to check if it is enabled.
                if (cur is Behaviour behaviour)
                {
                    if (behaviour.isActiveAndEnabled)
                        return true;
                }
                // If not, we can just return true as it's not possible to be disabled.
                else
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyActiveAndEnabledComponent<T>(Transform target)
        {
            return AnyActiveAndEnabledComponent<T>(target.gameObject);
        }
    }
}