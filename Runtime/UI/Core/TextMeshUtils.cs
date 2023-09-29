using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class TextMeshUtils
    {
        public static void Translate(List<UIVertex> verts, float pixelsPerUnit, MeshBuilder toFill)
        {
            // If there's no vertices, skip.
            var vertCount = verts.Count;
            if (vertCount == 0)
                return;

            Assert.IsTrue(vertCount % 4 == 0);
            var quadCount = vertCount / 4;

            var poses = toFill.Poses.SetUp(vertCount);
            var uvs = toFill.UVs.SetUp(vertCount);
            var colors = toFill.Colors.SetUp(vertCount);

            // Apply the offset to the vertices and add them to the mesh.
            var unitsPerPixel = 1 / pixelsPerUnit;
            for (var i = 0; i < vertCount; ++i)
            {
                var v = verts[i];
                var pos = v.position;
                pos.x *= unitsPerPixel;
                pos.y *= unitsPerPixel;
                poses[i] = pos;
                uvs[i] = v.uv0;
                colors[i] = v.color;
            }

            // Add indices.
            toFill.Indices.SetUp(GetIndex(quadCount), quadCount * 6);
        }

        static ushort[] _indexCache = Array.Empty<ushort>();

        static ushort[] GetIndex(int quadCount)
        {
            // Minimum 80 quads.
            if (quadCount < 80)
                quadCount = 80;

            var oldIndexCount = _indexCache.Length;
            var newIndexCount = quadCount * 6;

            if (oldIndexCount >= newIndexCount)
                return _indexCache;

            Array.Resize(ref _indexCache, newIndexCount);

            Assert.IsTrue(oldIndexCount % 6 == 0);
            var oldQuadCount = oldIndexCount / 6;
            for (var q = oldQuadCount; q < quadCount; q++)
            {
                var si = q * 6; // start index.
                var sv = q * 4; // start vertex.
                _indexCache[si] = (ushort) sv;
                _indexCache[si + 1] = (ushort) (sv + 1);
                _indexCache[si + 2] = (ushort) (sv + 2);
                _indexCache[si + 3] = (ushort) (sv + 2);
                _indexCache[si + 4] = (ushort) (sv + 3);
                _indexCache[si + 5] = (ushort) sv;
            }

            return _indexCache;
        }
    }
}