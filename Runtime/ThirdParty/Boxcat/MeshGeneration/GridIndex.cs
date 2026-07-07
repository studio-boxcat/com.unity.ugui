#nullable enable
// ReSharper disable InconsistentNaming

#nullable enable

namespace UnityEngine.UI
{
    public static partial class GridIndex
    {
        private static ushort[]? _r1c4;
        private static ushort[]? _r1c3;
        private static ushort[]? _r2c2;
        private static ushort[]? _r2c3;
        private static ushort[]? _r3c1;
        private static ushort[]? _r3c2;
        private static ushort[]? _r3c3;
        private static ushort[]? _r3c3_NF;
        private static ushort[]? _r3c4;
        private static ushort[]? _r3c6;

        public static ushort[] R1C3 => _r1c3 ??= CreateIndices(1, 3);
        public static ushort[] R1C4 => _r1c4 ??= CreateIndices(1, 4);
        public static ushort[] R2C2 => _r2c2 ??= CreateIndices(2, 2);
        public static ushort[] R2C3 => _r2c3 ??= CreateIndices(2, 3);
        public static ushort[] R3C1 => _r3c1 ??= CreateIndices(3, 1);
        public static ushort[] R3C2 => _r3c2 ??= CreateIndices(3, 2);
        public static ushort[] R3C3 => _r3c3 ??= CreateIndices(3, 3);
        // NF = No Fill
        public static ushort[] R3C3_NF => _r3c3_NF ??= new ushort[]
        {
            0, 4, 5, 5, 1, 0, // BL
            1, 5, 6, 6, 2, 1, // BC
            2, 6, 7, 7, 3, 2, // BR
            4, 8, 9, 9, 5, 4, // ML
            6, 10, 11, 11, 7, 6, // MR
            8, 12, 13, 13, 9, 8, // TL
            9, 13, 14, 14, 10, 9, // TC
            10, 14, 15, 15, 11, 10 // TR
        };

        public static ushort[] R3C4 => _r3c4 ??= CreateIndices(3, 4);
        public static ushort[] R3C6 => _r3c6 ??= CreateIndices(3, 6);

        private static ushort[] CreateIndices(int rows, int cols)
        {
            var indices = new ushort[rows * cols * 6];
            var i = 0;
            var vr = 0; // Starting vertex of the row.

            for (var y = 0; y < rows; y++, vr += cols + 1)
            for (var x = 0; x < cols; x++)
            {
                var vl = vr + x; // Vertex lower.
                var vu = vl + cols + 1; // Vertex upper.

                // Upper-left triangle. (clockwise)
                indices[i++] = (ushort) vl;
                indices[i++] = (ushort) vu;
                indices[i++] = (ushort) (vu + 1);

                // Lower-right triangle. (clockwise)
                indices[i++] = (ushort) (vu + 1);
                indices[i++] = (ushort) (vl + 1);
                indices[i++] = (ushort) vl;
            }

            return indices;
        }
    }
}
