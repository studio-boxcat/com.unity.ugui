#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class Maskable : MonoBehaviour, IMaskable, IMaterialModifier
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private Graphic _graphic = null!;

        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        private Material? _baseMaterial;
        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        private Material? _maskMaterial;

        private void OnEnable() => _graphic.SetMaterialDirty();
        private void OnDisable() => _graphic.SetMaterialDirty();

        private void OnDestroy()
        {
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }
        }

        private void OnTransformParentChanged() => _graphic.SetMaterialDirty();
        private void OnCanvasHierarchyChanged() => _graphic.SetMaterialDirty();
        void IMaskable.RecalculateMasking() => _graphic.SetMaterialDirty();

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            // We don't support multiple masking levels, so only the first mask will work.
            // This means we don't need to the mask component changed or not,
            // as material remains the same as long as the base material is the same.

            // If the graphic is not enabled, return the base material.
            // don't invalidate the mask material, as it will be used when the graphic is enabled again.
            if (enabled is false)
                return baseMaterial;

            // No mask, return base material.
            // don't invalidate the mask material, as it will be used when the graphic is enabled again.
            var mask = MaskUtilities.GetEligibleMask(transform);
            if (mask is null)
                return baseMaterial;

            // if the baseMaterial is not changed, return the cached mask material.
            var unchanged = ReferenceEquals(_baseMaterial, baseMaterial);
            if (unchanged) return _maskMaterial!;
            _baseMaterial = baseMaterial; // update base material reference

            // invalidate cached mask material first.
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }

            // create a new mask material. (StencilMaterial is pooling materials, so need to worry about leaks)
            return _maskMaterial = StencilMaterial.AddMaskable(baseMaterial, 1); // hardcoded to 1, as we don't support multiple masks.
        }

#if UNITY_EDITOR
        private void Reset() => _graphic = GetComponent<Graphic>();

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (_graphic && _graphic is MaskableGraphic)
                result.AddError("Maskable component should not be used with MaskableGraphic.");
        }
#endif
    }
}