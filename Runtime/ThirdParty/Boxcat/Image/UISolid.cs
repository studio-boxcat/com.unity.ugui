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
    }
}
