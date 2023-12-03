using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    [AddComponentMenu("Layout/Canvas Scaler", 101)]
    [DisallowMultipleComponent]
    /// <summary>
    ///   The Canvas Scaler component is used for controlling the overall scale and pixel density of UI elements in the Canvas. This scaling affects everything under the Canvas, including font sizes and image borders.
    /// </summary>
    /// <remarks>
    /// For a Canvas set to 'Screen Space - Overlay' or 'Screen Space - Camera', the Canvas Scaler UI Scale Mode can be set to Constant Pixel Size, Scale With Screen Size, or Constant Physical Size.
    /// Using the Scale With Screen Size mode, positions and sizes can be specified according to the pixels of a specified reference resolution. If the current screen resolution is larger than the reference resolution, the Canvas will keep having only the resolution of the reference resolution, but will scale up in order to fit the screen. If the current screen resolution is smaller than the reference resolution, the Canvas will similarly be scaled down to fit. If the current screen resolution has a different aspect ratio than the reference resolution, scaling each axis individually to fit the screen would result in non-uniform scaling, which is generally undesirable. Instead of this, the ReferenceResolution component will make the Canvas resolution deviate from the reference resolution in order to respect the aspect ratio of the screen. It is possible to control how this deviation should behave using the ::ref::screenMatchMode setting.
    /// For a Canvas set to 'World Space' the Canvas Scaler can be used to control the pixel density of UI elements in the Canvas.
    /// </remarks>
    public class CanvasScaler : UIBehaviour
    {
        [Tooltip("If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.")]
        [SerializeField]
        [OnValueChanged(nameof(Handle))]
        protected float m_ReferencePixelsPerUnit = 100;

        /// <summary>
        /// If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.
        /// </summary>
        public float referencePixelsPerUnit => m_ReferencePixelsPerUnit;


        /// Scale the canvas area with the width as reference, the height as reference, or something in between.
        /// <summary>
        /// Scale the canvas area with the width as reference, the height as reference, or something in between.
        /// </summary>
        public enum ScreenMatchMode
        {
            /// <summary>
            /// Expand the canvas area either horizontally or vertically, so the size of the canvas will never be smaller than the reference.
            /// </summary>
            Expand = 1,
            /// <summary>
            /// Crop the canvas area either horizontally or vertically, so the size of the canvas will never be larger than the reference.
            /// </summary>
            Shrink = 2
        }

        [Tooltip("The resolution the UI layout is designed for. If the screen resolution is larger, the UI will be scaled up, and if it's smaller, the UI will be scaled down. This is done in accordance with the Screen Match Mode.")]
        [SerializeField]
        [ShowIf("@!_editor_IsWorldCanvas"), OnValueChanged(nameof(Handle))]
        protected Vector2 m_ReferenceResolution = new(800, 600);

        /// <summary>
        /// The resolution the UI layout is designed for.
        /// </summary>
        /// <remarks>
        /// If the screen resolution is larger, the UI will be scaled up, and if it's smaller, the UI will be scaled down. This is done in accordance with the Screen Match Mode.
        /// </remarks>
        public Vector2 referenceResolution => m_ReferenceResolution;

        [Tooltip("A mode used to scale the canvas area if the aspect ratio of the current resolution doesn't fit the reference resolution.")]
        [SerializeField]
        [ShowIf("@!_editor_IsWorldCanvas"), OnValueChanged(nameof(Handle))]
        protected ScreenMatchMode m_ScreenMatchMode = ScreenMatchMode.Expand;
        /// <summary>
        /// A mode used to scale the canvas area if the aspect ratio of the current resolution doesn't fit the reference resolution.
        /// </summary>
        public ScreenMatchMode screenMatchMode => m_ScreenMatchMode;


        // World Canvas settings

        [Tooltip("The amount of pixels per unit to use for dynamically created bitmaps in the UI, such as Text.")]
        [SerializeField]
        [ShowIf("@_editor_IsWorldCanvas"), OnValueChanged(nameof(Handle))]
        protected float m_DynamicPixelsPerUnit = 1;

        /// <summary>
        /// The amount of pixels per unit to use for dynamically created bitmaps in the UI, such as Text.
        /// </summary>
        public float dynamicPixelsPerUnit => m_DynamicPixelsPerUnit;


        // General variables
        [NonSerialized] private Canvas m_Canvas;
        [NonSerialized] private float m_PrevScaleFactor = 1;
        [NonSerialized] private float m_PrevReferencePixelsPerUnit = 100;


        private void OnEnable()
        {
            m_Canvas ??= GetComponent<Canvas>();
            Handle();
            AddInstance(this);
        }

        private void OnDisable()
        {
            RemoveInstance(this);
        }

        ///<summary>
        ///Method that handles calculations of canvas scaling.
        ///</summary>
        private void Handle()
        {
            Assert.IsNotNull(m_Canvas);

            if (!m_Canvas.isRootCanvas)
                return;

            if (m_Canvas.renderMode == RenderMode.WorldSpace)
            {
                HandleWorldCanvas();
            }
            else
            {
                HandleScaleWithScreenSize();
            }
        }

        /// <summary>
        /// Handles canvas scaling for world canvas.
        /// </summary>
        private void HandleWorldCanvas()
        {
            SetScaleFactor(m_DynamicPixelsPerUnit);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        /// <summary>
        /// Handles canvas scaling that scales with the screen size.
        /// </summary>
        private void HandleScaleWithScreenSize()
        {
            var screenSize = m_Canvas.renderingDisplaySize;
            var proportionalScale = screenSize / m_ReferenceResolution;
            var scaleFactor = m_ScreenMatchMode switch
            {
                ScreenMatchMode.Expand => Mathf.Min(proportionalScale.x, proportionalScale.y),
                ScreenMatchMode.Shrink => Mathf.Max(proportionalScale.x, proportionalScale.y),
                _ => 0
            };

            SetScaleFactor(scaleFactor);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
        }

        /// <summary>
        /// Sets the scale factor on the canvas.
        /// </summary>
        /// <param name="scaleFactor">The scale factor to use.</param>
        private void SetScaleFactor(float scaleFactor)
        {
            if (scaleFactor == m_PrevScaleFactor)
                return;

            m_Canvas.scaleFactor = scaleFactor;
            m_PrevScaleFactor = scaleFactor;
        }

        /// <summary>
        /// Sets the referencePixelsPerUnit on the Canvas.
        /// </summary>
        /// <param name="referencePixelsPerUnit">The new reference pixels per Unity value</param>
        private void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
        {
            if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
                return;

            m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
            m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
        }


        [CanBeNull] static List<CanvasScaler> _instances;

        static void AddInstance(CanvasScaler instance)
        {
            // If it's first time, register callback to Canvas.preWillRenderCanvases.
            if (_instances is null)
            {
                _instances = new List<CanvasScaler>();

                Canvas.preWillRenderCanvases += static () =>
                {
                    foreach (var inst in _instances)
                    {
                        if (inst == null)
                        {
                            Debug.LogError("[CanvasScaler] instance is null.");
                            continue;
                        }

                        inst.Handle();
                    }
                };
            }

            _instances.Add(instance);
        }

        static void RemoveInstance(CanvasScaler instance)
        {
            Assert.IsNotNull(_instances, "RemoveInstance called but _instances is null.");

            // XXX: We don't use List.Remove() for performance.
            for (var i = 0; i < _instances.Count; i++)
            {
                if (ReferenceEquals(_instances[i], instance) is false)
                    continue;
                _instances.RemoveAt(i);
                return;
            }
        }

#if UNITY_EDITOR
        bool _editor_IsWorldCanvas => (m_Canvas ??= GetComponent<Canvas>()).renderMode == RenderMode.WorldSpace;
#endif
    }
}