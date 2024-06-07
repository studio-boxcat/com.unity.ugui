using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Layout Element", 140)]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class LayoutElement : UIBehaviour, ILayoutElement
    {
        [SerializeField, HorizontalGroup("Min")]
        float m_MinWidth = -1;
        [SerializeField, HorizontalGroup("Min")]
        float m_MinHeight = -1;
        [SerializeField, HorizontalGroup("Preferred")]
        float m_PreferredWidth = -1;
        [SerializeField, HorizontalGroup("Preferred")]
        float m_PreferredHeight = -1;

        public float minWidth => m_MinWidth;
        public float minHeight => m_MinHeight;
        public float preferredWidth
        {
            get => m_PreferredWidth;
            set => SetProperty(ref m_PreferredWidth, value);
        }
        public float preferredHeight
        {
            get => m_PreferredHeight;
            set => SetProperty(ref m_PreferredHeight, value);
        }
        public int layoutPriority => 1;

        void OnEnable() => SetDirty();
        void OnDisable() => SetDirty();
        void OnTransformParentChanged() => SetDirtyIfActive();
        void OnBeforeTransformParentChanged() => SetDirtyIfActive();
        void OnDidApplyAnimationProperties() => SetDirtyIfActive();

        void SetDirty()
        {
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform) transform);
        }

        void SetDirtyIfActive()
        {
            if (isActiveAndEnabled)
                SetDirty();
        }

        void SetProperty(ref float currentValue, float newValue)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (currentValue == newValue)
                return;
            currentValue = newValue;
            SetDirtyIfActive();
        }

#if UNITY_EDITOR
        void OnValidate() => SetDirtyIfActive();
#endif
    }
}