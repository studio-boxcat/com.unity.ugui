using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomPropertyDrawer(typeof(AnimationTriggers), true)]
    /// <summary>
    /// This is a PropertyDrawer for AnimationTriggers. It is implemented using the standard Unity PropertyDrawer framework.
    /// </summary>
    public class AnimationTriggersDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            Rect drawRect = rect;
            drawRect.height = EditorGUIUtility.singleLineHeight;

            SerializedProperty normalTrigger = prop.FindPropertyRelative("m_NormalTrigger");
            SerializedProperty pressedTrigger = prop.FindPropertyRelative("m_PressedTrigger");

            EditorGUI.PropertyField(drawRect, normalTrigger);
            drawRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(drawRect, pressedTrigger);
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return 2 * EditorGUIUtility.singleLineHeight + 1 * EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
