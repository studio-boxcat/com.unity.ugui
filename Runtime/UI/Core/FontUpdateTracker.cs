using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    public interface IFontUpdateListener
    {
        void FontTextureChanged();
    }

    public struct FontUpdateLink
    {
        readonly IFontUpdateListener _listener;
        [CanBeNull] Font _font;


        public FontUpdateLink(IFontUpdateListener listener)
        {
            _listener = listener;
            _font = null;
        }

        public bool IsTracking() => _font is not null;

        public void Update(Font font)
        {
            Assert.IsNotNull(_listener, "No listener to update");

            if (ReferenceEquals(_font, font))
                return;

            if (_font is not null)
                FontUpdateTracker.UntrackText(_font, _listener);
            _font = font;
            if (font is not null)
                FontUpdateTracker.TrackText(font, _listener);
        }

        public void Untrack()
        {
            Assert.IsNotNull(_listener, "No listener to untrack");

            if (_font is null)
                return;
            FontUpdateTracker.UntrackText(_font, _listener);
            _font = null;
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
        static readonly Dictionary<Font, HashSet<IFontUpdateListener>> _tracked = new(ReferenceEqualityComparer.Object);

        /// <summary>
        /// Register a Text element for receiving texture atlas rebuild calls.
        /// </summary>
        public static void TrackText(Font font, IFontUpdateListener listener)
        {
            Assert.IsNotNull(font, "Font is null");

            if (_tracked.TryGetValue(font, out var texts) == false)
            {
                // The textureRebuilt event is global for all fonts, so we add our delegate the first time we register *any* Text
                if (_tracked.Count == 0)
                {
                    _rebuildForFont ??= RebuildForFont;
                    Font.textureRebuilt += _rebuildForFont;
                }

                texts = new HashSet<IFontUpdateListener>();
                _tracked.Add(font, texts);
            }

            texts.Add(listener);
        }

        /// <summary>
        /// Deregister a Text element from receiving texture atlas rebuild calls.
        /// </summary>
        public static void UntrackText(Font font, IFontUpdateListener listener)
        {
            Assert.IsNotNull(font, "Font is null");

            var texts = _tracked[font];
            texts.Remove(listener);
            if (texts.Count != 0) return;

            _tracked.Remove(font);

            // There is a global textureRebuilt event for all fonts, so once the last Text reference goes away, remove our delegate
            if (_tracked.Count == 0)
                Font.textureRebuilt -= _rebuildForFont;
        }

        static Action<Font> _rebuildForFont;

        static void RebuildForFont(Font font)
        {
            if (_tracked.TryGetValue(font, out var listeners) == false)
                return;

            foreach (var listener in listeners)
            {
                Assert.IsNotNull((Object) listener);
                Assert.IsTrue(listener is not Text text || text.font == font);
                listener.FontTextureChanged();
            }
        }
    }
}