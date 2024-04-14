namespace UnityEngine.EventSystems
{
    public enum RaycastResultType : byte
    {
        Hit,
        Miss,
        Abort,
    }

    /// <summary>
    /// Base class for any RayCaster.
    /// </summary>
    /// <remarks>
    /// A Raycaster is responsible for raycasting against scene elements to determine if the cursor is over them. Default Raycasters include PhysicsRaycaster, Physics2DRaycaster, GraphicRaycaster.
    /// Custom raycasters can be added by extending this class.
    /// </remarks>
    public abstract class BaseRaycaster : MonoBehaviour
    {
        /// <summary>
        /// Raycast against the scene.
        /// </summary>
        public abstract RaycastResultType Raycast(Vector2 screenPosition, out RaycastResult result);

        /// <summary>
        /// The camera that will generate rays for this raycaster.
        /// </summary>
        public abstract Camera eventCamera { get; }

        public override string ToString()
        {
            return "Name: " + gameObject + "\n" +
                   "eventCamera: " + eventCamera;
        }

        private void OnEnable()
        {
            RaycasterManager.AddRaycaster(this);
        }

        private void OnDisable()
        {
            RaycasterManager.RemoveRaycasters(this);
        }
    }
}