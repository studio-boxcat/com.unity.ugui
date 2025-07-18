using System;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    internal struct ShadowBuilder
    {
        private readonly Color32 _color;
        private readonly bool _useGraphicAlpha;

        private int _orgVertCount;
        private int _orgVertStart;
        private Vector3[] _poses;
        private Color32[] _colors;


        public ShadowBuilder(Color32 color, bool useGraphicAlpha) : this()
        {
            _color = color;
            _useGraphicAlpha = useGraphicAlpha;
        }

        public void Populate(MeshBuilder mb, int copy)
        {
            mb.AssertPrepared();
            Assert.IsTrue(copy > 0);

            _orgVertCount = mb.Poses.Count;
            _orgVertStart = _orgVertCount * copy;

            // Pos & Color: Copy original vertices and colors to the end.
            var newVertCount = _orgVertCount * (copy + 1);
            _poses = mb.Poses.Resize(newVertCount);
            _colors = mb.Colors.Resize(newVertCount);
            Array.Copy(_poses, 0, _poses, _orgVertStart, _orgVertCount);
            Array.Copy(_colors, 0, _colors, _orgVertStart, _orgVertCount);

            // UV: Copy original UVs repeatedly.
            var uvs = mb.UVs.Resize(newVertCount);
            for (var batchIndex = 1; batchIndex < copy + 1; batchIndex++)
                Array.Copy(uvs, 0, uvs, _orgVertCount * batchIndex, _orgVertCount);

            // Index: Copy original indices repeatedly with offset.
            var orgIndexCount = mb.Indices.Count;
            var newIndexCount = orgIndexCount * (copy + 1);
            var indices = mb.Indices.Resize(newIndexCount);
            for (var batchIndex = 1; batchIndex < copy + 1; batchIndex++)
            {
                var indexOffset = orgIndexCount * batchIndex;
                var vertOffset = _orgVertCount * batchIndex;
                for (var i = 0; i < orgIndexCount; i++)
                    indices[i + indexOffset] = (ushort) (indices[i] + vertOffset);
            }
        }

        public void Translate(int shadowIndex, float x, float y)
        {
            var vertDstStart = _orgVertCount * shadowIndex;

            for (var i = 0; i < _orgVertCount; ++i)
            {
                var si = _orgVertStart + i; // source index.
                var di = vertDstStart + i; // destination index.

                var v = _poses[si];
                v.x += x;
                v.y += y;
                _poses[di] = v;

                var newColor = _color;
                if (_useGraphicAlpha)
                    newColor.a = (byte) ((newColor.a * _colors[si].a) / 255);
                _colors[di] = newColor;
            }
        }
    }
}