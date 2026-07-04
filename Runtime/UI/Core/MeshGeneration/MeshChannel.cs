#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public abstract class MeshChannel<T> where T : struct
    {
        protected T[]? Data { get; private set; }
        public int Count { get; private set; }

        private T[] _buf;


        protected MeshChannel(int capacity)
        {
            Count = MeshBuilder.Invalid;
            _buf = new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before accessing data.");
                Assert.IsTrue(index >= 0 && index < Count);
                return Data![index];
            }
        }

        [MustUseReturnValue]
        public T[] SetUp(int count)
        {
            Assert.AreEqual(MeshBuilder.Invalid, Count, "MeshChannel is not properly invalidated.");
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");
            Assert.IsTrue(count >= 0);

            if (_buf.Length < count)
                _buf = new T[count];
            Data = _buf;
            Count = count;
            return _buf;
        }

        public void SetUp(T[] data)
        {
            Assert.AreEqual(MeshBuilder.Invalid, Count, "MeshChannel is not properly invalidated.");
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");

            Data = data;
            Count = data.Length;
        }

        public void SetUp(T[] data, int count)
        {
            Assert.AreEqual(MeshBuilder.Invalid, Count, "MeshChannel is not properly invalidated.");
            Assert.IsNull(Data, "MeshChannel.SetUp() must be called only once.");
            Assert.IsTrue(count >= 0 && count <= data.Length);

            Data = data;
            Count = count;
        }

        [MustUseReturnValue]
        public Span<T> Edit()
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before editing.");

            // If the data is already writable, return it.
            if (ReferenceEquals(_buf, Data!))
                return _buf.AsSpan(0, Count);

            // Otherwise, copy the data to a writable array.
            if (_buf.Length < Count)
                _buf = new T[Count];
            Array.Copy(Data!, 0, _buf, 0, Count);
            Data = _buf;
            return _buf.AsSpan(0, Count);
        }

        public T[] Resize(int count)
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before resizing.");
            Assert.IsTrue(count >= 0);

            // When resizing is not necessary.
            if (_buf.Length >= count)
            {
                // Nothing to do when the data is already writable.
                if (ReferenceEquals(_buf, Data!))
                {
                    Count = count;
                    return _buf;
                }

                Array.Copy(Data!, 0, _buf, 0, Mathf.Min(Count, count));
            }
            // When the data is already writable.
            else if (ReferenceEquals(_buf, Data!))
            {
                Array.Resize(ref _buf, count);
            }
            // When the data is not writable.
            else
            {
                _buf = new T[count];
                Array.Copy(Data!, 0, _buf, 0, Count); // Data is smaller than _buf.
            }

            Data = _buf;
            Count = count;
            return _buf!;
        }

        // Resize to `copies` blocks of the current content, replicating it into each new block.
        // (cf. SetUp_Repeat — the modify-phase, in-place variant.)
        public T[] Resize_Repeat(int copies)
        {
            Assert.IsTrue(copies > 0);

            var count = Count;
            var data = Resize(count * copies);
            for (var b = 1; b < copies; b++)
                Array.Copy(data, 0, data, count * b, count);
            return data;
        }

        public void TrimAfter(int fromIndex)
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before trimming.");
            Assert.IsTrue(fromIndex <= Count, $"fromIndex must be less than Count: fromIndex={fromIndex}, Count={Count}");
            Count = fromIndex;
        }

        public void FillMesh(Mesh mesh)
        {
            Assert.IsNotNull(Data);
            Internal_FillMesh(mesh);
        }

        [MustDisposeResource]
        public NativeArray<T> AllocateNativeArray(Allocator allocator)
        {
            Assert.IsNotNull(Data, "MeshChannel.SetUp() must be called before allocating NativeArray.");

            var nativeArray = new NativeArray<T>(Count, allocator, NativeArrayOptions.UninitializedMemory);
            NativeArray<T>.Copy(Data!, nativeArray, Count);
            return nativeArray;
        }

        public void Invalidate()
        {
            Data = null;
            Count = MeshBuilder.Invalid;
        }

        // Raw pointer into the buffer for by-component writes. Unpinned managed pointer - valid only
        // under Unity's non-moving GC (Boehm, Mono and IL2CPP alike); consume within the current
        // build scope, never hold across allocations or frames. Breaks under a moving GC (CoreCLR).
        [MustUseReturnValue]
        protected unsafe void* SetUpUnsafeCore(int count)
        {
            var data = SetUp(count);
            return count == 0 ? null : UnsafeUtility.AddressOf(ref data[0]);
        }

        // Resize variant of SetUpUnsafeCore — existing content is kept (copy-on-write). Same GC contract.
        [MustUseReturnValue]
        protected unsafe void* ResizeUnsafeCore(int count)
        {
            var data = Resize(count);
            return count == 0 ? null : UnsafeUtility.AddressOf(ref data[0]);
        }

        // Edit variant of SetUpUnsafeCore — in-place modify of the current content (copy-on-write).
        // Same GC contract.
        [MustUseReturnValue]
        protected unsafe void* EditUnsafeCore()
        {
            _ = Edit(); // ensure the buffer is writable
            return Count == 0 ? null : UnsafeUtility.AddressOf(ref _buf[0]);
        }

        protected abstract void Internal_FillMesh(Mesh mesh);
    }

    public class MeshPosChannel : MeshChannel<Vector3>
    {
        public MeshPosChannel(int capacity) : base(capacity) { }

        public void SetUp(Vector2[] src)
        {
            var count = src.Length;
            var data = SetUp(count);
            for (var i = 0; i < count; ++i)
                data[i] = src[i];
        }

        // Raw view into the buffer (3 floats/vert) for by-axis writes. See SetUpUnsafeCore for the GC contract.
        [MustUseReturnValue]
        public unsafe UnsafeVecArray SetUpUnsafe(int vertCount) => new((float*)SetUpUnsafeCore(vertCount), 3, vertCount);

        [MustUseReturnValue]
        public unsafe UnsafeVecArray ResizeUnsafe(int vertCount) => new((float*)ResizeUnsafeCore(vertCount), 3, vertCount);

        [MustUseReturnValue]
        public unsafe UnsafeVecArray EditUnsafe() => new((float*)EditUnsafeCore(), 3, Count);

        public Rect CalculateBoundingRect()
        {
            Assert.IsNotNull(Data, "MeshPosChannel.SetUp() must be called before calculating bounding rect.");

            var xMin = float.MaxValue;
            var xMax = float.MinValue;
            var yMin = float.MaxValue;
            var yMax = float.MinValue;

            var data = Data!;
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
        public MeshUVChannel(int capacity) : base(capacity) { }

        public void SetUp(Vector2 uv, int count)
        {
            var data = SetUp(count);
            Array.Fill(data, uv, 0, count);
        }

        // Raw view into the buffer (2 floats/vert) for by-axis writes. See SetUpUnsafeCore for the GC contract.
        [MustUseReturnValue]
        public unsafe UnsafeVecArray SetUpUnsafe(int vertCount) => new((float*)SetUpUnsafeCore(vertCount), 2, vertCount);

        [MustUseReturnValue]
        public unsafe UnsafeVecArray EditUnsafe() => new((float*)EditUnsafeCore(), 2, Count);

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
        public MeshColorChannel(int capacity) : base(capacity) { }

        // Raw Color32* into the buffer for cursor writes.
        [MustUseReturnValue]
        public unsafe Color32* SetUpUnsafe(int vertCount)
        {
            var data = SetUp(vertCount);
            return vertCount == 0 ? null : (Color32*)UnsafeUtility.AddressOf(ref data[0]);
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

        public void SetUp_White(int count)
        {
            SetUp(WhiteColorCache.Opaque(count), count);
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
        public MeshIndexChannel(int capacity) : base(capacity) { }

        public void SetUp_Quad(int quadCount)
        {
            SetUp(QuadIndexCache.Get(quadCount), quadCount * 6);
        }

        // Grow by `copies` index blocks for effect copies drawn behind the original: draw block
        // c < copies references vert block c+1, the last draw block keeps vert block 0 (the original,
        // on top). Written high-to-low so block 0 stays readable as the source until its in-place offset.
        public void Resize_CopiesBehind(int copies, int vertStride)
        {
            Assert.IsTrue(copies > 0);

            var count = Count;
            var data = Resize(count * (copies + 1));
            for (var p = copies; p >= 1; --p)
            {
                var dst = p * count;
                var vertOffset = p == copies ? 0 : (p + 1) * vertStride;
                for (var k = 0; k < count; ++k)
                    data[dst + k] = (ushort) (data[k] + vertOffset);
            }
            for (var k = 0; k < count; ++k) // draw block 0 = copy 0 (vert block 1), in place
                data[k] = (ushort) (data[k] + vertStride);
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
                data[indexPtr++] = (ushort)(src[i] + toAdd);
        }

        private static readonly List<ushort> _meshIndexBuf = new();

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
