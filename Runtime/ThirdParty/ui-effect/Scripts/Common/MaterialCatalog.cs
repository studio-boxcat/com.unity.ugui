using UnityEngine;
using UnityEngine.Assertions;

namespace Coffee.UIEffects
{
    static class MaterialCatalog
    {
        static Material _effect_Add;
        static Material _effect_Fill;
        static Material _effect_Fill_Premult;
        static Material _shiny;
        static Material _shiny_Premult;


        static ParameterTexture _paramEffect;
        public static ParameterTexture ParamEffect => _paramEffect ??= CreateParamTexture(4, 32);
        static ParameterTexture _paramShiny;
        public static ParameterTexture ParamShiny => _paramShiny ??= CreateParamTexture(8, 32);

        static int _propertyIdCache;
        static int _propertyId
        {
            get
            {
                if (_propertyIdCache == 0)
                    _propertyIdCache = Shader.PropertyToID("_ParamTex");
                return _propertyIdCache;
            }
        }

        public static Material GetEffect(string baseShaderName, ColorMode colorMode)
        {
#if DEBUG
            Assert.IsTrue(IsValidShaderName(baseShaderName, colorMode),
                $"Invalid shader name: {baseShaderName}");
#endif

            return colorMode switch
            {
                ColorMode.Add => _effect_Add ??= LoadMaterial(MaterialNames.UIEffectAdd, ParamEffect),
                ColorMode.Fill when baseShaderName is ShaderNames.UIPremult
                    => _effect_Fill_Premult ??= LoadMaterial(MaterialNames.UIEffectFillPremult, ParamEffect),
                ColorMode.Fill => _effect_Fill ??= LoadMaterial(MaterialNames.UIEffectFill, ParamEffect),
                _ => throw new System.NotSupportedException("Only ColorMode.Add and ColorMode.Fill are supported.")
            };
        }

#if DEBUG
        internal static bool IsValidShaderName(string shaderName, ColorMode colorMode)
        {
            if (colorMode is ColorMode.Add) // premult would need Blend ONE+ONE, not implemented
                return shaderName is ShaderNames.UIDefault;
            if (colorMode is ColorMode.Fill)
                return shaderName is ShaderNames.UIDefault or ShaderNames.UIPremult;
            throw new System.NotSupportedException($"Unsupported ColorMode: {colorMode}");
        }
#endif

        public static Material GetShiny(string baseShaderName)
        {
            Assert.IsTrue(baseShaderName is ShaderNames.UIDefault or ShaderNames.UIPremult,
                $"Unsupported shader: {baseShaderName}");

            return baseShaderName is ShaderNames.UIDefault
                ? _shiny ??= LoadMaterial(MaterialNames.UIShiny, ParamShiny)
                : _shiny_Premult ??= LoadMaterial(MaterialNames.UIShinyPremult, ParamShiny);
        }

        static Material LoadMaterial(string path, ParameterTexture paramTex)
        {
            L.I($"LoadMaterial: {path}");
            var mat = Resources.Load<Material>(path);
#if UNITY_EDITOR
            mat = new Material(mat) { hideFlags = HideFlags.HideAndDontSave };
#endif
            Assert.IsNotNull(mat, $"Material not found: {path}");
            paramTex.SetTextureForMaterial(mat, _propertyId);
            return mat;
        }

        static ParameterTexture CreateParamTexture(int channels, int instanceLimit)
        {
            var paramTex = new ParameterTexture(channels, instanceLimit);
            paramTex.Initialize();
            return paramTex;
        }
    }
}