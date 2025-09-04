// ReSharper disable InconsistentNaming

#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

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
            // graphic might not be a child of this GameObject, so we cannot guarantee whether graphic is destroyed or not.
            foreach (var g in _targets)
            {
                if (g) ClipperRegistry.RestoreCullState(g);
            }
        }

        // before CanvasRenderer render.
        private void LateUpdate()
        {
            var len = _targets.Length;
            if (len is 0) return;

            var canvas = _targets[0].canvas;
            var clipRect = CanvasUtils.BoundingRect(
                rectTransform, canvas, _padding, out var validRect);

            var e = AliveEnumerator.Create(_targets);
            while (e.Next(out var g))
            {
                Assert.IsTrue(g, "Target graphic is null.");
                g!.SetClipSoftness(_softness);
                g.SetClipRect(clipRect, validRect);
            }
            _targets = e.Shrink();
        }

        public void AddGraphicsInChildren(Transform root)
        {
            var oldLength = _targets.Length;
            var graphics = root.GetGraphicsInChildrenShared(includeInactive: true);
            Array.Resize(ref _targets, _targets.Length + graphics.Count);
            for (var index = 0; index < graphics.Count; index++)
            {
                var g = graphics[index];
                Assert.IsTrue(g.NoComponent<Clippable>(), $"{g.name} should not have Clippable.");
                _targets[oldLength + index] = g;
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
                if (graphic.HasComponent<Clippable>())
                    result.AddError("TargetClipper cannot be used with Clippable components.");
            }
        }
#endif
    }
}