using UnityEditor;

#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

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

        private enum Side
        {
            L = 0, // x
            B = 1, // y
            R = 2, // z = xMax
            T = 3, // w = yMax
        }

        private static void DrawRaycastRect(Graphic graphic)
        {
            var t = graphic.rectTransform;
            var padding = graphic.raycastPadding; // Store initial padding for handle positioning
            t.CalcWorldCorners2D(padding, out var rect,
                out var p0Temp, out var p1Temp, out var p2Temp, out var p3Temp);

            var p0 = p0Temp.WithZ(0);
            var p1 = p1Temp.WithZ(0);
            var p2 = p2Temp.WithZ(0);
            var p3 = p3Temp.WithZ(0);
            Handles.DrawSolidRectangleWithOutline(
                new[] { p0, p1, p2, p3 },
                Handles.UIColliderHandleColor.WithAlpha(0.15f),
                Handles.UIColliderHandleColor);

            // Handle size based on scene view camera distance
            var handleSize = HandleUtility.GetHandleSize(p0) * 0.04f;
            if (handleSize == 0) handleSize = 0.02f; // Default small size

            Handles.color = Handles.UIColliderHandleColor;

            var ltw = t.localToWorldMatrix;
            var wtl = t.worldToLocalMatrix;
            var changed = ProcessHandle(Side.L, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(Side.R, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(Side.B, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(Side.T, rect, ltw, wtl, ref padding, handleSize);
            if (changed)
            {
                Undo.RecordObject(graphic, "Adjust Raycast Padding");
                graphic.raycastPadding = padding;
                EditorUtility.SetDirty(graphic);
            }
            return;

            static bool ProcessHandle(Side side,
                Rect r,
                Matrix4x4 ltw,
                Matrix4x4 wtl,
                ref Vector4 padding, // The padding being updated this frame
                float hSize)
            {
                EditorGUI.BeginChangeCheck();

                var oldHandle = ltw.MultiplyPoint3x4(side switch // translate into world-space
                {
                    Side.L => new Vector3(r.x + padding.x, r.center.y, 0f),
                    Side.R => new Vector3(r.xMax - padding.z, r.center.y, 0f),
                    Side.B => new Vector3(r.center.x, r.y + padding.y, 0f),
                    Side.T => new Vector3(r.center.x, r.yMax - padding.w, 0f),
                });

                var newHandle = Handles.FreeMoveHandle(oldHandle, hSize, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    var (lx, ly) = wtl.MultiplyPoint2D(newHandle); // local x, local y
                    var (a, b) = side switch // b for opposite side
                    {
                        Side.L => (lx - r.x, r.width - padding.z),
                        Side.B => (ly - r.y, r.height - padding.w),
                        Side.R => (r.xMax - lx, r.width - padding.x),
                        Side.T => (r.yMax - ly, r.height - padding.y),
                    };
                    padding[(int) side] = Mathf.Min(a, b).Round(); // snap to integer
                    return true;
                }

                return false;
            }
        }
    }
}