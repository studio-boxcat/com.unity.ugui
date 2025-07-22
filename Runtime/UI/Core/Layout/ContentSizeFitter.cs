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
        /// <summary>
        /// The size fit modes avaliable to use.
        /// </summary>
        internal enum FitMode
        {
            /// <summary>
            /// Don't perform any resizing.
            /// </summary>
            Unconstrained,
            /// <summary>
            /// Resize to the minimum size of the content.
            /// </summary>
            MinSize,
            /// <summary>
            /// Resize to the preferred size of the content.
            /// </summary>
            PreferredSize
        }

        [SerializeField, OnValueChanged(nameof(SetDirty))]
        private FitMode m_HorizontalFit = FitMode.Unconstrained;
        [SerializeField, OnValueChanged(nameof(SetDirty))]
        private FitMode m_VerticalFit = FitMode.Unconstrained;

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
                && _performingSetLayout is false) // GetMinSize(), GetPreferredSize(), SetSizeWithCurrentAnchors() will invoke OnRectTransformDimensionsChange().
            {
                SetDirty();
            }
        }

        private void HandleSelfFittingAlongAxis(int axis)
        {
            var fitting = axis == 0 ? m_HorizontalFit : m_VerticalFit;
            if (fitting == FitMode.Unconstrained) return;

            _performingSetLayout = true;

            var t = rectTransform;

            // Set size to min or preferred size
            var size = fitting switch
            {
                FitMode.MinSize => LayoutUtility.GetMinSize(t, axis),
                FitMode.PreferredSize => LayoutUtility.GetPreferredSize(t, axis),
                _ => throw new System.ArgumentOutOfRangeException(nameof(fitting), fitting, null)
            };
            t.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, size);

            _performingSetLayout = false;
        }

        /// <summary>
        /// Calculate and apply the horizontal component of the size to the RectTransform
        /// </summary>
        void ILayoutController.SetLayoutHorizontal()
        {
            HandleSelfFittingAlongAxis(0);
        }

        /// <summary>
        /// Calculate and apply the vertical component of the size to the RectTransform
        /// </summary>
        void ILayoutController.SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var props = DrivenTransformProperties.None;
            if (m_HorizontalFit != FitMode.Unconstrained)
                props |= DrivenTransformProperties.SizeDeltaX;
            if (m_VerticalFit != FitMode.Unconstrained)
                props |= DrivenTransformProperties.SizeDeltaY;
            DrivenRectTransManager.Clear(this);
            DrivenRectTransManager.SetSelf(this, props);
        }
#endif
    }
}