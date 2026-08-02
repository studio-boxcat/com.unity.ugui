using UnityEngine;
using UnityEngine.Assertions;

namespace Coffee.UIEffects
{
    static class MaterialCatalog
    {
        static Material? _effect_Add;
        static Material? _effect_Fill;
        static Material? _effect_Fill_Premult;
        static Material? _shiny;
        static Material? _shiny_Premult;


        public static Material GetEffect(ColorMode colorMode, bool isPremult)
        {
            return colorMode switch
            {
                ColorMode.Add => _effect_Add ??= LoadMaterial(MaterialNames.UIEffectAdd),
                ColorMode.Fill when isPremult
                    => _effect_Fill_Premult ??= LoadMaterial(MaterialNames.UIEffectFillPremult),
                ColorMode.Fill => _effect_Fill ??= LoadMaterial(MaterialNames.UIEffectFill),
                _ => throw new System.NotSupportedException("Only ColorMode.Add and ColorMode.Fill are supported.")
            };
        }

#if DEBUG
        internal static bool IsValidForEffect(ColorMode colorMode, bool isPremult)
        {
            if (colorMode is ColorMode.Add)
                return !isPremult; // premult would need Blend ONE+ONE, not implemented
            if (colorMode is ColorMode.Fill)
                return true;
            throw new System.NotSupportedException($"Unsupported ColorMode: {colorMode}");
        }
#endif

        public static Material GetShiny(bool isPremult)
        {
            return isPremult
                ? _shiny_Premult ??= LoadMaterial(MaterialNames.UIShinyPremult)
                : _shiny ??= LoadMaterial(MaterialNames.UIShiny);
        }

        static Material LoadMaterial(string path)
        {
            L.TI(path);
            var mat = Resources.Load<Material>(path);
            Assert.IsNotNull(mat, $"Material not found: {path}"); // before the copy, which would NRE first
#if UNITY_EDITOR
            mat = new Material(mat) { hideFlags = HideFlags.HideAndDontSave };
#endif
            mat.SetParamTex(ParamTex.Texture);
            return mat;
        }
    }
}
