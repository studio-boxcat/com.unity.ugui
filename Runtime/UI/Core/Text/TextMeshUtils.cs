using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class TextMeshUtils
    {
        private static readonly List<UIVertex> _vertBuf = new();

        public static unsafe void Translate(TextGenerator textGen, float pixelsPerUnit, float yOffset, MeshBuilder toFill)
        {
            _vertBuf.Clear();
            textGen.GetVertices(_vertBuf);

            // If there's no vertices, skip.
            var vertCount = _vertBuf.Count;
            if (vertCount == 0)
            {
                toFill.SetUp_Empty();
                return;
            }

            Assert.IsTrue(vertCount % 4 == 0);
            var quadCount = vertCount / 4;

            var pf = toFill.Poses.SetUpUnsafe(vertCount).Ptr;
            var uf = toFill.UVs.SetUpUnsafe(vertCount).Ptr;
            var cp = toFill.Colors.SetUpUnsafe(vertCount);

            // The generator emits quads in perimeter order (0=TL 1=TR 2=BR 3=BL); write them in the
            // channel convention (0=BL 1=BR 2=TL 3=TR) so the shared QuadIndexCache applies.
            var unitsPerPixel = 1 / pixelsPerUnit;
            for (var q = 0; q < quadCount; ++q)
            {
                var v = q * 4;
                Write(v + 3, ref pf, ref uf, ref cp, unitsPerPixel, yOffset); // BL
                Write(v + 2, ref pf, ref uf, ref cp, unitsPerPixel, yOffset); // BR
                Write(v + 0, ref pf, ref uf, ref cp, unitsPerPixel, yOffset); // TL
                Write(v + 1, ref pf, ref uf, ref cp, unitsPerPixel, yOffset); // TR
            }

            toFill.Indices.SetUp_Quad(quadCount);
        }

        // One vert from the generator buffer to the packed channels; advances the cursors. z stays 0.
        private static unsafe void Write(int src, ref float* pf, ref float* uf, ref Color32* cp,
            float unitsPerPixel, float yOffset)
        {
            var vert = _vertBuf[src];
            var pos = vert.position;
            pf[0] = pos.x * unitsPerPixel;
            pf[1] = (pos.y + yOffset) * unitsPerPixel;
            pf += 3;
            var uv = vert.uv0;
            uf[0] = uv.x;
            uf[1] = uv.y;
            uf += 2;
            *cp++ = vert.color;
        }
    }
}
