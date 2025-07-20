#nullable enable
namespace UnityEngine.UI
{
    public partial class LayoutRebuilder
    {
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
        private static Transform? ResolveLayoutRoot(Transform t) // must have ILayoutController
        {
            // find the topmost ILayoutGroup in the parent chain.
            var topMostGroup = TopMostGroupInParentChain(t);
            if (topMostGroup is not null) return topMostGroup;

            // if there is no ILayoutGroup in the parent chain,
            // check if the target itself is an ILayoutController. (ILayoutGroup or ILayoutSelfController)
            if (ComponentSearch.AnyEnabledComponent<ILayoutController>(t))
                return t;

            return null;

            static Transform? TopMostGroupInParentChain(Transform t) // must have ILayoutController
            {
                Transform? found = null;

                var ptr = t.parent;
                while (ptr is not null)
                {
                    // will return false at the first parent in most cases
                    if (ComponentSearch.AnyEnabledComponent<ILayoutGroup>(ptr) is false)
                        return found;

                    found = ptr;
                    ptr = ptr.parent; // climb up
                }

                return found;
            }
        }

        /// <summary>
        /// Mark the given Transform as needing it's layout to be recalculated during the next layout pass.
        /// </summary>
        public static void SetRootDirty(Transform t)
        {
            L.I("[LayoutRebuilder] SetRootDirty: " + t);

            // XXX: even if the target is inactive, we still need to register its root for layout rebuild.
            // e.g. inactivate a child of a HorizontalLayoutGroup.
            var layoutRoot = ResolveLayoutRoot(t);
            if (layoutRoot is null) return;

            // no need to rebuild if the layout root itself is not active.
            if (!layoutRoot.gameObject.activeInHierarchy) return;

            CanvasUpdateRegistry.QueueLayoutRoot(layoutRoot);
        }

        public static void SetRootDirty(MonoBehaviour comp) => SetRootDirty(comp.transform);
    }
}