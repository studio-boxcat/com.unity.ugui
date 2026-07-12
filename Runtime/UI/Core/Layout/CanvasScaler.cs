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
        [SerializeField]
        [OnValueChanged(nameof(Handle))]
        protected float m_ReferencePixelsPerUnit = 100;

        [SerializeField]
        [HideIf("Editor_IsWorldCanvas"), OnValueChanged(nameof(Handle))]
        protected Vector2 m_ReferenceResolution = new(1080, 1920); // = RefRes.SizeF
        public Vector2 referenceResolution => m_ReferenceResolution;

        public enum ScreenMatchMode
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
        [NonSerialized] private float m_PrevReferencePixelsPerUnit = 100;


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

            var isWorldCanvas = m_Canvas.renderMode == RenderMode.WorldSpace;
            var scaleFactor = isWorldCanvas ? 1 : CalcUIScaleFactor();
            if (scaleFactor.ENq(m_PrevScaleFactor))
            {
                m_Canvas!.scaleFactor = scaleFactor;
                m_PrevScaleFactor = scaleFactor;
            }

            if (m_ReferencePixelsPerUnit.ENq(m_PrevReferencePixelsPerUnit))
            {
                m_Canvas.referencePixelsPerUnit = m_ReferencePixelsPerUnit;
                m_PrevReferencePixelsPerUnit = m_ReferencePixelsPerUnit;
            }
        }

        private float CalcUIScaleFactor()
        {
            var screenSize = m_Canvas.renderingDisplaySize;
            var scale = screenSize / m_ReferenceResolution;
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
        private bool Editor_IsWorldCanvas() => (m_Canvas ??= GetComponent<Canvas>()).renderMode == RenderMode.WorldSpace;
#endif
    }
}
