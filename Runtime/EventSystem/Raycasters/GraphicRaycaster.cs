using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.UI.Collections;

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
            // For the component added on runtime, m_Canvas is not set.
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
            if (GraphicRegistry.TryGetRaycastableGraphicsForCanvas(canvas, out var canvasGraphics) == false)
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
        public static bool Raycast([NotNull] Camera eventCamera, Vector2 pointerPosition, IndexedSet<Graphic> foundGraphics, out Graphic result)
        {
            // Necessary for the event system
            Graphic maxDepthGraphic = null;
            int maxDepth = -1; // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
            foreach (var graphic in foundGraphics)
            {
                Assert.IsTrue(graphic.raycastTarget);
                Assert.IsTrue(graphic.isActiveAndEnabled);

                // XXX: foundGraphics should not contain null elements, but it seems to happen in some cases.
                // https://console.firebase.google.com/project/nyan-tower-306804/crashlytics/app/android:com.grapetree.meowtower/issues/3ad89d02972c2c5e2ac0a43fbd494aca?time=last-seven-days&versions=2.3.0%20(265);2.3.0%20(264)&sessionEventKey=648893B4006900014ABB0CF4090C225C_1822653280777558743
                if (graphic == null)
                {
                    Debug.LogError("GraphicRaycaster found a null Graphic in its list during a raycast.");
                    continue;
                }

                var graphicDepth = graphic.depth;
                var t = graphic.rectTransform;

                // If there's hit graphic but not initialized,
                // we should abort the raycast since it could be block the raycast if it's initialized later.
                if (graphicDepth is -1)
                {
                    L.W("Uninitialized Graphic found: " + graphic.name, graphic);
                    if (RectTransformUtility.RectangleContainsScreenPoint(t, pointerPosition, eventCamera, graphic.raycastPadding)
                        && RaycastUtils.IsEligibleForRaycast(t, pointerPosition, eventCamera))
                    {
                        L.W("Aborting raycast since the blocking Graphic is not initialized yet.");
                        result = default;
                        return false;
                    }
                }

                if (graphicDepth <= maxDepth)
                    continue;

                if (graphic.canvasRenderer.cull)
                    continue;

                // Check hit & eligibility.
                if (RectTransformUtility.RectangleContainsScreenPoint(t, pointerPosition, eventCamera, graphic.raycastPadding)
                    && RaycastUtils.IsEligibleForRaycast(t, pointerPosition, eventCamera))
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