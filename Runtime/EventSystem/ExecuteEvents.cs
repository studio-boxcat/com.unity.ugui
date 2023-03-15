using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.EventSystems
{
    public static class ExecuteEvents
    {
        public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);

        public static T ValidateEventData<T>(BaseEventData data) where T : class
        {
            if ((data as T) == null)
                throw new ArgumentException(String.Format("Invalid type: {0} passed to event expecting {1}", data.GetType(), typeof(T)));
            return data as T;
        }

        private static readonly EventFunction<IPointerMoveHandler> s_PointerMoveHandler = Execute;

        private static void Execute(IPointerMoveHandler handler, BaseEventData eventData)
        {
            handler.OnPointerMove(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerEnterHandler> s_PointerEnterHandler = Execute;

        private static void Execute(IPointerEnterHandler handler, BaseEventData eventData)
        {
            handler.OnPointerEnter(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerExitHandler> s_PointerExitHandler = Execute;

        private static void Execute(IPointerExitHandler handler, BaseEventData eventData)
        {
            handler.OnPointerExit(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerDownHandler> s_PointerDownHandler = Execute;

        private static void Execute(IPointerDownHandler handler, BaseEventData eventData)
        {
            handler.OnPointerDown(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerUpHandler> s_PointerUpHandler = Execute;

        private static void Execute(IPointerUpHandler handler, BaseEventData eventData)
        {
            handler.OnPointerUp(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IPointerClickHandler> s_PointerClickHandler = Execute;

        private static void Execute(IPointerClickHandler handler, BaseEventData eventData)
        {
            handler.OnPointerClick(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IInitializePotentialDragHandler> s_InitializePotentialDragHandler = Execute;

        private static void Execute(IInitializePotentialDragHandler handler, BaseEventData eventData)
        {
            handler.OnInitializePotentialDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IBeginDragHandler> s_BeginDragHandler = Execute;

        private static void Execute(IBeginDragHandler handler, BaseEventData eventData)
        {
            handler.OnBeginDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IDragHandler> s_DragHandler = Execute;

        private static void Execute(IDragHandler handler, BaseEventData eventData)
        {
            handler.OnDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IEndDragHandler> s_EndDragHandler = Execute;

        private static void Execute(IEndDragHandler handler, BaseEventData eventData)
        {
            handler.OnEndDrag(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IDropHandler> s_DropHandler = Execute;

        private static void Execute(IDropHandler handler, BaseEventData eventData)
        {
            handler.OnDrop(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IScrollHandler> s_ScrollHandler = Execute;

        private static void Execute(IScrollHandler handler, BaseEventData eventData)
        {
            handler.OnScroll(ValidateEventData<PointerEventData>(eventData));
        }

        private static readonly EventFunction<IUpdateSelectedHandler> s_UpdateSelectedHandler = Execute;

        private static void Execute(IUpdateSelectedHandler handler, BaseEventData eventData)
        {
            handler.OnUpdateSelected(eventData);
        }

        private static readonly EventFunction<ISelectHandler> s_SelectHandler = Execute;

        private static void Execute(ISelectHandler handler, BaseEventData eventData)
        {
            handler.OnSelect(eventData);
        }

        private static readonly EventFunction<IDeselectHandler> s_DeselectHandler = Execute;

        private static void Execute(IDeselectHandler handler, BaseEventData eventData)
        {
            handler.OnDeselect(eventData);
        }

        private static readonly EventFunction<ISubmitHandler> s_SubmitHandler = Execute;

        private static void Execute(ISubmitHandler handler, BaseEventData eventData)
        {
            handler.OnSubmit(eventData);
        }

        private static readonly EventFunction<ICancelHandler> s_CancelHandler = Execute;

        private static void Execute(ICancelHandler handler, BaseEventData eventData)
        {
            handler.OnCancel(eventData);
        }

        public static EventFunction<IPointerMoveHandler> pointerMoveHandler
        {
            get { return s_PointerMoveHandler; }
        }

        public static EventFunction<IPointerEnterHandler> pointerEnterHandler
        {
            get { return s_PointerEnterHandler; }
        }

        public static EventFunction<IPointerExitHandler> pointerExitHandler
        {
            get { return s_PointerExitHandler; }
        }

        public static EventFunction<IPointerDownHandler> pointerDownHandler
        {
            get { return s_PointerDownHandler; }
        }

        public static EventFunction<IPointerUpHandler> pointerUpHandler
        {
            get { return s_PointerUpHandler; }
        }

        public static EventFunction<IPointerClickHandler> pointerClickHandler
        {
            get { return s_PointerClickHandler; }
        }

        public static EventFunction<IInitializePotentialDragHandler> initializePotentialDrag
        {
            get { return s_InitializePotentialDragHandler; }
        }

        public static EventFunction<IBeginDragHandler> beginDragHandler
        {
            get { return s_BeginDragHandler; }
        }

        public static EventFunction<IDragHandler> dragHandler
        {
            get { return s_DragHandler; }
        }

        public static EventFunction<IEndDragHandler> endDragHandler
        {
            get { return s_EndDragHandler; }
        }

        public static EventFunction<IDropHandler> dropHandler
        {
            get { return s_DropHandler; }
        }

        public static EventFunction<IScrollHandler> scrollHandler
        {
            get { return s_ScrollHandler; }
        }

        public static EventFunction<IUpdateSelectedHandler> updateSelectedHandler
        {
            get { return s_UpdateSelectedHandler; }
        }

        public static EventFunction<ISelectHandler> selectHandler
        {
            get { return s_SelectHandler; }
        }

        public static EventFunction<IDeselectHandler> deselectHandler
        {
            get { return s_DeselectHandler; }
        }

        public static EventFunction<ISubmitHandler> submitHandler
        {
            get { return s_SubmitHandler; }
        }

        public static EventFunction<ICancelHandler> cancelHandler
        {
            get { return s_CancelHandler; }
        }

        public static bool Execute<T>(GameObject target, BaseEventData eventData, EventFunction<T> functor) where T : class, IEventSystemHandler
        {
            if (target == null || !target.activeInHierarchy)
                return false;

            var internalHandlers = ListPool<T>.Get();
            target.GetComponents(internalHandlers);

            var executed = false;
            foreach (var arg in internalHandlers)
            {
                // If the object is disabled, don't execute the event.
                if (arg is Behaviour {isActiveAndEnabled: false})
                    continue;

                executed = true;

                try
                {
                    functor(arg, eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            ListPool<T>.Release(internalHandlers);
            return executed;
        }

        public static GameObject ExecuteHierarchy<T>(GameObject root, BaseEventData eventData, EventFunction<T> callbackFunction) where T : class, IEventSystemHandler
        {
            if (root == null)
                return null;

            var t = root.transform;
            while (t is not null)
            {
                var go = t.gameObject;
                if (Execute(go, eventData, callbackFunction))
                    return go;

                t = t.parent;
            }

            return null;
        }

        /// <summary>
        /// Bubble the specified event on the game object, figuring out which object will actually receive the event.
        /// </summary>
        public static GameObject GetEventHandler<T>(GameObject root) where T : class, IEventSystemHandler
        {
            if (root == null)
                return null;

            var t = root.transform;
            var buffer = ListPool<T>.Get();

            while (t is not null)
            {
                var go = t.gameObject;
                if (CanHandleEvent(go, buffer))
                {
                    ListPool<T>.Release(buffer);
                    return go;
                }

                t = t.parent;
            }

            ListPool<T>.Release(buffer);
            return null;

            static bool CanHandleEvent(GameObject go, List<T> buffer)
            {
                if (!go.activeInHierarchy)
                    return false;

                go.GetComponents(buffer);

                foreach (var component in buffer)
                {
                    // When component is a Behaviour, we need to check if it is enabled.
                    if (component is Behaviour behaviour)
                    {
                        if (behaviour.isActiveAndEnabled)
                            return true;
                    }
                    // If not, we can just return true as it's not possible to be disabled.
                    else
                    {
                        return true;
                    }
                }

                // If we get here, it means that the game object has no components that can handle the event.
                return false;
            }
        }
    }
}
