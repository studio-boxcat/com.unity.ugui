using System.Collections.Generic;

namespace UnityEngine.UI
{
    class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        bool IEqualityComparer<T>.Equals(T x, T y) => ReferenceEquals(x, y);
        int IEqualityComparer<T>.GetHashCode(T obj) => obj.GetHashCode();
    }

    static class ReferenceEqualityComparer
    {
        public static readonly ReferenceEqualityComparer<Object> Object = new();
    }
}