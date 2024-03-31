#if UNITY_EDITOR
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// EditorOnly class for tracking all Graphics.
    /// Used when a source asset is reimported into the editor to ensure that Graphics are updated as intended.
    /// </summary>
    public static class GraphicRebuildTracker
    {
        static IndexedSet<Graphic> m_Tracked = new();
        static bool s_Initialized;

        /// <summary>
        /// Add a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to track</param>
        public static void TrackGraphic(Graphic g)
        {
            if (!s_Initialized)
            {
                CanvasRenderer.onRequestRebuild += OnRebuildRequested;
                s_Initialized = true;
            }

            m_Tracked.Add(g);
        }

        /// <summary>
        /// Remove a Graphic to the list of tracked Graphics
        /// </summary>
        /// <param name="g">The graphic to remove from tracking.</param>
        public static void UnTrackGraphic(Graphic g)
        {
            m_Tracked.Remove(g);
        }

        static void OnRebuildRequested()
        {
            foreach (var graphic in m_Tracked)
                graphic.OnRebuildRequested();
        }
    }
}
#endif // if UNITY_EDITOR
