using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(Selectable), true)]
    /// <summary>
    /// Custom Editor for the Selectable Component.
    /// Extend this class to write a custom editor for a component derived from Selectable.
    /// </summary>
    public class SelectableEditor : Editor
    {
        SerializedProperty m_Script;
        SerializedProperty m_InteractableProperty;

        // Whenever adding new SerializedProperties to the Selectable and SelectableEditor
        // Also update this guy in OnEnable. This makes the inherited classes from Selectable not require a CustomEditor.
        private string[] m_PropertyPathToExcludeForChildClasses;

        protected virtual void OnEnable()
        {
            m_Script                = serializedObject.FindProperty("m_Script");
            m_InteractableProperty  = serializedObject.FindProperty("m_Interactable");

            m_PropertyPathToExcludeForChildClasses = new[]
            {
                m_Script.propertyPath,
                m_InteractableProperty.propertyPath,
            };
        }

        protected virtual void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_InteractableProperty);

            EditorGUI.BeginChangeCheck();
            Rect toggleRect = EditorGUILayout.GetControlRect();
            toggleRect.xMin += EditorGUIUtility.labelWidth;

            // We do this here to avoid requiring the user to also write a Editor for their Selectable-derived classes.
            // This way if we are on a derived class we dont draw anything else, otherwise draw the remaining properties.
            ChildClassPropertiesGUI();

            serializedObject.ApplyModifiedProperties();
        }

        // Draw the extra SerializedProperties of the child class.
        // We need to make sure that m_PropertyPathToExcludeForChildClasses has all the Selectable properties and in the correct order.
        // TODO: find a nicer way of doing this. (creating a InheritedEditor class that automagically does this)
        private void ChildClassPropertiesGUI()
        {
            if (IsDerivedSelectableEditor())
                return;

            DrawPropertiesExcluding(serializedObject, m_PropertyPathToExcludeForChildClasses);
        }

        private bool IsDerivedSelectableEditor()
        {
            return GetType() != typeof(SelectableEditor);
        }
    }
}
