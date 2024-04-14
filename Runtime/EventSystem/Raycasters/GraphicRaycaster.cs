using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.UI.Collections;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A derived BaseRaycaster to raycast against Graphic elements.
    /// </summary>
    [AddComponentMenu("Event/Graphic Raycaster")]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class GraphicRaycaster : BaseRaycaster
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, ChildGameObjectsOnly, NotNull]
        Canvas m_Canvas;
        public Canvas canvas => m_Canvas;
        public override Camera eventCamera => m_Canvas.worldCamera;

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
        public override RaycastResultType Raycast(Vector2 screenPosition, out RaycastResult result)
        {
            if (GraphicRegistry.TryGetRaycastableGraphicsForCanvas(m_Canvas, out var canvasGraphics) is false)
            {
                result = default;
                return RaycastResultType.Miss;
            }

            var camera = eventCamera; // Property can call Camera.main, so cache the reference
            Assert.IsNotNull(camera);
            Assert.AreNotEqual(RenderMode.ScreenSpaceOverlay, m_Canvas.renderMode);

            var resultType = Raycast(camera, screenPosition, canvasGraphics, out var hitGraphic);
            result = resultType is RaycastResultType.Hit
                ? new RaycastResult(hitGraphic, camera, screenPosition)
                : default;
            return resultType;
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        /// <returns>
        /// Whether the raycast hits any graphics. Null if there's a blocking graphic that hasn't been initialized yet.
        /// </returns>
        static RaycastResultType Raycast([NotNull] Camera eventCamera, Vector2 pointerPosition, IndexedSet<Graphic> foundGraphics, out Graphic result)
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
                    L.E("[GraphicRaycaster] Found a null Graphic in its list during a raycast.");
                    continue;
                }

                var renderer = graphic.canvasRenderer;
                if (renderer.cull)
                    continue;

                var t = graphic.rectTransform;
                var depth = renderer.absoluteDepth;

                // If there's hit graphic but not initialized,
                // we should abort the raycast since it could be block the raycast if it's initialized later.
                if (depth is -1)
                {
                    L.W("[GraphicRaycaster] Uninitialized Graphic found: " + graphic.name, graphic);
                    if (Hit(t, graphic, eventCamera, pointerPosition))
                    {
                        L.W("[GraphicRaycaster] Aborting raycast since the blocking Graphic is not initialized yet.", graphic);
                        result = default;
                        return RaycastResultType.Abort;
                    }
                }

                if (depth <= maxDepth)
                    continue;

                // Check hit & eligibility.
                if (Hit(t, graphic, eventCamera, pointerPosition))
                {
                    maxDepthGraphic = graphic;
                    maxDepth = depth;
                }
            }

            result = maxDepthGraphic;
            return result is not null
                ? RaycastResultType.Hit
                : RaycastResultType.Miss;

            static bool Hit(RectTransform rt, Graphic graphic, Camera eventCamera, Vector2 pointerPosition)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rt, pointerPosition, eventCamera, graphic.raycastPadding)
                       && RaycastUtils.IsEligibleForRaycast(rt, pointerPosition, eventCamera);
            }
        }

#if UNITY_EDITOR
        void Reset() => TryGetComponent(out m_Canvas);

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (m_Canvas == null)
                return;

            if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                result.AddError("RenderMode.ScreenSpaceOverlay is not supported by GraphicRaycaster.");
        }
#endif
    }
}