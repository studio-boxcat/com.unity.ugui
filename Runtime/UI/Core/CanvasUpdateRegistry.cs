using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.UI.Collections;

namespace UnityEngine.UI
{
    /// <summary>
    /// Values of 'update' called on a Canvas update.
    /// </summary>
    /// <remarks> If modifying also modify m_CanvasUpdateProfilerStrings to match.</remarks>
    public enum CanvasUpdate
    {
        /// <summary>
        /// Called before layout.
        /// </summary>
        Prelayout = 0,
        /// <summary>
        /// Called for layout.
        /// </summary>
        Layout = 1,
        /// <summary>
        /// Called after layout.
        /// </summary>
        PostLayout = 2,
        /// <summary>
        /// Called before rendering.
        /// </summary>
        PreRender = 3,
        /// <summary>
        /// Called late, before render.
        /// </summary>
        LatePreRender = 4,
        /// <summary>
        /// Max enum value. Always last.
        /// </summary>
        MaxUpdateValue = 5
    }

    /// <summary>
    /// This is an element that can live on a Canvas.
    /// </summary>
    public interface ICanvasElement
    {
        /// <summary>
        /// Rebuild the element for the given stage.
        /// </summary>
        /// <param name="executing">The current CanvasUpdate stage being rebuild.</param>
        void Rebuild(CanvasUpdate executing);

        /// <summary>
        /// Get the transform associated with the ICanvasElement.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Callback sent when this ICanvasElement has completed layout.
        /// </summary>
        void LayoutComplete();

        /// <summary>
        /// Callback sent when this ICanvasElement has completed Graphic rebuild.
        /// </summary>
        void GraphicUpdateComplete();

        /// <summary>
        /// Used if the native representation has been destroyed.
        /// </summary>
        /// <returns>Return true if the element is considered destroyed.</returns>
        bool IsDestroyed();
    }

    /// <summary>
    /// A place where CanvasElements can register themselves for rebuilding.
    /// </summary>
    public class CanvasUpdateRegistry
    {
        private static CanvasUpdateRegistry s_Instance;

        private bool m_PerformingLayoutUpdate;
        private bool m_PerformingGraphicUpdate;

        // This list matches the CanvasUpdate enum above. Keep in sync
        private string[] m_CanvasUpdateProfilerStrings = new string[] { "CanvasUpdate.Prelayout", "CanvasUpdate.Layout", "CanvasUpdate.PostLayout", "CanvasUpdate.PreRender", "CanvasUpdate.LatePreRender" };
        private const string m_CullingUpdateProfilerString = "ClipperRegistry.Cull";

        private readonly IndexedSet<ICanvasElement> m_LayoutRebuildQueue = new();
        private readonly IndexedSet<ICanvasElement> m_GraphicRebuildQueue = new();
        static readonly List<ICanvasElement> _canvasElementsBuf = new();
        static readonly List<(ICanvasElement Element, int Depth)> _canvasElementsBufForSort = new();

        protected CanvasUpdateRegistry()
        {
            Canvas.willRenderCanvases += PerformUpdate;
        }

