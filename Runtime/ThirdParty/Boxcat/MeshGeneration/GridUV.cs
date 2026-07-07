#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEngine.UI
{
    public static class GridUV
    {
        // TODO: Should implement cache eviction like LRU.
        private static readonly Dictionary<int, Vector2[]> _r3c3 = new();
        private static readonly Dictionary<int, Vector2[]> _mx_R3C3 = new();
        private static readonly Dictionary<int, Vector2[]> _mxy_R3C3 = new();

        private static bool TryGetCached(Dictionary<int, Vector2[]> cache, int key, [NotNullWhen(true)] out Vector2[]? uvs)
        {
            PlayModeCache.Invalidate(cache);
            return cache.TryGetValue(key, out uvs);
        }

        public static void SetUp_R1C3(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(8);
            var m = new UVMatrix(sprite);
            uvs[0] = m._00;
            uvs[1] = uvs[2] = m._10;
            uvs[3] = m._30;
            uvs[4] = m._03;
            uvs[5] = uvs[6] = m._13;
            uvs[7] = m._33;
        }

        private static Vector2[] R3C3(Sprite sprite)
        {
            var dict = _r3c3;
            var id = sprite.GetInstanceID();
            if (TryGetCached(dict, id, out var uvs))
                return uvs;

            uvs = new Vector2[16];

            var m = new UVMatrix(sprite);
            uvs[0] = m._00;
            uvs[1] = m._10;
            uvs[2] = m._20;
            uvs[3] = m._30;
            uvs[4] = m._01;
            uvs[5] = m._11;
            uvs[6] = m._21;
            uvs[7] = m._31;
            uvs[8] = m._02;
            uvs[9] = m._12;
            uvs[10] = m._22;
            uvs[11] = m._32;
            uvs[12] = m._03;
            uvs[13] = m._13;
            uvs[14] = m._23;
            uvs[15] = m._33;

            return dict[id] = uvs;
        }

        public static void SetUp_R3C3(this MeshUVChannel c, Sprite sprite)
        {
            c.SetUp(R3C3(sprite));
        }

        public static void SetUp_MX_R1C3(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(8);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[3] = m._30;
            uvs[1] = uvs[2] = m._00;
            uvs[4] = uvs[7] = m._33;
            uvs[5] = uvs[6] = m._03;
        }

        public static void SetUp_MX_R1C4(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(10);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[4] = m._30;
            uvs[5] = uvs[9] = m._33;
            uvs[1] = uvs[3] = m._21;
            uvs[6] = uvs[8] = m._22;
            uvs[2] = m._00;
            uvs[7] = m._03;
        }

        public static void SetUp_MX_R3C2(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(12);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[2] = m._30;
            uvs[1] = m._00;
            uvs[3] = uvs[5] = uvs[6] = uvs[8] = m._31;
            uvs[4] = uvs[7] = m._01;
            uvs[10] = m._03;
            uvs[9] = uvs[11] = m._33;
        }

        public static void SetUp_MX_R3C3(this MeshUVChannel c, Sprite sprite)
        {
            var dict = _mx_R3C3;
            var id = sprite.GetInstanceID();
            if (TryGetCached(dict, id, out var uvs) is false)
                uvs = dict[id] = Create(sprite);
            c.SetUp(uvs);
            return;

            static Vector2[] Create(Sprite sprite)
            {
                var uvs = new Vector2[16];
                var m = new UVMatrix(sprite);
                uvs[0] = uvs[3] = m._30;
                uvs[1] = uvs[2] = m._00;
                uvs[4] = uvs[7] = m._31;
                uvs[5] = uvs[6] = m._01;
                uvs[8] = uvs[11] = m._32;
                uvs[9] = uvs[10] = m._02;
                uvs[12] = uvs[15] = m._33;
                uvs[13] = uvs[14] = m._03;
                return uvs;
            }
        }

        // MX_R3C3 minus the top border row: rows are V0, V1, V2 (the top border band above V2 is ignored).
        public static void SetUp_MX_R2C3_NoTop(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(12);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[3] = m._30;
            uvs[1] = uvs[2] = m._00;
            uvs[4] = uvs[7] = m._31;
            uvs[5] = uvs[6] = m._01;
            uvs[8] = uvs[11] = m._32;
            uvs[9] = uvs[10] = m._02;
        }

        public static void SetUp_MY_R3C1(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(4 * 2);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[6] = m._03;
            uvs[1] = uvs[7] = m._33;
            uvs[2] = uvs[4] = m._00;
            uvs[3] = uvs[5] = m._30;
        }

        public static void SetUp_MY_R3C2(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(4 * 3);
            var (min, max) = sprite.GetOuterUV();

            uvs[0] = uvs[1] = uvs[9] = uvs[10] = new Vector2(min.x, max.y);
            uvs[3] = uvs[4] = uvs[6] = uvs[7] = min;
            uvs[2] = uvs[11] = new Vector2(max.x, max.y);
            uvs[5] = uvs[8] = new Vector2(max.x, min.y);
        }

        public static void SetUp_MY_R3C3(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(16);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[12] = m._03;
            uvs[1] = uvs[13] = m._13;
            uvs[2] = uvs[14] = m._23;
            uvs[3] = uvs[15] = m._33;
            uvs[4] = uvs[8] = m._00;
            uvs[5] = uvs[9] = m._10;
            uvs[6] = uvs[10] = m._20;
            uvs[7] = uvs[11] = m._30;
        }

        public static void SetUp_MX_R3C4(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(20);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[4] = m._30;
            uvs[5] = uvs[9] = m._31;
            uvs[10] = uvs[14] = m._32;
            uvs[15] = uvs[19] = m._33;
            uvs[1] = uvs[3] = m._20;
            uvs[6] = uvs[8] = m._21;
            uvs[11] = uvs[13] = m._22;
            uvs[16] = uvs[18] = m._23;
            uvs[2] = m._00;
            uvs[7] = m._01;
            uvs[12] = m._02;
            uvs[17] = m._03;
        }

        public static void SetUp_MX_R3C6(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(28);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[6] = m._30;
            uvs[7] = uvs[13] = m._31;
            uvs[14] = uvs[20] = m._32;
            uvs[21] = uvs[27] = m._33;
            uvs[1] = uvs[5] = m._20;
            uvs[8] = uvs[12] = m._21;
            uvs[15] = uvs[19] = m._22;
            uvs[22] = uvs[26] = m._23;
            uvs[2] = uvs[4] = m._10;
            uvs[9] = uvs[11] = m._11;
            uvs[16] = uvs[18] = m._12;
            uvs[23] = uvs[25] = m._13;
            uvs[3] = m._00;
            uvs[10] = m._01;
            uvs[17] = m._02;
            uvs[24] = m._03;
        }

        public static void SetUp_MY_R2C2(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(3 * 3);
            var (min, max) = sprite.GetOuterUV();
            uvs[0] = uvs[1] = uvs[6] = uvs[7] = new Vector2(min.x, max.y);
            uvs[2] = uvs[8] = max;
            uvs[3] = uvs[4] = min;
            uvs[5] = new Vector2(max.x, min.y);
        }

        public static void SetUp_MY_R2C3(this MeshUVChannel c, Sprite sprite)
        {
            var uvs = c.SetUp(3 * 4);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[8] = m._03;
            uvs[1] = uvs[2] = uvs[9] = uvs[10] = m._13;
            uvs[3] = uvs[11] = m._33;
            uvs[4] = m._00;
            uvs[5] = uvs[6] = m._10;
            uvs[7] = m._30;
        }

        public static void SetUp_MXY_R3C2(this MeshUVChannel c, Sprite sprite)
        {
            // MXY_R3C3 minus the inner X column: outer X edges share U3, the center seam is U2.
            var uvs = c.SetUp(12);
            var m = new UVMatrix(sprite);
            uvs[0] = uvs[2] = uvs[9] = uvs[11] = m._33; // Outer corners.
            uvs[4] = uvs[7] = m._22; // Inner center (seam).
            uvs[1] = uvs[10] = m._23; // Horizontal edges (bottom/top center).
            uvs[3] = uvs[5] = uvs[6] = uvs[8] = m._32; // Vertical edges (left/right).
        }

        public static void SetUp_MXY_R3C3(this MeshUVChannel c, Sprite sprite)
        {
            var dict = _mxy_R3C3;
            var id = sprite.GetInstanceID();
            if (TryGetCached(dict, id, out var uvs) is false)
                uvs = dict[id] = Create(sprite);
            c.SetUp(uvs);
            return;

            static Vector2[] Create(Sprite sprite)
            {
                var uvs = new Vector2[16];
                var m = new UVMatrix(sprite);
                uvs[0] = uvs[3] = uvs[12] = uvs[15] = m._33; // Outer corners.
                uvs[5] = uvs[6] = uvs[9] = uvs[10] = m._22; // Inner corners.
                uvs[1] = uvs[2] = uvs[13] = uvs[14] = m._23; // Horizontal edges.
                uvs[4] = uvs[7] = uvs[8] = uvs[11] = m._32; // Vertical edges.
                return uvs;
            }
        }
    }
}
