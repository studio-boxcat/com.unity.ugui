// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    /// <summary>
    /// A Graphic that is capable of being masked out.
    /// </summary>
    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized]
        protected Material? m_MaskMaterial;

        // m_Maskable is whether this graphic is allowed to be masked or not. It has the matching public property maskable.
        // The default for m_Maskable is true, so graphics under a mask are masked out of the box.
        // The maskable property can be turned off from script by the user if masking is not desired.
        // m_IncludeForMasking is whether we actually consider this graphic for masking or not - this is an implementation detail.
        // m_IncludeForMasking should only be true if m_Maskable is true AND a parent of the graphic has an IMask component.
        // Things would still work correctly if m_IncludeForMasking was always true when m_Maskable is, but performance would suffer.
        [SerializeField, HideInInspector]
        private bool m_Maskable;

        /// <summary>
        /// Does this graphic allow masking.
        /// </summary>
        [ShowInInspector, FoldoutGroup("Advanced"), PropertyOrder(1), ShowIf("@CanShow(GraphicPropertyFlag.Maskable)")]
        public bool maskable
        {
            get => m_Maskable;
            set
            {
                if (value.CmpSet(ref m_Maskable) // skip if the value is the same
                    && isActiveAndEnabled) // skip if OnEnable() has not been called yet
                {
                    m_StencilDepth = null;
                    SetMaterialDirty();

                    if (value) ClipperRegistry.RegisterTarget(this);
                    else ClipperRegistry.UnregisterTarget(this);
                }
            }
        }


        /// <summary>
        /// Is this graphic the graphic on the same object as a Mask that is enabled.
        /// </summary>
        /// <remarks>
        /// If toggled ensure to call MaskUtilities.NotifyStencilStateChanged(this); manually as it changes how stenciles are calculated for this image.
        /// </remarks>
        private bool isMaskingGraphic => canvasRenderer.hasPopInstruction; // set by Mask.OnEnable, Mask.OnDisable

        [NonSerialized]
        protected int? m_StencilDepth;

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// </summary>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_StencilDepth.TryGetValue(out var d) is false)
            {
                d = maskable ? MaskUtilities.GetStencilDepth(transform) : 0;
                m_StencilDepth = d;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (d > 0 && !isMaskingGraphic)
            {
                var maskMat = StencilMaterial.AddMaskable(toUse, d);
                if (m_MaskMaterial is not null) StencilMaterial.Remove(m_MaskMaterial); // return the previous mask material if it exists.
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }
            return toUse;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StencilDepth = null;
            SetMaterialDirty();

            // connected mask also be in-effect, need to recalculate the stencil state.
            if (isMaskingGraphic)
                MaskUtilities.NotifyStencilStateChanged(this);

            if (maskable) ClipperRegistry.RegisterTarget(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StencilDepth = null;
            SetMaterialDirty();

            if (m_MaskMaterial is not null)
            {
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = null;
            }

            // connected mask also be disengaged, need to recalculate the stencil state.
            if (isMaskingGraphic)
                MaskUtilities.NotifyStencilStateChanged(this);

            if (maskable) ClipperRegistry.UnregisterTarget(this);
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            OnHierarchChanged();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            OnHierarchChanged();
        }

        private void OnHierarchChanged()
        {
            if (!isActiveAndEnabled) return;
            m_StencilDepth = null;
            SetMaterialDirty();
            if (maskable) ClipperRegistry.ReparentTarget(this);
        }

        /// <summary>
        /// See IMaskable.RecalculateMasking
        /// </summary>
        public void RecalculateMasking()
        {
            // Remove the material reference as either the graphic of the mask has been enable/ disabled.
            // This will cause the material to be repopulated from the original if need be. (case 994413)
            if (m_MaskMaterial is not null)
            {
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = null;
            }

            m_StencilDepth = null;
            SetMaterialDirty();
        }

        Graphic IClippable.Graphic => this;
    }
}