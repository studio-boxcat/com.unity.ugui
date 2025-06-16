using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Selectable", 35)]
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    /// <summary>
    /// Simple selectable object - derived from to create a selectable control.
    /// </summary>
    public class Selectable
        :
        UIBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Tooltip("Can the Selectable be interacted with?")]
        [SerializeField]
        private bool m_Interactable = true;

        private InteractabilityResolver m_GroupsAllowInteraction;

        public bool              interactable
        {
            get { return m_Interactable; }
            set
            {
                if (SetPropertyUtility.SetValue(ref m_Interactable, value))
                {
                    if (!m_Interactable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                        EventSystem.current.SetSelectedGameObject(null);
                    OnSetProperty();
                }
            }
        }

        public bool              isPointerDown     { get; private set; }
        private bool             hasSelection      { get; set; }

        void OnCanvasGroupChanged()
        {
            // When the pointer is currently down, we need to re-evaluate the interaction state immediately to apple the correct state.
            if (isPointerDown)
            {
                var interactable = m_GroupsAllowInteraction.Reevaluate(this);
                if (interactable == false) OnSetProperty();
            }
            // Otherwise we can just mark the interaction state as dirty and it will be re-evaluated the next time it is needed.
            else
            {
                m_GroupsAllowInteraction.SetDirty();
            }
        }

        // If our parenting changes figure out if we are under a new CanvasGroup.
        void OnTransformParentChanged() => OnCanvasGroupChanged();

        public bool IsInteractable()
        {
            return m_Interactable && m_GroupsAllowInteraction.IsInteractable(this);
        }

        // Call from unity if animation properties have changed
        protected virtual void OnDidApplyAnimationProperties()
        {
            OnSetProperty();
        }

        // Select on enable and add to the list.
        protected virtual void OnEnable()
        {
            if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                hasSelection = true;
            }

            isPointerDown = false;
            DoStateTransition(isPointerDown);
        }

        void OnSetProperty()
        {
            DoStateTransition(isPointerDown);
        }

        // Remove from the list.
        protected virtual void OnDisable()
        {
            InstantClearState();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsPressed())
                InstantClearState();
        }

        /// <summary>
        /// Clear any internal state from the Selectable (used when disabling).
        /// </summary>
        void InstantClearState()
        {
            isPointerDown = false;
            hasSelection = false;
        }

        /// <summary>
        /// Transition the Selectable to the entered state.
        /// </summary>
        protected virtual void DoStateTransition(bool pressed)
        {
        }

        /// <summary>
        /// Whether the current selectable is being pressed.
        /// </summary>
        bool IsPressed()
        {
            // If the pointer is not down, we are not pressed anyway.
            if (isPointerDown == false)
                return false;
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerDown;
        }

        // Change the button to the correct state
        void EvaluateAndTransitionToSelectionState()
        {
            if (IsActive() && IsInteractable())
                DoStateTransition(isPointerDown);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Selection tracking
            if (IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);

            isPointerDown = true;
            EvaluateAndTransitionToSelectionState();
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            EvaluateAndTransitionToSelectionState();
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            EvaluateAndTransitionToSelectionState();
        }

        public void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
