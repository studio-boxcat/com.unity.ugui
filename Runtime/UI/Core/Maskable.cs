using System;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class Maskable : MonoBehaviour, IMaskable, IMaterialModifier
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        Graphic _graphic;

        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        Material _maskMaterial;
        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        Mask _mask;
        [NonSerialized]
        bool _maskDirty = true;

        void OnEnable()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        void OnDisable()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        void OnDestroy()
        {
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }
        }

#if UNITY_EDITOR
        void Reset() => _graphic = GetComponent<Graphic>();
        void OnValidate() => _maskDirty = true;
#endif

        void OnTransformParentChanged()
        {
            _maskDirty = true;
            _graphic.SetMaterialDirty();
        }

        void OnCanvasHierarchyChanged()
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

            return _maskMaterial = StencilMaterial.Add(
                baseMaterial, stencilID: 1,
                StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All,
                readMask: 1, writeMask: 0);
        }
    }
}