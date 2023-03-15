using System;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [DisallowMultipleComponent]
    public class Clickable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public event Action<Object> OnClick;

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

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_eligibleForClick == false) return;
            if (IsInteractable() == false)
            {
                _eligibleForClick = false;
                return;
            }

            OnClick?.Invoke(this);
        }
    }
}