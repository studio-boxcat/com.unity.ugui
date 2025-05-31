using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(ContentSizeFitter), true)]
    [CanEditMultipleObjects]
    internal class ContentSizeFitterEditor : SelfControllerEditor
    {
        SerializedProperty m_HorizontalFit;
        SerializedProperty m_VerticalFit;

        protected virtual void OnEnable()
        {
            m_HorizontalFit = serializedObject.FindProperty("m_HorizontalFit");
            m_VerticalFit = serializedObject.FindProperty("m_VerticalFit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw both Horizontal and Vertical controls in a single line
            const float labelW = 20f;
            const float buttonW = 80f;
            const float spacing = 20f;
            const float totalWidth = (labelW + buttonW) * 2 + spacing;
            var rect = GUILayoutUtility.GetRect(totalWidth, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(false));

            // Horizontal control
            var rectHLabel = new Rect(rect.x, rect.y, labelW, rect.height);
            var rectHButton = new Rect(rect.x + labelW, rect.y, buttonW, rect.height);
            // Vertical control
            var rectVLabel = new Rect(rect.x + labelW + buttonW + spacing, rect.y, labelW, rect.height);
            var rectVButton = new Rect(rect.x + (labelW + buttonW + spacing) + labelW, rect.y, buttonW, rect.height);

            EnumToggleButton(rectHLabel, rectHButton, m_HorizontalFit, "H", "Horizontal Fit: - = Unconstrained, M = Min Size, P = Preferred Size");
            EnumToggleButton(rectVLabel, rectVButton, m_VerticalFit, "V", "Vertical Fit: - = Unconstrained, M = Min Size, P = Preferred Size");

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();

            // Replace EnumToggleButton to accept separate label and button rects
            static void EnumToggleButton(Rect labelRect, Rect buttonRect, SerializedProperty prop, string label, string tooltip)
            {
                EditorGUI.LabelField(labelRect, new GUIContent(label, tooltip));
                var current = (ContentSizeFitter.FitMode) prop.enumValueIndex;
                var newValue = (int) current;
                string[] labels = { "-", "M", "P" };
                newValue = GUI.Toolbar(buttonRect, (int) current, labels);
                if (newValue != (int) current)
                    prop.enumValueIndex = newValue;
            }
        }
    }
}