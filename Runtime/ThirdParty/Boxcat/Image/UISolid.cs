#nullable enable
using UnityEngine;

namespace UnityEngine.UI
{
    [Icon("Packages/com.unity.ugui/Runtime/ThirdParty/Boxcat/Image/UISolid.png")]
    public class UISolid : UIImageBase
    {
        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            var r = rectTransform.rect;
            var uv = SolidUVCache.Get(sprite);
            mb.SetUp_Quad(r.min, r.max, uv, uv, color);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Sprite = CommonAssets.WhiteSprite;
        }

        // Flattens a slice / rounded rect to a plain quad — the source sprite's borders and radius are
        // dropped, so only use it where the shape was already square.
        [UnityEditor.MenuItem("CONTEXT/UISlice/Convert to Solid")]
        private static void ConvertToSolid(UnityEditor.MenuCommand cmd)
        {
            var src = (UIImageBase)cmd.context;

            var mat = src.material;
            var color = src.color;
            var raycastTarget = src.raycastTarget;
            var raycastInset = src.raycastInset;

            var comp = EditorUtils.ReplaceComponentInSlot<UISolid>(src);
            comp.Sprite = CommonAssets.WhiteSprite;
            comp.material = mat;
            comp.color = color;
            comp.raycastTarget = raycastTarget;
            comp.raycastInset = raycastInset;
        }
#endif
    }
}
