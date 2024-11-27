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

        public static void CombineMesh(Mesh mesh, Mesh m1, Matrix4x4 t1)
        {
            var combine = Get(1);
            combine[0] = new CombineInstance { mesh = m1, transform = t1 };
            mesh.CombineMeshes(combine, true, true);
        }

        public static void CombineMesh(Mesh mesh, Mesh m1, Matrix4x4 t1, Mesh m2, Matrix4x4 t2)
        {
            var combine = Get(2);
            combine[0] = new CombineInstance { mesh = m1, transform = t1 };
            combine[1] = new CombineInstance { mesh = m2, transform = t2 };
            mesh.CombineMeshes(combine, true, true);
        }
    }
}