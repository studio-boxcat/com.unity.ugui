using JetBrains.Annotations;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility functions for querying layout elements for their minimum, preferred, and flexible sizes.
    /// </summary>
    public static class LayoutUtility
    {
        /// <summary>
        /// Returns the minimum size of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.</remarks>
        public static float GetMinSize(RectTransform rect, int axis)
        {
            return axis == 0 ? GetMinWidth(rect) : GetMinHeight(rect);
        }

        /// <summary>
        /// Returns the preferred size of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredSize([NotNull] RectTransform rect, int axis)
        {
            return axis == 0 ? GetPreferredWidth(rect) : GetPreferredHeight(rect);
        }

        /// <summary>
        /// Returns the flexible size of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <param name="axis">The axis to query. This can be 0 or 1.</param>
        public static float GetFlexibleSize([NotNull] RectTransform rect, int axis)
        {
            return axis == 0 ? GetFlexibleWidth(rect) : GetFlexibleHeight(rect);
        }

        /// <summary>
        /// Returns the minimum width of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinWidth([NotNull] RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minWidth, 0);
        }

        /// <summary>
        /// Returns the preferred width of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <returns>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </returns>
        public static float GetPreferredWidth(RectTransform rect)
        {
            return Mathf.Max(GetLayoutProperty(rect, e => e.minWidth, 0), GetLayoutProperty(rect, e => e.preferredWidth, 0));
        }

        /// <summary>
        /// Returns the flexible width of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleWidth([NotNull] RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleWidth, 0);
        }

        /// <summary>
        /// Returns the minimum height of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetMinHeight([NotNull] RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.minHeight, 0);
        }

        /// <summary>
        /// Returns the preferred height of the layout element.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        public static float GetPreferredHeight([NotNull] RectTransform rect)
        {
            return Mathf.Max(GetLayoutProperty(rect, e => e.minHeight, 0), GetLayoutProperty(rect, e => e.preferredHeight, 0));
        }

        /// <summary>
        /// Returns the flexible height of the layout element.
        /// </summary>
        /// <remarks>
        /// All components on the GameObject that implement the ILayoutElement are queried. The one with the highest priority which has a value for this setting is used. If multiple componets have this setting and have the same priority, the maximum value out of those is used.
        /// </remarks>
        /// <param name="rect">The RectTransform of the layout element to query.</param>
        public static float GetFlexibleHeight([NotNull] RectTransform rect)
        {
            return GetLayoutProperty(rect, e => e.flexibleHeight, 0);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <returns>The calculated value of the layout property.</returns>
        static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue)
        {
            return GetLayoutProperty(rect, property, defaultValue, out _);
        }

        /// <summary>
        /// Gets a calculated layout property for the layout element with the given RectTransform.
        /// </summary>
        /// <param name="rect">The RectTransform of the layout element to get a property for.</param>
        /// <param name="property">The property to calculate.</param>
        /// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
        /// <param name="source">Optional out parameter to get the component that supplied the calculated value.</param>
        /// <returns>The calculated value of the layout property.</returns>
        public static float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property, float defaultValue, out ILayoutElement source)
        {
            source = null;
            if (rect == null)
                return defaultValue;
            var value = defaultValue;
            var maxPriority = int.MinValue;

            using var _ = CompBuf.GetComponents(rect, typeof(ILayoutElement), out var components);

            foreach (ILayoutElement layoutComp in components)
            {
                if (layoutComp is Behaviour {isActiveAndEnabled: false})
                    continue;

                int priority = layoutComp.layoutPriority;
                // If this layout components has lower priority than a previously used, ignore it.
                if (priority < maxPriority)
                    continue;
                float curValue = property(layoutComp);
                // If this layout property is set to a negative value, it means it should be ignored.
                if (curValue < 0)
                    continue;

                // If this layout component has higher priority than all previous ones,
                // overwrite with this one's value.
                if (priority > maxPriority)
                {
                    value = curValue;
                    maxPriority = priority;
                    source = layoutComp;
                }
                // We already checked priority < maxPriority (false) && priority > maxPriority (false), so priority == maxPriority here.
                // If the layout component has the same priority as a previously used,
                // use the largest of the values with the same priority.
                else if (curValue > value)
                {
                    value = curValue;
                    source = layoutComp;
                }
            }

            return value;
        }
    }
}
