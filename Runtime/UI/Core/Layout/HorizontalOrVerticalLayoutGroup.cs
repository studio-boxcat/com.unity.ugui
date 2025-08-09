// ReSharper disable InconsistentNaming
#nullable enable
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Abstract base class for HorizontalLayoutGroup and VerticalLayoutGroup to generalize common functionality.
    /// </summary>
    ///
    [ExecuteAlways]
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        [SerializeField] protected float m_Spacing = 0;

        /// <summary>
        /// The spacing to use between layout elements in the layout group.
        /// </summary>
        public float spacing { get => m_Spacing; set => SetPropertyUtility.SetValue(ref m_Spacing, value); }

        [SerializeField] protected bool m_ChildControlWidth = false;

        /// <summary>
        /// Returns true if the Layout Group controls the widths of its children. Returns false if children control their own widths.
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the widths untouched. The widths of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the widths of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible widths. This is useful if the widths of the children should change depending on how much space is available.In this case the width of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible width for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlWidth { get { return m_ChildControlWidth; } set { SetPropertyUtility.SetValue(ref m_ChildControlWidth, value); } }

        [SerializeField] protected bool m_ChildControlHeight = false;

        /// <summary>
        /// Returns true if the Layout Group controls the heights of its children. Returns false if children control their own heights.
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the heights untouched. The heights of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the heights of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible heights. This is useful if the heights of the children should change depending on how much space is available.In this case the height of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible height for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlHeight { get { return m_ChildControlHeight; } set { SetPropertyUtility.SetValue(ref m_ChildControlHeight, value); } }

        /// <summary>
        /// Whether the order of children objects should be sorted in reverse.
        /// </summary>
        /// <remarks>
        /// If False the first child object will be positioned first.
        /// If True the last child object will be positioned first.
        /// </remarks>
        public bool reverseArrangement { get { return m_ReverseArrangement; } set { SetPropertyUtility.SetValue(ref m_ReverseArrangement, value); } }

        [SerializeField] protected bool m_ReverseArrangement = false;

        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void CalcAlongAxis(Axis axis, bool isVertical)
        {
            float combinedPadding = axis.SelectHorizontalOrVertical(padding);
            bool controlSize = axis.Select(m_ChildControlWidth, m_ChildControlHeight);

            float totalPreferred = combinedPadding;

            bool alongOtherAxis = (isVertical ^ axis.IsY());
            var rectChildrenCount = rectChildren.Count;
            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i];
                float preferred = GetChildSizes(child, axis, controlSize);

                if (alongOtherAxis)
                {
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                }
                else
                {
                    totalPreferred += preferred + spacing;
                }
            }

            if (!alongOtherAxis && rectChildren.Count > 0)
            {
                totalPreferred -= spacing;
            }
            SetLayoutInputForAxis(totalPreferred, axis);
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void SetChildrenAlongAxis(Axis axis, bool isVertical)
        {
            var rect = rectTransform.rect;
            float size = rect.size[axis.Idx()];
            bool controlSize = axis.Select(m_ChildControlWidth, m_ChildControlHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            bool alongOtherAxis = (isVertical ^ axis.IsY());
            int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
            int endIndex = m_ReverseArrangement ? 0 : rectChildren.Count;
            int increment = m_ReverseArrangement ? -1 : 1;
            if (alongOtherAxis)
            {
                float innerSize = size - axis.SelectHorizontalOrVertical(padding);

                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    float preferred = GetChildSizes(child, axis, controlSize);

                    float requiredSpace = Mathf.Clamp(innerSize, 0, preferred);
                    float startOffset = GetStartOffset(size, axis, requiredSpace);
                    if (controlSize)
                    {
                        SetChildAlongAxis(child, axis, startOffset, requiredSpace);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis.Idx()]) * alignmentOnAxis;
                        SetChildAlongAxis(child, axis, startOffset + offsetInCell);
                    }
                }
            }
            else
            {
                float pos = (axis == 0 ? padding.left : padding.top);
                float surplusSpace = size - GetTotalPreferredSize(axis);

                if (surplusSpace > 0)
                {
                    pos = GetStartOffset(size, axis, GetTotalPreferredSize(axis) - axis.SelectHorizontalOrVertical(padding));
                }

                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    float preferred = GetChildSizes(child, axis, controlSize);

                    float childSize = preferred;
                    if (controlSize)
                    {
                        SetChildAlongAxis(child, axis, pos, childSize);
                    }
                    else
                    {
                        float offsetInCell = (childSize - child.sizeDelta[axis.Idx()]) * alignmentOnAxis;
                        SetChildAlongAxis(child, axis, pos + offsetInCell);
                    }
                    pos += childSize + spacing;
                }
            }
        }

        private static float GetChildSizes(RectTransform child, Axis axis, bool controlSize)
        {
            return controlSize
                ? LayoutUtility.CalcPreferredSize(child, axis)
                : child.sizeDelta[axis.Idx()];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DrivenRectTransManager.Reset(this, out var tracker))
            {
                Assert.IsTrue(this is HorizontalLayoutGroup or VerticalLayoutGroup,
                    "HorizontalOrVerticalLayoutGroup should only be used as a base class for HorizontalLayoutGroup and VerticalLayoutGroup.");
                var props = BaseDrivenProperties;
                if (m_ChildControlWidth && this is HorizontalLayoutGroup)
                    props |= DrivenTransformProperties.SizeDeltaX;
                if (m_ChildControlHeight && this is VerticalLayoutGroup)
                    props |= DrivenTransformProperties.SizeDeltaY;
                tracker.SetChildren(transform, props);
            }
        }
#endif
    }
}