#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public enum ShadowStyle : byte
    {
        Shadow = 1,
        Outline4 = 2,
        Outline8 = 3,
    }

    // Shadow copies of the graphic's mesh: the original verts stay in block 0, shadow copies fill vert
    // blocks 1..copies with the original UVs (the graphic's own alpha shapes them). Layering comes from
    // index order alone (shadow blocks draw first, the original block last, on top).
    public class Shadow : BaseMeshEffect
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField] private ShadowStyle m_Style = ShadowStyle.Shadow;
        [SerializeField] private Vector2 m_EffectDistance = new(1f, -1f);
        [SerializeField] private Color m_EffectColor = new(0f, 0f, 0f, 0.5f);

        /// <summary>
        /// Color for the effect
        /// </summary>
        public Color effectColor
        {
            get => m_EffectColor;
            set
            {
                m_EffectColor = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public override unsafe void ModifyMesh(MeshBuilder mb)
        {
            var copies = m_Style switch
            {
                ShadowStyle.Shadow => 1,
                ShadowStyle.Outline4 => 4,
                ShadowStyle.Outline8 => 8,
                _ => throw new ArgumentOutOfRangeException(),
            };
            var n = mb.Poses.Count;
            var newVertCount = n * (copies + 1);

            // UV: every shadow block repeats the original UVs.
            mb.UVs.Resize_Repeat(copies + 1);

            // Colors: one flat fill covers all shadow blocks.
            var colors = mb.Colors.Resize(newVertCount);
            Array.Fill(colors, m_EffectColor, n, n * copies);

            // Index: shadow blocks draw first (vert blocks 1..copies), the original block last, on top.
            mb.Indices.Resize_CopiesBehind(copies, vertStride: n);

            // Pos: pointer taken last — it must not be held across the (possibly allocating) resizes above.
            var pf = mb.Poses.ResizeUnsafe(newVertCount).Ptr;
            var dx = m_EffectDistance.x;
            var dy = m_EffectDistance.y;
            Translate(pf, n, 0, dx, dy);
            if (m_Style != ShadowStyle.Shadow)
            {
                Translate(pf, n, 1, dx, -dy);
                Translate(pf, n, 2, -dx, dy);
                Translate(pf, n, 3, -dx, -dy);
            }
            if (m_Style == ShadowStyle.Outline8)
            {
                Translate(pf, n, 4, dx, 0);
                Translate(pf, n, 5, 0, dy);
                Translate(pf, n, 6, -dx, 0);
                Translate(pf, n, 7, 0, -dy);
            }
        }

        // Fills shadow vert block `shadowIndex + 1` with the originals (block 0) offset by (x, y).
        private static unsafe void Translate(float* pf, int n, int shadowIndex, float x, float y)
        {
            var src = pf;
            var dst = pf + (shadowIndex + 1) * n * 3;
            for (var i = 0; i < n; ++i, src += 3, dst += 3)
            {
                dst[0] = src[0] + x;
                dst[1] = src[1] + y;
            }
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = graphic;
            if (!g)
            {
                result.AddError("Shadow requires a Graphic on the same GameObject.");
                return;
            }

            // Text glyphs carry their own per-pixel alpha, and the Solid material shapes the shadow
            // from mesh geometry rather than the sprite — both work with any (or no) sprite.
            if (g is UITextBase) return;
            if (g.material == GraphicMaterialKind.Solid) return;

            // Otherwise the shadow copies rely on the sprite's alpha silhouette to shape themselves.
            if (g is not UIImageBase img)
            {
                result.AddError($"Shadow on {g.GetType().Name} is unsupported: needs a UIImageBase (or a UITextBase / Solid material).");
                return;
            }

            var sprite = img.Sprite;
            if (sprite && !WhiteSpriteFinder.IsWhiteSprite(sprite))
                result.AddError($"Shadow needs a white sprite for non-Solid materials, but the current sprite is '{sprite.name}'.");
        }
#endif
    }
}
