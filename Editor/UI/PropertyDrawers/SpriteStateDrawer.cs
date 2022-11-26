using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomPropertyDrawer(typeof(SpriteState), true)]
    /// <summary>
    /// This is a PropertyDrawer for SpriteState. It is implemented using the standard Unity PropertyDrawer framework.
    /// </summary>
    public class SpriteStateDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            Rect drawRect = rect;
            drawRect.height = EditorGUIUtility.singleLineHeight;
            SerializedProperty pressedSprite = prop.FindPropertyRelative("m_PressedSprite");

            EditorGUI.PropertyField(drawRect, pressedSprite);
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return 1 * EditorGUIUtility.singleLineHeight + 0 * EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
