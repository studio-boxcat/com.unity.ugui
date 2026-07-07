#nullable enable
using UnityEngine;

namespace UnityEngine.UI
{
    // Fused sprite-mesh writer behind the static shortcuts: one vert block per copy (UVs/indices
    // repeated), each copy written in a single fused pass — every float computed and stored exactly
    // once. Flips/mirrors are negated scale literals, so loops carry no zero terms. Writes skip z:
    // no pos writer ever stores a non-zero z, so the pooled slots stay 0 from allocation.
    internal readonly unsafe ref struct SpriteMeshWriter
    {
        private readonly Vector2[] _srcPoses;
        private readonly float* _pf;
        private readonly int _vertCount;

        private SpriteMeshWriter(Sprite sprite, int copies, MeshBuilder mb)
        {
            var (srcPoses, srcUVs, srcIndices, vertCount, _) = SpriteMeshCache.Get(sprite);
            _srcPoses = srcPoses;
            _vertCount = vertCount;

            if (copies == 1)
            {
                mb.UVs.SetUp(srcUVs);
                mb.Indices.SetUp(srcIndices);
            }
            else
            {
                mb.UVs.SetUp_Repeat(srcUVs, copies);
                mb.Indices.SetUp_Incremental(srcIndices, vertCount, copies);
            }

            // Poses pointer taken last: the UV/index SetUps above may allocate on growth, and a raw
            // channel pointer must not be held across allocations (MeshChannel.SetUpUnsafe contract).
            _pf = mb.Poses.SetUpUnsafe(vertCount * copies).Ptr;
        }

        // Single-copy shortcuts.
        public static void Scale(Sprite sprite, float sx, float sy, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 1, mb).Scale(sx, sy);

        public static void ScaleAdd(Sprite sprite, float sx, float ax, float sy, float ay, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 1, mb).ScaleAdd(sx, ax, sy, ay);

        // Mirror shortcuts: all copies in one fused pass — src × scale is computed once per vert and the
        // mirrored copy reuses it negated. `nx`/`ny` = add for the mirrored copy (vs `ax`/`ay` for the
        // original). Copy layout (X, Y): MirrorX/Y → (+), (−); MirrorXY → (+,+), (−,+), (−,−), (+,−).
        public static void ScaleMirrorX(Sprite sprite, float sx, float sy, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 2, mb).ScaleMirrorX(sx, sy);

        public static void ScaleMirrorY(Sprite sprite, float sx, float sy, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 2, mb).ScaleMirrorY(sx, sy);

        public static void ScaleMirrorXY(Sprite sprite, float sx, float sy, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 4, mb).ScaleMirrorXY(sx, sy);

        public static void ScaleAddMirrorX(Sprite sprite, float sx, float ax, float nx, float sy, float ay, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 2, mb).ScaleAddMirrorX(sx, ax, nx, sy, ay);

        public static void ScaleAddMirrorY(Sprite sprite, float sx, float ax, float sy, float ay, float ny, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 2, mb).ScaleAddMirrorY(sx, ax, sy, ay, ny);

        public static void ScaleAddMirrorXY(Sprite sprite, float sx, float ax, float nx, float sy, float ay, float ny, MeshBuilder mb)
            => new SpriteMeshWriter(sprite, copies: 4, mb).ScaleAddMirrorXY(sx, ax, nx, sy, ay, ny);

        // mesh = src × (sx, sy)
        private void Scale(float sx, float sy)
        {
            var src = _srcPoses;
            var q = _pf;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                q[0] = v.x * sx;
                q[1] = v.y * sy;
            }
        }

        // mesh = src × (sx, sy) + (ax, ay)
        private void ScaleAdd(float sx, float ax, float sy, float ay)
        {
            var src = _srcPoses;
            var q = _pf;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                q[0] = v.x * sx + ax;
                q[1] = v.y * sy + ay;
            }
        }

        private void ScaleMirrorX(float sx, float sy)
        {
            var src = _srcPoses;
            var q = _pf;
            var b = _vertCount * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx;
                var vy = v.y * sy;
                q[0] = vx; q[1] = vy;
                q[b] = -vx; q[b + 1] = vy;
            }
        }

        private void ScaleMirrorY(float sx, float sy)
        {
            var src = _srcPoses;
            var q = _pf;
            var b = _vertCount * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx;
                var vy = v.y * sy;
                q[0] = vx; q[1] = vy;
                q[b] = vx; q[b + 1] = -vy;
            }
        }

        private void ScaleMirrorXY(float sx, float sy)
        {
            var src = _srcPoses;
            var q = _pf;
            var b1 = _vertCount * 3;
            var b2 = b1 * 2;
            var b3 = b1 * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx;
                var vy = v.y * sy;
                var nx = -vx;
                var ny = -vy;
                q[0] = vx; q[1] = vy;
                q[b1] = nx; q[b1 + 1] = vy;
                q[b2] = nx; q[b2 + 1] = ny;
                q[b3] = vx; q[b3 + 1] = ny;
            }
        }

        private void ScaleAddMirrorX(float sx, float ax, float nx, float sy, float ay)
        {
            var src = _srcPoses;
            var q = _pf;
            var b = _vertCount * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx;
                var vy = v.y * sy + ay;
                q[0] = vx + ax; q[1] = vy;
                q[b] = nx - vx; q[b + 1] = vy;
            }
        }

        private void ScaleAddMirrorY(float sx, float ax, float sy, float ay, float ny)
        {
            var src = _srcPoses;
            var q = _pf;
            var b = _vertCount * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx + ax;
                var vy = v.y * sy;
                q[0] = vx; q[1] = vy + ay;
                q[b] = vx; q[b + 1] = ny - vy;
            }
        }

        private void ScaleAddMirrorXY(float sx, float ax, float nx, float sy, float ay, float ny)
        {
            var src = _srcPoses;
            var q = _pf;
            var b1 = _vertCount * 3;
            var b2 = b1 * 2;
            var b3 = b1 * 3;
            for (var i = 0; i < _vertCount; ++i, q += 3)
            {
                var v = src[i];
                var vx = v.x * sx;
                var vy = v.y * sy;
                float px = vx + ax, mx = nx - vx;
                float py = vy + ay, my = ny - vy;
                q[0] = px; q[1] = py;
                q[b1] = mx; q[b1 + 1] = py;
                q[b2] = mx; q[b2 + 1] = my;
                q[b3] = px; q[b3 + 1] = my;
            }
        }
    }
}
