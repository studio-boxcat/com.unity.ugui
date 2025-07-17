// ReSharper disable InconsistentNaming

#nullable enable
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Interface for elements that can be clipped if they are under an IClipper
    /// </summary>
    public interface IClippable
    {
        Graphic Graphic { get; }
    }

    [AddComponentMenu("UI/Clipper", 14)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class Clipper : UIBehaviour, ICanvasRaycastFilter
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [NonSerialized] private RectTransform? _rectTransform;
        public RectTransform rectTransform => _rectTransform ??= GetComponent<RectTransform>();

        [NonSerialized] private Rect _lastClipRect;
        [NonSerialized] private bool _forceClip;

        [SerializeField] private Vector4 m_Padding;

        /// <summary>
        /// Padding to be applied to the masking
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 padding
        {
            get => m_Padding;
            set => m_Padding = value;
        }

        [SerializeField]
        private Vector2Int m_Softness;

        /// <summary>
        /// The softness to apply to the horizontal and vertical axis.
        /// </summary>
        public Vector2Int softness
        {
            get => m_Softness;
            set
            {
                m_Softness.x = Mathf.Max(0, value.x);
                m_Softness.y = Mathf.Max(0, value.y);
            }
        }

        /// <remarks>
        /// Returns a non-destroyed instance or a null reference.
        /// </remarks>
        [NonSerialized]
        private Canvas? _canvasCache;
        public Canvas GetCanvas()
        {
            if (_canvasCache is not null) return _canvasCache;
            _canvasCache = ComponentSearch.SearchEnabledParentOrSelfComponent<Canvas>(this);
            Assert.IsTrue(_canvasCache, "Clipper requires a Canvas component in the hierarchy.");
            return _canvasCache!;
        }

        private void OnEnable() => ClipperRegistry.RegisterClipper(this);
        private void OnDisable() => ClipperRegistry.UnregisterClipper(this);

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Dont allow negative softness.
            m_Softness.x = Mathf.Max(0, m_Softness.x);
            m_Softness.y = Mathf.Max(0, m_Softness.y);
        }
#endif

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            Assert.IsTrue(isActiveAndEnabled, "Can't check raycast for disabled mask.");
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding);
        }

        internal void PerformClipping(List<IClippable> targets)
        {
            Assert.IsTrue(targets.TrueForAll(x => x.Graphic),
                "Destroyed Graphic components are not allowed in Clipper.PerformClipping.");

            var canvas = GetCanvas();

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // get the compound rects from
            // the clippers that are valid
            var clipRect = CanvasUtils.BoundingRect(
                rectTransform, canvas, padding, out var validRect);

            if (clipRect != _lastClipRect)
            {
                foreach (var target in targets)
                {
                    var g = target.Graphic;
                    g.SetClipRect(clipRect, validRect);
                    g.Cull(clipRect, validRect);
                }
            }
            else if (_forceClip) // clipRect is the same as last time, but we need to force a clip update
            {
                foreach (var target in targets)
                {
                    var g = target.Graphic;
                    g.SetClipRect(clipRect, validRect);
                    if (g.canvasRenderer.hasMoved)
                        g.Cull(clipRect, validRect);
                }
            }
            else
            {
                foreach (var target in targets)
                {
                    var g = target.Graphic;
                    //Case 1170399 - hasMoved is not a valid check when animating on pivot of the object
                    g.Cull(clipRect, validRect);
                }
            }

            _lastClipRect = clipRect;
            _forceClip = false;

            foreach (var target in targets)
            {
                var g = target.Graphic;
                g.SetClipSoftness(m_Softness);
            }
        }

        public void MarkNeedClip() => _forceClip = true;

        private void OnTransformParentChanged() => _canvasCache = null;
        private void OnCanvasHierarchyChanged() => _canvasCache = null;


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (UnityEditor.Selection.activeGameObject != gameObject)
                return;

            var orgColor = Gizmos.color;
            Gizmos.color = Color.yellow;

            var t = rectTransform;
            var rect = t.rect;
            var x0 = rect.xMin + m_Padding.x;
            var y0 = rect.yMin + m_Padding.y;
            var x1 = rect.xMax - m_Padding.z;
            var y1 = rect.yMax - m_Padding.w;

            var p0 = t.TransformPoint(new Vector2(x0, y0));
            var p1 = t.TransformPoint(new Vector2(x0, y1));
            var p2 = t.TransformPoint(new Vector2(x1, y1));
            var p3 = t.TransformPoint(new Vector2(x1, y0));
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);

            Gizmos.color = orgColor;
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // ReSharper disable once Unity.NoNullPropagation
            if (transform.parent?.GetComponentInParent<Clipper>() is not null)
                result.AddError("Clipper nesting is not supported.");
        }
#endif
    }
}