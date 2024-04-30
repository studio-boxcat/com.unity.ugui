using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine
{
    public static class ComponentSearch
    {
        [CanBeNull]
        public static T SearchEnabledParentOrSelfComponent<T>(Component root) where T : class
        {
            while (true)
            {
                var comp = root.GetComponentInParent(typeof(T), true); // Do not skip inactive parents.

                // Reached top of hierarchy, break.
                if (comp is null)
                    return null;

                // Found valid component, break.
                if (comp is Behaviour { enabled: true })
                    return comp as T;

                // Go up hierarchy.
                root = comp.transform.parent;
                if (root is null) // Reached top of hierarchy, break.
                    return null;
            }
        }

        // Dedicated component buffer for ValidController().
        static readonly List<Component> _compBuf = new();

        public static bool AnyEnabledComponent<T>(GameObject target)
        {
            // Before get entire component list, check if the target has any component.

            // When there is no component at all.
            if (target.TryGetComponent(typeof(T), out var found) is false)
                return false;

            // When the first found one is enabled.
            if (((Behaviour) found).enabled)
                return true;

            // Get all components, and check if there is any enabled one.
            target.GetComponents(typeof(T), _compBuf);

            // If there is only one component found, it must be the one we already checked.
            if (_compBuf.Count is 1)
                return false;

            foreach (var cur in _compBuf)
            {
                // Skip the one we already checked.
                if (ReferenceEquals(cur, found))
                    continue;

                // When component is a Behaviour, we need to check if it is enabled.
                if (((Behaviour) cur).enabled)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyEnabledComponent<T>(Transform target)
        {
            return AnyEnabledComponent<T>(target.gameObject);
        }

        public static void GetEnabledComponents<T>(Component target, List<Component> components)
        {
            Assert.AreEqual(0, components.Count);

            // Get all components.
            target.GetComponents(typeof(T), components);

            // Remove disabled components.
            for (var i = components.Count - 1; i >= 0; i--)
            {
                var comp = components[i];
                if (!((Behaviour) comp).isActiveAndEnabled)
                    components.RemoveAt(i);
            }
        }
    }
}