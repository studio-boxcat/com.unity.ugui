using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasRenderer))]
    /// <summary>
    /// A derived BaseRaycaster to raycast against Graphic elements.
    /// </summary>
    public class GraphicRaycaster : BaseRaycaster
    {
        [SerializeField, Required, ChildGameObjectsOnly, NotNull]
        Canvas m_Canvas;

        public Canvas canvas => m_Canvas;

        private void Awake()
        {
            if (m_Canvas is null)
            {
                TryGetComponent(out m_Canvas);
                Assert.IsNotNull(m_Canvas);
            }
        }

        /// <summary>
        /// Perform the raycast against the list of graphics associated with the Canvas.
        /// </summary>
        public override bool Raycast(Vector2 screenPosition, out RaycastResult result)
        {
            result = Raycast(screenPosition);
            return result.collider is not null;
        }

        private RaycastResult Raycast(Vector2 screenPosition)
        {
            var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return default;

            var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference
            Assert.IsNotNull(currentEventCamera);
            Assert.AreNotEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);

            if (RaycastUtils.IsInside(currentEventCamera, screenPosition) == false)
                return default;

            if (Raycast(currentEventCamera, screenPosition, canvasGraphics, out var hitGraphic) == false)
                return default;

            return new RaycastResult(hitGraphic, this, screenPosition);
        }

        public override Camera eventCamera => m_Canvas.worldCamera;

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
        public static bool Raycast([NotNull] Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, out Graphic result)
        {
            Assert.AreEqual(0, s_SortedGraphics.Count);

            // Necessary for the event system
            Graphic maxDepthGraphic = null;
            int maxDepth = -1; // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
            int totalCount = foundGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
            {
                Graphic graphic = foundGraphics[i];
                var graphicDepth = graphic.depth;
                if (graphicDepth < maxDepth)
                    continue;

                if (!graphic.raycastTarget || graphic.canvasRenderer.cull)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
                    continue;

                if (eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
                    continue;

                if (graphic.Raycast(pointerPosition, eventCamera))
                {
                    maxDepthGraphic = graphic;
                    maxDepth = graphicDepth;
                }
            }

            result = maxDepthGraphic;
            return result is not null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            TryGetComponent(out m_Canvas);
        }
#endif
    }
}