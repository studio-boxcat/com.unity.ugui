#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    public class UIMeshRuntimeGraphic : Graphic, ICustomMaterialProvider
    {
        private Mesh? _mesh;
        private Material? _customMaterial;

        [SerializeField, OnValueChanged("SetMaterialDirty")]
        private Texture? _texture;
        public override Texture mainTexture => _texture ? _texture! : whiteTexture;

        Material? ICustomMaterialProvider.ProvideMaterial() => _customMaterial;

        public void SetMesh(Mesh mesh)
        {
            _mesh = mesh;
            SetVerticesDirty();
        }

        public void ClearMesh()
        {
            _mesh = null;
            SetVerticesDirty();
        }

        public void SetTexture(Texture2D texture)
        {
            _texture = texture;
            SetMaterialDirty();
        }

        public void SetMaterial(Material mat, Texture2D tex)
        {
            _customMaterial = mat;
            _texture = tex;
            material = GraphicMaterialKind.Custom;
        }

        public void SetCanvasRendererColor(Color color)
        {
            // Graphic.color could mislead, this color is passed by the MeshBuilder.
            canvasRenderer.SetColor(color);
        }

        protected override void UpdateGeometry()
        {
            if (_mesh) // _mesh could be destroyed.
                canvasRenderer.SetMesh(_mesh);
        }
    }
}
