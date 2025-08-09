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
            CalcAlongAxis(Axis.X, isVertical: true);
        }

        public override void CalculateLayoutInputVertical() => CalcAlongAxis(Axis.Y, isVertical: true);
        public override void SetLayoutHorizontal() => SetChildrenAlongAxis(Axis.X, isVertical: true);
        public override void SetLayoutVertical() => SetChildrenAlongAxis(Axis.Y, isVertical: true);
    }
}
