using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public sealed class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required]
        private RectTransform m_FillRect;
        [SerializeField, Required]
        private RectTransform m_HandleRect;
        [SerializeField]
        private Axis m_Direction = Axis.X;
        [SerializeField]
        private float m_MinValue = 0;
        [SerializeField]
        private float m_MaxValue = 1;
        [SerializeField]
        private bool m_WholeNumbers = false;
        [SerializeField]
        private float m_Value;
        public float value
        {
            get => m_WholeNumbers ? Mathf.Round(m_Value) : m_Value;
            set => Set(value);
        }
        public float normalizedValue
        {
            get
            {
                if (Mathf.Approximately(m_MinValue, m_MaxValue))
                    return 0;
                return Mathf.InverseLerp(m_MinValue, m_MaxValue, value);
            }
            set
            {
                this.value = Mathf.Lerp(m_MinValue, m_MaxValue, value);
            }
        }

        // The offset from handle position to mouse down position
        private Vector2 m_Offset = Vector2.zero;

        protected override void OnEnable()
        {
            base.OnEnable();
            Set(m_Value);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, m_MinValue, m_MaxValue);
            if (m_WholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        private void Set(float input)
        {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            UpdateVisuals();
        }

        private void OnRectTransformDimensionsChange()
        {
            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
            var anchorMax = Vector2.one;
            anchorMax.SetComp(m_Direction, normalizedValue);
            m_FillRect.anchorMin = Vector2.zero;
            m_FillRect.anchorMax = anchorMax;

            // Handle: point-anchored at normalizedValue on the slide axis, stretched on the other.
            var handleMin = Vector2.zero;
            handleMin.SetComp(m_Direction, normalizedValue);
            m_HandleRect.anchorMin = handleMin;
            m_HandleRect.anchorMax = anchorMax;
        }

        // Update the slider's position based on the mouse.
        private void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            var clickRect = (RectTransform)transform; // self
            var position = eventData.position;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out var localCursor))
                return;
            localCursor -= clickRect.rect.position;

            var size = clickRect.rect.size[(int)m_Direction];
            if (size <= 0) // collapsed drag axis - 0/0 would NaN-poison m_Value and the anchors
                return;
            normalizedValue = Mathf.Clamp01((localCursor - m_Offset)[(int)m_Direction] / size);
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            m_Offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localMousePos))
                    m_Offset = localMousePos;
            }
            else
            {
                // Outside the slider handle - jump to this point instead
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DrivenRectTransManager.Reset(this, out var tracker))
            {
                if (m_FillRect)
                    tracker.Set(m_FillRect, DrivenTransformProperties.Anchors);
                if (m_HandleRect)
                    tracker.Set(m_HandleRect, DrivenTransformProperties.Anchors);
            }
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // UpdateDrag maps the cursor over the Slider's own rect while UpdateVisuals writes anchors
            // relative to each rect's parent - the two only agree when both are direct children.
            if (m_FillRect && m_FillRect.parent != transform)
                result.AddError("Fill must be a direct child of the Slider.");
            if (m_HandleRect && m_HandleRect.parent != transform)
                result.AddError("Handle must be a direct child of the Slider.");
        }
#endif // if UNITY_EDITOR
    }
}
