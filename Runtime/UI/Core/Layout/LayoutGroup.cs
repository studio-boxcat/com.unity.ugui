// ReSharper disable InconsistentNaming

using System.Collections.Generic;

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// Abstract base class to use for layout groups.
    /// </summary>
    public abstract class LayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        [SerializeField] protected RectOffset m_Padding;

        /// <summary>
        /// The padding to add around the child layout elements.
        /// </summary>
        public RectOffset padding => m_Padding ??= new RectOffset();

        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;

        /// <summary>
        /// The alignment to use for the child layout elements in the layout group.
        /// </summary>
        /// <remarks>
        /// If a layout element does not specify a flexible width or height, its child elements many not use the available space within the layout group. In this case, use the alignment settings to specify how to align child elements within their layout group.
        /// </remarks>
        public TextAnchor childAlignment { get => m_ChildAlignment; set => SetPropertyUtility.SetEnum(ref m_ChildAlignment, value); }

        [System.NonSerialized] private RectTransform m_Rect;
        protected RectTransform rectTransform => m_Rect ??= (RectTransform) transform;

        private Vector2 m_TotalPreferredSize = Vector2.zero;

        protected readonly List<RectTransform> rectChildren = new();

        public virtual void CalculateLayoutInputHorizontal()
        {
            rectChildren.Clear();

            var t = rectTransform;
            for (var i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (c is not RectTransform rt) continue; // only RectTransform children are considered.
                if (!c.gameObject.activeSelf) continue; // direct child, active self is enough.
                if (c.HasComponent<LayoutIgnorer>()) continue;
                rectChildren.Add(rt);
            }
        }

        public abstract void CalculateLayoutInputVertical();

        /// <summary>
        /// See LayoutElement.preferredWidth
        /// </summary>
        float ILayoutElement.preferredWidth => GetTotalPreferredSize(Axis.X);

        /// <summary>
        /// See LayoutElement.preferredHeight
        /// </summary>
        float ILayoutElement.preferredHeight => GetTotalPreferredSize(Axis.Y);

        // ILayoutController Interface

        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();

        // Implementation

        private void OnEnable() => SetLayoutDirty();
        private void OnDisable() => SetLayoutDirty();

        /// <summary>
        /// Callback for when properties have been changed by animation.
        /// </summary>
        private void OnDidApplyAnimationProperties()
        {
            if (isActiveAndEnabled)
                SetLayoutDirty();
        }

        /// <summary>
        /// The preferred size for the layout group on the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The preferred size.</returns>
        protected float GetTotalPreferredSize(Axis axis)
        {
            return m_TotalPreferredSize[axis.Idx()];
        }

        /// <summary>
        /// Returns the calculated position of the first child layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <param name="requiredSpaceWithoutPadding">The total space required on the given axis for all the layout elements including spacing and excluding padding.</param>
        /// <returns>The position of the first child along the given axis.</returns>
        protected float GetStartOffset(float availableSpace, Axis axis, float requiredSpaceWithoutPadding)
        {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);
            float surplusSpace = availableSpace - requiredSpace;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
        }

        /// <summary>
        /// Returns the alignment on the specified axis as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.
        /// </summary>
        /// <param name="axis">The axis to get alignment along. 0 is horizontal and 1 is vertical.</param>
        /// <returns>The alignment as a fraction where 0 is left/top, 0.5 is middle, and 1 is right/bottom.</returns>
        protected float GetAlignmentOnAxis(Axis axis)
        {
            return axis.IsX()
                ? ((int) childAlignment % 3) * 0.5f
                : ((int) childAlignment / 3) * 0.5f;
        }

        /// <summary>
        /// Used to set the calculated layout properties for the given axis.
        /// </summary>
        /// <param name="totalPreferred">The preferred size for the layout group.</param>
        /// <param name="axis">The axis to set sizes for. 0 is horizontal and 1 is vertical.</param>
        protected void SetLayoutInputForAxis(float totalPreferred, Axis axis)
        {
            m_TotalPreferredSize[axis.Idx()] = totalPreferred;
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        protected static void SetChildAlongAxis(RectTransform rect, Axis axis, float pos)
        {
            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);

            var anchoredPosition = rect.anchoredPosition;
            if (axis.IsX()) anchoredPosition.x = pos + rect.sizeDelta.x * rect.pivot.x;
            else anchoredPosition.y = -pos - rect.sizeDelta.y * (1f - rect.pivot.y);
            rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected static void SetChildAlongAxis(RectTransform rect, Axis axis, float pos, float size)
        {
            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);

            Vector2 sizeDelta = rect.sizeDelta;
            Vector2 anchoredPosition = rect.anchoredPosition;

            if (axis.IsX())
            {
                sizeDelta.x = size;
                anchoredPosition.x = pos + size * rect.pivot.x;
            }
            else
            {
                sizeDelta.y = size;
                anchoredPosition.y = -pos - size * (1f - rect.pivot.y);
            }
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled
                && CanvasUpdateRegistry.IsRebuildingLayout() is false
                && IsRootLayoutGroup(rectTransform))
            {
                SetLayoutDirty();
            }
            return;

            static bool IsRootLayoutGroup(Transform transform)
            {
                var parent = transform.parent;
                if (parent is null) return true;
                return parent.TryGetComponent(typeof(ILayoutGroup), out _) == false;
            }
        }

        private void OnTransformChildrenChanged()
        {
            if (isActiveAndEnabled)
                SetLayoutDirty();
        }

        /// <summary>
        /// Mark the LayoutGroup as dirty.
        /// </summary>
        private void SetLayoutDirty() => LayoutRebuilder.SetDirty(rectTransform);

#if UNITY_EDITOR
        protected const DrivenTransformProperties BaseDrivenProperties =
            DrivenTransformProperties.Anchors
            // track both x and y axis for legacy compatibility
            | DrivenTransformProperties.AnchoredPosition;
#endif
    }
}