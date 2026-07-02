#nullable enable
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
        [Required]
        [FormerlySerializedAs("font"), FormerlySerializedAs("m_Font")]
        public Font Font = null!; // Required — always assigned.

        [HideIf("@BestFit")]
        [FormerlySerializedAs("fontSize"), FormerlySerializedAs("m_FontSize")]
        public int FontSize;

        [FormerlySerializedAs("fontStyle"), FormerlySerializedAs("m_FontStyle")]
        public FontStyle FontStyle;

        [HorizontalGroup("Toggles", order: 100), ToggleLeft]
        [FormerlySerializedAs("m_BestFit")]
        public bool BestFit;

        [HorizontalGroup("BestFit", Width = 0.7f, DisableAutomaticLabelWidth = true), LabelText("Font Size")]
        [ShowIf("@BestFit")]
        [FormerlySerializedAs("m_MinSize")]
        public int MinSize;

        [HorizontalGroup("BestFit", Width = 0.3f), HideLabel]
        [ShowIf("@BestFit")]
        [FormerlySerializedAs("m_MaxSize")]
        public int MaxSize;

        [FormerlySerializedAs("alignment"), FormerlySerializedAs("m_Alignment")]
        public TextAnchor Alignment;

        [HorizontalGroup("Toggles"), ToggleLeft]
        [FormerlySerializedAs("m_AlignByGeometry")]
        public bool AlignByGeometry;

        [HorizontalGroup("Toggles"), ToggleLeft]
        [FormerlySerializedAs("richText"), FormerlySerializedAs("m_RichText")]
        public bool RichText;

        [HorizontalGroup("WrapMode", DisableAutomaticLabelWidth = true), LabelText("Overflow (H/V)")]
        [FormerlySerializedAs("m_HorizontalOverflow")]
        public bool HorizontalOverflow;

        [HorizontalGroup("WrapMode", Width = 0.3f), HideLabel]
        [FormerlySerializedAs("m_VerticalOverflow")]
        public bool VerticalOverflow;

        [Range(0, 2)]
        [FormerlySerializedAs("m_LineSpacing")]
        public float LineSpacing;

        // Populate the font-config-driven fields of the generation settings in place. Component/transform-driven
        // fields (generationExtents, scaleFactor, pivot) are the caller's responsibility.
        public void SetGenerationSettings(ref TextGenerationSettings settings)
        {
            settings.font = Font;
            if (Font.dynamic)
            {
                settings.fontSize = FontSize;
                settings.resizeTextMinSize = MinSize;
                settings.resizeTextMaxSize = MaxSize;
            }

            settings.textAnchor = Alignment;
            settings.alignByGeometry = AlignByGeometry;
            settings.richText = RichText;
            settings.lineSpacing = LineSpacing;
            settings.fontStyle = FontStyle;
            settings.resizeTextForBestFit = BestFit;
            settings.updateBounds = false;
            settings.horizontalOverflow = HorizontalOverflow ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalOverflow ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
        }

#if UNITY_EDITOR
        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (MinSize != 0)
                result.AddError("Min Size is not supported anymore.");
        }
#endif
    }
}
