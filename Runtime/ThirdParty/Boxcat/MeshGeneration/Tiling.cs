#nullable enable
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    internal static class Tiling
    {
        public static void TileX(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var tileSize = RequireTileSize(sprite.rect.width, sprite);
            var rect = t.rect;
            sprite.GetOuterUV(out var uMin, out var vMin, out var uMax, out var vMax);
            TileStrip(mb, mainAxis: 0, rect.xMin, rect.width, tileSize, uMin, uMax,
                cross0Pos: rect.yMin, cross1Pos: rect.yMax, cross0UV: vMin, cross1UV: vMax);
        }

        public static void TileY(RectTransform t, Sprite sprite, MeshBuilder mb)
        {
            var tileSize = RequireTileSize(sprite.rect.height, sprite);
            var rect = t.rect;
            sprite.GetOuterUV(out var uMin, out var vMin, out var uMax, out var vMax);
            // cross positions reversed to keep CW winding
            TileStrip(mb, mainAxis: 1, rect.yMin, rect.height, tileSize, vMin, vMax,
                cross0Pos: rect.xMax, cross1Pos: rect.xMin, cross0UV: uMin, cross1UV: uMax);
        }

        // TileX/TileY core: center-out ping-pong along `mainAxis` (0=X, 1=Y); the cross axis is one fixed
        // vert pair.
        private static void TileStrip(
            MeshBuilder mb, int mainAxis,
            float rectMin, float rectSize, float tileSize,
            float uvMainMin, float uvMainMax,
            float cross0Pos, float cross1Pos, float cross0UV, float cross1UV)
        {
            if (mb.TrySetUpEmpty(rectSize <= 0f)) return;

            var cols = new TileColumns(tileSize, rectMin, rectSize, uvMainMin, uvMainMax);
            var b = new StripBuilder(cols.Count * 2, mb);
            cols.Emit(ref b, mainAxis, cross0Pos, cross1Pos, cross0UV, cross1UV);
            cols.BuildIndices(bands: 1, mb);
        }

        // TX_MX_C3: mirrored left/right edges (border.z wide, border.x == 0) with a tiled ping-pong centre.
        // Columns left→right, each 2 verts (bottom/top): [leftOuter leftInner tiles... rightInner rightOuter].
        public static void TileX_MX_C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            Assert.AreEqual(0, sprite.border.x, "Left border should be 0 for TX_MX_C3.");
            Assert.IsTrue(sprite.border.z > 0, "Right border should be > 0 for TX_MX_C3.");

            var rect = t.rect;
            var edgeW = sprite.border.z * borderMult;
            var rectW = rect.width;

            if (mb.TrySetUpEmpty(rectW <= 0f)) return;

            sprite.GetOuterUV(out var uMin, out var vMin, out var uMax, out var vMax);
            var spriteW = sprite.rect.width;
            var innerU = Mathf.Lerp(uMin, uMax, (spriteW - sprite.border.z) / spriteW); // center/edge UV seam

            var xMin = rect.xMin;
            var xMax = rect.xMax;
            var yMin = rect.yMin;
            var yMax = rect.yMax;
            var xL = xMin + edgeW; // left edge inner boundary
            var xR = xMax - edgeW; // right edge inner boundary
            var centerW = Mathf.Max(0, xR - xL);

            var tileSizeRect = (spriteW - sprite.border.z) * (edgeW / sprite.border.z);
            var tiles = centerW <= _eps || tileSizeRect <= _eps ? 0f : centerW / tileSizeRect;
            var (segments, tailFract, hasTail) = SplitTiles(tiles);
            var tileCols = Mathf.Max(0, segments - 1); // internal tile boundaries (excluding xL/xR)

            var totalCols = 4 + tileCols; // leftOuter, leftInner, [tileCols], rightInner, rightOuter
            var b = new StripBuilder(totalCols * 2, mb);

            WriteCol(ref b, xMin, yMin, yMax, uMax, vMin, vMax); // left edge outer (mirrored → uMax)
            WriteCol(ref b, xL, yMin, yMax, innerU, vMin, vMax); // left edge inner

            var x = xL;
            for (var i = 0; i < tileCols; ++i)
            {
                var isLast = (i == tileCols - 1) && hasTail;
                x += isLast ? tailFract * tileSizeRect : tileSizeRect;

                // ping-pong: even columns land on uMin, odd on innerU; the partial tail lerps the seam
                // span by the tail fraction — the same expression for either flow direction.
                var u = isLast ? Mathf.Lerp(uMin, innerU, tailFract)
                    : (i + 1).IsEven() ? uMin : innerU;

                WriteCol(ref b, x, yMin, yMax, u, vMin, vMax);
            }

            WriteCol(ref b, xR, yMin, yMax, innerU, vMin, vMax); // right edge inner
            WriteCol(ref b, xMax, yMin, yMax, uMax, vMin, vMax); // right edge outer

            // Left→right strip: adjacent columns share verts (quad i spans columns i, i+1), so step 2 verts.
            var quadCount = totalCols - 1;
            var ib = new IndexBuilder(quadCount, mb);
            for (var i = 0; i < quadCount; ++i)
            {
                var a = i * 2;
                ib.Quad(a, a + 1, a + 2, a + 3);
            }
        }

        // One shared-strip column: bottom + top verts at `x`.
        private static void WriteCol(ref StripBuilder b, float x, float yMin, float yMax, float u, float vMin, float vMax)
        {
            b.Vert(x, yMin, u, vMin);
            b.Vert(x, yMax, u, vMax);
        }

        // CAP_MY: mirror-Y caps (top/bottom, height border.w) tiled in X; middle stretches the (uMin,vMin)
        // texel. Only border.w set. Per column = 4 verts (rows y0..y3), V = vMax/vMin/vMin/vMax.
        public static void CapMY(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            Assert.IsTrue(sprite.border is { x: 0, y: 0, z: 0 }, "Only the top border (w) may be set for CAP_MY: " + sprite.name);
            Assert.IsTrue(sprite.border.w > 0, "Top border (w) must define the cap height for CAP_MY: " + sprite.name);

            var tileSize = RequireTileSize(sprite.rect.width, sprite);

            var rect = t.rect;
            var rectW = rect.width;
            if (mb.TrySetUpEmpty(rectW <= 0f)) return;

            sprite.GetOuterUV(out var uMin, out var vMin, out var uMax, out var vMax);
            var cols = new TileColumns(tileSize, rect.xMin, rectW, uMin, uMax);

            var edgeH = sprite.border.w * borderMult;
            var y0 = rect.yMin;
            var y3 = rect.yMax;
            var y1 = y0 + edgeH;
            var y2 = y3 - edgeH;

            var midVert = cols.Count * 4; // 4 middle-quad verts appended after the columns
            var b = new StripBuilder(midVert + 4, mb);
            cols.Emit(ref b, mainAxis: 0, y0, y1, y2, y3, vMax, vMin, vMin, vMax);

            // middle: stretched (uMin, vMin) fill across the full width
            WriteCol(ref b, rect.xMin, y1, y2, uMin, vMin, vMin);
            WriteCol(ref b, rect.xMax, y1, y2, uMin, vMin, vMin);

            cols.BuildIndices(bands: 2, mb, midVert);
        }

        // CAP_MXY: tiled border frame from one edge sprite (thickness border.w). Top/bottom tile in X;
        // left/right tile in Y as the edge rotated 90°. Centre is a stretched solid quad. Each edge
        // extrudes half a corner so adjacent edges overlap at the inner corner.
        public static void CapMXY(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
            => CapFrame(t, sprite, corner: null, Corner.None, borderMult, cornerFactor: 0f, mb);

        // TileCornerRect: a CAP_MXY frame where each corner in `corners` is filled by a first-quadrant
        // quarter-circle `corner` sprite (mirrored across the corners; cf. UIRoundedRect); corners not in
        // the set stay plain CAP_MXY (edges overlap). Edge thickness stays border.w; the sprite-corner size
        // is corner.rect.width × `cornerFactor` (factor 1 = native size). Corner shares the edge texture.
        public static void TileCornerRect(RectTransform t, Sprite edge, Sprite corner, Corner corners, float cornerFactor, MeshBuilder mb)
            => CapFrame(t, edge, corner, corners, borderMult: 1f, cornerFactor, mb);

        // Shared core. Each corner is either a sprite corner (in `corners`, inset by c = corner.rect.width ×
        // cornerFactor and filled by the quarter-circle) or plain CAP_MXY (inset by half the thickness,
        // edges overlap). Every edge tiles between the insets of its two end corners.
        private static void CapFrame(RectTransform t, Sprite sprite, Sprite? corner, Corner corners, float borderMult, float cornerFactor, MeshBuilder mb)
        {
            Assert.IsTrue(sprite.border is { x: 0, y: 0, z: 0 }, "Only the top border (w) may be set for CAP_MXY: " + sprite.name);
            Assert.IsTrue(sprite.border.w > 0, "Top border (w) must define the frame thickness for CAP_MXY: " + sprite.name);

            var tile = RequireTileSize(sprite.rect.width, sprite);

            var rect = t.rect;
            if (mb.TrySetUpEmpty(rect.width <= 0f || rect.height <= 0f)) return;
            sprite.GetOuterUV(out var uMin, out var vMin, out var uMax, out var vMax);

            // Degenerate-rect clamp (cf. UIRoundedRect.MaxRadius): insets apply at both ends of each axis,
            // so capping b and c at half the smaller dimension keeps bands non-inverted and corners disjoint.
            var maxInset = Mathf.Min(rect.width, rect.height) * 0.5f;
            var thick = Mathf.Min(sprite.border.w * borderMult, maxInset); // edge thickness (all 4 sides)
            var half = thick * 0.5f;                                       // plain (CAP_MXY) corner overlap inset
            bool hasCorner = corner; // implicit bool: also rejects destroyed sprites (fake-null)
            var c = hasCorner ? Mathf.Min(corner!.rect.width * cornerFactor, maxInset) : 0f; // sprite corner inset (native size × factor)
            var x0 = rect.xMin; var x3 = rect.xMax; var x1 = x0 + thick; var x2 = x3 - thick;
            var y0 = rect.yMin; var y3 = rect.yMax; var y1 = y0 + thick; var y2 = y3 - thick;

            // Which corners get the sprite (rest fall back to CAP_MXY overlap); inset from the outer rect
            // edge is c for a sprite corner, half otherwise.
            var drawBL = hasCorner && corners.Has(Corner.BL);
            var drawBR = hasCorner && corners.Has(Corner.BR);
            var drawTL = hasCorner && corners.Has(Corner.TL);
            var drawTR = hasCorner && corners.Has(Corner.TR);
            var iBL = drawBL ? c : half;
            var iBR = drawBR ? c : half;
            var iTL = drawTL ? c : half;
            var iTR = drawTR ? c : half;

            var nTop = TileCount((x3 - iTR) - (x0 + iTL), tile);
            var nBot = TileCount((x3 - iBR) - (x0 + iBL), tile);
            var nLeft = TileCount((y3 - iTL) - (y0 + iBL), tile);
            var nRight = TileCount((y3 - iTR) - (y0 + iBR), tile);

            var cornerCount = (drawBL ? 1 : 0) + (drawBR ? 1 : 0) + (drawTL ? 1 : 0) + (drawTR ? 1 : 0);
            // How far a corner square reaches past the edge band into the interior. Where it's positive the
            // solid centre must step back by `d` at sprite corners so it never fills their rounded-away area.
            // No sprite corners drawn -> no step-back needed; keeps the centre a single quad.
            var d = cornerCount > 0 ? Mathf.Max(0f, c - thick) : 0f;
            var centreQuads = d > _eps ? 3 : 1;
            var quadCount = nTop + nBot + nLeft + nRight + centreQuads + cornerCount;
            var b = new StripBuilder(quadCount * 4, mb);

            // V spans thickness: vMin = inner, vMax = outer. axis 0 = tile in X; axis 1 = tile in Y (90°).
            EmitCapStrip(ref b, axis: 0, x0 + iTL, x3 - iTR, tile, nTop,   y2, y3, uMin, uMax, vMin, vMax); // top
            EmitCapStrip(ref b, axis: 0, x0 + iBL, x3 - iBR, tile, nBot,   y0, y1, uMin, uMax, vMax, vMin); // bottom
            EmitCapStrip(ref b, axis: 1, y0 + iBL, y3 - iTL, tile, nLeft,  x0, x1, uMin, uMax, vMax, vMin); // left
            EmitCapStrip(ref b, axis: 1, y0 + iBR, y3 - iTR, tile, nRight, x2, x3, uMin, uMax, vMin, vMax); // right

            // Centre solid (uMin,vMin). Full-width middle band + top/bottom bands stepped in by `d` at the
            // sprite corners, so the fill abuts each quarter-circle instead of overlapping it.
            b.Quad(x1, x2, y1 + d, y2 - d, uMin, uMin, vMin, vMin); // middle band
            if (d > _eps)
            {
                b.Quad(x1 + (drawBL ? d : 0f), x2 - (drawBR ? d : 0f), y1, y1 + d, uMin, uMin, vMin, vMin); // bottom band
                b.Quad(x1 + (drawTL ? d : 0f), x2 - (drawTR ? d : 0f), y2 - d, y2, uMin, uMin, vMin, vMin); // top band
            }

            // Sprite corners: the squares the strips leave open, each a quarter-circle (opaque at outer-UV
            // min, arc tip at outer-UV max) mirrored so the arc faces the outer rect corner.
            if (cornerCount > 0)
            {
                corner!.GetOuterUV(out var cuMin, out var cvMin, out var cuMax, out var cvMax);
                if (drawBL) b.Quad(x0, x0 + c, y0, y0 + c, cuMax, cuMin, cvMax, cvMin); // BL
                if (drawBR) b.Quad(x3 - c, x3, y0, y0 + c, cuMin, cuMax, cvMax, cvMin); // BR (mirror X)
                if (drawTL) b.Quad(x0, x0 + c, y3 - c, y3, cuMax, cuMin, cvMin, cvMax); // TL (mirror Y)
                if (drawTR) b.Quad(x3 - c, x3, y3 - c, y3, cuMin, cuMax, cvMin, cvMax); // TR (mirror XY)
            }

            mb.Indices.SetUp_Quad(quadCount);
        }

        // One cap strip of `n` tiles (4 verts each), tiling along `axis` (0=X, 1=Y rotated 90°), ping-ponging
        // U over [uMin..uMax]; cross axis spans the thickness [crossMin..crossMax] with V = vAtMin→vAtMax.
        private static void EmitCapStrip(ref StripBuilder b,
            int axis, float tileMin, float tileMax, float tile, int n,
            float crossMin, float crossMax, float uMin, float uMax, float vAtMin, float vAtMax)
        {
            var cross = 1 - axis;
            var s = tileMin;
            for (var i = 0; i < n; i++)
            {
                var last = i == n - 1;
                var sEnd = last ? tileMax : s + tile;
                var flow = (i & 1) == 0;
                var u0 = flow ? uMin : uMax;
                var u1 = flow ? uMax : uMin;
                if (last) u1 = Mathf.Lerp(u0, u1, (tileMax - s) / tile); // partial tail

                // layout 0=BL, 1=BR, 2=TL, 3=TR
                b.VertAxis(axis, cross, s,    crossMin, u0, vAtMin);
                b.VertAxis(axis, cross, sEnd, crossMin, u1, vAtMin);
                b.VertAxis(axis, cross, s,    crossMax, u0, vAtMax);
                b.VertAxis(axis, cross, sEnd, crossMax, u1, vAtMax);
                s = sEnd;
            }
        }

        private const float _eps = 0.0001f;

        private static float RequireTileSize(float size, Sprite sprite)
        {
            Assert.IsTrue(size > 0, $"Sprite '{sprite.name}' has a zero tiling dimension.");
            return size;
        }

        // Splits a tile count into covering segments (whole tiles + the partial tail when > Eps).
        private static (int segments, float tailFract, bool hasTail) SplitTiles(float tiles)
        {
            var full = Mathf.FloorToInt(tiles);
            var tailFract = tiles - full;
            var hasTail = tailFract > _eps;
            return (full + (hasTail ? 1 : 0), tailFract, hasTail);
        }

        // Tiles of width `tile` covering `span`, including a partial trailing tile.
        private static int TileCount(float span, float tile)
        {
            if (span <= _eps || tile <= _eps) return 0;
            var (segments, _, _) = SplitTiles(span / tile);
            return segments;
        }

        // Center-out ping-pong column layout: index 0 = centre, 1..colsHalf forward, colsHalf+1..2×colsHalf
        // mirrored. Owns the scratch buffers, so layout, vert emit, and index build stay in sync.
        private readonly struct TileColumns
        {
            // Reusable scratch (builds run sequentially per mesh, so sharing is safe).
            private static float[] _colPos = new float[16];
            private static float[] _colUV = new float[16];

            public readonly int Count; // total columns
            private readonly int _colsHalf;
            private readonly float[] _pos;
            private readonly float[] _uv;

            public TileColumns(float tileSize, float rectMin, float rectSize, float uvMin, float uvMax)
            {
                var (segments, tailFract, hasTail) = SplitTiles(rectSize / tileSize * 0.5f); // tiles on one side
                var colsHalf = _colsHalf = segments; // forward (= backward) column count, no centre
                Count = colsHalf * 2 + 1;
                if (_colPos.Length < Count) { Array.Resize(ref _colPos, Count); Array.Resize(ref _colUV, Count); }
                var pos = _pos = _colPos;
                var uv = _uv = _colUV;

                var mid = rectMin + rectSize * 0.5f;
                pos[0] = mid;
                uv[0] = uvMin;

                var p = mid;
                for (var col = 1; col <= colsHalf; ++col)
                {
                    var flow = col.IsEven();
                    var isPartial = (col == colsHalf) && hasTail;
                    if (isPartial)
                    {
                        p += tailFract * tileSize;
                        // partial tail: lerp the tile's UV span by the tail fraction (direction-aware)
                        uv[col] = flow ? Mathf.Lerp(uvMax, uvMin, tailFract) : Mathf.Lerp(uvMin, uvMax, tailFract);
                    }
                    else
                    {
                        p += tileSize;
                        uv[col] = flow ? uvMin : uvMax; // ping-pong
                    }
                    pos[col] = p;
                }

                var posOffset = mid * 2; // mirror: cx*2 - p
                for (var i = 1; i <= colsHalf; ++i)
                {
                    pos[colsHalf + i] = posOffset - pos[i];
                    uv[colsHalf + i] = uv[i];
                }
            }

            // Per column, one vert per cross-axis row (pos/uv args pair 1:1).
            public void Emit(ref StripBuilder b, int mainAxis, float pos0, float pos1, float uv0, float uv1)
            {
                var cross = 1 - mainAxis;
                for (var c = 0; c < Count; c++)
                {
                    var p = _pos[c];
                    var u = _uv[c];
                    b.VertAxisUV(mainAxis, cross, p, pos0, u, uv0);
                    b.VertAxisUV(mainAxis, cross, p, pos1, u, uv1);
                }
            }

            public void Emit(ref StripBuilder b, int mainAxis,
                float pos0, float pos1, float pos2, float pos3, float uv0, float uv1, float uv2, float uv3)
            {
                var cross = 1 - mainAxis;
                for (var c = 0; c < Count; c++)
                {
                    var p = _pos[c];
                    var u = _uv[c];
                    b.VertAxisUV(mainAxis, cross, p, pos0, u, uv0);
                    b.VertAxisUV(mainAxis, cross, p, pos1, u, uv1);
                    b.VertAxisUV(mainAxis, cross, p, pos2, u, uv2);
                    b.VertAxisUV(mainAxis, cross, p, pos3, u, uv3);
                }
            }

            // Adjacency: forward columns, first backward column stitched to the centre, then backward
            // columns; one quad per band per pair. `midVert` appends a trailing quad on 4 extra verts.
            public void BuildIndices(int bands, MeshBuilder mb, int midVert = MeshBuilder.Invalid)
            {
                var colsHalf = _colsHalf;
                var hasMid = midVert != MeshBuilder.Invalid;
                var quadCount = colsHalf * 2 * bands + (hasMid ? 1 : 0);
                var ib = new IndexBuilder(quadCount, mb);

                if (colsHalf > 0) // a lone centre column (sub-tile sliver rect) has no adjacent pairs
                {
                    for (var i = 0; i < colsHalf; ++i) // forward
                        AddPair(ref ib, leftCol: i, rightCol: i + 1, bands);

                    var b1 = colsHalf + 1;
                    AddPair(ref ib, leftCol: b1, rightCol: 0, bands); // first backward → centre

                    for (var i = 1; i < colsHalf; ++i) // backward
                        AddPair(ref ib, leftCol: b1 + i, rightCol: b1 + i - 1, bands);
                }

                if (hasMid)
                    ib.Quad(midVert, midVert + 1, midVert + 2, midVert + 3);

                return;

                static void AddPair(ref IndexBuilder ib, int leftCol, int rightCol, int bands)
                {
                    var l = leftCol * bands * 2;
                    var r = rightCol * bands * 2;
                    for (var band = 0; band < bands * 2; band += 2)
                        ib.Quad(l + band, l + band + 1, r + band, r + band + 1);
                }
            }
        }
    }
}
