#nullable enable
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

using Sirenix.OdinInspector.Editor;
using UnityEditor;

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
                var mode = _mode;
                if (mode is 0) return; // disabled

                foreach (var transform in Selection.transforms)
                {
                    var main = transform.GetComponent<Graphic>(); // can be null.

                    if (mode is 1) // enabled
                    {
                        if (main is null) continue;
                        ProcessRaycastArea(main, true);
                    }
                    else // deep
                    {
                        var children = transform.GetGraphicsInChildrenShared();
                        foreach (var g in children)
                            ProcessRaycastArea(g, g.RefEq(main));
                    }
                }
            };
            return;

            static void ProcessRaycastArea(Graphic g, bool main)
            {
                if (g.raycastTarget is false) return;
                if (EditorGUIUtility.IsGizmosAllowedForObject(g) is false) return;
                var rect = DrawRect(g);
                if (main) DrawHandle(g, rect);
            }
        }

        private static Rect DrawRect(Graphic graphic)
        {
            var t = graphic.rectTransform;
            var padding = graphic.raycastPadding; // Store initial padding for handle positioning
            t.CalcWorldCorners2D(padding,
                out var p0Temp, out var p1Temp, out var p2Temp, out var p3Temp, out var rect);

            var p0 = p0Temp.WithZ(0);
            var p1 = p1Temp.WithZ(0);
            var p2 = p2Temp.WithZ(0);
            var p3 = p3Temp.WithZ(0);
            Handles.DrawSolidRectangleWithOutline(
                new[] { p0, p1, p2, p3 },
                Handles.UIColliderHandleColor.WithA(0.15f),
                Handles.UIColliderHandleColor);
            return rect;
        }

        private static void DrawHandle(Graphic graphic, Rect rect)
        {
            // Handle size based on scene view camera distance
            var handleSize = HandleUtility.GetHandleSize(default) * 0.03f;
            if (handleSize == 0) handleSize = 0.02f; // Default small size

            Handles.color = Handles.UIColliderHandleColor;

            var t = graphic.rectTransform;
            var ltw = t.localToWorldMatrix;
            var wtl = t.worldToLocalMatrix;
            var padding = graphic.raycastPadding; // Store initial padding for handle positioning
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
                Rect r, // transform rect in local-space
                Matrix4x4 ltw,
                Matrix4x4 wtl,
                ref Vector4 padding, // The padding being updated this frame
                float hSize)
            {
                EditorGUI.BeginChangeCheck();

                var ctrlX = side is Side.L or Side.R; // controlling x?
                var ltr = side is Side.L or Side.B; // left -> right, bottom -> top

                var len = ctrlX ? r.width : r.height; // length of the side being adjusted
                var pos = (ctrlX ? r.x : r.y) + (ltr ? 0 : len);

                ref var paddingControl = ref padding.Get(side); // padding value for this side
                ref var paddingOpposite = ref padding.Get(side.Opposite()); // opposite side padding value
                var signedPadding = paddingControl * ltr.Sign(); // signed padding value

                var center = ctrlX ? r.MidY() : r.MidX();
                var centerFlow = side is Side.L or Side.T; // flip center for left and top sides
                center += (padding.Get(side.Repeat(1)) - padding.Get(side.Repeat(3))).Half() * centerFlow.Sign(); // adjust center
                var oldHandle = new Vector2(pos + signedPadding, center); // x for control point, y for center
                if (!ctrlX) oldHandle = oldHandle.YX(); // flip for vertical handles (T, B)
                oldHandle = ltw.MultiplyPoint2D(oldHandle); // translate into world-space

                var newHandle = (Vector2) Handles.FreeMoveHandle(oldHandle, hSize, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    var newPos = wtl.MultiplyPoint1D(newHandle, axisX: ctrlX); // translate back to local-space
                    var mirror = Event.current.alt;

                    var a = newPos - pos;
                    if (!ltr) a = -a; // flip for right and top sides
                    var b = mirror ? len.Half() : len - paddingOpposite;
                    paddingControl = Mathf.Min(a, b).Round(); // snap to integer
                    if (mirror) paddingOpposite = paddingControl;
                    return true;
                }

                return false;
            }
        }

        private static EditorPrefInt? _modePref;
        private static int _mode => _modePref ??= new EditorPrefInt("G0HmzQzL", 1); // 0: disabled, 1: enabled, 2: enabled (deep)

        [MenuItem(MenuPath.UI + "Hide Raycast Area")]
        private static void HideRaycastArea() => SetMode(0);
        [MenuItem(MenuPath.UI + "Show Raycast Area")]
        private static void ShowRaycastArea() => SetMode(1);
        [MenuItem(MenuPath.UI + "Show Raycast Area (Deep)")]
        private static void ShowRaycastAreaDeep() => SetMode(2);

        private static void SetMode(int value)
        {
            _modePref!.Value = value;
            SceneView.RepaintAll(); // Force all SceneViews to repaint
        }
    }
}