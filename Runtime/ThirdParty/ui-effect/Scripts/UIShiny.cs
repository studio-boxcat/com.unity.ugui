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
        [SerializeField, OnValueChanged("Editor_Refresh")]
        float m_EffectFactor = 0.5f;
        [SerializeField, HorizontalGroup("Geometry"), OnValueChanged("Editor_Refresh")]
        float m_Width = 0.25f;
        [SerializeField, HorizontalGroup("Geometry"), OnValueChanged("Editor_Refresh")]
        float m_Rotation = 135;
        [SerializeField, HorizontalGroup("Visual"), OnValueChanged("Editor_Refresh")]
        float m_Softness = 1f;
        [SerializeField, HorizontalGroup("Visual"), OnValueChanged("Editor_Refresh")]
        float m_Brightness = 1f;
        [SerializeField, HorizontalGroup("Visual"), OnValueChanged("Editor_Refresh")]
        float m_Gloss = 1;
        [FormerlySerializedAs("m_EffectArea")]
        [SerializeField, OnValueChanged("Editor_Refresh")] bool m_FitAABB;
        [SerializeField] EffectPlayer m_Player = null!;

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
            var normalizedIndex = ParamTex.GetNormalizedIndex(ParamSlot);
            var rect = m_FitAABB
                ? mb.Poses.CalculateBoundingRect()
                : graphic.rectTransform.rect;

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
            if (!ParamTex.Edit(ParamSlot, out var w)) return;

            var location = m_Player.current ?? m_EffectFactor;
            w.Set(0, location); // param1.x : location
            w.Set(1, m_Width); // param1.y : width
            w.Set(2, m_Softness); // param1.z : softness
            w.Set(3, m_Brightness); // param1.w : blightness
            w.Set(4, m_Gloss); // param2.x : gloss
        }

#if UNITY_EDITOR
        // The base has no OnValidate, so Odin drives the refresh. Rotation and FitAABB reach
        // ModifyMesh, the rest only the shader params — one helper covers both.
        private void Editor_Refresh()
        {
            if (!isActiveAndEnabled) return;
            SetVerticesDirty();
            SetEffectParamsDirty();
        }

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
