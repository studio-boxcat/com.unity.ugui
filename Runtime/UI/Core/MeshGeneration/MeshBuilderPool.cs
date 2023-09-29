using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class MeshBuilderPool
    {
        static readonly List<MeshBuilder> _pool = new();

        public static MeshBuilder Rent()
        {
            if (_pool.Count > 0)
            {
                var last = _pool.Count - 1;
                var result = _pool[last];
                _pool.RemoveAt(last);
                return result;
            }

            return new MeshBuilder();
        }

        public static Scope Rent(out MeshBuilder mb)
        {
            mb = Rent();
            return new Scope(mb);
        }

        public static void Return(MeshBuilder mb)
        {
            _pool.Add(mb);
        }

        public readonly struct Scope : IDisposable
        {
            readonly MeshBuilder _mb;

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