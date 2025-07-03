using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    /// <summary>
    /// Use this interface to modify a Material that renders a Graphic. The Material is modified before the it is passed to the CanvasRenderer.
    /// </summary>
    /// <remarks>
    /// When a Graphic sets a material that is passed (in order) to any components on the GameObject that implement IMaterialModifier. This component can modify the material to be used for rendering.
    /// </remarks>
    public interface IMaterialModifier
    {
        /// <summary>
        /// Perform material modification in this function.
        /// </summary>
        /// <param name="baseMaterial">The material that is to be modified</param>
        /// <returns>The modified material.</returns>
        Material GetModifiedMaterial(Material baseMaterial);
    }

    public static class MaterialModifierUtils
    {
        private static readonly List<IMaterialModifier> _buf = new();

        public static Material ResolveMaterialForRendering(Component comp, Material baseMaterial)
        {
            var currentMat = baseMaterial;
            comp.GetComponents(_buf);
            var count = _buf.Count; // mostly 0.
            for (var i = 0; i < count; i++)
                currentMat = _buf[i].GetModifiedMaterial(currentMat);
            return currentMat;
        }

        public static Material ResolveMaterialForRenderingExceptSelf(Component comp, Material baseMaterial)
        {
            var currentMat = baseMaterial;
            var self = comp as IMaterialModifier;

            comp.GetComponents(_buf);
            var count = _buf.Count; // mostly 0.
            for (var i = 0; i < count; i++)
            {
                var mod = _buf[i];
                if (self.RefEq(mod)) continue; // skip self.
                currentMat = mod.GetModifiedMaterial(currentMat);
            }
            return currentMat;
        }
    }
}