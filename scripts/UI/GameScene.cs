using Godot;

namespace Overland;

public partial class GameScene : Node2D
{
	public override void _Ready()
	{
		GD.Print("GameScene ready — Kilnwalk");
		AddChild(new WorldRoot { Name = "World" });
		AddChild(new GameUi { Name = "UI" });
	}
}
