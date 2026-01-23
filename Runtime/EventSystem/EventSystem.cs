using System.Text;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Event System")]
    [DisallowMultipleComponent]
    public sealed class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; private set; }

        [SerializeField, Required, ChildGameObjectsOnly]
        BaseInputModule m_InputModule;

        [SerializeField]
        int m_DragThreshold = 10;

        /// <summary>
        /// The soft area for dragging in pixels.
        /// </summary>
        public int pixelDragThreshold => m_DragThreshold;

        GameObject m_CurrentSelected;

        /// <summary>
        /// The GameObject currently considered active by the EventSystem.
        /// </summary>
        public GameObject currentSelectedGameObject => m_CurrentSelected;

        bool m_HasFocus = true;

        /// <summary>
        /// Flag to say whether the EventSystem thinks it should be paused or not based upon focused state.
        /// </summary>
        /// <remarks>
        /// Used to determine inside the individual InputModules if the module should be ticked while the application doesnt have focus.
        /// </remarks>
        public bool isFocused => m_HasFocus;

        bool m_SelectionGuard;

        /// <summary>
        /// Returns true if the EventSystem is already in a SetSelectedGameObject.
        /// </summary>
        public bool alreadySelecting => m_SelectionGuard;

        /// <summary>
        /// Set the object as selected. Will send an OnDeselect the the old selected object and OnSelect to the new selected object.
        /// </summary>
        /// <param name="selected">GameObject to select.</param>
        /// <param name="pointer">Associated EventData.</param>
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer)
        {
            if (m_SelectionGuard)
            {
                Debug.LogError("Attempting to select " + selected +  "while already selecting an object.");
                return;
            }

            m_SelectionGuard = true;
            if (selected == m_CurrentSelected)
            {
                m_SelectionGuard = false;
                return;
            }

            // Debug.Log("Selection: new (" + selected + ") old (" + m_CurrentSelected + ")");
            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.deselectHandler);
            m_CurrentSelected = selected;
            ExecuteEvents.Execute(m_CurrentSelected, pointer, ExecuteEvents.selectHandler);
            m_SelectionGuard = false;
        }

        static BaseEventData m_DummyData;

        public void SetSelectedGameObject(GameObject selected) => SetSelectedGameObject(selected, m_DummyData ??= new BaseEventData());

        void OnEnable()
        {
            Assert.IsNull(current, "Cannot have more than one EventSystem at a time");
            current = this;
        }

        void OnDisable()
        {
            if (ReferenceEquals(current, this))
            {
                current = null;
            }
            else
            {
                L.E("[EventSystem] EventSystem.current must be set to this");
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            m_HasFocus = hasFocus;
            if (!m_HasFocus)
                m_InputModule.UpdateModule();
        }

        void Update()
        {
            if (current.RefNq(this))
                return;
            m_InputModule.UpdateModule();
            m_InputModule.Process();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Selected:</b>" + currentSelectedGameObject);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(m_InputModule != null ? m_InputModule.ToString() : "No module");
            return sb.ToString();
        }
    }
}
