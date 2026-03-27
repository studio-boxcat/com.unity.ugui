using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    /// <summary>
    /// Area for effect.
    /// </summary>
    public enum EffectArea
    {
        RectTransform,
        Fit,
        Character,
    }

    public static class EffectAreaExtensions
    {
        static readonly Rect rectForCharacter = new Rect(0, 0, 1, 1);

        /// <summary>
        /// Gets effect for area.
        /// </summary>
        public static Rect GetEffectArea(this EffectArea area, MeshBuilder mb, Rect rectangle, float aspectRatio = -1)
        {
            var rect = default(Rect);
            switch (area)
            {
                case EffectArea.RectTransform:
                    rect = rectangle;
                    break;
                case EffectArea.Character:
                    rect = rectForCharacter;
                    break;
                case EffectArea.Fit:
                    rect = mb.Poses.CalculateBoundingRect();
                    break;
                default:
                    rect = rectangle;
                    break;
            }


            if (0 < aspectRatio)
            {
                if (rect.width < rect.height)
                {
                    rect.width = rect.height * aspectRatio;
                }
                else
                {
                    rect.height = rect.width / aspectRatio;
                }
            }

            return rect;
        }
    }
}
