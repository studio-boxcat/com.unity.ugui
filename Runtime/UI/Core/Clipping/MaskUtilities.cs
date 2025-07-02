#nullable enable
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Mask related utility class. This class provides masking-specific utility functions.
    /// </summary>
    public class MaskUtilities
    {
        /// <summary>
        /// Notify all IClippables under the given component that they need to recalculate clipping.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<IClippable>.Get();
            mask.GetComponentsInChildren(components);
            var maskGO = mask.gameObject;
            foreach (var comp in components)
            {
                if (ReferenceEquals(comp.gameObject, maskGO)) continue;
                comp.RecalculateClipping();
            }
            ListPool<IClippable>.Release(components);
        }

        /// <summary>
        /// Notify all IMaskable under the given component that they need to recalculate masking.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<IMaskable>.Get();
            mask.GetComponentsInChildren(components);
            var maskGO = mask.gameObject;
            foreach (var comp in components)
            {
                if (ReferenceEquals(comp.gameObject, maskGO)) continue;
                comp.RecalculateMasking();
            }
            ListPool<IMaskable>.Release(components);
        }

        /// <summary>
        /// Find the stencil depth for a given element.
        /// </summary>
        /// <param name="transform">The starting transform to search.</param>
        /// <returns>What the proper stencil buffer index should be.</returns>
        public static int GetStencilDepth(Transform transform)
        {
            if (ShouldStopSearchingMask(transform))
                return 0;

            var t = transform.parent;
            var depth = 0;

            while (t is not null)
            {
                if (t.TryGetComponent<Mask>(out var mask) && mask.enabled && mask.graphic.enabled)
                    ++depth;
                if (ShouldStopSearchingMask(t))
                    break;
                t = t.parent;
            }

            return depth;
        }

        public static Mask? GetEligibleMask(Transform transform)
        {
            if (ShouldStopSearchingMask(transform))
                return null;

            var t = transform.parent;

            while (t is not null)
            {
                if (t.TryGetComponent<Mask>(out var mask) && mask.enabled && mask.graphic.enabled)
                    return mask;
                if (ShouldStopSearchingMask(t))
                    break;
                t = t.parent;
            }

            return null;
        }

        static bool ShouldStopSearchingMask(Component c)
        {
            // Stop if we find a canvas with override sorting or root canvas.
            return c.TryGetComponent(out Canvas canvas)
                   && ((canvas.enabled && canvas.overrideSorting) || canvas.isRootCanvas);
        }

        /// <summary>
        /// Find the correct RectMask2D for a given IClippable.
        /// </summary>
        /// <param name="clippable">Clippable to search from.</param>
        /// <returns>The Correct RectMask2D</returns>
        public static RectMask2D GetRectMaskForClippable(MaskableGraphic clippable)
        {
            var t = clippable.transform;

            // Handle most common cases.
            {
                // No mask at all.
                var mask = t.GetComponentInParent<RectMask2D>(true); // Do not skip inactive.
                if (mask is null) return null;

                // There is a enabled mask, and it's located on the same canvas with the graphic.
                if (mask.enabled && ReferenceEquals(mask.Canvas, clippable.canvas))
                    return mask;
            }

            // Going up the hierarchy to find first mask.
            do
            {
                if (t.TryGetComponent(out RectMask2D mask) && mask.enabled)
                    return mask;
                // Stop if we find a canvas with override sorting or root canvas.
                if (t.TryGetComponent(out Canvas canvas) && ((canvas.enabled && canvas.overrideSorting) || canvas.isRootCanvas))
                    return null;
                t = t.parent;
            } while (t is not null);

            return null;
        }
    }
}