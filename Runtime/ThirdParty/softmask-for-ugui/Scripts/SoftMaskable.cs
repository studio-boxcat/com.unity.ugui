// ReSharper disable InconsistentNaming

#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UISoftMask
{
    internal enum MaskInteraction : byte
    {
        VisibleInside = ((1 << 0) + (1 << 2) + (1 << 4) + (1 << 6)), // 170, 0b01
        VisibleOutside = (1 << 1) + (1 << 3) + (1 << 5) + (1 << 7), // 85, 0b10
    }

    /// <summary>
    /// Soft maskable.
    /// Add this component to Graphic under SoftMask for smooth masking.
    /// </summary>
    public sealed class SoftMaskable : MaterialModifierBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, OnValueChanged("SetMaterialDirty")]
        private MaskInteraction m_MaskInteraction = MaskInteraction.VisibleInside;

        private MaterialLink? _materialLink;
        public Material? modifiedMaterial => _materialLink?.Material;

        public override Material? GetModifiedMaterial(GraphicMaterialKey material)
        {
            if (!isActiveAndEnabled)
            {
                L.W("[SoftMaskable] is not active. Falling back to base: " + this, this);
                return null;
            }

            var softMask = transform.NearestUpwards_GOActiveAndCompEnabled<SoftMask>();
            if (!softMask)
            {
                L.I("[SoftMaskable] No SoftMask in parent hierarchy. Falling back to base: " + this, this);
                return null;
            }

            // Generate soft maskable material.
            var maskRt = softMask!.PopulateMaskRt();
            MaterialCache.Rent(ref _materialLink, material, m_MaskInteraction, maskRt);

            var mat = _materialLink!.Material;
#if UNITY_EDITOR
            // XXX: material properties will be cleared after the scene or prefab is saved.
            if (_materialLink.IsMaterialConfigured() is false)
            {
                if (Editing.No(this)) L.E("[SoftMaskable] Material properties were cleared. Reconfiguring material: " + this, this);
                _materialLink.ConfigureMaterial();
                SoftMaskSceneViewHandler.SetUpGameVP(mat, Graphic);
            }
#endif

            return mat;
        }

        public void SetMaskInteraction(bool visibleInside)
        {
            m_MaskInteraction = visibleInside
                ? MaskInteraction.VisibleInside : MaskInteraction.VisibleOutside;
            SetMaterialDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            SoftMaskSceneViewHandler.Add(this);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_materialLink is not null)
            {
                _materialLink.Release();
                _materialLink = null;
            }

#if UNITY_EDITOR
            SoftMaskSceneViewHandler.Remove(this);
#endif
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = Graphic;
            var material = GraphicMaterialResolver.ResolveKey(g);
            if (MaterialCache.TryResolveShaderIndex(material, out _) is false)
                result.AddError($"Material '{material}' is not supported by SoftMaskable.");

            if (m_MaskInteraction is not (MaskInteraction.VisibleInside or MaskInteraction.VisibleOutside))
                result.AddError($"Invalid mask interaction value: {m_MaskInteraction}.");

            if (this.HasComponentInParent<SoftMask>(includeInactive: true) is false)
                result.AddError("SoftMaskable must be a child of SoftMask component to work properly.");

            if (ComponentSearch.AnyComponentExcept<IMaterialModifier>(this,
                    except1: typeof(Graphic), except2: typeof(SoftMaskable)))
            {
                result.AddError("SoftMaskable should not be used with other IMaterialModifier components " +
                                "except Graphic or SoftMaskable itself. This may cause unexpected behavior.");
            }
        }
#endif
    }
}
