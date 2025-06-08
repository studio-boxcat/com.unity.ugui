using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(UnityEngine.UI.CanvasRendererValidator))]

namespace UnityEngine.UI
{
    public class CanvasRendererValidator : ValueValidator<CanvasRenderer>
    {
        protected override void Validate(Sirenix.OdinInspector.Editor.Validation.ValidationResult result)
        {
            var value = ValueEntry.SmartValue;
            if (value is null) return;

            // Graphic must be attached.
            if (value.TryGetComponent<Graphic>(out _) == false)
            {
                result.ResultType = ValidationResultType.Error;
                result.Message = "Graphic must be attached.";
            }
        }
    }
}

