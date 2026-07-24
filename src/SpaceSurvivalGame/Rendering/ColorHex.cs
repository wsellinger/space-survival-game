using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Parses config-file color strings into a Color: "#RRGGBB" (opaque) or "#RRGGBBAA" (with
/// alpha), leading "#" optional either way.
/// </summary>
public static class ColorHex
{
    public static Color Parse(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');
        var r = byte.Parse(span[..2], NumberStyles.HexNumber);
        var g = byte.Parse(span[2..4], NumberStyles.HexNumber);
        var b = byte.Parse(span[4..6], NumberStyles.HexNumber);
        var a = span.Length >= 8 ? byte.Parse(span[6..8], NumberStyles.HexNumber) : (byte)255;
        return new Color(r, g, b, a);
    }
}
