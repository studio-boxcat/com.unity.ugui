#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public class Maskable : MaterialModifierBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [NonSerialized, ShowInInspector, ReadOnly]
        private Material? _baseMaterial;
        [NonSerialized, ShowInInspector, ReadOnly]
        private Material? _maskMaterial;

        private void OnDestroy()
        {
            if (_maskMaterial is not null)
            {
                StencilMaterial.RemoveMaskable(_maskMaterial);
                _maskMaterial = null;
                _baseMaterial = null;
            }
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            // We don't support multiple masking levels, so only the first mask will work.
            // This means we don't need to the mask component changed or not,
            // as material remains the same as long as the base material is the same.

            // If the graphic is not enabled, return the base material.
            // don't invalidate the mask material, as it will be used when the graphic is enabled again.
            if (enabled is false)
                return baseMaterial;

            // if the baseMaterial is not changed, return the cached mask material.
            var unchanged = _baseMaterial.RefEq(baseMaterial);
            if (unchanged)
            {
#if DEBUG
                StencilMaterial.ConfigureRenderMaterialForDebug(_maskMaterial!);
#endif
                return _maskMaterial!;
            }
            _baseMaterial = baseMaterial; // update base material reference

            // invalidate cached mask material first.
            if (_maskMaterial is not null)
            {
                StencilMaterial.RemoveMaskable(_maskMaterial);
                _maskMaterial = null;
            }

            // create a new mask material. (StencilMaterial is pooling materials, so need to worry about leaks)
            return _maskMaterial = StencilMaterial.AddMaskable(baseMaterial); // hardcoded to 1, as we don't support multiple masks.
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (this.HasComponentInParent<Mask>(includeInactive: true) is false)
                result.AddError($"{nameof(Maskable)} has no parent {nameof(Mask)} component.");
        }
#endif
    }
}