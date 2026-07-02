using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public sealed class Scrollbar : Selectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required]
        private RectTransform m_HandleRect;
        [SerializeField]
        private Axis m_Direction;

        [Range(0f, 1f)]
        [SerializeField]
        private float m_Value;

        /// <summary>
        /// The current value of the scrollbar, between 0 and 1.
        /// </summary>
        public float value
        {
            get => m_Value;
            set => Set(value);
        }

        [Range(0f, 1f)]
        [SerializeField]
        private float m_Size = 0.2f;

        /// <summary>
        /// The size of the scrollbar handle where 1 means it fills the entire scrollbar.
        /// </summary>
        public float size { get { return m_Size; } set { if (SetPropertyUtility.SetValue(ref m_Size, Mathf.Clamp01(value))) UpdateVisuals(); } }

        // Private fields

        // The offset from handle position to mouse down position
        private Vector2 m_Offset = Vector2.zero;

        private bool isPointerDownAndNotDragging = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            Set(m_Value);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        void Set(float input)
        {
            float currentValue = m_Value;

            // bugfix (case 802330) clamp01 input in callee before calling this function, this allows inertia from dragging content to go past extremities without being clamped
            m_Value = input;

            // Skip redundant updates.
            if (currentValue == value)
                return;

            UpdateVisuals();
        }

        private void OnRectTransformDimensionsChange()
        {
            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        // Force-update the scroll bar. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.one;

            float movement = Mathf.Clamp01(value) * (1 - size);
            anchorMin[(int)m_Direction] = movement;
            anchorMax[(int)m_Direction] = movement + size;

            m_HandleRect.anchorMin = anchorMin;
            m_HandleRect.anchorMax = anchorMax;
        }

        // Update the scroll bar's position based on the mouse.
        private void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            var position = eventData.position;
            var containerRect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, position, eventData.pressEventCamera, out var localCursor))
                return;

            Vector2 handleCenterRelativeToContainerCorner = localCursor - m_Offset - containerRect.rect.position;
            Vector2 handleCorner = handleCenterRelativeToContainerCorner - (m_HandleRect.rect.size - m_HandleRect.sizeDelta) * 0.5f;

            float parentSize = m_Direction == 0 ? containerRect.rect.width : containerRect.rect.height;
            float remainingSize = parentSize * (1 - size);
            if (remainingSize <= 0)
                return;

            DoUpdateDrag(handleCorner, remainingSize);
        }

        //this function is testable, it is found using reflection in ScrollbarClamp test
        private void DoUpdateDrag(Vector2 handleCorner, float remainingSize)
        {
            // Same low-to-high mapping as UpdateVisuals/ClickRepeat: value 1 = right/top.
            Set(Mathf.Clamp01(handleCorner[(int)m_Direction] / remainingSize));
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// Handling for when the scrollbar value is begin being dragged.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            // Keep the grab offset only when the press started on the handle; for track presses
            // (ClickRepeat may have paged the handle since) drag with zero offset so the handle
            // snaps under the finger instead of dragging remotely.
            m_Offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localMousePos))
                m_Offset = localMousePos - m_HandleRect.rect.center;
        }

        /// <summary>
        /// Handling for when the scrollbar value is dragged.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (MayDrag(eventData))
                UpdateDrag(eventData);
        }

        /// <summary>
        /// Event triggered when pointer is pressed down on the scrollbar.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);
            isPointerDownAndNotDragging = true;
            StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));
        }

        /// <summary>
        /// Coroutine function for handling continual press during Scrollbar.OnPointerDown.
        /// </summary>
        private IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, screenPosition, camera))
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, screenPosition, camera, out var localMousePos))
                    {
                        var axisCoordinate = m_Direction == 0 ? localMousePos.x : localMousePos.y;

                        // modifying value depending on direction, fixes (case 925824)

                        value += axisCoordinate > 0 ? size : -size;
                        value = Mathf.Clamp01(value);
                        // Only keep 4 decimals of precision
                        value = Mathf.Round(value * 10000f) / 10000f;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        /// <summary>
        /// Event triggered when pointer is released after pressing on the scrollbar.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            isPointerDownAndNotDragging = false;
        }

        /// <summary>
        /// See: IInitializePotentialDragHandler.OnInitializePotentialDrag
        /// </summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_HandleRect && DrivenRectTransManager.Reset(this, out var t))
                t.Set(m_HandleRect, DrivenTransformProperties.Anchors);
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // UpdateDrag maps the cursor over the Scrollbar's own rect while UpdateVisuals writes anchors
            // relative to the handle's parent - the two only agree when the handle is a direct child.
            if (m_HandleRect && m_HandleRect.parent != transform)
                result.AddError("Handle must be a direct child of the Scrollbar.");
        }
#endif
    }
}
