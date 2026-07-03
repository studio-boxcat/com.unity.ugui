using System;

namespace UnityEngine.UI
{
    public static class UGUIExtensions
    {
        public static void SetMaterialSingle(this CanvasRenderer cr, Material material)
        {
            cr.materialCount = 1;
            cr.SetMaterial(material, 0);
        }

        public static void SetMaterialSingle(this CanvasRenderer cr, Material material, Texture texture)
        {
            cr.materialCount = 1;
            cr.SetMaterial(material, 0);
            cr.SetTexture(texture);
        }

        public static void SetPopMaterialSingle(this CanvasRenderer cr, Material material)
        {
            cr.hasPopInstruction = true;
            cr.popMaterialCount = 1;
            cr.SetPopMaterial(material, 0);
        }

        // Prefer the sibling Graphic's cached canvas (cheap); fall back to a hierarchy search when the
        // renderer has no Graphic. Resolved the same way as Graphic.canvas / Clipper.GetCanvas().
        public static Canvas ResolveCanvas(this CanvasRenderer cr)
        {
            if (cr.TryGetComponent<Graphic>(out var g)) return g.canvas;
            var canvas = ComponentSearch.NearestUpwards_GOAnyAndCompEnabled<Canvas>(cr);
            Assertions.Assert.IsTrue(canvas, "ResolveCanvas: CanvasRenderer has no Canvas ancestor.");
            return canvas!;
        }

        public static void SetClipRect(this CanvasRenderer cr, Rect clipRect, bool validRect)
        {
            if (validRect)
                cr.EnableRectClipping(clipRect);
            else
                cr.DisableRectClipping();
        }

        public static void UpdateCullAgainstClipRect(this CanvasRenderer cr, Rect clipRect, bool validRect, Matrix4x4 wtc)
        {
            if (validRect is false)
            {
                cr.UpdateCull(true); // true = don't draw
                return;
            }

            // Clip target must have a RectTransform (Graphic enforces one; graphic-less targets must too).
            var graphicRect = CanvasUtils.BoundingRect((RectTransform)cr.transform, wtc);
            cr.UpdateCull(!clipRect.Overlaps(graphicRect));
        }

        // Reset a renderer to its unclipped, unculled state (used when no clipper owns it anymore).
        public static void DisableCullAndClipRect(this CanvasRenderer cr)
        {
            cr.SetClipRect(new Rect(), validRect: false);
            cr.UpdateCull(cull: false);
        }

        private static void UpdateCull(this CanvasRenderer cr, bool cull)
        {
            if (cr.cull == cull) return;

            cr.cull = cull;
            // Let the Graphic react to the cull change (e.g. re-queue a rebuild skipped while culled).
            if (!cull && cr.TryGetComponent<Graphic>(out var g))
                g.OnUncull();
        }

        public static void SetRGB(this Graphic g, Color color) => g.color = color.WithA(g.color.a);
        public static float GetAlpha(this Graphic g) => g.color.a;
        public static void SetAlpha(this Graphic g, float alpha) => g.color = g.color.WithA(alpha);

        public static void SetColor(this Graphic[] graphics, Color color)
        {
            foreach (var g in graphics)
                g.color = color;
        }

        private static readonly Type[] _graphicTypes = { typeof(RectTransform), typeof(CanvasRenderer) };

        public static GameObject NewGraphicChildBase(this Transform t, string name = "")
        {
            var childGO = new GameObject(name, _graphicTypes) { layer = t.gameObject.layer };
            var child = childGO.transform;
            child.SetParent(t, false);
            return childGO;
        }

        public static TGraphic NewGraphicChild<TGraphic>(this Transform t, string name = "") where TGraphic : Graphic
        {
            var childGO = t.NewGraphicChildBase(name);
            return childGO.AddComponent<TGraphic>(); // add component after SetParent() to avoid null parent canvas.
        }

        public static float GetScaledWidth(this Canvas canvas)
            => canvas.pixelRect.width / canvas.scaleFactor;

        /// The (0..1) normalized 2D position the anchor represents.
        /// LowerLeft → (0,0); UpperRight → (1,1); MiddleCenter → (0.5,0.5). Pivot-shaped.
        public static Vector2 Norm(this TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => new Vector2(0f, 1f),
            TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
            TextAnchor.UpperRight => new Vector2(1f, 1f),
            TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
            TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
            TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
            TextAnchor.LowerLeft => new Vector2(0f, 0f),
            TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
            TextAnchor.LowerRight => new Vector2(1f, 0f),
            _ => new Vector2(0.5f, 0.5f),
        };
    }
}
