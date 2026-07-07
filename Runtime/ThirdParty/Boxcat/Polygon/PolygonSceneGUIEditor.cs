#nullable enable
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnityEngine.UI
{
    interface IPolygonSceneGUIDelegate
    {
        int PointCount { get; }
        void AddPoint(Vector2 pos);
        void RemovePoint(int index);
        void SetPoints(Vector2[] verts);
        Vector2[] ClonePoints();
    }

    internal struct PolygonSceneGUIEditor
    {
        private const int _controlMask = 0x493A6900;

        private readonly IPolygonSceneGUIDelegate _polygon;
        private int _lastNearestControl;

        public PolygonSceneGUIEditor(IPolygonSceneGUIDelegate polygon) : this()
        {
            _polygon = polygon;
        }

        public bool IsInvalid => _polygon is null;

        public void OnSceneGUI(Event e)
        {
            var target = (Component) _polygon;
            var p = _polygon;
            var t = target.transform;
            var controlVert = GetControlVert(_lastNearestControl);
            var mouseWPos = EditorUtils.TranslateSVToWorld(e.mousePosition);
            var mouseLPos = t.InverseTransformPoint(mouseWPos);

            if (e.UseLD(KeyMod.S)) // If click with shift down, create a point
            {
                Undo.RecordObject(target, "Add Point");
                p.AddPoint(mouseLPos);
            }
            else if (e.UseDeleteIntention())
            {
                Undo.RecordObject(target, "Remove Point");
                p.RemovePoint(controlVert ?? (p.PointCount - 1)); // remove last point if no handle is selected
            }

            // Generate Handles and lines between the handles
            using (new Handles.DrawingScope(t.localToWorldMatrix))
            {
                var verts = p.ClonePoints();
                var handleSize = HandleUtility.GetHandleSize(t.position) * 0.07f;
                if (DrawVertsHandles(verts, mouseLPos, handleSize, controlVert, e))
                {
                    Undo.RecordObject(target, "Move Point");
                    p.SetPoints(verts);
                }
            }

            var eventType = e.type;
            if (eventType is EventType.Layout or EventType.Repaint or EventType.MouseMove)
                _lastNearestControl = HandleUtility.nearestControl;

            // to get keyboard events
            if (eventType is EventType.Layout)
                HandleUtility.AddDefaultControl(target.GetInstanceID());

            if (e.modifiers.IsShift())
                EditorUtils.SetMouseCursorOnSceneView(MouseCursor.ArrowPlus);

            return;

            static int? GetControlVert(int id)
            {
                return (id & _controlMask) == _controlMask
                    ? id & 0xFF : null;
            }
        }

        private static bool DrawVertsHandles(Vector2[] verts, Vector3 mouseLocalPos, float handleSize, int? controlVert, Event e)
        {
            var count = verts.Length;
            var color = Handles.color;
            var lineColor = Color.magenta;

            var keyMod = e.modifiers;
            if (keyMod.IsShift())
            {
                Handles.color = lineColor;
                if (count > 0) Handles.DrawLine(verts[count - 1], mouseLocalPos);
            }

            EditorGUI.BeginChangeCheck();
            for (var i = 0; i < count; i++)
            {
                // Generate line
                var i2 = i is 0 ? count - 1 : i - 1;
                var a = verts[i];
                var b = verts[i2];

                Handles.color = lineColor.WithA(0.5f);
                Handles.DrawLine(a, b);

                // Generate Handle
                var isLast = i == count - 1;
                Handles.color = isLast ? Color.black : Color.magenta;

                var curHandleSize = handleSize;
                var isControlled = controlVert == i;
                if (isControlled) curHandleSize *= 1.5f;

                var controlId = _controlMask | i;
                var newPosition = Handles.FreeMoveHandle(
                    controlId, a, curHandleSize, Vector3.zero,
                    Handles.DotHandleCap);

                // Update vertex position
                if (keyMod.IsShiftOrNone())
                    verts[i] = newPosition;
            }

            return EditorGUI.EndChangeCheck();
        }
    }
}
#endif
