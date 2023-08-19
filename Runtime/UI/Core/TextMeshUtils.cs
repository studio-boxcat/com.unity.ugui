using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class TextMeshUtils
    {
        public static void Translate(List<UIVertex> verts, float pixelsPerUnit, VertexHelper toFill)
        {
            Assert.AreEqual(0, toFill.currentVertCount);
            Assert.AreEqual(0, toFill.currentIndexCount);

            // If there's no vertices, skip.
            var vertCount = verts.Count;
            if (vertCount == 0)
                return;


            // Apply the offset to the vertices and add them to the mesh.
            var unitsPerPixel = 1 / pixelsPerUnit;
            for (var i = 0; i < vertCount; ++i)
            {
                var v = verts[i];
                var pos = v.position;
                pos.x *= unitsPerPixel;
                pos.y *= unitsPerPixel;
                toFill.AddVert(pos, v.color, v.uv0);
            }


            // Add indices.
            Assert.IsTrue(vertCount % 4 == 0);
            var quadCount = vertCount / 4;
            for (var q = 0; q < quadCount; q++)
            {
                var i = q * 4;
                toFill.AddTriangle(i, i + 1, i + 2);
                toFill.AddTriangle(i + 2, i + 3, i);
            }
        }
    }
}