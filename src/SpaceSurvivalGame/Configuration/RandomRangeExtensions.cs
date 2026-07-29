using System;

namespace SpaceSurvivalGame.Configuration;

/// <summary>Sampling helpers for FloatRange/IntRange, so "Min + random * (Max - Min)" isn't hand-rolled at every call site.</summary>
public static class RandomRangeExtensions
{
    public static float NextFloat(this Random random, FloatRange range) =>
        range.Min + (float)random.NextDouble() * (range.Max - range.Min);

    public static int NextInt(this Random random, IntRange range) =>
        random.Next(range.Min, range.Max + 1);
}
