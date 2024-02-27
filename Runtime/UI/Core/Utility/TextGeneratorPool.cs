using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class TextGeneratorPool
    {
        static readonly Stack<TextGenerator> _pool = new();

        public static TextGenerator Rent()
        {
            return _pool.TryPop(out var instance)
                ? instance : new TextGenerator();
        }

        public static void Return(TextGenerator instance)
        {
            if (_pool.Count > 100)
            {
#if DEBUG
                Debug.LogWarning("TextGeneratorPool is too big. Something is wrong.");
#endif
                return;
            }


            // Reset m_CachedXXX fields to false.
            instance.PopulateWithErrors(null, default, null);
            // Make sure next time we use it, it will be re-populated.
            instance.Invalidate();

            _pool.Push(instance);
        }

#if UNITY_EDITOR
        static TextGeneratorPool()
        {
            // Clear the pool when the play mode changes.
            UnityEditor.EditorApplication.playModeStateChanged += _ => _pool.Clear();
        }
#endif
    }
}