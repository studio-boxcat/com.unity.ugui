#nullable enable
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    public interface IUIImage
    {
        public Sprite Sprite { get; set; }
    }

    public abstract class UIImageBase : Graphic, IUIImage
    {
        [SerializeField, Required]
        [FormerlySerializedAs("m_Sprite")] // for legacy Image component
        [FormerlySerializedAs("_white")] // for legacy UIPolygon compat.
        [OnValueChanged("Editor_OnSpriteChanged", InvokeOnUndoRedo = false)]
        protected Sprite? _sprite;

        public Sprite? Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite.RefEq(value))
                    return;

                if (!value)
                {
                    _sprite = null;
                    SetVerticesDirty();
                    SetMaterialDirty();
                    return;
                }

                var oldSprite = _sprite;
                _sprite = value;
                SetVerticesDirty();

                // set material dirty only if the texture has changed
                // MaterialPropertyBlock will be set by CanvasRenderer
                if (!oldSprite || _sprite.texture.RefNq(oldSprite.texture))
                    SetMaterialDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                var sprite = ResolveSpriteToRender();
                return sprite ? sprite.texture : whiteTexture;
            }
        }

        protected Sprite? ResolveSpriteToRender()
        {
            var sprite = _sprite;
            this.OverlaySpriteToRender(ref sprite);
            return sprite;
        }

        protected override void OnPopulateMesh(Color color, MeshBuilder mb)
        {
            var sprite = ResolveSpriteToRender();
            if (sprite) OnPopulateMesh(sprite, color, mb);
        }

        protected abstract void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb);

        protected override void OnDidApplyAnimationProperties()
        {
            SetVerticesDirty();
            SetMaterialDirty();
            SetRaycastDirty();
        }

#if UNITY_EDITOR
        protected virtual void Reset() => _sprite = WhiteSpriteFinder.Find(gameObject);

        protected virtual void Editor_OnSpriteChanged()
        {
            SetVisualDirty();
        }
#endif
    }
}
