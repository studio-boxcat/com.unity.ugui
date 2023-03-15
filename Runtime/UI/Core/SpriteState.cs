using System;

namespace UnityEngine.UI
{
    [Serializable]
    /// <summary>
    /// Structure that stores the state of a sprite transition on a Selectable.
    /// </summary>
    public struct SpriteState : IEquatable<SpriteState>
    {
        [SerializeField]
        private Sprite m_PressedSprite;

        /// <summary>
        /// Pressed sprite.
        /// </summary>
        public Sprite pressedSprite
        {
            get => m_PressedSprite;
            set => m_PressedSprite = value;
        }

        public bool Equals(SpriteState other)
        {
            return ReferenceEquals(pressedSprite, other.pressedSprite);
        }
    }
}