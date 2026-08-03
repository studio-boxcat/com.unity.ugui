using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class UIEffect : BaseMaterialEffect
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField] [Range(0, 1)]
        [OnValueChanged(nameof(SetEffectParamsDirty))]
        float m_ColorFactor = 1;

        [SerializeField]
        [OnValueChanged(nameof(SetMaterialDirty))]
        ColorMode m_ColorMode = ColorMode.Fill;

        public float colorFactor
        {
            get => m_ColorFactor;
            set
            {
                value = Mathf.Clamp(value, 0, 1);
                if (Mathf.Approximately(m_ColorFactor, value)) return;
                m_ColorFactor = value;
                SetEffectParamsDirty();
            }
        }

        protected override Material GetEffectMaterial(bool isPremult)
        {
            return MaterialCatalog.GetEffect(m_ColorMode, isPremult);
        }

        public override void ModifyMesh(MeshBuilder mb)
        {
            var uvs = mb.UVs.Edit();
            var count = uvs.Length;
            var normalizedIndex = ParamTex.GetNormalizedIndex(ParamSlot);

            for (var i = 0; i < count; i++)
            {
                var uv = uvs[i];
                uvs[i] = new Vector2(
                    Numeric.PackUNorm12x2((uv.x + 0.5f) / 2f, (uv.y + 0.5f) / 2f),
                    normalizedIndex);
            }
        }

        protected override void SetEffectParamsDirty()
        {
            if (ParamTex.Edit(ParamSlot, out var w))
                w.Set(1, m_ColorFactor); // param.y : color factor
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = GetComponent<Graphic>();
            var isPremult = GraphicMaterialResolver.IsPremult(g.mainTexture);
            if (!MaterialCatalog.IsValidForEffect(m_ColorMode, isPremult))
                result.AddError($"UIEffect with colorMode={m_ColorMode} does not support premult={isPremult}.");
        }
#endif
    }
}
