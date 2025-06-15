using System;
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

        private enum HandleSide
        {
            Left = 0,
            Bottom = 1,
            Right = 2,
            Top = 3,
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
            var changed = ProcessHandle(HandleSide.Left, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(HandleSide.Right, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(HandleSide.Bottom, rect, ltw, wtl, ref padding, handleSize)
                          || ProcessHandle(HandleSide.Top, rect, ltw, wtl, ref padding, handleSize);
            if (changed)
            {
                Undo.RecordObject(graphic, "Adjust Raycast Padding");
                graphic.raycastPadding = padding;
                EditorUtility.SetDirty(graphic);
            }
            return;

            static bool ProcessHandle(HandleSide side,
                Rect r,
                Matrix4x4 ltw,
                Matrix4x4 wtl,
                ref Vector4 padding, // The padding being updated this frame
                float hSize
                // Padding state at the start of this Draw call
            )
            {
                EditorGUI.BeginChangeCheck();

                var oldHandle = ltw.MultiplyPoint3x4(side switch
                {
                    HandleSide.Left => new Vector3(r.x + padding.x, r.center.y, 0f),
                    HandleSide.Right => new Vector3(r.xMax - padding.z, r.center.y, 0f),
                    HandleSide.Bottom => new Vector3(r.center.x, r.y + padding.y, 0f),
                    HandleSide.Top => new Vector3(r.center.x, r.yMax - padding.w, 0f),
                    _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
                });

                var newHandle = Handles.FreeMoveHandle(oldHandle, hSize, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    var (lx, ly) = wtl.MultiplyPoint2D(newHandle); // local x, local y
                    var value = side switch
                    {
                        HandleSide.Left => Mathf.Clamp(lx - r.x, 0, r.width - padding.z),
                        HandleSide.Bottom => Mathf.Clamp(ly - r.y, 0, r.height - padding.w),
                        HandleSide.Right => Mathf.Clamp(r.xMax - lx, 0, r.width - padding.x),
                        HandleSide.Top => Mathf.Clamp(r.yMax - ly, 0, r.height - padding.y),
                        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
                    };
                    padding[(int) side] = value.Round(); // snap to integer
                    return true;
                }

                return false;
            }
        }
    }
}