#nullable enable
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    // ID: Identity
    // FX, FY: Flip X, Flip Y
    // MX, MY: Mirror X, Mirror Y
    public static class UISliceMeshGen
    {
        public static void ID(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var (scale, offset) = GetPosMapping(t, sprite, sprite.CalcNormPivot());
            SpriteMeshWriter.ScaleAdd(sprite, scale.x, offset.x, scale.y, offset.y, mb);
        }

        public static void FX(RectTransform t, Sprite sprite, MeshBuilder mb) => Flip(t, sprite, flipX: true, flipY: false, mb);
        public static void FY(RectTransform t, Sprite sprite, MeshBuilder mb) => Flip(t, sprite, flipX: false, flipY: true, mb);
        public static void FXY(RectTransform t, Sprite sprite, MeshBuilder mb) => Flip(t, sprite, flipX: true, flipY: true, mb);

        public static void TX(RectTransform t, Sprite sprite, MeshBuilder mb) => Tiling.TileX(t, sprite, mb);
        public static void TY(RectTransform t, Sprite sprite, MeshBuilder mb) => Tiling.TileY(t, sprite, mb);
        public static void TX_MX_C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb) => Tiling.TileX_MX_C3(t, sprite, borderMult, mb);

        // Flip: rect-fitted 1:1 copy, mirroring the pivot + negating the scale on each flipped axis.
        private static void Flip(RectTransform t, Sprite sprite, bool flipX, bool flipY, MeshBuilder mb)
        {
            var spritePivot = sprite.CalcNormPivot();
            if (flipX) spritePivot.x = 1 - spritePivot.x;
            if (flipY) spritePivot.y = 1 - spritePivot.y;
            var (scale, offset) = GetPosMapping(t, sprite, spritePivot);
            if (flipX) scale.x = -scale.x;
            if (flipY) scale.y = -scale.y;
            SpriteMeshWriter.ScaleAdd(sprite, scale.x, offset.x, scale.y, offset.y, mb);
        }

        public static void MX(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var (scale, tr, offset) = GetSliceVertexTranslation(
                t, sprite, new Vector2(0.5f, 1), new Vector2(0.5f, 0));
            float ax = tr.x + offset.x, nx = offset.x - tr.x; // add for the original / mirrored X
            SpriteMeshWriter.ScaleAddMirrorX(sprite, scale.x, ax, nx, scale.y, tr.y + offset.y, mb);
        }

        public static void MY(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var (scale, tr, offset) = GetSliceVertexTranslation(
                t, sprite, new Vector2(1, 0.5f), new Vector2(0, 0.5f));
            float ay = tr.y + offset.y, ny = offset.y - tr.y; // add for the original / mirrored Y
            SpriteMeshWriter.ScaleAddMirrorY(sprite, scale.x, tr.x + offset.x, scale.y, ay, ny, mb);
        }

        public static void MXY(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var (scale, tr, offset) = GetSliceVertexTranslation(
                t, sprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            float ax = tr.x + offset.x, nx = offset.x - tr.x; // add for the original / mirrored X
            float ay = tr.y + offset.y, ny = offset.y - tr.y; // add for the original / mirrored Y
            SpriteMeshWriter.ScaleAddMirrorXY(sprite, scale.x, ax, nx, scale.y, ay, ny, mb);
        }

        public static void R1C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  4  5  6  7
            //  0  1  2  3

            Assert.IsTrue(sprite.BorderSumX().Equals(sprite.rect.width),
                "Left + Right should be the width of the sprite.");

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b.x;
            var x2 = x3 - b.z;
            var y0 = r.yMin;
            var y1 = r.yMax;
            mb.Poses.SetUp_R1C3(
                x0, x1, x2, x3,
                y0, y1);

            // UV & Index
            mb.UVs.SetUp_R1C3(sprite);
            mb.Indices.SetUp(GridIndex.R1C3);
        }

        public static void R3C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            var (border, padding) = sprite.GetBorderAndPadding(borderMult);
            mb.Poses.SetUp_R3C3(t.rect, border, padding);
            mb.UVs.SetUp_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3);
        }

        public static void R3C3_NF(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            var (border, padding) = sprite.GetBorderAndPadding(borderMult);
            mb.Poses.SetUp_R3C3(t.rect, border, padding);
            mb.UVs.SetUp_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3_NF);
        }

        public static void MX_R1C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  4  5  6  7
            //  0  1  2  3

            Assert.AreEqual(0, sprite.border.x, "Left border should be 0.");
            Assert.AreEqual(sprite.rect.width, sprite.border.z, "Right border should be width of the sprite.");

            // Pos
            var r = t.rect;
            var b = sprite.rect.size.x * borderMult;
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b;
            var x2 = x3 - b;
            var y0 = r.yMin;
            var y1 = r.yMax;
            mb.Poses.SetUp_R1C3(
                x0, x1, x2, x3,
                y0, y1);

            // UV & Index
            mb.UVs.SetUp_MX_R1C3(sprite);
            mb.Indices.SetUp(GridIndex.R1C3);
        }

        public static void MX_R1C4(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            // 5 6 7 8 9
            // 0 1 2 3 4

            Assert.IsTrue(sprite.border is { x: 0, y: 0 } && sprite.border.z != 0 && sprite.border.w == 0);

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(1); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x4 = r.xMax;
            var x1 = x0 + b.z;
            var x2 = x0 + r.width / 2;
            var x3 = x4 - b.z;
            var y0 = r.yMin;
            var y1 = r.yMax;
            mb.Poses.SetUp_R1C4(
                x0, x1, x2, x3, x4,
                y0, y1);

            // UV & Index
            mb.UVs.SetUp_MX_R1C4(sprite);
            mb.Indices.SetUp(GridIndex.R1C4);
        }

        public static void MX_R3C2(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  9 10 11
            //  6  7  8
            //  3  4  5
            //  0  1  2

            Assert.IsTrue(sprite.border.x == 0, "The Left Border should be 0.");
            Assert.IsTrue(sprite.border.z.Equals(sprite.rect.width),
                "The Right Border should be the width of the sprite.");
            Assert.AreEqual(sprite.rect.height, sprite.border.y + sprite.border.w,
                "The sum of the Top and Bottom Border should be the height of the sprite.");

            // Pos
            var r = t.rect;
            var b = sprite.border;
            var x0 = r.xMin;
            var x2 = r.xMax;
            var x1 = x0 + r.width / 2;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.y;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C2(
                x0, x1, x2,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MX_R3C2(sprite);
            mb.Indices.SetUp(GridIndex.R3C2);
        }

        public static void MX_R3C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            // 12 13 14 15
            //  8  9 10 11
            //  4  5  6  7
            //  0  1  2  3

            Assert.IsTrue(sprite.border.x == 0 && sprite.border.z.Equals(sprite.rect.width),
                "Left should be 0 and Right should be width of the sprite.");

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b.z;
            var x2 = x3 - b.z;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.y;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C3(
                x0, x1, x2, x3,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MX_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3);
        }

        public static void MX_R2C3_NoTop(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // MX_R3C3 without the top border row: bottom border stays fixed, body stretches up.
            // The sprite's top border band (above the inner V2) is ignored, so a top border is allowed.
            // Visual presentation of the vertex order.
            //  8  9 10 11
            //  4  5  6  7
            //  0  1  2  3

            Assert.IsTrue(sprite.border.x == 0 && sprite.border.z.Equals(sprite.rect.width),
                "Left should be 0 and Right should be width of the sprite.");

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b.z;
            var x2 = x3 - b.z;
            var y0 = r.yMin;
            var y2 = r.yMax;
            var y1 = y0 + b.y;
            mb.Poses.SetUp_R2C3(
                x0, x1, x2, x3,
                y0, y1, y2);

            // UV & Index
            mb.UVs.SetUp_MX_R2C3_NoTop(sprite);
            mb.Indices.SetUp(GridIndex.R2C3);
        }

        public static void MX_R3C4(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            // 15 16 17 18 19
            // 10 11 12 13 14
            //  5  6  7  8  9
            //  0  1  2  3  4

            // Pos
            var r = t.rect;
            var (b, p) = sprite.GetBorderAndPadding(1); // TODO: 패딩 대응.
            Assert.IsTrue(b.x == 0 && b.z != 0);
            var x0 = r.xMin;
            var x4 = r.xMax;
            var x1 = x0 + b.z;
            var x2 = x0 + r.width / 2;
            var x3 = x4 - b.z;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.y;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C4(
                x0, x1, x2, x3, x4,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MX_R3C4(sprite);
            mb.Indices.SetUp(GridIndex.R3C4);
        }

        public static void MX_R3C6(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            // 21 22 23 24 25 26 27
            // 14 15 16 17 18 19 20
            //  7  8  9 10 11 12 13
            //  0  1  2  3  4  5  6

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(1); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMin + r.width / 2;
            var x6 = r.xMax;
            var x1 = x0 + b.z;
            var x2 = x3 - b.x;
            var x4 = x3 + b.x;
            var x5 = x6 - b.z;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.y;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C6(
                x0, x1, x2, x3, x4, x5, x6,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MX_R3C6(sprite);
            mb.Indices.SetUp(GridIndex.R3C6);
        }

        public static void MY_R2C2(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  6  7  8
            //  3  4  5
            //  0  1  2

            Assert.IsTrue(sprite.border.x == 0 && sprite.border.z.Equals(sprite.rect.width),
                "Left should be 0 and Right should be width of the sprite: " + sprite.name);
            Assert.IsTrue(sprite.border.y == 0 && sprite.border.w.Equals(sprite.rect.height),
                "Bottom should be 0 and Top should be height of the sprite: " + sprite.name);

            // Pos
            var r = t.rect;
            var b = sprite.border * borderMult;
            var x0 = r.xMin;
            var x2 = r.xMax;
            var x1 = x2 - b.z;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = (y0 + y3) / 2;
            mb.Poses.SetUp_R2C2(
                x0, x1, x2,
                y0, y1, y3);

            // UV & Index
            mb.UVs.SetUp_MY_R2C2(sprite);
            mb.Indices.SetUp(GridIndex.R2C2);
        }

        public static void MY_R2C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  8  9 10 11
            //  4  5  6  7
            //  0  1  2  3

            Assert.IsTrue(sprite.BorderSumX().Equals(sprite.rect.width),
                "Left + Right should be the width of the sprite: " + sprite.name);
            Assert.IsTrue(sprite.border.y == 0 && sprite.border.w.Equals(sprite.rect.height),
                "Bottom should be 0 and Top should be height of the sprite: " + sprite.name);

            // Pos
            var r = t.rect;
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b.x;
            var x2 = x3 - b.z;
            var y0 = r.yMin;
            var y1 = y0 + r.height / 2;
            var y2 = r.yMax;
            mb.Poses.SetUp_R2C3(
                x0, x1, x2, x3,
                y0, y1, y2);

            // UV & Index
            mb.UVs.SetUp_MY_R2C3(sprite);
            mb.Indices.SetUp(GridIndex.R2C3);
        }

        public static void MY_R3C1(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  6  7
            //  4  5
            //  2  3
            //  0  1

            Assert.IsTrue(sprite.border is { x: 0, z: 0 }, "Left and Right should be 0: " + sprite.name);
            Assert.IsTrue(sprite.border.y == 0, "Bottom should be 0: " + sprite.name);
            Assert.IsTrue(sprite.border.w.Equals(sprite.rect.height), "Top should be height of the sprite: " + sprite.name);

            // Pos
            var r = t.rect;
            var b = sprite.border * borderMult;
            var x0 = r.xMin;
            var x1 = r.xMax;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.w;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C1(
                x0, x1,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MY_R3C1(sprite);
            mb.Indices.SetUp(GridIndex.R3C1);
        }

        public static void MY_R3C2(Rect r, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            //  8  9 10
            //  7  8  9
            //  4  5  6
            //  1  2  3

            // col #1: expanded to the right.
            // col #2: only occupy the left adjusted border x.
            Assert.IsTrue(sprite.border.x == 0 && sprite.border.z.Equals(sprite.rect.width),
                "Left should be 0 and Right should be width of the sprite: " + sprite.name);
            Assert.IsTrue(sprite.border.y == 0 && sprite.border.w.Equals(sprite.rect.height),
                "Bottom should be 0 and Top should be height of the sprite: " + sprite.name);

            // Pos
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x2 = r.xMax;
            var x1 = x2 - b.z;

            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.w;
            var y2 = y3 - b.w;

            mb.Poses.SetUp_R3C2(
                x0, x1, x2,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MY_R3C2(sprite);
            mb.Indices.SetUp(GridIndex.R3C2);
        }

        public static void MY_R3C2(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            MY_R3C2(t.rect, sprite, borderMult, mb);
        }

        public static void MY_R3C3(Rect r, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // Visual presentation of the vertex order.
            // 12 13 14 15
            //  8  9 10 11
            //  4  5  6  7
            //  0  1  2  3

            Assert.IsTrue(sprite.border.y == 0 && sprite.border.w.Equals(sprite.rect.height),
                "Bottom should be 0 and Top should be height of the sprite.");

            // Pos
            var (b, _) = sprite.GetBorderAndPadding(borderMult); // TODO: 패딩 대응.
            var x0 = r.xMin;
            var x3 = r.xMax;
            var x1 = x0 + b.x;
            var x2 = x3 - b.z;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + b.w;
            var y2 = y3 - b.w;
            mb.Poses.SetUp_R3C3(
                x0, x1, x2, x3,
                y0, y1, y2, y3);

            // UV & Index
            mb.UVs.SetUp_MY_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3);
        }

        public static void MY_R3C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            MY_R3C3(t.rect, sprite, borderMult, mb);
        }

        public static void MXY_R3C2(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            // MXY_R3C3 collapsed to 2 columns: the inner X column is dropped, so the left and
            // right borders meet at the center seam (no horizontal stretch band). X mirrors about
            // the center; Y is the mirrored top/bottom border (V2 inner, V3 outer).
            // Visual presentation of the vertex order.
            //  9 10 11
            //  6  7  8
            //  3  4  5
            //  0  1  2

            var (border, _) = sprite.GetBorderAndPadding_MirrorXY(borderMult);
            var r = t.rect;
            var x0 = r.xMin;
            var x2 = r.xMax;
            var x1 = (x0 + x2) / 2;
            var y0 = r.yMin;
            var y3 = r.yMax;
            var y1 = y0 + border.w;
            var y2 = y3 - border.w;
            mb.Poses.SetUp_R3C2(
                x0, x1, x2,
                y0, y1, y2, y3);

            mb.UVs.SetUp_MXY_R3C2(sprite);
            mb.Indices.SetUp(GridIndex.R3C2);
        }

        public static void MXY_R3C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            var (border, padding) = sprite.GetBorderAndPadding_MirrorXY(borderMult);
            mb.Poses.SetUp_R3C3(t.rect, border, padding);
            mb.UVs.SetUp_MXY_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3);
        }

        public static void MXY_R3C3_NF(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            var (border, padding) = sprite.GetBorderAndPadding_MirrorXY(borderMult);
            mb.Poses.SetUp_R3C3(t.rect, border, padding);
            mb.UVs.SetUp_MXY_R3C3(sprite);
            mb.Indices.SetUp(GridIndex.R3C3_NF);
        }

        // Mirror-Y caps (top/bottom) tile in X; middle stretches. (vs MY_R3C1 stretched / C3 sliced caps)
        public static void CAP_MY(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb) => Tiling.CapMY(t, sprite, borderMult, mb);

        // Tiled border frame from one edge sprite; left/right are the top/bottom edge rotated 90°.
        public static void CAP_MXY(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb) => Tiling.CapMXY(t, sprite, borderMult, mb);

        private static (Vector2 Scale, Vector2 Translate, Vector2 Offset) GetSliceVertexTranslation(
            RectTransform t, Sprite sprite, Vector2 slice, Vector2 mirrorPivot)
        {
            var rectPivot = t.pivot;
            var drawingSize = t.rect.size;
            var sliceSize = drawingSize * slice;

            var spritePivot = sprite.CalcNormPivot();
            var spriteBoundSize = sprite.bounds.size;

            var scale = sliceSize / spriteBoundSize;
            var translation = spritePivot * sliceSize;
            var offset = (mirrorPivot - rectPivot) * drawingSize;
            return (scale, translation, offset);
        }

        // Rect-fit mapping for ID/Flip: scale sprite-local verts to the rect, offset by pivot delta.
        private static (Vector2 Scale, Vector2 Offset) GetPosMapping(RectTransform t, Sprite sprite, Vector2 spritePivot)
        {
            var drawingSize = t.rect.size;
            var scale = drawingSize / sprite.bounds.size;
            var offset = (spritePivot - t.pivot) * drawingSize;
            return (scale, offset);
        }

        private static (Vector4 Border, Vector4 Padding) GetBorderAndPadding(this Sprite sprite, float borderMultiplier)
        {
            var border = sprite.border * borderMultiplier;
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite) * borderMultiplier;
            return (border, padding);
        }

        private static (Vector4 Border, Vector4 Padding) GetBorderAndPadding_MirrorXY(this Sprite sprite, float borderMultiplier)
        {
            var (border, padding) = sprite.GetBorderAndPadding(borderMultiplier);
            Assert.IsTrue(border.x < 2 && border.z != 0, "Left border should be 0 and Right should not be 0: " + sprite.name);
            Assert.IsTrue(border.y < 2 && border.w != 0, "Bottom border should be 0 and Top should not be 0: " + sprite.name);
            Assert.IsTrue(padding.x == 0, "Left padding should be 0: " + sprite.name);
            Assert.IsTrue(padding.y == 0, "Bottom padding should be 0: " + sprite.name);

            border.x = border.z;
            border.y = border.w;
            padding.x = padding.z;
            padding.y = padding.w;

            return (border, padding);
        }
    }
}
