using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace UnityEngine.UI
{
    [Flags]
    public enum GraphicPropertyFlag
    {
        Color = 1 << 0,
        Material = 1 << 1,
        Raycast = 1 << 2,
        All = Color | Material | Raycast
    }

    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class)]
    public class GraphicPropertyHideAttribute : Attribute
    {
        public readonly GraphicPropertyFlag Flags;

        public GraphicPropertyHideAttribute(GraphicPropertyFlag flags)
        {
            Flags = flags;
        }
    }

#if UNITY_EDITOR
    public static class GraphicPropertyVisible
    {
        private static readonly Dictionary<Type, GraphicPropertyFlag> _cache = new();

        public static bool IsVisible(Type type, GraphicPropertyFlag property)
        {
            if (_cache.TryGetValue(type, out var hideFlags) is false)
            {
                var attr = type.GetCustomAttribute<GraphicPropertyHideAttribute>();
                hideFlags = _cache[type] = attr?.Flags ?? default;
            }
            return (hideFlags & property) is 0;
        }
    }
#endif
}