#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public partial class LayoutRebuilder
    {
        /// <summary>
        /// Mark the given RectTransform as needing it's layout to be recalculated during the next layout pass.
        /// </summary>
        /// <param name="t">Rect to rebuild.</param>
        public static void MarkLayoutForRebuild(RectTransform t)
        {
            // XXX: even if the target is inactive, we still need to register its root for layout rebuild.
            // e.g. inactivate a child of a HorizontalLayoutGroup,
            var layoutRoot = ResolveLayoutRoot(t);
            if (layoutRoot is null) return;

            // no need to rebuild if the layout root itself is not active.
            if (!layoutRoot.gameObject.activeInHierarchy) return;

            var rebuilder = LayoutBuildProxy.Rent(layoutRoot);
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                LayoutBuildProxy.Release(rebuilder);
            return;

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
            static RectTransform? ResolveLayoutRoot(RectTransform t)
            {
                // find the topmost ILayoutGroup in the parent chain.
                var topMostGroup = TopMostGroupInParentChain(t);
                if (topMostGroup is not null) return topMostGroup;

                // if there is no ILayoutGroup in the parent chain,
                // check if the target itself is an ILayoutController. (ILayoutGroup or ILayoutSelfController)
                if (ComponentSearch.AnyEnabledComponent<ILayoutController>(t))
                    return t;

                return null;
            }

            static RectTransform? TopMostGroupInParentChain(RectTransform t)
            {
                RectTransform? found = null;

                var ptr = t.parent as RectTransform;
                while (ptr is not null)
                {
                    // will return false at the first parent in most cases
                    if (ComponentSearch.AnyEnabledComponent<ILayoutGroup>(ptr) is false)
                        return found;

                    found = ptr;
                    ptr = ptr.parent as RectTransform; // climb up
                }

                return found;
            }
        }

        public static void AfterLayoutCompleted(List<ICanvasElement> canvasElements)
        {
            foreach (var canvasElement in canvasElements)
            {
                if (canvasElement is LayoutBuildProxy proxy)
                    LayoutBuildProxy.Release(proxy);
            }
        }

        private class LayoutBuildProxy : ICanvasElement
        {
            private static readonly List<LayoutBuildProxy> _pool = new();

            public static LayoutBuildProxy Rent(RectTransform controller)
            {
                if (_pool.TryPop(out var proxy) is false)
                    proxy = new LayoutBuildProxy();
                Assert.IsTrue(proxy._target is null, "LayoutBuildProxy should not be initialized twice.");
                proxy._target = controller;
                return proxy;
            }

            public static void Release(LayoutBuildProxy proxy)
            {
                Assert.IsTrue(proxy._target is not null, "LayoutBuildProxy should not be cleared twice.");
                proxy._target = null;
                _pool.Add(proxy);
            }

            private RectTransform? _target;

            Transform ICanvasElement.transform => _target!;

            /// <summary>
            /// Has the native representation of this LayoutRebuilder been destroyed?
            /// </summary>
            bool ICanvasElement.IsDestroyed() => !_target;

            void ICanvasElement.Rebuild(CanvasUpdate executing)
            {
                if (executing != CanvasUpdate.Layout) return;
                if (_target) ForceRebuildLayoutImmediate(_target!);
            }

            // GetHashCode() is used by IndexedSet.
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            public override int GetHashCode() => _target!.GetInstanceID();
            public override bool Equals(object? obj) => obj!.GetHashCode() == GetHashCode();
            public override string ToString() => "(Layout Rebuilder for) " + _target;
        }
    }
}