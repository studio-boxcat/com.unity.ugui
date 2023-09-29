using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public class MeshQuadBuilder
    {
        readonly MeshBuilder _mb;
        readonly int _capacity;
        readonly Vector3[] _poses;
        readonly Vector2[] _uvs;
        int _count;


        public MeshQuadBuilder(MeshBuilder mb, int capacity)
        {
            _mb = mb;
            _capacity = capacity;

            var vertCapacity = capacity * 4;
            _poses = _mb.Poses.SetUp(vertCapacity);
            _uvs = _mb.UVs.SetUp(vertCapacity);
        }

        public void Add(Vector2 p1, Vector2 p2, Vector2 uv1, Vector2 uv2)
        {
            Assert.IsTrue(_count < _capacity);

            var i = _count++ * 4;

            _poses[i] = p1;
            _poses[i + 1] = new Vector3(p2.x, p1.y);
            _poses[i + 2] = new Vector3(p1.x, p2.y);
            _poses[i + 3] = p2;

            _uvs[i] = uv1;
            _uvs[i + 1] = new Vector2(uv2.x, uv1.y);
            _uvs[i + 2] = new Vector2(uv1.x, uv2.y);
            _uvs[i + 3] = uv2;
        }

        public void Add(Vector2[] poses, Vector2[] uvs)
        {
            Assert.AreEqual(4, poses.Length);
            Assert.AreEqual(4, uvs.Length);

            var s = _count++ * 4; // start index
            for (var i = 0; i < 4; ++i)
            {
                _poses[s + i] = poses[i];
                _uvs[s + i] = uvs[i];
            }
        }

        public void Commit(Color32 color)
        {
            var vertCount = _count * 4;
            _mb.Poses.TrimEnd(vertCount);
            _mb.UVs.TrimEnd(vertCount);
            _mb.Colors.SetUp(color, vertCount);
            _mb.Indices.SetUp_Quad(_count);
        }
    }

    public class MeshBuilder
    {
        public readonly MeshPosChannel Poses = new(64);
        public readonly MeshUVChannel UVs = new(64);
        public readonly MeshColorChannel Colors = new(64);
        public readonly MeshIndexChannel Indices = new(96);

        public bool CheckPrepared()
        {
            var posCount = Poses.Count;
            var uvCount = UVs.Count;
            var colorCount = Colors.Count;
            var indexCount = Indices.Count;

            return posCount != -1
                   && posCount == uvCount
                   && uvCount == colorCount
                   && indexCount >= posCount;
        }

        [MustUseReturnValue]
        public MeshQuadBuilder SetUp_Quad(int quadCapacity)
        {
            return new MeshQuadBuilder(this, quadCapacity);
        }

        public void SetUp_Quad(Vector2 pos1, Vector2 pos2, Vector2 uv1, Vector2 uv2, Color32 color)
        {
            // Pos
            var poses = Poses.SetUp(4);
            poses[0] = pos1;
            poses[1] = new Vector3(pos2.x, pos1.y);
            poses[2] = new Vector3(pos1.x, pos2.y);
            poses[3] = pos2;

            // UV
            var uvs = UVs.SetUp(4);
            uvs[0] = uv1;
            uvs[1] = new Vector2(uv2.x, uv1.y);
            uvs[2] = new Vector2(uv1.x, uv2.y);
            uvs[3] = uv2;

            // Color & Index
            Colors.SetUp(color, 4);
            Indices.SetUp(QuadIndexCache.Single);
        }

        public void SetUp_Quad_FullUV(Vector2 pos1, Vector2 pos2, Color32 color)
        {
            SetUp_Quad(pos1, pos2, new Vector2(0, 0), new Vector2(1, 1), color);
        }

        public void Clear()
        {
            Poses.Clear();
            UVs.Clear();
            Colors.Clear();
            Indices.Clear();
        }

        public void TrimEnd(int trimVert, int trimIndex)
        {
            Assert.IsTrue(CheckPrepared());

            Poses.TrimEnd(trimVert);
            UVs.TrimEnd(trimVert);
            Colors.TrimEnd(trimVert);
            Indices.TrimEnd(trimIndex);
        }

        public void FillMesh(Mesh mesh)
        {
            Assert.IsTrue(CheckPrepared(), "Mesh is not prepared");
            Assert.AreEqual(0, mesh.vertexCount, "Mesh is not empty");
            Assert.AreEqual(0, mesh.GetIndexCount(0), "Mesh is not empty");

            if (Poses.Count == 0)
            {
                mesh.Clear();
                return;
            }

            Poses.FillMesh(mesh);
            UVs.FillMesh(mesh);
            Colors.FillMesh(mesh);
            Indices.FillMesh(mesh);
            mesh.RecalculateBounds();
        }

        public void Invalidate()
        {
            Poses.Invalidate();
            UVs.Invalidate();
            Colors.Invalidate();
            Indices.Invalidate();
        }
    }
}