        /// <summary>
        /// Get the singleton registry instance.
        /// </summary>
        public static CanvasUpdateRegistry instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new CanvasUpdateRegistry();
                return s_Instance;
            }
        }

        static void CleanInvalidItemsForLayout(List<ICanvasElement> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    list.RemoveAt(i);
                    item.LayoutComplete();
                }
            }
        }

        static void CleanInvalidItemsForGraphicUpdate(List<ICanvasElement> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (item.IsDestroyed())
                {
                    list.RemoveAt(i);
                    item.GraphicUpdateComplete();
                }
            }
        }

        private void PerformUpdate()
        {
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);

            m_PerformingLayoutUpdate = true;

            _canvasElementsBuf.Clear();
            m_LayoutRebuildQueue.Flush(_canvasElementsBuf);
            CleanInvalidItemsForLayout(_canvasElementsBuf);

            _canvasElementsBufForSort.Clear();
            foreach (var canvasElement in _canvasElementsBuf)
                _canvasElementsBufForSort.Add((canvasElement, ParentCount(canvasElement.transform)));
            _canvasElementsBufForSort.Sort((a, b) => a.Depth - b.Depth);

            for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
            {
                Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);

                foreach (var rebuild in _canvasElementsBufForSort)
                {
                    try
                    {
                        rebuild.Element.Rebuild((CanvasUpdate) i);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Debug.LogException(e);
#endif
                    }
                }
                Profiling.Profiler.EndSample();
            }

            foreach (var element in _canvasElementsBuf)
                element.LayoutComplete();

            m_PerformingLayoutUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
            UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Render);

            // now layout is complete do culling...
            Profiling.Profiler.BeginSample(m_CullingUpdateProfilerString);
            ClipperRegistry.instance.Cull();
            Profiling.Profiler.EndSample();

            m_PerformingGraphicUpdate = true;

            _canvasElementsBuf.Clear();
            m_GraphicRebuildQueue.Flush(_canvasElementsBuf);
            CleanInvalidItemsForGraphicUpdate(_canvasElementsBuf);

            for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
            {
                Profiling.Profiler.BeginSample(m_CanvasUpdateProfilerStrings[i]);
                foreach (var element in _canvasElementsBuf)
                {
                    try
                    {
                        element.Rebuild((CanvasUpdate)i);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Debug.LogException(e);
#endif
                    }
                }
                Profiling.Profiler.EndSample();
            }

            foreach (var element in _canvasElementsBuf)
                element.GraphicUpdateComplete();

            m_PerformingGraphicUpdate = false;
            UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Render);
        }

        private static int ParentCount(Transform child)
        {
            var parent = child.parent;
            int count = 0;
            while (parent is not null)
            {
                count++;
                parent = parent.parent;
            }
            return count;
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        /// <summary>
        /// Try and add the given element to the layout rebuild list.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        /// <returns>
        /// True if the element was successfully added to the rebuilt list.
        /// False if either already inside a Graphic Update loop OR has already been added to the list.
        /// </returns>
        public static bool TryRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            return instance.InternalRegisterCanvasElementForLayoutRebuild(element);
        }

        private bool InternalRegisterCanvasElementForLayoutRebuild(ICanvasElement element)
        {
            /* TODO: this likely should be here but causes the error to show just resizing the game view (case 739376)
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to add {0} for layout rebuild while we are already inside a layout rebuild loop. This is not supported.", element));
                return false;
            }*/

            return m_LayoutRebuildQueue.TryAdd(element);
        }

        /// <summary>
        /// Try and add the given element to the rebuild list.
        /// Will not return if successfully added.
        /// </summary>
        /// <param name="element">The element that is needing rebuilt.</param>
        public static void RegisterCanvasElementForGraphicRebuild([NotNull] ICanvasElement element)
        {
            instance.InternalRegisterCanvasElementForGraphicRebuild(element);
        }

        private void InternalRegisterCanvasElementForGraphicRebuild([NotNull] ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
                Debug.LogError(string.Format("Trying to add {0} for graphic rebuild while we are already inside a graphic rebuild loop. This is not supported.", element));
            m_GraphicRebuildQueue.TryAdd(element);
        }

        /// <summary>
        /// Remove the given element from both the graphic and the layout rebuild lists.
        /// </summary>
        /// <param name="element"></param>
        public static void UnRegisterCanvasElementForRebuild([NotNull] ICanvasElement element)
        {
            instance.InternalUnRegisterCanvasElementForLayoutRebuild(element);
            instance.InternalUnRegisterCanvasElementForGraphicRebuild(element);
        }

        private void InternalUnRegisterCanvasElementForLayoutRebuild([NotNull] ICanvasElement element)
        {
            if (m_PerformingLayoutUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }

            element.LayoutComplete();
            instance.m_LayoutRebuildQueue.TryRemove(element);
        }

        private void InternalUnRegisterCanvasElementForGraphicRebuild(ICanvasElement element)
        {
            if (m_PerformingGraphicUpdate)
            {
                Debug.LogError(string.Format("Trying to remove {0} from rebuild list while we are already inside a rebuild loop. This is not supported.", element));
                return;
            }
            element.GraphicUpdateComplete();
            instance.m_GraphicRebuildQueue.TryRemove(element);
        }

        /// <summary>
        /// Are graphics layouts currently being calculated..
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.Prelayout, CanvasUpdate.Layout or CanvasUpdate.Postlayout</returns>
        public static bool IsRebuildingLayout()
        {
            return instance.m_PerformingLayoutUpdate;
        }

        /// <summary>
        /// Are graphics currently being rebuild.
        /// </summary>
        /// <returns>True if the rebuild loop is CanvasUpdate.PreRender or CanvasUpdate.Render</returns>
        public static bool IsRebuildingGraphics()
        {
            return instance.m_PerformingGraphicUpdate;
        }
    }
}
