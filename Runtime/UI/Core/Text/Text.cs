using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    [GraphicPropertyHide(GraphicPropertyFlag.Raycast | GraphicPropertyFlag.Material)]
    public sealed class Text : UITextBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField, InlineProperty, HideLabel, PropertyOrder(500)]
        [OnValueChanged("FontData_OnValueChanged")]
        private FontData m_FontData;
        [SerializeField, PropertyOrder(-1)]
        [OnValueChanged("SetVerticesDirty"), OnValueChanged("SetLayoutDirty")]
        private string m_Text = string.Empty;

        protected override Font ResolveFont() => m_FontData.Font;
        public override string ResolveTextToRender() => m_Text;

        public Font font => m_FontData.Font;

        public string text
        {
            get => m_Text;
            set
            {
                if (m_Text == value) return;
                m_Text = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public bool supportRichText => m_FontData.RichText;

        public int fontSize
        {
            get => m_FontData.FontSize;
            set
            {
                if (m_FontData.FontSize == value) return;
                m_FontData.FontSize = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public bool horizontalOverflow => m_FontData.HorizontalOverflow;
        public bool verticalOverflow => m_FontData.VerticalOverflow;

        /// <summary>
        /// Convenience function to populate the generation setting for the text.
        /// </summary>
        /// <param name="extents">The extents the text can draw in.</param>
        /// <param name="settings">Settings to populate in place.</param>
        public override void GetGenerationSettings(Vector2 extents, ref TextGenerationSettings settings)
        {
            settings.generationExtents = extents;
            settings.scaleFactor = pixelsPerUnit;
            settings.pivot = rectTransform.pivot;
            m_FontData.SetGenerationSettings(ref settings);
        }

        protected override void OnPopulateMesh(Vector2 extents, Color color, MeshBuilder toFill)
        {
            if (string.IsNullOrEmpty(text))
                return;

            TextGenerationSettings settings = default;
            GetGenerationSettings(extents, ref settings);
            settings.color = color;
            PopulateTextGen(text, settings);

            TextMeshUtils.Translate(TextGen, pixelsPerUnit, 0, toFill);
        }

        public void ForcePopulateMesh(MeshBuilder toFill) => OnPopulateMesh(color, toFill);

#if UNITY_EDITOR
        // The Text inspector editor can change the font, and we need a way to track changes so that we get the appropriate rebuild callbacks
        // We can intercept changes in OnValidate, and keep track of the previous font reference
        private void FontData_OnValueChanged()
        {
            UpdateFontTracking(m_FontData.Font);
            SetVerticesDirty();
            SetLayoutDirty();
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (font.dynamic is false)
                result.AddError($"Non-dynamic font '{font.name}' used.");
            // geometry + no vertical overflow -> potential text clipping, when sizeDelta set to preferredHeight by ILayoutController ContentSizeFitter.
            if (m_FontData.VerticalOverflow is false && m_FontData.AlignByGeometry)
                result.AddError("Align By Geometry is not compatible with Vertical Overflow set to Truncate. This may cause text clipping.");
        }
#endif // if UNITY_EDITOR
    }
}
