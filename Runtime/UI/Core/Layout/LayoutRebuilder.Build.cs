using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    public partial class LayoutRebuilder
    {
        /// <summary>
        /// Forces an immediate rebuild of the layout element and child layout elements affected by the calculations.
        /// </summary>
        /// <param name="target">The layout element to perform the layout rebuild on.</param>
        /// <remarks>
        /// Normal use of the layout system should not use this method. Instead MarkLayoutForRebuild should be used instead, which triggers a delayed layout rebuild during the next layout pass. The delayed rebuild automatically handles objects in the entire layout hierarchy in the correct order, and prevents multiple recalculations for the same layout elements.
        /// However, for special layout calculation needs, ::ref::ForceRebuildLayoutImmediate can be used to get the layout of a sub-tree resolved immediately. This can even be done from inside layout calculation methods such as ILayoutController.SetLayoutHorizontal orILayoutController.SetLayoutVertical. Usage should be restricted to cases where multiple layout passes are unavaoidable despite the extra cost in performance.
        /// </remarks>
        public static void ForceRebuildLayoutImmediate([NotNull] RectTransform target)
        {
            var layoutElements = ListPool<ILayoutElement>.Get();
            var layoutControllers = ListPool<ILayoutController>.Get();
            CollectLayoutCalculationTargets(target, layoutElements);
            CollectLayoutTargets(target, layoutControllers);

            // Horizontal layout first.
            foreach (var layoutElement in layoutElements)
                layoutElement.CalculateLayoutInputHorizontal();
            foreach (var layoutController in layoutControllers)
                layoutController.SetLayoutHorizontal();

            // Then vertical layout.
            foreach (var layoutElement in layoutElements)
                layoutElement.CalculateLayoutInputVertical();
            foreach (var layoutController in layoutControllers)
                layoutController.SetLayoutVertical();

            ListPool<ILayoutElement>.Release(layoutElements);
            ListPool<ILayoutController>.Release(layoutControllers);
        }

        static void CollectLayoutTargets(RectTransform rect, List<ILayoutController> result)
        {
            using var _ = CompBuf.GetEnabledComponents(
                rect, typeof(ILayoutController), out var components);

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            var count = components.Count;
            if (count is 0)
                return;

            // Layout control needs to executed top down with parents being done before their children,
            // because the children rely on the sizes of the parents.

            // First call layout controllers that may change their own RectTransform
            for (var i = 0; i < count; i++)
                if (components[i] is ILayoutSelfController selfController)
                    result.Add(selfController);

            // Then call the remaining, such as layout groups that change their children, taking their own RectTransform size into account.
            for (var i = 0; i < count; i++)
            {
                var comp = components[i];
                if (comp is ILayoutSelfController)
                    continue;

                if (comp is ScrollRect scrollRect
                    && ReferenceEquals(scrollRect.content, rect))
                    continue;

                result.Add((ILayoutController) comp);
            }

            // Then recurse to children.
            for (var i = 0; i < rect.childCount; i++)
            {
                if (rect.GetChild(i) is RectTransform child)
                    CollectLayoutTargets(child, result);
            }
        }

        static void CollectLayoutCalculationTargets(RectTransform rect, List<ILayoutElement> result)
        {
            // If rect is a layout group, we need to recurse to children.
            // Layout calculations needs to executed bottom up with children being done before their parents,
            // because the parent calculated sizes rely on the sizes of the children.
            if (rect.TryGetComponent(typeof(ILayoutGroup), out _))
            {
                for (var i = 0; i < rect.childCount; i++)
                {
                    if (rect.GetChild(i) is RectTransform child)
                        CollectLayoutCalculationTargets(child, result);
                }
            }

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            using (CompBuf.GetEnabledComponents(rect, typeof(ILayoutElement), out var layoutElements))
            {
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (ILayoutElement layoutElement in layoutElements)
                    result.Add(layoutElement);
            }
        }
    }
}