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

            // Layer must be UI or AUI
            var layer = (LayerIndex) value.gameObject.layer;
            if (layer is LayerIndex.UI or LayerIndex.AUI)
                result.AddError("CanvasRenderer must be on a GameObject with layer 'UI' or 'AUI'.");

            // Graphic must be attached.
            if (value.TryGetComponent<Graphic>(out _) == false)
                result.AddError("CanvasRenderer must be attached to a GameObject with a Graphic component.");
        }
    }
}