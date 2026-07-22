# Space Survival Game

A top-down, mouse+keyboard (twin-stick style) space survival/building game.
You pilot a ship exploring a large, free-form, procedurally generated
asteroid field (continuous positions, not tile/grid-based). You mine
asteroids, gather resources, and build a persistent space station out of
placeable modules.

## Tech stack

- **[MonoGame](https://monogame.net/)** (.NET, DesktopGL) — rendering, input, game loop
- **[Box2DNet](https://github.com/thomasvt/Box2DNet)** — physics (ship momentum/thrust, asteroid collision, debris), wraps Box2D v3
- **[Arch](https://github.com/genaray/Arch)** — archetype-based ECS for asteroids, debris, projectiles, station modules (hundreds to low thousands of entities)
- **Chunk-based world streaming** — the asteroid field is too large to hold entirely in memory; a chunk manager generates/loads regions around the ship and unloads distant ones, with per-chunk persistence for anything the player has changed
- **MessagePack or protobuf-net** — save data serialization, chunked by region so only changed data is written/read

## Project layout

```
SpaceSurvivalGame.slnx
src/
  SpaceSurvivalGame/          # the executable MonoGame project
    Program.cs                # entry point
    MainGame.cs                # MonoGame's Game subclass — boot/wiring only
    Content/                  # MGCB content pipeline (Content.mgcb + raw assets)
    Physics/                   # Box2D world wrapper, vector conversions
    Rendering/                 # Camera, procedural placeholder textures
    ShipConfig.cs              # loads config/ship-config.json
    WorldConfig.cs             # loads config/world-config.json
    config/
      ship-config.json          # tunable ship movement values, edit without recompiling
      world-config.json         # tunable asteroid field values, edit without recompiling
    ECS/
      Components/              # Transform, Velocity, Sprite, PhysicsBody, ShipMovement, PlayerControlled, Asteroid
      Systems/                 # ShipInputSystem, PhysicsSyncSystem, SpeedCapSystem, CameraFollowSystem, RenderSystem
      ShipEntity.cs            # creates the player ship entity; handles respawn
      Starfield.cs             # scatters placeholder background star entities
      AsteroidField.cs         # scatters a fixed-size field of dynamic, collidable asteroids
    World/                     # chunk manager — deferred, see note below
    Persistence/                # save/load — later milestone
```

## Roadmap

### Milestone 1 — project skeleton and a moving ship (done)

- [x] Set up a MonoGame project skeleton (.NET, DesktopGL cross-platform desktop target)
- [x] Get a basic game loop running: empty window, fixed timestep
- [x] Integrate Box2D (Box2DNet) for physics and get a single ship entity moving with thrust + rotation (momentum-based, no friction)
- [x] Set up Arch ECS with basic components (Transform, Velocity, Sprite) and get the ship rendering/moving through the ECS rather than as a one-off object
- [x] Camera that follows the ship

Also picked up along the way: Xbox controller support (mutually exclusive
with keyboard/mouse — whichever was used most recently wins) and a scattered
placeholder starfield so camera movement is visible against something.

### Milestone 2 — procedural asteroid field (fixed-size, no streaming yet)

- [x] Procedurally generated asteroid field: dynamic, collidable circle
  entities scattered across a large fixed-size area (no chunk streaming),
  tunable via world-config.json, deterministic per WorldSeed

**Chunk-based world streaming is deliberately deferred**, not dropped: the
parts that matter for iterating on gameplay feel (density, size variety,
collision behavior) don't need it, and it only pays off once the world needs
to feel genuinely unbounded or once the save/load milestone (chunked
persistence) arrives. Revisit then.

### Later milestones (not yet planned in detail)

- Chunk-based world streaming (if/when the field needs to feel unbounded)
- Mining and resource gathering
- Placeable space station modules and persistent building
- Save/load via chunked region serialization

## Status

Milestones 1 and 2 (deferred-chunking version) are complete. The ship and
asteroids are all Arch ECS entities with real Box2D physics — the ship is
player-driven (WASD/left-stick + right-stick or WASD-direction facing,
tunable via ship-config.json); asteroids are dynamic bodies with a small
random drift velocity and bounce off each other and the ship, density
calibrated so the smallest asteroid masses about the same as the ship
(tunable via world-config.json). Rendering is camera-relative and
depth-sorted (stars behind everything). Next up: deciding what comes after —
likely mining/resource gathering, or revisiting chunk streaming if the fixed
field starts to feel limiting.
