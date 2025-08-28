// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class Mask : MonoBehaviour, IMeshModifier, IMaterialModifier, IPostGraphicRebuildCallback
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private Graphic _graphic = null!;
        [SerializeField, FormerlySerializedAs("m_ShowMaskGraphic")]
        private bool _showMaskGraphic = true;
        [SerializeField, Required, ChildGameObjectsOnly, ListDrawerSettings(IsReadOnly = true)]
        private Maskable[] _maskables = null!;

        [NonSerialized] private Mesh? _mesh;

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

            var cr = _graphic.canvasRenderer;
            cr.hasPopInstruction = false;

            foreach (var m in _maskables)
            {
                if (m) m.Graphic.SetMaterialDirty();
                else L.W($"[Mask] Maskable is destroyed: {m.SafeName()}");
            }
        }

        private void OnDestroy()
        {
            if (_mesh is not null)
            {
                DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        void IMeshModifier.ModifyMesh(MeshBuilder mb)
        {
            CanvasUpdateRegistry.QueueGraphicRebuildCallback(this);
        }

        Material IMaterialModifier.GetModifiedMaterial(Material baseMaterial)
        {
            CanvasUpdateRegistry.QueueGraphicRebuildCallback(this);
            return baseMaterial;
        }

        // override material after graphic rebuild
        void IPostGraphicRebuildCallback.PostGraphicRebuild()
        {
            Assert.IsTrue(isActiveAndEnabled, "[Mask] Should only be called when active and enabled");

            var (maskMat, unmaskMat) = StencilMaterial.LoadMaskPair();
            var cr = _graphic.canvasRenderer;
            Assert.IsTrue(cr.materialCount is 1, "[Mask] CanvasRenderer should have exactly one material");

            // set up mask
            var tex = _graphic.mainTexture;
            if (_showMaskGraphic)
            {
                cr.materialCount = 2;
                cr.SetMaterial(maskMat, 1);

                // combine original mesh with mask mesh
                var orgMesh = cr.GetMesh();
                _mesh ??= MeshPool.CreateDynamicMesh();
                _mesh.Clear(keepVertexLayout: true);
                _mesh.CombineMeshes(orgMesh, orgMesh, mergeSubMeshes: false); // first submesh is original, second is mask
                cr.SetMesh(_mesh);
            }
            else
            {
                cr.SetMaterial(maskMat, tex);
            }

            // set up unmask
            cr.hasPopInstruction = true;
            cr.popMaterialCount = 1;
            cr.SetPopMaterial(unmaskMat, 0);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _graphic = GetComponent<Graphic>();
            _maskables = GetComponentsInChildren<Maskable>(true);
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (!_graphic) return;
            if (_graphic.gameObject.RefNq(gameObject))
                result.AddError($"[Mask] Graphic ({_graphic.SafeName()}) must be on the same GameObject as Mask ({gameObject.name})");
        }
#endif
    }
}