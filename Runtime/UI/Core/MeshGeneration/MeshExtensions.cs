using System;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public static class MeshExtensions
    {
        // _colorBuf could be changed by calling Set(), so it needs extreme care when returning it
        static Color32[] _colorBuf = Array.Empty<Color32>();

        static Color32[] GetTempColors(Color32 color, int count)
        {
            if (_colorBuf.Length < count)
                _colorBuf = new Color32[count];

            Array.Fill(_colorBuf, color, 0, count);
            return _colorBuf;
        }

        public static void SetColorsFast(this Mesh mesh, Color color, int count)
        {
            var colors = color switch
            {
                { r: 1, g: 1, b: 1, a: 1 } => WhiteColorCache.Opaque(count),
                { r: 1, g: 1, b: 1, a: 0 } => WhiteColorCache.Transparent(count),
                _ => GetTempColors(color, count)
            };

            mesh.SetColors(colors, 0, count,
                MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
        }
    }
}