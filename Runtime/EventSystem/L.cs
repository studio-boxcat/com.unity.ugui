using System.Diagnostics;

namespace UnityEngine.EventSystems
{
    public static class L
    {
        [Conditional("DEBUG")]
        public static void W(string message, Object context = null)
        {
            Debug.LogWarning(message, context);
        }
    }
}