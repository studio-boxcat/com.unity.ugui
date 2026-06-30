using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    /// <summary>
    /// UIGradient.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UIEffects/UIGradient", 101)]
    public class UIGradient : BaseMeshEffect
    {
        [Tooltip("Gradient Direction.")] [SerializeField]
        Axis m_Direction;

        [Tooltip("Color1: Top or Left.")] [SerializeField]
        Color m_Color1 = Color.white;

        [Tooltip("Color2: Bottom or Right.")] [SerializeField]
        Color m_Color2 = Color.white;

        [Tooltip("Gradient offset for Horizontal, Vertical or Angle.")] [SerializeField] [Range(-1, 1)]
        float m_Offset1;

        [Tooltip("Color space to correct color.")] [SerializeField]
        [HideIf("@m_ColorSpace == ColorSpace.Gamma")] // Will be removed.
        ColorSpace m_ColorSpace = ColorSpace.Gamma;

        /// <summary>
        /// Call used to modify mesh.
        /// </summary>
        public override void ModifyMesh(MeshBuilder mb)
        {
            // Gradient space.
            var rect = graphic.rectTransform.rect;

            // Calculate min max range.
            var (min, max) = m_Direction == Axis.X
                ? (rect.xMin, rect.xMax)
                : (rect.yMin, rect.yMax);

            var vertCount = mb.Poses.Count;
            var poses = mb.Poses;
            var colors = mb.Colors.Edit();

            for (var i = 0; i < vertCount; i++)
            {
                // Normalize vertex position.
                var pos = m_Direction == Axis.X ? poses[i].x : poses[i].y;
                var normalizedPos = Mathf.InverseLerp(min, max, pos) + m_Offset1;

                // Interpolate vertex color.
                var color = Color.Lerp(m_Color2, m_Color1, normalizedPos);

                // Correct color.
                colors[i] *= m_ColorSpace switch
                {
                    ColorSpace.Gamma => color.gamma,
                    ColorSpace.Linear => color.linear,
                    _ => color
                };
            }
        }
    }
}
