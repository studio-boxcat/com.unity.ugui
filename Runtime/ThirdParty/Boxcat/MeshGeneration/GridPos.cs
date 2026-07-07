#nullable enable
using UnityEngine;

namespace UnityEngine.UI
{
    // R for "Row", C for "Column"
    // e.g. R3C3 means 3 rows and 3 columns, 3x3 grid, 9 quads
    public static partial class GridPos
    {
        public static void SetUp_R1C3(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3,
            float y0, float y1)
        {
            var p = c.SetUp(8);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);

            p[4] = new Vector3(x0, y1, 0);
            p[5] = new Vector3(x1, y1, 0);
            p[6] = new Vector3(x2, y1, 0);
            p[7] = new Vector3(x3, y1, 0);
        }

        public static void SetUp_R1C4(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3, float x4,
            float y0, float y1)
        {
            var p = c.SetUp(10);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);
            p[4] = new Vector3(x4, y0, 0);

            p[5] = new Vector3(x0, y1, 0);
            p[6] = new Vector3(x1, y1, 0);
            p[7] = new Vector3(x2, y1, 0);
            p[8] = new Vector3(x3, y1, 0);
            p[9] = new Vector3(x4, y1, 0);
        }

        public static void SetUp_R2C2(
            this MeshPosChannel c,
            float x0, float x1, float x2,
            float y0, float y1, float y2)
        {
            var p = c.SetUp(3 * 3);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);

            p[3] = new Vector3(x0, y1, 0);
            p[4] = new Vector3(x1, y1, 0);
            p[5] = new Vector3(x2, y1, 0);

            p[6] = new Vector3(x0, y2, 0);
            p[7] = new Vector3(x1, y2, 0);
            p[8] = new Vector3(x2, y2, 0);
        }

        public static void SetUp_R2C3(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3,
            float y0, float y1, float y2)
        {
            var p = c.SetUp(4 * 3);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);

            p[4] = new Vector3(x0, y1, 0);
            p[5] = new Vector3(x1, y1, 0);
            p[6] = new Vector3(x2, y1, 0);
            p[7] = new Vector3(x3, y1, 0);

            p[8] = new Vector3(x0, y2, 0);
            p[9] = new Vector3(x1, y2, 0);
            p[10] = new Vector3(x2, y2, 0);
            p[11] = new Vector3(x3, y2, 0);
        }

        public static void SetUp_R3C1(
            this MeshPosChannel c,
            float x0, float x1,
            float y0, float y1, float y2, float y3)
        {
            var p = c.SetUp(4 * 2);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);

            p[2] = new Vector3(x0, y1, 0);
            p[3] = new Vector3(x1, y1, 0);

            p[4] = new Vector3(x0, y2, 0);
            p[5] = new Vector3(x1, y2, 0);

            p[6] = new Vector3(x0, y3, 0);
            p[7] = new Vector3(x1, y3, 0);
        }

        public static void SetUp_R3C2(
            this MeshPosChannel c,
            float x0, float x1, float x2,
            float y0, float y1, float y2, float y3)
        {
            var p = c.SetUp(12);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);

            p[3] = new Vector3(x0, y1, 0);
            p[4] = new Vector3(x1, y1, 0);
            p[5] = new Vector3(x2, y1, 0);

            p[6] = new Vector3(x0, y2, 0);
            p[7] = new Vector3(x1, y2, 0);
            p[8] = new Vector3(x2, y2, 0);

            p[9] = new Vector3(x0, y3, 0);
            p[10] = new Vector3(x1, y3, 0);
            p[11] = new Vector3(x2, y3, 0);
        }

        public static void SetUp_R3C3(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3,
            float y0, float y1, float y2, float y3)
        {
            var p = c.SetUp(16);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);

            p[4] = new Vector3(x0, y1, 0);
            p[5] = new Vector3(x1, y1, 0);
            p[6] = new Vector3(x2, y1, 0);
            p[7] = new Vector3(x3, y1, 0);

            p[8] = new Vector3(x0, y2, 0);
            p[9] = new Vector3(x1, y2, 0);
            p[10] = new Vector3(x2, y2, 0);
            p[11] = new Vector3(x3, y2, 0);

            p[12] = new Vector3(x0, y3, 0);
            p[13] = new Vector3(x1, y3, 0);
            p[14] = new Vector3(x2, y3, 0);
            p[15] = new Vector3(x3, y3, 0);
        }

        public static void SetUp_R3C3(this MeshPosChannel p, Rect rect, Vector4 border, Vector4 padding)
        {
            var x0 = rect.xMin + padding.x;
            var x1 = rect.xMin + border.x;
            var x2 = rect.xMax - border.z;
            var x3 = rect.xMax - padding.z;

            var y0 = rect.yMin + padding.y;
            var y1 = rect.yMin + border.y;
            var y2 = rect.yMax - border.w;
            var y3 = rect.yMax - padding.w;

            // Set up the vertices.
            p.SetUp_R3C3(
                x0, x1, x2, x3,
                y0, y1, y2, y3);
        }

        public static void SetUp_R3C4(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3, float x4,
            float y0, float y1, float y2, float y3)
        {
            var p = c.SetUp(20);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);
            p[4] = new Vector3(x4, y0, 0);

            p[5] = new Vector3(x0, y1, 0);
            p[6] = new Vector3(x1, y1, 0);
            p[7] = new Vector3(x2, y1, 0);
            p[8] = new Vector3(x3, y1, 0);
            p[9] = new Vector3(x4, y1, 0);

            p[10] = new Vector3(x0, y2, 0);
            p[11] = new Vector3(x1, y2, 0);
            p[12] = new Vector3(x2, y2, 0);
            p[13] = new Vector3(x3, y2, 0);
            p[14] = new Vector3(x4, y2, 0);

            p[15] = new Vector3(x0, y3, 0);
            p[16] = new Vector3(x1, y3, 0);
            p[17] = new Vector3(x2, y3, 0);
            p[18] = new Vector3(x3, y3, 0);
            p[19] = new Vector3(x4, y3, 0);
        }

        public static void SetUp_R3C6(
            this MeshPosChannel c,
            float x0, float x1, float x2, float x3, float x4, float x5, float x6,
            float y0, float y1, float y2, float y3)
        {
            var p = c.SetUp(28);

            p[0] = new Vector3(x0, y0, 0);
            p[1] = new Vector3(x1, y0, 0);
            p[2] = new Vector3(x2, y0, 0);
            p[3] = new Vector3(x3, y0, 0);
            p[4] = new Vector3(x4, y0, 0);
            p[5] = new Vector3(x5, y0, 0);
            p[6] = new Vector3(x6, y0, 0);

            p[7] = new Vector3(x0, y1, 0);
            p[8] = new Vector3(x1, y1, 0);
            p[9] = new Vector3(x2, y1, 0);
            p[10] = new Vector3(x3, y1, 0);
            p[11] = new Vector3(x4, y1, 0);
            p[12] = new Vector3(x5, y1, 0);
            p[13] = new Vector3(x6, y1, 0);

            p[14] = new Vector3(x0, y2, 0);
            p[15] = new Vector3(x1, y2, 0);
            p[16] = new Vector3(x2, y2, 0);
            p[17] = new Vector3(x3, y2, 0);
            p[18] = new Vector3(x4, y2, 0);
            p[19] = new Vector3(x5, y2, 0);
            p[20] = new Vector3(x6, y2, 0);

            p[21] = new Vector3(x0, y3, 0);
            p[22] = new Vector3(x1, y3, 0);
            p[23] = new Vector3(x2, y3, 0);
            p[24] = new Vector3(x3, y3, 0);
            p[25] = new Vector3(x4, y3, 0);
            p[26] = new Vector3(x5, y3, 0);
            p[27] = new Vector3(x6, y3, 0);
        }
    }
}
