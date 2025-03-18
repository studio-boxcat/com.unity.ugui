using System;
using System.Diagnostics;

namespace UnityEngine
{
    internal static class L
    {
        [Conditional("DEBUG")]
        public static void I(string message, Object context = null) => Debug.Log(message, context);
        [Conditional("DEBUG")]
        public static void W(string message, Object context = null) => Debug.LogWarning(message, context);
        public static void E(string message, Object context = null) => Debug.LogError(message, context);
        public static void E(Exception exception, Object context = null) => Debug.LogException(exception, context);
    }
}