using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [Serializable]
    public class FontData
#if UNITY_EDITOR
        : ISelfValidator
#endif
    {
        [SerializeField]
        [FormerlySerializedAs("font")]
        private Font m_Font;

        [SerializeField, HideIf("@m_BestFit")]
        [FormerlySerializedAs("fontSize")]
        private int m_FontSize;

        [SerializeField]
        [FormerlySerializedAs("fontStyle")]
        private FontStyle m_FontStyle;

        [SerializeField, HorizontalGroup("Toggles", order: 100), ToggleLeft]
        private bool m_BestFit;

        [SerializeField, HorizontalGroup("BestFit", Width = 0.7f, DisableAutomaticLabelWidth = true), LabelText("Font Size")]
        [ShowIf("@m_BestFit")]
        private int m_MinSize;

        [SerializeField, HorizontalGroup("BestFit", Width = 0.3f), HideLabel]
        [ShowIf("@m_BestFit")]
        private int m_MaxSize;

        [SerializeField]
        [FormerlySerializedAs("alignment")]
        private TextAnchor m_Alignment;

        [SerializeField, HorizontalGroup("Toggles"), ToggleLeft]
        private bool m_AlignByGeometry;

        [SerializeField, HorizontalGroup("Toggles"), ToggleLeft]
        [FormerlySerializedAs("richText")]
        private bool m_RichText;

        [SerializeField, HorizontalGroup("WrapMode", DisableAutomaticLabelWidth = true), LabelText("WrapMode (H/V)")]
        private HorizontalWrapMode m_HorizontalOverflow;

        [SerializeField, HorizontalGroup("WrapMode", Width = 0.3f), HideLabel]
        private VerticalWrapMode m_VerticalOverflow;

        [SerializeField, Range(0, 2)]
        private float m_LineSpacing;

        /// <summary>
        /// The Font to use for this generated Text object.
        /// </summary>
        public Font font
        {
            get { return m_Font; }
            set { m_Font = value; }
        }

        /// <summary>
        /// The Font size to use for this generated Text object.
        /// </summary>
        public int fontSize
        {
            get { return m_FontSize; }
            set { m_FontSize = value; }
        }

        /// <summary>
        /// The font style to use for this generated Text object.
        /// </summary>
        public FontStyle fontStyle
        {
            get { return m_FontStyle; }
            set { m_FontStyle = value; }
        }

        /// <summary>
        /// Is best fit used for this generated Text object.
        /// </summary>
        public bool bestFit
        {
            get { return m_BestFit; }
            set { m_BestFit = value; }
        }

        /// <summary>
        /// The min size for this generated Text object.
        /// </summary>
        public int minSize
        {
            get { return m_MinSize; }
            set { m_MinSize = value; }
        }

        /// <summary>
        /// The max size for this generated Text object.
        /// </summary>
        public int maxSize
        {
            get { return m_MaxSize; }
            set { m_MaxSize = value; }
        }

        /// <summary>
        /// How is the text aligned for this generated Text object.
        /// </summary>
        public TextAnchor alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        /// <summary>
        /// Use the extents of glyph geometry to perform horizontal alignment rather than glyph metrics.
        /// </summary>
        /// <remarks>
        /// This can result in better fitting left and right alignment, but may result in incorrect positioning when attempting to overlay multiple fonts (such as a specialized outline font) on top of each other.
        /// </remarks>
        public bool alignByGeometry
        {
            get { return m_AlignByGeometry; }
            set { m_AlignByGeometry = value; }
        }

        /// <summary>
        /// Should rich text be used for this generated Text object.
        /// </summary>
        public bool richText
        {
            get { return m_RichText; }
            set { m_RichText = value; }
        }

        /// <summary>
        /// The horizontal overflow policy for this generated Text object.
        /// </summary>
        public HorizontalWrapMode horizontalOverflow
        {
            get { return m_HorizontalOverflow; }
            set { m_HorizontalOverflow = value; }
        }

        /// <summary>
        /// The vertical overflow policy for this generated Text object.
        /// </summary>
        public VerticalWrapMode verticalOverflow
        {
            get { return m_VerticalOverflow; }
            set { m_VerticalOverflow = value; }
        }

        /// <summary>
        /// The line spaceing for this generated Text object.
        /// </summary>
        public float lineSpacing
        {
            get { return m_LineSpacing; }
            set { m_LineSpacing = value; }
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (m_MinSize != 0)
                result.AddError("Min Size is not supported anymore.");
        }
#endif
    }
}