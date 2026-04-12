using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class GridLayoutGroup : MonoBehaviour, ILayoutElementV, ILayoutGroup
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        private Vector2 m_CellSize = new(100, 100);
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        private Vector2Int m_Padding; // x=top, y=bottom
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        private Vector2 m_Spacing;
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        protected TextAnchor m_ChildAlignment;
        [FormerlySerializedAs("m_ConstraintCount")]
        [SerializeField, OnValueChanged("SetLayoutDirty")]
        private int m_ColumnCount; // 0 to flexible

        [System.NonSerialized] private RectTransform _rectTransBacking;
        private RectTransform _rectTrans => _rectTransBacking ??= (RectTransform)transform;

        private float _preferredSize;
        float ILayoutElementV.preferredHeight => _preferredSize;

        private readonly List<RectTransform> _children = new(); // filtered list of direct children that are considered for layout
        private int _cellCountX; // cached column count, computed in CalcV, reused in SetV
        private int _cellCountY; // cached row count, computed in CalcV, reused in SetV

        void ILayoutElementV.CalculateLayoutInputVertical()
        {
            _children.Clear();

            var t = _rectTrans;
            var childCount = t.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var c = t.GetChild(i);
                if (c is not RectTransform rt) continue; // only RectTransform children are considered.
                if (!c.gameObject.activeSelf) continue; // direct child, active self is enough.
                if (c.HasComponent<LayoutIgnorer>()) continue;
                _children.Add(rt);
            }

            // early return if no children
            childCount = _children.Count; // update child count after filtering
            if (childCount is 0)
            {
                _preferredSize = m_Padding.XPlusY(); // no children, only padding contributes to preferred size
                return;
            }

            var cellWidthWithSpace = m_CellSize.x + m_Spacing.x;
            _cellCountX = m_ColumnCount is not 0 ? m_ColumnCount
                : Mathf.Max(1, Mathf.FloorToInt((_rectTrans.rect.width + m_Spacing.x + 0.001f) / cellWidthWithSpace)); // flexible, at least 1 required.

            _cellCountY = (childCount - 1) / _cellCountX + 1;
            _preferredSize = m_Padding.XPlusY() + (m_CellSize.y + m_Spacing.y) * _cellCountY - m_Spacing.y;
        }

        void ILayoutController.SetLayoutVertical()
        {
            var childCount = _children.Count;
            if (childCount is 0) return;

            var (width, height) = _rectTrans.rect.size;
            height -= m_Padding.XPlusY(); // available height after removing padding
            var cws = m_CellSize.x + m_Spacing.x; // cell width with spacing
            var chs = m_CellSize.y + m_Spacing.y; // cell height with spacing

            var actualCellCountX = Mathf.Min(_cellCountX, childCount);
            var startX = (width - actualCellCountX * cws + m_Spacing.x) * m_ChildAlignment.PivotX();
            var startY = m_Padding.x + (height - _cellCountY * chs + m_Spacing.y) * m_ChildAlignment.PivotYInverted();
            for (var i = 0; i < childCount; i++)
            {
                var child = _children[i];
                child.anchorMin = child.anchorMax = new Vector2(0, 1); // Vector2.up
                child.sizeDelta = m_CellSize;
                child.pivot = new Vector2(0.5f, 0.5f);

                var posX = startX + cws * (i % _cellCountX);
                var posY = startY + chs * (i / _cellCountX);
                child.anchoredPosition = new Vector2(
                    posX + m_CellSize.x / 2,
                    -posY - m_CellSize.y / 2);
            }
        }

        private void OnEnable() => SetLayoutDirty();
        private void OnDisable() => SetLayoutDirty();

        private void OnDidApplyAnimationProperties()
        {
            if (isActiveAndEnabled)
                SetLayoutDirty();
        }

        private void OnTransformChildrenChanged()
        {
            if (isActiveAndEnabled)
                SetLayoutDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && CanvasUpdateRegistry.IsRebuildingLayout() is false)
                SetLayoutDirty();
        }

        private void SetLayoutDirty() => LayoutRebuilder.SetDirty(_rectTrans);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DrivenRectTransManager.Reset(this, out var tracker))
            {
                tracker.SetChildren(transform,
                    DrivenTransformProperties.Anchors
                    | DrivenTransformProperties.AnchoredPosition
                    | DrivenTransformProperties.SizeDelta
                    | DrivenTransformProperties.Pivot);
            }
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (m_CellSize.x + m_Spacing.x <= 0)
                result.AddError("Cell Size X + Spacing X must be greater than 0.");
        }
#endif
    }
}
