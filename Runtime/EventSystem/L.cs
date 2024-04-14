using System.Diagnostics;

namespace UnityEngine.EventSystems
{
    static class L
    {
        [Conditional("DEBUG")]
        public static void I(string message, Object context = null)
        {
            Debug.Log(message, context);
        }

        [Conditional("DEBUG")]
        public static void W(string message, Object context = null)
        {
            Debug.LogWarning(message, context);
        }

        public static void E(string message, Object context = null)
        {
            Debug.LogError(message, context);
        }
    }
}