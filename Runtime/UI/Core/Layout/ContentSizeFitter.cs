// ReSharper disable InconsistentNaming

#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    /// <summary>
    /// Resizes a RectTransform to fit the size of its content.
    /// </summary>
    /// <remarks>
    /// The ContentSizeFitter can be used on GameObjects that have one or more ILayoutElement components, such as Text, Image, HorizontalLayoutGroup, VerticalLayoutGroup, and GridLayoutGroup.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ContentSizeFitter : UIBehaviour, ILayoutSelfController
    {
        [SerializeField, OnValueChanged(nameof(SetDirty))]
        private bool m_HorizontalFit = true;
        [SerializeField, OnValueChanged(nameof(SetDirty))]
        private bool m_VerticalFit;

        [System.NonSerialized]
        private RectTransform? m_Rect;
        private RectTransform rectTransform => m_Rect ??= (RectTransform) transform;

        private bool _performingSetLayout;

        private void OnEnable() => SetDirty();
        private void OnDisable() => SetDirty();

        private void SetDirty() => LayoutRebuilder.SetDirty(rectTransform);

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled
                && _performingSetLayout is false) // CalcPreferredSize(), SetSizeWithCurrentAnchors() will invoke OnRectTransformDimensionsChange().
            {
                SetDirty();
            }
        }

        private void HandleSelfFittingAlongAxis(Axis axis)
        {
            var fitting = axis.Select(m_HorizontalFit, m_VerticalFit);
            if (fitting is false) return;

            _performingSetLayout = true;

            var t = rectTransform;

            // Set size to preferred size
            var size = LayoutUtility.CalcPreferredSize(t, axis);
            t.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, size);

            _performingSetLayout = false;
        }

        void ILayoutController.SetLayoutHorizontal() => HandleSelfFittingAlongAxis(Axis.X);
        void ILayoutController.SetLayoutVertical() => HandleSelfFittingAlongAxis(Axis.Y);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DrivenRectTransManager.Reset(this, out var tracker))
            {
                var props = DrivenTransformProperties.None;
                if (m_HorizontalFit) props |= DrivenTransformProperties.SizeDeltaX;
                if (m_VerticalFit) props |= DrivenTransformProperties.SizeDeltaY;
                tracker.SetSelf(props);
            }
        }
#endif
    }
}