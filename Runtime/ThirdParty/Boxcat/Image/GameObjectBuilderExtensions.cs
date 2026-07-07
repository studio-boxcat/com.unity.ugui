#nullable enable
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI
{
    // ugui-typed conveniences for GameObjectBuilder (Foundation) — here so Foundation stays ugui-agnostic
    public static class GameObjectBuilderExtensions
    {
        // gb.New parents before the Graphic is added — null parent canvas otherwise
        public static GameObjectBuilder NewUIIcon(this GameObjectBuilder gb, Sprite sprite, string? name = null)
        {
            var child = gb.New(name ?? sprite.name, typeof(RectTransform));
            child.Component_UIIcon(sprite);
            return child;
        }

        public static GameObjectBuilder NewUITextureImage(this GameObjectBuilder gb, Texture2D tex, string? name = null)
        {
            var child = gb.New(name ?? tex.name, typeof(RectTransform));
            var g = child.Component<UITextureImage>();
            g.rectTransform.sizeDelta = tex.Size();
            g.Texture = tex;
            return child;
        }

        public static UIIcon Component_UIIcon(this GameObjectBuilder gb, Sprite sprite, UIMeshMode method = UIMeshMode.ID)
        {
            var g = gb.Component<UIIcon>();

#if UNITY_EDITOR
            var editing = Editing.Yes(g.gameObject);
            if (editing)
            {
                Undo.RecordObject(g, $"Set Sprite of {g.name}");
                Undo.RecordObject(g.transform, $"Set Sprite of {g.name}"); // potential RectTransform change
            }
#endif

            g.Method = method;

#if UNITY_EDITOR
            if (editing)
            {
                g.SetSpriteAndMatchDimension(sprite);
                return g;
            }
#endif

            g.Sprite = sprite;
            return g;
        }

        public static UISlice Component_UISlice(this GameObjectBuilder gb, Sprite sprite, UISliceMethod method)
        {
            var g = gb.Component<UISlice>();

#if UNITY_EDITOR
            if (Editing.Yes(g.gameObject))
                Undo.RecordObject(g, $"Set Sprite of {g.name}");
#endif

            g.Sprite = sprite;
            g.Method = method;
            return g;
        }
    }
}
