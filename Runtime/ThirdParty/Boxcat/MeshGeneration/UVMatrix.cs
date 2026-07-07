#nullable enable
using UnityEngine;
using UnityEngine.Sprites;

namespace UnityEngine.UI
{
    internal struct UVMatrix
    {
        public UVMatrix(Sprite sprite)
        {
            (U0, V0, U3, V3) = DataUtility.GetOuterUV(sprite);
            (U1, V1, U2, V2) = DataUtility.GetInnerUV(sprite);
        }

        public float U0;
        public float U1;
        public float U2;
        public float U3;
        public float V0;
        public float V1;
        public float V2;
        public float V3;

        public Vector2 _00 => new(U0, V0);
        public Vector2 _01 => new(U0, V1);
        public Vector2 _02 => new(U0, V2);
        public Vector2 _03 => new(U0, V3);

        public Vector2 _10 => new(U1, V0);
        public Vector2 _11 => new(U1, V1);
        public Vector2 _12 => new(U1, V2);
        public Vector2 _13 => new(U1, V3);

        public Vector2 _20 => new(U2, V0);
        public Vector2 _21 => new(U2, V1);
        public Vector2 _22 => new(U2, V2);
        public Vector2 _23 => new(U2, V3);

        public Vector2 _30 => new(U3, V0);
        public Vector2 _31 => new(U3, V1);
        public Vector2 _32 => new(U3, V2);
        public Vector2 _33 => new(U3, V3);
    }
}
