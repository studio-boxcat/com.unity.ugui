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
        public Camera camera;

        /// <summary>
        /// The screen position from which the raycast was generated.
        /// </summary>
        public Vector2 screenPosition;


        public RaycastResult(Component collider, Camera camera, Vector2 screenPosition)
        {
            this.gameObject = collider?.gameObject;
            this.collider = collider;
            this.camera = camera;
            this.screenPosition = screenPosition;
        }

        public override string ToString()
        {
            if (gameObject == null || camera == null)
                return "";

            return "Name: " + gameObject.name + "\n" +
                   "camera: " + camera + "\n" +
                   "screenPosition: " + screenPosition;
        }
    }
}