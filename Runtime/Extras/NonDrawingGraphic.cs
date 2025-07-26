using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    /// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
    /// Useful for providing a raycast target without actually drawing anything.
    [RequireComponent(typeof(CanvasRenderer))]
    [GraphicPropertyHide(GraphicPropertyFlag.Color | GraphicPropertyFlag.Material)]
    public class NonDrawingGraphic : Graphic
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
        protected override void UpdateGeometry() => canvasRenderer.Clear();

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var (w, h) = rectTransform.rect.size;
            if (w.Approximately(0) || h.Approximately(0))
                result.AddError("NonDrawingGraphic must have a non-zero width and height.");
        }
#endif
    }
}