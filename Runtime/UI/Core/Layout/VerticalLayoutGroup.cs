namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Vertical Layout Group", 151)]
    /// <summary>
    /// Layout child layout elements below each other.
    /// </summary>
    public sealed class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, isVertical: true);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, isVertical: true);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, isVertical: true);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, isVertical: true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DrivenRectTransManager.Clear(this);
            DrivenRectTransManager.SetChildren(this, transform,
                GetDrivenProps(x: false, y: true, size: m_ChildControlHeight));
        }
#endif
    }
}
