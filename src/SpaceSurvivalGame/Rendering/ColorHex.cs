using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace SpaceSurvivalGame.Rendering;

/// <summary>
/// Parses config-file color strings into a Color: "#RRGGBB" (opaque) or "#RRGGBBAA" (with
/// alpha), leading "#" optional either way. RGB is premultiplied by the alpha fraction, since
/// every SpriteBatch.Begin() in this codebase uses the default (premultiplied-alpha) blend
/// state — without this, a low/zero alpha wouldn't actually fade the color out, since the
/// full-brightness RGB would still blend in at full strength.
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

        var alphaFraction = a / 255f;
        return new Color((byte)(r * alphaFraction), (byte)(g * alphaFraction), (byte)(b * alphaFraction), a);
    }
}
