using System.Collections.Generic;

namespace UnityEngine
{
    public static class ExtensionMethods
    {
        public static int IndexOfRef<T>(this List<T> list, T obj) where T : class
        {
            for (var index = 0; index < list.Count; index++) // for better performance.
            {
                if (ReferenceEquals(list[index], obj))
                    return index;
            }
            return -1;
        }

        public static bool ContainsRef<T>(this List<T> list, T obj) where T : class
        {
            return list.IndexOfRef(obj) is not -1;
        }

        public static bool RemoveSingleRef<T>(this List<T> list, T obj) where T : class
        {
            var index = list.IndexOfRef(obj);
            if (index is -1) return false;
            list.RemoveAt(index);
            return true;
        }
    }
}