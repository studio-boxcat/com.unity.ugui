using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public interface IClickablePressedHandler
    {
        /// <summary>
        /// Called when the pointer is pressed down on the Clickable.
        /// Only interactable Clickable will receive this event.
        /// </summary>
        void OnPressed(Clickable sender);
    }

    public interface IClickableReleasedHandler
    {
        /// <summary>
        /// Called when the pointer is released on the Clickable, where the pointer was eligible for click when pressed down.
        /// Even after interactable is set to false, this event will be called.
        /// </summary>
        void OnReleased(Clickable sender);
    }

    public interface IClickableClickHandler
    {
        void OnClick(Clickable sender);
    }

    [DisallowMultipleComponent]
    public class Clickable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField, OnValueChanged(nameof(SetInteractable))]
        bool _interactable = true;

        bool _isPointerDowned;
        bool _eligibleForClick;
        InteractabilityResolver _groupsAllowInteraction;

        void OnEnable()
        {
            _isPointerDowned = false;
            _eligibleForClick = false;
        }

        void OnDisable()
        {
            _isPointerDowned = false;
            _eligibleForClick = false;
        }

        void OnTransformParentChanged() => _groupsAllowInteraction.SetDirty();
        void OnCanvasGroupChanged() => _groupsAllowInteraction.SetDirty();

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _eligibleForClick)
                _eligibleForClick = false;
        }

        public bool IsInteractable() => _interactable && _groupsAllowInteraction.IsInteractable(this);

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (value == false)
                _eligibleForClick = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Assert.IsFalse(_isPointerDowned);
            Assert.IsFalse(_eligibleForClick);

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (IsInteractable() == false) return;

            _isPointerDowned = true;
            _eligibleForClick = true;
            InvokePressedEvent(this);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var wasPointerDowned = _isPointerDowned;
            _isPointerDowned = false;
            _eligibleForClick = false;

            // If interactable was set to false when pointer was downed, we should not invoke released event.
            if (wasPointerDowned)
                InvokeReleasedEvent(this);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_eligibleForClick == false) return;
            if (IsInteractable() == false)
            {
                _eligibleForClick = false;
                return;
            }

            InvokeClickEvent(this);
        }

        static readonly List<IClickablePressedHandler> _pressedHandlerBuffer = new();
        static void InvokePressedEvent(Clickable target)
        {
            target.GetComponents(_pressedHandlerBuffer);
            foreach (var handler in _pressedHandlerBuffer)
            {
                // If handler is disabled, just skip it.
                if (handler is Behaviour {isActiveAndEnabled: false})
                    continue;
                handler.OnPressed(target);
            }
        }

        static readonly List<IClickableReleasedHandler> _releasedHandlerBuffer = new();
        static void InvokeReleasedEvent(Clickable target)
        {
            target.GetComponents(_releasedHandlerBuffer);
            foreach (var handler in _releasedHandlerBuffer)
            {
                // If handler is disabled, just skip it.
                if (handler is Behaviour {isActiveAndEnabled: false})
                    continue;
                handler.OnReleased(target);
            }
        }

        static readonly List<IClickableClickHandler> _clickHandlerBuffer = new();
        static void InvokeClickEvent(Clickable target)
        {
            target.GetComponents(_clickHandlerBuffer);
            foreach (var handler in _clickHandlerBuffer)
            {
                // If handler is disabled, just skip it.
                if (handler is Behaviour {isActiveAndEnabled: false})
                    continue;
                handler.OnClick(target);
            }
        }
    }
}