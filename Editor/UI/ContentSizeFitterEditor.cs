using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(ContentSizeFitter), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the ContentSizeFitter Component.
    /// Extend this class to write a custom editor for a component derived from ContentSizeFitter.
    /// </summary>
    public class ContentSizeFitterEditor : SelfControllerEditor
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
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20; // Set a smaller label width for compactness

            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_HorizontalFit, GUIContent.Temp("H"), true);
            EditorGUILayout.Space(4, false);
            EditorGUILayout.PropertyField(m_VerticalFit, GUIContent.Temp("V"), true);
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.labelWidth = labelWidth; // Restore original label width

            base.OnInspectorGUI();
        }
    }
}
