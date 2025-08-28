using System;
using Sirenix.OdinInspector;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    [GraphicPropertyHide(GraphicPropertyFlag.Raycast | GraphicPropertyFlag.Material)]
    public class Text : Graphic, ILayoutElement, IFontUpdateListener
    {
        [SerializeField, InlineProperty, HideLabel, PropertyOrder(500)]
        [OnValueChanged("FontData_OnValueChanged")]
        private FontData m_FontData;

        [SerializeField, PropertyOrder(-1)]
        [OnValueChanged("SetVerticesDirty"), OnValueChanged("SetLayoutDirty")]
        private string m_Text = string.Empty;

        private TextGenerator m_TextCache;
        private TextGenerator m_TextCacheForLayout;
        public TextGenerator cachedTextGenerator => m_TextCache ??= TextGeneratorPool.Rent();
        public TextGenerator cachedTextGeneratorForLayout => m_TextCacheForLayout ??= TextGeneratorPool.Rent();

        // We use this flag instead of Unregistering/Registering the callback to avoid allocation.
        [NonSerialized] protected bool m_DisableFontTextureRebuiltCallback = false;

        FontUpdateLink _fontUpdateLink;

        protected Text()
        {
            _fontUpdateLink = new FontUpdateLink(this);
        }

        /// <summary>
        /// Text's texture comes from the font.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (font != null && font.material != null && font.material.mainTexture != null)
                    return font.material.mainTexture;

                if (m_Material != null)
                    return m_Material.mainTexture;

                return base.mainTexture;
            }
        }

        /// <summary>
        /// Called by the FontUpdateTracker when the texture associated with a font is modified.
        /// </summary>
        void IFontUpdateListener.FontTextureChanged()
        {
            Assert.IsTrue(isActiveAndEnabled);

            if (m_DisableFontTextureRebuiltCallback)
                return;

            cachedTextGenerator.Invalidate();

            // this is a bit hacky, but it is currently the
            // cleanest solution....
            // if we detect the font texture has changed and are in a rebuild loop
            // we just regenerate the verts for the new UV's
            if (CanvasUpdateRegistry.IsIdle()) SetVerticesDirty();
            else UpdateGeometry();
        }

        /// <summary>
        /// The Font used by the text.
        /// </summary>
        /// <remarks>
        /// This is the font used by the Text component. Use it to alter or return the font from the Text. There are many free fonts available online.
        /// </remarks>
        public Font font
        {
            get => m_FontData.font;
            set
            {
                if (ReferenceEquals(m_FontData.font, value))
                    return;

                m_FontData.font = value;

                if (_fontUpdateLink.IsTracking())
                    _fontUpdateLink.Update(value);

                SetAllDirty();
            }
        }

        /// <summary>
        /// Text that's being displayed by the Text.
        /// </summary>
        /// <remarks>
        /// This is the string value of a Text component. Use this to read or edit the message displayed in Text.
        /// </remarks>
        public virtual string text
        {
            get
            {
                return m_Text;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (String.IsNullOrEmpty(m_Text))
                        return;
                    m_Text = "";
                    SetVerticesDirty();
                }
                else if (m_Text != value)
                {
                    m_Text = value;
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        /// <summary>
        /// Whether this Text will support rich text.
        /// </summary>
        public bool supportRichText
        {
            get
            {
                return m_FontData.richText;
            }
            set
            {
                if (m_FontData.richText == value)
                    return;
                m_FontData.richText = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Should the text be allowed to auto resized.
        /// </summary>
        public bool resizeTextForBestFit
        {
            get
            {
                return m_FontData.bestFit;
            }
            set
            {
                if (m_FontData.bestFit == value)
                    return;
                m_FontData.bestFit = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// The minimum size the text is allowed to be.
        /// </summary>
        public int resizeTextMinSize
        {
            get
            {
                return m_FontData.minSize;
            }
            set
            {
                if (m_FontData.minSize == value)
                    return;
                m_FontData.minSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// The maximum size the text is allowed to be. 1 = infinitely large.
        /// </summary>
        public int resizeTextMaxSize
        {
            get
            {
                return m_FontData.maxSize;
            }
            set
            {
                if (m_FontData.maxSize == value)
                    return;
                m_FontData.maxSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// The positioning of the text reliative to its [[RectTransform]].
        /// </summary>
        /// <remarks>
        /// This is the positioning of the Text relative to its RectTransform. You can alter this via script or in the Inspector of a Text component using the buttons in the Alignment section.
        /// </remarks>
        public TextAnchor alignment
        {
            get
            {
                return m_FontData.alignment;
            }
            set
            {
                if (m_FontData.alignment == value)
                    return;
                m_FontData.alignment = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Use the extents of glyph geometry to perform horizontal alignment rather than glyph metrics.
        /// </summary>
        /// <remarks>
        /// This can result in better fitting left and right alignment, but may result in incorrect positioning when attempting to overlay multiple fonts (such as a specialized outline font) on top of each other.
        /// </remarks>
        public bool alignByGeometry
        {
            get
            {
                return m_FontData.alignByGeometry;
            }
            set
            {
                if (m_FontData.alignByGeometry == value)
                    return;
                m_FontData.alignByGeometry = value;

                SetVerticesDirty();
            }
        }

        /// <summary>
        /// The size that the Font should render at. Unit of measure is Points.
        /// </summary>
        /// <remarks>
        /// This is the size of the Font of the Text. Use this to fetch or change the size of the Font. When changing the Font size, remember to take into account the RectTransform of the Text. Larger Font sizes or messages may not fit in certain rectangle sizes and do not show in the Scene.
        /// Note: Point size is not consistent from one font to another.
        /// </remarks>
        public int fontSize
        {
            get
            {
                return m_FontData.fontSize;
            }
            set
            {
                if (m_FontData.fontSize == value)
                    return;
                m_FontData.fontSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Horizontal overflow mode.
        /// </summary>
        /// <remarks>
        /// When set to HorizontalWrapMode.Overflow, text can exceed the horizontal boundaries of the Text graphic. When set to HorizontalWrapMode.Wrap, text will be word-wrapped to fit within the boundaries.
        /// </remarks>
        public HorizontalWrapMode horizontalOverflow
        {
            get
            {
                return m_FontData.horizontalOverflow;
            }
            set
            {
                if (m_FontData.horizontalOverflow == value)
                    return;
                m_FontData.horizontalOverflow = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Vertical overflow mode.
        /// </summary>
        public VerticalWrapMode verticalOverflow
        {
            get
            {
                return m_FontData.verticalOverflow;
            }
            set
            {
                if (m_FontData.verticalOverflow == value)
                    return;
                m_FontData.verticalOverflow = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Line spacing, specified as a factor of font line height. A value of 1 will produce normal line spacing.
        /// </summary>
        public float lineSpacing
        {
            get
            {
                return m_FontData.lineSpacing;
            }
            set
            {
                if (m_FontData.lineSpacing == value)
                    return;
                m_FontData.lineSpacing = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Font style used by the Text's text.
        /// </summary>

        public FontStyle fontStyle
        {
            get
            {
                return m_FontData.fontStyle;
            }
            set
            {
                if (m_FontData.fontStyle == value)
                    return;
                m_FontData.fontStyle = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Provides information about how fonts are scale to the screen.
        /// </summary>
        /// <remarks>
        /// For dynamic fonts, the value is equivalent to the scale factor of the canvas. For non-dynamic fonts, the value is calculated from the requested text size and the size from the font.
        /// </remarks>
        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                    return 1;
                // For dynamic fonts, ensure we use one pixel per pixel on the screen.
                if (!font || font.dynamic)
                    return localCanvas.scaleFactor;
                // For non-dynamic fonts, calculate pixels per unit based on specified font size relative to font object's own font size.
                if (m_FontData.fontSize <= 0 || font.fontSize <= 0)
                    return 1;
                return font.fontSize / (float) m_FontData.fontSize;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            cachedTextGenerator.Invalidate();
            _fontUpdateLink.Update(font);
        }

        protected override void OnDisable()
        {
            _fontUpdateLink.Untrack();
            base.OnDisable();
        }

        void OnDestroy()
        {
            if (m_TextCache != null)
            {
                TextGeneratorPool.Return(m_TextCache);
                m_TextCache = null;
            }

            if (m_TextCacheForLayout != null)
            {
                TextGeneratorPool.Return(m_TextCacheForLayout);
                m_TextCacheForLayout = null;
            }
        }

        protected override void UpdateGeometry()
        {
            if (font != null)
            {
                base.UpdateGeometry();
            }
        }

        /// <summary>
        /// Convenience function to populate the generation setting for the text.
        /// </summary>
        /// <param name="extents">The extents the text can draw in.</param>
        /// <returns>Generated settings.</returns>
        public TextGenerationSettings GetGenerationSettings(Vector2 extents)
        {
            var settings = new TextGenerationSettings();

            settings.generationExtents = extents;
            if (font != null && font.dynamic)
            {
                settings.fontSize = m_FontData.fontSize;
                settings.resizeTextMinSize = m_FontData.minSize;
                settings.resizeTextMaxSize = m_FontData.maxSize;
            }

            // Other settings
            settings.textAnchor = m_FontData.alignment;
            settings.alignByGeometry = m_FontData.alignByGeometry;
            settings.scaleFactor = pixelsPerUnit;
            settings.color = color;
            settings.font = font;
            settings.pivot = rectTransform.pivot;
            settings.richText = m_FontData.richText;
            settings.lineSpacing = m_FontData.lineSpacing;
            settings.fontStyle = m_FontData.fontStyle;
            settings.resizeTextForBestFit = m_FontData.bestFit;
            settings.updateBounds = false;
            settings.horizontalOverflow = m_FontData.horizontalOverflow;
            settings.verticalOverflow = m_FontData.verticalOverflow;

            return settings;
        }

        protected override void OnPopulateMesh(MeshBuilder toFill)
        {
            if (!font || string.IsNullOrEmpty(text))
                return;

            // We don't care if we the font Texture changes while we are doing our Update.
            // The end result of cachedTextGenerator will be valid for this instance.
            // Otherwise we can get issues like Case 619238.
            m_DisableFontTextureRebuiltCallback = true;
            var extents = rectTransform.rect.size;
            var settings = GetGenerationSettings(extents);
            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
            m_DisableFontTextureRebuiltCallback = false;

            TextMeshUtils.Translate(cachedTextGenerator, pixelsPerUnit, 0, toFill);
        }

        public void ForcePopulateMesh(MeshBuilder toFill) => OnPopulateMesh(toFill);

        public float preferredWidth
        {
            get
            {
                var settings = GetGenerationSettings(Vector2.zero);
                return cachedTextGeneratorForLayout.GetPreferredWidth(m_Text, settings) / pixelsPerUnit;
            }
        }

        public float preferredHeight
        {
            get
            {
                var settings = GetGenerationSettings(new Vector2(GetPixelAdjustedRect().size.x, 0.0f));
                return cachedTextGeneratorForLayout.GetPreferredHeight(m_Text, settings) / pixelsPerUnit;
            }
        }

#if UNITY_EDITOR
        public override void OnRebuildRequested()
        {
            // After a Font asset gets re-imported the managed side gets deleted and recreated,
            // that means the delegates are not persisted.
            // so we need to properly enforce a consistent state here.
            if (_fontUpdateLink.IsTracking())
                _fontUpdateLink.Update(font);

            // Also the textgenerator is no longer valid.
            cachedTextGenerator.Invalidate();

            base.OnRebuildRequested();
        }

        // The Text inspector editor can change the font, and we need a way to track changes so that we get the appropriate rebuild callbacks
        // We can intercept changes in OnValidate, and keep track of the previous font reference
        private void FontData_OnValueChanged()
        {
            if (_fontUpdateLink.IsTracking())
                _fontUpdateLink.Update(m_FontData.font);
            SetVerticesDirty();
            SetLayoutDirty();
        }
#endif // if UNITY_EDITOR
    }
}