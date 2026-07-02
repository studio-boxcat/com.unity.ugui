#nullable enable
namespace UnityEngine.UI
{
    // Sequential packed-vert cursor over the pos/uv channels (3/2 floats per vert; z stays 0 — no pos
    // writer ever stores a non-zero z, so the pooled slots stay 0 from allocation). Each call writes
    // one vert (or quad) and advances — no index math or cursor threading at call sites.
    public unsafe ref struct StripBuilder
    {
        private float* _pf;
        private float* _uf;

        public StripBuilder(int vertCount, MeshBuilder mb)
        {
            _pf = mb.Poses.SetUpUnsafe(vertCount).Ptr;
            _uf = mb.UVs.SetUpUnsafe(vertCount).Ptr;
        }

        public void Vert(float x, float y, float u, float v)
        {
            _pf[0] = x; _pf[1] = y; _pf += 3;
            _uf[0] = u; _uf[1] = v; _uf += 2;
        }

        // Pos slots picked by axis/cross; UV stays (u, v) — a strip rotated 90° keeps U horizontal on the sprite.
        public void VertAxis(int axis, int cross, float axisPos, float crossPos, float u, float v)
        {
            _pf[axis] = axisPos; _pf[cross] = crossPos; _pf += 3;
            _uf[0] = u; _uf[1] = v; _uf += 2;
        }

        // Pos and UV slots both picked by axis/cross — e.g. a Y-tiling strip tiles V instead of U.
        public void VertAxisUV(int axis, int cross, float axisPos, float crossPos, float axisUV, float crossUV)
        {
            _pf[axis] = axisPos; _pf[cross] = crossPos; _pf += 3;
            _uf[axis] = axisUV; _uf[cross] = crossUV; _uf += 2;
        }

        // Axis-aligned quad: x in [xL,xR], y in [yB,yT], U = uL/uR, V = vB/vT (flip to mirror or collapse
        // to a solid fill). Vert layout 0=BL,1=BR,2=TL,3=TR — matches Indices.SetUp_Quad.
        public void Quad(float xL, float xR, float yB, float yT, float uL, float uR, float vB, float vT)
        {
            Vert(xL, yB, uL, vB);
            Vert(xR, yB, uR, vB);
            Vert(xL, yT, uL, vT);
            Vert(xR, yT, uR, vT);
        }
    }
}
