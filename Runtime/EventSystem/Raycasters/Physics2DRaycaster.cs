#if PACKAGE_PHYSICS2D
namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Raycaster for casting against 2D Physics components.
    /// </summary>
    [AddComponentMenu("Event/Physics 2D Raycaster")]
    [RequireComponent(typeof(Camera))]
    public class Physics2DRaycaster : PhysicsRaycaster
    {
        /// <summary>
        /// Raycast against 2D elements in the scene.
        /// </summary>
        public override RaycastResultType Raycast(Vector2 screenPosition, out RaycastResult result)
        {
            var camera = eventCamera; // Cache the camera to prevent multiple property accesses.
            var ray = camera.ScreenPointToRay(screenPosition);
            var distance = camera.farClipPlane - camera.nearClipPlane;

            var hit = Physics2D.Raycast(ray.origin, ray.direction, distance, finalEventMask);
            if (hit.collider is null)
            {
                result = default;
                return RaycastResultType.Miss;
            }

            result = new RaycastResult
            {
                gameObject = hit.collider.gameObject,
                camera = camera,
                screenPosition = screenPosition,
            };
            return RaycastResultType.Hit;
        }
    }
}
#endif