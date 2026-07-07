#nullable enable
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    // F for Flip
    // M for Mirror
    // T for Tile
    // NF for No Fill (center)
    // NoTop for No Top border (top edge stretches; sprite border.w == 0)
    public enum UISliceMethod
    {
        Identity = 9,
        FX = 10,
        FY = 11,
        FXY = 12,
        MX = 0,
        MY = 17,
        MXY = 3,
        TX = 23,
        TY = 24,
        TX_MX_C3 = 25,
        R1C3 = 15,
        R3C3 = 13,
        R3C3_NF = 18,
        MX_R1C3 = 7,
        MX_R1C4 = 1,
        MX_R2C3_NoTop = 28, // MX_R3C3 without the top border row; body stretches to the top edge
        MX_R3C2 = 8,
        MX_R3C3 = 6,
        MX_R3C4 = 2,
        MX_R3C6 = 4,
        MY_R2C2 = 22,
        MY_R2C3 = 16,
        MY_R3C1 = 20,
        MY_R3C2 = 21,
        MY_R3C3 = 14,
        MXY_R3C2 = 29,
        MXY_R3C3 = 5,
        MXY_R3C3_NF = 19,
        CAP_MY = 26, // mirror-Y caps (top/bottom) tile in X; middle stretches
        CAP_MXY = 27, // tiled border frame from one edge sprite (border.w); L/R = top/bottom rotated 90°
    }

    [Icon("Packages/com.unity.ugui/Runtime/ThirdParty/Boxcat/Image/UISlice.png")]
    public sealed class UISlice : UIImageBase
#if UNITY_EDITOR
        , ISelfValidator
