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
            if (value.TryGetComponent<Canvas>(out _) == false)
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "Canvas must be attached.";
            }
        }
    }
}