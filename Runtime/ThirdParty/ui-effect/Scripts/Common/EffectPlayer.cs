using UnityEngine;
using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Coffee.UIEffects
{
    [Serializable, BoxGroup]
    public class EffectPlayer
    {
        [FormerlySerializedAs("play")]
        public bool playOnEnable = false;
        [HorizontalGroup("Time")]
        [FormerlySerializedAs("initialPlayDelay")]
        public float initialDelay = 0;
        [HorizontalGroup("Time")] [MinValue(0.01f)]
        public float duration = 1;
        [HorizontalGroup("Loop")]
        public bool loop = false;
        [HorizontalGroup("Loop")]
        public float loopDelay = 0;

        public bool playing { get; private set; }
        private float _time = float.NaN;

        public void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        public void Play()
        {
            _time = -initialDelay;
            playing = true;
        }

        public void Pause()
        {
            playing = false;
        }

        public bool Update()
        {
            if (!playing)
                return false;

            var old = Mathf.Clamp01(_time);
            _time += Time.deltaTime;

            if (_time > duration)
            {
                playing = loop;
                _time = loop ? -loopDelay : duration;
            }

            var cur = Mathf.Clamp01(_time);
            return cur.ENq(old);
        }

        public float? current => _time.IsNan() ? null : Mathf.Clamp01(_time / duration);

#if UNITY_EDITOR
        private bool _preview;

        [Button(DirtyOnClick = false)]
        private void TogglePlay()
        {
            if (_preview.GetAndFlip())
            {
                Pause();
                AnimationModeManager.Stop();
            }
            else // was false
            {
                Play();
                AnimationModeManager.Start();
            }
        }
#endif
    }
}
