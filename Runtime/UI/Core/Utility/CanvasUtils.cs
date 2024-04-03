using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility class to help when clipping using IClipper.
    /// </summary>
    public static class CanvasUtils
    {
        public static Rect BoundingRect(RectTransform rectTransform, Canvas canvas)
        {
            Assert.IsNotNull(canvas, "Canvas cannot be null");
            return BoundingRect(rectTransform.rect, rectTransform, canvas);
        }

        public static Rect BoundingRect(RectTransform rectTransform, Canvas canvas, Vector4 padding, out bool validRect)
        {
            Assert.IsNotNull(canvas, "Canvas cannot be null");

            if (padding.Equals(default(Vector4)))
            {
                validRect = true;
                return BoundingRect(rectTransform, canvas);
            }

            var r = rectTransform.rect;
            var ax = r.xMin + padding.x;
            var ay = r.yMin + padding.y;
            var bx = r.xMax - padding.z;
            var by = r.yMax - padding.w;

            // Check if local rect is valid.
            validRect = ax < bx && ay < by;

            return validRect
                ? BoundingRect(Rect.MinMaxRect(ax, ay, bx, by), rectTransform, canvas)
                : default;
        }

        static Rect BoundingRect(Rect r, RectTransform rectTransform, Canvas canvas)
        {
            // XXX: Graphic clipping & culling is done in root canvas space.
            canvas = canvas.rootCanvas;

            // Local space corners.
            var l0 = new Vector2(r.xMin, r.yMin);
            var l1 = new Vector2(r.xMax, r.yMax);
            var l2 = new Vector2(r.xMin, r.yMax);
            var l3 = new Vector2(r.xMax, r.yMin);

            // Convert to canvas space.
            var mat = canvas.transform.worldToLocalMatrix * rectTransform.localToWorldMatrix;
            var w0 = mat.MultiplyPoint2D(l0); // Only need x and y.
            var w1 = mat.MultiplyPoint2D(l1);
            var w2 = mat.MultiplyPoint2D(l2);
            var w3 = mat.MultiplyPoint2D(l3);

            // Get min and max.
            var minX = Min(w0.x, w1.x, w2.x, w3.x);
            var minY = Min(w0.y, w1.y, w2.y, w3.y);
            var maxX = Max(w0.x, w1.x, w2.x, w3.x);
            var maxY = Max(w0.y, w1.y, w2.y, w3.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);

            static float Min(float a, float b, float c, float d) => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
            static float Max(float a, float b, float c, float d) => Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
        }

        static Vector2 MultiplyPoint2D(this Matrix4x4 mat, Vector2 point)
        {
            Vector2 v;
            v.x = (float) (mat.m00 * (double) point.x + mat.m01 * (double) point.y) + mat.m03;
            v.y = (float) (mat.m10 * (double) point.x + mat.m11 * (double) point.y) + mat.m13;
            var num = 1f / ((float) (mat.m30 * (double) point.x + mat.m31 * (double) point.y) + mat.m33);
            v.x *= num;
            v.y *= num;
            return v;
        }
    }
}