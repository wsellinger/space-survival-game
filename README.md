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
    Rendering/                 # procedural placeholder textures
    Ship.cs                    # hand-rolled Box2D-driven ship (placeholder until ECS)
    ShipConfig.cs              # loads ship-config.json
    ship-config.json           # tunable ship movement values, edit without recompiling
    ECS/                       # Components/ and Systems/ (Arch) — not yet created
    World/                     # chunk manager, procedural generation — later milestone
    Persistence/                # save/load — later milestone
```

## Roadmap

### Milestone 1 — project skeleton and a moving ship (current)

- [x] Set up a MonoGame project skeleton (.NET, DesktopGL cross-platform desktop target)
- [x] Get a basic game loop running: empty window, fixed timestep
- [x] Integrate Box2D (Box2DNet) for physics and get a single ship entity moving with thrust + rotation (momentum-based, no friction)
- [ ] Set up Arch ECS with basic components (Transform, Velocity, Sprite) and get the ship rendering/moving through the ECS rather than as a one-off object
- [ ] Camera that follows the ship

### Later milestones (not yet planned in detail)

- Procedurally generated asteroid field with chunk-based world streaming
- Mining and resource gathering
- Placeable space station modules and persistent building
- Save/load via chunked region serialization

## Status

Milestone 1 is in progress: the ship moves under real Box2D physics (WASD
thrust, mouse-facing, momentum with a speed cap, no drag), tunable via
ship-config.json. It's still a hand-wired object rather than an ECS entity.
Next up is setting up Arch ECS and moving the ship onto it.
