#nullable enable
namespace UnityEngine.UI
{
    /// <summary>
    /// Mask related utility class. This class provides masking-specific utility functions.
    /// </summary>
    public static class MaskUtilities
    {
        private static readonly ListPool<IMaskable> _maskablePool = new();

        /// <summary>
        /// Notify all IMaskable under the given component that they need to recalculate masking.
        /// </summary>
        /// <param name="root">The object thats changed for whose children should be notified.</param>
        public static void NotifyStencilStateChanged(Component root)
        {
            using var _ = _maskablePool.Rent(out var comps);
            root.GetComponentsInChildren(comps);
            var rootGO = root.gameObject;
            foreach (var comp in comps)
            {
                if (rootGO.RefEq(comp.gameObject)) continue; // skip self.
                comp.RecalculateMasking();
            }
        }

        /// <summary>
        /// Find the stencil depth for a given element.
        /// </summary>
        /// <param name="transform">The starting transform to search.</param>
        /// <returns>What the proper stencil buffer index should be. 0 means no mask in the parents.</returns>
        public static byte GetStencilDepth(Transform transform)
        {
            if (CanvasUtils.IsRenderRoot(transform))
                return 0;

            var t = transform.parent;
            byte depth = 0;

            while (t is not null)
            {
                if (GetEffectiveMask(t, out _)) ++depth; // increment depth if we found an effective mask.
                if (CanvasUtils.IsRenderRoot(t)) break; // stop climbing if we reach a render root.
                t = t.parent;
            }

            return depth;
        }

        public static Mask? GetEligibleMask(Transform transform)
        {
            if (CanvasUtils.IsRenderRoot(transform))
                return null;

            var t = transform.parent;

            while (t is not null)
            {
                if (GetEffectiveMask(t, out var mask)) return mask; // return the first eligible mask.
                if (CanvasUtils.IsRenderRoot(t)) break; // stop climbing if we reach a render root.
                t = t.parent;
            }

            return null;
        }

        private static bool GetEffectiveMask(Component c, out Mask mask)
        {
            if (c.TryGetComponent<Mask>(out var m) && m.enabled && m.graphic.enabled)
            {
                mask = m;
                return true;
            }
            else
            {
                mask = null!; // never use this value.
                return false;
            }
        }
    }
}