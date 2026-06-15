# Open Rodent's Revenge — WPF port

A C# / WPF (.NET) port of the original C++ (Qt 5 + SFML 2 + MicroPather) source
located in the repository's `src/` folder. This is the 1.0 rewrite of the
open-source remake of Microsoft's *Rodent's Revenge* (1991).

The port lives entirely inside this `wpf/` folder; nothing outside it was
modified.

## Goal

Map the gameplay code **1:1** with the original C++ so that important
functionality is preserved exactly. Class names, method names, control flow and
even quirks (e.g. the verbatim use of `SIZE_MAX_LIMIT_X` when validating the Y
size in `TiledMapFactory::interpretOption`) were kept on purpose.

## Building & running

Requires the .NET SDK (the project targets `net10.0-windows`).

```bash
cd wpf
dotnet build
dotnet run --project OpenRodentsRevenge
```

The first campaign level loads automatically on launch, and an on-screen banner
explains the controls (a keyboard is required).

Use **Game ▸ New game** to start the built-in campaign, **Game ▸ Select level**
to jump to a specific built-in level, or **Game ▸ Play a level from file…** to
load one of the bundled `.txt`/`.xml` files (copied into
`OpenRodentsRevenge/levels`, also shipped next to the executable).

Move the mouse with the arrow keys; push blocks by walking into a run of blocks
that ends on a free ground tile. Trap each cat by surrounding it completely
(blocks + walls) — after a few seconds a trapped cat turns into cheese. Clear
every cat to advance to the next level; if a cat reaches you, the level
restarts.

## Gameplay completed beyond the original 1.0 source

The original 1.0 rewrite was **incomplete**: `GameScreen::start` never placed
any cats (the "Place the cats" section was empty), `Cat::isBlocked` was a stub
returning `true`, and the cheese transformation / win-lose handling did not
exist. To make the game actually playable, this port finishes those pieces:

- **Cats are spawned** on the ground ring at game start (`GameScreen`), in a
  count defined per built-in level.
- **Cat AI** (`Cat.Update`) chases the mouse via the ported pathfinder, wanders
  when no path exists, and turns into cheese after being fully trapped for
  `WAITING_TIME` (3s) — the original's own constants and chase/wait flow are
  reused.
- **Win / lose**: clearing all cats advances the campaign; being caught restarts
  the level.
- **Built-in campaign** (`Game/Campaign.cs`): 8 increasing-difficulty arenas in
  the classic "wall border + ground ring + pushable block core" style of the
  original `levels/1.txt`, loaded in memory via `TiledMapFactory.LoadLevelFromText`.

## Performance

The first version drove rendering from `CompositionTarget.Rendering` (the host's
full refresh rate, e.g. 144 Hz) and redrew every tile individually each frame,
which pegged the machine. This is fixed by:

- A **capped ~60 FPS** `DispatcherTimer` update/render loop that only repaints
  while a game/edit session is running.
- Caching the **static map as a single frozen `DrawingGroup`** (`TiledMap`),
  rebuilt only when a tile changes, so each frame is one `DrawDrawing` call plus
  a handful of moving entities — analogous to the original SFML vertex-array
  batching.

## Source mapping (C++ ➜ C#)

| Original (`src/…`)                         | Port (`OpenRodentsRevenge/…`)                  |
| ------------------------------------------ | ---------------------------------------------- |
| `main.cpp`                                 | `App.xaml(.cs)`                                |
| `MainWindow.{hpp,cpp,ui}`                  | `MainWindow.xaml(.cs)`                         |
| `GameCanvas.{hpp,cpp}` / `QSfmlCanvas.*`   | `Game/GameCanvas.cs` (WPF `FrameworkElement`)  |
| `game/Screen.*`                            | `Game/Screen.cs`                               |
| `game/GameScreen.*`                        | `Game/GameScreen.cs`                           |
| `game/EditorScreen.*`                      | `Game/EditorScreen.cs`                         |
| `game/EmptyScreen.*`                       | `Game/EmptyScreen.cs`                          |
| `entities/TiledEntity.*`                   | `Entities/TiledEntity.cs`                      |
| `entities/Tile.*`                          | `Entities/Tile.cs`                             |
| `entities/Mouse.*`                         | `Entities/Mouse.cs`                            |
| `entities/Cat.*`                           | `Entities/Cat.cs`                              |
| `entities/Trap.*`                          | `Entities/Trap.cs`                             |
| `map/TiledMap.*`                           | `Map/TiledMap.cs`                              |
| `map/TiledMapPathfinder.*` + `micropather` | `Map/TiledMapPathfinder.cs` (A* equivalent)    |
| `map/LevelInfo.*`                          | `Map/LevelInfo.cs`                             |
| `factories/TiledMapFactory.*`             | `Factories/TiledMapFactory.cs`                 |
| `managers/TilesTypesManager.*`             | `Managers/TilesTypesManager.cs` + `TileInfo.cs`|
| `managers/AssetsManager.*`                 | `Managers/AssetsManager.cs` + `Texture.cs`     |
| `managers/FilespathProvider.*`             | `Managers/FilespathProvider.cs`                |
| `dialogs/EditorLevelPropertiesDialog.*`    | `Dialogs/EditorLevelPropertiesDialog.xaml(.cs)`|
| `sf::Vector2i`, `sf::Clock`                | `Common/Vec2i.cs`, `Common/Clock.cs`           |
| `QsLog`                                    | `Logging/Logger.cs`                            |

## Intentional deviations (non-important functionality, per the request)

These do **not** affect gameplay:

1. **Sprites are generated in code** (`Managers/SpriteLibrary.cs`). The original
   proprietary bitmaps are not shipped with the repository (only mod
   `credit.txt` files remain), so each texture *alias* (`mouse.png`,
   `block.png`, …) is drawn as an equivalent 16×16 vector sprite. The
   alias→entity mapping is identical to the original.
2. **Modding system skipped (v1).** `FilespathProvider` keeps its public surface
   but no longer scans mod folders / overrides sprites. `ModsDialog`,
   `AvailableModsDialog` and the related menu were not ported.
3. **Pathfinder.** MicroPather's memory-pool implementation was replaced by a
   faithful A* using the exact same graph definition (8-neighbour adjacency,
   GROUND-only walkable tiles, uniform step cost, squared-Euclidean heuristic)
   and the same output contract (path includes start and end).
6. **Cats / campaign / win-lose** were added on top of the original (which left
   them unimplemented) — see "Gameplay completed beyond the original 1.0 source"
   above.
4. **Multi-language / translations, settings persistence, the "About Qt" box and
   the QsLog file/debug destinations** were not ported (replaced by a trivial
   `Trace` logger and a simple About box).
5. **Rendering.** The original batched tiles into SFML vertex arrays for
   performance; here tiles are drawn directly via a WPF `DrawingContext`, which
   is the idiomatic equivalent and produces identical output.

## Gameplay logic preserved 1:1

- Level loading for both the old TXT format and the new XML format, including
  the option parsing, mouse-position handling, ragged tile rows and the
  post-load size-fix logic (`TiledMapFactory`).
- Mouse movement and block pushing (`Mouse.Move`): walking onto ground moves the
  mouse; walking into a run of blocks shifts them one cell if the cell past the
  run is free ground.
- Mouse placement on start (fixed position if valid, otherwise a random ground
  tile via the bounded-retry `RandomEmptyPos`).
- Cat tracking/clock/blocked-state logic (`Cat.Update`).
- The level editor: click-to-place tiles and the mouse start position, the
  contiguous-tile growth rule in `TiledMap.SetTileChar`, and TXT/XML saving.
