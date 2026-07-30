using Arch.Core;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Rendering;

namespace SpaceSurvivalGame.ECS.Systems;

/// <summary>
/// Runs one Playing-state frame's fixed-order system dispatch. The order here is load-bearing
/// (see the per-call comments) — this is the one place that sequence is allowed to live, so it
/// isn't scattered across MainGame or silently reordered by an unrelated edit.
/// </summary>
public static class GameplaySystemsPipeline
{
    public static void Run(World world, PhysicsWorld physicsWorld, Camera camera, GameConfigs configs, GameAssets assets,
        OxygenPickupField.PickupAssets pickupAssets, IronPickupField.PickupAssets ironAssets, System.Random random,
        KeyboardState keyboard, GamePadState gamePad, bool useController, System.Numerics.Vector2? mouseFacingDirection,
        float deltaSeconds, out bool shipDied, out bool suffocated)
    {
        ShipInputSystem.Run(world, keyboard, gamePad, useController, mouseFacingDirection, deltaSeconds, configs.Engine);
        RotationJetSystem.Run(world, assets.RotationJet, configs.Engine, configs.Ship.SpriteSizePixels, assets.RotationJetColor, random);
        physicsWorld.Step(deltaSeconds);
        CollisionDamageSystem.Run(world, physicsWorld, configs.Player, assets.Spark, random, configs.Spark, camera, configs.ScreenShake, configs.HitFlash, configs.HudFeedback); // must read hit events before the next Step overwrites them
        OxygenCrystalReleaseSystem.Run(world, physicsWorld, pickupAssets, configs.OxygenPickup, configs.World.Asteroid.OxygenRich, random, deltaSeconds); // same hit-event buffer, same must-run-before-next-Step constraint
        IronOreReleaseSystem.Run(world, physicsWorld, ironAssets, configs.IronPickup, configs.World.Asteroid.IronRich, random, deltaSeconds); // same hit-event buffer, same must-run-before-next-Step constraint

        shipDied = PlayerDeathSystem.TryTriggerCollisionDeath(world, assets, configs.DeathSequence, random);

        VitalsSystem.Run(world, deltaSeconds, configs.Player, configs.Suffocation);
        OxygenPickupSystem.Run(world, configs.Ship, configs.OxygenPickup, configs.Spark, assets.Spark, configs.FloatingText, camera, random);
        IronPickupSystem.Run(world, configs.Ship, configs.IronPickup, configs.Spark, assets.Spark, configs.FloatingText, camera, random);

        // Suffocation kills once its post-process effect has fully played out. Skipped if a
        // collision death already fired this frame, so the two death paths can't race each other.
        suffocated = !shipDied && PlayerDeathSystem.TryTriggerSuffocationDeath(world, configs.Suffocation);

        ParticleSystem.Run(world, deltaSeconds);
        FloatingTextSystem.Run(world, deltaSeconds);
        HitFlashSystem.Run(world, deltaSeconds, configs.HitFlash);
        InvulnerabilitySystem.Run(world, deltaSeconds);
        HudFeedbackSystem.Run(world, deltaSeconds, configs.HudFeedback, random);
        SpeedCapSystem.Run(world, deltaSeconds, configs.Ship.SpeedCapEaseSpeed);
        PhysicsSyncSystem.Run(world);
        // Must run after PhysicsSyncSystem so it copies the ship's just-synced position for
        // this frame, not last frame's stale value (a one-frame lag reads as constant drift
        // while riding along).
        StationCoreSystem.Run(world, camera, physicsWorld, configs.StationCore, deltaSeconds, random, assets.StationCoreDriftPuff, assets.StationCoreDriftPuffColor);

        // Camera casts out toward wherever the aim input points, not the ship's facing
        // (which lags behind at a capped turn rate): the right stick's own direction in
        // controller mode; in mouse mode, a point MouseFocusRatio of the way from the
        // ship's on-screen position to the cursor's — only while RMB is held (same gate as
        // mouseFacingDirection), so idly moving the mouse without aiming doesn't drag the
        // camera around.
        System.Numerics.Vector2 lookAheadOffsetMeters;
        if (useController)
        {
            var rightStick = new System.Numerics.Vector2(gamePad.ThumbSticks.Right.X, -gamePad.ThumbSticks.Right.Y);
            if (rightStick.LengthSquared() > 1f) rightStick = System.Numerics.Vector2.Normalize(rightStick);
            lookAheadOffsetMeters = rightStick * configs.Camera.MaxDistanceMeters;
        }
        else if (mouseFacingDirection.HasValue)
        {
            lookAheadOffsetMeters = PhysicsWorld.PixelsToMeters(mouseFacingDirection.Value * configs.Camera.MouseFocusRatio);
        }
        else
        {
            lookAheadOffsetMeters = System.Numerics.Vector2.Zero;
        }

        // Tweens in both modes now — since the look-ahead offset itself only engages while
        // RMB is held (or the right stick is pushed), an instant snap read as an abrupt jump
        // right at the moment of pressing/releasing; easing that transition in and out feels
        // smoother without lagging behind the cursor's own live position while held.
        CameraFollowSystem.Run(world, camera, lookAheadOffsetMeters, deltaSeconds, configs.Camera.TweenSpeed,
            configs.Camera.StrafeZoomMultiplier, configs.Camera.ZoomTweenSpeed);
    }
}
