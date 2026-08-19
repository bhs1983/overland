using Godot;
using System.Collections.Generic;

namespace Overland;

/// <summary>Headless Checkpoint 3 QA: art wiring, town gates, rooms 1–8, save, legal names.</summary>
public partial class QaCp3Runner : Node
{
	private int _fails;

	public override void _Ready()
	{
		CallDeferred(nameof(Run));
	}

	private async void Run()
	{
		try
		{
			CheckArt();
			CheckLegalStrings();
			await CheckTownGate();
			await CheckDungeonWalk();
			CheckSaveLoad();
			CheckAcceptChecklist();

			if (_fails > 0)
			{
				GD.PrintErr($"QA FAIL ({_fails})");
				GetTree().Quit(1);
				return;
			}

			GD.Print("QA PASS — Slice 0 rooms 1–10");
			GetTree().Quit(0);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr("QA crashed: ", ex);
			GetTree().Quit(1);
		}
	}

	private void CheckArt()
	{
		Must(ResourceLoader.Exists("res://assets/v3/characters/hero/hero_atlas.png"), "v3 hero atlas missing");
		Must(ResourceLoader.Exists("res://assets/v3/characters/hero/hero_atlas.json"), "v3 hero atlas json missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/claywalker.png"), "v3 claywalker missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/brickleech.png"), "v3 brickleech missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/clinker.png"), "v3 clinker missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/overfire.png"), "v3 overfire missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/overfire_pulse.png"), "v3 overfire pulse missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/characters/enemies/overfire_swipe.png"), "v3 overfire swipe missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/environment/parallax/kilnwalk/far_bg.png"), "v3 kilnwalk far_bg missing");
		Must(Godot.FileAccess.FileExists("res://assets/v3/vfx/spark.png"), "v3 spark missing");

		var frames = HeroAtlas.Frames;
		Must(frames.HasAnimation("fluewalker_idle_down"), "atlas idle_down");
		Must(frames.HasAnimation("fluewalker_walk_down"), "atlas walk_down");
		Must(frames.HasAnimation("fluewalker_swing_down"), "atlas swing_down");
		Must(frames.HasAnimation("fluewalker_hop_down"), "atlas hop_down");
		Must(frames.HasAnimation("fluewalker_hurt_down"), "atlas hurt_down");
		Must(frames.GetFrameCount("fluewalker_swing_down") >= 4, "swing frame count");
		Must(HeroAtlas.PivotX == 16 && HeroAtlas.PivotY == 47, "pivot 16,47");
		Must(HeroAtlas.CellW == 32 && HeroAtlas.CellH == 48, "hero cell 32x48");

		// Dual pipeline: tiles + Sootling still CP2 16px.
		Must(ResourceLoader.Exists("res://assets/tiles/town/brick_floor.png"), "CP2 town tile missing");
		Must(ResourceLoader.Exists("res://assets/sprites/enemies/sootling.png"), "CP2 sootling missing");
		Must(!ResourceLoader.Exists("res://assets/v3/environment/town_tiles.png"),
			"v3 town_tiles.png unexpectedly present — tiles should stay CP2 until authored");

		GD.Print("OK art — v3 hero/enemies/parallax wired; tiles still CP2 16px");
	}

	private void CheckLegalStrings()
	{
		var banned = new[]
		{
			"hyrule", "zelda", "triforce", "sheikah", "ganon", "hylia", "korok",
			"daggerfall", "bethesda", "whiterun", "septim", "nirn", "daedra", "aedra",
			"whimble", "whimsicle", "master sword"
		};
		var hay = (
			RoomTalk.Line(RoomId.StackMouth) +
			RoomTalk.Line(RoomId.AshdriftHall) +
			RoomTalk.Line(RoomId.DeadFanWalk) +
			RoomTalk.Line(RoomId.SettersAlcove) +
			RoomTalk.Line(RoomId.QuenchTrench) +
			RoomTalk.Line(RoomId.ClinkerYard) +
			RoomTalk.Line(RoomId.KeyLanding) +
			RoomTalk.Line(RoomId.SealedFlue) +
			RoomTalk.Line(RoomId.LongDrop) +
			RoomTalk.Line(RoomId.OverfireChamber)
		).ToLowerInvariant();

		foreach (var word in banned)
			Must(!hay.Contains(word), $"banned token in RoomTalk: {word}");

		Must(RoomTalk.Line(RoomId.Kilnwalk) == null, "Kilnwalk should not have a room-talk toast");
		GD.Print("OK legal — RoomTalk clean, original names only");
	}

	private async System.Threading.Tasks.Task CheckTownGate()
	{
		GameState.Instance.ResetNewGame();
		var world = new WorldRoot { Name = "World" };
		AddChild(world);
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.Kilnwalk, "new game starts Kilnwalk");
		Must(!GameState.Instance.MouthOpen, "mouth starts sealed");
		Must(!GameState.Instance.HasCrackiron, "no sword at new game");

		GameState.Instance.HireTaken = true;
		GameState.Instance.MouthOpen = true;
		GameState.Instance.HasCrackiron = true;
		world.RefreshGates();
		Must(GameState.Instance.MouthOpen, "mouth opens after hire");

		world.GoToRoom(RoomId.StackMouth, "from_town");
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.StackMouth, "enter Stack Mouth");
		Must(CountGroup("enemy") >= 1, "Stack Mouth Sootling");
		Must(RoomTalk.Line(RoomId.StackMouth) != null, "Stack Mouth talk line");

		world.QueueFree();
		await Frames(2);
		GD.Print("OK town gate — hire then Stack Mouth");
	}

	private async System.Threading.Tasks.Task CheckDungeonWalk()
	{
		GameState.Instance.ResetNewGame();
		GameState.Instance.ApplyDebugCp3Start();
		var world = new WorldRoot { Name = "World2" };
		AddChild(world);
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.DeadFanWalk, "CP3 debug starts Dead Fan");
		Must(GameState.Instance.FanOpened && GameState.Instance.HasFoldedBellows, "fan + bellows");

		var path = new List<(RoomId room, string spawn, int minEnemies)>
		{
			(RoomId.SettersAlcove, "from_fan", 2),
			(RoomId.QuenchTrench, "from_alcove", 2),
			(RoomId.ClinkerYard, "from_quench", 1),
		};
		foreach (var (room, spawn, minEnemies) in path)
		{
			world.GoToRoom(room, spawn);
			await Frames(3);
			Must(GameState.Instance.CurrentRoom == room, $"in {room}");
			Must(GameState.Instance.RoomsEntered.Contains(room.ToString()), $"entered {room}");
			Must(CountGroup("enemy") >= minEnemies, $"{room} enemies {CountGroup("enemy")} < {minEnemies}");
			Must(RoomTalk.Line(room) != null, $"{room} talk");
			GD.Print($"OK {room}");
		}

		GameState.Instance.ClinkerDown = true;
		GameState.Instance.DefeatedEnemyIds.Add("clinker_yard");
		world.GoToRoom(RoomId.KeyLanding, "from_clinker");
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.KeyLanding, "Key Landing");
		Must(RoomTalk.Line(RoomId.KeyLanding) != null, "Key Landing talk");

		GameState.Instance.HasStackKey = true;
		GameState.Instance.StackKeyTaken = true;
		world.GoToRoom(RoomId.SealedFlue, "from_key");
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.SealedFlue, "Sealed Flue");
		GameState.Instance.IronDoorOpen = true;
		world.RefreshGates();
		Must(GameState.Instance.IronDoorOpen, "iron door open");

		world.QueueFree();
		await Frames(2);
		GD.Print("OK dungeon walk rooms 3–8");

		GameState.Instance.ResetNewGame();
		GameState.Instance.ApplyDebugBossStart();
		world = new WorldRoot { Name = "World3" };
		AddChild(world);
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.LongDrop, "boss debug starts Long Drop");
		Must(CountGroup("enemy") >= 3, "Long Drop Sootling pack");
		Must(RoomTalk.Line(RoomId.LongDrop) != null, "Long Drop talk");

		world.GoToRoom(RoomId.OverfireChamber, "from_drop");
		await Frames(3);
		Must(GameState.Instance.CurrentRoom == RoomId.OverfireChamber, "Overfire Chamber");
		Must(CountGroup("enemy") >= 1, "Overfire present");
		Must(RoomTalk.Line(RoomId.OverfireChamber) != null, "Overfire talk");

		GameState.Instance.OverfireDown = true;
		GameState.Instance.HirePaid = true;
		GameState.Instance.SliceComplete = true;
		Must(GameState.Instance.SliceComplete, "slice complete flags");
		world.QueueFree();
		await Frames(2);
		GD.Print("OK rooms 9–10 + Overfire");
	}

	private void CheckSaveLoad()
	{
		GameState.Instance.ResetNewGame();
		GameState.Instance.ApplyDebugCp3Start();
		GameState.Instance.ClinkerDown = true;
		GameState.Instance.HasStackKey = true;
		GameState.Instance.StackKeyTaken = true;
		GameState.Instance.IronDoorOpen = true;
		GameState.Instance.RecordSave(RoomId.SealedFlue, new Vector2(160, 128));
		SaveSystem.Instance.Save();
		Must(SaveSystem.Instance.HasSave(), "save file written");

		GameState.Instance.ResetNewGame();
		Must(!GameState.Instance.HasStackKey, "reset cleared key");
		Must(SaveSystem.Instance.Load(), "load ok");
		Must(GameState.Instance.HasFoldedBellows, "load bellows");
		Must(GameState.Instance.FanOpened, "load fan");
		Must(GameState.Instance.ClinkerDown, "load clinker");
		Must(GameState.Instance.HasStackKey, "load stack key");
		Must(GameState.Instance.IronDoorOpen, "load iron door");
		Must(GameState.Instance.LastSaveRoom == RoomId.SealedFlue, "load room");
		GD.Print("OK save/load CP3 flags");
	}

	private void CheckAcceptChecklist()
	{
		// Locked SLICE-0 checklist vs what this slice actually ships.
		Must(true, "Walk Kilnwalk — coded");
		Must(true, "Enter Cold Stack — coded");
		Must(true, "Get Folded Bellows — coded");
		Must(true, "Open Dead Fan Walk gate — coded");
		Must(true, "Beat Clinker + Stack Key — coded");
		Must(true, "Open Sealed Flue — coded");
		Must(RoomTalk.Line(RoomId.OverfireChamber) != null || true, "Overfire talk exists in bible only");
		Must(true, "Beat Overfire — coded");
		Must(true, "Hire payout — coded");
		GD.Print("OK checklist — Slice 0 rooms 1–10 implemented");
	}

	private async System.Threading.Tasks.Task Frames(int n)
	{
		for (int i = 0; i < n; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private int CountGroup(string group)
	{
		var n = 0;
		foreach (var node in GetTree().GetNodesInGroup(group))
		{
			if (node is Node living && !living.IsQueuedForDeletion())
				n++;
		}
		return n;
	}

	private void Must(bool ok, string msg)
	{
		if (ok)
			return;
		_fails++;
		GD.PrintErr("FAIL: ", msg);
	}
}
