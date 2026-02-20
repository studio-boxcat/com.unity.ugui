#nullable enable

using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public enum Axis : byte
    {
        X = 0,
        Y = 1
    }

    /// <summary>
    /// Utility functions for querying layout elements for their minimum, preferred, and flexible sizes.
    /// </summary>
    public static class LayoutUtility
    {
        public static int Idx(this Axis axis) => (int) axis;
        public static bool IsX(this Axis axis) => axis == Axis.X;
        public static bool IsY(this Axis axis) => axis == Axis.Y;

        public static bool Select(this Axis axis, bool x, bool y) => axis.IsX() ? x : y;
        public static int Select(this Axis axis, int x, int y) => axis.IsX() ? x : y;
        public static float Select(this Axis axis, float x, float y) => axis.IsX() ? x : y;
        public static int SelectHorizontalOrVertical(this Axis axis, RectOffset r) => axis.IsX() ? r.horizontal : r.vertical;

        public static float CalcPreferredSize(RectTransform rect, Axis axis) => axis.IsX() ? CalcPreferredWidth(rect) : CalcPreferredHeight(rect);

        public static float CalcPreferredWidth(RectTransform rect)
            => ResolveLayoutElement<ILayoutElementH>(rect)?.preferredWidth ?? 0;

        public static float CalcPreferredHeight(RectTransform rect)
            => ResolveLayoutElement<ILayoutElementV>(rect)?.preferredHeight ?? 0;

        /// <summary>
        /// Resolves the highest-priority enabled layout element from a RectTransform.
        /// Returns null if no enabled element is found.
        /// </summary>
        public static T? ResolveLayoutElement<T>(RectTransform rect) where T : class, ILayoutPriority
        {
            using var _ = CompBuf.GetComponents(rect, typeof(T), out var components);
            var count = components.Count;
            if (count is 0) return null;
            var first = (T) (object) components[0];
            if (count is 1) return first is Behaviour { enabled: false } ? null : first;

            T? result = null;
            var maxPriority = -1;
            for (var i = 0; i < count; i++)
            {
                var elem = (T) (object) components[i];
                if (elem is Behaviour { enabled: false }) continue; // check enabled, not isActiveAndEnabled
                var priority = elem.layoutPriority;
                Assert.IsFalse(priority < 0, "layoutPriority must not be negative");
                if (priority <= maxPriority) continue;
                result = elem;
                maxPriority = priority;
            }
            return result;
        }
    }
}