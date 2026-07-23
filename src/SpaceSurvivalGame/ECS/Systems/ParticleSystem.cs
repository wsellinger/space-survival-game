using System.Collections.Generic;
using Arch.Core;
using SpaceSurvivalGame.ECS.Components;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>Ages and moves particle entities (Particle+Transform+Velocity+Sprite, no PhysicsBody), fading them out and destroying them once their lifetime expires. Position/rotation are integrated manually here since these aren't Box2D bodies.</summary>
public static class ParticleSystem
{
    private static readonly QueryDescription Query = new QueryDescription().WithAll<Particle, Transform, Velocity, Sprite>();

    public static void Run(World world, float deltaSeconds)
    {
        var expired = new List<Entity>();

        world.Query(in Query, (Entity entity, ref Particle particle, ref Transform transform, ref Velocity velocity, ref Sprite sprite) =>
        {
            particle.RemainingSeconds -= deltaSeconds;
            if (particle.RemainingSeconds <= 0f)
            {
                expired.Add(entity);
                return;
            }

            transform.PositionMeters += velocity.LinearMetersPerSecond * deltaSeconds;
            transform.RotationRadians += velocity.AngularRadiansPerSecond * deltaSeconds;

            var fraction = particle.RemainingSeconds / particle.TotalSeconds;
            sprite.Color = particle.BaseColor * fraction;
        });

        foreach (var entity in expired) world.Destroy(entity);
    }

    /// <summary>Destroys every live particle entity immediately, regardless of remaining lifetime — used on Restart so leftover explosion sparks/ship fragments from the previous life don't carry over (their shared textures are owned elsewhere and aren't affected).</summary>
    public static void Clear(World world)
    {
        var toDestroy = new List<Entity>();
        world.Query(in Query, (Entity entity, ref Particle particle) => toDestroy.Add(entity));
        foreach (var entity in toDestroy) world.Destroy(entity);
    }
}
