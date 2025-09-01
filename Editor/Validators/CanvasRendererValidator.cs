using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(UnityEngine.UI.CanvasRendererValidator))]

namespace UnityEngine.UI
{
    public class CanvasRendererValidator : RootObjectValidator<CanvasRenderer>
    {
        protected override void Validate(Sirenix.OdinInspector.Editor.Validation.ValidationResult result)
        {
            var obj = Object;
            if (!obj) return;

            // Layer must be UI or AUI
            var layer = (LayerIndex) obj.gameObject.layer;
            if (layer is not LayerIndex.UI and not LayerIndex.AUI and not LayerIndex.SceneTransition)
                result.AddError("CanvasRenderer must be on a GameObject with layer 'UI' or 'AUI'.");

            // Graphic or GraphicRaycaster must be present.
            // GraphicRaycaster uses CanvasRenderer internally for determining depth.
            if (obj.NoComponent<Graphic>()
                && obj.NoComponent<GraphicRaycaster>())
            {
                result.AddError("CanvasRenderer must be attached to a GameObject with a Graphic component.")
                    .WithFix("Remove CanvasRenderer", () => UnityEngine.Object.DestroyImmediate(obj));
            }
        }
    }
}