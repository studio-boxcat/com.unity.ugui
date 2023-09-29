using System;

namespace UnityEngine.UI
{
    public enum ShadowStyle : byte
    {
        Shadow = 1,
        Outline4 = 2,
        Outline8 = 3,
    }

    [AddComponentMenu("UI/Effects/Shadow", 80)]
    /// <summary>
    /// Adds an outline to a graphic using IVertexModifier.
    /// </summary>
    public class Shadow : BaseMeshEffect
    {
        [SerializeField] ShadowStyle m_Style = ShadowStyle.Shadow;
        [SerializeField] Vector2 m_EffectDistance = new(1f, -1f);
        [SerializeField] Color m_EffectColor = new(0f, 0f, 0f, 0.5f);
        [SerializeField] bool m_UseGraphicAlpha;

        /// <summary>
        /// Color for the effect
        /// </summary>
        public Color effectColor
        {
            get => m_EffectColor;
            set
            {
                m_EffectColor = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// How far is the shadow from the graphic.
        /// </summary>
        public Vector2 effectDistance
        {
            get => m_EffectDistance;
            set
            {
                if (m_EffectDistance == value)
                    return;

                m_EffectDistance = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Should the shadow inherit the alpha from the graphic?
        /// </summary>
        public bool useGraphicAlpha
        {
            get => m_UseGraphicAlpha;
            set
            {
                m_UseGraphicAlpha = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(MeshBuilder mb)
        {
            var sm = new MeshShadowManipulator(m_EffectColor, m_UseGraphicAlpha);

            var dx = m_EffectDistance.x;
            var dy = m_EffectDistance.y;

            switch (m_Style)
            {
                case ShadowStyle.Shadow:
                    sm.Populate(mb, 1);
                    sm.Translate(0, dx, dy);
                    break;
                case ShadowStyle.Outline4:
                    sm.Populate(mb, 4);
                    sm.Translate(0, dx, dy);
                    sm.Translate(1, dx, -dy);
                    sm.Translate(2, -dx, dy);
                    sm.Translate(3, -dx, -dy);
                    break;
                case ShadowStyle.Outline8:
                    sm.Populate(mb, 8);
                    sm.Translate(0, dx, dy);
                    sm.Translate(1, dx, -dy);
                    sm.Translate(2, -dx, dy);
                    sm.Translate(3, -dx, -dy);
                    sm.Translate(4, dx, 0);
                    sm.Translate(5, 0, dy);
                    sm.Translate(6, -dx, 0);
                    sm.Translate(7, 0, -dy);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}