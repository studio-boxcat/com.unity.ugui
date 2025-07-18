using System;
using JetBrains.Annotations;

namespace UnityEngine.EventSystems
{
    public static class ExecuteEvents
    {
        public delegate void BaseEventFunc<in T>(T handler, BaseEventData eventData);
        public delegate void PointerEventFunc<in T>(T handler, PointerEventData eventData);

        public static PointerEventFunc<IPointerEnterHandler> pointerEnterHandler = (h, e) => h.OnPointerEnter(e);
        public static PointerEventFunc<IPointerExitHandler> pointerExitHandler = (h, e) => h.OnPointerExit(e);
        public static PointerEventFunc<IPointerDownHandler> pointerDownHandler = (h, e) => h.OnPointerDown(e);
        public static PointerEventFunc<IPointerUpHandler> pointerUpHandler = (h, e) => h.OnPointerUp(e);
        public static PointerEventFunc<IPointerClickHandler> pointerClickHandler = (h, e) => h.OnPointerClick(e);
        public static PointerEventFunc<IInitializePotentialDragHandler> initializePotentialDrag = (h, e) => h.OnInitializePotentialDrag(e);
        public static PointerEventFunc<IBeginDragHandler> beginDragHandler = (h, e) => h.OnBeginDrag(e);
        public static PointerEventFunc<IDragHandler> dragHandler = (h, e) => h.OnDrag(e);
        public static PointerEventFunc<IEndDragHandler> endDragHandler = (h, e) => h.OnEndDrag(e);
        public static PointerEventFunc<IDropHandler> dropHandler = (h, e) => h.OnDrop(e);
        public static PointerEventFunc<IScrollHandler> scrollHandler = (h, e) => h.OnScroll(e);
        public static BaseEventFunc<IUpdateSelectedHandler> updateSelectedHandler = (h, e) => h.OnUpdateSelected(e);
        public static BaseEventFunc<ISelectHandler> selectHandler = (h, e) => h.OnSelect(e);
        public static BaseEventFunc<IDeselectHandler> deselectHandler = (h, e) => h.OnDeselect(e);

        public static bool Execute<T>(GameObject target, PointerEventData eventData, PointerEventFunc<T> functor) where T : class, IEventSystemHandler
        {
            if (target == null || !target.activeInHierarchy)
                return false;

            using var _ = CompBuf.GetComponents(target, typeof(T), out var internalHandlers);

            var executed = false;
            foreach (var arg in internalHandlers)
            {
                // If the object is disabled, don't execute the event.
                if (arg is Behaviour { isActiveAndEnabled: false })
                    continue;

                executed = true;

#if DEBUG
                var actionName = GetActionName(functor);
                if (actionName != null)
                    L.I($"[UGUI] Pointer {actionName}: {target.name}, type={arg.GetType().Name}, frame={Time.frameCount}", target);
#endif

                try
                {
                    functor(arg as T, eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, arg);
                }
            }

            return executed;

#if DEBUG
            [CanBeNull]
            static string GetActionName(PointerEventFunc<T> functor)
            {
                if (ReferenceEquals(functor, pointerClickHandler)) return "Click";
                if (ReferenceEquals(functor, pointerDownHandler)) return "Down";
                if (ReferenceEquals(functor, pointerUpHandler)) return "Up";
                if (ReferenceEquals(functor, initializePotentialDrag)) return "Initialize Drag";
                if (ReferenceEquals(functor, beginDragHandler)) return "Begin Drag";
                if (ReferenceEquals(functor, endDragHandler)) return "End Drag";
                return null;
            }
#endif
        }

        public static bool Execute<T>(GameObject target, BaseEventData eventData, BaseEventFunc<T> functor) where T : class, IEventSystemHandler
        {
            if (target == null || !target.activeInHierarchy)
                return false;

            using var _ = CompBuf.GetComponents(target, typeof(T), out var internalHandlers);

            var executed = false;
            foreach (var arg in internalHandlers)
            {
                // If the object is disabled, don't execute the event.
                if (arg is Behaviour { isActiveAndEnabled: false })
                    continue;

                executed = true;

                try
                {
                    functor(arg as T, eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, arg);
                }
            }

            return executed;
        }

        public static GameObject ExecuteHierarchy<T>(GameObject root, PointerEventData eventData, PointerEventFunc<T> callbackFunction) where T : class, IEventSystemHandler
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
        [MustUseReturnValue, CanBeNull]
        public static GameObject GetEventHandler<T>(GameObject root) where T : class, IEventSystemHandler
        {
            if (root == null)
                return null;

            if (root.activeInHierarchy is false)
                return null;

            var t = root.transform;
            do
            {
                if (ComponentSearch.AnyEnabledComponent<T>(t))
                    return t.gameObject;
                t = t.parent;
            } while (t is not null);

            return null;
        }
    }
}