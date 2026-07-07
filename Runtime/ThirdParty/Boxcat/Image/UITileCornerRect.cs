#nullable enable
using Sirenix.OdinInspector;
using UnityEngine;

namespace UnityEngine.UI
{
    // Tiled border frame: the four edges tile from a single edge sprite (CAP_MXY, thickness = border.w),
    // the centre is a solid stretch, and the four corners are drawn from a separate first-quadrant
    // quarter-circle sprite mirrored across the corners (cf. UIRoundedRect). Both sprites must live on one
    // texture so the frame stays a single draw call — enforced by Validate(). See [[ui-custom-components.md]].
    public class UITileCornerRect : UIImageBase
    {
        [SerializeField, Required]
        [OnValueChanged("SetVerticesDirty")]
        private Sprite _cornerSprite;

        // Which corners get the quarter-circle sprite; the rest stay plain CAP_MXY (edges overlap).
        [SerializeField]
        [OnValueChanged("SetVerticesDirty")]
        private Corner _corners = Corner.All;

        // Corner square size as a multiple of the corner sprite's native size (factor 1 = native).
        [SerializeField, Range(0.01f, 5f)]
        [OnValueChanged("SetVerticesDirty")]
        private float _cornerSizeFactor = 1f;

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            if (!_cornerSprite)
            {
                mb.SetUp_Empty();
                return;
            }

            Tiling.TileCornerRect(rectTransform, sprite, _cornerSprite, _corners, _cornerSizeFactor, mb);
            mb.Colors.SetUp(color, mb.Poses.Count);
        }

#if UNITY_EDITOR
        public override void Validate(SelfValidationResult result)
        {
            base.Validate(result);

            var edge = Sprite;
            var corner = _cornerSprite;
            if (!edge || !corner)
                return; // null sprites are already flagged by [Required].

            // One texture keeps edge + corners in a single draw call (both sample the shared atlas UVs).
            if (corner.texture.RefNq(edge.texture))
                result.AddError($"Corner sprite '{corner.name}' must be on the same texture as edge sprite '{edge.name}'.");
        }
#endif
    }
}
