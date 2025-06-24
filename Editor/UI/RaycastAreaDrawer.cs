using UnityEditor;

#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace UnityEngine.UI
{
    [InitializeOnLoad]
    internal static class RaycastAreaDrawer
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


                var dim = (int) side; // dimension index for Vector4

                var pos = side switch
                {
                    Side.L => r.x,
                    Side.B => r.y,
                    Side.R => r.xMax,
                    Side.T => r.yMax,
                };

                var movingX = side is Side.L or Side.R;
                var fixedCenter = movingX ? r.MidY() : r.MidX();
                var paddingValue = padding[dim]; // padding value for this side
                var paddingFlipped = side is Side.R or Side.T; // flipped axis for right and top
                var signedPadding = paddingValue * (paddingFlipped ? -1 : 1); // signed padding value

                var oldHandle = new Vector2(pos + signedPadding, fixedCenter);
                if (!movingX) oldHandle = oldHandle.YX(); // flip for vertical handles (T, B)
                oldHandle = ltw.MultiplyPoint2D(oldHandle); // translate into world-space

                var newHandle = (Vector2) Handles.FreeMoveHandle(oldHandle, hSize, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    var newPos = movingX
                        ? wtl.MultiplyPoint2D_X(newHandle)
                        : wtl.MultiplyPoint2D_Y(newHandle);
                    var a = newPos - pos;
                    if (paddingFlipped) a = -a; // flip for right and top sides
                    var paddingOpposite = padding[dim.Repeat(length: 4, offset: 2)]; // opposite side padding value
                    var b = (movingX ? r.width : r.height) - paddingOpposite; // opposite side padding
                    padding[dim] = Mathf.Min(a, b).Round(); // snap to integer
                    return true;
                }

                return false;
            }
        }
    }
}