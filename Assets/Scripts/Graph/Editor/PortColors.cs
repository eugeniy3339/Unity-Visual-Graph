using System;
using UnityEngine;

namespace GraphEditor
{
    /// <summary>Picks a stable color for a value port based on its declared type.</summary>
    internal static class PortColors
    {
        public static Color ForType(Type type)
        {
            if (type == null) return Color.gray;
            if (type == typeof(bool)) return new Color(0.85f, 0.35f, 0.85f);
            if (type == typeof(int) || type == typeof(long)) return new Color(0.45f, 0.85f, 0.95f);
            if (type == typeof(float) || type == typeof(double)) return new Color(0.55f, 0.95f, 0.55f);
            if (type == typeof(string)) return new Color(1f, 0.6f, 0.85f);
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4))
                return new Color(1f, 0.85f, 0.35f);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return new Color(0.4f, 0.75f, 1f);

            // Deterministic hash-based color for custom classes / interfaces / enums.
            int hash = (type.FullName ?? type.Name).GetHashCode();
            var rng = new System.Random(hash);
            return Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 0.95f);
        }
    }
}
