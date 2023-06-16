using System.Collections.Generic;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Registry class to keep track of all IClippers that exist in the scene
    /// </summary>
    /// <remarks>
    /// This is used during the CanvasUpdate loop to cull clippable elements. The clipping is called after layout, but before Graphic update.
    /// </remarks>
    public class ClipperRegistry
    {
        static ClipperRegistry s_Instance;

        readonly IndexedSet<IClipper> m_Clippers = new();

        /// <summary>
        /// The singleton instance of the clipper registry.
        /// </summary>
        public static ClipperRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new ClipperRegistry();
                return s_Instance;
            }
        }

        /// <summary>
        /// Perform the clipping on all registered IClipper
        /// </summary>
        public void Cull()
        {
            foreach (var clipper in m_Clippers)
                clipper.PerformClipping();
        }

        /// <summary>
        /// Register a unique IClipper element
        /// </summary>
        /// <param name="c">The clipper element to add</param>
        public static void Register(IClipper c)
        {
            if (c == null)
                return;
            instance.m_Clippers.Add(c);
        }

        /// <summary>
        /// UnRegister a IClipper element
        /// </summary>
        /// <param name="c">The Element to try and remove.</param>
        public static void Unregister(IClipper c)
        {
            instance.m_Clippers.Remove(c);
        }
    }
}
