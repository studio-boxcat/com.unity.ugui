using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class MeshBuilderPool
    {
        private static readonly List<MeshBuilder> _pool = new();

        public static MeshBuilder Rent()
        {
            if (_pool.Count > 0)
            {
                var last = _pool.Count - 1;
                var result = _pool[last];
                _pool.RemoveAt(last);
                return result;
            }

            L.I("[UGUI] Creating new MeshBuilder.");
            return new MeshBuilder();
        }

        public static Scope Rent(out MeshBuilder mb)
        {
            mb = Rent();
            return new Scope(mb);
        }

        public static void Return(MeshBuilder mb)
        {
            Assert.IsFalse(_pool.Contains(mb), "MeshBuilder is already in the pool. It must be invalidated before returning to the pool.");
            Assert.AreEqual(MeshBuilder.Invalid, mb.Poses.Count, "MeshBuilder must be invalidated before returning to the pool.");
            Assert.AreEqual(MeshBuilder.Invalid, mb.Indices.Count, "MeshBuilder must be invalidated before returning to the pool.");
            _pool.Add(mb);
        }

        public readonly struct Scope : IDisposable
        {
            private readonly MeshBuilder _mb;

            public Scope(MeshBuilder mb)
            {
                _mb = mb;
            }

            public void Dispose()
            {
                Return(_mb);
            }
        }
    }
}