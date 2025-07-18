#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// EditorOnly class for tracking all Graphics.
    /// Used when a source asset is reimported into the editor to ensure that Graphics are updated as intended.
    /// </summary>
    public static class GraphicRebuildTracker
    {
        private static readonly HashSet<Graphic> _tracked = new(RefComparer.Instance);

        /// <summary>
        /// Add a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to track</param>
        public static void TrackGraphic(Graphic g)
        {
            if (_tracked.IsEmpty())
                CanvasRenderer.onRequestRebuild += OnRebuildRequested;
            var added = _tracked.Add(g);
            Assert.IsTrue(added, "Graphic was already tracked: " + g.SafeName());
        }

        /// <summary>
        /// Remove a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to remove from tracking.</param>
        public static void UnTrackGraphic(Graphic g)
        {
            var removed = _tracked.Remove(g);
            Assert.IsTrue(removed, "Graphic was not tracked: " + g.SafeName());
            if (_tracked.IsEmpty())
                CanvasRenderer.onRequestRebuild -= OnRebuildRequested;
        }

        private static void OnRebuildRequested()
        {
            foreach (var graphic in _tracked)
                graphic.OnRebuildRequested();
        }
    }
}
#endif // if UNITY_EDITOR