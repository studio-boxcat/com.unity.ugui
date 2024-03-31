using System.Collections.Generic;

namespace UnityEngine.UI
{
    /// <summary>
    /// Registry class to keep track of all IClippers that exist in the scene
    /// </summary>
    /// <remarks>
    /// This is used during the CanvasUpdate loop to cull clippable elements. The clipping is called after layout, but before Graphic update.
    /// </remarks>
    public static class ClipperRegistry
    {
        static readonly HashSet<RectMask2D> _clippers = new(ReferenceEqualityComparer.Object);

        public static void Register(RectMask2D c) => _clippers.Add(c);
        public static void Unregister(RectMask2D c) => _clippers.Remove(c);

        /// <summary>
        /// Perform the clipping on all registered IClipper
        /// </summary>
        public static void Cull()
        {
            foreach (var clipper in _clippers)
                clipper.PerformClipping();
        }
    }
}