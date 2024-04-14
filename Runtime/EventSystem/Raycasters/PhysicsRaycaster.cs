#if PACKAGE_PHYSICS2D || PACKAGE_PHYSICS3D
using Sirenix.OdinInspector;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// Simple event system using physics raycasts.
    /// </summary>
    [AddComponentMenu("Event/Physics Raycaster")]
    [RequireComponent(typeof(Camera))]
    /// <summary>
    /// Raycaster for casting against 3D Physics components.
    /// </summary>
    public abstract class PhysicsRaycaster : BaseRaycaster
    {
        /// <summary>
        /// Const to use for clarity when no event mask is set
        /// </summary>
        protected const int kNoEventMaskSet = -1;

        [SerializeField, Required]
        Camera m_EventCamera;

        public override Camera eventCamera => m_EventCamera;

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        /// <summary>
        /// Event mask used to determine which objects will receive events.
        /// </summary>
        public int finalEventMask
        {
            get { return eventCamera.cullingMask & m_EventMask; }
        }

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }
    }
}
#endif