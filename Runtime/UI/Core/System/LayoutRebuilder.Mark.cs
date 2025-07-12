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
        /// <param name="rect">Rect to rebuild.</param>
        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            var rebuildTarget = ResolveRebuildTarget(rect);
            if (rebuildTarget is null) return;

            var rebuilder = LayoutBuildProxy.Rent(rebuildTarget);
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                LayoutBuildProxy.Release(rebuilder);
            return;

            static RectTransform? ResolveRebuildTarget(RectTransform rect)
            {
                // When there is an layout root, mark it for rebuild.
                var layoutRoot = GetLayoutRoot(rect);
                if (layoutRoot is not null)
                    return layoutRoot;

                // If there is no layout root, we need to check if the rect itself is a ILayoutController.
                if (ComponentSearch.AnyEnabledComponent<ILayoutController>(rect))
                    return rect;

                return null;
            }

            static RectTransform? GetLayoutRoot(RectTransform rect)
            {
                // Layout root is the topmost consecutive parent chain of ILayoutGroups.
                RectTransform? layoutRoot = null;

                var parent = rect.parent as RectTransform;
                while (parent is not null)
                {
                    if (ComponentSearch.AnyEnabledComponent<ILayoutGroup>(parent) == false)
                        return layoutRoot;

                    layoutRoot = parent;
                    parent = parent.parent as RectTransform;
                }

                return layoutRoot;
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
                if (_target) ForceRebuildLayoutImmediate(_target);
            }

            // GetHashCode() is used by IndexedSet.
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            public override int GetHashCode() => _target!.GetInstanceID();
            public override bool Equals(object? obj) => obj!.GetHashCode() == GetHashCode();
            public override string ToString() => "(Layout Rebuilder for) " + _target;
        }
    }
}