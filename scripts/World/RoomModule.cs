using Godot;
using System.Collections.Generic;

namespace Overland.World;

/// <summary>
/// Base class for every authored room in Slice 0.
/// Declares connection sockets so rooms can later become modular modules.
/// </summary>
public partial class RoomModule : Node2D
{
    [Export] public string ModuleId = "unnamed_room";
    [Export] public string[] ThemeTags = System.Array.Empty<string>();
    [Export] public string[] RequiredTools = System.Array.Empty<string>();
    [Export] public bool IsTerminal = false;

    // Sockets will be defined as child Marker2D nodes or via exported data
    // For now we keep it simple and document the expected structure.

    public override void _Ready()
    {
        // Validation / registration can go here later
        GD.Print($"RoomModule ready: {ModuleId}");
    }
}

/// <summary>
/// Data description of a single connection socket.
/// </summary>
[System.Serializable]
public partial class SocketData : Resource
{
    public enum Side { North, East, South, West }
    public enum SocketType { Standard, Gated, OneWay, Vertical }

    [Export] public Side SocketSide;
    [Export] public float Offset;          // tiles from center of side
    [Export] public SocketType Type = SocketType.Standard;
    [Export] public string RequiredTool;   // only for Gated
}
