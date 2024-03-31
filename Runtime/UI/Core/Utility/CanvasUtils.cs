using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility class to help when clipping using IClipper.
    /// </summary>
    public static class CanvasUtils
    {
        public static Rect GetRectInCanvas(RectTransform rectTransform, Canvas canvas)
        {
            Assert.IsNotNull(canvas, "Canvas cannot be null");

            var r = rectTransform.rect;
            var localPointA = r.min;
            var localPointB = r.max;
            return GetRectInCanvas(localPointA, localPointB, rectTransform, canvas);
        }

        public static Rect GetRectInCanvas(RectTransform rectTransform, Canvas canvas, Vector4 padding, out bool validRect)
        {
            Assert.IsNotNull(canvas, "Canvas cannot be null");

            if (padding.Equals(default(Vector4)))
            {
                validRect = true;
                return GetRectInCanvas(rectTransform, canvas);
            }

            var r = rectTransform.rect;
            var localPointA = new Vector2(
                r.xMin + padding.x,
                r.yMin + padding.y);
            var localPointB = new Vector2(
                r.xMax - padding.z,
                r.yMax - padding.w);

            // Check if local rect is valid.
            validRect = localPointB.x > localPointA.x && localPointB.y > localPointA.y;

            return validRect
                ? GetRectInCanvas(localPointA, localPointB, rectTransform, canvas)
                : default;
        }

        static Rect GetRectInCanvas(Vector2 localPointA, Vector2 localPointB, RectTransform rectTransform, Canvas canvas)
        {
            // Convert to canvas space.
            var mat = canvas.transform.worldToLocalMatrix * rectTransform.localToWorldMatrix;
            Vector2 worldPointA = mat.MultiplyPoint(localPointA); // Only need x and y.
            Vector2 worldPointB = mat.MultiplyPoint(localPointB);

            // Get min and max.
            var min = Vector2.Min(worldPointA, worldPointB);
            var max = Vector2.Max(worldPointA, worldPointB);
            return new Rect(min, max - min);
        }
    }
}