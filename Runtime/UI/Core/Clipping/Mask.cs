// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    public class Mask : MaterialModifierBase, IMeshModifier, IPostGraphicRebuildCallback
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, FormerlySerializedAs("m_ShowMaskGraphic")]
        private bool _showMaskGraphic = true;
        [SerializeField, Required, ChildGameObjectsOnly]
        [ListDrawerSettings(IsReadOnly = true), AllChildren, RequiredListLength(MinLength = 1)]
        private Maskable[] _maskables = null!;

        [NonSerialized] private Mesh? _mesh;

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (var m in _maskables)
            {
                if (m) m.Graphic.SetMaterialDirty();
                else L.W($"[Mask] Maskable is destroyed: {m.SafeName()}");
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            var cr = Graphic.canvasRenderer;
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

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            CanvasUpdateRegistry.QueueGraphicRebuildCallback(this);
            return baseMaterial;
        }

        // override material after graphic rebuild
        void IPostGraphicRebuildCallback.PostGraphicRebuild()
        {
            Assert.IsTrue(isActiveAndEnabled, "[Mask] Should only be called when active and enabled");

            var cr = Graphic.canvasRenderer;
            var (maskMat, unmaskMat) = StencilMaterial.LoadMaskPair(_showMaskGraphic);
            cr.SetMaterial(maskMat, 0);
            cr.hasPopInstruction = true;
            cr.popMaterialCount = 1;
            cr.SetPopMaterial(unmaskMat, 0);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _maskables = GetComponentsInChildren<Maskable>(true);
        }

        protected override void Awake()
        {
            base.Awake();
            _maskables ??= GetComponentsInChildren<Maskable>(true);
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            var g = Graphic;
            if (g)
            {
                if (g.material.RefNq(Graphic.defaultGraphicMaterial))
                    result.AddError($"[Mask] Graphic ({g.SafeName()}) must have default material.");
                if (g.gameObject.RefNq(gameObject))
                    result.AddError($"[Mask] Graphic ({g.SafeName()}) must be on the same GameObject as Mask ({gameObject.name})");
            }
        }
#endif
    }
}