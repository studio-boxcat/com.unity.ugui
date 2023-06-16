using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public static class QuickRaycast
    {
        static readonly List<RaycasterComparisonData> _raycasterBuffer = new();
        static readonly Dictionary<Camera, bool> _eligibleCameraCache = new(ReferenceEqualityComparer.Object);

        public static bool Raycast(Vector2 screenPosition, Camera targetCamera, out RaycastResult raycastResult)
        {
            EnsureBufferCleared();

            // Raycaster 모으기.
            var modules = RaycasterManager.GetRaycasters();
            foreach (var module in modules)
            {
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
                if (Raycast(raycaster, item, screenPosition, out raycastResult))
                {
                    ClearBuffers();
                    return true;
                }
            }

            raycastResult = default;
            ClearBuffers();
            return false;

            static bool Raycast(
                BaseRaycaster raycaster, RaycasterComparisonData data, Vector2 screenPosition,
                out RaycastResult raycastResult)
            {
                // For GraphicRaycaster, we will call optimized Raycast method.
                if (raycaster is GraphicRaycaster)
                {
                    return RaycastToGraphicRaycaster(raycaster, data.Camera, data.Canvas, screenPosition,
                        out raycastResult);
                }

                return raycaster.Raycast(screenPosition, out raycastResult);
            }
        }

        public static bool RaycastAll(Vector2 screenPosition, out RaycastResult raycastResult)
        {
            return Raycast(screenPosition, null, out raycastResult);
        }

        private static void EnsureBufferCleared()
        {
            if (_raycasterBuffer.Count > 0)
            {
                Debug.LogError("_raycasterBuffer 가 비어있지 않습니다.");
                ClearBuffers();
            }

            if (_eligibleCameraCache.Count > 0)
            {
                Debug.LogError("_eligibleCameraCache 가 비어있지 않습니다.");
                ClearBuffers();
            }
        }

        private static void ClearBuffers()
        {
            foreach (var item in _raycasterBuffer)
                RaycasterComparisonData.Release(item);
            _raycasterBuffer.Clear();
            _eligibleCameraCache.Clear();
        }

        private static bool RaycastToGraphicRaycaster(
            BaseRaycaster raycaster, Camera camera, Canvas canvas, Vector2 screenPosition,
            out RaycastResult raycastResult)
        {
            Assert.AreNotEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            if (graphics == null || graphics.Count == 0)
            {
                raycastResult = default;
                return false;
            }

            if (_eligibleCameraCache.TryGetValue(camera, out var isCameraEligible) == false)
                isCameraEligible = _eligibleCameraCache[camera] = RaycastUtils.IsInside(camera, screenPosition);
            if (isCameraEligible == false)
            {
                raycastResult = default;
                return false;
            }

            if (GraphicRaycaster.Raycast(camera, screenPosition, graphics, out var hitGraphic))
            {
                raycastResult = new RaycastResult(hitGraphic, raycaster, screenPosition);
                return true;
            }

            raycastResult = default;
            return false;
        }

        class RaycasterComparisonData
        {
            static readonly Stack<RaycasterComparisonData> _pool = new();

            public static RaycasterComparisonData Rent(BaseRaycaster raycaster, Camera eventCamera)
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

            public BaseRaycaster Raycaster;
            public Camera Camera;

            Canvas _canvas;
            public Canvas Canvas => _canvas ??= ((GraphicRaycaster) Raycaster).canvas;

            float _cameraDepth = float.NaN;
            int _sortingLayerID = int.MaxValue;
            int _sortingLayerValue = int.MaxValue;
            int _sortingOrder = int.MaxValue;
            Canvas _rootCanvas;
            int _canvasRendererDepth = int.MaxValue;


            public RaycasterComparisonData(BaseRaycaster raycaster, Camera eventCamera)
            {
                Raycaster = raycaster;
                Camera = eventCamera;
            }

            public void Init(BaseRaycaster raycaster, Camera eventCamera)
            {
                Assert.IsNotNull(raycaster);
                Assert.IsNotNull(eventCamera);

                Raycaster = raycaster;
                Camera = eventCamera;
                _canvas = default;
                _cameraDepth = float.NaN;
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

                if (_cameraDepth == other._cameraDepth)
                    throw new Exception("Given cameras are different but have the same depth.");

                result = _cameraDepth < other._cameraDepth ? 1 : -1;
                return true;
            }

            public bool CompareRaycasterType(RaycasterComparisonData other, out int result)
            {
                if (Raycaster is not GraphicRaycaster)
                {
                    Assert.IsTrue(other.Raycaster is GraphicRaycaster);
                    result = 1;
                    return true;
                }

                if (other.Raycaster is not GraphicRaycaster)
                {
                    Assert.IsTrue(Raycaster is GraphicRaycaster);
                    result = -1;
                    return true;
                }

                result = default;
                return false;
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

                // If the cameras are different and the depths are different, compare by depth.
                if (lhs.CompareCameraDepth(rhs, out var compareResult))
                    return compareResult;

                // If the raycasters are different and one is a GraphicRaycaster, compare by raycaster type.
                if (lhs.CompareRaycasterType(rhs, out compareResult))
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