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

        // TX_MX_C3: mirrored left/right edges (border.z wide, border.x == 0) with a mirror ping-pong
        // tiled centre.
        public static void TileX_MX_C3(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            Assert.AreEqual(0, sprite.border.x, "Left border should be 0 for TX_MX_C3.");
            Assert.IsTrue(sprite.border.z > 0, "Right border should be > 0 for TX_MX_C3.");

            var rect = t.rect;
            if (mb.TrySetUpEmpty(rect.width <= 0f)) return;

            // inner U max = the center/edge UV seam
            sprite.GetUV44(out var uMin, out _, out var innerU, out var uMax, out var vMin, out _, out _, out var vMax);

            var edgeW = sprite.border.z * borderMult;
            var x0 = rect.xMin;
            var x3 = rect.xMax;
            var y0 = rect.yMin;
            var y1 = rect.yMax;
            var xL = x0 + edgeW; // left edge inner boundary
            var xR = x3 - edgeW; // right edge inner boundary
            var centerW = xR - xL;

            // Both centre ends anchor at the edge seam (innerU), so the mirror ping-pong needs an even
            // half-tile count — round to it and scale the body uniformly (a proportional partial tile
            // can't land back on the seam).
            var tileW = (sprite.rect.width - sprite.border.z) * borderMult; // body width scaled like the edges
            var n = centerW <= _eps || tileW <= _eps ? 0 : Mathf.Max(2, Mathf.RoundToInt(centerW / tileW * 0.5f) * 2);

            var quadCount = 2 + n;
            var b = new StripBuilder(quadCount * 4, mb);

            b.Quad(x0, xL, y0, y1, uMax, innerU, vMin, vMax); // left edge (mirrored)

            for (var i = 0; i < n; i++)
            {
                var (uA, uB) = i.IsEven() ? (innerU, uMin) : (uMin, innerU); // ping-pong: seam→body, then back
                b.Quad(xL + centerW * i / n, xL + centerW * (i + 1) / n, y0, y1, uA, uB, vMin, vMax);
            }

            b.Quad(xR, x3, y0, y1, innerU, uMax, vMin, vMax); // right edge

            mb.Indices.SetUp_Quad(quadCount);
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

        // CAP_TY: top/bottom caps from the sprite's top (border.w) / bottom (border.y) borders; the body
        // between them tiles in Y. Width stretches. (vs MY_R3C1: real caps instead of mirrored, tiled
        // middle instead of stretched.)
        public static void CapTY(RectTransform t, Sprite sprite, float borderMult, MeshBuilder mb)
        {
            Assert.IsTrue(sprite.border is { x: 0, z: 0 }, "Left/right borders must be 0 for CAP_TY: " + sprite.name);
            Assert.IsTrue(sprite.border is { y: > 0, w: > 0 }, "Bottom (y) and top (w) borders must define the cap heights for CAP_TY: " + sprite.name);

            var rect = t.rect;
            if (mb.TrySetUpEmpty(rect.height <= 0f)) return;

            // inner V = the cap/body UV seams
            sprite.GetUV44(out var uMin, out _, out _, out var uMax, out var vMin, out var vBotSeam, out var vTopSeam, out var vMax);

            var x0 = rect.xMin;
            var x1 = rect.xMax;
            var y0 = rect.yMin;
            var y3 = rect.yMax;
            var y1 = y0 + sprite.border.y * borderMult; // bottom cap inner boundary
            var y2 = y3 - sprite.border.w * borderMult; // top cap inner boundary
            var bodyH = y2 - y1;

            // Both body ends anchor at a cap seam, so round to a whole tile count and scale the body
            // uniformly to fit (a proportional partial tile would leave a cut against one cap).
            var tileH = (sprite.rect.height - sprite.border.y - sprite.border.w) * borderMult; // body height scaled like the caps
            var n = bodyH <= _eps || tileH <= _eps ? 0 : Mathf.Max(1, Mathf.RoundToInt(bodyH / tileH));

            var quadCount = 2 + n;
            var b = new StripBuilder(quadCount * 4, mb);

            b.Quad(x0, x1, y2, y3, uMin, uMax, vTopSeam, vMax); // top cap

            for (var i = 0; i < n; i++)
                b.Quad(x0, x1, y2 - bodyH * (i + 1) / n, y2 - bodyH * i / n, uMin, uMax, vBotSeam, vTopSeam);

            b.Quad(x0, x1, y0, y1, uMin, uMax, vMin, vBotSeam); // bottom cap

            mb.Indices.SetUp_Quad(quadCount);
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
