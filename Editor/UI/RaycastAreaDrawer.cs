using UnityEditor;

namespace UnityEngine.UI
{
    [InitializeOnLoad]
    public static class RaycastAreaDrawer
    {
        static RaycastAreaDrawer()
        {
            SceneView.duringSceneGui += sceneView =>
            {
                if (!sceneView.drawGizmos)
                    return;

                foreach (var transform in Selection.transforms)
                {
                    if (transform.TryGetComponent(out Graphic graphic) == false)
                        continue;
                    if (EditorGUIUtility.IsGizmosAllowedForObject(graphic) == false)
                        continue;
                    if (graphic.raycastTarget == false)
                        continue;

                    DrawRaycastRect(graphic.rectTransform, graphic.raycastPadding);
                }
            };
        }

        static void DrawRaycastRect(RectTransform transform, Vector4 raycastPadding)
        {
            var rect = transform.rect;
            var z = transform.position.z;

            var xMin = rect.x + raycastPadding.x;
            var xMax = rect.xMax - raycastPadding.z;
            var yMin = rect.y + raycastPadding.y;
            var yMax = rect.yMax - raycastPadding.w;

            var p0 = new Vector3(xMin, yMin, z);
            var p1 = new Vector3(xMin, yMax, z);
            var p2 = new Vector3(xMax, yMax, z);
            var p3 = new Vector3(xMax, yMin, z);

            var mat = transform.localToWorldMatrix;
            p0 = mat.MultiplyPoint(p0);
            p1 = mat.MultiplyPoint(p1);
            p2 = mat.MultiplyPoint(p2);
            p3 = mat.MultiplyPoint(p3);

            Handles.color = Handles.UIColliderHandleColor;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p2);
            Handles.DrawLine(p2, p3);
            Handles.DrawLine(p3, p0);
        }
    }
}