using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public interface IClickHandler
    {
        void OnClick(Clickable sender);
    }

    [DisallowMultipleComponent]
    public class Clickable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField, ValidateInput(nameof(SetInteractable))]
        bool _interactable = true;

        bool _eligibleForClick;
        InteractabilityResolver _groupsAllowInteraction;

        void OnEnable() => _eligibleForClick = false;
        void OnDisable() => _eligibleForClick = false;

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
            Assert.IsFalse(_eligibleForClick);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (IsInteractable() == false) return;

            _eligibleForClick = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _eligibleForClick = false;
        }

        static readonly List<IClickHandler> _handlerBuffer = new();

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_eligibleForClick == false) return;
            if (IsInteractable() == false)
            {
                _eligibleForClick = false;
                return;
            }

            GetComponents(_handlerBuffer);
            foreach (var handler in _handlerBuffer)
            {
                // If handler is disabled, just skip it.
                if (handler is Behaviour {isActiveAndEnabled: false})
                    continue;
                handler.OnClick(this);
            }
        }
    }
}