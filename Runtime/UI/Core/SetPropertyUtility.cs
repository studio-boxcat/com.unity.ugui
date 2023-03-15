using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    internal static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetEnum<T>(ref T currentValue, T newValue) where T : Enum
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct(ref bool currentValue, bool newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct(ref char currentValue, char newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct(ref int currentValue, int newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct(ref float currentValue, float newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct, IEquatable<T>
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : UnityEngine.Object
        {
            if (ReferenceEquals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}