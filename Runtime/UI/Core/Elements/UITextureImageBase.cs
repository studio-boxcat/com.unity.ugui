#nullable enable
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    // Texture-backed sibling of UIImageBase: a Graphic whose source is a raw Texture (not a Sprite).
    // Shares the editor-overlay preview path (see UGUIExtensions.OverlayTextureToRender) so a
    // runtime-loaded texture previews in-editor without baking into the prefab.
    public abstract class UITextureImageBase : Graphic
    {
        [SerializeField, Required]
        [FormerlySerializedAs("m_Texture")] // for legacy RawImage component
        [FormerlySerializedAs("m_Tex")] // for even-older RawImage data
        [OnValueChanged("Editor_OnTextureChanged", InvokeOnUndoRedo = false)]
        private Texture? _texture;

        public Texture? Texture
        {
            get => _texture;
            set
            {
                if (_texture.RefEq(value))
                    return;

                _texture = value;
                // A raw texture is the material's texture, so a swap always dirties the material; verts too,
                // since UV mapping can depend on the texture size (e.g. UITexturePattern).
                SetVerticesDirty();
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                var texture = ResolveTextureToRender();
                return texture ? texture : whiteTexture;
            }
        }

        private Texture? ResolveTextureToRender()
        {
            var texture = _texture;
            this.OverlayTextureToRender(ref texture);
            return texture;
        }

        protected override void OnPopulateMesh(Color color, MeshBuilder mb)
        {
            var texture = ResolveTextureToRender();
            if (texture) OnPopulateMesh(texture, rectTransform.rect, color, mb);
        }

        protected abstract void OnPopulateMesh(Texture texture, Rect rect, Color color, MeshBuilder mb);

        protected override void OnDidApplyAnimationProperties()
        {
            SetVerticesDirty();
            SetMaterialDirty();
            SetRaycastDirty();
        }

        [ContextMenu("Set Native Size _n")]
        public void SetNativeSize()
        {
            Assert.IsTrue(_texture, "Texture is not set. Cannot set native size.");
            Assert.IsTrue(rectTransform.IsSingularAnchor(), "Cannot set native size when anchors are not singular.");
            rectTransform.sizeDelta = _texture!.Size();
        }

#if UNITY_EDITOR
        private void Editor_OnTextureChanged() => SetVisualDirty();
#endif
    }
}
