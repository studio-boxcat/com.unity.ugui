using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class SetPropertyUtility
    {
        public static bool SetValue(ref bool currentValue, bool newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetValue(ref char currentValue, char newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetValue(ref int currentValue, int newValue)
        {
            if (currentValue == newValue)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetValue(ref float currentValue, float newValue)
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

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : Object
        {
            if (ReferenceEquals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetVector2(ref Vector2 currentValue, Vector2 newValue)
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

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
    }
}