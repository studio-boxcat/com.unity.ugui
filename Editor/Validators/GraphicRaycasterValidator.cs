using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine.EventSystems;

[assembly: RegisterValidator(typeof(UnityEngine.UI.GraphicRaycasterValidator))]

namespace UnityEngine.UI
{
    public class GraphicRaycasterValidator : ValueValidator<GraphicRaycaster>
    {
        protected override void Validate(Sirenix.OdinInspector.Editor.Validation.ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            if (value is null) return;

            // CanvasRenderer must be attached.
            if (value.TryGetComponent<CanvasRenderer>(out _) == false)
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "CanvasRenderer must be attached.";
            }

            // Canvas must be attached.
            if (value.TryGetComponent<Canvas>(out var canvas) == false)
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "Canvas must be attached.";
            }

            // Canvas must override sorting if it is not the root canvas.
            // This is because the sorting order of the root canvas is used to determine to which canvas the raycast should be sent.
            if (ReferenceEquals(canvas, canvas.rootCanvas) == false)
            {
                if (canvas.overrideSorting == false)
                {
                    result.ResultType = ValidationResultType.Error;
                    result.Message = "Canvas must override sorting if it is not the root canvas.";
                }
            }
        }
    }
}