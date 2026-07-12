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

        // `wtc` is the (root) canvas' worldToLocalMatrix, i.e. the world->canvas transform. Callers clipping
        // many renderers against one clipper should compute it once and hoist it out of the per-renderer
        // loop. Bounds are returned in that (root) canvas space. See Clipper.PerformClipping.
        public static Rect BoundingRect(RectTransform rectTransform, Matrix4x4 wtc)
            => Matrix2x3.Multiply(wtc, rectTransform.localToWorldMatrix).MultiplyAABB(rectTransform.rect);

        public static Rect BoundingRect(RectTransform rectTransform, Matrix4x4 wtc, Vector4 padding, out bool validRect)
        {
            var r = rectTransform.rect;
            // local->world->canvas composed into a single 2D-affine transform.
            var ltc = Matrix2x3.Multiply(wtc, rectTransform.localToWorldMatrix);

            if (padding.Equals(default(Vector4)))
            {
                validRect = true;
                return ltc.MultiplyAABB(r);
            }

            var ax = r.xMin + padding.x;
            var ay = r.yMin + padding.y;
            var bx = r.xMax - padding.z;
            var by = r.yMax - padding.w;

            // Check if local rect is valid.
            validRect = ax < bx && ay < by;

            return validRect
                ? ltc.MultiplyAABB(Rect.MinMaxRect(ax, ay, bx, by))
                : default;
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
