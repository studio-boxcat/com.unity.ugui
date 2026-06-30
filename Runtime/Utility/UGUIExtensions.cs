using System;
using System.Diagnostics;

namespace UnityEngine.UI
{
    public static class UGUIExtensions
    {
        static UGUIExtensions()
        {
            // Set(...) only stashes the overlay; the side-effect repaints the target so a preview shows immediately.
            Override.RegisterSideEffect(_colorPreviewKey,
                static (target, _) => ((Graphic)target).SetVisualDirty());
            Override.RegisterSideEffect(_spritePreviewKey,
                static (target, _) => ((Graphic)target).SetVisualDirty());
        }

        private const int _colorPreviewKey = 1060026621;

        // Apply the color for runtime use, or — while editing — overlay it as an editor-only preview so a
        // data-driven tint renders in-editor without baking into the prefab.
        public static void SetColorOrPreview(this Graphic graphic, Color color)
        {
#if UNITY_EDITOR
            if (Override.SetOrElse(graphic, _colorPreviewKey, color))
#endif
                graphic.color = color;
        }

        [Conditional("UNITY_EDITOR")]
        internal static void OverlayColorToRender(this Graphic graphic, ref Color color) =>
            Override.Overlay(graphic, _colorPreviewKey, ref color);


        private const int _spritePreviewKey = 1294963985;

        // Apply the sprite for runtime use, or — while editing — overlay it as an editor-only preview so a
        // bundle-loaded sprite renders in-editor without baking into the prefab.
        public static void SetSpriteOrPreview(this UIImageBase icon, Sprite sprite)
        {
#if UNITY_EDITOR
            if (Override.SetOrElse(icon, _spritePreviewKey, sprite))
#endif
                icon.Sprite = sprite;
        }

        public static void PreviewSprite(this UIImageBase icon, Sprite sprite)
        {
#if UNITY_EDITOR
            _ = Override.SetOrElse(icon, _spritePreviewKey, sprite);
#endif
        }

        [Conditional("UNITY_EDITOR")]
        internal static void OverlaySpriteToRender(this UIImageBase icon, ref Sprite? sprite) =>
            Override.Overlay(icon, _spritePreviewKey, ref sprite);


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
