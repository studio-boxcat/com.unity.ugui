using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Pool;

namespace UnityEngine.UI
{
    /// <summary>
    /// Mask related utility class. This class provides masking-specific utility functions.
    /// </summary>
    public class MaskUtilities
    {
        /// <summary>
        /// Notify all IClippables under the given component that they need to recalculate clipping.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<IClippable>.Get();
            mask.GetComponentsInChildren(components);
            var maskGO = mask.gameObject;
            foreach (var comp in components)
            {
                if (ReferenceEquals(comp.gameObject, maskGO)) continue;
                comp.RecalculateClipping();
            }
            ListPool<IClippable>.Release(components);
        }

        /// <summary>
        /// Notify all IMaskable under the given component that they need to recalculate masking.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<IMaskable>.Get();
            mask.GetComponentsInChildren(components);
            var maskGO = mask.gameObject;
            foreach (var comp in components)
            {
                if (ReferenceEquals(comp.gameObject, maskGO)) continue;
                comp.RecalculateMasking();
            }
            ListPool<IMaskable>.Release(components);
        }

        /// <summary>
        /// Find a root Canvas.
        /// </summary>
        /// <param name="start">Transform to start the search at going up the hierarchy.</param>
        /// <returns>Finds either the most root canvas, or the first canvas that overrides sorting.</returns>
        [CanBeNull]
        public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvas = start.GetComponentInParent<Canvas>(false);
            if (canvas is null) return null;
            var canvasTrans = canvas.transform;

            do
            {
                if (canvas.overrideSorting)
                    return canvasTrans;
                var parentTrans = canvasTrans.parent;
                if (parentTrans is null)
                    return canvasTrans;
                var parentCanvas = canvasTrans.parent.GetComponentInParent<Canvas>(false);
                if (parentCanvas is null)
                    return canvasTrans;
                canvas = parentCanvas;
                canvasTrans = canvas.transform;
            } while (true);
        }

        /// <summary>
        /// Find the stencil depth for a given element.
        /// </summary>
        /// <param name="transform">The starting transform to search.</param>
        /// <param name="stopAfter">Where the search of parents should stop</param>
        /// <returns>What the proper stencil buffer index should be.</returns>
        public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && components[i].MaskEnabled() && components[i].graphic.IsActive())
                    {
                        ++depth;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }
            ListPool<Mask>.Release(components);
            return depth;
        }

        static readonly List<RectMask2D> _rectMaskBuf = new();
        static readonly List<Canvas> _canvasBuf = new();

        /// <summary>
        /// Find the correct RectMask2D for a given IClippable.
        /// </summary>
        /// <param name="clippable">Clippable to search from.</param>
        /// <returns>The Correct RectMask2D</returns>
        public static RectMask2D GetRectMaskForClippable(IClippable clippable)
        {
            var clippableGO = clippable.gameObject;
            clippableGO.GetComponentsInParent(false, _rectMaskBuf);
            if (_rectMaskBuf.Count == 0)
                return null;

            RectMask2D componentToReturn = null;
            foreach (var rectMask in _rectMaskBuf)
            {
                componentToReturn = rectMask;
                if (ReferenceEquals(componentToReturn.gameObject, clippableGO))
                {
                    componentToReturn = null;
                    continue;
                }
                if (!componentToReturn.isActiveAndEnabled)
                {
                    componentToReturn = null;
                    continue;
                }
                clippableGO.GetComponentsInParent(false, _canvasBuf);
                for (int i = _canvasBuf.Count - 1; i >= 0; i--)
                {
                    var isDescendantOrSelf = componentToReturn.transform.IsChildOf(_canvasBuf[i].transform);
                    if (!isDescendantOrSelf && _canvasBuf[i].overrideSorting)
                    {
                        componentToReturn = null;
                        break;
                    }
                }
                break;
            }

            return componentToReturn;
        }

        /// <summary>
        /// Search for all RectMask2D that apply to the given RectMask2D (includes self).
        /// </summary>
        /// <param name="clipper">Starting clipping object.</param>
        /// <param name="masks">The list of Rect masks</param>
        public static void GetRectMasksForClip(RectMask2D clipper, List<RectMask2D> masks)
        {
            masks.Clear();

            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            clipper.transform.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                clipper.transform.GetComponentsInParent(false, canvasComponents);
                for (int i = rectMaskComponents.Count - 1; i >= 0; i--)
                {
                    if (!rectMaskComponents[i].IsActive())
                        continue;
                    bool shouldAdd = true;
                    for (int j = canvasComponents.Count - 1; j >= 0; j--)
                    {
                        var isDescendantOrSelf = rectMaskComponents[i].transform.IsChildOf(canvasComponents[i].transform);
                        if (!isDescendantOrSelf && canvasComponents[j].overrideSorting)
                        {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd)
                        masks.Add(rectMaskComponents[i]);
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);
        }
    }
}
