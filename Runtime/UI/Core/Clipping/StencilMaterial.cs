#nullable enable
using System.Collections.Generic;
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
        class MatEntry
        {
            public Material baseMat;
            public Material customMat;
            public int refCount;

            public int stencilId;
            public StencilOp operation = StencilOp.Keep;
            public CompareFunction compareFunction = CompareFunction.Always;
            public int readMask;
            public int writeMask;
            public ColorWriteMask colorMask;
        }

        static readonly List<MatEntry> m_List = new();

        static readonly int _stencil = Shader.PropertyToID("_Stencil");
        static readonly int _stencilOp = Shader.PropertyToID("_StencilOp");
        static readonly int _stencilComp = Shader.PropertyToID("_StencilComp");
        static readonly int _stencilReadMask = Shader.PropertyToID("_StencilReadMask");
        static readonly int _stencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
        static readonly int _colorMask = Shader.PropertyToID("_ColorMask");
        static readonly int _useUIAlphaClip = Shader.PropertyToID("_UseUIAlphaClip");

        /// <summary>
        /// Add a new material using the specified base and stencil ID.
        /// </summary>
        private static Material Add(Material baseMat,
            int stencilID, StencilOp operation, CompareFunction compareFunction,
            ColorWriteMask colorWriteMask, int readMask = 255, int writeMask = 255)
        {
            if ((stencilID <= 0 && colorWriteMask == ColorWriteMask.All) || baseMat == null)
                return baseMat;

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
            foreach (var ent in m_List)
            {
                if (ReferenceEquals(ent.baseMat, baseMat)
                    && ent.stencilId == stencilID
                    && ent.operation == operation
                    && ent.compareFunction == compareFunction
                    && ent.readMask == readMask
                    && ent.writeMask == writeMask
                    && ent.colorMask == colorWriteMask)
                {
                    ++ent.refCount;
                    return ent.customMat;
                }
            }

            var newEnt = new MatEntry();
            newEnt.refCount = 1;
            newEnt.baseMat = baseMat;
            newEnt.customMat = new Material(baseMat);

            newEnt.stencilId = stencilID;
            newEnt.operation = operation;
            newEnt.compareFunction = compareFunction;
            newEnt.readMask = readMask;
            newEnt.writeMask = writeMask;
            newEnt.colorMask = colorWriteMask;


            var newMat = newEnt.customMat;
            newMat.hideFlags = HideFlags.HideAndDontSave; // Prevent material from unloading.
#if DEBUG
            newMat.name = $"{baseMat.name} (Stencil Id:{stencilID}, Op:{operation}, Comp:{compareFunction}, WriteMask:{writeMask}, ReadMask:{readMask}, ColorMask:{colorWriteMask})";
#endif
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

            m_List.Add(newEnt);

            L.I($"[UGUI] Stencil material created: {newMat.name}", baseMat);
            return newMat;
        }

        public static Material AddMaskable(Material toUse, int depth)
        {
            return Add(toUse,
                stencilID: (1 << depth) - 1,
                StencilOp.Keep,
                CompareFunction.Equal,
                ColorWriteMask.All,
                readMask: (1 << depth) - 1,
                writeMask: 0);
        }

        public static (Material Mask, Material Unmask) AddMaskPair(Material baseMaterial, int depth, bool showMaskGraphic)
        {
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
                return (Add(baseMaterial, stencilID: bit | (bit - 1), StencilOp.Replace, CompareFunction.Equal,
                        colorWriteMask: colorWriteMask, readMask: bit - 1, writeMask: bit | (bit - 1)),
                    Add(baseMaterial, stencilID: bit - 1, StencilOp.Replace, CompareFunction.Equal,
                        colorWriteMask: 0, readMask: bit - 1, writeMask: bit | (bit - 1)));
            }
        }

        /// <summary>
        /// Remove an existing material, automatically cleaning it up if it's no longer in use.
        /// </summary>
        public static void Remove([NotNull] Material customMat)
        {
            if (customMat == null)
                return;

            // Iterate in reverse order as the most recently added materials are most likely to be removed.
            var count = m_List.Count;
            for (var i = count - 1; i >= 0; i--)
            {
                var ent = m_List[i];

                if (ReferenceEquals(ent.customMat, customMat) is false)
                    continue;

                var noRef = --ent.refCount is 0;

                // Destroy material if no longer in use.
                // Keep some instances in the list to reduce allocations.
                if (noRef && count > 4)
                {
                    L.I($"[UGUI] Stencil material destroyed: {ent.customMat.name}", ent.baseMat);
                    Object.DestroyImmediate(ent.customMat);
                    ent.baseMat = null;
                    m_List.RemoveAt(i);
                }
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