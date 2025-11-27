#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Utility class to help when clipping using IClipper.
    /// </summary>
    public static class CanvasUtils
    {
        private static readonly DLog _log = new(nameof(CanvasUtils));

        public static bool IsRenderRoot(Canvas c) =>
            c is { enabled: true, overrideSorting: true } || c.isRootCanvas;

        public static bool IsRenderRoot(Component c) =>
            c.TryGetComponent(out Canvas canvas) && IsRenderRoot(canvas);

        public static Canvas? ResolveRootCanvas(this Transform t)
        {
            var result = t.root.TryGetComponent<Canvas>(out var canvas);
            if (result is true)
            {
                return canvas;
            }
            else
            {
                _log.e($"No root canvas found for the transform: {t}");
                return null;
            }
        }

        public static Canvas? ResolveRenderRoot(this Graphic g)
        {
            var canvas = g.canvas; // nearest canvas.

            while (canvas is not null && IsRenderRoot(canvas) is false)
            {
                var t = canvas.transform.parent;
                canvas = ComponentSearch.NearestUpwards_GOAnyAndCompEnabled<Canvas>(t);
            }

            if (canvas is null)
                _log.e($"No render root canvas found for the graphic: {g}");

            return canvas;
        }

        public static Camera? ResolveWorldCamera(Graphic g)
        {
            Assert.IsTrue(g, "Graphic is null.");

            if (!g.canvas)
            {
                _log.e("No canvas found for the graphic: " + g);
                return null;
            }

            var cam = g.canvas.worldCamera;
#if UNITY_EDITOR
            if (!cam && g.canvas.rootCanvas.name == "Prefab Mode in Context")
                cam = Camera.current; // camera is not exists for prefab stage.
#endif
            if (!cam) _log.e("No camera found for the graphic: " + g);
            return cam;
        }

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

        private static Rect BoundingRect(Rect r, RectTransform rectTransform, Canvas canvas)
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

        private static Vector2 MultiplyPoint2D(this Matrix4x4 mat, Vector2 point)
        {
            Vector2 v;
            v.x = (float) (mat.m00 * (double) point.x + mat.m01 * (double) point.y) + mat.m03;
            v.y = (float) (mat.m10 * (double) point.x + mat.m11 * (double) point.y) + mat.m13;
            var num = 1f / ((float) (mat.m30 * (double) point.x + mat.m31 * (double) point.y) + mat.m33);
            v.x *= num;
            v.y *= num;
            return v;
        }

        // It is always unique, and never has the value 0.
        // XXX: Instance ID could be reused when the Edit mode exits, the same GameObject in the scene will have the same ID,
        // but the old one will be destroyed.
        // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.GetInstanceID.html
        public static void PruneDestroyedAndDedup<TObject>(List<(TObject Object, int InstanceID)> list) where TObject : Object
        {
            // first remove destroyed objects
            list.RemoveAll(item => !item.Object);

            // sort for deduplication.
            list.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID));

            // remove duplicates.
            var count = list.Count;
            var dupCount = 0;
            var lastID = 0; // 0 is invalid ID.
            for (var i = 0; i < count; i++)
            {
                var item = list[i];
                var curID = item.InstanceID;
                if (curID == lastID)
                {
                    dupCount++; // skip duplicates.
                }
                else
                {
                    lastID = curID; // update lastId to current.
                    if (dupCount is not 0)
                        list[i - dupCount] = item; // move the item to the left.
                }
            }

            // remove the tail.
            if (dupCount is not 0)
                list.RemoveRange(count - dupCount, dupCount);
        }
    }
}