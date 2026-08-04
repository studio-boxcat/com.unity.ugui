using System;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Image is a textured element in the UI hierarchy.
    /// </summary>

    public class Image : UIImageBase
    {
        public enum Type
        {
            Simple = 0,
            Filled = 3
        }

        public enum FillMethod
        {
            Horizontal = 0,
            Vertical = 1,
            Radial360 = 4,
        }

        public enum OriginHorizontal
        {
            Left,
            Right,
        }


        [SerializeField] private Type m_Type = Type.Simple;
        public Type type { set { if (SetPropertyUtility.SetEnum(ref m_Type, value)) SetVerticesDirty(); } }

        [SerializeField] private bool m_PreserveAspect = false;

        /// Filling method for filled sprites.
        [SerializeField] private FillMethod m_FillMethod = FillMethod.Radial360;
        public FillMethod fillMethod { set { if (SetPropertyUtility.SetEnum(ref m_FillMethod, value)) { SetVerticesDirty(); m_FillOrigin = 0; } } }

        /// Amount of the Image shown. 0-1 range with 0 being nothing shown, and 1 being the full Image.
        [Range(0, 1)]
        [SerializeField]
        private float m_FillAmount = 1.0f;
        public float fillAmount { get { return m_FillAmount; } set { if (SetPropertyUtility.SetValue(ref m_FillAmount, Mathf.Clamp01(value))) SetVerticesDirty(); } }

        [SerializeField] private bool m_FillClockwise = true;

        [SerializeField] private int m_FillOrigin;
        public int fillOrigin { set { if (SetPropertyUtility.SetValue(ref m_FillOrigin, value)) SetVerticesDirty(); } }

        private static void PreserveSpriteAspectRatio(ref Rect rect, Vector2 pivot, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * pivot.x;
            }
        }

        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        private static Vector4 GetDrawingDimensions(Sprite sprite, Rect r, Vector2 pivot, bool shouldPreserveAspect)
        {
            var padding = Sprites.DataUtility.GetPadding(sprite);
            var size = sprite.rect.size;

            // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

            int spriteW = Mathf.RoundToInt(size.x);
            int spriteH = Mathf.RoundToInt(size.y);

            var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

            if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
            {
                PreserveSpriteAspectRatio(ref r, pivot, size);
            }

            v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
            );

            return v;
        }

        /// <summary>
        /// Update the UI renderer mesh.
        /// </summary>
        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            var rect = rectTransform.rect;
            var pivot = rectTransform.pivot;
            switch (m_Type)
            {
                case Type.Simple:
                {
                    GenerateSprite(sprite, color, mb, rect, pivot, m_PreserveAspect);
                    break;
                }
                case Type.Filled:
                {
                    GenerateFilledSprite(sprite, color, mb, rect, pivot, m_PreserveAspect,
                        m_FillAmount, m_FillMethod, m_FillOrigin, m_FillClockwise);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void GenerateSprite(Sprite sprite, Color color, MeshBuilder toFill, Rect r, Vector2 rectPivot, bool lPreserveAspect)
        {
            var spriteSize = sprite.rect.size;

            // Covert sprite pivot into normalized space.
            var spritePivot = sprite.pivot / spriteSize;

            if (lPreserveAspect & spriteSize.sqrMagnitude > 0.0f)
                PreserveSpriteAspectRatio(ref r, rectPivot, spriteSize);

            var drawingSize = new Vector2(r.width, r.height);
            var spriteBoundSize = (Vector2) sprite.bounds.size;

            // Calculate the drawing offset based on the difference between the two pivots.
            var drawOffset = (rectPivot - spritePivot) * drawingSize;

            var srcPoses = sprite.vertices;
            var vertCount = srcPoses.Length;
            var poses = toFill.Poses.SetUp(vertCount);
            var vertexMult =  drawingSize / spriteBoundSize;
            for (var i = 0; i < vertCount; ++i)
                poses[i] = srcPoses[i] * vertexMult - drawOffset;

            toFill.UVs.SetUp(sprite.uv);
            toFill.Colors.SetUp(color, vertCount);
            toFill.Indices.SetUp(sprite.triangles);
        }

        // Scratch quads, in QuadMesh corner order.
        static readonly Vector2[] s_Xy = new Vector2[4];
        static readonly Vector2[] s_Uv = new Vector2[4];

        // The sweep order. Each step fills the quadrant anchored at that corner, and the anchor's bits
        // give both the quadrant's extent and the pivot to cut about.
        static readonly int[] s_SweepAnchors = { QuadMesh.BL, QuadMesh.TL, QuadMesh.TR, QuadMesh.BR };

        // Sub-rect [fMin,fMax] of the drawing rect `v` and UV rect `outer` (both x=left y=bottom z=right w=top).
        private static void SetScratchQuad(Vector4 v, Vector4 outer, Vector2 fMin, Vector2 fMax)
        {
            SetCorners(s_Xy, Mathf.Lerp(v.x, v.z, fMin.x), Mathf.Lerp(v.x, v.z, fMax.x),
                Mathf.Lerp(v.y, v.w, fMin.y), Mathf.Lerp(v.y, v.w, fMax.y));
            SetCorners(s_Uv, Mathf.Lerp(outer.x, outer.z, fMin.x), Mathf.Lerp(outer.x, outer.z, fMax.x),
                Mathf.Lerp(outer.y, outer.w, fMin.y), Mathf.Lerp(outer.y, outer.w, fMax.y));
            return;

            static void SetCorners(Vector2[] quad, float x0, float x1, float y0, float y1)
            {
                quad[QuadMesh.BL] = new Vector2(x0, y0);
                quad[QuadMesh.BR] = new Vector2(x1, y0);
                quad[QuadMesh.TL] = new Vector2(x0, y1);
                quad[QuadMesh.TR] = new Vector2(x1, y1);
            }
        }

        // Shrinks one axis to `fill` of its length, growing from the near edge or, with `fromHi`, the far
        // one. UVs track the rect so the sprite clips rather than squashes.
        private static void CollapseAxis(ref float lo, ref float hi, ref float uLo, ref float uHi, float fill, bool fromHi)
        {
            if (fromHi)
            {
                lo = Mathf.Lerp(hi, lo, fill);
                uLo = Mathf.Lerp(uHi, uLo, fill);
            }
            else
            {
                hi = Mathf.Lerp(lo, hi, fill);
                uHi = Mathf.Lerp(uLo, uHi, fill);
            }
        }

        /// <summary>
        /// Generate vertices for a filled Image.
        /// </summary>
        private static void GenerateFilledSprite(Sprite sprite, Color color, MeshBuilder toFill, Rect rect, Vector2 pivot, bool preserveAspect,
            float fillAmount, FillMethod fillMethod, int fillOrigin, bool fillClockwise)
        {
            if (fillAmount < 0.001f)
                return;

            var v = GetDrawingDimensions(sprite, rect, pivot, preserveAspect);
            var outer = Sprites.DataUtility.GetOuterUV(sprite);

            // A single quad covers everything but a partial radial fill. Horizontal and vertical just end
            // the Image prematurely; a full fill needs no cut at all.
            if (fillMethod is FillMethod.Horizontal or FillMethod.Vertical || fillAmount >= 1f)
            {
                if (fillMethod == FillMethod.Horizontal)
                    CollapseAxis(ref v.x, ref v.z, ref outer.x, ref outer.z, fillAmount, fillOrigin == 1);
                else if (fillMethod == FillMethod.Vertical)
                    CollapseAxis(ref v.y, ref v.w, ref outer.y, ref outer.w, fillAmount, fillOrigin == 1);

                var (pos1, pos2) = v.Split_XY_ZW();
                var (uv1, uv2) = outer.Split_XY_ZW();
                toFill.SetUp_Quad(pos1, pos2, uv1, uv2, color);
                return;
            }

            Assert.IsTrue(fillMethod == FillMethod.Radial360, "Only Radial360 fill method is supported for partial fill");

            // RadialCut drops the quadrants the sweep hasn't reached, so pack as we go.
            const int maxQuads = 4;
            var poses = toFill.Poses.SetUp(maxQuads * 4);
            var uvs = toFill.UVs.SetUp(maxQuads * 4);
            var quadCount = 0;

            for (var cycle = 0; cycle < maxQuads; ++cycle)
            {
                var anchor = s_SweepAnchors[cycle];
                var fMin = new Vector2(
                    (anchor & QuadMesh.FlipX) != 0 ? 0.5f : 0f,
                    (anchor & QuadMesh.FlipY) != 0 ? 0.5f : 0f);
                SetScratchQuad(v, outer, fMin, fMin + new Vector2(0.5f, 0.5f));

                // fillOrigin rotates which quadrant fills first.
                var step = (cycle + fillOrigin) % 4;
                if (!fillClockwise) step = 3 - step;

                // Alternating quadrants sweep the opposite way; the cut pivots on the rect centre.
                var invert = fillClockwise ^ ((cycle & 1) == 1);
                if (!RadialCut(s_Xy, s_Uv, Mathf.Clamp01(fillAmount * 4f - step), invert, anchor ^ QuadMesh.Opposite))
                    continue;

                var b = quadCount++ * 4;
                for (var i = 0; i < 4; i++)
                {
                    poses[b + i] = s_Xy[i];
                    uvs[b + i] = s_Uv[i];
                }
            }

            var vertCount = quadCount * 4;
            toFill.Poses.TrimAfter(vertCount);
            toFill.UVs.TrimAfter(vertCount);
            toFill.Colors.SetUp(color, vertCount);
            toFill.Indices.SetUp_Quad(quadCount);
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// </summary>
        private static bool RadialCut(Vector2[] xy, Vector2[] uv, float fill, bool invert, int pivot)
        {
            // Nothing to fill
            if (fill < 0.001f) return false;

            // Nothing to adjust
            if (!invert && fill > 0.999f) return true;

            // Convert 0-1 value into 0 to 90 degrees angle in radians
            float angle = Mathf.Clamp01(fill);
            if (invert) angle = 1f - angle;
            angle *= Mathf.PI / 2;

            // Calculate the effective X and Y factors
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            RadialCut(xy, cos, sin, invert, pivot);
            RadialCut(uv, cos, sin, invert, pivot);
            return true;
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// </summary>
        private static void RadialCut(Vector2[] xy, float cos, float sin, bool invert, int pivot)
        {
            // The cut spans the pivot to its diagonal, moving the two neighbours in between.
            var i0 = pivot;
            var i1 = pivot ^ QuadMesh.FlipX;
            var i2 = pivot ^ QuadMesh.Opposite;
            var i3 = pivot ^ QuadMesh.FlipY;

            if (sin > cos)
            {
                cos /= sin;
                sin = 1f;

                if (invert)
                {
                    xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                    xy[i2].x = xy[i1].x;
                }
            }
            else if (cos > sin)
            {
                sin /= cos;
                cos = 1f;

                if (!invert)
                {
                    xy[i2].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                    xy[i3].y = xy[i2].y;
                }
            }
            else
            {
                cos = 1f;
                sin = 1f;
            }

            if (!invert) xy[i3].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
            else xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
    }
}
