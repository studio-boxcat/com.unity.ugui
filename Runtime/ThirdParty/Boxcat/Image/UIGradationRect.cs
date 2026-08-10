#nullable enable
using Sirenix.OdinInspector;

namespace UnityEngine.UI
{
    public class UIGradationRect : UIImageBase
    {
        [SerializeField, OnValueChanged("SetVerticesDirty"), ValidateInput("_value_Validate")]
        private Gradient _value = null!;

        [SerializeField, PropertyOrder(550)]
        private bool _horizontal;
        public bool Horizontal
        {
            get => _horizontal;
            set
            {
                if (value.CmpSet(ref _horizontal))
                    SetVerticesDirty();
            }
        }


        public Gradient Gradient
        {
            get => _value;
            set
            {
                _value = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb) =>
            UIGradationMeshGen.Populate(_value, _horizontal, rectTransform.rect, color, sprite, mb);

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _value = new Gradient();
        }

        private bool _value_Validate(ref string errorMessage) =>
            UIGradationMeshGen.Validate(_value, ref errorMessage);

        [Button, FoldoutGroup(GraphicEditorConst.Advanced)]
        private void Sample8(Object texOrSprite)
        {
            UnityEditor.Undo.RecordObject(this, "Sample8");

            Texture2D tex;
            Rect rect;

            if (texOrSprite is Texture2D texture)
            {
                tex = texture;
                rect = new Rect(0, 0, tex.width, tex.height);
            }
            else if (texOrSprite is Sprite sprite)
            {
                tex = sprite.texture;
                rect = sprite.textureRect;
            }
            else
            {
                throw new System.ArgumentException("Argument must be Texture or Sprite");
            }

            var th = tex.height;
            var u = rect.MidX() / tex.width;

            // sample points
            var c = new GradientColorKey[8];
            var a = new GradientAlphaKey[8];
            for (var i = 0; i < 8; i++)
            {
                var t = i / 7f;
                var v = (rect.y + rect.height * t) / th;
                var p = tex.GetPixelBilinear(u, v);
                c[i] = new GradientColorKey(p, t);
                a[i] = new GradientAlphaKey(p.a, t);
            }

            _value.colorKeys = c;
            _value.alphaKeys = a;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
