#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    public readonly struct SpriteMeshInfo
    {
        public readonly Vector2[] Poses;
        public readonly Vector2[] UV;
        public readonly ushort[] Index;

        public readonly ushort VertCount;
        public readonly ushort IndexCount;

        public SpriteMeshInfo(Sprite sprite)
        {
            // TODO: Convert this into SpriteDataAccessExtensions.GetVertexAttribute<T>(sprite, VertexAttribute.Position);
            // Returns a copy of the array containing Sprite mesh vertex positions.
            Poses = sprite.vertices;
            UV = sprite.uv;
            Index = sprite.triangles;
            VertCount = (ushort)Poses.Length;
            IndexCount = (ushort)Index.Length;
        }

        public void Deconstruct(out Vector2[] pos, out Vector2[] uv, out ushort[] index)
        {
            pos = Poses;
            uv = UV;
            index = Index;
        }

        public void Deconstruct(out Vector2[] pos, out Vector2[] uv, out ushort[] index, out ushort vertCount, out ushort indexCount)
        {
            pos = Poses;
            uv = UV;
            index = Index;
            vertCount = VertCount;
            indexCount = IndexCount;
        }

        public (int VertexCount, int IndexCount) GetGeometryCounts()
        {
            return (VertCount, IndexCount);
        }
    }

    public static class SpriteMeshCache
    {
        // TODO: Clear cache when too many sprites are cached. Maybe generational GC?
        private static readonly Dictionary<int, SpriteMeshInfo> _cache = new();

        public static SpriteMeshInfo Get(Sprite sprite)
        {
            PlayModeCache.Invalidate(_cache);

            var instanceId = sprite.GetInstanceID();
            if (_cache.TryGetValue(instanceId, out var info))
                return info;

            info = new SpriteMeshInfo(sprite);
            _cache.Add(instanceId, info);
            return info;
        }
    }
}
