using Godot;

namespace Overland.Core;

/// <summary>
/// Lightweight game manager for Slice 0.
/// Handles high-level state (in town, in dungeon, pause, save points).
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Title,
        Town,
        Dungeon,
        Pause,
        Dialogue
    }

    public GameState CurrentState { get; private set; } = GameState.Town;

    public override void _Ready()
    {
        Instance = this;
        GD.Print("Overland GameManager ready - Slice 0");
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        // Emit signals or update systems here later
    }
}
