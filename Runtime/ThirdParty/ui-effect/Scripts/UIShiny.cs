using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    /// <summary>
    /// UIEffect.
    /// </summary>
    [AddComponentMenu("UI/UIEffects/UIShiny", 2)]
    public class UIShiny : BaseMaterialEffect
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        float _lastRotation;
        EffectArea _lastEffectArea;

        [Tooltip("Location for shiny effect.")] [FormerlySerializedAs("m_Location")] [SerializeField] [Range(0, 1)]
        float m_EffectFactor = 0.5f;

        [Tooltip("Width for shiny effect.")] [SerializeField] [Range(0, 1)]
        float m_Width = 0.25f;

        [Tooltip("Rotation for shiny effect.")] [SerializeField] [Range(-180, 180)]
        float m_Rotation = 135;

        [Tooltip("Softness for shiny effect.")] [SerializeField] [Range(0.01f, 1)]
        float m_Softness = 1f;

        [Tooltip("Brightness for shiny effect.")] [FormerlySerializedAs("m_Alpha")] [SerializeField] [Range(0, 1)]
        float m_Brightness = 1f;

        [Tooltip("Gloss factor for shiny effect.")] [FormerlySerializedAs("m_Highlight")] [SerializeField] [Range(0, 1)]
        float m_Gloss = 1;

        [Header("Advanced Option")] [Tooltip("The area for effect.")] [SerializeField]
        protected EffectArea m_EffectArea;

        [SerializeField] EffectPlayer m_Player;

        /// <summary>
        /// Effect factor between 0(start) and 1(end).
        /// </summary>
        public float effectFactor
        {
            get { return m_EffectFactor; }
            set
            {
                value = Mathf.Clamp(value, 0, 1);
                if (Mathf.Approximately(m_EffectFactor, value)) return;
                m_EffectFactor = value;
                SetEffectParamsDirty();
            }
        }

        /// <summary>
        /// Gets the parameter texture.
        /// </summary>
        public override ParameterTexture paramTex => MaterialCatalog.ParamShiny;

        public EffectPlayer effectPlayer => m_Player ??= new EffectPlayer();

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            effectPlayer.OnEnable(f => effectFactor = f);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled () or inactive.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            effectPlayer.OnDisable();
        }

        protected override Material GetEffectMaterial(bool isPremult)
        {
            return MaterialCatalog.GetShiny(isPremult);
        }

        /// <summary>
        /// Modifies the mesh.
        /// </summary>
        public override void ModifyMesh(MeshBuilder mb)
        {
            var normalizedIndex = paramTex.GetNormalizedIndex(this);
            var rect = m_EffectArea.GetEffectArea(mb, rectTransform.rect);

            // Calculate vertex position.
            var poses = mb.Poses;
            var uvs = mb.UVs.Edit();
            var vertCount = poses.Count;
            var localMatrix = Matrix2x3.NormalizeRotated(rect, m_Rotation * Mathf.Deg2Rad); // Get local matrix.
            for (int i = 0; i < vertCount; i++)
            {
                var normalizedPos = localMatrix.MultiplyPoint(poses[i]);
                var uv = uvs[i];
                uvs[i] = new Vector2(
                    Numeric.PackUNorm12x2(uv.x, uv.y),
                    Numeric.PackUNorm12x2(normalizedPos.y, normalizedIndex)
                );
            }
        }

        /// <summary>
        /// Play effect.
        /// </summary>
        public void Play(bool reset = true)
        {
            effectPlayer.Play(reset);
        }

        /// <summary>
        /// Stop effect.
        /// </summary>
        public void Stop(bool reset = true)
        {
            effectPlayer.Stop(reset);
        }

        protected override void SetEffectParamsDirty()
        {
            paramTex.SetData(this, 0, m_EffectFactor); // param1.x : location
            paramTex.SetData(this, 1, m_Width); // param1.y : width
            paramTex.SetData(this, 2, m_Softness); // param1.z : softness
            paramTex.SetData(this, 3, m_Brightness); // param1.w : blightness
            paramTex.SetData(this, 4, m_Gloss); // param2.x : gloss
        }

        protected override void SetVerticesDirty()
        {
            base.SetVerticesDirty();

            _lastRotation = m_Rotation;
            _lastEffectArea = m_EffectArea;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();

            if (!Mathf.Approximately(_lastRotation, m_Rotation)
                || _lastEffectArea != m_EffectArea)
                SetVerticesDirty();
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = GetComponent<Graphic>();
            if (!g)
            {
                result.AddError("Graphic component is missing.");
                return;
            }

            if (g.material is not GraphicMaterialKind.Normal)
                result.AddError($"UIShiny only supports Normal material (got {g.material}).");
        }
#endif
    }
}
