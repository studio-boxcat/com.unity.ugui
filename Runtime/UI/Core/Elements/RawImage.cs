using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Displays a Texture2D for the UI System.
    /// </summary>
    /// <remarks>
    /// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
    /// Keep in mind though that this will create an extra draw call with each RawImage present, so it's
    /// best to use it only for backgrounds or temporary visible graphics.
    /// </remarks>
    [AddComponentMenu("UI/Raw Image", 12)]
    public class RawImage : MaskableGraphic
    {
        [FormerlySerializedAs("m_Tex")]
        [SerializeField, Required] Texture m_Texture;
        [SerializeField, PropertyOrder(600)] Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);

        public override Texture mainTexture => m_Texture;

        public Texture texture
        {
            get => m_Texture;
            set
            {
                if (ReferenceEquals(m_Texture, value))
                    return;

                m_Texture = value;
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public Rect uvRect
        {
            get => m_UVRect;
            set
            {
                if (m_UVRect == value)
                    return;
                m_UVRect = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(MeshBuilder mb)
        {
            var tex = mainTexture;
            if (tex is null) return;

            var r = GetPixelAdjustedRect();
            var pos1 = r.min;
            var pos2 = r.max;
            var uvScale = new Vector2(tex.width * tex.texelSize.x, tex.height * tex.texelSize.y);
            var uv1 = m_UVRect.min * uvScale;
            var uv2 = m_UVRect.max * uvScale;
            mb.SetUp_Quad(pos1, pos2, uv1, uv2, color);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
    }
}