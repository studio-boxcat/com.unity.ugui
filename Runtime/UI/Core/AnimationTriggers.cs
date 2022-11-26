using System;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Structure that stores the state of an animation transition on a Selectable.
    /// </summary>
    [Serializable]
    public class AnimationTriggers
    {
        private const string kDefaultNormalAnimName      = "Normal";
        private const string kDefaultPressedAnimName     = "Pressed";

        [FormerlySerializedAs("normalTrigger")]
        [SerializeField]
        private string m_NormalTrigger    = kDefaultNormalAnimName;

        [FormerlySerializedAs("pressedTrigger")]
        [SerializeField]
        private string m_PressedTrigger = kDefaultPressedAnimName;

        /// <summary>
        /// Trigger to send to animator when entering normal state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Animator buttonAnimator;
        ///     public Button button;
        ///     void SomeFunction()
        ///     {
        ///         //Sets the button to the Normal state (Useful when making tutorials).
        ///         buttonAnimator.SetTrigger(button.animationTriggers.normalTrigger);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public string normalTrigger      { get { return m_NormalTrigger; } set { m_NormalTrigger = value; } }

        /// <summary>
        /// Trigger to send to animator when entering pressed state.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class ExampleClass : MonoBehaviour
        /// {
        ///     public Animator buttonAnimator;
        ///     public Button button;
        ///     void SomeFunction()
        ///     {
        ///         //Sets the button to the Pressed state (Useful when making tutorials).
        ///         buttonAnimator.SetTrigger(button.animationTriggers.pressedTrigger);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public string pressedTrigger     { get { return m_PressedTrigger; } set { m_PressedTrigger = value; } }
    }
}
