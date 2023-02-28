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

        public Component collider;

        /// <summary>
        /// BaseRaycaster that raised the hit.
        /// </summary>
        public BaseRaycaster module;

        /// <summary>
        /// The screen position from which the raycast was generated.
        /// </summary>
        public Vector2 screenPosition;

        public RaycastResult(Component collider, BaseRaycaster module, Vector2 screenPosition)
        {
            this.gameObject = collider?.gameObject;
            this.collider = collider;
            this.module = module;
            this.screenPosition = screenPosition;
        }

        public override string ToString()
        {
            if (module == null || collider == null)
                return "";

            return "Name: " + collider.name + "\n" +
                   "module: " + module + "\n" +
                   "screenPosition: " + screenPosition;
        }
    }
}