#endif
    {
        [SerializeField]
        [OnValueChanged("SetVerticesDirty")]
        private UISliceMethod _method;
        [SerializeField, Range(0.01f, 5f)]
        [OnValueChanged("SetVerticesDirty")]
        [HideIf("_borderMultiplier_HideIf")]
        private float _borderMultiplier = 1f;

        private static readonly DLog _log = new(nameof(UISlice));

        public UISliceMethod Method
        {
            get => _method;
            set
            {
                _method = value;
                SetVerticesDirty();
            }
        }

        public float BorderMultiplier
        {
            get => _borderMultiplier;
            set
            {
                if (_borderMultiplier.Equals(value))
                    return;
                _borderMultiplier = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
#if UNITY_EDITOR
            // Padding is not supported other than R3C3.
            if (!SupportsPadding(_method) && !UnityEngine.Sprites.DataUtility.GetPadding(sprite).EE0())
            {
                _log.e($"Padding is not supported: method={_method}, sprite={sprite.SafeName()}, padding={UnityEngine.Sprites.DataUtility.GetPadding(sprite)}");
            }
#endif

            var t = rectTransform;
            switch (_method)
            {
                case UISliceMethod.Identity:
                    UISliceMeshGen.ID(t, sprite, mb);
                    break;
                case UISliceMethod.FX:
                    UISliceMeshGen.FX(t, sprite, mb);
                    break;
                case UISliceMethod.FY:
                    UISliceMeshGen.FY(t, sprite, mb);
                    break;
                case UISliceMethod.FXY:
                    UISliceMeshGen.FXY(t, sprite, mb);
                    break;
                case UISliceMethod.MX:
                    UISliceMeshGen.MX(t, sprite, mb);
                    break;
                case UISliceMethod.MY:
                    UISliceMeshGen.MY(t, sprite, mb);
                    break;
                case UISliceMethod.MXY:
                    UISliceMeshGen.MXY(t, sprite, mb);
                    break;
                case UISliceMethod.TX:
                    UISliceMeshGen.TX(t, sprite, mb);
                    break;
                case UISliceMethod.TY:
                    UISliceMeshGen.TY(t, sprite, mb);
                    break;
                case UISliceMethod.TX_MX_C3:
                    UISliceMeshGen.TX_MX_C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.R1C3:
                    UISliceMeshGen.R1C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.R3C3:
                    UISliceMeshGen.R3C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.R3C3_NF:
                    UISliceMeshGen.R3C3_NF(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MX_R1C3:
                    UISliceMeshGen.MX_R1C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MX_R1C4:
                    UISliceMeshGen.MX_R1C4(t, sprite, mb);
                    break;
                case UISliceMethod.MX_R3C2:
                    UISliceMeshGen.MX_R3C2(t, sprite, mb);
                    break;
                case UISliceMethod.MX_R3C3:
                    UISliceMeshGen.MX_R3C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MX_R2C3_NoTop:
                    UISliceMeshGen.MX_R2C3_NoTop(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MX_R3C4:
                    UISliceMeshGen.MX_R3C4(t, sprite, mb);
                    break;
                case UISliceMethod.MX_R3C6:
                    UISliceMeshGen.MX_R3C6(t, sprite, mb);
                    break;
                case UISliceMethod.MY_R2C2:
                    UISliceMeshGen.MY_R2C2(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MY_R2C3:
                    UISliceMeshGen.MY_R2C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MY_R3C1:
                    UISliceMeshGen.MY_R3C1(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MY_R3C2:
                    UISliceMeshGen.MY_R3C2(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MY_R3C3:
                    UISliceMeshGen.MY_R3C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MXY_R3C2:
                    UISliceMeshGen.MXY_R3C2(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MXY_R3C3:
                    UISliceMeshGen.MXY_R3C3(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.MXY_R3C3_NF:
                    UISliceMeshGen.MXY_R3C3_NF(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.CAP_MY:
                    UISliceMeshGen.CAP_MY(t, sprite, _borderMultiplier, mb);
                    break;
                case UISliceMethod.CAP_MXY:
                    UISliceMeshGen.CAP_MXY(t, sprite, _borderMultiplier, mb);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            mb.Colors.SetUp(color, mb.Poses.Count);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetMaterialDirty();
            SetVerticesDirty();
            SetRaycastDirty();
        }

#if UNITY_EDITOR
        private bool _borderMultiplier_HideIf()
        {
            return _method
                is UISliceMethod.Identity
                or UISliceMethod.FX or UISliceMethod.FY or UISliceMethod.FXY
                or UISliceMethod.MX or UISliceMethod.MXY
                or UISliceMethod.TX or UISliceMethod.TY;
        }

        private static bool SupportsPadding(UISliceMethod method)
        {
            return method
                is UISliceMethod.Identity
                or UISliceMethod.FX or UISliceMethod.FY or UISliceMethod.FXY
                or UISliceMethod.MX or UISliceMethod.MY or UISliceMethod.MXY
                or UISliceMethod.TX or UISliceMethod.TY or UISliceMethod.TX_MX_C3
                or UISliceMethod.R3C3 or UISliceMethod.R3C3_NF;
        }

        private static bool MustBeQuad(UISliceMethod method)
        {
            return method
                is UISliceMethod.TX_MX_C3
                or UISliceMethod.R1C3 or UISliceMethod.R3C3 or UISliceMethod.R3C3_NF
                or UISliceMethod.MX_R1C3 or UISliceMethod.MX_R1C4
                or UISliceMethod.MX_R2C3_NoTop
                or UISliceMethod.MX_R3C2 or UISliceMethod.MX_R3C3 or UISliceMethod.MX_R3C4 or UISliceMethod.MX_R3C6
                or UISliceMethod.MY_R2C2 or UISliceMethod.MY_R2C3
                or UISliceMethod.MY_R3C1 or UISliceMethod.MY_R3C2 or UISliceMethod.MY_R3C3
                or UISliceMethod.MXY_R3C2 or UISliceMethod.MXY_R3C3 or UISliceMethod.MXY_R3C3_NF
                or UISliceMethod.CAP_MY or UISliceMethod.CAP_MXY;
        }

        void ISelfValidator.Validate(SelfValidationResult result)
        {
            if (Sprite && MustBeQuad(_method) && !Sprite.IsQuad())
                result.AddError($"Sprite must be a quad for method '{_method}'.");
        }
#endif
    }
}
