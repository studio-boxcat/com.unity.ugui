namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Horizontal Layout Group", 150)]
    /// <summary>
    /// Layout class for arranging child elements side by side.
    /// </summary>
    public sealed class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, isVertical: false);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, isVertical: false);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, isVertical: false);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, isVertical: false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DrivenRectTransManager.Clear(this);
            DrivenRectTransManager.SetChildren(this, transform,
                GetDrivenProps(x: true, y: false, size: m_ChildControlWidth));
        }
#endif
    }
}
