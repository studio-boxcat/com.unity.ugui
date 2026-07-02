#nullable enable
using System.Diagnostics;

namespace UnityEngine.UI
{
    // Editor-only preview overlays for Graphics (see Override).
    // Set*OrPreview applies the value for real at runtime; while editing it stashes the value as an
    // editor-only overlay so a data/bundle-driven value previews in-editor without baking into the
    // prefab. In player builds every Override touch is #if'd out — only the plain assignment remains.
    public static class GraphicPreviewExtensions
    {
        private const uint _colorPreviewKey = 1060026621;
        private const uint _spritePreviewKey = 1294963985;
        private const uint _texturePreviewKey = 1748295013;

#if UNITY_EDITOR
        static GraphicPreviewExtensions()
        {
            // Side-effect repaints the target so a stashed preview shows immediately.
            foreach (var key in new[] { _colorPreviewKey, _spritePreviewKey, _texturePreviewKey })
                Override.RegisterSideEffect(key, static (t, _) => ((Graphic)t).SetVisualDirty());
        }
#endif

        public static void SetColorOrPreview(this Graphic graphic, Color color)
        {
#if UNITY_EDITOR
            // While editing, stash as an overlay and skip the real assignment (Override.Set returns true).
            if (Override.Set(graphic, _colorPreviewKey, color)) return;
#endif
            graphic.color = color;
        }

        public static void SetSpriteOrPreview(this UIImageBase icon, Sprite sprite)
        {
#if UNITY_EDITOR
            if (Override.Set(icon, _spritePreviewKey, sprite)) return;
#endif
            icon.Sprite = sprite;
        }

        public static void SetTextureOrPreview(this UITextureImageBase image, Texture texture)
        {
#if UNITY_EDITOR
            if (Override.Set(image, _texturePreviewKey, texture)) return;
#endif
            image.Texture = texture;
        }

        [Conditional("UNITY_EDITOR")]
        public static void PreviewSprite(this UIImageBase icon, Sprite sprite) =>
            Override.Preview(icon, _spritePreviewKey, sprite);

        [Conditional("UNITY_EDITOR")]
        public static void PreviewTexture(this UITextureImageBase image, Texture texture) =>
            Override.Preview(image, _texturePreviewKey, texture);

        [Conditional("UNITY_EDITOR")]
        internal static void OverlayColorToRender(this Graphic graphic, ref Color color) =>
            Override.Overlay(graphic, _colorPreviewKey, ref color);

        [Conditional("UNITY_EDITOR")]
        internal static void OverlaySpriteToRender(this UIImageBase icon, ref Sprite? sprite) =>
            Override.Overlay(icon, _spritePreviewKey, ref sprite);

        [Conditional("UNITY_EDITOR")]
        internal static void OverlayTextureToRender(this UITextureImageBase image, ref Texture? texture) =>
            Override.Overlay(image, _texturePreviewKey, ref texture);
    }
}
