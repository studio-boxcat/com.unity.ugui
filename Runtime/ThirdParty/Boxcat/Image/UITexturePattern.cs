#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    public class UITexturePattern : UITextureImageBase
    {
        [SerializeField, LabelText("UV Offset"), PropertyOrder(GraphicPropOrder.Appendix)]
        [OnValueChanged(nameof(SetVerticesDirty))]
        private Vector2 _uvOffset;
        [SerializeField, LabelText("UV Scale"), PropertyOrder(GraphicPropOrder.Appendix)]
        [OnValueChanged(nameof(SetVerticesDirty))]
        private Vector2 _uvScale = new(1, 1);

        public Vector2 UVOffset
        {
            get => _uvOffset;
            set
            {
                if (_uvOffset.Equals(value)) return;
                _uvOffset = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(Texture texture, Rect rect, Color color, MeshBuilder mb)
        {
            var pos1 = rect.min;
            var pos2 = rect.max;
            var repeat = rect.size / (texture.Size() * _uvScale);
            mb.SetUp_Quad(pos1, pos2, uv1: _uvOffset, uv2: _uvOffset + repeat, color);
        }

#if UNITY_EDITOR
        private void Awake()
        {
            // one time initialization
            if (!Texture) Texture = AssetDatabaseUtils.LoadTextureWithGUID("c0f3ee1ca86c845b8bc0ead1a3b60649")!; // dotted line
        }

        [ContextMenu("Match Size With Texture _m")]
        private void MatchSizeWithTexture()
        {
            var tex = Texture;
            if (!tex) return;
            if (!rectTransform.IsSingularAnchor()) return; // to set size
            rectTransform.sizeDelta = new Vector2(tex.width * _uvScale.x, tex.height * _uvScale.y);
        }

        public override void Validate(SelfValidationResult result)
        {
            base.Validate(result);

            var tex = Texture;
            if (!tex) return;

            if (tex.wrapModeU is not (TextureWrapMode.Repeat or TextureWrapMode.Mirror)
                || tex.wrapModeV is not (TextureWrapMode.Repeat or TextureWrapMode.Mirror))
                result.AddError("Texture must use Repeat wrap mode.");
        }
#endif
    }
}
