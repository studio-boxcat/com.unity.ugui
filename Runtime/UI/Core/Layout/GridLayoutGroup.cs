using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Grid Layout Group", 152)]
    /// <summary>
    /// Layout class to arrange child elements in a grid format.
    /// </summary>
    /// <remarks>
    /// The GridLayoutGroup component is used to layout child layout elements in a uniform grid where all cells have the same size. The size and the spacing between cells is controlled by the GridLayoutGroup itself. The children have no influence on their sizes.
    /// </remarks>
    public class GridLayoutGroup : LayoutGroup
    {
        /// <summary>
        /// Which corner is the starting corner for the grid.
        /// </summary>
        protected enum Corner
        {
            UpperLeft = 0,
            UpperRight = 1,
            LowerLeft = 2,
            LowerRight = 3
        }

        protected enum Constraint
        {
            Flexible = 0,
            FixedColumnCount = 1,
            FixedRowCount = 2
        }

        /// <summary>
        /// Which corner should the first cell be placed in?
        /// </summary>
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected Corner m_StartCorner = Corner.UpperLeft;

        /// <summary>
        /// Which axis should cells be placed along first
        /// </summary>
        /// <remarks>
        /// When startAxis is set to horizontal, an entire row will be filled out before proceeding to the next row. When set to vertical, an entire column will be filled out before proceeding to the next column.
        /// </remarks>
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected Axis m_StartAxis = Axis.X;

        /// <summary>
        /// The size to use for each cell in the grid.
        /// </summary>
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected Vector2 m_CellSize = new Vector2(100, 100);

        /// <summary>
        /// The spacing to use between layout elements in the grid on both axises.
        /// </summary>
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected Vector2 m_Spacing = Vector2.zero;

        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected Constraint m_Constraint = Constraint.Flexible;

        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected int m_ConstraintCount = 2;

        /// <summary>
        /// Called by the layout system to calculate the horizontal layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var preferredColumns = m_Constraint switch
            {
                Constraint.FixedColumnCount => m_ConstraintCount,
                Constraint.FixedRowCount => Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f),
                _ => Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count))
            };

            SetLayoutInputForAxis(
                padding.horizontal + (m_CellSize.x + m_Spacing.x) * preferredColumns - m_Spacing.x,
                Axis.X);
        }

        /// <summary>
        /// Called by the layout system to calculate the vertical layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            int minRows = 0;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                minRows = m_ConstraintCount;
            }
            else
            {
                var width = rectTransform.rect.width;
                var cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + m_Spacing.x + 0.001f) / (m_CellSize.x + m_Spacing.x)));
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
            }

            var minSpace = padding.vertical + (m_CellSize.y + m_Spacing.y) * minRows - m_Spacing.y;
            SetLayoutInputForAxis(minSpace, Axis.Y);
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal() => SetCellsAlongAxis(0);

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical() => SetCellsAlongAxis(1);

        private void SetCellsAlongAxis(int axis)
        {
            // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
            // and only vertical values when invoked for the vertical axis.
            // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
            // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
            // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.
            var rectChildrenCount = rectChildren.Count;
            if (axis == 0)
            {
                // Only set the sizes when invoked for horizontal axis, not the positions.

                for (int i = 0; i < rectChildrenCount; i++)
                {
                    var rect = rectChildren[i];
                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                    rect.sizeDelta = m_CellSize;
                }
                return;
            }

            var (width, height) = rectTransform.rect.size;

            int cellCountX = 1;
            int cellCountY = 1;
            if (m_Constraint == Constraint.FixedColumnCount)
            {
                cellCountX = m_ConstraintCount;

                if (rectChildrenCount > cellCountX)
                    cellCountY = rectChildrenCount / cellCountX + (rectChildrenCount % cellCountX > 0 ? 1 : 0);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                cellCountY = m_ConstraintCount;

                if (rectChildrenCount > cellCountY)
                    cellCountX = rectChildrenCount / cellCountY + (rectChildrenCount % cellCountY > 0 ? 1 : 0);
            }
            else
            {
                cellCountX = (m_CellSize.x + m_Spacing.x) > 0
                    ? Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + m_Spacing.x + 0.001f) / (m_CellSize.x + m_Spacing.x)))
                    : int.MaxValue;

                cellCountY = (m_CellSize.y + m_Spacing.y > 0)
                    ? Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + m_Spacing.y + 0.001f) / (m_CellSize.y + m_Spacing.y)))
                    : int.MaxValue;
            }

            int cellsPerMainAxis, actualCellCountX, actualCellCountY;
            if (m_StartAxis.IsX())
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildrenCount);

                actualCellCountY = m_Constraint is Constraint.FixedRowCount
                    ? Mathf.Min(cellCountY, rectChildrenCount)
                    : Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildrenCount);

                actualCellCountX = m_Constraint is Constraint.FixedColumnCount
                    ? Mathf.Min(cellCountX, rectChildrenCount)
                    : Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
            }

            Vector2 requiredSpace = new Vector2(
                actualCellCountX * m_CellSize.x + (actualCellCountX - 1) * m_Spacing.x,
                actualCellCountY * m_CellSize.y + (actualCellCountY - 1) * m_Spacing.y
            );
            Vector2 startOffset = new Vector2(
                GetStartOffset(width, Axis.X, requiredSpace.x),
                GetStartOffset(height, Axis.Y, requiredSpace.y)
            );

            // Fixes case 1345471 - Makes sure the constraint column / row amount is always respected
            int childrenToMove = 0;
            if (rectChildrenCount > m_ConstraintCount && Mathf.CeilToInt((float)rectChildrenCount / (float)cellsPerMainAxis) < m_ConstraintCount)
            {
                childrenToMove = m_ConstraintCount - Mathf.CeilToInt((float)rectChildrenCount / (float)cellsPerMainAxis);
                childrenToMove += Mathf.FloorToInt((float)childrenToMove / ((float)cellsPerMainAxis - 1));
                if (rectChildrenCount % cellsPerMainAxis == 1)
                    childrenToMove += 1;
            }

            for (int i = 0; i < rectChildrenCount; i++)
            {
                int positionX;
                int positionY;
                if (m_StartAxis.IsX())
                {
                    if (m_Constraint == Constraint.FixedRowCount && rectChildrenCount - i <= childrenToMove)
                    {
                        positionX = 0;
                        positionY = m_ConstraintCount - (rectChildrenCount - i);
                    }
                    else
                    {
                        positionX = i % cellsPerMainAxis;
                        positionY = i / cellsPerMainAxis;
                    }
                }
                else
                {
                    if (m_Constraint == Constraint.FixedColumnCount && rectChildrenCount - i <= childrenToMove)
                    {
                        positionX = m_ConstraintCount - (rectChildrenCount - i);
                        positionY = 0;
                    }
                    else
                    {
                        positionX = i / cellsPerMainAxis;
                        positionY = i % cellsPerMainAxis;
                    }
                }

                var cornerX = (int)m_StartCorner % 2;
                var cornerY = (int)m_StartCorner / 2;
                if (cornerX == 1)
                    positionX = actualCellCountX - 1 - positionX;
                if (cornerY == 1)
                    positionY = actualCellCountY - 1 - positionY;

                SetChildAlongAxis(rectChildren[i], Axis.X, startOffset.x + (m_CellSize.x + m_Spacing.x) * positionX, m_CellSize.x);
                SetChildAlongAxis(rectChildren[i], Axis.Y, startOffset.y + (m_CellSize.y + m_Spacing.y) * positionY, m_CellSize.y);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DrivenRectTransManager.Reset(this, out var tracker))
            {
                tracker.SetChildren(transform,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta);
            }
        }
#endif
    }
}
