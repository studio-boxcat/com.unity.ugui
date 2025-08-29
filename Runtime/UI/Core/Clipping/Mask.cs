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
        [SerializeField, Required, ChildGameObjectsOnly]
        [ListDrawerSettings(IsReadOnly = true), AllChildren, RequiredListLength(MinLength = 1)]
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

            var cr = _graphic.canvasRenderer;
            var (maskMat, unmaskMat) = StencilMaterial.LoadMaskPair(_showMaskGraphic);
            cr.SetMaterial(maskMat, 0);
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

        private void Awake()
        {
            _graphic ??= GetComponent<Graphic>();
            _maskables ??= GetComponentsInChildren<Maskable>(true);
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (_graphic && _graphic.gameObject.RefNq(gameObject))
                result.AddError($"[Mask] Graphic ({_graphic.SafeName()}) must be on the same GameObject as Mask ({gameObject.name})");
        }
#endif
    }
}