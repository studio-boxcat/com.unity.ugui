using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class CombineInstancePool
    {
        static readonly Dictionary<int, CombineInstance[]> s_Pool = new();

        // No return method as this CombineInstance[] only used temporarily.
        public static CombineInstance[] Get(int count)
        {
            if (!s_Pool.TryGetValue(count, out var dst))
            {
                dst = new CombineInstance[count];
                s_Pool.Add(count, dst);
            }

            return dst;
        }
    }
}