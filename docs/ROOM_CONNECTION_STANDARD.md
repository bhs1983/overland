# Overland Room Connection Standard (v1)

**Status:** Locked for Slice 0 compatibility. Designed so all authored rooms can later become modular modules without redesign.

## Goals
- Keep Slice 0 fully hand-authored and high quality
- Make future modular / light procedural expansion (Daggerfall-style) trivial
- Guarantee connectivity through standardized sockets

## Rules

### 1. Grid
All rooms are rectangular modules on a fixed tile grid (recommended base: 16px or 32px tiles).

### 2. Connection Sockets
- Each of the four sides may have 0, 1, or 2 sockets.
- Sockets are always centered or use fixed offsets (±4 or ±8 tiles from center).
- All sockets share the same height/Z and the same doorway width (2–3 tiles).
- Matching socket positions on adjacent modules = guaranteed walkable connection.

### 3. Socket Types
| Type       | Purpose                              |
|------------|--------------------------------------|
| Standard   | Normal walkable doorway              |
| Gated      | Requires tool or key                 |
| OneWay     | Directional (enter only one way)     |
| Vertical   | Ladder / drop (future multi-level)   |

### 4. Required Module Metadata
Every room scene must expose:

```csharp
public partial class RoomModule : Node2D
{
    [Export] public string ModuleId;
    [Export] public string[] ThemeTags;          // e.g. "cold", "ash", "fan", "boss"
    [Export] public Godot.Collections.Array<SocketData> Sockets;
    [Export] public string[] RequiredTools;      // for gated connections
    [Export] public bool IsTerminal;             // boss / end room
}
```

### 5. Slice 0 Authoring Rule
All 10 Cold Stack rooms must declare their sockets even while they are hard-wired. This makes later conversion to a modular system almost free.

### 6. Future Generation Pattern (post-Slice 0)
1. Place core modules so open sockets match.
2. Fill remaining open sockets with "outer / dead-end" modules.
3. Enforce theme and tool constraints via tags.

This is the same high-level approach Daggerfall used successfully with inner + outer blocks.
