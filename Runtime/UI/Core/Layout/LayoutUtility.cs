#nullable enable

using System;

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

        public static float CalcPreferredSize(RectTransform rect, Axis axis) => GetLayoutProperty(rect, axis);
        public static float CalcPreferredWidth(RectTransform rect) => GetLayoutProperty(rect, Axis.X);
        public static float CalcPreferredHeight(RectTransform rect) => GetLayoutProperty(rect, Axis.Y);

        private static float GetLayoutProperty(RectTransform rect, Axis axis)
        {
            using var _ = CompBuf.GetComponents(rect, typeof(ILayoutElement), out var components);

            var count = components.Count;
            // no layout elements, return 0.
            if (count is 0) return 0;

            // only one layout element, return its property value.
            if (count is 1)
            {
                var layoutComp = (ILayoutElement) components[0];
                if (layoutComp is Behaviour { enabled: false }) // only check for enabled, not isActiveAndEnabled.
                    return 0f;
                return GetValue(layoutComp, axis).LB0();
            }

            var value = 0f; // default 0.
            var maxPriority = int.MinValue;
            for (var i = 0; i < count; i++)
            {
                var layoutComp = (ILayoutElement) components[i];
                if (layoutComp is Behaviour { enabled: false }) // only check for enabled, not isActiveAndEnabled.
                    continue;

                var priority = layoutComp.layoutPriority;
                // If this layout components has lower priority than a previously used, ignore it.
                if (priority < maxPriority)
                    continue;
                float curValue = GetValue(layoutComp, axis);
                // If this layout property is set to a negative value, it means it should be ignored.
                if (curValue < 0)
                    continue;

                // If this layout component has higher priority than all previous ones,
                // overwrite with this one's value.
                if (priority > maxPriority)
                {
                    value = curValue;
                    maxPriority = priority;
                }
                // We already checked priority < maxPriority (false) && priority > maxPriority (false), so priority == maxPriority here.
                // If the layout component has the same priority as a previously used,
                // use the largest of the values with the same priority.
                else if (curValue > value)
                {
                    value = curValue;
                }
            }

            return value;

            static float GetValue(ILayoutElement e, Axis axis)
            {
                return axis switch
                {
                    Axis.X => e.preferredWidth,
                    Axis.Y => e.preferredHeight,
                    _ => throw new ArgumentOutOfRangeException(nameof(axis), "Axis must be either 0 (width) or 1 (height).")
                };
            }
        }
    }
}