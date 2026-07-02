using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public class RawImage : UITextureImageBase
    {
        [SerializeField, PropertyOrder(600)] Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);

        protected override void OnPopulateMesh(Texture texture, Rect rect, Color color, MeshBuilder mb)
        {
            var uvScale = new Vector2(texture.width * texture.texelSize.x, texture.height * texture.texelSize.y);
            var uv1 = m_UVRect.min * uvScale;
            var uv2 = m_UVRect.max * uvScale;
            mb.SetUp_Quad(rect.min, rect.max, uv1, uv2, color);
        }
    }
}
