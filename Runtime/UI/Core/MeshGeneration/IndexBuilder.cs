#nullable enable
namespace UnityEngine.UI
{
    // Sequential index cursor: each Quad call appends the 6 indices (2 triangles) of one quad.
    public ref struct IndexBuilder
    {
        private readonly ushort[] _indices;
        private int _ip;

        public IndexBuilder(int quadCount, MeshBuilder mb)
        {
            _indices = mb.Indices.SetUp(quadCount * 6);
            _ip = 0;
        }

        // bl/tl = left column low/high; br/tr = right column low/high.
        public void Quad(int bl, int tl, int br, int tr)
        {
            var indices = _indices;
            var ip = _ip;
            indices[ip] = (ushort) bl;
            indices[ip + 1] = (ushort) tl;
            indices[ip + 2] = (ushort) tr;
            indices[ip + 3] = (ushort) bl;
            indices[ip + 4] = (ushort) tr;
            indices[ip + 5] = (ushort) br;
            _ip = ip + 6;
        }
    }
}
