// ReSharper disable CompareOfFloatsByEqualityOperator

#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

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

        private static readonly List<(float Pos, Color Color)> _points = new();

        protected override void OnPopulateMesh(Sprite sprite, Color color, MeshBuilder mb)
        {
            Assert.IsTrue(_points.Count == 0, "Points should be empty");

            MergeGradientPoints(_value, _points);

            var pointCount = _points.Count;
            var quadCount = pointCount + 1;
            var start0 = _points[0].Pos == 0;
            var end1 = _points[pointCount - 1].Pos == 1;
            if (start0) quadCount--;
            if (end1) quadCount--;

            var vertexCount = quadCount * 4; // TODO: Share in-between vertices.

            var rect = rectTransform.rect;
            var w = rect.width;
            var h = rect.height;
            var l = rect.min.x;
            var r = l + w;
            var b = rect.min.y;
            var t = b + h;

            var poses = mb.Poses.SetUp(vertexCount);
            var colors = mb.Colors.SetUp(vertexCount);

            var vertexIndex = 0;

            // 2 3
            // 0 1
            if (_horizontal)
            {
                var x = l + w * _points[0].Pos;
                Color32 c = _points[0].Color;

                if (start0 is false)
                {
                    SetPose(poses, vertexIndex, l, x, b, t);
                    SetColor(colors, vertexIndex, c);
                    vertexIndex += 4;
                }

                for (var i = 1; i < pointCount; i++)
                {
                    var p = _points[i];
                    var prevColor = c;
                    c = p.Color;
                    var prevX = x;
                    x = l + w * p.Pos;

                    SetPose(poses, vertexIndex, prevX, x, b, t);
                    SetColor(colors, vertexIndex, prevColor, c, prevColor, c);
                    vertexIndex += 4;
                }

                if (end1 is false)
                {
                    SetPose(poses, vertexIndex, x, r, b, t);
                    SetColor(colors, vertexIndex, c);
                }
            }
            else
            {
                var y = b + h * _points[0].Pos;
                Color32 c = _points[0].Color;

                if (start0 is false)
                {
                    SetPose(poses, vertexIndex, l, r, b, y);
                    SetColor(colors, vertexIndex, c);
                    vertexIndex += 4;
                }

                for (var i = 1; i < pointCount; i++)
                {
                    var p = _points[i];
                    var prevColor = c;
                    c = p.Color;
                    var prevY = y;
                    y = b + h * p.Pos;

                    SetPose(poses, vertexIndex, l, r, prevY, y);
                    SetColor(colors, vertexIndex, prevColor, prevColor, c, c);
                    vertexIndex += 4;
                }

                if (end1 is false)
                {
                    SetPose(poses, vertexIndex, l, r, y, t);
                    SetColor(colors, vertexIndex, c);
                }
            }

            // Multiply color with base graphic color.
            var graphicColor = color;
            for (var i = 0; i < vertexCount; i++)
                colors[i] = graphicColor * colors[i];

            // Setup other channels.
            mb.UVs.SetUp(SolidUVCache.Get(sprite), vertexCount);
            mb.Indices.SetUp_Quad(quadCount);
            _points.Clear();
        }

        // Emits one point at every color-key AND alpha-key time (their union), so each band
        // between consecutive points varies linearly in both color and alpha — letting GPU
        // vertex interpolation reproduce the gradient exactly. Color is delegated to
        // Gradient.Evaluate (Blend-mode only; Fixed is rejected at authoring and asserted below).
        public static void MergeGradientPoints(Gradient gradient, List<(float Pos, Color Color)> points)
        {
            Assert.IsTrue(points.Count == 0, "Points should be empty");
            Assert.IsTrue(gradient.mode != GradientMode.Fixed, "Fixed gradient mode is not supported");

            var colorKeys = gradient.colorKeys;
            var alphaKeys = gradient.alphaKeys;
            int colorIndex = 0, alphaIndex = 0;

            while (colorIndex < colorKeys.Length || alphaIndex < alphaKeys.Length)
            {
                var colorTime = colorIndex < colorKeys.Length ? colorKeys[colorIndex].time : float.MaxValue;
                var alphaTime = alphaIndex < alphaKeys.Length ? alphaKeys[alphaIndex].time : float.MaxValue;
                var t = Mathf.Min(colorTime, alphaTime);

                // Advance both arrays when their times coincide, merging the shared time into one point.
                if (colorTime == t) colorIndex++;
                if (alphaTime == t) alphaIndex++;

                points.Add((t, gradient.Evaluate(t)));
                Assert.IsTrue(points.Count <= colorKeys.Length + alphaKeys.Length, "Too many points");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetPose(Vector3[] poses, int i, float l, float r, float b, float t)
        {
            poses[i] = new(l, b);
            poses[i + 1] = new(r, b);
            poses[i + 2] = new(l, t);
            poses[i + 3] = new(r, t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetColor(Color32[] colors, int i, Color32 c)
        {
            colors[i] = c;
            colors[i + 1] = c;
            colors[i + 2] = c;
            colors[i + 3] = c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetColor(Color32[] colors, int i, Color32 bl, Color32 br, Color32 tl, Color32 tr)
        {
            colors[i] = bl;
            colors[i + 1] = br;
            colors[i + 2] = tl;
            colors[i + 3] = tr;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _value = new Gradient();
        }

        private bool _value_Validate(ref string errorMessage)
        {
            // Note that the alpha and colors keys will be automatically sorted by time value
            // and that it is ensured to always have a minimum of 2 color keys and 2 alpha keys.
            if (_value.alphaKeys.Length < 2)
            {
                errorMessage = "At least 2 alpha keys are required";
                return false;
            }

            if (_value.colorKeys.Length < 2)
            {
                errorMessage = "At least 2 color keys are required";
                return false;
            }

            if (_value.mode is GradientMode.Fixed)
            {
                errorMessage = "Fixed gradient mode is used";
                return false;
            }

            return true;
        }

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
