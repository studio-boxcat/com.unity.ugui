using System;

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
            Horizontal,
            Vertical,
            Radial90,
            Radial180,
            Radial360,
        }

        public enum OriginHorizontal
        {
            Left,
            Right,
        }


        [SerializeField] private Type m_Type = Type.Simple;
        public Type type { get { return m_Type; } set { if (SetPropertyUtility.SetEnum(ref m_Type, value)) SetVerticesDirty(); } }

        [SerializeField] private bool m_PreserveAspect = false;

        [SerializeField] private bool m_FillCenter = true;

        /// Filling method for filled sprites.
        [SerializeField] private FillMethod m_FillMethod = FillMethod.Radial360;
        public FillMethod fillMethod { get { return m_FillMethod; } set { if (SetPropertyUtility.SetEnum(ref m_FillMethod, value)) { SetVerticesDirty(); m_FillOrigin = 0; } } }

        /// Amount of the Image shown. 0-1 range with 0 being nothing shown, and 1 being the full Image.
        [Range(0, 1)]
        [SerializeField]
        private float m_FillAmount = 1.0f;
        public float fillAmount { get { return m_FillAmount; } set { if (SetPropertyUtility.SetValue(ref m_FillAmount, Mathf.Clamp01(value))) SetVerticesDirty(); } }

        [SerializeField] private bool m_FillClockwise = true;

        [SerializeField] private int m_FillOrigin;
        public int fillOrigin { get { return m_FillOrigin; } set { if (SetPropertyUtility.SetValue(ref m_FillOrigin, value)) SetVerticesDirty(); } }

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
            switch (type)
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

        static readonly Vector2[] s_Xy = new Vector2[4];
        static readonly Vector2[] s_Uv = new Vector2[4];

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

            var tx0 = outer.x;
            var ty0 = outer.y;
            var tx1 = outer.z;
            var ty1 = outer.w;

            // Horizontal and vertical filled sprites are simple -- just end the Image prematurely
            if (fillMethod == FillMethod.Horizontal)
            {
                var fill = (tx1 - tx0) * fillAmount;

                if (fillOrigin == 1)
                {
                    v.x = v.z - (v.z - v.x) * fillAmount;
                    tx0 = tx1 - fill;
                }
                else
                {
                    v.z = v.x + (v.z - v.x) * fillAmount;
                    tx1 = tx0 + fill;
                }
            }
            else if (fillMethod == FillMethod.Vertical)
            {
                var fill = (ty1 - ty0) * fillAmount;

                if (fillOrigin == 1)
                {
                    v.y = v.w - (v.w - v.y) * fillAmount;
                    ty0 = ty1 - fill;
                }
                else
                {
                    v.w = v.y + (v.w - v.y) * fillAmount;
                    ty1 = ty0 + fill;
                }
            }

            if (fillAmount >= 1f || (fillMethod is FillMethod.Horizontal or FillMethod.Vertical))
            {
                toFill.SetUp_Quad(
                    new Vector2(v.x, v.y), new Vector2(v.z, v.w),
                    new Vector2(tx0, ty0), new Vector2(tx1, ty1),
                    color);
                return;
            }

            var qb = toFill.SetUp_Quad(4);

            if (fillMethod == FillMethod.Radial90)
            {
                s_Xy[0] = new Vector2(v.x, v.y);
                s_Xy[1] = new Vector2(v.x, v.w);
                s_Xy[2] = new Vector2(v.z, v.w);
                s_Xy[3] = new Vector2(v.z, v.y);

                s_Uv[0] = new Vector2(tx0, ty0);
                s_Uv[1] = new Vector2(tx0, ty1);
                s_Uv[2] = new Vector2(tx1, ty1);
                s_Uv[3] = new Vector2(tx1, ty0);

                if (RadialCut(s_Xy, s_Uv, fillAmount, fillClockwise, fillOrigin))
                    qb.Add_0312(s_Xy, s_Uv);
            }
            else if (fillMethod == FillMethod.Radial180)
            {
                for (var side = 0; side < 2; ++side)
                {
                    float fx0, fx1, fy0, fy1;
                    var even = fillOrigin > 1 ? 1 : 0;

                    if (fillOrigin is 0 or 2)
                    {
                        fy0 = 0f;
                        fy1 = 1f;
                        if (side == even)
                        {
                            fx0 = 0f;
                            fx1 = 0.5f;
                        }
                        else
                        {
                            fx0 = 0.5f;
                            fx1 = 1f;
                        }
                    }
                    else
                    {
                        fx0 = 0f;
                        fx1 = 1f;
                        if (side == even)
                        {
                            fy0 = 0.5f;
                            fy1 = 1f;
                        }
                        else
                        {
                            fy0 = 0f;
                            fy1 = 0.5f;
                        }
                    }

                    s_Xy[0].x = s_Xy[1].x = Mathf.Lerp(v.x, v.z, fx0);
                    s_Xy[2].x = s_Xy[3].x = Mathf.Lerp(v.x, v.z, fx1);

                    s_Xy[0].y = s_Xy[3].y = Mathf.Lerp(v.y, v.w, fy0);
                    s_Xy[1].y = s_Xy[2].y = Mathf.Lerp(v.y, v.w, fy1);

                    s_Uv[0].x = s_Uv[1].x = Mathf.Lerp(tx0, tx1, fx0);
                    s_Uv[2].x = s_Uv[3].x = Mathf.Lerp(tx0, tx1, fx1);

                    s_Uv[0].y = s_Uv[3].y = Mathf.Lerp(ty0, ty1, fy0);
                    s_Uv[1].y = s_Uv[2].y = Mathf.Lerp(ty0, ty1, fy1);

                    float val = fillClockwise ? fillAmount * 2f - side : fillAmount * 2f - (1 - side);

                    if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), fillClockwise, ((side + fillOrigin + 3) % 4)))
                        qb.Add_0312(s_Xy, s_Uv);
                }
            }
            else if (fillMethod == FillMethod.Radial360)
            {
                for (var corner = 0; corner < 4; ++corner)
                {
                    float fx0, fx1, fy0, fy1;

                    if (corner < 2)
                    {
                        fx0 = 0f;
                        fx1 = 0.5f;
                    }
                    else
                    {
                        fx0 = 0.5f;
                        fx1 = 1f;
                    }

                    if (corner is 0 or 3)
                    {
                        fy0 = 0f;
                        fy1 = 0.5f;
                    }
                    else
                    {
                        fy0 = 0.5f;
                        fy1 = 1f;
                    }

                    s_Xy[0].x = s_Xy[1].x = Mathf.Lerp(v.x, v.z, fx0);
                    s_Xy[2].x = s_Xy[3].x = Mathf.Lerp(v.x, v.z, fx1);

                    s_Xy[0].y = s_Xy[3].y = Mathf.Lerp(v.y, v.w, fy0);
                    s_Xy[1].y = s_Xy[2].y = Mathf.Lerp(v.y, v.w, fy1);

                    s_Uv[0].x = s_Uv[1].x = Mathf.Lerp(tx0, tx1, fx0);
                    s_Uv[2].x = s_Uv[3].x = Mathf.Lerp(tx0, tx1, fx1);

                    s_Uv[0].y = s_Uv[3].y = Mathf.Lerp(ty0, ty1, fy0);
                    s_Uv[1].y = s_Uv[2].y = Mathf.Lerp(ty0, ty1, fy1);

                    var val = fillClockwise ?
                        fillAmount * 4f - ((corner + fillOrigin) % 4) :
                        fillAmount * 4f - (3 - ((corner + fillOrigin) % 4));

                    if (RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), fillClockwise, ((corner + 2) % 4)))
                        qb.Add_0312(s_Xy, s_Uv);
                }
            }

            qb.Commit(color);
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// </summary>
        private static bool RadialCut(Vector2[] xy, Vector2[] uv, float fill, bool invert, int corner)
        {
            // Nothing to fill
            if (fill < 0.001f) return false;

            // Even corners invert the fill direction
            if ((corner & 1) == 1) invert = !invert;

            // Nothing to adjust
            if (!invert && fill > 0.999f) return true;

            // Convert 0-1 value into 0 to 90 degrees angle in radians
            float angle = Mathf.Clamp01(fill);
            if (invert) angle = 1f - angle;
            angle *= Mathf.PI / 2;

            // Calculate the effective X and Y factors
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            RadialCut(xy, cos, sin, invert, corner);
            RadialCut(uv, cos, sin, invert, corner);
            return true;
        }

        /// <summary>
        /// Adjust the specified quad, making it be radially filled instead.
        /// </summary>
        private static void RadialCut(Vector2[] xy, float cos, float sin, bool invert, int corner)
        {
            int i0 = corner;
            int i1 = ((corner + 1) % 4);
            int i2 = ((corner + 2) % 4);
            int i3 = ((corner + 3) % 4);

            if ((corner & 1) == 1)
            {
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
            else
            {
                if (cos > sin)
                {
                    sin /= cos;
                    cos = 1f;

                    if (!invert)
                    {
                        xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                        xy[i2].y = xy[i1].y;
                    }
                }
                else if (sin > cos)
                {
                    cos /= sin;
                    sin = 1f;

                    if (invert)
                    {
                        xy[i2].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
                        xy[i3].x = xy[i2].x;
                    }
                }
                else
                {
                    cos = 1f;
                    sin = 1f;
                }

                if (invert) xy[i3].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
                else xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
            }
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }
    }
}
