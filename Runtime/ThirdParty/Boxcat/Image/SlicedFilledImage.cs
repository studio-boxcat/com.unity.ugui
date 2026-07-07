#nullable enable
// ReSharper disable InconsistentNaming

using Sirenix.OdinInspector;
using UnityEngine;
using Sprites = UnityEngine.Sprites;

namespace UnityEngine.UI
{
    // Credit: https://bitbucket.org/Unity-Technologies/ui/src/2018.4/UnityEngine.UI/UI/Core/Image.cs
    [AddComponentMenu("UI/Sliced Filled Image", 11)]
    public class SlicedFilledImage : UIImageBase
    {
        private enum FillDirection
        {
            Right = 0,
            Left = 1,
            Up = 2,
            Down = 3
        }

        private static readonly Vector2[] s_SlicedVertices = new Vector2[4];
        private static readonly Vector2[] s_SlicedUVs = new Vector2[4];

        [SerializeField, OnValueChanged(nameof(SetVerticesDirty))]
        private FillDirection m_FillDirection;

        [SerializeField, Range(0, 1), OnValueChanged(nameof(SetVerticesDirty))]
        private float m_FillAmount = 1f;

        public float fillAmount
        {
            get => m_FillAmount;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_FillAmount, Mathf.Clamp01(value)))
                    SetVerticesDirty();
            }
        }

        [SerializeField, OnValueChanged(nameof(SetVerticesDirty))]
        private bool m_FillCenter = true;

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            if (m_FillAmount < 0.001f)
                return;

            // Maximum 9 quads with 4 verts each; 9 quads with 6 indices each.
            var qb = mb.SetUp_Quad(9);

            var rect = rectTransform.rect;
            var outer = Sprites.DataUtility.GetOuterUV(sprite);
            var padding = Sprites.DataUtility.GetPadding(sprite);

            var inner = Sprites.DataUtility.GetInnerUV(sprite);
            var border = GetAdjustedBorders(sprite.border, rect);

            s_SlicedVertices[0] = new Vector2(padding.x, padding.y);
            s_SlicedVertices[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

            s_SlicedVertices[1].x = border.x;
            s_SlicedVertices[1].y = border.y;

            s_SlicedVertices[2].x = rect.width - border.z;
            s_SlicedVertices[2].y = rect.height - border.w;

            for (int i = 0; i < 4; ++i)
            {
                s_SlicedVertices[i].x += rect.x;
                s_SlicedVertices[i].y += rect.y;
            }

            s_SlicedUVs[0] = new Vector2(outer.x, outer.y);
            s_SlicedUVs[1] = new Vector2(inner.x, inner.y);
            s_SlicedUVs[2] = new Vector2(inner.z, inner.w);
            s_SlicedUVs[3] = new Vector2(outer.z, outer.w);

            float rectStartPos;
            float _1OverTotalSize;
            if (m_FillDirection is FillDirection.Left or FillDirection.Right)
            {
                rectStartPos = s_SlicedVertices[0].x;
                var totalSize = (s_SlicedVertices[3].x - s_SlicedVertices[0].x);
                _1OverTotalSize = totalSize > 0f ? 1f / totalSize : 1f;
            }
            else
            {
                rectStartPos = s_SlicedVertices[0].y;
                var totalSize = (s_SlicedVertices[3].y - s_SlicedVertices[0].y);
                _1OverTotalSize = totalSize > 0f ? 1f / totalSize : 1f;
            }

            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
            {
                if (!m_FillCenter && x == 1 && y == 1)
                    continue;

                var x2 = x + 1;
                var y2 = y + 1;

                float sliceStart, sliceEnd;
                switch (m_FillDirection)
                {
                    case FillDirection.Right:
                        sliceStart = (s_SlicedVertices[x].x - rectStartPos) * _1OverTotalSize;
                        sliceEnd = (s_SlicedVertices[x2].x - rectStartPos) * _1OverTotalSize;
                        break;
                    case FillDirection.Up:
                        sliceStart = (s_SlicedVertices[y].y - rectStartPos) * _1OverTotalSize;
                        sliceEnd = (s_SlicedVertices[y2].y - rectStartPos) * _1OverTotalSize;
                        break;
                    case FillDirection.Left:
                        sliceStart = 1f - (s_SlicedVertices[x2].x - rectStartPos) * _1OverTotalSize;
                        sliceEnd = 1f - (s_SlicedVertices[x].x - rectStartPos) * _1OverTotalSize;
                        break;
                    case FillDirection.Down:
                        sliceStart = 1f - (s_SlicedVertices[y2].y - rectStartPos) * _1OverTotalSize;
                        sliceEnd = 1f - (s_SlicedVertices[y].y - rectStartPos) * _1OverTotalSize;
                        break;
                    default: // Just there to get rid of the "Use of unassigned local variable" compiler error
                        sliceStart = sliceEnd = 0f;
                        break;
                }

                if (sliceStart >= m_FillAmount)
                    continue;

                var vertices = new Vector4(s_SlicedVertices[x].x, s_SlicedVertices[y].y, s_SlicedVertices[x2].x, s_SlicedVertices[y2].y);
                var uvs = new Vector4(s_SlicedUVs[x].x, s_SlicedUVs[y].y, s_SlicedUVs[x2].x, s_SlicedUVs[y2].y);
                var fillAmount = (m_FillAmount - sliceStart) / (sliceEnd - sliceStart);

                AddQuadIfNecessary(qb, vertices, uvs, fillAmount);
            }

            qb.Commit(color);
        }

        private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
        {
            Rect originalRect = rectTransform.rect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                // The adjusted rect (adjusted for pixel correctness) may be slightly larger than the original rect.
                // Adjust the border to match the adjustedRect to avoid small gaps between borders (case 833201).
                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        private void AddQuadIfNecessary(MeshQuadBuilder qb, Vector4 vertices, Vector4 uvs, float fillAmount)
        {
            if (m_FillAmount < 0.001f)
                return;

            float uvLeft = uvs.x;
            float uvBottom = uvs.y;
            float uvRight = uvs.z;
            float uvTop = uvs.w;

            if (fillAmount < 1f)
            {
                if (m_FillDirection is FillDirection.Left or FillDirection.Right)
                {
                    if (m_FillDirection == FillDirection.Left)
                    {
                        vertices.x = vertices.z - (vertices.z - vertices.x) * fillAmount;
                        uvLeft = uvRight - (uvRight - uvLeft) * fillAmount;
                    }
                    else
                    {
                        vertices.z = vertices.x + (vertices.z - vertices.x) * fillAmount;
                        uvRight = uvLeft + (uvRight - uvLeft) * fillAmount;
                    }
                }
                else
                {
                    if (m_FillDirection == FillDirection.Down)
                    {
                        vertices.y = vertices.w - (vertices.w - vertices.y) * fillAmount;
                        uvBottom = uvTop - (uvTop - uvBottom) * fillAmount;
                    }
                    else
                    {
                        vertices.w = vertices.y + (vertices.w - vertices.y) * fillAmount;
                        uvTop = uvBottom + (uvTop - uvBottom) * fillAmount;
                    }
                }
            }

            qb.Add(
                new Vector2(vertices.x, vertices.y),
                new Vector2(vertices.z, vertices.w),
                new Vector2(uvLeft, uvBottom),
                new Vector2(uvRight, uvTop));
        }
    }
}
