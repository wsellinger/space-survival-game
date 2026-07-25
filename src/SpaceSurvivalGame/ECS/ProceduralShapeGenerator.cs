using System;
using System.Numerics;

namespace SpaceSurvivalGame.ECS;

/// <summary>
/// Shared "jittered polygon in unit (-1..1) space" generator used by every irregular-shape
/// silhouette in the game (asteroid rocks, O2 pickup crystals, and oxygen-rich asteroids'
/// embedded crystal speckles): vertices at evenly-spaced angles around the center, each with
/// its own small angle and radius jitter. Jitter is kept below half the angle step so vertices
/// stay in angular order — guarantees a simple polygon (possibly concave) rather than one with
/// self-intersecting edges.
/// </summary>
public static class ProceduralShapeGenerator
{
    /// <param name="vertexCount">Number of vertices.</param>
    /// <param name="minVertexRadiusFactor">Lower bound of each vertex's radius, in [0,1]; 1 = perfect circle, lower = jaggeder.</param>
    /// <param name="angleJitterFraction">Per-vertex angle jitter, as a fraction of the even angle step between vertices. Keep below 0.5 to preserve angular order.</param>
    /// <param name="elongationFactor">Stretches the Y axis by this factor before any rescale — 1 = no stretch (round rock), &gt;1 = gem-like silhouette (crystals).</param>
    /// <param name="rescaleToUnitBounds">If true, rescales every vertex by the largest single X/Y component across the shape so the shape's bounding box exactly touches +-1 (needed after elongation so a stretched shape still fits its canvas; rock shapes leave this false to keep their existing [minVertexRadiusFactor,1] radius distribution untouched).</param>
    public static Vector2[] GenerateJitteredPolygon(Random random, int vertexCount, float minVertexRadiusFactor,
        float angleJitterFraction, float elongationFactor = 1f, bool rescaleToUnitBounds = false)
    {
        var vertices = new Vector2[vertexCount];
        var angleStep = MathF.PI * 2f / vertexCount;
        var maxExtent = 0f;

        for (var i = 0; i < vertexCount; i++)
        {
            var angleJitter = ((float)random.NextDouble() * 2f - 1f) * (angleStep * angleJitterFraction);
            var angle = i * angleStep + angleJitter;
            var radius = minVertexRadiusFactor + (float)random.NextDouble() * (1f - minVertexRadiusFactor);
            var vertex = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * elongationFactor);
            vertices[i] = vertex;
            maxExtent = MathF.Max(maxExtent, MathF.Max(MathF.Abs(vertex.X), MathF.Abs(vertex.Y)));
        }

        if (rescaleToUnitBounds)
            for (var i = 0; i < vertices.Length; i++) vertices[i] /= maxExtent;

        return vertices;
    }
}
