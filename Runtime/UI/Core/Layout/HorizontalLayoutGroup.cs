namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Horizontal Layout Group", 150)]
    public sealed class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup
    {
        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(Axis.X, isVertical: false);
        }

        public override void CalculateLayoutInputVertical() => CalcAlongAxis(Axis.Y, isVertical: false);
        public override void SetLayoutHorizontal() => SetChildrenAlongAxis(Axis.X, isVertical: false);
        public override void SetLayoutVertical() => SetChildrenAlongAxis(Axis.Y, isVertical: false);
    }
}
