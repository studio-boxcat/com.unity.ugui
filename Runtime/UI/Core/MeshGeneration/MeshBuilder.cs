#nullable enable
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.U2D; // SpriteDataAccessExtensions: GetIndices

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

            var q = _count++;
            QuadMesh.SetPos(_poses, q, p1, p2);
            QuadMesh.SetUV(_uvs, q, uv1, uv2);
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
        public void AssertPrepared(bool checkColor = true)
        {
            var posCount = Poses.Count;
            var uvCount = UVs.Count;
            var indexCount = Indices.Count;
            Assert.AreNotEqual(Invalid, posCount, "Poses is not prepared");
            Assert.AreEqual(posCount, uvCount, "UVs is not prepared");
            Assert.AreNotEqual(Invalid, indexCount, "Indices is not prepared");

            if (checkColor)
            {
                var colorCount = Colors.Count;
                Assert.AreEqual(posCount, colorCount, "Colors is not prepared");
            }
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

        // Fills an empty mesh (colours only) when `isEmpty`; returns true so a caller can bail on a zero-area rect.
        public bool TrySetUpEmpty(bool isEmpty)
        {
            if (!isEmpty) return false;
            SetUp_EmptyExceptColors();
            return true;
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
            QuadMesh.SetPos(Poses.SetUp(4), 0, pos1, pos2);
            QuadMesh.SetUV(UVs.SetUp(4), 0, uv1, uv2);

            // Color & Index
            Colors.SetUp(color, 4);
            Indices.SetUp(QuadIndexCache.Single);
        }

        // Arbitrary quad (e.g. a sheared parallelogram) with a single shared UV.
        public void SetUp_Quad(Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, Vector2 uv, Color32 color)
        {
            var poses = Poses.SetUp(4);
            poses[QuadMesh.BL] = bl;
            poses[QuadMesh.BR] = br;
            poses[QuadMesh.TL] = tl;
            poses[QuadMesh.TR] = tr;

            UVs.SetUp(uv, 4);
            Colors.SetUp(color, 4);
            Indices.SetUp(QuadIndexCache.Single);
        }

        public void SetUp_Quad_FullUV(Vector2 pos1, Vector2 pos2, Color32 color) => SetUp_Quad(pos1, pos2, new Vector2(0, 0), new Vector2(1, 1), color);
        public void SetUp_Quad_FullUV(Rect rect, Color32 color) => SetUp_Quad_FullUV(rect.min, rect.max, color);

        public void Invalidate()
        {
            Poses.Invalidate();
            UVs.Invalidate();
            Colors.Invalidate();
            Indices.Invalidate();
        }

        public void TrimAfter(int trimVert, int trimIndex)
        {
            AssertPrepared();

            Poses.TrimAfter(trimVert);
            UVs.TrimAfter(trimVert);
            Colors.TrimAfter(trimVert);
            Indices.TrimAfter(trimIndex);
        }

        public void Fill(Mesh mesh)
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

        public void FillAndInvalidate(Mesh mesh)
        {
            Fill(mesh);
            Invalidate();
        }

        public void FillAndInvalidate(Sprite sprite)
        {
            AssertPrepared(checkColor: false);

            using var ps = Poses.AllocateNativeArray(Allocator.Temp);
            using var uvs = UVs.AllocateNativeArray(Allocator.Temp);
            using var tris = Indices.AllocateNativeArray(Allocator.Temp);
            sprite.SetMesh(ps, uvs, tris);

            Invalidate();
        }

        public void SetMeshAndInvalidate(CanvasRenderer canvasRenderer)
        {
            Assert.IsTrue(Poses.Count is not Invalid, "Poses is not prepared.");
            var mesh = MeshPool.Rent();
            Assert.IsTrue(mesh.vertexCount is 0, "Mesh is not empty. Please clear the mesh before building it again.");
            FillAndInvalidate(mesh);
            canvasRenderer.SetMesh(mesh);
            MeshPool.Return(mesh); // return the mesh to the pool
        }
    }
}
