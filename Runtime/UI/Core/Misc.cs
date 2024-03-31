using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Helper class containing generic functions used throughout the UI library.
    /// </summary>
    static class Misc
    {
        /// <summary>
        /// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
        /// </summary>
        public static void DestroyImmediate(Object obj)
        {
            Assert.IsNotNull(obj, "Object to destroy must not be null");
#if UNITY_EDITOR
            Object.DestroyImmediate(obj);
#else
            Object.Destroy(obj);
#endif
        }
    }
}