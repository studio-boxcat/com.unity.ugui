// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [ExecuteAlways]
    public sealed class TargetClipper : UIBehaviour
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, Required, RequiredListLength(MinLength = 1)]
        private Graphic[] _targets = null!;

        [SerializeField] private Vector4 _padding;
        public Vector4 padding
        {
            get => _padding;
            set => _padding = value;
        }

        [SerializeField] private Vector2Int _softness;
        public Vector2Int softness
        {
            get => _softness;
            set => _softness = value;
        }

        [NonSerialized] private RectTransform? _rectTransform;
        public RectTransform rectTransform => _rectTransform ??= GetComponent<RectTransform>();

        private void OnDisable()
        {
            foreach (var g in _targets)
                ClipperRegistry.RestoreCullState(g);
        }

        // before CanvasRenderer render.
        private void LateUpdate()
        {
            var canvas = _targets[0].canvas;
            var clipRect = CanvasUtils.BoundingRect(
                rectTransform, canvas, _padding, out var validRect);

            foreach (var target in _targets)
            {
                var g = target;
                g.SetClipSoftness(_softness);
                g.SetClipRect(clipRect, validRect);
            }
        }

#if UNITY_EDITOR
        private void Reset() => _targets = Array.Empty<Graphic>();

        // ReSharper disable once Unity.DuplicateShortcut
        [ContextMenu("Collect _c")]
        private void Collect() => _targets = GetComponentsInChildren<Graphic>(true);

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            foreach (var graphic in _targets)
            {
                if (!graphic) continue;
                // prevent the target graphic is added to the other clipper by ClipperRegistry.
                if (graphic.HasComponent<Maskable>() )
                    result.AddError("TargetClipper cannot be used with Maskable components.");
                if (graphic.HasComponent<Clippable>())
                    result.AddError("TargetClipper cannot be used with Clippable components.");
            }
        }
#endif
    }
}