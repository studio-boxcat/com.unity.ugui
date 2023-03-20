using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class TextGeneratorPool
    {
        static readonly Stack<TextGenerator> _pool = new();

        public static TextGenerator Rent()
        {
#if UNITY_EDITOR
            if (_pool.Count > 100)
                Debug.LogError("TextGeneratorPool is too big. Something is wrong.");
#endif

            return _pool.TryPop(out var instance)
                ? instance : new TextGenerator();
        }

        public static void Return(TextGenerator instance)
        {
            // Reset m_CachedXXX fields to false.
            instance.PopulateWithErrors(null, default, null);
            // Make sure next time we use it, it will be re-populated.
            instance.Invalidate();

            _pool.Push(instance);
        }
    }
}