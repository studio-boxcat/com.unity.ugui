using System;
using JetBrains.Annotations;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Mask", 13)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// A component for masking children elements.
    /// </summary>
    /// <remarks>
    /// By using this element any children elements that have masking enabled will mask where a sibling Graphic would write 0 to the stencil buffer.
    /// </remarks>
    public class Mask : UIBehaviour, IMaterialModifier
    {
        [NonSerialized]
        private RectTransform m_RectTransform;
        public RectTransform rectTransform => m_RectTransform ??= (RectTransform) transform;

        [SerializeField]
        private bool m_ShowMaskGraphic = true;

        /// <summary>
        /// Show the graphic that is associated with the Mask render area.
        /// </summary>
        public bool showMaskGraphic
        {
            get => m_ShowMaskGraphic;
            set
            {
                if (m_ShowMaskGraphic == value)
                    return;

                m_ShowMaskGraphic = value;
                if (graphic != null)
                    graphic.SetMaterialDirty();
            }
        }

        [NonSerialized]
        private Graphic m_Graphic;

        /// <summary>
        /// The graphic associated with the Mask.
        /// </summary>
        public Graphic graphic => m_Graphic ??= GetComponent<Graphic>();

        [NonSerialized, CanBeNull]
        Material m_MaskMaterial;
        [NonSerialized, CanBeNull]
        Material m_UnmaskMaterial;

        public bool MaskEnabled() { return IsActive() && graphic != null; }

        protected virtual void OnEnable()
        {
            if (graphic != null)
            {
                graphic.canvasRenderer.hasPopInstruction = true;
                graphic.SetMaterialDirty();

                // Default the graphic to being the maskable graphic if its found.
                if (graphic is MaskableGraphic maskableGraphic)
                    maskableGraphic.isMaskingGraphic = true;
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }

        protected virtual void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            if (graphic != null)
            {
                graphic.SetMaterialDirty();
                graphic.canvasRenderer.hasPopInstruction = false;
                graphic.canvasRenderer.popMaterialCount = 0;

                if (graphic is MaskableGraphic maskableGraphic)
                    maskableGraphic.isMaskingGraphic = false;
            }

            if (m_MaskMaterial is not null)
            {
                StencilMaterial.Remove(m_MaskMaterial);
                StencilMaterial.Remove(m_UnmaskMaterial);
                m_MaskMaterial = null;
                m_UnmaskMaterial = null;
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!IsActive())
                return;

            if (graphic is not null)
            {
                // Default the graphic to being the maskable graphic if its found.
                if (graphic is MaskableGraphic maskableGraphic)
                    maskableGraphic.isMaskingGraphic = true;
                graphic.SetMaterialDirty();
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }
#endif

        /// Stencil calculation time!
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!MaskEnabled())
                return baseMaterial;

            var stencilDepth = MaskUtilities.GetStencilDepth(transform);
            if (stencilDepth >= 8)
            {
                Debug.LogWarning("Attempting to use a stencil mask with depth > 8", gameObject);
                return baseMaterial;
            }

            var renderer = graphic.canvasRenderer;
            var oldMaskMaterial = m_MaskMaterial;
            var oldUnmaskMaterial = m_UnmaskMaterial;

            // if we are at the first level...
            // we want to destroy what is there
            var desiredStencilBit = 1 << stencilDepth;
            if (desiredStencilBit == 1)
            {
                m_MaskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Replace, CompareFunction.Always, m_ShowMaskGraphic ? ColorWriteMask.All : 0);
                m_UnmaskMaterial = StencilMaterial.Add(baseMaterial, 1, StencilOp.Zero, CompareFunction.Always, 0);
            }
            //otherwise we need to be a bit smarter and set some read / write masks
            else
            {
                m_MaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit | (desiredStencilBit - 1), StencilOp.Replace, CompareFunction.Equal, m_ShowMaskGraphic ? ColorWriteMask.All : 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
                m_UnmaskMaterial = StencilMaterial.Add(baseMaterial, desiredStencilBit - 1, StencilOp.Replace, CompareFunction.Equal, 0, desiredStencilBit - 1, desiredStencilBit | (desiredStencilBit - 1));
                renderer.hasPopInstruction = true;
            }

            renderer.popMaterialCount = 1;
            renderer.SetPopMaterial(m_UnmaskMaterial, 0);

            if (oldMaskMaterial is not null)
            {
                StencilMaterial.Remove(oldMaskMaterial);
                StencilMaterial.Remove(oldUnmaskMaterial);
            }

            return m_MaskMaterial;
        }
    }
}