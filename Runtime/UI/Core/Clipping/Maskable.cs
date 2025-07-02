using System;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class Maskable : MonoBehaviour, IMaskable, IMaterialModifier
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private Graphic _graphic;

        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        private Material _maskMaterial;
        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        private Mask _mask;
        [NonSerialized]
        private bool _maskDirty = true;

        private void OnEnable()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        private void OnDisable()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        private void OnDestroy()
        {
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }
        }

#if UNITY_EDITOR
        private void Reset() => _graphic = GetComponent<Graphic>();
        private void OnValidate() => _maskDirty = true;
#endif

        private void OnTransformParentChanged()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        private void OnCanvasHierarchyChanged()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        void IMaskable.RecalculateMasking()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (enabled is false)
                return baseMaterial;

            if (_maskDirty)
            {
                _mask = MaskUtilities.GetEligibleMask(transform);
                _maskDirty = false;
            }

            if (_mask is null)
                return baseMaterial;

            // Invalidate cached mask material.
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }

            // Only the first masking level will work.
            return _maskMaterial = StencilMaterial.Add(
                baseMaterial, stencilID: 1,
                StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All,
                readMask: 1, writeMask: 0);
        }
    }
}