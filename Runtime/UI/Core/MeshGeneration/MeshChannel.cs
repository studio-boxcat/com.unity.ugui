using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public abstract class MeshChannel<T> where T : struct
    {
        protected T[] Data { get; private set; }
        public int Count { get; private set; }

        T[] _data;


        protected MeshChannel(int capacity)
        {
            _data = new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before accessing data.");
                Assert.IsTrue(index >= 0 && index < Count);
                return Data[index];
            }
        }

        public T[] SetUp(int count)
        {
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");
            Assert.IsTrue(count >= 0);

            if (_data.Length < count)
                _data = new T[count];
            Data = _data;
            Count = count;
            return _data;
        }

        public void SetUp(T[] data)
        {
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");

            Data = data;
            Count = data.Length;
        }

        public void SetUp(T[] data, int count)
        {
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");
            Assert.IsTrue(count >= 0 && count <= data.Length);

            Data = data;
            Count = count;
        }

        public void Clear()
        {
            Data = null;
            Count = 0;
        }

        [MustUseReturnValue]
        public T[] Edit()
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before editing.");

            // If the data is already writable, return it.
            if (ReferenceEquals(_data, Data))
                return _data;

            // Otherwise, copy the data to a writable array.
            if (_data.Length < Count)
                _data = new T[Count];
            Array.Copy(Data, 0, _data, 0, Count);
            Data = _data;
            return _data;
        }

        public T[] Resize(int count)
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before resizing.");
            Assert.IsTrue(count >= 0);

            // When resizing is not necessary.
            if (_data.Length >= count)
            {
                var _ = Edit();
                Count = count;
                return _data;
            }

            // When the data is already writable.
            if (ReferenceEquals(_data, Data))
            {
                Array.Resize(ref _data, count);
                Data = _data;
                Count = count;
                return _data;
            }

            // When the data is not writable.
            _data = new T[count];
            Array.Copy(Data, 0, _data, 0, Count);
            Data = _data;
            Count = count;
            return _data;
        }

        public void TrimEnd(int fromIndex)
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before trimming.");
            Assert.IsTrue(fromIndex <= Count, "fromIndex must be less than Count.");
            Count = fromIndex;
        }

        public void FillMesh(Mesh mesh)
        {
            Assert.IsNotNull(Data);
            Internal_FillMesh(mesh);
        }

        public void Invalidate()
        {
            Data = null;
            Count = 0;
        }

        protected abstract void Internal_FillMesh(Mesh mesh);
    }

    public class MeshPosChannel : MeshChannel<Vector3>
    {
        public MeshPosChannel(int capacity) : base(capacity)
        {
        }

        public Rect CalculateBoundingRect()
        {
            var xMin = float.MaxValue;
            var xMax = float.MinValue;
            var yMin = float.MaxValue;
            var yMax = float.MinValue;

            var data = Data;
            var count = Count;
            for (var i = 0; i < count; i++)
            {
                var v = data[i];
                var x = v.x;
                var y = v.y;
                xMin = Mathf.Min(xMin, x);
                yMin = Mathf.Min(yMin, y);
                xMax = Mathf.Max(xMax, x);
                yMax = Mathf.Max(yMax, y);
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        protected override void Internal_FillMesh(Mesh mesh)
        {
            mesh.SetVertices(Data, 0, Count,
                MeshUpdateFlags.DontValidateIndices
                | MeshUpdateFlags.DontResetBoneBounds
                | MeshUpdateFlags.DontNotifyMeshUsers
                | MeshUpdateFlags.DontRecalculateBounds);
        }
    }

    public class MeshUVChannel : MeshChannel<Vector2>
    {
        public MeshUVChannel(int capacity) : base(capacity)
        {
        }

        public void SetUp_Repeat(Vector2[] src, int repeat)
        {
            var count = src.Length;
            var data = SetUp(count * repeat);
            var offset = 0;
            for (var i = 0; i < repeat; i++, offset += count)
                Array.Copy(src, 0, data, offset, count);
        }

        protected override void Internal_FillMesh(Mesh mesh)
        {
            mesh.SetUVs(0, Data, 0, Count,
                MeshUpdateFlags.DontValidateIndices
                | MeshUpdateFlags.DontResetBoneBounds
                | MeshUpdateFlags.DontNotifyMeshUsers
                | MeshUpdateFlags.DontRecalculateBounds);
        }
    }

    public class MeshColorChannel : MeshChannel<Color32>
    {
        public MeshColorChannel(int capacity) : base(capacity)
        {
        }

        public void SetUp(Color32 color, int count)
        {
            if (WhiteColorCache.TryGet(color, count, out var data))
            {
                SetUp(data, count);
            }
            else
            {
                data = SetUp(count);
                Array.Fill(data, color, 0, count);
            }
        }

        protected override void Internal_FillMesh(Mesh mesh)
        {
            mesh.SetColors(Data, 0, Count,
                MeshUpdateFlags.DontValidateIndices
                | MeshUpdateFlags.DontResetBoneBounds
                | MeshUpdateFlags.DontNotifyMeshUsers
                | MeshUpdateFlags.DontRecalculateBounds);
        }
    }

    public class MeshIndexChannel : MeshChannel<ushort>
    {
        public MeshIndexChannel(int capacity) : base(capacity)
        {
        }

        public void SetUp_Quad(int quadCount)
        {
            SetUp(QuadIndexCache.Get(quadCount), quadCount * 6);
        }

        public void SetUp_Incremental(ushort[] src, int increment, int repeat)
        {
            Assert.IsTrue(repeat > 0);

            var count = src.Length;
            var data = SetUp(count * repeat);

            // Copy initial indices.
            Array.Copy(src, 0, data, 0, count);

            // Copy indices for each repeat.
            var indexPtr = count;
            var toAdd = increment;
            for (var j = 0; j < repeat - 1; j++, toAdd += increment)
            for (var i = 0; i < count; i++)
                data[indexPtr++] = (ushort) (src[i] + toAdd);
        }

        static readonly List<ushort> _meshIndexBuf = new();

        public void SetUp(Mesh mesh)
        {
            Assert.AreEqual(IndexFormat.UInt16, mesh.indexFormat);
            _meshIndexBuf.Clear();
            mesh.GetIndices(_meshIndexBuf, 0);
            var data = SetUp(_meshIndexBuf.Count);
            _meshIndexBuf.CopyTo(data);
        }

        protected override void Internal_FillMesh(Mesh mesh)
        {
            mesh.SetIndices(Data, 0, Count, MeshTopology.Triangles, 0, false);
        }
    }
}