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

## Performance & rendering model

The game uses a **retained-mode scene** instead of repainting every frame. This
is both fast and a prerequisite for OpenSilver (which has no immediate-mode
drawing). `GameCanvas` is a `Canvas` with two child layers:

- a **map layer** rendered by `Rendering/MapRenderer.cs`. Every cell that shares
  a colour is merged into a single vector `Path` (a `GeometryGroup` of cell
  rectangles), so a 23×23 arena with hundreds of blocks is ~3 elements total, not
  hundreds. It is rebuilt **only** when the map actually changes, detected via a
  `TiledMap.Version` counter — most ticks do nothing.
- an **entity layer** holding one small vector visual per mouse/cat/cheese
  (`Rendering/EntityVisuals.cs`). Each is created once and merely repositioned
  with `Canvas.SetLeft/SetTop` when it moves (`Rendering/EntitySprite.cs`).

A light **~30 Hz `DispatcherTimer`** advances the logic; after each update the
scene is *reconciled* (only changed elements are touched), and key presses
reconcile immediately so movement feels instant. Because there is no per-frame
repaint and the element count stays tiny, this runs comfortably even on
OpenSilver's DOM-backed renderer, where every `UIElement` is a DOM node.

## Running under OpenSilver

The gameplay + rendering code was written against the subset of `System.Windows.*`
APIs that OpenSilver implements (`Canvas`, `Shape`/`Path`/`Polygon`/`Ellipse`/
`Rectangle`, `GeometryGroup`, `Canvas.SetLeft/SetTop`, `DispatcherTimer`,
`MessageBox`), and rendering is retained-mode — so the bulk of the project ports
as-is. To create the OpenSilver app:

1. Install the OpenSilver templates (`dotnet new install OpenSilver.Templates`)
   and create an OpenSilver app, e.g. `dotnet new opensilver -o opensilver/O2R`.
2. **Share the engine code.** Add the existing `Common/`, `Entities/`, `Map/`,
   `Factories/`, `Managers/`, `Rendering/`, `Game/` and `Logging/` folders to the
   OpenSilver project (a shared project or `<Compile Include="..\..\wpf\…" Link="…"/>`
   links keep a single source of truth). They compile unchanged.
3. **Re-host the shell.** `MainWindow` is WPF-specific; re-create it as an
   OpenSilver `Page`/`UserControl` that hosts a `GameCanvas` and wires the menu
   commands. For keyboard, call `gameCanvas.AttachKeyboard(layoutRoot)` (the same
   call the WPF window makes). It uses `AddHandler(KeyDownEvent, …,
   handledEventsToo: true)` so the arrow keys aren't swallowed by focus
   navigation — without this they appear "not detected" under OpenSilver.
   **Also give the root focus**: OpenSilver only raises key events while some
   element is focused, so set the root focusable (`IsTabStop = true`) and call
   `Focus()` on load and whenever the play area is tapped, e.g.:

   ```csharp
   public MainPage()
   {
       InitializeComponent();
       _game = new GameCanvas();
       host.Child = _game;          // host is e.g. a Border/Viewbox in XAML
       _game.AttachKeyboard(this);  // 'this' = the page root

       IsTabStop = true;
       Loaded += (_, _) => Focus();
       // keep focus on the game after clicking the board / a menu:
       AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => Focus()), true);
   }
   ```
4. **File I/O.** The built-in campaign (`Game/Campaign.cs`,
   `TiledMapFactory.LoadLevelFromText`) needs no filesystem and works in the
   browser as-is. The desktop "load/save level from file" paths use
   `OpenFileDialog`/`SaveFileDialog` and `System.IO`; in the browser swap these
   for OpenSilver file pickers / `localStorage` (or omit them for a campaign-only
   build).

Nothing in `Rendering/`, the entities, the map or the pathfinder needs changing.

## Source mapping (C++ ➜ C#)

| Original (`src/…`)                         | Port (`OpenRodentsRevenge/…`)                  |
| ------------------------------------------ | ---------------------------------------------- |
| `main.cpp`                                 | `App.xaml(.cs)`                                |
| `MainWindow.{hpp,cpp,ui}`                  | `MainWindow.xaml(.cs)`                         |
| `GameCanvas.{hpp,cpp}` / `QSfmlCanvas.*`   | `Game/GameCanvas.cs` (`Canvas` + retained scene) |
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
| *(sprites — generated, not in original)*   | `Rendering/{TilePalette,EntityVisuals,EntitySprite,MapRenderer}.cs` |
| `managers/FilespathProvider.*`             | `Managers/FilespathProvider.cs`                |
| `dialogs/EditorLevelPropertiesDialog.*`    | `Dialogs/EditorLevelPropertiesDialog.xaml(.cs)`|
| `sf::Vector2i`, `sf::Clock`                | `Common/Vec2i.cs`, `Common/Clock.cs`           |
| `QsLog`                                    | `Logging/Logger.cs`                            |

## Intentional deviations (non-important functionality, per the request)

These do **not** affect gameplay:

1. **Sprites are generated in code** (`Rendering/EntityVisuals.cs` for entities,
   `Rendering/TilePalette.cs` for tile colours). The original proprietary bitmaps
   are not shipped with the repository (only mod `credit.txt` files remain), so
   each texture *alias* (`mouse.png`, `block.png`, …) is drawn as an equivalent
   16×16 vector visual. The alias→entity mapping is identical to the original.
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
5. **Rendering.** The original batched tiles into SFML vertex arrays and redrew
   each frame; here the scene is retained (a `Canvas` of vector `Path`/`Shape`
   elements) and reconciled only on change. This produces equivalent output, is
   faster, and is portable to OpenSilver (see "Performance & rendering model"
   and "Running under OpenSilver").

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
