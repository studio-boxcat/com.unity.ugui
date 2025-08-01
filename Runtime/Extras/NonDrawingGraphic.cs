namespace UnityEngine.UI
{
    /// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
    /// Useful for providing a raycast target without actually drawing anything.
    [GraphicPropertyHide(GraphicPropertyFlag.Color | GraphicPropertyFlag.Material)]
    public class NonDrawingGraphic : Graphic
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
        protected override void UpdateGeometry() => canvasRenderer.Clear();
    }
}