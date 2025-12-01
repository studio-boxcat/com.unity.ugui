using System;
using UnityEngine.Events;

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

        public static float GetScaledWidth(this Canvas canvas)
            => canvas.pixelRect.width / canvas.scaleFactor;

        public static void AddOnClick(this Button button, UnityAction action)
        {
            button.onClick ??= new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }
    }
}