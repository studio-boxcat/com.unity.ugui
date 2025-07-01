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
            if (layer is not LayerIndex.UI and not LayerIndex.AUI and not LayerIndex.SceneTransition)
                result.AddError("CanvasRenderer must be on a GameObject with layer 'UI' or 'AUI'.");

            // Graphic or GraphicRaycaster must be present.
            // GraphicRaycaster uses CanvasRenderer internally for determining depth.
            if (value.HasComponent<Graphic>() is false
                && value.HasComponent<GraphicRaycaster>() is false)
            {
                result.AddError("CanvasRenderer must be attached to a GameObject with a Graphic component.")
                    .WithFix("Remove CanvasRenderer", () => Object.DestroyImmediate(value));
            }
        }
    }
}