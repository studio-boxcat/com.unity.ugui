using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.UI
{
    public static class WhiteColorCache
    {
        private static Color32[] _opaque = new Color32[64];
        private static Color32[] _transparent = new Color32[64];


        static WhiteColorCache()
        {
            Array.Fill(_opaque, new Color32(255, 255, 255, 255));
            Array.Fill(_transparent, new Color32(255, 255, 255, 0));
        }

        public static bool TryGet(Color32 color, int count, out Color32[] colors)
        {
            if (color is not { r: 255, g: 255, b: 255 })
            {
                colors = null;
                return false;
            }

            var a = color.a;
            switch (a)
            {
                case 255:
                    colors = Opaque(count);
                    return true;
                case 0:
                    colors = Transparent(count);
                    return true;
                default:
                    colors = null;
                    return false;
            }
        }

        public static Color32[] Opaque(int count)
        {
            PrepareArray(ref _opaque, count, new Color32(255, 255, 255, 255));
            return _opaque;
        }

        public static Color32[] Transparent(int count)
        {
            PrepareArray(ref _transparent, count, new Color32(255, 255, 255, 0));
            return _transparent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrepareArray(ref Color32[] colors, int vertexCount, Color32 color)
        {
            if (colors.Length >= vertexCount) return;
            colors = new Color32[vertexCount];
            Array.Fill(colors, color);
        }
    }
}