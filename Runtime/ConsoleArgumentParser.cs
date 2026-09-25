using System;
using System.Globalization;
using UnityEngine;

namespace AlpTheDev.DevConsole
{
    public static class ConsoleArgumentParser
    {
        private static readonly char[] VectorSeparators = { ' ', ',', '\t' };

        public static bool TryParse(Type type, string argument, out object parsed)
        {
            parsed = null;

            if (type == typeof(string))
            {
                parsed = argument;
                return true;
            }

            if (type == typeof(int))
            {
                if (!int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) return false;

                parsed = value;
                return true;
            }

            if (type == typeof(float))
            {
                if (!TryParseFloat(argument, out float value)) return false;

                parsed = value;
                return true;
            }

            if (type == typeof(bool))
            {
                if (!bool.TryParse(argument, out bool value)) return false;

                parsed = value;
                return true;
            }

            if (type == typeof(Vector3))
            {
                if (!TryParseVector3(argument, out Vector3 value)) return false;

                parsed = value;
                return true;
            }

            if (type.IsEnum) return TryParseEnum(type, argument, out parsed);

            return false;
        }

        public static string Describe(Type type)
        {
            if (type == typeof(string)) return "text";
            if (type == typeof(int)) return "an int";
            if (type == typeof(float)) return "a float";
            if (type == typeof(bool)) return "true or false";
            if (type == typeof(Vector3)) return "three floats";
            if (type.IsEnum) return $"one of {string.Join("|", Enum.GetNames(type)).ToLowerInvariant()}";

            return type.Name;
        }

        private static bool TryParseFloat(string argument, out float value)
        {
            return float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseVector3(string argument, out Vector3 value)
        {
            value = Vector3.zero;

            string[] parts = argument.Split(VectorSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            if (!TryParseFloat(parts[0], out float x)) return false;
            if (!TryParseFloat(parts[1], out float y)) return false;
            if (!TryParseFloat(parts[2], out float z)) return false;

            value = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseEnum(Type type, string argument, out object parsed)
        {
            foreach (string name in Enum.GetNames(type))
            {
                if (!string.Equals(name, argument, StringComparison.OrdinalIgnoreCase)) continue;

                parsed = Enum.Parse(type, name);
                return true;
            }

            parsed = null;
            return false;
        }
    }
}
