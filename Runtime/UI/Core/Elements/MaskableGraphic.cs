// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    /// <summary>
    /// A Graphic that is capable of being masked out.
    /// </summary>
    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        [NonSerialized]
        protected bool m_StencilDepthDirty = true;

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

        private bool m_IsMaskingGraphic;

        /// <summary>
        /// Does this graphic allow masking.
        /// </summary>
        [ShowInInspector, FoldoutGroup("Advanced"), PropertyOrder(1), ShowIf("@CanShow(GraphicPropertyFlag.Maskable)")]
        public bool maskable
        {
            get { return m_Maskable; }
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;

                if (!isActiveAndEnabled)
                    return;

                m_StencilDepthDirty = true;
                SetMaterialDirty();

                if (value) ClipperRegistry.RegisterTarget(this);
                else ClipperRegistry.UnregisterTarget(this);
            }
        }


        /// <summary>
        /// Is this graphic the graphic on the same object as a Mask that is enabled.
        /// </summary>
        /// <remarks>
        /// If toggled ensure to call MaskUtilities.NotifyStencilStateChanged(this); manually as it changes how stenciles are calculated for this image.
        /// </remarks>
        public bool isMaskingGraphic
        {
            get => m_IsMaskingGraphic;
            set => m_IsMaskingGraphic = value;
        }

        [NonSerialized]
        protected int m_StencilDepth;

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// </summary>
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_StencilDepthDirty)
            {
                m_StencilDepth = maskable ? MaskUtilities.GetStencilDepth(transform) : 0;
                m_StencilDepthDirty = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (m_StencilDepth > 0 && !isMaskingGraphic)
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilDepth) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilDepth) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }
            return toUse;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_StencilDepthDirty = true;
            SetMaterialDirty();

            if (isMaskingGraphic)
                MaskUtilities.NotifyStencilStateChanged(this);

            if (maskable) ClipperRegistry.RegisterTarget(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_StencilDepthDirty = true;
            SetMaterialDirty();

            if (m_MaskMaterial is not null)
            {
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = null;
            }

            if (isMaskingGraphic)
                MaskUtilities.NotifyStencilStateChanged(this);

            if (maskable) ClipperRegistry.UnregisterTarget(this);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_StencilDepthDirty = true;
        }
#endif

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            if (!isActiveAndEnabled)
                return;

            m_StencilDepthDirty = true;
            SetMaterialDirty();
            if (maskable) ClipperRegistry.ReparentTarget(this);
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();

            if (!isActiveAndEnabled)
                return;

            m_StencilDepthDirty = true;
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

            m_StencilDepthDirty = true;
            SetMaterialDirty();
        }

        Graphic IClippable.Graphic => this;
    }
}