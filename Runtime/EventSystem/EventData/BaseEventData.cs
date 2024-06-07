namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A class that contains the base event data that is common to all event types in the new EventSystem.
    /// </summary>
    public class BaseEventData
    {
        bool _used;

        /// <summary>
        /// Is the event used?
        /// </summary>
        public bool used => _used;

        /// <summary>
        /// Reset the event.
        /// </summary>
        public void Reset() => _used = false;

        /// <summary>
        /// Use the event.
        /// </summary>
        /// <remarks>
        /// Internally sets a flag that can be checked via used to see if further processing should happen.
        /// </remarks>
        public void Use() => _used = true;
    }
}