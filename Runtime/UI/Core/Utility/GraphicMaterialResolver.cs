#nullable enable
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public static class GraphicMaterialResolver
    {
        private static readonly Dictionary<int, bool> _premultCache = new(128);

        public static GraphicMaterialKey ResolveKey(Graphic graphic)
        {
            return new GraphicMaterialKey(graphic.material, IsPremult(graphic.mainTexture));
        }

        public static Material ResolveBase(GraphicMaterialKind material, Graphic graphic)
        {
            if (material == GraphicMaterialKind.Custom)
                return ResolveCustom(graphic);
            // mainTexture accessed lazily — avoids recursion for components whose mainTexture depends on material.
            return MaterialCatalog.Resolve(new GraphicMaterialKey(material, IsPremult(graphic.mainTexture)));
        }

        public static Material ResolveRender(Graphic graphic)
        {
            if (!graphic.TryGetComponent<IMaterialModifier>(out var modifier))
                return ResolveBase(graphic.material, graphic);
            return modifier.GetModifiedMaterial(ResolveKey(graphic))
                   ?? ResolveBase(graphic.material, graphic);
        }

        public static bool IsPremult(Texture? tex)
        {
            if (!tex) return false;
            var id = tex.GetInstanceID();
            if (_premultCache.TryGetValue(id, out var result)) return result;
            var baked = PremultTextureNames.Contains(tex.name);
#if UNITY_EDITOR
            // Only assert when the baked list and editor disagree on a tpsheet-sourced texture.
            // Skip non-Texture2D, and skip textures not in the baked list (built-in, font, runtime — alphaIsTransparency is unreliable for those).
            if (baked && tex is Texture2D t2d && t2d.alphaIsTransparency)
                L.E($"Premult mismatch for \"{tex.name}\": baked=premult but editor=normal. Rebake PremultTextureNames.");
#endif
            _premultCache[id] = baked;
            return baked;
        }

        private static Material ResolveCustom(Graphic graphic)
        {
            if (graphic.TryGetComponent<ICustomMaterialProvider>(out var provider))
            {
                var mat = provider.ProvideMaterial();
                if (mat) return mat;
            }
#if DEBUG
            L.W($"[GraphicMaterialResolver] Custom material on \"{graphic.name}\" but no ICustomMaterialProvider found. Falling back to Normal.", graphic);
#endif
            return ResolveBase(GraphicMaterialKind.Normal, graphic);
        }

#if UNITY_EDITOR
        [PlayModeGate]
        private static void ClearCache() => _premultCache.Clear();
#endif
    }
}
