using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public abstract class BaseMaterialEffect : UIBehaviour, IMeshModifier, IMaterialModifier
    {
        [NonSerialized] Graphic? _graphic;
        public Graphic graphic => _graphic ??= GetComponent<Graphic>();

        protected int ParamSlot { get; private set; } = ParamTex.NoSlot;

        protected virtual void OnEnable()
        {
            Assert.AreEqual(ParamTex.NoSlot, ParamSlot, "slot is already acquired.");
            ParamSlot = ParamTex.Acquire();

            SetVerticesDirty();
            SetMaterialDirty();
            SetEffectParamsDirty();
        }

        protected virtual void OnDisable()
        {
            SetVerticesDirty();
            SetMaterialDirty();

            ParamTex.Release(ParamSlot);
            ParamSlot = ParamTex.NoSlot;
        }

        protected void SetVerticesDirty() => graphic.SetVerticesDirty();
        public void SetMaterialDirty() => graphic.SetMaterialDirty();
        protected abstract void SetEffectParamsDirty();

        public abstract void ModifyMesh(MeshBuilder mb);

        protected abstract Material GetEffectMaterial(bool isPremult);

        Material? IMaterialModifier.GetModifiedMaterial(GraphicMaterialKey key)
        {
            if (enabled is false) return null;
            var effectMat = GetEffectMaterial(key.IsPremult);
            Assert.IsNotNull(effectMat.GetParamTex(), "Material must have a texture property '_ParamTex'.");
            return effectMat;
        }

        protected void OnDidApplyAnimationProperties()
        {
            if (!isActiveAndEnabled) return;
            SetEffectParamsDirty();
        }

#if UNITY_EDITOR
        protected void Reset()
        {
            if (!isActiveAndEnabled) return;
            SetVerticesDirty();
            SetMaterialDirty();
            SetEffectParamsDirty();
        }
#endif
    }
}
