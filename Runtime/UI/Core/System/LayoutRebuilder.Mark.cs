using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public partial class LayoutRebuilder
    {
        /// <summary>
        /// Mark the given RectTransform as needing it's layout to be recalculated during the next layout pass.
        /// </summary>
        /// <param name="rect">Rect to rebuild.</param>
        public static void MarkLayoutForRebuild([NotNull] RectTransform rect)
        {
            var rebuildTarget = ResolveRebuildTarget(rect);
            if (rebuildTarget is null) return;

            var rebuilder = LayoutBuildProxy.Rent();
            rebuilder.Initialize(rebuildTarget);
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                LayoutBuildProxy.Release(rebuilder);
            return;

            static RectTransform ResolveRebuildTarget(RectTransform rect)
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

            [CanBeNull]
            static RectTransform GetLayoutRoot(RectTransform rect)
            {
                // Layout root is the topmost consecutive parent chain of ILayoutGroups.
                RectTransform layoutRoot = null;

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

        class LayoutBuildProxy : ICanvasElement
        {
            static readonly List<LayoutBuildProxy> _pool = new();

            public static LayoutBuildProxy Rent()
            {
                if (_pool.Count == 0)
                    return new LayoutBuildProxy();

                var index = _pool.Count - 1;
                var proxy = _pool[index];
                _pool.RemoveAt(index);
                return proxy;
            }

            public static void Release(LayoutBuildProxy proxy)
            {
                proxy.Clear();
                _pool.Add(proxy);
            }

            RectTransform _target;

            public void Initialize(RectTransform controller)
            {
                Assert.IsTrue(_target is null, "LayoutBuildProxy should not be initialized twice.");
                _target = controller;
            }

            void Clear()
            {
                Assert.IsTrue(_target is not null, "LayoutBuildProxy should not be cleared twice.");
                _target = null;
            }

            Transform ICanvasElement.transform => _target;

            /// <summary>
            /// Has the native representation of this LayoutRebuilder been destroyed?
            /// </summary>
            bool ICanvasElement.IsDestroyed()
            {
                return _target == null;
            }

            void ICanvasElement.Rebuild(CanvasUpdate executing)
            {
                if (executing != CanvasUpdate.Layout) return;
                if (_target == null) return;
                ForceRebuildLayoutImmediate(_target);
            }

            public override int GetHashCode() => _target.GetInstanceID();

            /// <summary>
            /// Does the passed rebuilder point to the same CanvasElement.
            /// </summary>
            /// <param name="obj">The other object to compare</param>
            /// <returns>Are they equal</returns>
            public override bool Equals(object obj) => obj.GetHashCode() == GetHashCode();

            public override string ToString() => "(Layout Rebuilder for) " + _target;
        }
    }
}