using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    /// <summary>
    /// A derived BaseRaycaster to raycast against Graphic elements.
    /// </summary>
    public class GraphicRaycaster : BaseRaycaster
    {
        protected const int kNoEventMaskSet = -1;

        /// <summary>
        /// Priority of the raycaster based upon sort order.
        /// </summary>
        /// <returns>
        /// The sortOrder priority.
        /// </returns>
        public override int sortOrderPriority
        {
            get
            {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.sortingOrder;

                return base.sortOrderPriority;
            }
        }

        /// <summary>
        /// Priority of the raycaster based upon render order.
        /// </summary>
        /// <returns>
        /// The renderOrder priority.
        /// </returns>
        public override int renderOrderPriority
        {
            get
            {
                // We need to return the sorting order here as distance will all be 0 for overlay.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas.rootCanvas.renderOrder;

                return base.renderOrderPriority;
            }
        }

        [SerializeField, Required, ChildGameObjectsOnly, NotNull]
        Canvas m_Canvas;

        public Canvas canvas => m_Canvas;

        static List<Graphic> m_RaycastResults = new List<Graphic>();

        protected override void Awake()
        {
            base.Awake();

            if (m_Canvas is null)
            {
                TryGetComponent(out m_Canvas);
                Assert.IsNotNull(m_Canvas);
            }
        }

        /// <summary>
        /// Perform the raycast against the list of graphics associated with the Canvas.
        /// </summary>
        /// <param name="eventData">Current event data</param>
        /// <param name="resultAppendList">List of hit objects to append new results to.</param>
        public override void Raycast(Vector2 screenPosition, List<RaycastResult> resultAppendList)
        {
            var canvasGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return;

            int displayIndex;
            var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference
            Assert.IsNotNull(currentEventCamera);

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                displayIndex = canvas.targetDisplay;
            else
                displayIndex = currentEventCamera.targetDisplay;

            var eventPosition = MultipleDisplayUtilities.RelativeMouseAtScaled(screenPosition);
            if (eventPosition != Vector3.zero)
            {
                // We support multiple display and display identification based on event position.

                int eventDisplayIndex = (int)eventPosition.z;

                // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                if (eventDisplayIndex != displayIndex)
                    return;
            }
            else
            {
                // The multiple display system is not supported on all platforms, when it is not supported the returned position
                // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
                eventPosition = screenPosition;

#if UNITY_EDITOR
                if (Display.activeEditorGameViewTarget != displayIndex)
                    return;
                eventPosition.z = Display.activeEditorGameViewTarget;
#endif

                // We dont really know in which display the event occured. We will process the event assuming it occured in our display.
            }

            // Convert to view space
            Vector2 pos = currentEventCamera.ScreenToViewportPoint(eventPosition);

            // If it's outside the camera's viewport, do nothing
            if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                return;

            m_RaycastResults.Clear();

            Raycast(currentEventCamera, eventPosition, canvasGraphics, m_RaycastResults);

            int totalCount = m_RaycastResults.Count;
            for (var index = 0; index < totalCount; index++)
            {
                var go = m_RaycastResults[index].gameObject;

                {
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        screenPosition = eventPosition,
                        index = resultAppendList.Count,
                        depth = m_RaycastResults[index].depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder,
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        Camera m_EventCameraCache;

        public override Camera eventCamera
        {
            get
            {
                if (m_EventCameraCache is not null)
                {
                    Assert.AreEqual(m_EventCameraCache, m_Canvas.worldCamera);
                    return m_EventCameraCache;
                }

                Assert.IsNotNull(m_Canvas);
                Assert.AreNotEqual(RenderMode.ScreenSpaceOverlay, m_Canvas.renderMode);
                m_EventCameraCache = m_Canvas.worldCamera;
                Assert.IsNotNull(m_EventCameraCache);
                return m_EventCameraCache;
            }
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        [NonSerialized] static readonly List<Graphic> s_SortedGraphics = new List<Graphic>();
        private static void Raycast([NotNull] Camera eventCamera, Vector2 pointerPosition, IList<Graphic> foundGraphics, List<Graphic> results)
        {
            // Necessary for the event system
            int totalCount = foundGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
                    continue;

                if (eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z > eventCamera.farClipPlane)
                    continue;

                if (graphic.Raycast(pointerPosition, eventCamera))
                {
                    s_SortedGraphics.Add(graphic);
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));
            totalCount = s_SortedGraphics.Count;
            for (int i = 0; i < totalCount; ++i)
                results.Add(s_SortedGraphics[i]);

            s_SortedGraphics.Clear();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            TryGetComponent(out m_Canvas);
        }
#endif
    }
}