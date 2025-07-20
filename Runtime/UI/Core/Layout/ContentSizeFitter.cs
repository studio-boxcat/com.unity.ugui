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

        // field is never assigned warning
        private DrivenRectTransformTracker m_Tracker;

        private void OnEnable() => SetDirty();

        private void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.SetRootDirty(rectTransform);
        }

        private void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void HandleSelfFittingAlongAxis(int axis)
        {
            var fitting = axis == 0 ? m_HorizontalFit : m_VerticalFit;
            if (fitting == FitMode.Unconstrained)
            {
                // Keep a reference to the tracked transform, but don't control its properties:
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
                return;
            }

            m_Tracker.Add(this, rectTransform, axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);

            // Set size to min or preferred size
            var size = fitting switch
            {
                FitMode.MinSize => LayoutUtility.GetMinSize(m_Rect, axis),
                FitMode.PreferredSize => LayoutUtility.GetPreferredSize(m_Rect, axis),
                _ => throw new System.ArgumentOutOfRangeException(nameof(fitting), fitting, null)
            };
            rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis) axis, size);
        }

        /// <summary>
        /// Calculate and apply the horizontal component of the size to the RectTransform
        /// </summary>
        void ILayoutController.SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }

        /// <summary>
        /// Calculate and apply the vertical component of the size to the RectTransform
        /// </summary>
        void ILayoutController.SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }

        private void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.SetRootDirty(rectTransform);
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            SetDirty();
        }
    #endif
    }
}
