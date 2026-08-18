using Godot;

namespace Overland;

/// <summary>Checkpoint 2 — Kilnwalk + Cold Stack rooms 1–3.</summary>
public partial class WorldRoot : Node2D
{
	private Node2D _roomLayer = null!;
	private PlayerController _player = null!;
	private MouthGate? _mouthGate;
	private FanEastDoor? _fanEastDoor;
	private bool _transitioning;

	public override void _Ready()
	{
		AddToGroup("world");
		_roomLayer = new Node2D { Name = "RoomLayer" };
		AddChild(_roomLayer);

		_player = new PlayerController { Name = "Player" };
		AddChild(_player);

		var cam = new Camera2D
		{
			Enabled = true,
			Zoom = new Vector2(3, 3),
			PositionSmoothingEnabled = true,
			PositionSmoothingSpeed = 8f
		};
		_player.AddChild(cam);

		var startRoom = GameState.Instance.LastSaveRoom;
		var spawn = GameState.Instance.LastSavePosition;
		if (spawn == Vector2.Zero)
			spawn = SpawnFor(RoomId.Kilnwalk, "default");
		LoadRoom(startRoom, spawn);
	}

	public void RespawnAtSave()
	{
		GameState.Instance.InputLocked = false;
		LoadRoom(GameState.Instance.LastSaveRoom, GameState.Instance.LastSavePosition);
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Woke at last save.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	public void RefreshGates()
	{
		_mouthGate?.Refresh();
		_fanEastDoor?.Refresh();
		foreach (var n in GetTree().GetNodesInGroup("fan_east_door"))
		{
			if (n is FanEastDoor d)
				d.Refresh();
		}
	}

	public void GoToRoom(RoomId room, string spawnId)
	{
		if (_transitioning)
			return;
		_transitioning = true;
		LoadRoom(room, SpawnFor(room, spawnId));
		_transitioning = false;
	}

	private void LoadRoom(RoomId room, Vector2 spawn)
	{
		foreach (var c in _roomLayer.GetChildren())
			c.QueueFree();
		_mouthGate = null;
		_fanEastDoor = null;

		switch (room)
		{
			case RoomId.StackMouth:
				BuildStackMouth();
				break;
			case RoomId.AshdriftHall:
				BuildAshdriftHall();
				break;
			case RoomId.DeadFanWalk:
				BuildDeadFanWalk();
				break;
			default:
				BuildKilnwalk();
				room = RoomId.Kilnwalk;
				break;
		}

		_player.GlobalPosition = spawn;
		GameState.Instance.MarkRoomEntered(room);
		GameState.Instance.CurrentRoom = room;
		CallDeferred(nameof(RefreshGates));
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	private static Vector2 SpawnFor(RoomId room, string spawnId) => room switch
	{
		RoomId.StackMouth when spawnId == "from_town" => new Vector2(10 * Tiles.Size, 8 * Tiles.Size),
		RoomId.StackMouth when spawnId == "from_ash" => new Vector2(10 * Tiles.Size, 2.5f * Tiles.Size),
		RoomId.StackMouth => new Vector2(10 * Tiles.Size, 10 * Tiles.Size),
		RoomId.AshdriftHall when spawnId == "from_mouth" => new Vector2(10 * Tiles.Size, 12 * Tiles.Size),
		RoomId.AshdriftHall when spawnId == "from_fan" => new Vector2(17 * Tiles.Size, 7 * Tiles.Size),
		RoomId.AshdriftHall => new Vector2(10 * Tiles.Size, 10 * Tiles.Size),
		RoomId.DeadFanWalk when spawnId == "from_ash" => new Vector2(2.5f * Tiles.Size, 7 * Tiles.Size),
		RoomId.DeadFanWalk => new Vector2(4 * Tiles.Size, 7 * Tiles.Size),
		RoomId.Kilnwalk when spawnId == "from_mouth" => new Vector2(10 * Tiles.Size, 3 * Tiles.Size),
		_ => new Vector2(10 * Tiles.Size, 11 * Tiles.Size)
	};

	private void BuildKilnwalk()
	{
		var root = new Node2D { Name = "Kilnwalk" };
		_roomLayer.AddChild(root);

		const int W = 20;
		const int H = 15;

		for (int y = 1; y < H - 1; y++)
		{
			for (int x = 1; x < W - 1; x++)
			{
				var tile = (y < 6) ? "brick_floor" : "street";
				if (x >= 7 && x <= 12 && y >= 3 && y <= 7)
					tile = "brick_floor";
				PlaceFloor(root, tile, x, y);
			}
		}

		AddBorderWalls(root, W, H);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);

		AddRoomTitle(root, "Kilnwalk");
		PlaceTile(root, "kiln", 8, 4);
		PlaceTile(root, "kiln", 11, 4);
		PlaceTile(root, "door", 5, 2);

		AddNpc(root, new Vector2(4 * Tiles.Size, 5 * Tiles.Size), "Tamsin Cole", Palette.NpcTamsin);
		AddNpc(root, new Vector2(4 * Tiles.Size, 9 * Tiles.Size), "Holt Vetch", Palette.NpcHolt);
		AddNpc(root, new Vector2(10 * Tiles.Size, 8 * Tiles.Size), "Wren Quill", Palette.NpcWren);
		AddNpc(root, new Vector2(14 * Tiles.Size, 4 * Tiles.Size), "Rook Darnel", Palette.NpcRook);

		root.AddChild(new SavePoint
		{
			Position = new Vector2(10 * Tiles.Size, 9.5f * Tiles.Size),
			SaveRoom = RoomId.Kilnwalk
		});

		_mouthGate = new MouthGate
		{
			Position = new Vector2(10 * Tiles.Size, 1.5f * Tiles.Size)
		};
		root.AddChild(_mouthGate);

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 0.5f * Tiles.Size),
			Target = RoomId.StackMouth,
			SpawnId = "from_town",
			RequiresMouthOpen = true,
			TriggerSize = new Vector2(28, 12)
		});
	}

	private void BuildStackMouth()
	{
		var root = new Node2D { Name = "StackMouth" };
		_roomLayer.AddChild(root);
		const int W = 20;
		const int H = 14;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 4, 4);
		PlaceCracked(root, 15, 8);
		PlaceCracked(root, 7, 11);
		ClearWallAt(root, 9, H - 1);
		ClearWallAt(root, 10, H - 1);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);
		AddRoomTitle(root, "Stack Mouth");

		root.AddChild(new SavePoint
		{
			Position = new Vector2(6 * Tiles.Size, 10 * Tiles.Size),
			SaveRoom = RoomId.StackMouth
		});

		root.AddChild(new Sootling
		{
			Position = new Vector2(12 * Tiles.Size, 6 * Tiles.Size),
			EnemyId = "sootling_stack_mouth"
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.Kilnwalk,
			SpawnId = "from_mouth",
			TriggerSize = new Vector2(28, 12)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 4),
			Target = RoomId.AshdriftHall,
			SpawnId = "from_mouth",
			TriggerSize = new Vector2(28, 12)
		});
	}

	private void BuildAshdriftHall()
	{
		var root = new Node2D { Name = "AshdriftHall" };
		_roomLayer.AddChild(root);
		const int W = 20;
		const int H = 14;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 5, 3);
		PlaceCracked(root, 13, 9);
		PlaceCracked(root, 16, 5);
		ClearWallAt(root, 9, H - 1);
		ClearWallAt(root, 10, H - 1);
		ClearWallAt(root, W - 1, 6);
		ClearWallAt(root, W - 1, 7);
		AddRoomTitle(root, "Ashdrift Hall");

		// Ash piles block the mid corridor
		PlaceAsh(root, "ashdrift_a", 8, 5);
		PlaceAsh(root, "ashdrift_b", 9, 5);
		PlaceAsh(root, "ashdrift_c", 10, 5);
		PlaceAsh(root, "ashdrift_d", 11, 5);
		PlaceAsh(root, "ashdrift_e", 9, 6);
		PlaceAsh(root, "ashdrift_f", 10, 7);

		root.AddChild(new BellowsChest
		{
			Position = new Vector2(14 * Tiles.Size, 4 * Tiles.Size)
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.StackMouth,
			SpawnId = "from_ash",
			TriggerSize = new Vector2(28, 12)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2((W - 1) * Tiles.Size + 4, 7 * Tiles.Size),
			Target = RoomId.DeadFanWalk,
			SpawnId = "from_ash",
			TriggerSize = new Vector2(12, 28)
		});
	}

	private void BuildDeadFanWalk()
	{
		var root = new Node2D { Name = "DeadFanWalk" };
		_roomLayer.AddChild(root);
		const int W = 18;
		const int H = 12;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 3, 3);
		PlaceCracked(root, 14, 8);
		ClearWallAt(root, 0, 5);
		ClearWallAt(root, 0, 6);
		ClearWallAt(root, W - 1, 5);
		ClearWallAt(root, W - 1, 6);
		AddRoomTitle(root, "Dead Fan Walk");

		root.AddChild(new DeadFan
		{
			Position = new Vector2(9 * Tiles.Size, 5 * Tiles.Size)
		});

		_fanEastDoor = new FanEastDoor
		{
			Position = new Vector2((W - 1) * Tiles.Size + Tiles.Size / 2f, 6 * Tiles.Size)
		};
		root.AddChild(_fanEastDoor);

		// West back to Ashdrift
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(4, 6 * Tiles.Size),
			Target = RoomId.AshdriftHall,
			SpawnId = "from_fan",
			TriggerSize = new Vector2(12, 28)
		});

		// East past the open door — Checkpoint 2 ends here (rooms 4+ later)
		root.AddChild(new CheckpointToastZone
		{
			Position = new Vector2(W * Tiles.Size + 8, 6 * Tiles.Size),
			Message = "East door open. Checkpoint 2 complete — rooms 4–10 later.",
			RequiresFanOpen = true,
			TriggerSize = new Vector2(14, 28)
		});
	}

	private void PlaceAsh(Node2D root, string id, int x, int y)
	{
		root.AddChild(new AshPile
		{
			AshId = id,
			Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f)
		});
	}

	private static void FillBrickRoom(Node2D root, int w, int h)
	{
		for (int y = 1; y < h - 1; y++)
			for (int x = 1; x < w - 1; x++)
				PlaceColdFloor(root, x, y);
		AddBorderWalls(root, w, h);
	}

	private static void PlaceColdFloor(Node2D root, int x, int y)
	{
		var s = Assets.ColdStackFloorSprite();
		s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
		root.AddChild(s);
	}

	private static void PlaceCracked(Node2D root, int x, int y)
	{
		var s = Assets.ColdStackSprite("cracked_brick");
		s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
		s.ZIndex = 1;
		root.AddChild(s);
	}

	private static void PlaceFloor(Node2D root, string tile, int x, int y)
	{
		var s = Assets.TileSprite(tile);
		s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
		root.AddChild(s);
	}

	private static void AddBorderWalls(Node2D root, int w, int h)
	{
		for (int x = 0; x < w; x++)
		{
			AddWall(root, x, 0);
			AddWall(root, x, h - 1);
		}
		for (int y = 1; y < h - 1; y++)
		{
			AddWall(root, 0, y);
			AddWall(root, w - 1, y);
		}
	}

	private static void AddWall(Node2D root, int x, int y)
	{
		var body = new StaticBody2D
		{
			Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f),
			CollisionLayer = 1 << 0,
			Name = $"Wall_{x}_{y}"
		};
		body.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Tiles.Size, Tiles.Size) }
		});
		body.AddChild(Assets.ColdStackSprite("flue_wall"));
		root.AddChild(body);
	}

	private static void ClearWallAt(Node2D root, int x, int y)
	{
		root.GetNodeOrNull($"Wall_{x}_{y}")?.QueueFree();
	}

	private static void PlaceTile(Node2D root, string tile, int x, int y)
	{
		var s = Assets.TileSprite(tile);
		s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
		root.AddChild(s);
	}

	private static void AddRoomTitle(Node2D root, string text)
	{
		var title = new Label
		{
			Text = text,
			Position = new Vector2(8, 2),
			Size = new Vector2(220, 12)
		};
		title.AddThemeFontSizeOverride("font_size", 10);
		title.AddThemeColorOverride("font_color", Palette.UiText);
		root.AddChild(title);
	}

	private static void AddNpc(Node2D root, Vector2 pos, string name, Color color)
	{
		root.AddChild(new NpcInteractable { Position = pos, NpcName = name, NpcColor = color });
	}
}
