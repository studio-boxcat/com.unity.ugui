#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public interface IFontUpdateListener
    {
        void FontTextureChanged();
    }

    public struct FontUpdateLink
    {
        private readonly IFontUpdateListener _listener;
        private int _fontId; // It is always unique, and never has the value 0.


        public FontUpdateLink(IFontUpdateListener listener)
        {
            _listener = listener;
            _fontId = 0;
        }

        public bool IsTracking() => _fontId is not 0;

        public void Update(Font font)
        {
            Assert.IsNotNull(_listener, "No listener to update");
            Assert.IsFalse(font is null, "Given font is null");

            var fontId = font!.GetInstanceID();
            if (fontId == _fontId)
                return;

            // Untrack the previous font.
            if (_fontId is not 0)
                FontUpdateTracker.UntrackText(_fontId, _listener);

            _fontId = fontId;

            // Track the new font.
            if (fontId is not 0)
                FontUpdateTracker.TrackText(fontId, _listener);
        }

        public void Untrack()
        {
            Assert.IsNotNull(_listener, "No listener to untrack");

            if (_fontId is 0)
                return;
            FontUpdateTracker.UntrackText(_fontId, _listener);
            _fontId = 0;
        }
    }

    /// <summary>
    /// Utility class that is used to help with Text update.
    /// </summary>
    /// <remarks>
    /// When Unity rebuilds a font atlas a callback is sent to the font. Using this class you can register your text as needing to be rebuilt if the font atlas is updated.
    /// </remarks>
    public static class FontUpdateTracker
    {
        private static readonly Dictionary<int, HashSet<IFontUpdateListener>> _tracked = new();
        private static readonly Dictionary<int, ulong> _fontTexHashes = new();

        /// <summary>
        /// Register a Text element for receiving texture atlas rebuild calls.
        /// </summary>
        public static void TrackText(int fontId, IFontUpdateListener listener)
        {
            if (_tracked.TryGetValue(fontId, out var listeners) == false)
            {
                // The textureRebuilt event is global for all fonts, so we add our delegate the first time we register *any* Text
                if (_tracked.Count == 0)
                {
                    _rebuildForFont ??= RebuildForFont;
                    Font.textureRebuilt += _rebuildForFont;
                }

                listeners = new HashSet<IFontUpdateListener>();
                _tracked.Add(fontId, listeners);
            }

            listeners.Add(listener);
        }

        /// <summary>
        /// Deregister a Text element from receiving texture atlas rebuild calls.
        /// </summary>
        public static void UntrackText(int fontId, IFontUpdateListener listener)
        {
            var listeners = _tracked[fontId];
            listeners.Remove(listener);
            if (listeners.Count != 0) return;

            _tracked.Remove(fontId);

            // There is a global textureRebuilt event for all fonts, so once the last Text reference goes away, remove our delegate
            if (_tracked.Count == 0)
                Font.textureRebuilt -= _rebuildForFont;
        }

        private static Action<Font>? _rebuildForFont;

        private static void RebuildForFont(Font font)
        {
            var fontId = font.GetInstanceID();
            if (_tracked.TryGetValue(fontId, out var listeners) == false)
                return;

            var newHash = 0ul;
            if (_fontTexHashes.TryGetValue(fontId, out var lastHash)
                && lastHash == (newHash = CalcFontTexHash(font)))
            {
                // No change in the texture, so we don't need to rebuild.
                return;
            }

            _fontTexHashes[fontId] = newHash;
            L.I($"[UGUI] Rebuild for font: {font.name}, hash: {newHash:X16}");

            foreach (var listener in listeners)
            {
                Assert.IsTrue((Object) listener, "IFontUpdateListener is destroyed");
                Assert.IsTrue(listener is not Text text || text.font == font);
                listener.FontTextureChanged();
            }
            return;

            static ulong CalcFontTexHash(Font font)
            {
                // Use the texture's update count as a hash.
                var tex = font.material.mainTexture;
                var instanceId = tex.GetInstanceID();
                var updateCount = tex.updateCount;
                return (((ulong) instanceId) << 32) | updateCount;
            }
        }
    }
}