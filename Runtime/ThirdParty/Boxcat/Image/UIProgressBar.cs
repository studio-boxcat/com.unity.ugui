#nullable enable

using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    internal sealed class UIProgressBar : MonoBehaviour
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private CanvasGroup _bar;

        public void Set(float ratio)
        {
            _bar.GetRectTransform().SetAnchorMaxX1(ratio);
            _bar.alpha = GetProgressAlpha(ratio);
        }

        public void To(float to)
        {
            var x = _bar.GetRectTransform().anchorMax.x;
            DOTween.To(() => x, v =>
                {
                    x = v;
                    _bar.GetRectTransform().SetAnchorMaxX1(v);
                    _bar.alpha = GetProgressAlpha(v);
                }, to, 0.9f)
                .SetEase(Ease.OutQuint);
        }

        private static float GetProgressAlpha(float value)
        {
            return value switch
            {
                > 0.1f => 1f,
                // initial progress looks glitchy
                <= 0.1f and > 0.04f => Mathf.InverseLerp(0.04f, 0.1f, value),
                _ => 0f,
            };
        }

#if UNITY_EDITOR
        [ShowInInspector, PropertyRange(0, 1)]
        private float _previewValue
        {
            get => _bar ? _bar.GetRectTransform().anchorMax.x : 0f;
            set => Set(value);
        }

        private void OnValidate()
        {
            if (_bar && DrivenRectTransManager.Reset(this, out var tracker, out var isNew))
            {
                tracker.Set(_bar.GetRectTransform(), DrivenTransformProperties.AnchorMaxX);
                if (isNew) _previewValue = 0.5f;
            }
        }
#endif
    }
}
