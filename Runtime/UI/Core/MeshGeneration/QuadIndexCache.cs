using System;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class QuadIndexCache
    {
        // Visual presentation of the vertex order.
        // 2 3
        // 0 1

        const int _minQuadCount = 40;
        const int _maxQuadCount = 250;
        static int _cachedQuadCount = 0;
        static ushort[] _indices = Array.Empty<ushort>();

        public static readonly ushort[] Single = {0, 2, 3, 3, 1, 0};

        public static ushort[] Get(int quadCount)
        {
            Assert.IsTrue(_indices.Length % 6 == 0, "Indices must be a multiple of 6.");
            Assert.IsTrue(_indices.Length / 6 == _cachedQuadCount, "_indices.Length / 6 == _cachedQuadCount");


            // If the requested quad count is already allocated, return the indices.
            if (quadCount <= _cachedQuadCount)
                return _indices;


            // Resize if the requested quad count is larger than the current cache.
            var oldQuadCount = _indices.Length / 6;
            if (oldQuadCount < quadCount // If not enough indices, resize.
                && oldQuadCount != _maxQuadCount) // but not if it's already maxed out.
            {
                var newQuadCount = Mathf.Clamp(quadCount, _minQuadCount, _maxQuadCount);
                Array.Resize(ref _indices, newQuadCount * 6);

                // Fill newly allocated quads.
                for (var i = oldQuadCount; i < newQuadCount; i++)
                    SetQuadIndices(_indices, i);
                _cachedQuadCount = newQuadCount;
            }

            // If the requested quad count is successfully allocated, return the indices.
            if (quadCount <= _cachedQuadCount)
                return _indices;

            // If the requested quad count is larger than the max, return a new array.
#if DEBUG
            Debug.LogWarning($"[QuadIndexCache] Quad count is larger than the max: {quadCount} > {_maxQuadCount}");
#endif
            Assert.IsTrue(quadCount > _maxQuadCount, "Quad count must be larger than the max.");
            var tmpIndices = new ushort[quadCount * 6];
            Array.Copy(_indices, tmpIndices, _cachedQuadCount * 6);
            for (var i = _cachedQuadCount; i < quadCount; i++)
                SetQuadIndices(tmpIndices, i);
            return tmpIndices;

            static void SetQuadIndices(ushort[] indices, int quadIndex)
            {
                var startingIndex = quadIndex * 6;
                var vertexOffset = quadIndex * 4;
                indices[startingIndex + 0] = (ushort) (0 + vertexOffset);
                indices[startingIndex + 1] = (ushort) (2 + vertexOffset);
                indices[startingIndex + 2] = (ushort) (3 + vertexOffset);
                indices[startingIndex + 3] = (ushort) (3 + vertexOffset);
                indices[startingIndex + 4] = (ushort) (1 + vertexOffset);
                indices[startingIndex + 5] = (ushort) (0 + vertexOffset);
            }
        }
    }
}