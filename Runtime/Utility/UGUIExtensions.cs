using System;

namespace UnityEngine.UI
{
    public static class UGUIExtensions
    {
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

        public static void SetRGB(this Graphic g, Color color) => g.color = color.WithA(g.color.a);
        public static float GetAlpha(this Graphic g) => g.color.a;
        public static void SetAlpha(this Graphic g, float alpha) => g.color = g.color.WithA(alpha);

        public static void SetColor(this Graphic[] graphics, Color color)
        {
            foreach (var g in graphics)
                g.color = color;
        }

        public static void SetVisualDirty(this Graphic g)
        {
            g.SetVerticesDirty();
            g.SetMaterialDirty();
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
