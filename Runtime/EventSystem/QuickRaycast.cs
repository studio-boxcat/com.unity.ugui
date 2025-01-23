using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEngine.EventSystems
{
    public static class QuickRaycast
    {
        static readonly List<RaycasterComparisonData> _raycasterBuffer = new();

        public static RaycastResultType Raycast(Vector2 screenPosition, Camera targetCamera, out RaycastResult raycastResult)
        {
            // L.I($"Raycast: {screenPosition}, {targetCamera.name}");

            if (_raycasterBuffer.Count is not 0)
            {
                L.E("[QuickRaycast] _raycasterBuffer is not empty.");
                ClearBuffer();
            }

            // Raycaster 모으기.
            var modules = RaycasterManager.GetRaycasters();
            foreach (var module in modules)
            {
                var eventCamera = module.eventCamera;
                if (targetCamera is not null && ReferenceEquals(eventCamera, targetCamera) is false)
                    continue;
                Assert.IsTrue(module.isActiveAndEnabled, $"{module.name} is not active and enabled.");

                _raycasterBuffer.Add(RaycasterComparisonData.Rent(module, eventCamera));
            }

            // If there's no raycaster, return false.
            if (_raycasterBuffer.Count == 0)
            {
                raycastResult = default;
                return RaycastResultType.Miss;
            }

            try
            {
                _raycasterBuffer.Sort(RaycasterComparer.Instance);
            }
            catch (Exception e)
            {
                _raycasterBuffer.Clear(); // XXX: _raycasterBuffer 가 오염되었기 때문에 _pool 로 반환하지 않음.
                L.W(e.InnerException?.Message);
                raycastResult = default;
                return RaycastResultType.Abort;
            }

            if (_raycasterBuffer[^1].RendererDepth is -1)
            {
                L.I("[QuickRaycast] Aborting raycast because there's a CanvasRenderer that is not initialized.");
                raycastResult = default;
                ClearBuffer();
                return RaycastResultType.Abort;
            }

            // Raycaster 를 하나씩 돌면서 Raycast.
            foreach (var item in _raycasterBuffer)
            {
                var raycaster = item.Raycaster;
                var resultType = raycaster.Raycast(screenPosition, out raycastResult);

                // If there's no hit graphic, continue to the next raycaster.
                if (resultType is RaycastResultType.Miss)
                    continue;

                // If one of raycaster have hit graphic, return the result.
                // Otherwise, if the hit graphic is not initialized yet, abort the whole raycast.
                ClearBuffer();
                return resultType;
            }

            raycastResult = default;
            ClearBuffer();
            return RaycastResultType.Miss;

            static void ClearBuffer()
            {
                foreach (var item in _raycasterBuffer)
                    RaycasterComparisonData.Release(item);
                _raycasterBuffer.Clear();
            }
        }

        public static bool RaycastAll(Vector2 screenPosition, out RaycastResult raycastResult)
        {
            return Raycast(screenPosition, null, out raycastResult) is RaycastResultType.Hit;
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
            int _rendererDepth = int.MaxValue;
            public int RendererDepth => _rendererDepth != int.MaxValue ? _rendererDepth : Raycaster.GetComponent<CanvasRenderer>().absoluteDepth;


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
                _rendererDepth = int.MaxValue;
            }

            public override string ToString() => $"{Raycaster.name} ({Camera.name})";

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

                var a = RendererDepth;
                var b = other.RendererDepth;
                if (a != b)
                {
                    compareResult = b.CompareTo(a);
                    return true;
                }
                else
                {
                    Assert.AreEqual(-1, a, "CanvasRenderer is initialized but there's other renderer with the same depth.");
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

                return lhs.Raycaster.GetInstanceID().CompareTo(rhs.Raycaster.GetInstanceID());
            }
        }
    }
}