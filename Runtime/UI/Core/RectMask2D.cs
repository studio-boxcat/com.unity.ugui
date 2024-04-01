using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Rect Mask 2D", 14)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A 2D rectangular mask that allows for clipping / masking of areas outside the mask.
    /// </summary>
    /// <remarks>
    /// The RectMask2D behaves in a similar way to a standard Mask component. It differs though in some of the restrictions that it has.
    /// A RectMask2D:
    /// *Only works in the 2D plane
    /// *Requires elements on the mask to be coplanar.
    /// *Does not require stencil buffer / extra draw calls
    /// *Requires fewer draw calls
    /// *Culls elements that are outside the mask area.
    /// </remarks>
    public sealed class RectMask2D : UIBehaviour, ICanvasRaycastFilter
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [NonSerialized]
        RectTransform _rectTransform;
        public RectTransform rectTransform => _rectTransform ??= GetComponent<RectTransform>();

        [NonSerialized]
        readonly HashSet<Graphic> _targets = new(ReferenceEqualityComparer.Object);

        [NonSerialized] Rect _lastClipRect;
        [NonSerialized] bool _forceClip;

        [SerializeField]
        Vector4 m_Padding;

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
            set
            {
                m_Padding = value;
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        [SerializeField]
        Vector2Int m_Softness;

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
                MaskUtilities.Notify2DMaskStateChanged(this);
            }
        }

        /// <remarks>
        /// Returns a non-destroyed instance or a null reference.
        /// </remarks>
        [NonSerialized, CanBeNull] Canvas _canvas;
        public Canvas Canvas => _canvas ??= GetComponentInParent<Canvas>(false);

        void OnEnable()
        {
            ClipperRegistry.Register(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

        void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            _targets.Clear();
            ClipperRegistry.Unregister(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Dont allow negative softness.
            m_Softness.x = Mathf.Max(0, m_Softness.x);
            m_Softness.y = Mathf.Max(0, m_Softness.y);

            if (isActiveAndEnabled)
                MaskUtilities.Notify2DMaskStateChanged(this);
        }
#endif

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            Assert.IsTrue(isActiveAndEnabled, "Can't check raycast for disabled mask.");
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding);
        }

        public void PerformClipping()
        {
            var canvas = Canvas;
            if (ReferenceEquals(canvas, null))
                return;

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // get the compound rects from
            // the clippers that are valid
            var clipRect = CanvasUtils.GetRectInCanvas(
                rectTransform, canvas, padding, out var validRect);

            if (clipRect != _lastClipRect)
            {
                foreach (var target in _targets)
                {
                    target.SetClipRect(clipRect, validRect);
                    target.Cull(clipRect, validRect);
                }
            }
            else if (_forceClip)
            {
                foreach (var target in _targets)
                {
                    target.SetClipRect(clipRect, validRect);

                    if (target.canvasRenderer.hasMoved)
                        target.Cull(clipRect, validRect);
                }
            }
            else
            {
                foreach (var target in _targets)
                {
                    //Case 1170399 - hasMoved is not a valid check when animating on pivot of the object
                    target.Cull(clipRect, validRect);
                }
            }

            _lastClipRect = clipRect;
            _forceClip = false;

            UpdateClipSoftness();
        }

        public void UpdateClipSoftness()
        {
            if (Canvas is null)
                return;

            foreach (var maskableTarget in _targets)
                maskableTarget.SetClipSoftness(m_Softness);
        }

        /// <summary>
        /// Add a IClippable to be tracked by the mask.
        /// </summary>
        /// <param name="target">Add the clippable object for this mask</param>
        public void AddClippable(Graphic target)
        {
            Assert.IsTrue(target is not null, "Given IClippable is null");
            _targets.Add(target);
            _forceClip = true;
        }

        /// <summary>
        /// Remove an IClippable from being tracked by the mask.
        /// </summary>
        /// <param name="clippable">Remove the clippable object from this mask</param>
        public void RemoveClippable(Graphic clippable)
        {
            Assert.IsTrue(clippable is not null, "Given IClippable is null");
            clippable.SetClipRect(new Rect(), false);
            _targets.Remove(clippable);
            _forceClip = true;
        }

        void OnTransformParentChanged() => _canvas = null;
        void OnCanvasHierarchyChanged() => _canvas = null;


#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            // ReSharper disable once Unity.NoNullPropagation
            if (transform.parent?.GetComponentInParent<RectMask2D>() is not null)
                result.AddError("RectMask2D nesting is not supported.");
        }
#endif
    }
}