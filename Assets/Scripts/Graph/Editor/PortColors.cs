using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphEditor
{
    /// <summary>Picks a stable color for a value port based on its declared type.</summary>

    internal static class PortColors
    {
        private readonly static Dictionary<Type, Color> colors =
        new Dictionary<Type, Color> {
            { typeof(bool), new Color(0.85f, 0.35f, 0.85f) },
            { typeof(int), new Color(0.45f, 0.85f, 0.95f) },
            { typeof(float), new Color(0.55f, 0.95f, 0.55f) },
            { typeof(string), new Color(1f, 0.6f, 0.85f) },
            { typeof(Vector2), new Color(1f, 0.85f, 0.35f) },
            { typeof(Vector3), new Color(1f, 0.85f, 0.35f) },
            { typeof(Vector4), new Color(1f, 0.85f, 0.35f) }
        };

        public static Color ForType(Type type)
        {
            Color color = colors.ContainsKey(type) ? colors[type] : new Color(0.4f, 0.75f, 1f);
            return color;
        }
    }
}