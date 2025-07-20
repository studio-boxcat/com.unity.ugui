// ReSharper disable InconsistentNaming

#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public interface IPostLayoutRebuildCallback
    {
        void PostLayoutRebuild();
    }

    public interface IPostGraphicRebuildCallback
    {
        void PostGraphicRebuild();
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// </summary>
    public static class CanvasUpdateRegistry
    {
        private static bool _performingLayoutUpdate;
        private static bool _performingGraphicUpdate;

        private static readonly List<(Transform, int)> _layoutRebuildQueue = new(); // ILayoutRebuildTarget or Transform (layout node).
        private static readonly List<(Graphic, int)> _graphicRebuildQueue = new();
        private static readonly List<(Object, int)> _layoutRebuildCallbacks = new();
        private static readonly List<(Object, int)> _graphicRebuildCallbacks = new();
        private static readonly List<Object> _tempBuf = new();
        private static readonly HashSet<Transform> _visitedBuf = new(RefComparer.Instance);

        static CanvasUpdateRegistry() => Canvas.willRenderCanvases += PerformUpdate;

        public static bool IsRebuildingLayout() => _performingLayoutUpdate;
        public static bool IsRebuildingGraphic() => _performingGraphicUpdate;

        private static void PerformUpdate()
        {
            // layout -> cull -> render

            // Perform Layout Rebuild.
            if (_layoutRebuildQueue.NotEmpty() || _layoutRebuildCallbacks.NotEmpty())
            {
                _performingLayoutUpdate = true;
                UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);

                if (_layoutRebuildQueue.NotEmpty())
                {
                    _tempBuf.Clear();
                    FlushLayoutRoot(_layoutRebuildQueue, result: _tempBuf);
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (Transform layoutRoot in _tempBuf) // element is guaranteed to be non-destroyed here.
                    {
                        try
                        {
                            LayoutRebuilder.RebuildRootImmediate(layoutRoot);
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            L.E($"Exception while rebuilding Layout {layoutRoot}");
                            L.E(e);
#endif
                        }
                    }
                }

                if (_layoutRebuildCallbacks.NotEmpty())
                {
                    _tempBuf.Clear();
                    FlushCallbacks(_layoutRebuildCallbacks, result: _tempBuf);
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (IPostLayoutRebuildCallback callback in _tempBuf)
                    {
                        try
                        {
                            callback.PostLayoutRebuild();
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            L.E($"Exception while executing PostLayoutRebuild for {callback}");
                            L.E(e);
#endif
                        }
                    }
                }

                UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
                _performingLayoutUpdate = false;
            }


            // now layout is complete do culling...
            Profiling.Profiler.BeginSample("ClipperRegistry.Cull");
            ClipperRegistry.Cull();
            Profiling.Profiler.EndSample();


            // Perform Graphic Rebuild.
            if (_graphicRebuildQueue.NotEmpty() || _graphicRebuildCallbacks.NotEmpty())
            {
                _performingGraphicUpdate = true;
                UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

                if (_graphicRebuildQueue.NotEmpty())
                {
                    _tempBuf.Clear();
                    FlushGraphic(_graphicRebuildQueue, result: _tempBuf);
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (Graphic graphic in _tempBuf)
                    {
                        try
                        {
                            graphic.Rebuild();
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            L.E($"Exception while rebuilding Graphic {graphic}");
                            L.E(e);
#endif
                        }
                    }
                }

                if (_graphicRebuildCallbacks.NotEmpty())
                {
                    _tempBuf.Clear();
                    FlushCallbacks(_graphicRebuildCallbacks, result: _tempBuf);
                    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                    foreach (IPostGraphicRebuildCallback callback in _tempBuf)
                    {
                        try
                        {
                            callback.PostGraphicRebuild();
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            L.E($"Exception while executing PostGraphicRebuild for {callback}");
                            L.E(e);
#endif
                        }
                    }
                }

                UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
                _performingGraphicUpdate = false;
            }

            return;

            static void FlushLayoutRoot(List<(Transform, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                PruneDestroyedAndSortByInstanceID(source);

                var orgCount = source.Count;
                var lastId = 0; // 0 is invalid ID.
                for (var index = 0; index < orgCount; index++)
                {
                    var (node, instanceID) = source[index];
                    if (instanceID == lastId) continue; // skip duplicates.
                    lastId = instanceID; // update lastId to current.
                    if (!node) continue; // skip destroyed transforms.

                    // even if the item is not active, we still need to resolve the layout root.
                    // add the item to the source list again to be sorted by depth.
                    source.Add((node, node.CalcParentCount()));
                }

                // remove the original items and then sort by depth.
                source.RemoveRange(0, orgCount);
                source.Sort((a, b) => b.Item2 - a.Item2); // descending order. more parent count (depth) = far from the root.

                // now it's time to resolve the layout root.
                _visitedBuf.Clear();
                foreach (var (node, _) in source)
                {
                    var root = LayoutRebuilder.ResolveUnvisitedLayoutRoot(node, _visitedBuf);
                    if (root is null) continue; // already visited or no root found.
                    if (root.gameObject.activeInHierarchy is false) continue; // skip inactive layout roots.
                    result.Add(root);
                }

                source.Clear(); // clear the source list.
            }

            static void FlushGraphic(List<(Graphic, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                PruneDestroyedAndSortByInstanceID(source);

                var lastId = 0; // 0 is invalid ID.
                foreach (var (graphic, instanceID) in source)
                {
                    if (instanceID == lastId) continue; // skip duplicates.
                    lastId = instanceID; // update lastId to current.
                    if (!graphic) continue; // skip destroyed graphics.
                    if (graphic.isActiveAndEnabled is false) continue; // skip disabled or never enabled components.
                    result.Add(graphic);
                }

                source.Clear(); // flush the source list.
            }

            static void FlushCallbacks(List<(Object, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                PruneDestroyedAndSortByInstanceID(source);

                var lastId = 0; // 0 is invalid ID.
                foreach (var (callback, instanceID) in source)
                {
                    if (instanceID == lastId) continue; // skip duplicates.
                    lastId = instanceID; // update lastId to current.
                    if (!callback) continue; // skip destroyed objects.
                    result.Add(callback);
                }

                source.Clear(); // flush the source list.
            }

            // It is always unique, and never has the value 0.
            // XXX: Instance ID could be reused when the Edit mode exits, the same GameObject in the scene will have the same ID,
            // but the old one will be destroyed.
            // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.GetInstanceID.html
            static void PruneDestroyedAndSortByInstanceID<TObject>(List<(TObject, int)> list) where TObject : Object
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    if (!list[i].Item1) // if the object is destroyed.
                        list.RemoveAt(i); // remove it.
                }

                list.Sort((a, b) => a.Item2 - b.Item2);
            }
        }

        internal static void QueueLayoutNode(Transform target)
        {
            if (_performingLayoutUpdate)
                L.E($"[CanvasUpdateRegistry] Trying to add {target} for layout rebuild while we are already inside a rebuild loop.");

            // root will be resolved by LayoutRebuilder.
            _layoutRebuildQueue.Add((target, target.GetInstanceID()));
        }

        public static void QueueGraphic(Graphic target)
        {
            if (_performingGraphicUpdate)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for graphic rebuild while we are already inside a rebuild loop.");

            _graphicRebuildQueue.Add((target, target.GetInstanceID()));
        }

        public static void QueueLayoutRebuildCallback<TLayoutRebuildTarget>(TLayoutRebuildTarget target)
            where TLayoutRebuildTarget : MonoBehaviour, IPostLayoutRebuildCallback
        {
            if (_performingLayoutUpdate)
                L.E($"[CanvasUpdateRegistry] Trying to add {target} for layout rebuild while we are already inside a rebuild loop.");

            _layoutRebuildCallbacks.Add((target, target.GetInstanceID()));
        }

        public static void QueueGraphicRebuildCallback<TGraphicRebuildTarget>(TGraphicRebuildTarget target)
            where TGraphicRebuildTarget : MonoBehaviour, IPostGraphicRebuildCallback
        {
            if (_performingGraphicUpdate)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for graphic rebuild while we are already inside a rebuild loop.");

            _graphicRebuildCallbacks.Add((target, target.GetInstanceID()));
        }
    }
}