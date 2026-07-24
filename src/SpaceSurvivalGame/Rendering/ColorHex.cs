using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Rendering;

/// <summary>Parses config-file color strings ("#RRGGBB" or "RRGGBB", leading "#" optional) into a Color.</summary>
public static class ColorHex
{
    public static Color Parse(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');
        var r = byte.Parse(span[..2], NumberStyles.HexNumber);
        var g = byte.Parse(span[2..4], NumberStyles.HexNumber);
        var b = byte.Parse(span[4..6], NumberStyles.HexNumber);
        return new Color(r, g, b);
    }
}
