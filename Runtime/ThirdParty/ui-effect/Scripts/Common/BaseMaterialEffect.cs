using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    [DisallowMultipleComponent]
    public abstract class BaseMaterialEffect : BaseMeshEffect, IMaterialModifier
    {
        protected int ParamSlot { get; private set; } = ParamTex.NoSlot;

        public void SetMaterialDirty() => graphic.SetMaterialDirty();

        public Material? GetModifiedMaterial(GraphicMaterialKey key)
        {
            if (enabled is false) return null;
            var effectMat = GetEffectMaterial(key.IsPremult);
            Assert.IsNotNull(effectMat.GetParamTex(), "Material must have a texture property '_ParamTex'.");
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

            Assert.AreEqual(ParamTex.NoSlot, ParamSlot, "slot is already acquired.");
            ParamSlot = ParamTex.Acquire();

            SetMaterialDirty();
            SetEffectParamsDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SetMaterialDirty();

            ParamTex.Release(ParamSlot);
            ParamSlot = ParamTex.NoSlot;
        }
    }
}
