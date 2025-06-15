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
                    if (graphic.raycastTarget == false)
                        continue;
                    if (EditorGUIUtility.IsGizmosAllowedForObject(graphic) == false)
                        continue;
                    DrawRaycastRect(graphic);
                }
            };
        }

        private static void DrawRaycastRect(Graphic graphic)
        {
            var t = graphic.rectTransform;
            var raycastPadding = graphic.raycastPadding;

            var rect = t.rect;

            var xMin = rect.x + raycastPadding.x;
            var xMax = rect.xMax - raycastPadding.z;
            var yMin = rect.y + raycastPadding.y;
            var yMax = rect.yMax - raycastPadding.w;

            var p0 = new Vector2(xMin, yMin);
            var p1 = new Vector2(xMin, yMax);
            var p2 = new Vector2(xMax, yMax);
            var p3 = new Vector2(xMax, yMin);

            var mat = t.localToWorldMatrix;
            p0 = mat.MultiplyPoint2D(p0);
            p1 = mat.MultiplyPoint2D(p1);
            p2 = mat.MultiplyPoint2D(p2);
            p3 = mat.MultiplyPoint2D(p3);

            var z = t.position.z;
            Handles.DrawSolidRectangleWithOutline(
                new[] { p0.To3(z), p1.To3(z), p2.To3(z), p3.To3(z) },
                Handles.UIColliderHandleColor.WithAlpha(0.15f),
                Handles.UIColliderHandleColor);
        }
    }
}