#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Sprites;

namespace UnityEngine.UI
{
    // Per-sprite solid-sample UV: the center of the inner UV, safe to flat-fill for untextured geometry.
    public static class SolidUVCache
    {
        private static readonly Dictionary<int, Vector2> _uvCache = new();

        public static Vector2 Get(Sprite sprite)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) _uvCache.Clear(); // sprite reimports invalidate instance ids
#endif

            var id = sprite.GetInstanceID();
            if (_uvCache.TryGetValue(id, out var uv))
                return uv;

            // Center of the inner UV.
            var innerUV = DataUtility.GetInnerUV(sprite);
            uv = new Vector2(
                (innerUV.x + innerUV.z) * 0.5f,
                (innerUV.y + innerUV.w) * 0.5f);

            _uvCache.Add(id, uv);
            return uv;
        }

        public static Vector2[] UVs(Sprite sprite, int count)
        {
            var uv = new Vector2[count];
            Array.Fill(uv, Get(sprite));
            return uv;
        }
    }
}
