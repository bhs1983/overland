using Godot;
using System.Collections.Generic;

namespace Overland;

/// <summary>Scene runner for CP3 room walk smoke test.</summary>
public partial class Cp3WalkRunner : Node
{
	public override void _Ready()
	{
		CallDeferred(nameof(Run));
	}

	private async void Run()
	{
		try
		{
			GameState.Instance.ResetNewGame();
			GameState.Instance.ApplyDebugCp3Start();

			var world = new WorldRoot { Name = "World" };
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			if (GameState.Instance.CurrentRoom != RoomId.DeadFanWalk)
				Fail($"start room {GameState.Instance.CurrentRoom}");

			var path = new List<(RoomId room, string spawn)>
			{
				(RoomId.SettersAlcove, "from_fan"),
				(RoomId.QuenchTrench, "from_alcove"),
				(RoomId.ClinkerYard, "from_quench"),
			};
			foreach (var (room, spawn) in path)
			{
				world.GoToRoom(room, spawn);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (GameState.Instance.CurrentRoom != room)
					Fail($"Expected {room}, got {GameState.Instance.CurrentRoom}");
				GD.Print($"OK room {room}");
			}

			GameState.Instance.ClinkerDown = true;
			GameState.Instance.DefeatedEnemyIds.Add("clinker_yard");
			world.GoToRoom(RoomId.KeyLanding, "from_clinker");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (GameState.Instance.CurrentRoom != RoomId.KeyLanding)
				Fail("Key Landing failed");
			GameState.Instance.HasStackKey = true;
			GameState.Instance.StackKeyTaken = true;
			GD.Print("OK Key Landing + Stack Key");

			world.GoToRoom(RoomId.SealedFlue, "from_key");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (GameState.Instance.CurrentRoom != RoomId.SealedFlue)
				Fail("Sealed Flue failed");
			GameState.Instance.IronDoorOpen = true;
			world.RefreshGates();
			GD.Print("OK Sealed Flue iron open");

			var frames = HeroAtlas.Frames;
			if (!frames.HasAnimation("fluewalker_idle_down"))
				Fail("missing fluewalker_idle_down");
			if (!frames.HasAnimation("fluewalker_swing_down"))
				Fail("missing fluewalker_swing_down");
			var swingFrames = frames.GetFrameCount("fluewalker_swing_down");
			if (swingFrames < 4)
				Fail($"swing frames {swingFrames}");
			if (HeroAtlas.PivotX != 16 || HeroAtlas.PivotY != 47)
				Fail("bad pivot");
			GD.Print("OK fluewalker SpriteFrames + pivot 16,47");
			GD.Print("PLAYABLE");
			GetTree().Quit(0);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr("CP3 walk failed: ", ex);
			GetTree().Quit(1);
		}
	}

	private void Fail(string msg)
	{
		GD.PrintErr("FAIL: ", msg);
		GetTree().Quit(1);
		throw new System.InvalidOperationException(msg);
	}
}
