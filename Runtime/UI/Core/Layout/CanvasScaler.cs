#nullable enable
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CanvasScaler : UIBehaviour
    {
        public enum ScreenMatchMode : byte
        {
            Expand = 1,
            Shrink = 2
        }

        [SerializeField]
        [HideIf("Editor_IsWorldCanvas"), OnValueChanged(nameof(Handle))]
        protected ScreenMatchMode m_ScreenMatchMode = ScreenMatchMode.Expand;
        public ScreenMatchMode screenMatchMode => m_ScreenMatchMode;


        // General variables
        [NonSerialized] private Canvas? m_Canvas;
        [NonSerialized] private float m_PrevScaleFactor = 1;


        private void OnEnable()
        {
            m_Canvas ??= GetComponent<Canvas>();
            Handle();
            AddInstance(this);
        }

        private void OnDisable()
        {
            RemoveInstance(this);
        }

        ///<summary>
        ///Method that handles calculations of canvas scaling.
        ///</summary>
        private void Handle()
        {
            Assert.IsNotNull(m_Canvas);

            if (!m_Canvas.isRootCanvas)
                return;

            m_Canvas.referencePixelsPerUnit = RefRes.PPU;

            var scaleFactor = CalcScaleFactor();
            if (scaleFactor.ENq(m_PrevScaleFactor))
            {
                m_Canvas!.scaleFactor = scaleFactor;
                m_PrevScaleFactor = scaleFactor;
            }
        }

        private float CalcScaleFactor()
        {
            var isWorldCanvas = m_Canvas!.renderMode == RenderMode.WorldSpace;
            if (isWorldCanvas) return 1;

            var screenSize = m_Canvas.renderingDisplaySize;
            var scale = screenSize / RefRes.SizeF;
            return m_ScreenMatchMode switch
            {
                ScreenMatchMode.Expand => Mathf.Min(scale.x, scale.y),
                ScreenMatchMode.Shrink => Mathf.Max(scale.x, scale.y),
                _ => 0
            };
        }


        private static List<CanvasScaler>? _instances;

        private static void AddInstance(CanvasScaler instance)
        {
            // If it's first time, register callback to Canvas.preWillRenderCanvases.
            if (_instances is null)
            {
                _instances = new List<CanvasScaler>();

                Canvas.preWillRenderCanvases += static () =>
                {
                    foreach (var inst in _instances)
                    {
                        if (!inst)
                        {
                            Debug.LogError("[CanvasScaler] instance is null.");
                            continue;
                        }

                        inst.Handle();
                    }
                };
            }

            _instances.Add(instance);
        }

        private static void RemoveInstance(CanvasScaler instance)
        {
            Assert.IsNotNull(_instances, "RemoveInstance called but _instances is null.");
            _instances!.RemoveLastRef(instance);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // to prevent prefab stage flickering on domain reload
            if (Editing.Yes(this) && enabled)
                Handle();
        }

        private bool Editor_IsWorldCanvas() => (m_Canvas ??= GetComponent<Canvas>()).renderMode == RenderMode.WorldSpace;
#endif
    }
}
