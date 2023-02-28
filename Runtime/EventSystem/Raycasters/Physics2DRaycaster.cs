namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// </summary>
    [AddComponentMenu("Event/Physics 2D Raycaster")]
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Raycaster for casting against 2D Physics components.
    /// </summary>
    public class Physics2DRaycaster : PhysicsRaycaster
    {
        /// <summary>
        /// Raycast against 2D elements in the scene.
        /// </summary>
        public override bool Raycast(Vector2 screenPosition, out RaycastResult result)
        {
#if PACKAGE_PHYSICS2D
            if (!ComputeRayAndDistance(screenPosition, out var ray, out var distance))
            {
                result = default;
                return false;
            }

            var hit = Physics2D.Raycast(ray.origin, ray.direction, distance, finalEventMask);
            if (hit.collider is null)
            {
                result = default;
                return false;
            }

            result = new RaycastResult
            {
                gameObject = hit.collider.gameObject,
                module = this,
                screenPosition = screenPosition,
            };
            return true;
#endif
        }
    }
}
