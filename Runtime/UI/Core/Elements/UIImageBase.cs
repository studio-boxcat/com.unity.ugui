#nullable enable
using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    public interface IUIImage
    {
        public Sprite Sprite { get; set; }
    }

    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class UIImageSpriteConstraintAttribute : Attribute
    {
        public bool Quad;
        public Side FullBorders;
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

        public override void Validate(SelfValidationResult result)
        {
            base.Validate(result);

            // Enforce the concrete component's declared sprite constraints (e.g. UIRoundedRect needs a
            // full-border quarter quad). Null sprite is already covered by [Required].
            var constraint = GetType().GetCustomAttribute<UIImageSpriteConstraintAttribute>();
            if (constraint is null) return;
            var sprite = _sprite;
            if (!sprite) return;

            if (constraint.Quad && !sprite.IsQuad())
                result.AddError($"Sprite '{sprite.name}' must be a quad (4-vertex axis-aligned rect).");

            var border = sprite.border;
            var size = sprite.rect.size;
            constraint.FullBorders.ForEach(side =>
            {
                if (!border.Get(side).Equals(side.IsLR() ? size.x : size.y))
                    result.AddError($"Sprite '{sprite.name}' {side} border must span the full width or height.");
            });
        }
#endif
    }
}
