using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public class MeshQuadBuilder
    {
        private readonly MeshBuilder _mb;
        private readonly int _capacity;
        private readonly Vector3[] _poses;
        private readonly Vector2[] _uvs;
        private int _count;


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

        public void Add_0312(Vector2[] poses, Vector2[] uvs)
        {
            // Visual representation:
            // 1 2
            // 0 3

            Assert.AreEqual(4, poses.Length);
            Assert.AreEqual(4, uvs.Length);

            var s = _count++ * 4; // start index
            _poses[s + 0] = poses[0];
            _poses[s + 1] = poses[3];
            _poses[s + 2] = poses[1];
            _poses[s + 3] = poses[2];
            _uvs[s + 0] = uvs[0];
            _uvs[s + 1] = uvs[3];
            _uvs[s + 2] = uvs[1];
            _uvs[s + 3] = uvs[2];
        }

        public void Commit(Color32 color)
        {
            var vertCount = _count * 4;
            _mb.Poses.TrimAfter(vertCount);
            _mb.UVs.TrimAfter(vertCount);
            _mb.Colors.SetUp(color, vertCount);
            _mb.Indices.SetUp_Quad(_count);
        }
    }

    public class MeshBuilder
    {
        public const int Invalid = -1;

        public readonly MeshPosChannel Poses = new(64);
        public readonly MeshUVChannel UVs = new(64);
        public readonly MeshColorChannel Colors = new(64);
        public readonly MeshIndexChannel Indices = new(96);

        [Conditional("DEBUG")]
        public void AssertPrepared()
        {
            var posCount = Poses.Count;
            var uvCount = UVs.Count;
            var colorCount = Colors.Count;
            var indexCount = Indices.Count;
            Assert.AreNotEqual(Invalid, posCount, "Poses is not prepared");
            Assert.AreEqual(posCount, uvCount, "UVs is not prepared");
            Assert.AreEqual(posCount, colorCount, "Colors is not prepared");
            Assert.AreNotEqual(Invalid, indexCount, "Indices is not prepared");
        }

        public bool HasSetUp() => Poses.Count is not Invalid;

        public void SetUp_Empty()
        {
            // ReSharper disable MustUseReturnValue
            Poses.SetUp(0);
            UVs.SetUp(0);
            Colors.SetUp(0);
            Indices.SetUp(0);
            // ReSharper restore MustUseReturnValue
        }

        public void SetUp_EmptyExceptColors()
        {
            // ReSharper disable MustUseReturnValue
            Poses.SetUp(0);
            UVs.SetUp(0);
            Indices.SetUp(0);
            // ReSharper restore MustUseReturnValue
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

        public void TrimAfter(int trimVert, int trimIndex)
        {
            AssertPrepared();

            Poses.TrimAfter(trimVert);
            UVs.TrimAfter(trimVert);
            Colors.TrimAfter(trimVert);
            Indices.TrimAfter(trimIndex);
        }

        public void FillMesh(Mesh mesh)
        {
            AssertPrepared();
            Assert.AreEqual(0, mesh.vertexCount, "Mesh is not empty");
            Assert.AreEqual(0, mesh.GetIndexCount(0), "Mesh is not empty");

            if (Poses.Count == 0)
            {
                mesh.Clear();
                return;
            }

            // Check all index is in range.
            // var vertCount = Poses.Count;
            // var indexCount = Indices.Count;
            // for (var i = 0; i < indexCount; i++)
            // {
            //     if (Indices[i] < vertCount) continue;
            //     Debug.LogError("Index is out of range: " + Indices[i] + " / " + vertCount);
            //     break;
            // }

            Poses.FillMesh(mesh);
            UVs.FillMesh(mesh);
            Colors.FillMesh(mesh);
            Indices.FillMesh(mesh);
            mesh.RecalculateBounds();
        }

        public void Invalidate()
        {
            AssertPrepared();
            Poses.Invalidate();
            UVs.Invalidate();
            Colors.Invalidate();
            Indices.Invalidate();
        }

        public void FillMeshAndInvalidate(Mesh mesh)
        {
            FillMesh(mesh);
            Invalidate();
        }

        public void SetMeshAndInvalidate(CanvasRenderer canvasRenderer)
        {
            Assert.IsTrue(Poses.Count is not Invalid, "Poses is not prepared.");
            var mesh = MeshPool.Rent();
            Assert.IsTrue(mesh.vertexCount is 0, "Mesh is not empty. Please clear the mesh before building it again.");
            FillMeshAndInvalidate(mesh);
            canvasRenderer.SetMesh(mesh);
            MeshPool.Return(mesh); // return the mesh to the pool
        }
    }
}