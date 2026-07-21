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
    ECS/                       # Components/ and Systems/ (Arch) — not yet created
    Physics/                   # Box2D world wrapper, ECS<->Box2D sync — not yet created
    World/                     # chunk manager, procedural generation — later milestone
    Persistence/                # save/load — later milestone
```

## Roadmap

### Milestone 1 — project skeleton and a moving ship (current)

- [x] Set up a MonoGame project skeleton (.NET, DesktopGL cross-platform desktop target)
- [x] Get a basic game loop running: empty window, fixed timestep
- [ ] Integrate Box2D (Box2DNet) for physics and get a single ship entity moving with thrust + rotation (momentum-based, no friction)
- [ ] Set up Arch ECS with basic components (Transform, Velocity, Sprite) and get the ship rendering/moving through the ECS rather than as a one-off object
- [ ] Camera that follows the ship

### Later milestones (not yet planned in detail)

- Procedurally generated asteroid field with chunk-based world streaming
- Mining and resource gathering
- Placeable space station modules and persistent building
- Save/load via chunked region serialization

## Status

Milestone 1 is in progress: the project builds and runs, showing a blank
window via MonoGame's fixed-timestep game loop. Next up is wiring in Box2D
physics for ship movement.
