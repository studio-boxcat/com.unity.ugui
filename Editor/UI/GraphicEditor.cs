using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    /// <summary>
    /// Editor class used to edit UI Graphics.
    /// Extend this class to write your own graphic editor.
    /// </summary>

    [CustomEditor(typeof(MaskableGraphic), false)]
    [CanEditMultipleObjects]
    public class GraphicEditor : Editor
    {
        protected SerializedProperty m_Script;
        protected SerializedProperty m_Color;
        protected SerializedProperty m_Material;
        protected SerializedProperty m_RaycastTarget;
        protected SerializedProperty m_RaycastPadding;
        protected SerializedProperty m_Maskable;

        GUIContent m_PaddingContent;
        GUIContent m_LeftContent;
        GUIContent m_RightContent;
        GUIContent m_TopContent;
        GUIContent m_BottomContent;
        static private bool m_ShowPadding = false;

        protected virtual void OnDisable()
        {
            Tools.hidden = false;
        }

        protected virtual void OnEnable()
        {
            m_PaddingContent = EditorGUIUtility.TrTextContent("Raycast Padding");
            m_LeftContent = EditorGUIUtility.TrTextContent("Left");
            m_RightContent = EditorGUIUtility.TrTextContent("Right");
            m_TopContent = EditorGUIUtility.TrTextContent("Top");
            m_BottomContent = EditorGUIUtility.TrTextContent("Bottom");

            m_Script = serializedObject.FindProperty("m_Script");
            m_Color = serializedObject.FindProperty("m_Color");
            m_Material = serializedObject.FindProperty("m_Material");
            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            m_RaycastPadding = serializedObject.FindProperty("m_RaycastPadding");
            m_Maskable = serializedObject.FindProperty("m_Maskable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Script);
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected void MaskableControlsGUI()
        {
            EditorGUILayout.PropertyField(m_Maskable);
        }

        /// <summary>
        /// GUI related to the appearance of the Graphic. Color and Material properties appear here.
        /// </summary>
        protected void AppearanceControlsGUI()
        {
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_Material);
        }

        /// <summary>
        /// GUI related to the Raycasting settings for the graphic.
        /// </summary>
        protected void RaycastControlsGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_RaycastTarget);
            if (EditorGUI.EndChangeCheck() && target is Graphic graphic)
            {
                graphic.SetRaycastDirty();
            }

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (m_ShowPadding)
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4;

            var rect = EditorGUILayout.GetControlRect(true, height);
            EditorGUI.BeginProperty(rect, m_PaddingContent, m_RaycastPadding);
            rect.height = EditorGUIUtility.singleLineHeight;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                m_ShowPadding = EditorGUI.Foldout(rect, m_ShowPadding, m_PaddingContent, true);
                if (check.changed)
                {
                    SceneView.RepaintAll();
                }
            }

            if (m_ShowPadding)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.indentLevel++;
                    Vector4 newPadding = m_RaycastPadding.vector4Value;

                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    newPadding.x = EditorGUI.FloatField(rect, m_LeftContent, newPadding.x);

                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    newPadding.y = EditorGUI.FloatField(rect, m_BottomContent, newPadding.y);

                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    newPadding.z = EditorGUI.FloatField(rect, m_RightContent, newPadding.z);

                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    newPadding.w = EditorGUI.FloatField(rect, m_TopContent, newPadding.w);

                    if (check.changed)
                    {
                        m_RaycastPadding.vector4Value = newPadding;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
