// ReSharper disable InconsistentNaming

#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public enum LayoutRebuildTiming { Pre, Post }

    public enum GraphicRebuildTiming { Pre, Post }

    public interface ILayoutRebuildTarget
    {
        void Rebuild(LayoutRebuildTiming timing);
    }

    public interface IGraphicRebuildTarget
    {
        void Rebuild(GraphicRebuildTiming timing);
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// </summary>
    public static class CanvasUpdateRegistry
    {
        private static bool _performingLayoutUpdate;
        private static bool _performingGraphicUpdate;

        private static readonly List<Component> _layoutRebuildQueue = new(); // ILayoutRebuildTarget or Transform.
        private static readonly List<(Component, int Depth)> _layoutBuf = new();
        private static readonly List<MonoBehaviour> _graphicRebuildQueue = new();
        private static readonly List<MonoBehaviour> _graphicBuf = new();

        static CanvasUpdateRegistry() => Canvas.willRenderCanvases += PerformUpdate;

        public static bool IsRebuildingLayout() => _performingLayoutUpdate;
        public static bool IsRebuildingGraphic() => _performingGraphicUpdate;

        private static void PerformUpdate()
        {
            // layout -> cull -> render

            // Perform Layout Rebuild.
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);

            // rebuild queue to buffer and then sort by depth.
            // layout rebuild must be done in depth order.
            _layoutBuf.Clear();
            FlushAndSortByDepth(_layoutRebuildQueue, result: _layoutBuf);

            // rebuild the layout.
            _performingLayoutUpdate = true;
            for (var t = LayoutRebuildTiming.Pre; t <= LayoutRebuildTiming.Post; t++)
            {
                Profiling.Profiler.BeginSample(GetProfilerString_Layout(t));

                foreach (var (layout, _) in _layoutBuf) // element is guaranteed to be non-destroyed here.
                {
                    try
                    {
                        if (layout is Transform trans) // most common case.
                        {
                            if (t is LayoutRebuildTiming.Pre)
                                LayoutRebuilder.RebuildLayoutRootImmediate(trans);
                        }
                        else if (layout is ILayoutRebuildTarget target)
                        {
                            target.Rebuild(t);
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported type for layout rebuild: {layout.GetType()}. Expected ILayoutRebuildTarget or RectTransform.");
                        }
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        L.E($"Exception while rebuilding {layout} for {GetProfilerString_Layout(t)}");
                        L.E(e);
#endif
                    }
                }
                Profiling.Profiler.EndSample();
            }
            _performingLayoutUpdate = false;

            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);


            // now layout is complete do culling...
            Profiling.Profiler.BeginSample("ClipperRegistry.Cull");
            ClipperRegistry.Cull();
            Profiling.Profiler.EndSample();


            // Perform Graphic Rebuild.
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

            _graphicBuf.Clear();
            Flush(_graphicRebuildQueue, result: _graphicBuf);

            _performingGraphicUpdate = true;
            for (var t = GraphicRebuildTiming.Pre; t <= GraphicRebuildTiming.Post; t++)
            {
                Profiling.Profiler.BeginSample(GetProfilerString_Graphic(t));
                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (var graphic in _graphicBuf)
                {
                    try
                    {
                        ((IGraphicRebuildTarget) graphic).Rebuild(t);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        L.E($"Exception while rebuilding {graphic} for {GetProfilerString_Graphic(t)}");
                        L.E(e);
#endif
                    }
                }
                Profiling.Profiler.EndSample();
            }
            _performingGraphicUpdate = false;

            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
            return;

            static void FlushAndSortByDepth(List<Component> source, List<(Component, int Depth)> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                // sort by GetInstanceID(), if the object is destroyed, use 0.
                // It is always unique, and never has the value 0.
                // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.GetInstanceID.html
                source.Sort((a, b) =>
                {
                    var aId = a ? a.GetInstanceID() : 0;
                    var bId = b ? b.GetInstanceID() : 0;
                    return aId - bId;
                });

                var lastId = 0; // 0 is invalid ID.
                foreach (var item in source)
                {
                    if (!item) continue; // skip destroyed components.
                    var id = item.GetInstanceID();
                    if (id == lastId) continue; // skip duplicates.
                    lastId = id; // update lastId to current.

                    Transform t;

                    if (item is Transform t2)
                    {
                        // skip the inactive layout root.
                        if (t2.gameObject.activeInHierarchy is false)
                            continue;

                        t = t2;
                    }
                    else
                    {
                        // skip disabled components.
                        if (((MonoBehaviour) item).isActiveAndEnabled is false)
                            continue;

                        t = item.transform;
                    }

                    result.Add((item, t.CalcParentCount()));
                }

                result.Sort((a, b) => b.Depth - a.Depth);

                source.Clear(); // flush the source list.
            }

            static void Flush(List<MonoBehaviour> source, List<MonoBehaviour> result)
            {
                Assert.IsTrue(result.IsEmpty(), "Result list should be empty before processing.");

                // sort by GetInstanceID(), if the object is destroyed, use 0.
                // It is always unique, and never has the value 0.
                // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.GetInstanceID.html
                source.Sort((a, b) =>
                {
                    var aId = a ? a.GetInstanceID() : 0;
                    var bId = b ? b.GetInstanceID() : 0;
                    return aId - bId;
                });

                var lastId = 0; // 0 is invalid ID.
                foreach (var item in source)
                {
                    if (!item) continue; // skip destroyed components.
                    var id = item.GetInstanceID();
                    if (id == lastId) continue; // skip duplicates.
                    lastId = id; // update lastId to current.
                    if (item.isActiveAndEnabled is false) continue; // skip disabled or never enabled components.
                    result.Add(item);
                }

                source.Clear(); // flush the source list.
            }

            static string GetProfilerString_Layout(LayoutRebuildTiming t) => t switch
            {
                LayoutRebuildTiming.Pre => "CanvasUpdateRegistry.PreLayout",
                LayoutRebuildTiming.Post => "CanvasUpdateRegistry.PostLayout",
                _ => throw new ArgumentOutOfRangeException(nameof(t), t, null)
            };

            static string GetProfilerString_Graphic(GraphicRebuildTiming t) => t switch
            {
                GraphicRebuildTiming.Pre => "CanvasUpdateRegistry.PreRender",
                GraphicRebuildTiming.Post => "CanvasUpdateRegistry.PostRender",
                _ => throw new ArgumentOutOfRangeException(nameof(t), t, null)
            };
        }

        public static void QueueLayout<TLayoutRebuildTarget>(TLayoutRebuildTarget target) where TLayoutRebuildTarget : MonoBehaviour, ILayoutRebuildTarget =>
            DoQueueLayout(target);

        internal static void QueueLayoutRoot(Transform target) => DoQueueLayout(target); // resolved layout root only.

        private static void DoQueueLayout(Component target)
        {
            if (_performingLayoutUpdate)
                L.E($"[CanvasUpdateRegistry] Trying to add {target} for layout rebuild while we are already inside a rebuild loop.");

            _layoutRebuildQueue.Add(target);
        }

        public static void QueueGraphic<TGraphicRebuildTarget>(TGraphicRebuildTarget target)
            where TGraphicRebuildTarget : MonoBehaviour, IGraphicRebuildTarget
        {
            if (_performingGraphicUpdate)
                L.W($"[CanvasUpdateRegistry] Trying to add {target} for graphic rebuild while we are already inside a rebuild loop.");

            _graphicRebuildQueue.Add(target);
        }
    }
}