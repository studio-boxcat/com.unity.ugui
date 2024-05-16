using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class TextGeneratorPool
    {
        static readonly Stack<TextGenerator> _pool = new();

        public static TextGenerator Rent()
        {
            if (_pool.TryPop(out var instance))
                return instance;

#if DEBUG
            _debugCreationCount++;
            L.I("[TextGeneratorPool] Created a new instance: " + _debugCreationCount);
#endif
            return new TextGenerator();
        }

        public static void Return(TextGenerator instance)
        {
            if (_pool.Count > 100)
            {
                L.W("[TextGeneratorPool] Pool is too large. There might be a leak.");
                return;
            }


            // Reset m_CachedXXX fields to false.
            instance.PopulateWithErrors(null, default, null);
            // Make sure next time we use it, it will be re-populated.
            instance.Invalidate();

            _pool.Push(instance);
        }

#if DEBUG
        static int _debugCreationCount = 0;
#endif

#if UNITY_EDITOR
        static TextGeneratorPool()
        {
            // Clear the pool when the play mode changes.
            UnityEditor.EditorApplication.playModeStateChanged += _ =>
            {
                _debugCreationCount = 0;
                _pool.Clear();
            };
        }
#endif
    }
}