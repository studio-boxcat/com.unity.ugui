#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

// ReSharper disable InconsistentNaming
// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

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
        private enum Phase : byte
        {
            Idle,
            LayoutUpdate,
            LayoutCallback,
            Cull,
            GraphicUpdate,
            GraphicCallback,
        }

        private static Phase _phase = Phase.Idle;

        private static readonly List<(Object, int)> _layoutRebuildQueue = new(); // ILayoutRebuildTarget or Transform (layout node).
        private static readonly List<(Object, int)> _graphicRebuildQueue = new(); // Graphic
        private static readonly List<(Object, int)> _layoutRebuildCallbacks = new(); // MonoBehaviour, IPostLayoutRebuildCallback
        private static readonly List<(Object, int)> _graphicRebuildCallbacks = new(); // MonoBehaviour, IPostGraphicRebuildCallback
        private static readonly List<Object> _tempBuf = new();
        private static readonly HashSet<Transform> _visitedBuf = new(RefComparer.Instance);

        static CanvasUpdateRegistry() => Canvas.willRenderCanvases += PerformUpdate;

        public static bool IsIdle() => _phase is Phase.Idle;
        public static bool IsRebuildingLayout() => _phase is Phase.LayoutUpdate;

        public static void PerformUpdate()
        {
            // layout -> cull -> render

            // Perform Layout Rebuild.
            if (_layoutRebuildQueue.NotEmpty() || _layoutRebuildCallbacks.NotEmpty())
            {
                UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);

                _phase = Phase.LayoutUpdate;
                _tempBuf.Clear();
                if (_layoutRebuildQueue.NotEmpty())
                    FlushLayoutRoot(_layoutRebuildQueue, result: _tempBuf);
                Assert.IsTrue(_layoutRebuildQueue.IsEmpty());
                if (_tempBuf.NotEmpty())
                {
                    L.I($"[CanvasUpdateRegistry] Rebuilding Layout Roots: {_tempBuf.Count.Strm()}");

                    foreach (Transform layoutRoot in _tempBuf) // element is guaranteed to be non-destroyed here.
                    {
                        try
                        {
                            LayoutRebuilder.RebuildRootImmediate(layoutRoot);
                        }
                        catch (Exception e)
                        {
                            DebugException($"[CanvasUpdateRegistry] Exception while rebuilding Layout {layoutRoot}", e);
                        }
                    }
                }


                _phase = Phase.LayoutCallback;
                _tempBuf.Clear();
                if (_layoutRebuildCallbacks.NotEmpty())
                    FlushCallbacks(_layoutRebuildCallbacks, result: _tempBuf);
                Assert.IsTrue(_layoutRebuildCallbacks.IsEmpty());
                if (_tempBuf.NotEmpty())
                {
                    L.I($"[CanvasUpdateRegistry] Executing PostLayoutRebuildCallbacks: {_tempBuf.Count.Strm()}");

                    foreach (IPostLayoutRebuildCallback callback in _tempBuf)
                    {
                        try
                        {
                            callback.PostLayoutRebuild();
                        }
                        catch (Exception e)
                        {
                            DebugException($"[CanvasUpdateRegistry] Exception while executing PostLayoutRebuild for {callback}", e);
                        }
                    }
                }

                UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
            }


            // now layout is complete do culling...
            try
            {
                Profiling.Profiler.BeginSample("ClipperRegistry.Cull");
                _phase = Phase.Cull;
                ClipperRegistry.Cull();
            }
            catch (Exception e)
            {
                DebugException("[CanvasUpdateRegistry] Exception during culling", e);
            }
            finally
            {
                Profiling.Profiler.EndSample();
            }


            // Perform Graphic Rebuild.
            if (_graphicRebuildQueue.NotEmpty() || _graphicRebuildCallbacks.NotEmpty())
            {
                UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

                _phase = Phase.GraphicUpdate;
                _tempBuf.Clear();
                if (_graphicRebuildQueue.NotEmpty())
                    FlushGraphic(_graphicRebuildQueue, result: _tempBuf);
                if (_tempBuf.NotEmpty())
                {
                    foreach (Graphic graphic in _tempBuf)
                    {
                        try
                        {
                            graphic.Rebuild();
                        }
                        catch (Exception e)
                        {
                            DebugException($"[CanvasUpdateRegistry] Exception while rebuilding Graphic {graphic}", e);
                        }
                    }
                }


                _phase = Phase.GraphicCallback;
                _tempBuf.Clear();
                if (_graphicRebuildCallbacks.NotEmpty())
                    FlushCallbacks(_graphicRebuildCallbacks, result: _tempBuf);
                if (_tempBuf.NotEmpty())
                {
                    foreach (IPostGraphicRebuildCallback callback in _tempBuf)
                    {
                        try
                        {
                            callback.PostGraphicRebuild();
                        }
                        catch (Exception e)
                        {
                            DebugException($"[CanvasUpdateRegistry] Exception while executing PostGraphicRebuild for {callback}", e);
                        }
                    }
                }

                UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
            }


            _phase = Phase.Idle;
            return;


            static void FlushLayoutRoot(List<(Object, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                CanvasUtils.PruneDestroyedAndDedup(source);

                // now it's time to resolve the layout root.
                _visitedBuf.Clear();
                foreach (var (node, _) in source)
                {
                    var root = LayoutRebuilder.ResolveUnvisitedLayoutRoot((Transform) node, _visitedBuf);
                    if (root is null) continue; // already visited or no root found.
                    if (root.gameObject.activeInHierarchy) // skip inactive layout roots.
                        result.Add(root);
                }

                source.Clear(); // clear the source list.
            }

            static void FlushGraphic(List<(Object, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                CanvasUtils.PruneDestroyedAndDedup(source);

                foreach (var (graphic, _) in source)
                {
                    if (((Graphic) graphic).isActiveAndEnabled) // skip disabled or never enabled components.
                        result.Add(graphic);
                }

                source.Clear(); // flush the source list.
            }

            static void FlushCallbacks(List<(Object, int)> source, List<Object> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                CanvasUtils.PruneDestroyedAndDedup(source);

                foreach (var (callback, _) in source)
                {
                    if (((MonoBehaviour) callback).isActiveAndEnabled) // skip disabled or never enabled components.
                        result.Add(callback);
                }

                source.Clear(); // flush the source list.
            }
        }

        internal static void QueueLayoutNode(Transform target)
        {
            if (_phase is Phase.LayoutUpdate)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for layout rebuild while we are already inside a rebuild loop.");

            // root will be resolved by LayoutRebuilder.
            _layoutRebuildQueue.Add((target, target.GetInstanceID()));
        }

        public static void QueueGraphic(Graphic target)
        {
            if (_phase is Phase.GraphicUpdate)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for graphic rebuild while we are already inside a rebuild loop.");

            _graphicRebuildQueue.Add((target, target.GetInstanceID()));
        }

        public static void QueueLayoutRebuildCallback<TLayoutRebuildTarget>(TLayoutRebuildTarget target)
            where TLayoutRebuildTarget : MonoBehaviour, IPostLayoutRebuildCallback
        {
            if (_phase is Phase.LayoutCallback)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for layout rebuild while we are already inside a rebuild loop.");

            _layoutRebuildCallbacks.Add((target, target.GetInstanceID()));
        }

        public static void QueueGraphicRebuildCallback<TGraphicRebuildTarget>(TGraphicRebuildTarget target)
            where TGraphicRebuildTarget : MonoBehaviour, IPostGraphicRebuildCallback
        {
            if (_phase is Phase.GraphicCallback)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for graphic rebuild while we are already inside a rebuild loop.");

            _graphicRebuildCallbacks.Add((target, target.GetInstanceID()));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugException(string message, Exception e)
        {
            L.E(message);
            L.E(e);
        }
    }
}