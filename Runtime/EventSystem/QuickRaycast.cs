using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public static class QuickRaycast
    {
        static readonly List<RaycasterComparisonData> _raycasterBuffer = new();
        static readonly Dictionary<Camera, (Vector2, int)?> _eventPositionCache = new();

        public static bool Raycast(Vector2 screenPosition, Camera targetCamera, out RaycastResult raycastResult)
        {
            EnsureBufferCleared();

            // Raycaster 모으기.
            var modules = RaycasterManager.GetRaycasters();
            foreach (GraphicRaycaster module in modules)
            {
                if (!module.IsActive()) continue;

                var eventCamera = module.eventCamera;
                if (targetCamera is not null && ReferenceEquals(eventCamera, targetCamera) == false)
                    continue;

                _raycasterBuffer.Add(RaycasterComparisonData.Rent(module, eventCamera));
            }

            try
            {
                _raycasterBuffer.Sort(RaycasterComparer.Instance);
            }
            catch (Exception e)
            {
                _raycasterBuffer.Clear(); // XXX: _raycasterBuffer 가 오염되었기 때문에 _pool 로 반환하지 않음.
                Debug.LogWarning(e.InnerException.Message);
                raycastResult = default;
                return false;
            }

            // Raycaster 를 하나씩 돌면서 Raycast.
            foreach (var item in _raycasterBuffer)
            {
                var raycaster = item.Raycaster;
                var eventCamera = item.Camera;

                Assert.AreNotEqual(RenderMode.ScreenSpaceOverlay, item.Canvas.renderMode);
                var graphics = GraphicRegistry.GetGraphicsForCanvas(item.Canvas);
                if (graphics == null || graphics.Count == 0)
                    continue;

                if (_eventPositionCache.TryGetValue(eventCamera, out var eventPositionValue) == false)
                    eventPositionValue = _eventPositionCache[eventCamera] = CalculateEventPosition(eventCamera, screenPosition);
                if (eventPositionValue == null)
                    continue;

                var (eventPosition, displayIndex) = eventPositionValue.Value;
                if (GraphicRaycaster.Raycast(eventCamera, eventPosition, graphics, out var hitGraphic))
                {
                    raycastResult = new RaycastResult(hitGraphic, raycaster, displayIndex, eventPosition);
                    ClearBuffers();
                    return true;
                }
            }

            raycastResult = default;
            ClearBuffers();
            return false;
        }

        public static bool RaycastAll(Vector2 screenPosition, out RaycastResult raycastResult)
        {
            return Raycast(screenPosition, null, out raycastResult);
        }

        static void EnsureBufferCleared()
        {
            if (_raycasterBuffer.Count > 0)
            {
                Debug.LogError("_raycasterBuffer 가 비어있지 않습니다.");
                ClearBuffers();
            }

            if (_eventPositionCache.Count > 0)
            {
                Debug.LogError("_eventPositionCache 가 비어있지 않습니다.");
                ClearBuffers();
            }
        }

        static void ClearBuffers()
        {
            foreach (var item in _raycasterBuffer)
                RaycasterComparisonData.Release(item);
            _raycasterBuffer.Clear();
            _eventPositionCache.Clear();
        }

        static (Vector2, int)? CalculateEventPosition(Camera currentEventCamera, Vector2 screenPosition)
        {
            var displayIndex = currentEventCamera.targetDisplay;

            var eventPosition = MultipleDisplayUtilities.RelativeMouseAtScaled(screenPosition);
            if (eventPosition != Vector3.zero)
            {
                // We support multiple display and display identification based on event position.

                int eventDisplayIndex = (int) eventPosition.z;

                // Discard events that are not part of this display so the user does not interact with multiple displays at once.
                if (eventDisplayIndex != displayIndex)
                    return null;
            }
            else
            {
                // The multiple display system is not supported on all platforms, when it is not supported the returned position
                // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
                eventPosition = screenPosition;

#if UNITY_EDITOR
                if (Display.activeEditorGameViewTarget != displayIndex)
                    return null;
                eventPosition.z = Display.activeEditorGameViewTarget;
#endif

                // We dont really know in which display the event occured. We will process the event assuming it occured in our display.
            }

            // Convert to view space
            Vector2 pos = currentEventCamera.ScreenToViewportPoint(eventPosition);

            // If it's outside the camera's viewport, do nothing
            if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
                return null;

            return (eventPosition, displayIndex);
        }

        class RaycasterComparisonData
        {
            static readonly Stack<RaycasterComparisonData> _pool = new();

            public static RaycasterComparisonData Rent(GraphicRaycaster raycaster, Camera eventCamera)
            {
                if (_pool.TryPop(out var data))
                {
                    data.Init(raycaster, eventCamera);
                    return data;
                }
                else
                {
                    return new RaycasterComparisonData(raycaster, eventCamera);
                }
            }

            public static void Release(RaycasterComparisonData data)
            {
                Assert.IsFalse(_pool.Contains(data));
                _pool.Push(data);
            }

            public GraphicRaycaster Raycaster;
            public Camera Camera;

            Canvas _canvas;
            public Canvas Canvas => _canvas ??= Raycaster.canvas;

            float _cameraDepth = float.NaN;
            int _sortOrderPriority = int.MaxValue;
            int _renderOrderPriority = int.MaxValue;
            int _sortingLayerID = int.MaxValue;
            int _sortingLayerValue = int.MaxValue;
            int _sortingOrder = int.MaxValue;
            Canvas _rootCanvas;
            int _canvasRendererDepth = int.MaxValue;


            public RaycasterComparisonData(GraphicRaycaster raycaster, Camera eventCamera)
            {
                Raycaster = raycaster;
                Camera = eventCamera;
            }

            public void Init(GraphicRaycaster raycaster, Camera eventCamera)
            {
                Assert.IsNotNull(raycaster);
                Assert.IsNotNull(eventCamera);

                Raycaster = raycaster;
                Camera = eventCamera;
                _canvas = default;
                _cameraDepth = float.NaN;
                _sortOrderPriority = int.MaxValue;
                _renderOrderPriority = int.MaxValue;
                _sortingLayerID = int.MaxValue;
                _sortingLayerValue = int.MaxValue;
                _sortingOrder = int.MaxValue;
                _rootCanvas = default;
                _canvasRendererDepth = int.MaxValue;
            }

            public bool CompareCameraDepth(RaycasterComparisonData other, out int result)
            {
                if (ReferenceEquals(Camera, other.Camera))
                {
                    result = default;
                    return false;
                }

                if (_cameraDepth == float.NaN)
                    _cameraDepth = Camera.depth;
                if (other._cameraDepth == float.NaN)
                    other._cameraDepth = other.Camera.depth;

                if (_cameraDepth != other._cameraDepth)
                {
                    result = _cameraDepth < other._cameraDepth ? 1 : -1;
                    return true;
                }

                result = default;
                return false;
            }

            public bool CompareSortOrderPriority(RaycasterComparisonData other, out int compareResult)
            {
                if (_sortOrderPriority == int.MaxValue)
                    _sortOrderPriority = Raycaster.sortOrderPriority;
                if (other._sortOrderPriority == int.MaxValue)
                    other._sortOrderPriority = other.Raycaster.sortOrderPriority;

                if (_sortOrderPriority != other._sortOrderPriority)
                {
                    compareResult = other._sortOrderPriority.CompareTo(_sortOrderPriority);
                    return true;
                }
                else
                {
                    compareResult = default;
                    return false;
                }
            }

            public bool CompareRenderOrderPriority(RaycasterComparisonData other, out int compareResult)
            {
                if (_renderOrderPriority == int.MaxValue)
                    _renderOrderPriority = Raycaster.renderOrderPriority;
                if (other._renderOrderPriority == int.MaxValue)
                    other._renderOrderPriority = other.Raycaster.renderOrderPriority;

                if (_renderOrderPriority != other._renderOrderPriority)
                {
                    compareResult = other._renderOrderPriority.CompareTo(_renderOrderPriority);
                    return true;
                }
                else
                {
                    compareResult = default;
                    return false;
                }
            }

            public bool CompareSortingLayerValue(RaycasterComparisonData other, out int result)
            {
                if (_sortingLayerID == int.MaxValue)
                    _sortingLayerID = Canvas.sortingLayerID;
                if (other._sortingLayerID == int.MaxValue)
                    other._sortingLayerID = other.Canvas.sortingLayerID;

                if (_sortingLayerID == other._sortingLayerID)
                {
                    result = default;
                    return false;
                }

                if (_sortingLayerValue == float.NaN)
                    _sortingLayerValue = SortingLayer.GetLayerValueFromID(_sortingLayerID);
                if (other._sortingLayerValue == float.NaN)
                    other._sortingLayerValue = SortingLayer.GetLayerValueFromID(other._sortingLayerID);

                if (_sortingLayerValue != other._sortingLayerValue)
                {
                    // Uses the layer value to properly compare the relative order of the layers.
                    result = other._sortingLayerValue.CompareTo(_sortingLayerValue);
                    return true;
                }

                result = default;
                return false;
            }

            public bool CompareSortingOrder(RaycasterComparisonData other, out int compareResult)
            {
                if (_sortingOrder == int.MaxValue)
                    _sortingOrder = Canvas.sortingOrder;
                if (other._sortingOrder == int.MaxValue)
                    other._sortingOrder = other.Canvas.sortingOrder;

                if (_sortingOrder != other._sortingOrder)
                {
                    compareResult = other._sortingOrder.CompareTo(_sortingOrder);
                    return true;
                }
                else
                {
                    compareResult = default;
                    return false;
                }
            }

            public bool CompareCanvasRendererDepth(RaycasterComparisonData other, out int compareResult)
            {
                _rootCanvas ??= Canvas.rootCanvas;
                other._rootCanvas ??= other.Canvas.rootCanvas;

                if (ReferenceEquals(_rootCanvas, other._rootCanvas) == false)
                {
                    compareResult = default;
                    return false;
                }

                var canvasRenderer = Canvas.GetComponent<CanvasRenderer>();
                var otherCanvasRenderer = other.Canvas.GetComponent<CanvasRenderer>();
                if (_canvasRendererDepth == int.MaxValue)
                    _canvasRendererDepth = canvasRenderer.absoluteDepth;
                if (other._canvasRendererDepth == int.MaxValue)
                    other._canvasRendererDepth = otherCanvasRenderer.absoluteDepth;

                if (_canvasRendererDepth != other._canvasRendererDepth)
                {
                    compareResult = other._canvasRendererDepth.CompareTo(_canvasRendererDepth);
                    return true;
                }
                else
                {
                    compareResult = default;
                    return false;
                }
            }
        }

        class RaycasterComparer : IComparer<RaycasterComparisonData>
        {
            public static readonly RaycasterComparer Instance = new();

            public int Compare(RaycasterComparisonData lhs, RaycasterComparisonData rhs)
            {
                Assert.AreNotEqual(lhs.Raycaster, rhs.Raycaster);

                // 카메라가 다르고 뎁스도 다른 경우, 뎁스로 비교.
                if (lhs.CompareCameraDepth(rhs, out var compareResult))
                    return compareResult;

                // 아래부터는 카메라가 동일하거나 뎁스가 동일한 경우.
                if (lhs.CompareSortOrderPriority(rhs, out compareResult))
                    return compareResult;
                if (lhs.CompareRenderOrderPriority(rhs, out compareResult))
                    return compareResult;

                var lhsCanvas = lhs.Canvas;
                var rhsCanvas = rhs.Canvas;
                Assert.AreNotEqual(lhsCanvas, rhsCanvas);

                if (lhs.CompareSortingLayerValue(rhs, out compareResult))
                    return compareResult;
                if (lhs.CompareSortingOrder(rhs, out compareResult))
                    return compareResult;
                if (lhs.CompareCanvasRendererDepth(rhs, out compareResult))
                    return compareResult;

                throw new Exception($"두 Raycaster 의 순서를 비교할 수 없습니다: {lhs.Raycaster.name}, {rhs.Raycaster.name}");
            }
        }
    }
}