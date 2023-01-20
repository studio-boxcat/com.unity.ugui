using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A hit result from a BaseRaycaster.
    /// </summary>
    public struct RaycastResult
    {
        /// <summary>
        /// The GameObject that was hit by the raycast.
        /// </summary>
        public GameObject gameObject;

        public Graphic graphic;

        /// <summary>
        /// BaseRaycaster that raised the hit.
        /// </summary>
        public GraphicRaycaster module;

        /// <summary>
        /// The display index from which the raycast was generated.
        /// </summary>
        public int displayIndex;

        /// <summary>
        /// The screen position from which the raycast was generated.
        /// </summary>
        public Vector2 screenPosition;

        /// <summary>
        /// Is there an associated module and a hit GameObject.
        /// </summary>
        public bool isValid
        {
            get { return module != null && graphic != null; }
        }

        public RaycastResult(Graphic graphic, GraphicRaycaster module, int displayIndex, Vector2 screenPosition)
        {
            this.gameObject = graphic?.gameObject;
            this.graphic = graphic;
            this.module = module;
            this.displayIndex = displayIndex;
            this.screenPosition = screenPosition;
        }

        public override string ToString()
        {
            if (!isValid)
                return "";

            return "Name: " + graphic.name + "\n" +
                "module: " + module + "\n" +
                "screenPosition: " + screenPosition + "\n" +
                "module.sortOrderPriority: " + module.sortOrderPriority + "\n" +
                "module.renderOrderPriority: " + module.renderOrderPriority;
        }
    }
}
