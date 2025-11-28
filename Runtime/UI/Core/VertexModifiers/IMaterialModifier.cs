using System.Collections.Generic;
using Sirenix.OdinInspector;

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

    [ExecuteAlways]
    public abstract class MaterialModifierBase : MonoBehaviour, IMaterialModifier
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        [HideIf("_graphic_HideIf")]
        private Graphic _graphic = null!;
        public Graphic Graphic => _graphic;

        protected virtual void Awake() => _graphic ??= GetComponent<Graphic>();
        protected virtual void OnEnable() => SetMaterialDirty();
        protected virtual void OnDisable() => SetMaterialDirty();
        public void SetMaterialDirty() => _graphic.SetMaterialDirty();
        public abstract Material GetModifiedMaterial(Material baseMaterial);

#if UNITY_EDITOR
        protected virtual void Reset() => _graphic = GetComponent<Graphic>();
        private bool _graphic_HideIf() => _graphic && _graphic.gameObject.RefEq(gameObject);
#endif
    }

    public static class MaterialModifierUtils
    {
        // assume that there's no nested usage, so we can reuse the same buffer.
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