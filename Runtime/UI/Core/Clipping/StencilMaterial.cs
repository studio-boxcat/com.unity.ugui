// ReSharper disable InconsistentNaming

#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    /// <summary>
    /// Dynamic material class makes it possible to create custom materials on the fly on a per-Graphic basis,
    /// and still have them get cleaned up correctly.
    /// </summary>
    public static class StencilMaterial
    {
        private class MatEntry
        {
            public readonly Material baseMat;
            public readonly Material customMat;
            public readonly ulong hash;
            public int refCount;

            public MatEntry(Material baseMat, Material customMat, ulong hash)
            {
                this.baseMat = baseMat;
                this.customMat = customMat;
                this.hash = hash;
            }
        }

        private static readonly List<MatEntry> m_List = new();

        private static readonly int _stencil = Shader.PropertyToID("_Stencil");
        private static readonly int _stencilOp = Shader.PropertyToID("_StencilOp");
        private static readonly int _stencilComp = Shader.PropertyToID("_StencilComp");
        private static readonly int _stencilReadMask = Shader.PropertyToID("_StencilReadMask");
        private static readonly int _stencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
        private static readonly int _colorMask = Shader.PropertyToID("_ColorMask");
        private static readonly int _useUIAlphaClip = Shader.PropertyToID("_UseUIAlphaClip");

        /// <summary>
        /// Add a new material using the specified base and stencil ID.
        /// </summary>
        private static Material Add(Material baseMat,
            int stencilID, StencilOp operation, CompareFunction compareFunction,
            ColorWriteMask colorWriteMask, int readMask = 255, int writeMask = 255)
        {
            Assert.IsTrue(baseMat, "Base material must not be null.");
            Assert.IsTrue(stencilID > 0, "Stencil ID must be greater than 0.");
            Assert.IsTrue(stencilID <= 0xff && (int) operation <= 0xff && (int) compareFunction <= 0xff
                          && (int) colorWriteMask <= 0xff && readMask <= 0xff && writeMask <= 0xff,
                "Stencil ID, operation, compare function, color write mask, read mask and write mask must be <= 0xff.");

#if DEBUG
            CheckPropertyExists(baseMat, _stencil);
            CheckPropertyExists(baseMat, _stencilOp);
            CheckPropertyExists(baseMat, _stencilComp);
            CheckPropertyExists(baseMat, _stencilReadMask);
            CheckPropertyExists(baseMat, _stencilWriteMask);
            CheckPropertyExists(baseMat, _colorMask);
#endif

            // If we have a pre-existing entry matching the description,
            // just increase the ref count and return the material.
            var hash = Hash(stencilID, operation, compareFunction, colorWriteMask, readMask, writeMask);
            foreach (var e in m_List) // not that huge, so we can use foreach.
            {
                if (e.baseMat.RefEq(baseMat) && e.hash == hash)
                {
                    ++e.refCount;
                    return e.customMat;
                }
            }

            var newMat = new Material(baseMat);
            newMat.EditorDontSaveFlag(); // Prevent material from unloading.
            newMat.SetNameDebug($"{baseMat.name} (Stencil Id:{stencilID}, Op:{operation}, Comp:{compareFunction}, WriteMask:{writeMask}, ReadMask:{readMask}, ColorMask:{colorWriteMask})");

            newMat.SetFloat(_stencil, stencilID);
            newMat.SetFloat(_stencilOp, (float) operation);
            newMat.SetFloat(_stencilComp, (float) compareFunction);
            newMat.SetFloat(_stencilReadMask, readMask);
            newMat.SetFloat(_stencilWriteMask, writeMask);
            newMat.SetFloat(_colorMask, (float) colorWriteMask);

            var useAlphaClip = operation != StencilOp.Keep && writeMask > 0;
            if (useAlphaClip)
            {
                newMat.SetFloat(_useUIAlphaClip, 1);
                newMat.EnableKeyword("UNITY_UI_ALPHACLIP");
            }
            else
            {
                newMat.SetFloat(_useUIAlphaClip, 0);
                newMat.DisableKeyword("UNITY_UI_ALPHACLIP");
            }

            L.I($"[UGUI] Stencil material created: {newMat.name}", baseMat);

            m_List.Add(new MatEntry(baseMat, newMat, hash) { refCount = 1 });

#if UNITY_EDITOR
            if (m_List.Count > 16)
                L.E($"[UGUI] Too many stencil materials created: {m_List.Count}, list=[{string.Join(", ", m_List.Select(e => e.customMat.name))}]");
#endif
            return newMat;

            static ulong Hash(
                int stencilID, StencilOp operation, CompareFunction compareFunction,
                ColorWriteMask colorWriteMask, int readMask, int writeMask)
            {
                // stencil ID, op, compare function, color write mask, read mask, write mask are all 1 byte.
                return (uint) stencilID
                       | ((ulong) operation << 8)
                       | ((ulong) compareFunction << 16)
                       | ((ulong) colorWriteMask << 24)
                       | ((ulong) readMask << 32)
                       | ((ulong) writeMask << 40);
            }
        }

        public static Material AddMaskable(Material toUse, int depth)
        {
            Assert.IsTrue(depth is > 0 and < 8, "Stencil depth must be greater than 0 and less than 8.");

            return Add(toUse,
                stencilID: (1 << depth) - 1,
                StencilOp.Keep,
                CompareFunction.Equal,
                ColorWriteMask.All,
                readMask: (1 << depth) - 1,
                writeMask: 0);
        }

        public static (Material Mask, Material Unmask) AddMaskPair(Material baseMaterial, byte depth, bool showMaskGraphic)
        {
            Assert.IsTrue(baseMaterial, "Base material must not be null.");
            Assert.IsTrue(depth < 8, "Stencil depth must be less than 8.");

            var colorWriteMask = showMaskGraphic ? ColorWriteMask.All : 0;

            // if we are at the first level...
            // we want to destroy what is there
            if (depth is 0)
            {
                return (Add(baseMaterial, stencilID: 1, StencilOp.Replace, CompareFunction.Always, colorWriteMask: colorWriteMask),
                    Add(baseMaterial, stencilID: 1, StencilOp.Zero, CompareFunction.Always, colorWriteMask: 0));
            }
            //otherwise we need to be a bit smarter and set some read / write masks
            else
            {
                var bit = 1 << depth;
                var whole = bit | (bit - 1); // this is the mask that will include self and all previous levels.
                var below = bit - 1; // this is the mask that will include all previous levels, but not self.
                return (Add(baseMaterial, stencilID: whole, StencilOp.Replace, CompareFunction.Equal,
                        colorWriteMask: colorWriteMask, readMask: below, writeMask: whole),
                    Add(baseMaterial, stencilID: below, StencilOp.Replace, CompareFunction.Equal,
                        colorWriteMask: 0, readMask: below, writeMask: whole));
            }
        }

        /// <summary>
        /// Remove an existing material, automatically cleaning it up if it's no longer in use.
        /// </summary>
        public static void Remove([NotNull] Material customMat)
        {
            // Iterate in reverse order as the most recently added materials are most likely to be removed.
            var count = m_List.Count;

            for (var i = count - 1; i >= 0; i--)
            {
                var e = m_List[i];
                if (ReferenceEquals(e.customMat, customMat) is false)
                    continue;

                // Destroy material if no longer in use.
                // Keep some instances in the list to reduce allocations.
                var noRef = --e.refCount is 0;
                if (noRef && count > 4)
                {
                    L.I($"[UGUI] Stencil material destroyed: {e.customMat.name}", e.baseMat);
                    Object.DestroyImmediate(e.customMat);
                    m_List.RemoveAt(i);
                }

                break;
            }
        }

#if DEBUG
        private static void CheckPropertyExists(Material mat, int id)
        {
            if (mat.HasProperty(id)) return;
            L.W("[UGUI] Material " + mat.name + " doesn't have " + id + " property", mat);
        }
#endif
    }
}