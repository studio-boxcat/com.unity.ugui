// ReSharper disable InconsistentNaming

#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    ///   Registry which maps a Graphic to the canvas it belongs to.
    /// </summary>
    internal static class RaycastableRegistry
    {
        private static readonly Dictionary<int, HashSet<Graphic>> _dict = new();

        public static void Register(Canvas canvas, Graphic graphic)
        {
            Assert.IsTrue(graphic is { isActiveAndEnabled: true, raycastTarget: true });

            var hashCode = canvas.GetHashCode();

            if (_dict.TryGetValue(hashCode, out var graphics) == false)
            {
                graphics = new HashSet<Graphic>(RefComparer.Instance);
                _dict.Add(hashCode, graphics);
            }

            graphics.Add(graphic);
        }

        public static void Unregister(Canvas canvas, Graphic graphic)
        {
            var hashCode = canvas.GetHashCode();

            if (_dict.TryGetValue(hashCode, out var graphics) == false)
                return;

            if (graphics.Remove(graphic) && graphics.Count == 0)
                _dict.Remove(hashCode);
        }

        public static bool TryGetForCanvas(Canvas canvas, out ICollection<Graphic>? graphics)
        {
            if (_dict.TryGetValue(canvas.GetHashCode(), out var graphicsSet))
            {
                graphics = graphicsSet;
                return true;
            }
            else
            {
                graphics = null;
                return false;
            }
        }
    }
}