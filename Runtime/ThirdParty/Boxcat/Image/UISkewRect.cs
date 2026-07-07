#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    // A solid parallelogram: the full rect sheared symmetrically about its center along one axis by a
    // slope — see UISolid for the plain (un-sheared) variant.
    public class UISkewRect : UIImageBase
    {
        [SerializeField, OnValueChanged(nameof(SetVerticesDirty))]
        private Axis _axis = Axis.X;
        // Edge tangent: X shears the vertical edges (dx/dy = slope), Y the horizontal edges (dy/dx =
        // slope). Centered, so the mid-line stays put. 0 = no shear.
        [SerializeField, OnValueChanged(nameof(SetVerticesDirty))]
        private float _slope = 1f;

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            var r = rectTransform.rect;
            var uv = SolidUVCache.Get(sprite);

            var w = r.width;
            var h = r.height;
            var xMin = r.xMin;
            var yMin = r.yMin;
            var xMax = xMin + w;
            var yMax = yMin + h;

            var bl = new Vector2(xMin, yMin);
            var br = new Vector2(xMax, yMin);
            var tl = new Vector2(xMin, yMax);
            var tr = new Vector2(xMax, yMax);

            // Corners 0=bl, 1=br, 2=tl, 3=tr; sheared symmetrically about the center.
            if (_axis is Axis.X)
            {
                var dx = _slope * h;
                if (_slope < 0)
                {
                    bl.x -= dx;
                    tr.x += dx;
                }
                else
                {
                    br.x -= dx;
                    tl.x += dx;
                }
            }
            else
            {
                // Vertical shear: corners move in y, inward (br at min-y → +dy), so the quad stays inscribed.
                var dy = _slope * w;
                if (_slope < 0)
                {
                    bl.y -= dy;
                    tr.y += dy;
                }
                else
                {
                    br.y += dy;
                    tl.y -= dy;
                }
            }

            mb.SetUp_Quad(bl, br, tl, tr, uv, color);
        }
    }
}
