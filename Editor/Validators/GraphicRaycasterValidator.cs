using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(UnityEngine.UI.GraphicRaycasterValidator))]

namespace UnityEngine.UI
{
    public class GraphicRaycasterValidator : RootObjectValidator<GraphicRaycaster>
    {
        protected override void Validate(Sirenix.OdinInspector.Editor.Validation.ValidationResult result)
        {
            var obj = Object;
            if (!obj) return;

            // CanvasRenderer must be attached.
            if (obj.NoComponent<CanvasRenderer>())
                result.AddError("CanvasRenderer must be attached.");

            // Canvas must be attached.
            if (obj.NoComponent<Canvas>())
                result.AddError("Canvas must be attached.");
        }
    }
}