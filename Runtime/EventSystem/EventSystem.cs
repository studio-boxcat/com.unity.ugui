using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Serialization;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Event System")]
    [DisallowMultipleComponent]
    /// <summary>
    /// Handles input, raycasting, and sending events.
    /// </summary>
    /// <remarks>
    /// The EventSystem is responsible for processing and handling events in a Unity scene. A scene should only contain one EventSystem. The EventSystem works in conjunction with a number of modules and mostly just holds state and delegates functionality to specific, overrideable components.
    /// When the EventSystem is started it searches for any BaseInputModules attached to the same GameObject and adds them to an internal list. On update each attached module receives an UpdateModules call, where the module can modify internal state. After each module has been Updated the active module has the Process call executed.This is where custom module processing can take place.
    /// </remarks>
    public class EventSystem : UIBehaviour
    {
        private List<BaseInputModule> m_SystemInputModules = new List<BaseInputModule>();

        private BaseInputModule m_CurrentInputModule;

        private  static List<EventSystem> m_EventSystems = new List<EventSystem>();

        /// <summary>
        /// Return the current EventSystem.
        /// </summary>
        public static EventSystem current
        {
            get { return m_EventSystems.Count > 0 ? m_EventSystems[0] : null; }
            set
            {
                int index = m_EventSystems.IndexOf(value);

                if (index > 0)
                {
                    m_EventSystems.RemoveAt(index);
                    m_EventSystems.Insert(0, value);
                }
                else if (index < 0)
                {
                    Debug.LogError("Failed setting EventSystem.current to unknown EventSystem " + value);
                }
            }
        }

        [SerializeField]
        private int m_DragThreshold = 10;

        /// <summary>
        /// The soft area for dragging in pixels.
        /// </summary>
        public int pixelDragThreshold
        {
            get { return m_DragThreshold; }
            set { m_DragThreshold = value; }
        }

        private GameObject m_CurrentSelected;

        /// <summary>
        /// The currently active EventSystems.BaseInputModule.
        /// </summary>
        public BaseInputModule currentInputModule
        {
            get { return m_CurrentInputModule; }
        }

        /// <summary>
        /// The GameObject currently considered active by the EventSystem.
        /// </summary>
        public GameObject currentSelectedGameObject
        {
            get { return m_CurrentSelected; }
        }

        private bool m_HasFocus = true;

        /// <summary>
        /// Flag to say whether the EventSystem thinks it should be paused or not based upon focused state.
        /// </summary>
        /// <remarks>
        /// Used to determine inside the individual InputModules if the module should be ticked while the application doesnt have focus.
        /// </remarks>
        public bool isFocused
        {
            get { return m_HasFocus; }
        }

        protected EventSystem()
        {}

        /// <summary>
        /// Recalculate the internal list of BaseInputModules.
        /// </summary>
        public void UpdateModules()
        {
            GetComponents(m_SystemInputModules);
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (int i = systemInputModulesCount - 1; i >= 0; i--)
            {
                if (m_SystemInputModules[i] && m_SystemInputModules[i].IsActive())
                    continue;

                m_SystemInputModules.RemoveAt(i);
            }
        }

        private bool m_SelectionGuard;

        /// <summary>
        /// Returns true if the EventSystem is already in a SetSelectedGameObject.
        /// </summary>
        public bool alreadySelecting
        {
            get { return m_SelectionGuard; }
        }

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

        private BaseEventData m_DummyData;
        private BaseEventData baseEventDataCache
        {
            get
            {
                if (m_DummyData == null)
                    m_DummyData = new BaseEventData();

                return m_DummyData;
            }
        }

        /// <summary>
        /// Set the object as selected. Will send an OnDeselect the the old selected object and OnSelect to the new selected object.
        /// </summary>
        /// <param name="selected">GameObject to select.</param>
        public void SetSelectedGameObject(GameObject selected)
        {
            SetSelectedGameObject(selected, baseEventDataCache);
        }

        private static int RaycastComparer(RaycastResult lhs, RaycastResult rhs)
        {
            if (lhs.module != rhs.module)
            {
                var lhsEventCamera = lhs.module.eventCamera;
                var rhsEventCamera = rhs.module.eventCamera;
                if (lhsEventCamera.depth != rhsEventCamera.depth)
                {
                    // need to reverse the standard compareTo
                    if (lhsEventCamera.depth < rhsEventCamera.depth)
                        return 1;
                    if (lhsEventCamera.depth == rhsEventCamera.depth)
                        return 0;

                    return -1;
                }

                if (lhs.module.sortOrderPriority != rhs.module.sortOrderPriority)
                    return rhs.module.sortOrderPriority.CompareTo(lhs.module.sortOrderPriority);

                if (lhs.module.renderOrderPriority != rhs.module.renderOrderPriority)
                    return rhs.module.renderOrderPriority.CompareTo(lhs.module.renderOrderPriority);
            }

            if (lhs.sortingLayer != rhs.sortingLayer)
            {
                // Uses the layer value to properly compare the relative order of the layers.
                var rid = SortingLayer.GetLayerValueFromID(rhs.sortingLayer);
                var lid = SortingLayer.GetLayerValueFromID(lhs.sortingLayer);
                return rid.CompareTo(lid);
            }

            if (lhs.sortingOrder != rhs.sortingOrder)
                return rhs.sortingOrder.CompareTo(lhs.sortingOrder);

            // comparing depth only makes sense if the two raycast results have the same root canvas (case 912396)
            if (lhs.depth != rhs.depth && lhs.module.rootRaycaster == rhs.module.rootRaycaster)
                return rhs.depth.CompareTo(lhs.depth);

            return lhs.index.CompareTo(rhs.index);
        }

        private static readonly Comparison<RaycastResult> s_RaycastComparer = RaycastComparer;

        /// <summary>
        /// Raycast into the scene using all configured BaseRaycasters.
        /// </summary>
        /// <param name="eventData">Current pointer data.</param>
        /// <param name="raycastResults">List of 'hits' to populate.</param>
        public void RaycastAll(Vector2 screenPosition, List<RaycastResult> raycastResults)
        {
            raycastResults.Clear();
            var modules = RaycasterManager.GetRaycasters();
            var modulesCount = modules.Count;
            for (int i = 0; i < modulesCount; ++i)
            {
                var module = modules[i];
                if (module == null || !module.IsActive())
                    continue;

                module.Raycast(screenPosition, raycastResults);
            }

            raycastResults.Sort(s_RaycastComparer);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventSystems.Add(this);
        }

        protected override void OnDisable()
        {
            if (m_CurrentInputModule != null)
            {
                m_CurrentInputModule.DeactivateModule();
                m_CurrentInputModule = null;
            }

            m_EventSystems.Remove(this);

            base.OnDisable();
        }

        private void TickModules()
        {
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (var i = 0; i < systemInputModulesCount; i++)
            {
                if (m_SystemInputModules[i] != null)
                    m_SystemInputModules[i].UpdateModule();
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            m_HasFocus = hasFocus;
            if (!m_HasFocus)
                TickModules();
        }

        protected virtual void Update()
        {
            if (current != this)
                return;
            TickModules();

            bool changedModule = false;
            var systemInputModulesCount = m_SystemInputModules.Count;
            for (var i = 0; i < systemInputModulesCount; i++)
            {
                var module = m_SystemInputModules[i];
                if (module.IsModuleSupported() && module.ShouldActivateModule())
                {
                    if (m_CurrentInputModule != module)
                    {
                        ChangeEventModule(module);
                        changedModule = true;
                    }
                    break;
                }
            }

            // no event module set... set the first valid one...
            if (m_CurrentInputModule == null)
            {
                for (var i = 0; i < systemInputModulesCount; i++)
                {
                    var module = m_SystemInputModules[i];
                    if (module.IsModuleSupported())
                    {
                        ChangeEventModule(module);
                        changedModule = true;
                        break;
                    }
                }
            }

            if (!changedModule && m_CurrentInputModule != null)
                m_CurrentInputModule.Process();

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                int eventSystemCount = 0;
                for (int i = 0; i < m_EventSystems.Count; i++)
                {
                    if (m_EventSystems[i].GetType() == typeof(EventSystem))
                        eventSystemCount++;
                }

                if (eventSystemCount > 1)
                    Debug.LogWarning("There are " + eventSystemCount + " event systems in the scene. Please ensure there is always exactly one event system in the scene");
            }
#endif
        }

        private void ChangeEventModule(BaseInputModule module)
        {
            if (m_CurrentInputModule == module)
                return;

            if (m_CurrentInputModule != null)
                m_CurrentInputModule.DeactivateModule();

            if (module != null)
                module.ActivateModule();
            m_CurrentInputModule = module;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Selected:</b>" + currentSelectedGameObject);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(m_CurrentInputModule != null ? m_CurrentInputModule.ToString() : "No module");
            return sb.ToString();
        }
    }
}
