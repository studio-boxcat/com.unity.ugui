#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    public class UIRuntimeTextureImage : Graphic
    {
        [NonSerialized]
        private Texture? _texture;

        [ShowInInspector, ReadOnly]
        public Texture? Texture
        {
            get => _texture;
            set
            {
                if (_texture.RefEq(value))
                    return;

                _texture = value;
                SetMaterialDirty();
            }
        }

        public override Texture mainTexture => _texture;

        protected override void OnPopulateMesh(Color color, MeshBuilder mb) =>
            mb.SetUp_Quad_FullUV(rectTransform.rect, color);
    }
}
