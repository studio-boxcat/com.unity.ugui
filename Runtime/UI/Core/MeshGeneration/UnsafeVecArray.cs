#nullable enable
using System.Runtime.CompilerServices;

namespace UnityEngine.UI
{
    // Raw view over a packed float channel buffer (returned by the channels' *Unsafe accessors):
    // base pointer + per-vert stride + vert count. Same GC contract as MeshChannel.SetUpUnsafeCore.
    // The lane passes touch each float exactly once; builders take Ptr for cursor writes instead.
    public readonly unsafe ref struct UnsafeVecArray
    {
        public readonly float* Ptr;
        public readonly int Stride; // floats per vert
        public readonly int Count;  // verts

        public UnsafeVecArray(float* ptr, int stride, int count)
        {
            Ptr = ptr;
            Stride = stride;
            Count = count;
        }

        // lane += offset
        public void OffsetX(float offset) => Offset(0, offset);
        public void OffsetY(float offset) => Offset(1, offset);

        // lane ×= scale
        public void ScaleX(float scale) => Scale(0, scale);
        public void ScaleY(float scale) => Scale(1, scale);

        // lane = lane × scale + offset (scale = −1 mirrors about offset/2)
        public void ScaleOffsetX(float scale, float offset) => ScaleOffset(0, scale, offset);
        public void ScaleOffsetY(float scale, float offset) => ScaleOffset(1, scale, offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Offset(int lane, float offset)
        {
            var p = Ptr + lane;
            for (var i = 0; i < Count; ++i, p += Stride)
                *p += offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Scale(int lane, float scale)
        {
            var p = Ptr + lane;
            for (var i = 0; i < Count; ++i, p += Stride)
                *p *= scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ScaleOffset(int lane, float scale, float offset)
        {
            var p = Ptr + lane;
            for (var i = 0; i < Count; ++i, p += Stride)
                *p = *p * scale + offset;
        }
    }
}
