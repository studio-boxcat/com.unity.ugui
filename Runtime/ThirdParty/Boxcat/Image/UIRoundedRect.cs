#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    // Rounded-rect fill: corners are drawn from a quarter-circle sprite (e.g. CM_Quarter_32) so the
    // edge is anti-aliased by the sprite alpha. 9-cell grid; corners mirror the sprite, edges/centre
    // sample its opaque centre. See [[ugui-custom.md]].
    [UIImageSpriteConstraint(FullBorders = Side.T | Side.R)]
    public class UIRoundedRect : UIImageBase
    {
        [SerializeField, Min(0)]
        [OnValueChanged("SetVerticesDirty")]
        private float _radius = 99999; // to clearly distinguish default value.

        // Rounded corners; absent ones stay sharp (square).
        [SerializeField]
        [OnValueChanged("SetVerticesDirty")]
        private Corner _corners = Corner.All;

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            var rect = rectTransform.rect;
            var radius = Mathf.Min(_radius, MaxRadius(rect, _corners));

            // 3x3 grid: a side is inset by radius only when a corner on it is rounded; a square side
            // stays flush so the centre band covers it (its corner cell collapses, drawing no arc).
            // TODO: skip empty quads when radius collapses a band to zero (degenerate tris otherwise).
            var x0 = rect.xMin;
            var x3 = rect.xMax;
            var x1 = _corners.Has(Corner.L) ? x0 + radius : x0;
            var x2 = _corners.Has(Corner.R) ? x3 - radius : x3;
            var y0 = rect.yMin;
            var y3 = rect.yMax;
            var y1 = _corners.Has(Corner.B) ? y0 + radius : y0;
            var y2 = _corners.Has(Corner.T) ? y3 - radius : y3;
            mb.Poses.SetUp_R3C3(x0, x1, x2, x3, y0, y1, y2, y3);

            mb.UVs.SetUp_MXY_R3C3(sprite);

            // Sharp corner: re-point its outer + 2 edge verts to the opaque centre (a solid square).
            // Those edge verts are shared only with edge cells, opaque there anyway. Edit() copies the
            // cached UVs so the shared cache stays intact.
            if (_corners != Corner.All)
            {
                var solid = sprite.GetInnerUVMin();
                var uvs = mb.UVs.Edit();
                if (_corners.None(Corner.BL)) uvs[0] = uvs[1] = uvs[4] = solid;
                if (_corners.None(Corner.BR)) uvs[3] = uvs[2] = uvs[7] = solid;
                if (_corners.None(Corner.TL)) uvs[12] = uvs[13] = uvs[8] = solid;
                if (_corners.None(Corner.TR)) uvs[15] = uvs[14] = uvs[11] = solid;
            }

            mb.Indices.SetUp(GridIndex.R3C3);
            mb.Colors.SetUp(color, 16);
        }

        // Combined inset of the two sides on an axis can't exceed its length: a fully-rounded pair
        // caps radius at half, one rounded side caps at full, a square axis is unbounded (no inset).
        internal static float MaxRadius(Rect rect, Corner corners)
        {
            var xInsets = (corners.Has(Corner.L) ? 1 : 0) + (corners.Has(Corner.R) ? 1 : 0);
            var yInsets = (corners.Has(Corner.B) ? 1 : 0) + (corners.Has(Corner.T) ? 1 : 0);
            var maxX = xInsets == 0 ? float.MaxValue : rect.width / xInsets;
            var maxY = yInsets == 0 ? float.MaxValue : rect.height / yInsets;
            return Mathf.Min(maxX, maxY);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Sprite = CommonAssets.QuarterSprite;
        }

        // Converts a slice authored as a fully-rounded rect from a CM corner sprite: every family
        // maps to Corner.All, only the parsed radius differs (radius = sprite radius × border
        // multiplier). Other sprites/methods need manual conversion.
        [UnityEditor.MenuItem("CONTEXT/UISlice/Convert to Rounded Rect")]
        private static void ConvertFromSlice(UnityEditor.MenuCommand cmd)
        {
            var slice = (UISlice)cmd.context;
            var sprite = slice.Sprite;
            if (!sprite || !TryParseCornerSprite(sprite!.name, out var spriteRadius))
            {
                L.E($"ConvertFromSlice: sprite must be CM_Quarter_* / CM_Semicircle_R* / CM_Circle_* - sprite={sprite.SafeName()}");
                return;
            }

            var mat = slice.material;
            var color = slice.color;
            var raycastTarget = slice.raycastTarget;
            var raycastInset = slice.raycastInset;
            var radius = Mathf.Round(spriteRadius * slice.BorderMultiplier);

            var comp = EditorUtils.ReplaceComponentInSlot<UIRoundedRect>(slice);
            comp.Sprite = CommonAssets.QuarterSprite;
            comp.material = mat;
            comp.color = color;
            comp.raycastTarget = raycastTarget;
            comp.raycastInset = raycastInset;
            comp._radius = radius;
            return;

            static bool TryParseCornerSprite(string name, out float radius)
            {
                radius = 0;
                var m = System.Text.RegularExpressions.Regex.Match(name, @"^CM_(Quarter_|Semicircle_R|Circle_)(\d+)$");
                if (!m.Success) return false;

                var n = int.Parse(m.Groups[2].Value);
                switch (m.Groups[1].Value)
                {
                    case "Quarter_":
                    case "Semicircle_R":
                        radius = n; // n is the arc radius
                        return true;
                    default: // Circle_: n is the diameter
                        radius = n / 2f;
                        return true;
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UIRoundedRect))]
    public class UIRoundedRectEditor : Sirenix.OdinInspector.Editor.OdinEditor
    {
        private void OnSceneGUI()
        {
            var img = (UIRoundedRect)target;
            var t = img.rectTransform;
            var rect = t.rect;

            var so = new UnityEditor.SerializedObject(img);
            var radiusProp = so.FindProperty("_radius");
            var corners = (Corner)so.FindProperty("_corners").intValue;

            // clamp mirrors OnPopulateMesh so the handle tracks the rendered radius
            var maxRadius = UIRoundedRect.MaxRadius(rect, corners);
            var radius = Mathf.Min(radiusProp.floatValue, maxRadius);

            // Whole gizmo in the rect's local space (one matrix, no TransformPoint round-trips).
            using (new UnityEditor.Handles.DrawingScope(Color.cyan, t.localToWorldMatrix))
            {
                // Handle on the top edge above the arc start; its inset from the right edge is the radius.
                var handle = new Vector3(rect.xMax - radius, rect.yMax, 0f);
                if (IMGUIUtils.FreeMoveHandle(handle, out var moved))
                {
                    radiusProp.floatValue = Mathf.Round(Mathf.Clamp(rect.xMax - moved.x, 0, maxRadius));
                    so.ApplyModifiedProperties();
                    img.SetVerticesDirty();
                }

                var center = new Vector3(rect.xMax - radius, rect.yMax - radius, 0f);
                UnityEditor.Handles.DrawWireArc(center, Vector3.forward, Vector3.right, 90f, radius);
            }
        }
    }
#endif
}
