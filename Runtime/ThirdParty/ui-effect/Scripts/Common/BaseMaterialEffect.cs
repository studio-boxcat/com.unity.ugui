using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    [DisallowMultipleComponent]
    public abstract class BaseMaterialEffect : BaseMeshEffect, IParameterInstance, IMaterialModifier
    {
        int IParameterInstance.index { get; set; }

        public abstract ParameterTexture paramTex { get; }

        public void SetMaterialDirty() => graphic.SetMaterialDirty();

        public Material? GetModifiedMaterial(GraphicMaterialKey key)
        {
            if (enabled is false) return null;
            var effectMat = GetEffectMaterial(key.IsPremult);
            Assert.IsNotNull(effectMat.GetTexture("_ParamTex"), "Material must have a texture property '_ParamTex'.");
            return effectMat;
        }

        protected abstract Material GetEffectMaterial(bool isPremult);

#if UNITY_EDITOR
        protected override void Reset()
        {
            if (!isActiveAndEnabled) return;
            SetMaterialDirty();
            SetVerticesDirty();
            SetEffectParamsDirty();
        }

        protected override void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            SetVerticesDirty();
            SetEffectParamsDirty();
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            paramTex?.Register(this);

            SetMaterialDirty();
            SetEffectParamsDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SetMaterialDirty();

            paramTex?.Unregister(this);
        }
    }
}
