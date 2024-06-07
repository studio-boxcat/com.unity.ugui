namespace UnityEngine.EventSystems
{
    static class UIInput
    {
        /// <summary>
        /// Interface to Input.compositionString. Can be overridden to provide custom input instead of using the Input class.
        /// </summary>
        public static string compositionString => Input.compositionString;

        /// <summary>
        /// Interface to Input.imeCompositionMode. Can be overridden to provide custom input instead of using the Input class.
        /// </summary>
        public static IMECompositionMode imeCompositionMode
        {
            get => Input.imeCompositionMode;
            set => Input.imeCompositionMode = value;
        }

        /// <summary>
        /// Interface to Input.compositionCursorPos. Can be overridden to provide custom input instead of using the Input class.
        /// </summary>
        public static Vector2 compositionCursorPos
        {
            get => Input.compositionCursorPos;
            set => Input.compositionCursorPos = value;
        }

        /// <summary>
        /// Interface to Input.mousePresent. Can be overridden to provide custom input instead of using the Input class.
        /// </summary>
        public static bool mousePresent => Input.mousePresent;

        public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);
        public static bool GetMouseButtonUp(int button) => Input.GetMouseButtonUp(button);

        public static Vector2 mousePosition => Input.mousePosition;
        public static Vector2 mouseScrollDelta => Input.mouseScrollDelta;
        public static bool touchSupported => Input.touchSupported;
        public static int touchCount => Input.touchCount;
        public static Touch GetTouch(int index) => Input.GetTouch(index);
    }
}