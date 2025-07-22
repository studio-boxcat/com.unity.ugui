// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    /// <summary>
    /// A component for masking children elements.
    /// </summary>
    /// <remarks>
    /// By using this element any children elements that have masking enabled will mask where a sibling Graphic would write 0 to the stencil buffer.
    /// </remarks>
    [AddComponentMenu("UI/Mask", 13)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class Mask : UIBehaviour, IMaterialModifier
    {
        [NonSerialized] private RectTransform? m_RectTransform;
        public RectTransform rectTransform => m_RectTransform ??= (RectTransform) transform;

        [SerializeField, OnValueChanged("SetMaterialDirty")]
        private bool m_ShowMaskGraphic = true;

        /// <summary>
        /// Show the graphic that is associated with the Mask render area.
        /// </summary>
        public bool showMaskGraphic
        {
            get => m_ShowMaskGraphic;
            set
            {
                if (value.CmpSet(ref m_ShowMaskGraphic))
                    graphic.SetMaterialDirty();
            }
        }

        [NonSerialized] private Graphic? m_Graphic;
        public Graphic graphic => m_Graphic ??= GetComponent<Graphic>();

        [NonSerialized] private Material? m_MaskMaterial;
        [NonSerialized] private Material? m_UnmaskMaterial;

        protected virtual void OnEnable()
        {
            var g = graphic;
            g.canvasRenderer.hasPopInstruction = true; // this makes the Graphic.isMaskingGraphic return true.
            g.SetMaterialDirty();
            MaskUtilities.NotifyStencilStateChanged(this);
        }

        protected virtual void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            var g = graphic;
            g.SetMaterialDirty();
            g.canvasRenderer.hasPopInstruction = false; // this makes the Graphic.isMaskingGraphic return true.
            g.canvasRenderer.popMaterialCount = 0;

            if (m_MaskMaterial is not null)
            {
                StencilMaterial.Remove(m_MaskMaterial);
                StencilMaterial.Remove(m_UnmaskMaterial!);
                m_MaskMaterial = null;
                m_UnmaskMaterial = null;
            }

            MaskUtilities.NotifyStencilStateChanged(this);
        }

        private void SetMaterialDirty() => graphic.SetMaterialDirty();

        /// Stencil calculation time!
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!enabled) return baseMaterial; // only check enabled, not isActiveAndEnabled.
            var g = graphic;
            if (g.enabled is false) return baseMaterial; // if the graphic is disabled, the mask also be disabled too.

            var stencilDepth = MaskUtilities.GetStencilDepth(transform);
            if (stencilDepth >= 8)
            {
                Debug.LogWarning("Attempting to use a stencil mask with depth > 8", gameObject);
                return baseMaterial;
            }

            var oldMaskMaterial = m_MaskMaterial;
            var oldUnmaskMaterial = m_UnmaskMaterial;
            (m_MaskMaterial, m_UnmaskMaterial) = StencilMaterial.AddMaskPair(baseMaterial, stencilDepth, m_ShowMaskGraphic);

            // configure the CanvasRenderer to use the mask material.
            var cr = g.canvasRenderer;
            cr.popMaterialCount = 1;
            cr.SetPopMaterial(m_UnmaskMaterial, 0);

            // remove the old mask at last to avoid destroying & creating materials.
            if (oldMaskMaterial is not null)
            {
                StencilMaterial.Remove(oldMaskMaterial);
                StencilMaterial.Remove(oldUnmaskMaterial!);
            }

            return m_MaskMaterial;
        }
    }
}