#nullable enable

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    [Icon("Packages/com.unity.ugui/Runtime/ThirdParty/Boxcat/Image/UIPolygon.png")]
    public class UIPolygon : UIImageBase
#if UNITY_EDITOR
        , IPolygonSceneGUIDelegate
#endif
    {
        [SerializeField, PropertyOrder(GraphicPropOrder.Appendix)]
        private Vector2[] _vertices = null!;
        [SerializeField, FoldoutGroup(GraphicEditorConst.Advanced)]
        private ushort[] _indices = null!;

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            var vertCount = _vertices.Length;
            mb.Poses.SetUp(_vertices);
            mb.UVs.SetUp(SolidUVCache.Get(sprite), vertCount);
            mb.Colors.SetUp(color, vertCount);
            mb.Indices.SetUp(_indices);
        }

#if UNITY_EDITOR
        private static readonly DLog _log = new(nameof(UIPolygon));

        protected override void Reset()
        {
            base.Reset();
            _vertices = Array.Empty<Vector2>();
            _indices = Array.Empty<ushort>();
        }

        [ContextMenu("Match Size _m")]
        private void MatchSize()
        {
            var t = rectTransform;
            if (t.HasChild())
            {
                _log.e("Cannot rebase polygon with children.");
                return;
            }

            // need at least 3 vertices to form a polygon
            if (_vertices.Length < 3)
                return;

            UnityEditor.Undo.RecordObject(t, "Match Polygon");

            var orgRect = t.rect;
            var orgPivotVec = orgRect.size * t.pivot;

            _vertices.Encapsulate(out var min, out var max);
            var size = max - min;
            t.sizeDelta = size;
            var newPivotPos = (orgRect.min - min) + orgPivotVec;
            t.pivot = newPivotPos / size;

            SetVerticesDirty();
        }

        [ContextMenu("Scale To 1 _s")]
        private void ScaleTo1()
        {
            var orgScale = transform.localScale;
            UnityEditor.Undo.RecordObject(this, "Scale To 1");
            for (var i = 0; i < _vertices.Length; i++)
                _vertices[i] *= orgScale.To2();
            transform.localScale = Vector3.one;
            SetVerticesDirty();
        }

        int IPolygonSceneGUIDelegate.PointCount => _vertices.Length;

        void IPolygonSceneGUIDelegate.AddPoint(Vector2 pos) => ((IPolygonSceneGUIDelegate)this).SetPoints(_vertices.CloneAdd(pos));

        void IPolygonSceneGUIDelegate.RemovePoint(int index) => ((IPolygonSceneGUIDelegate)this).SetPoints(_vertices.CloneRemoveAt(index));

        void IPolygonSceneGUIDelegate.SetPoints(Vector2[] verts)
        {
            _vertices = verts;
            _indices = Triangulator.Triangulate(_vertices);
            MatchSize();
            SetVerticesDirty();
        }

        Vector2[] IPolygonSceneGUIDelegate.ClonePoints() => _vertices.CloneStruct();
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UIPolygon))]
    public class UIPolygonEditor : Sirenix.OdinInspector.Editor.OdinEditor
    {
        private PolygonSceneGUIEditor _guiEditor;

        private void OnSceneGUI()
        {
            if (_guiEditor.IsInvalid)
                _guiEditor = new PolygonSceneGUIEditor((IPolygonSceneGUIDelegate)target);
            _guiEditor.OnSceneGUI(Event.current);

            // performance is not a big deal here, so just reset the mesh every frame to support undo.
            ((UIPolygon)target).SetVerticesDirty();
        }
    }
#endif
}
