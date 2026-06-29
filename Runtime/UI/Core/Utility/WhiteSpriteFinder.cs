#if UNITY_EDITOR
#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.UI
{
    // Editor-only: seeds a freshly added UISolid/UIPolygon with the white sprite of its own
    // content folder (e.g. OG_0603_White) instead of the global CM_White. The per-folder white
    // is the 1x1 atlas cell every solid/polygon in that prefab should source from — see
    // OrgelContent.ValidateSpriteAtlas.
    public static class WhiteSpriteFinder
    {
        // Anchor atlas texture (instanceID) -> resolved folder white, or null when the folder has
        // no *_White (negative marker, so the scan never repeats). Absent key = not yet scanned.
        private static readonly Dictionary<int, Sprite?> _cache = new();

        // Climb from `go` up the hierarchy; at each level scan descendant graphics for one whose
        // sprite folder holds a *_White sprite (nearest wins). Falls back to the global CM_White —
        // loaded by GUID since ugui can't reference CommonAssets in the higher Boxcat.Core layer.
        public static Sprite? Find(GameObject go)
        {
            for (var t = go.transform; t; t = t.parent)
            {
                // Shared list, no per-level allocation; safe since each level is fully scanned
                // before the next call overwrites it, and TryResolveFolderWhite gathers no graphics.
                foreach (var g in t.GetGraphicsInChildrenShared(includeInactive: true))
                {
                    if (TryResolveFolderWhite(g, out var white))
                        return white;
                }
            }

            // CM_White.asset — keep this GUID in sync with CommonAssets.WhiteSprite (CommonAssets.cs).
            return (Sprite)AssetDatabase.LoadMainAssetAtGUID(
                new GUID("ab3570c4e0a070a122f2a1424533e216"));
        }

        private static bool TryResolveFolderWhite(Graphic g, out Sprite white)
        {
            white = null!;

            if (g is not UIImageBase img) return false;
            var anchor = img.Sprite;
            if (!anchor) return false;
            var tex = anchor.texture;
            if (!tex) return false;

            var texId = tex.GetInstanceID();
            if (_cache.TryGetValue(texId, out var cached))
            {
                if (!cached) return false; // null negative marker: keep climbing
                white = cached;
                return true;
            }

            var found = AssetDatabaseUtils.LoadSiblingAssetsInFolder<Sprite>(anchor, "*_White.asset")
                .FirstOrDefault(sprite =>
                    (sprite.SizeInPx().MaxComp() <= 4) // size constraint
                    && sprite.IsQuad());
            _cache[texId] = found; // null caches the negative result
            if (found is null) return false;

            white = found;
            return true;
        }
    }
}
#endif
