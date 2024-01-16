using UnityEngine.Serialization;
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

        /// <summary>
        /// Is this object interactable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///     public bool playersReady;
        ///
        ///
        ///     void Update()
        ///     {
        ///         // checks if the players are ready and if the start button is useable
        ///         if (playersReady == true && startButton.interactable == false)
        ///         {
        ///             //allows the start button to be used
        ///             startButton.interactable = true;
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
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

        /// <summary>
        /// Convenience function to get the Animator component on the GameObject.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     private Animator buttonAnimator;
        ///     public Button button;
        ///
        ///     void Start()
        ///     {
        ///         //Assigns the "buttonAnimator" with the button's animator.
        ///         buttonAnimator = button.animator;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
#if PACKAGE_ANIMATION
        Animator m_Animator;
        public Animator animator
        {
            get
            {
                if (m_Animator is null)
                    TryGetComponent(out m_Animator);
                return m_Animator;
            }
        }
#endif

        protected virtual void OnCanvasGroupChanged()
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

        /// <summary>
        /// Is the object interactable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Button startButton;
        ///
        ///     void Update()
        ///     {
        ///         if (!startButton.IsInteractable())
        ///         {
        ///             Debug.Log("Start Button has been Disabled");
        ///         }
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual bool IsInteractable()
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

        protected virtual void OnTransformParentChanged()
        {
            // If our parenting changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }

        private void OnSetProperty()
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
            {
                InstantClearState();
            }
        }

        /// <summary>
        /// Clear any internal state from the Selectable (used when disabling).
        /// </summary>
        private void InstantClearState()
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
        protected bool IsPressed()
        {
            // If the pointer is not down, we are not pressed anyway.
            if (isPointerDown == false)
                return false;
            if (!IsActive() || !IsInteractable())
                return false;
            return isPointerDown;
        }

        // Change the button to the correct state
        private void EvaluateAndTransitionToSelectionState()
        {
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(isPointerDown);
        }

        /// <summary>
        /// Evaluate current state and transition to pressed state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerDownHandler// required interface when using the OnPointerDown method.
        /// {
        ///     //Do this when the mouse is clicked over the selectable object this script is attached to.
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " Was Clicked.");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
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

        /// <summary>
        /// Evaluate eventData and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IPointerUpHandler, IPointerDownHandler// These are the interfaces the OnPointerUp method requires.
        /// {
        ///     //OnPointerDown is also required to receive OnPointerUp callbacks
        ///     public void OnPointerDown(PointerEventData eventData)
        ///     {
        ///     }
        ///
        ///     //Do this when the mouse click on this selectable UI object is released.
        ///     public void OnPointerUp(PointerEventData eventData)
        ///     {
        ///         Debug.Log("The mouse click was released");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Set selection and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, ISelectHandler// required interface when using the OnSelect method.
        /// {
        ///     //Do this when the selectable UI object is selected.
        ///     public void OnSelect(BaseEventData eventData)
        ///     {
        ///         Debug.Log(this.gameObject.name + " was selected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Unset selection and transition to appropriate state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI;
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour, IDeselectHandler //This Interface is required to receive OnDeselect callbacks.
        /// {
        ///     public void OnDeselect(BaseEventData data)
        ///     {
        ///         Debug.Log("Deselected");
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            EvaluateAndTransitionToSelectionState();
        }

        /// <summary>
        /// Selects this Selectable.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // required when using UI elements in scripts
        /// using UnityEngine.EventSystems;// Required when using Event data.
        ///
        /// public class ExampleClass : MonoBehaviour// required interface when using the OnSelect method.
        /// {
        ///     public InputField myInputField;
        ///
        ///     //Do this OnClick.
        ///     public void SaveGame()
        ///     {
        ///         //Makes the Input Field the selected UI Element.
        ///         myInputField.Select();
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
