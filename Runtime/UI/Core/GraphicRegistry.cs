using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    ///   Registry which maps a Graphic to the canvas it belongs to.
    /// </summary>
    public class GraphicRegistry
    {
        private static GraphicRegistry s_Instance;

        private readonly CanvasDictionary m_RaycastableGraphics = new(8);

        /// <summary>
        /// The singleton instance of the GraphicRegistry. Creates a new instance if it does not exist.
        /// </summary>
        public static GraphicRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new GraphicRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// Associates a raycastable Graphic with a Canvas and stores this association in the registry.
        /// </summary>
        /// <param name="c">The canvas being associated with the Graphic.</param>
        /// <param name="graphic">The Graphic being associated with the Canvas.</param>
        public static void RegisterRaycastGraphicForCanvas(Canvas c, [NotNull] Graphic graphic)
        {
            Assert.IsTrue(graphic.isActiveAndEnabled && graphic.raycastTarget);
            instance.m_RaycastableGraphics.Add(c, graphic);
        }

        /// <summary>
        /// Dissociates a Graphic from a Canvas, removing this association from the registry.
        /// </summary>
        /// <param name="c">The Canvas to dissociate from the Graphic.</param>
        /// <param name="graphic">The Graphic to dissociate from the Canvas.</param>
        public static void UnregisterRaycastGraphicForCanvas(Canvas c, [NotNull] Graphic graphic)
        {
            instance.m_RaycastableGraphics.Remove(c, graphic);
        }

        /// <summary>
        /// Retrieves the list of Graphics that are raycastable and associated with a Canvas.
        /// </summary>
        /// <param name="canvas">The Canvas to search</param>
        /// <returns>Returns a list of Graphics. Returns an empty list if no Graphics are associated with the specified Canvas.</returns>
        public static bool TryGetRaycastableGraphicsForCanvas(Canvas canvas, out IndexedSet<Graphic> graphics)
        {
            return instance.m_RaycastableGraphics.TryGetValue(canvas, out graphics);
        }

        readonly struct CanvasDictionary
        {
            readonly Dictionary<int, IndexedSet<Graphic>> _dict;

            public CanvasDictionary(int capacity)
            {
                _dict = new Dictionary<int, IndexedSet<Graphic>>(capacity);
            }

            public bool TryGetValue(Canvas canvas, out IndexedSet<Graphic> graphics)
            {
                return _dict.TryGetValue(canvas.GetHashCode(), out graphics);
            }

            public void Add(Canvas canvas, Graphic graphic)
            {
                var hashCode = canvas.GetHashCode();

                if (_dict.TryGetValue(hashCode, out var graphics) == false)
                {
                    graphics = new IndexedSet<Graphic>();
                    _dict.Add(hashCode, graphics);
                }

                graphics.Add(graphic);
            }

            public void Remove(Canvas canvas, Graphic graphic)
            {
                var hashCode = canvas.GetHashCode();

                if (_dict.TryGetValue(hashCode, out var graphics) == false)
                    return;

                if (graphics.TryRemove(graphic) && graphics.Count == 0)
                    _dict.Remove(hashCode);
            }
        }
    }
}