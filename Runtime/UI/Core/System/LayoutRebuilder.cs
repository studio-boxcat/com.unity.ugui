#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    public partial class LayoutRebuilder
    {
        /// <summary>
        /// Mark the given Transform as needing it's layout to be recalculated during the next layout pass.
        /// </summary>
        public static void SetDirty(Transform t) => CanvasUpdateRegistry.QueueLayoutNode(t);

        public static void SetDirty(MonoBehaviour comp) => SetDirty(comp.transform);

        // null if not found or already visited. returned Transform always has ILayoutController.
        // Case #1: has ILayoutGroup parent chain (p1, p2)
        // p3
        //   p2 (ILayoutGroup) *
        //     p1 (ILayoutGroup)
        //       target (ILayoutGroup)
        // Case #2: no ILayoutGroup parent chain, but has ILayoutController
        // p1
        //   target (ILayoutGroup) *
        // Case #3: has ILayoutController
        // p1
        //   target (ILayoutSelfController) *
        internal static Transform? ResolveUnvisitedLayoutRoot(Transform t, HashSet<Transform>? visited)
        {
            // L.I("[LayoutRebuilder] ResolveUnvisitedLayoutRoot: " + t.name, t);

            // skip if already visited.
            if (visited?.Add(t) is false)
                return null;

            // find the topmost ILayoutGroup in the parent chain.
            if (StopVisitOrClimbToTopMostGroupInParentChain(t, visited, out var topMostGroup))
                return topMostGroup;

            // if there is no ILayoutGroup in the parent chain,
            // check if the target itself is an ILayoutController. (ILayoutGroup or ILayoutSelfController)
            if (ComponentSearch.AnyEnabledComponent<ILayoutController>(t))
                return t;

            return null;

            // found transform must have ILayoutController
            // true if already visited or ILayoutGroup found.
            static bool StopVisitOrClimbToTopMostGroupInParentChain(
                Transform t, HashSet<Transform>? visited, out Transform? found)
            {
                found = null;

                var ptr = t.parent;
                while (ptr is not null)
                {
                    // already visited
                    if (visited?.Add(ptr) is false)
                        return true;

                    // will return false at the first parent in most cases
                    if (ComponentSearch.AnyEnabledComponent<ILayoutGroup>(ptr) is false)
                        return found is not null;

                    found = ptr;
                    ptr = ptr.parent; // climb up
                }

                return found is not null;
            }
        }

        /// <summary>
        /// Forces an immediate rebuild of the layout element and child layout elements affected by the calculations.
        /// </summary>
        /// <param name="layoutRoot">The layout element to perform the layout rebuild on.</param>
        /// <remarks>
        /// Normal use of the layout system should not use this method. Instead SetDirty should be used instead, which triggers a delayed layout rebuild during the next layout pass. The delayed rebuild automatically handles objects in the entire layout hierarchy in the correct order, and prevents multiple recalculations for the same layout elements.
        /// However, for special layout calculation needs, ::ref::RebuildRootImmediate can be used to get the layout of a sub-tree resolved immediately. This can even be done from inside layout calculation methods such as ILayoutController.SetLayoutHorizontal orILayoutController.SetLayoutVertical. Usage should be restricted to cases where multiple layout passes are unavaoidable despite the extra cost in performance.
        /// </remarks>
        internal static void RebuildRootImmediate(Transform layoutRoot)
        {
            L.I("[LayoutRebuilder] RebuildRootImmediate: " + layoutRoot.name, layoutRoot);

#if DEBUG
            if (!layoutRoot.gameObject.activeInHierarchy)
                L.E("[LayoutRebuilder] Attempting calculate layout for inactive object: " + layoutRoot.name, layoutRoot);
            if (layoutRoot.NoComponent<ILayoutController>())
                L.E("[LayoutRebuilder] No ILayoutController on target: " + layoutRoot.name, layoutRoot);
#endif

            var layoutCalcTargets = ListPool<ILayoutElement>.Get(); // calculate layout, dimensions, etc.
            var layoutControllers = ListPool<ILayoutController>.Get(); // controls rect transforms
            CollectLayoutCalcTargets(layoutRoot, layoutCalcTargets); // child to parent order.
            CollectLayoutControllers(layoutRoot, layoutControllers); // parent to child order. (ILayoutSelfController first).

            // Horizontal layout first.
            foreach (var layoutElement in layoutCalcTargets)
            {
                // L.I("[LayoutRebuilder] CalculateLayoutInputHorizontal: " + layoutElement, (Object) layoutElement);
                layoutElement.CalculateLayoutInputHorizontal();
            }
            foreach (var layoutController in layoutControllers)
            {
                // L.I("[LayoutRebuilder] SetLayoutHorizontal: " + layoutController, (Object) layoutController);
                layoutController.SetLayoutHorizontal();
            }

            // Then vertical layout.
            foreach (var layoutElement in layoutCalcTargets)
            {
                // L.I("[LayoutRebuilder] CalculateLayoutInputVertical: " + layoutElement, (Object) layoutElement);
                layoutElement.CalculateLayoutInputVertical();
            }
            foreach (var layoutController in layoutControllers)
            {
                // L.I("[LayoutRebuilder] SetLayoutVertical: " + layoutController, (Object) layoutController);
                layoutController.SetLayoutVertical();
            }

            ListPool<ILayoutElement>.Release(layoutCalcTargets);
            ListPool<ILayoutController>.Release(layoutControllers);
        }

        private static void CollectLayoutCalcTargets(Transform t, List<ILayoutElement> result)
        {
            Assert.IsTrue(t.gameObject.activeInHierarchy, "Target must be active in hierarchy: " + t.name);

            // If the target is a layout group, we need to recurse to children.
            // Layout calculations needs to executed bottom up with children being done before their parents,
            // because the parent calculated sizes rely on the sizes of the children.
            if (t.HasComponent(typeof(ILayoutGroup)))
            {
                for (var i = 0; i < t.childCount; i++)
                {
                    var c = t.GetChild(i);
                    if (c.gameObject.activeSelf) // only consider active children
                        CollectLayoutCalcTargets(c, result);
                }
            }

            // If there are no controllers on the target we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            using (CompBuf.GetEnabledComponents(t, typeof(ILayoutElement), out var elems))
            {
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (ILayoutElement elem in elems)
                    result.Add(elem);
            }
        }

        private static void CollectLayoutControllers(Transform t, List<ILayoutController> result)
        {
            Assert.IsTrue(t.gameObject.activeInHierarchy, "Target must be active in hierarchy: " + t.name);

            using var _ = CompBuf.GetEnabledComponents(
                t, typeof(ILayoutController), out var components);

            // If there are no controllers on the target we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            var count = components.Count; // mostly 1 or 2.
            if (count is 0)
                return;

            // Layout control needs to executed top down with parents being done before their children,
            // because the children rely on the sizes of the parents.

            // First call layout controllers that may change their own Transform
            for (var i = 0; i < count; i++)
                if (components[i] is ILayoutSelfController selfController)
                    result.Add(selfController);

            // Then call the remaining, such as layout groups that change their children, taking their own Transform size into account.
            for (var i = 0; i < count; i++)
            {
                var comp = components[i];
                if (comp is ILayoutSelfController) // already added
                    continue;

                // when the scrollRect is the content itself (scrollRect == comp == t == content?)
                // XXX: it should be validated by Odin validator.
                if (comp is ScrollRect scrollRect
                    && ReferenceEquals(scrollRect.content, t))
                    continue;

                result.Add((ILayoutController) comp);
            }

            // Then recurse to children.
            for (var i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (c.gameObject.activeSelf) // only consider active children
                    CollectLayoutControllers(c, result);
            }
        }

        public static void ResolveRootAndRebuildImmediate(Transform t)
        {
            var layoutRoot = ResolveUnvisitedLayoutRoot(t, visited: null);
            if (layoutRoot is not null) RebuildRootImmediate(layoutRoot);
        }
    }
}