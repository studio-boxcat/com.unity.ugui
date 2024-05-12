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
        [SerializeField]
        bool _canNestMask;

        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        Material _maskMaterial;
        [NonSerialized, ShowInInspector, ReadOnly, FoldoutGroup("Advanced")]
        int _stencilValue;
        [NonSerialized] bool _shouldRecalculateStencil = true;

        void OnEnable()
        {
            _shouldRecalculateStencil = true;
            _graphic.SetMaterialDirty();
        }

        void OnDisable()
        {
            _shouldRecalculateStencil = true;
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
        void OnValidate() => _shouldRecalculateStencil = true;
#endif

        void OnTransformParentChanged()
        {
            _shouldRecalculateStencil = true;
            _graphic.SetMaterialDirty();
        }

        void OnCanvasHierarchyChanged()
        {
            _shouldRecalculateStencil = true;
            _graphic.SetMaterialDirty();
        }

        /// <summary>
        /// See IMaskable.RecalculateMasking
        /// </summary>
        void IMaskable.RecalculateMasking()
        {
            _shouldRecalculateStencil = true;
            _graphic.SetMaterialDirty();
        }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            if (enabled is false)
                return baseMaterial;

            if (_shouldRecalculateStencil)
            {
                _stencilValue = GetStencilDepth();
                _shouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (_stencilValue is 0)
                return baseMaterial;

            // Invalidate cached mask material.
            if (_maskMaterial is not null)
            {
                StencilMaterial.Remove(_maskMaterial);
                _maskMaterial = null;
            }

            var readMask = (1 << _stencilValue) - 1;
            return _maskMaterial = StencilMaterial.Add(
                baseMaterial, stencilID: readMask,
                StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All,
                readMask, 0);

            int GetStencilDepth()
            {
                if (_canNestMask)
                    return MaskUtilities.GetStencilDepth(transform);
                return MaskUtilities.HasEligibleMask(transform) ? 1 : 0;
            }
        }
    }
}