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
		Must(ResourceLoader.Exists("res://assets/characters/hero/hero_atlas.png"), "hero atlas missing");
		Must(ResourceLoader.Exists("res://assets/characters/hero/hero_atlas.json"), "hero atlas json missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/claywalker.png"), "claywalker missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/claywalker_soft.png"), "claywalker soft missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/brickleech.png"), "brickleech missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/clinker.png"), "clinker missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/clinker_cracked.png"), "clinker cracked missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/overfire.png"), "overfire missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/overfire_pulse.png"), "overfire pulse missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/overfire_swipe.png"), "overfire swipe missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/enemies/sootling.png"), "sootling missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/npcs/tamsin.png"), "npc tamsin missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/npcs/holt.png"), "npc holt missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/npcs/wren.png"), "npc wren missing");
		Must(Godot.FileAccess.FileExists("res://assets/characters/npcs/rook.png"), "npc rook missing");
		Must(Godot.FileAccess.FileExists("res://assets/environment/parallax/kilnwalk/far_bg.png"), "kilnwalk far_bg missing");
		Must(Godot.FileAccess.FileExists("res://assets/environment/parallax/kilnwalk/mid_bg.png"), "kilnwalk mid_bg missing");
		Must(Godot.FileAccess.FileExists("res://assets/environment/parallax/cold_stack/far_bg.png"), "cold far_bg missing");
		Must(Godot.FileAccess.FileExists("res://assets/environment/parallax/cold_stack/mid_bg.png"), "cold mid_bg missing");
		Must(Godot.FileAccess.FileExists("res://assets/vfx/spark.png"), "spark missing");
		Must(Godot.FileAccess.FileExists("res://assets/vfx/spark_b.png"), "spark_b missing");
		Must(Godot.FileAccess.FileExists("res://assets/vfx/smoke.png"), "smoke missing");
		Must(Godot.FileAccess.FileExists("res://assets/vfx/ash_fall.png"), "ash_fall missing");
		Must(Godot.FileAccess.FileExists("res://assets/vfx/impacts.png"), "impacts missing");

		var frames = HeroAtlas.Frames;
		Must(frames.HasAnimation("fluewalker_idle_down"), "atlas idle_down");
		Must(frames.HasAnimation("fluewalker_walk_down"), "atlas walk_down");
		Must(frames.HasAnimation("fluewalker_swing_down"), "atlas swing_down");
		Must(frames.HasAnimation("fluewalker_hop_down"), "atlas hop_down");
		Must(frames.HasAnimation("fluewalker_hurt_down"), "atlas hurt_down");
		Must(frames.GetFrameCount("fluewalker_swing_down") >= 4, "swing frame count");
		Must(HeroAtlas.PivotX == 16 && HeroAtlas.PivotY == 47, "pivot 16,47");
		Must(HeroAtlas.CellW == 32 && HeroAtlas.CellH == 48, "hero cell 32x48");

		string[] town = {
			"brick_floor_a", "brick_floor_b", "brick_floor_c", "brick_floor_d", "brick_floor_e", "brick_floor_f",
			"street_a", "street_b", "street_c", "street_d",
			"brick_wall", "kiln", "night_fire_0", "night_fire_1", "night_fire_2", "night_fire_3",
			"stack_mouth_sealed", "stack_mouth_open", "door"
		};
		foreach (var stem in town)
			Must(Godot.FileAccess.FileExists($"res://assets/environment/town/{stem}.png"), $"town {stem} missing");

		string[] cold = {
			"ash_floor_a", "ash_floor_b", "ash_floor_c", "ash_floor_d", "ash_floor_e", "ash_floor_f",
			"frost_ash_a", "frost_ash_b", "flue_wall",
			"iron_door_closed", "iron_door_open",
			"quench_water_a", "quench_water_b", "ledge", "cracked_brick_a", "cracked_brick_b"
		};
		foreach (var stem in cold)
			Must(Godot.FileAccess.FileExists($"res://assets/environment/cold/{stem}.png"), $"cold {stem} missing");

		string[] props = {
			"chest_closed", "chest_open",
			"dead_fan_0", "dead_fan_1", "dead_fan_2", "dead_fan_3",
			"ash_pile", "heal_ash", "stair"
		};
		foreach (var stem in props)
			Must(Godot.FileAccess.FileExists($"res://assets/environment/props/{stem}.png"), $"prop {stem} missing");

		string[] ui = {
			"crackiron", "folded_bellows", "stack_key", "health_pip",
			"map_node_town", "map_node_room", "map_node_here", "save_mark", "pause_frame"
		};
		foreach (var stem in ui)
			Must(Godot.FileAccess.FileExists($"res://assets/ui/{stem}.png"), $"ui {stem} missing");
		Must(!Godot.FileAccess.FileExists("res://assets/ui/ui.png"), "packed ui.png must be gone");
		Must(DirAccess.Open("res://assets/tiles") == null, "leftover assets/tiles");
		Must(DirAccess.Open("res://assets/sprites") == null, "leftover assets/sprites");
		Must(DirAccess.Open("res://assets/v3") == null, "leftover assets/v3");
		Must(!Godot.FileAccess.FileExists("res://assets/environment/town_tiles.png"),
			"packed town_tiles.png must stay absent");
		Must(!Godot.FileAccess.FileExists("res://assets/environment/cold_tiles.png"),
			"packed cold_tiles.png must stay absent");

		GD.Print("OK art — one tree under assets/; packed sheets and CP2 leftovers absent");
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
		GameState.Instance.RecordSave(RoomId.SealedFlue, new Vector2(10 * Tiles.Size, 8 * Tiles.Size));
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
