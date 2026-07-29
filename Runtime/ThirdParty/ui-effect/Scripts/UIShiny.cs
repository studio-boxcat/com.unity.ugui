using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    public class UIShiny : BaseMaterialEffect
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField]
        float m_EffectFactor = 0.5f;
        [SerializeField, HorizontalGroup("Geometry")]
        float m_Width = 0.25f;
        [SerializeField, HorizontalGroup("Geometry")]
        float m_Rotation = 135;
        [SerializeField, HorizontalGroup("Visual")]
        float m_Softness = 1f;
        [SerializeField, HorizontalGroup("Visual")]
        float m_Brightness = 1f;
        [SerializeField, HorizontalGroup("Visual")]
        float m_Gloss = 1;
        [FormerlySerializedAs("m_EffectArea")]
        [SerializeField] bool m_FitAABB;
        [SerializeField] EffectPlayer m_Player = null!;

        public override ParameterTexture paramTex => MaterialCatalog.ParamShiny;

        protected override void OnEnable()
        {
            // Before base: its SetEffectParamsDirty pass has to see a player that already started,
            // or the first frames render the authored location instead of the sweep's start.
            m_Player.OnEnable();
            base.OnEnable();
        }

        private void Update()
        {
            if (m_Player.Update())
                SetEffectParamsDirty();
        }

        public void Play() => m_Player.Play();

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
            var rect = m_FitAABB
                ? mb.Poses.CalculateBoundingRect()
                : rectTransform.rect;

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

        protected override void SetEffectParamsDirty()
        {
            var location = m_Player.current ?? m_EffectFactor;
            paramTex.SetData(this, 0, location); // param1.x : location
            paramTex.SetData(this, 1, m_Width); // param1.y : width
            paramTex.SetData(this, 2, m_Softness); // param1.z : softness
            paramTex.SetData(this, 3, m_Brightness); // param1.w : blightness
            paramTex.SetData(this, 4, m_Gloss); // param2.x : gloss
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = GetComponent<Graphic>();
            if (!g) return; // [RequireComponent] from BaseMeshEffect
            if (g.material is not GraphicMaterialKind.Normal)
                result.AddError($"UIShiny only supports Normal material (got {g.material}).");
        }
#endif
    }
}
