// ReSharper disable InconsistentNaming

#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    /// <summary>
    /// Dynamic material class makes it possible to create custom materials on the fly on a per-Graphic basis,
    /// and still have them get cleaned up correctly.
    /// </summary>
    public static class StencilMaterial
    {
        private static readonly Dictionary<int, MatEntry> _baseToEntry = new();
        private static readonly Dictionary<int, MatEntry> _renderToEntry = new();

        private static readonly int _stencil = Shader.PropertyToID("_Stencil");

        public static Material AddMaskable(Material baseMat)
        {
            Assert.IsTrue(baseMat, "Base material must not be null.");

            // If we have a pre-existing entry matching the description,
            // just increase the ref count and return the material.
            var baseID = baseMat.GetInstanceID();
            if (_baseToEntry.TryGetValue(baseID, out var e))
            {
                ++e.RefCount;
                return e.Render;
            }

            var renderMat = new Material(baseMat);
            renderMat.SetDontSave(); // Prevent material from unloading.
            renderMat.SetNameDebug($"{baseMat.name} (Maskable)");
            renderMat.SetFloat(_stencil, 1); // XXX: we only support 1 level of masking for now

            L.I($"[UGUI] Stencil material created: {renderMat.name}", baseMat);

            var entry = new MatEntry(baseMat, renderMat) { RefCount = 1 };
            _baseToEntry.Add(baseID, entry);
            _renderToEntry.Add(renderMat.GetInstanceID(), entry);

#if DEBUG
            if (_baseToEntry.Count > 8)
                L.E($"[UGUI] Too many stencil materials created: {_baseToEntry.Count}, " +
                    $"list=[{string.Join(", ", _baseToEntry.Values)}]");
#endif
            return renderMat;
        }

        /// <summary>
        /// Remove an existing material, automatically cleaning it up if it's no longer in use.
        /// </summary>
        public static void RemoveMaskable(Material renderMat)
        {
            var renderID = renderMat.GetInstanceID();
            if (_renderToEntry.TryGetValue(renderID, out var e) is false)
            {
                L.E($"[UGUI] Trying to remove a stencil material that doesn't exist: {renderMat.SafeName()}", renderMat);
                return;
            }

            if (--e.RefCount is not 0 // still in use
                || _renderToEntry.Count <= 4) // keep some instances to reduce allocations
            {
                return;
            }

            // Destroy material if no longer in use.
            L.I($"[UGUI] Stencil material destroyed: {e.Render.SafeName()}", e.Render);
            Object.DestroyImmediate(e.Render);
            _baseToEntry.Remove(e.Base.GetInstanceID());
            _renderToEntry.Remove(renderID);
        }

        public static byte GetDepthFromRenderMaterial(Material mat)
        {
            var id = (int) mat.GetFloat(_stencil);
            return id switch
            {
                0 => 0, // most common case
                0b0000_0001 => 1, // most common case
                0b0000_0011 => 2,
                0b0000_0111 => 3,
                0b0000_1111 => 4,
                0b0001_1111 => 5,
                0b0011_1111 => 6,
                0b0111_1111 => 7,
                0b1111_1111 => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(mat), $"Invalid stencil ID: {id}")
            };
        }

        private static Material? _maskMat;
        private static Material? _unmaskMat;

        public static (Material Mask, Material Unmask) LoadMaskPair()
        {
            if (_maskMat is not null)
                return (_maskMat, _unmaskMat!);

            // XXX: only depth 1 supported for now
            const int stencilID = 1;
            var baseMat = Graphic.defaultGraphicMaterial;
            var stencilOp = Shader.PropertyToID("_StencilOp");
            var stencilComp = Shader.PropertyToID("_StencilComp");
            var stencilReadMask = Shader.PropertyToID("_StencilReadMask");
            var stencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
            var colorMask = Shader.PropertyToID("_ColorMask");

            var mask = new Material(baseMat);
            mask.SetDontSave();
            mask.SetFloat(_stencil, stencilID);
            mask.SetFloat(stencilOp, (float) StencilOp.Replace);
            mask.SetFloat(stencilComp, (float) CompareFunction.Always);
            mask.SetFloat(stencilReadMask, stencilID);
            mask.SetFloat(stencilWriteMask, stencilID);
            mask.SetFloat(colorMask, 0); // don't draw, just write to stencil buffer
            mask.EnableKeyword("UNITY_UI_ALPHACLIP");

            var unmask = new Material(baseMat);
            unmask.SetDontSave();
            unmask.SetFloat(_stencil, stencilID);
            unmask.SetFloat(stencilOp, (float) StencilOp.Zero);
            unmask.SetFloat(stencilComp, (float) CompareFunction.Always);
            unmask.SetFloat(stencilReadMask, stencilID);
            unmask.SetFloat(stencilWriteMask, stencilID);
            unmask.SetFloat(colorMask, 0);
            unmask.EnableKeyword("UNITY_UI_ALPHACLIP");

            return (mask, unmask);
        }

        private class MatEntry
        {
            public readonly Material Base;
            public readonly Material Render;
            public int RefCount;

            public MatEntry(Material @base, Material render)
            {
                Base = @base;
                Render = render;
            }

            public override string ToString()
            {
                return $"(base='{Base.name}', render='{Render.name}', refCount={RefCount})";
            }
        }
    }
}