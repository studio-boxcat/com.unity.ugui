#nullable enable
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Coffee.UIEffects
{
    // One texture row of shader parameters per acquired slot. Shared by every effect: a row belongs
    // to one holder, so UIEffect (channel 1) and UIShiny (channels 0-4) never collide.
    internal static class ParamTex
    {
        public const int NoSlot = -1;

        private const int _slotCount = 64; // one slot per bit of _usedSlots.
        // UIShiny needs 5, rounded up to a whole RGBA texel -> 8, so the texture is 2px wide.
        private const int _channels = 8;

        private static BitField64 _usedSlots; // set while the slot is held.
        private static Texture2D? _texture;
        private static bool _needUpload;

        public static Texture2D Texture
        {
            get
            {
                EnsureCreated();
                return _texture!;
            }
        }

        // NoSlot when every row is taken.
        public static int Acquire()
        {
            var slot = _usedSlots.LowestZeroIndex;
            if (slot is NoSlot)
            {
                L.E("Slots exhausted.");
                return NoSlot;
            }

            _usedSlots.Set(slot);
            return slot;
        }

        public static void Release(int slot)
        {
            if (slot is NoSlot) return;
            // A double release hands the row to the next holder while the old one still writes it.
            Assert.IsTrue(_usedSlots[slot], "slot is not held.");
            _usedSlots.Unset(slot);
        }

        // False when the holder has no row, so Writer can assume one.
        public static bool Edit(int slot, out Writer writer)
        {
            if (slot is NoSlot)
            {
                writer = default;
                return false;
            }

            EnsureCreated();
            writer = new Writer(slot);
            return true;
        }

        // ref struct so the view cannot be cached in a field or held across an await/yield: it is
        // only valid until the texture is next modified.
        public ref struct Writer
        {
            private readonly int _base;
            private NativeArray<byte> _data; // the texture's CPU copy, written in place.

            internal Writer(int slot)
            {
                _data = _texture!.GetRawTextureData<byte>();
                _base = slot * _channels;
            }

            // Uploads only on an actual change: most params are set once and never move again.
            public void Set(int channelId, float value)
            {
                // An overflowing channel lands in the next slot's row instead of out of bounds.
                Assert.IsTrue(channelId is >= 0 and < _channels, "channelId is out of range.");

                var dataIndex = _base + channelId;
                var valueByte = (byte)(Mathf.Clamp01(value) * 255);
                if (_data[dataIndex] == valueByte) return;
                _data[dataIndex] = valueByte;
                _needUpload = true;
            }
        }

        // Row center, as the v coordinate the shader samples _ParamTex with.
        public static float GetNormalizedIndex(int slot) => (slot + 0.5f) / _slotCount;

        private static void EnsureCreated()
        {
            if (_texture is not null) return;

            var isLinear = QualitySettings.activeColorSpace is ColorSpace.Linear;
            _texture = new Texture2D(_channels / 4, _slotCount, TextureFormat.RGBA32, false, isLinear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave, // XXX: To survive exiting play mode.
            };
            // No zero-init needed: a holder writes every channel its own shader reads, and this
            // first upload pushes the whole buffer, so CPU and GPU never disagree.
            _needUpload = true;
            Canvas.willRenderCanvases += Upload;
        }

        private static void Upload()
        {
            if (_needUpload is false) return;
            _needUpload = false;
            // makeNoLongerReadable stays false: dropping the CPU copy would invalidate the raw
            // view that Writer writes through.
            _texture!.Apply(false, false);
        }
    }
}
