// ReSharper disable InconsistentNaming

#nullable enable
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    public class Mask : MonoBehaviour, IMeshModifier, IMaterialModifier, IPostGraphicRebuildCallback
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private Graphic _graphic = null!;
        [SerializeField, Required, ChildGameObjectsOnly]
        private Maskable[] _maskables = null!;
        [SerializeField, FormerlySerializedAs("m_ShowMaskGraphic")]
        private bool _showMaskGraphic = true;

        private void OnEnable()
        {
            _graphic.SetMaterialDirty();

            foreach (var m in _maskables)
            {
                if (m) m.Graphic.SetMaterialDirty();
                else L.W($"[Mask] Maskable is destroyed: {m.SafeName()}");
            }
        }

        protected void OnDisable()
        {
            _graphic.SetMaterialDirty();

            foreach (var m in _maskables)
            {
                if (m) m.Graphic.SetMaterialDirty();
                else L.W($"[Mask] Maskable is destroyed: {m.SafeName()}");
            }
        }

        void IMeshModifier.ModifyMesh(MeshBuilder mb) =>
            CanvasUpdateRegistry.QueueGraphicRebuildCallback(this);

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            CanvasUpdateRegistry.QueueGraphicRebuildCallback(this);
            return baseMaterial;
        }

        // override material after graphic rebuild
        void IPostGraphicRebuildCallback.PostGraphicRebuild()
        {
            var (maskMat, unmaskMat) = StencilMaterial.LoadMaskPair();
            var cr = _graphic.canvasRenderer;

            // set up mask
            var tex = _graphic.mainTexture;
            if (_showMaskGraphic)
            {
                cr.materialCount = 2;
                cr.SetMaterial(_graphic.material, tex);
                cr.SetMaterial(maskMat, 1);
            }
            else
            {
                cr.materialCount = 1;
                cr.SetMaterial(maskMat, tex);
            }

            // set up unmask
            cr.hasPopInstruction = true;
            cr.popMaterialCount = 1;
            cr.SetPopMaterial(unmaskMat, 0);
        }
    }
}