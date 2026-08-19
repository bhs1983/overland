using Godot;

namespace Overland;

public partial class GameScene : Node2D
{
	public override void _Ready()
	{
		GD.Print("GameScene ready — Kilnwalk");
		AddChild(new WorldRoot { Name = "World" });
		AddChild(new GameUi { Name = "UI" });
		foreach (var arg in OS.GetCmdlineUserArgs())
		{
			if (arg == "--shot")
			{
				CallDeferred(nameof(CaptureShot));
				break;
			}
		}
	}

	private async void CaptureShot()
	{
		for (var i = 0; i < 50; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		RenderingServer.ForceDraw();
		var tex = GetViewport().GetTexture();
		if (tex == null)
		{
			GD.PrintErr("SHOT failed: no viewport texture");
			GetTree().Quit(1);
			return;
		}
		var img = tex.GetImage();
		if (img == null)
		{
			GD.PrintErr("SHOT failed: no image");
			GetTree().Quit(1);
			return;
		}
		var path = ProjectSettings.GlobalizePath("res://demo_kilnwalk.png");
		var err = img.SavePng(path);
		GD.Print("SHOT ", path, " ", img.GetWidth(), "x", img.GetHeight(), " err=", err);
		GetTree().Quit(err == Error.Ok ? 0 : 1);
	}
}
