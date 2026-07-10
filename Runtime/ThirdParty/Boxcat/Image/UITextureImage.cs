#nullable enable

namespace UnityEngine.UI
{
    public class UITextureImage : UITextureImageBase
    {
        protected override void OnPopulateMesh(Texture texture, Rect rect, Color color, MeshBuilder mb) =>
            mb.SetUp_Quad_FullUV(rect, color);
    }
}
