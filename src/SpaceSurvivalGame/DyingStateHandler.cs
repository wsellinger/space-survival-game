using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceSurvivalGame.Configuration;
using SpaceSurvivalGame.ECS;
using SpaceSurvivalGame.ECS.Systems;
using SpaceSurvivalGame.Physics;
using SpaceSurvivalGame.Platform;

namespace SpaceSurvivalGame;

/// <summary>
/// Handles the Dying cutscene: no player input, but physics/particles keep animating
/// (including the ship's own residual momentum from Box2D) so the explosion and the dead
/// ship drifting still read as part of the world, not a frozen snapshot.
/// </summary>
public class DyingStateHandler : IGameStateHandler
{
    private readonly World _world;
    private readonly PhysicsWorld _physicsWorld;
    private readonly DeathSequenceConfig _deathConfig;
    private readonly DeathTimer _deathTimer;
    private readonly IGameHost _host;

    public DyingStateHandler(World world, PhysicsWorld physicsWorld, DeathSequenceConfig deathConfig, DeathTimer deathTimer, IGameHost host)
    {
        _world = world;
        _physicsWorld = physicsWorld;
        _deathConfig = deathConfig;
        _deathTimer = deathTimer;
        _host = host;
    }

    public GameState? Update(GameTime gameTime, KeyboardState keyboard, GamePadState gamePad, MouseState mouse,
        KeyboardState previousKeyboard, MouseState previousMenuMouse)
    {
        _host.IsMouseVisible = true;
        WindowsCursorLock.Release();

        var dyingDeltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _physicsWorld.Step(dyingDeltaSeconds);
        ParticleSystem.Run(_world, dyingDeltaSeconds);
        PhysicsSyncSystem.Run(_world);
        ShipEntity.Hide(_world); // re-assert each frame — HitFlashSystem still runs once more in the Playing frame where death triggers (after the initial Hide() call) and clobbers it back to visible

        _deathTimer.ElapsedSeconds += dyingDeltaSeconds;
        return _deathTimer.ElapsedSeconds >= _deathConfig.Fade.DelaySeconds + _deathConfig.Fade.DurationSeconds
            ? GameState.GameOver
            : null;
    }
}
