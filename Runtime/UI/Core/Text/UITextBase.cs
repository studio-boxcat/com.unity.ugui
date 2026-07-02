#nullable enable
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    // Shared base for font-driven text graphics (Text, LText): pooled TextGenerators, font-update
    // tracking, font-texture rebuild handling, and preferred-size layout inputs. The font itself is
    // subclass-owned — exposed through ActiveFont / PrepareFont.
    public abstract class UITextBase : Graphic, ILayoutElement, IFontUpdateListener
    {
        private TextGenerator? _textGen;
        private TextGenerator? _textGenForLayout;
        public TextGenerator TextGen => _textGen ??= TextGeneratorPool.Rent();
        public TextGenerator TextGenForLayout => _textGenForLayout ??= TextGeneratorPool.Rent();

        private FontUpdateLink _fontUpdateLink;


        protected UITextBase()
        {
            _fontUpdateLink = new FontUpdateLink(this);
        }

        // The font in use — resolves it when needed (e.g. theme lookup). May be a destroyed instance
        // when the owning bundle unloaded; callers check via the implicit bool.
        protected abstract Font ResolveFont();

        // The string measured for the layout inputs.
        public abstract string ResolveTextToRender();

        public abstract void GetGenerationSettings(Vector2 extents, ref TextGenerationSettings settings);

        // Convenience overload: allocate a fresh settings struct, populate it, and return it.
        public TextGenerationSettings GetGenerationSettings(Vector2 extents)
        {
            TextGenerationSettings settings = default;
            GetGenerationSettings(extents, ref settings);
            return settings;
        }

        // Graphic mesh entry — supplies the layout extents so subclasses don't recompute the rect size.
        protected sealed override void OnPopulateMesh(Color color, MeshBuilder mb) =>
            OnPopulateMesh(rectTransform.rect.size, color, mb);

        protected abstract void OnPopulateMesh(Vector2 extents, Color color, MeshBuilder mb);

        public override Texture mainTexture => ResolveFont().material.mainTexture;

        // Re-point font tracking after the subclass swaps fonts (no-op when not enabled/tracking).
        protected void UpdateFontTracking(Font font)
        {
            if (_fontUpdateLink.IsTracking())
                _fontUpdateLink.Update(font);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _fontUpdateLink.Update(ResolveFont());
            // Font texture might have been changed while this component was disabled.
            TextGen.Invalidate();
        }

        protected override void OnDisable()
        {
            _fontUpdateLink.Untrack();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            if (_textGen != null)
            {
                TextGeneratorPool.Return(_textGen);
                _textGen = null;
            }

            if (_textGenForLayout != null)
            {
                TextGeneratorPool.Return(_textGenForLayout);
                _textGenForLayout = null;
            }
        }

        // Set while populating so the font-texture-rebuilt callback can't recurse into regeneration.
        private bool _suppressFontTextureCallback;

        // Populate TextGen while muting the font-texture-rebuilt callback: glyph repacking mid-populate
        // must not recurse into regeneration — the end result is valid for this pass (Case 619238).
        protected void PopulateTextGen(string text, TextGenerationSettings settings)
        {
            _suppressFontTextureCallback = true;
            TextGen.PopulateWithErrors(text, settings, gameObject);
            _suppressFontTextureCallback = false;
        }

        /// <summary>
        /// Called by the FontUpdateTracker when the texture associated with a font is modified.
        /// </summary>
        void IFontUpdateListener.FontTextureChanged()
        {
            Assert.IsTrue(isActiveAndEnabled, "FontTextureChanged called on a disabled text.");

            if (_suppressFontTextureCallback)
                return;

            TextGen.Invalidate();

            // If the font texture changed mid-rebuild, regenerate immediately for the new UVs.
            if (CanvasUpdateRegistry.IsIdle()) SetVerticesDirty();
            else UpdateGeometry();
        }

        /// <summary>
        /// Provides information about how fonts are scaled to the screen: equivalent to the canvas scale
        /// factor (only dynamic fonts are supported).
        /// </summary>
        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                {
                    L.W("[UITextBase] Canvas is null, returning default scale factor of 1.", this);
                    return 1;
                }

                // An under-scaled canvas (game view smaller than the reference resolution) would rasterize
                // glyphs below their logical size and blur the scene view — never bake below 1:1.
                return Mathf.Max(localCanvas.scaleFactor, 1f);
            }
        }

        public float preferredWidth
        {
            get
            {
                TextGenerationSettings settings = default;
                GetGenerationSettings(Vector2.zero, ref settings);
                return TextGenForLayout.GetPreferredWidth(ResolveTextToRender(), settings) / pixelsPerUnit;
            }
        }

        public float preferredHeight
        {
            get
            {
                TextGenerationSettings settings = default;
                GetGenerationSettings(new Vector2(rectTransform.rect.size.x, 0.0f), ref settings);
                return TextGenForLayout.GetPreferredHeight(ResolveTextToRender(), settings) / pixelsPerUnit;
            }
        }

#if UNITY_EDITOR
        public override void OnRebuildRequested()
        {
            // After a Font asset re-import the managed side gets deleted and recreated, so the tracking
            // delegates and the generator caches are no longer valid.
            UpdateFontTracking(ResolveFont());
            TextGen.Invalidate();
            base.OnRebuildRequested();
        }
#endif
    }
}
