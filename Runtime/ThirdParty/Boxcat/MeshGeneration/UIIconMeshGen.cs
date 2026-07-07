#nullable enable
using System;
using UnityEngine;

namespace UnityEngine.UI
{
    public static class UIIconMeshGen
    {
        public static void Populate(UIMeshMode mode, Sprite sprite, float scaleFactor, MeshBuilder mb)
        {
            switch (mode)
            {
                case UIMeshMode.ID: Identity(sprite, scaleFactor, mb); break;
                case UIMeshMode.MX: MX(sprite, scaleFactor, mb); break;
                case UIMeshMode.MY: MY(sprite, scaleFactor, mb); break;
                case UIMeshMode.MXY: MXY(sprite, scaleFactor, mb); break;
                case UIMeshMode.FX: SpriteMeshWriter.Scale(sprite, -scaleFactor, scaleFactor, mb); break;
                case UIMeshMode.FY: SpriteMeshWriter.Scale(sprite, scaleFactor, -scaleFactor, mb); break;
                case UIMeshMode.FXY: SpriteMeshWriter.Scale(sprite, -scaleFactor, -scaleFactor, mb); break;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public static void Identity(Sprite sprite, float scaleFactor, MeshBuilder mb)
            => SpriteMeshWriter.Scale(sprite, scaleFactor, scaleFactor, mb);

        public static void MX(Sprite sprite, float scaleFactor, MeshBuilder mb)
            => SpriteMeshWriter.ScaleMirrorX(sprite, scaleFactor, scaleFactor, mb);

        public static void MY(Sprite sprite, float scaleFactor, MeshBuilder mb)
            => SpriteMeshWriter.ScaleMirrorY(sprite, scaleFactor, scaleFactor, mb);

        public static void MXY(Sprite sprite, float scaleFactor, MeshBuilder mb)
            => SpriteMeshWriter.ScaleMirrorXY(sprite, scaleFactor, scaleFactor, mb);
    }
}
