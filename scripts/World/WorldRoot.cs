using Godot;

namespace Overland;

/// <summary>Checkpoint 3 — Kilnwalk + Cold Stack rooms 1–8. Rooms 9–10 later.</summary>
public partial class WorldRoot : Node2D
{
	private Node2D _roomLayer = null!;
	private PlayerController _player = null!;
	private SliceParallax _parallax = null!;
	private MouthGate? _mouthGate;
	private FanEastDoor? _fanEastDoor;
	private bool _transitioning;

	public override void _Ready()
	{
		AddToGroup("world");

		_parallax = new SliceParallax { Name = "Parallax" };
		AddChild(_parallax);

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
		foreach (var n in GetTree().GetNodesInGroup("iron_door"))
		{
			if (n is IronDoorGate d)
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

		var cold = room != RoomId.Kilnwalk;
		_parallax.SetTheme(cold ? "cold_stack" : "kilnwalk");

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
			case RoomId.SettersAlcove:
				BuildSettersAlcove();
				break;
			case RoomId.QuenchTrench:
				BuildQuenchTrench();
				break;
			case RoomId.ClinkerYard:
				BuildClinkerYard();
				break;
			case RoomId.KeyLanding:
				BuildKeyLanding();
				break;
			case RoomId.SealedFlue:
				BuildSealedFlue();
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
		RoomId.DeadFanWalk when spawnId == "from_alcove" => new Vector2(15 * Tiles.Size, 6 * Tiles.Size),
		RoomId.DeadFanWalk when spawnId == "from_quench" => new Vector2(9 * Tiles.Size, 9 * Tiles.Size),
		RoomId.DeadFanWalk => new Vector2(4 * Tiles.Size, 7 * Tiles.Size),
		RoomId.SettersAlcove when spawnId == "from_fan" => new Vector2(2.5f * Tiles.Size, 7 * Tiles.Size),
		RoomId.SettersAlcove when spawnId == "from_quench" => new Vector2(10 * Tiles.Size, 11 * Tiles.Size),
		RoomId.SettersAlcove => new Vector2(4 * Tiles.Size, 7 * Tiles.Size),
		RoomId.QuenchTrench when spawnId == "from_alcove" => new Vector2(10 * Tiles.Size, 2.5f * Tiles.Size),
		RoomId.QuenchTrench when spawnId == "from_clinker" => new Vector2(17 * Tiles.Size, 7 * Tiles.Size),
		RoomId.QuenchTrench when spawnId == "from_fan" => new Vector2(2.5f * Tiles.Size, 7 * Tiles.Size),
		RoomId.QuenchTrench => new Vector2(10 * Tiles.Size, 7 * Tiles.Size),
		RoomId.ClinkerYard when spawnId == "from_quench" => new Vector2(2.5f * Tiles.Size, 7 * Tiles.Size),
		RoomId.ClinkerYard when spawnId == "from_key" => new Vector2(10 * Tiles.Size, 2.5f * Tiles.Size),
		RoomId.ClinkerYard => new Vector2(5 * Tiles.Size, 7 * Tiles.Size),
		RoomId.KeyLanding when spawnId == "from_clinker" => new Vector2(10 * Tiles.Size, 11 * Tiles.Size),
		RoomId.KeyLanding when spawnId == "from_sealed" => new Vector2(10 * Tiles.Size, 2.5f * Tiles.Size),
		RoomId.KeyLanding => new Vector2(10 * Tiles.Size, 8 * Tiles.Size),
		RoomId.SealedFlue when spawnId == "from_key" => new Vector2(10 * Tiles.Size, 11 * Tiles.Size),
		RoomId.SealedFlue => new Vector2(10 * Tiles.Size, 8 * Tiles.Size),
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

		AddBorderWalls(root, W, H, town: true);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);

		AddRoomTitle(root, "Kilnwalk");
		PlaceTile(root, "kiln", 8, 4);
		PlaceTile(root, "kiln", 11, 4);
		PlaceTile(root, "door", 5, 2);

		// Tall crowns on main 1.0× with −4..−8 Y.
		root.AddChild(SliceParallax.TallTop("kilnwalk", new Vector2(8 * Tiles.Size, 3.2f * Tiles.Size)));
		root.AddChild(SliceParallax.TallTop("kilnwalk", new Vector2(11 * Tiles.Size, 3.2f * Tiles.Size)));

		root.AddChild(SliceParallax.Cookie("kilnwalk", "light_kiln", new Vector2(8 * Tiles.Size, 4 * Tiles.Size), 1.2f, 3f));
		root.AddChild(SliceParallax.Cookie("kilnwalk", "light_kiln", new Vector2(11 * Tiles.Size, 4 * Tiles.Size), 1.2f, 3f));
		root.AddChild(SliceParallax.Cookie("kilnwalk", "light_lantern", new Vector2(10 * Tiles.Size, 9 * Tiles.Size), 0.9f, 2.2f));

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
		root.AddChild(SliceParallax.TallTop("cold_stack", new Vector2(4 * Tiles.Size, 1.5f * Tiles.Size)));
		root.AddChild(SliceParallax.Cookie("cold_stack", "light_overfire", new Vector2(10 * Tiles.Size, 5 * Tiles.Size), 0.7f, 4f));

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
			TriggerSize = new Vector2(28, 48)
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

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(4, 6 * Tiles.Size),
			Target = RoomId.AshdriftHall,
			SpawnId = "from_fan",
			TriggerSize = new Vector2(12, 28)
		});

		// East → Setter's Alcove (CP3) when fan open
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(W * Tiles.Size + 4, 6 * Tiles.Size),
			Target = RoomId.SettersAlcove,
			SpawnId = "from_fan",
			RequiresFanOpen = true,
			TriggerSize = new Vector2(14, 28)
		});

		// Side path hint toward Quench (via south stub — actual side path is from Quench west)
		ClearWallAt(root, 8, H - 1);
		ClearWallAt(root, 9, H - 1);
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(9 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.QuenchTrench,
			SpawnId = "from_fan",
			TriggerSize = new Vector2(28, 12)
		});
	}

	private void BuildSettersAlcove()
	{
		var root = new Node2D { Name = "SettersAlcove" };
		_roomLayer.AddChild(root);
		const int W = 16;
		const int H = 14;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 4, 4);
		PlaceCracked(root, 11, 5);
		PlaceCracked(root, 7, 9);
		ClearWallAt(root, 0, 6);
		ClearWallAt(root, 0, 7);
		ClearWallAt(root, 9, H - 1);
		ClearWallAt(root, 10, H - 1);
		AddRoomTitle(root, "Setter's Alcove");
		root.AddChild(SliceParallax.TallTop("cold_stack", new Vector2(12 * Tiles.Size, 1.6f * Tiles.Size)));

		root.AddChild(new Claywalker
		{
			Position = new Vector2(7 * Tiles.Size, 6 * Tiles.Size),
			EnemyId = "claywalker_alcove_a"
		});
		root.AddChild(new Claywalker
		{
			Position = new Vector2(11 * Tiles.Size, 8 * Tiles.Size),
			EnemyId = "claywalker_alcove_b"
		});

		root.AddChild(new AlcoveHeal
		{
			Position = new Vector2(13 * Tiles.Size, 4 * Tiles.Size)
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(4, 7 * Tiles.Size),
			Target = RoomId.DeadFanWalk,
			SpawnId = "from_alcove",
			TriggerSize = new Vector2(12, 28)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.QuenchTrench,
			SpawnId = "from_alcove",
			TriggerSize = new Vector2(28, 12)
		});
	}

	private void BuildQuenchTrench()
	{
		var root = new Node2D { Name = "QuenchTrench" };
		_roomLayer.AddChild(root);
		const int W = 20;
		const int H = 12;
		FillBrickRoom(root, W, H);
		// Water channel down the middle
		for (int x = 3; x < W - 3; x++)
		{
			var w = new QuenchWaterTile
			{
				Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, 6 * Tiles.Size)
			};
			root.AddChild(w);
		}
		PlaceCracked(root, 5, 3);
		PlaceCracked(root, 14, 8);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);
		ClearWallAt(root, W - 1, 6);
		ClearWallAt(root, W - 1, 7);
		ClearWallAt(root, 0, 6);
		ClearWallAt(root, 0, 7);
		AddRoomTitle(root, "Quench Trench");
		root.AddChild(SliceParallax.Cookie("cold_stack", "light_quench", new Vector2(10 * Tiles.Size, 6 * Tiles.Size), 1.0f, 3.2f));

		root.AddChild(new Brickleech
		{
			Position = new Vector2(6 * Tiles.Size, 2.5f * Tiles.Size),
			ClingPos = new Vector2(6 * Tiles.Size, 2.5f * Tiles.Size),
			EnemyId = "brickleech_quench_a"
		});
		root.AddChild(new Brickleech
		{
			Position = new Vector2(13 * Tiles.Size, 2.5f * Tiles.Size),
			ClingPos = new Vector2(13 * Tiles.Size, 2.5f * Tiles.Size),
			EnemyId = "brickleech_quench_b"
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 4),
			Target = RoomId.SettersAlcove,
			SpawnId = "from_quench",
			TriggerSize = new Vector2(28, 12)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2((W - 1) * Tiles.Size + 4, 7 * Tiles.Size),
			Target = RoomId.ClinkerYard,
			SpawnId = "from_quench",
			TriggerSize = new Vector2(12, 28)
		});
		// Side path back to Dead Fan Walk
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(4, 7 * Tiles.Size),
			Target = RoomId.DeadFanWalk,
			SpawnId = "from_quench",
			TriggerSize = new Vector2(12, 28)
		});
	}

	private void BuildClinkerYard()
	{
		var root = new Node2D { Name = "ClinkerYard" };
		_roomLayer.AddChild(root);
		const int W = 16;
		const int H = 14;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 4, 4);
		PlaceCracked(root, 11, 5);
		PlaceCracked(root, 8, 10);
		ClearWallAt(root, 0, 6);
		ClearWallAt(root, 0, 7);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);
		AddRoomTitle(root, "Clinker Yard");
		root.AddChild(SliceParallax.Cookie("cold_stack", "light_overfire", new Vector2(8 * Tiles.Size, 7 * Tiles.Size), 0.85f, 4.5f));
		root.AddChild(SliceParallax.TallTop("cold_stack", new Vector2(5 * Tiles.Size, 1.5f * Tiles.Size)));

		root.AddChild(new Clinker
		{
			Position = new Vector2(8 * Tiles.Size, 7 * Tiles.Size),
			EnemyId = "clinker_yard"
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(4, 7 * Tiles.Size),
			Target = RoomId.QuenchTrench,
			SpawnId = "from_clinker",
			TriggerSize = new Vector2(12, 28)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 4),
			Target = RoomId.KeyLanding,
			SpawnId = "from_clinker",
			RequiresClinkerDown = true,
			TriggerSize = new Vector2(28, 48)
		});
	}

	private void BuildKeyLanding()
	{
		var root = new Node2D { Name = "KeyLanding" };
		_roomLayer.AddChild(root);
		const int W = 14;
		const int H = 12;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 5, 5);
		PlaceCracked(root, 9, 7);
		ClearWallAt(root, 9, H - 1);
		ClearWallAt(root, 10, H - 1);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);
		AddRoomTitle(root, "Key Landing");

		// Ledge feel
		var ledge = Assets.ColdStackSprite("ledge");
		ledge.Position = new Vector2(10 * Tiles.Size, 5 * Tiles.Size);
		root.AddChild(ledge);

		root.AddChild(new StackKeyPickup
		{
			Position = new Vector2(10 * Tiles.Size, 4.5f * Tiles.Size)
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.ClinkerYard,
			SpawnId = "from_key",
			TriggerSize = new Vector2(28, 12)
		});
		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 4),
			Target = RoomId.SealedFlue,
			SpawnId = "from_key",
			TriggerSize = new Vector2(28, 48)
		});
	}

	private void BuildSealedFlue()
	{
		var root = new Node2D { Name = "SealedFlue" };
		_roomLayer.AddChild(root);
		const int W = 14;
		const int H = 12;
		FillBrickRoom(root, W, H);
		PlaceCracked(root, 4, 4);
		PlaceCracked(root, 10, 8);
		ClearWallAt(root, 9, H - 1);
		ClearWallAt(root, 10, H - 1);
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);
		AddRoomTitle(root, "Sealed Flue");
		root.AddChild(SliceParallax.TallTop("cold_stack", new Vector2(7 * Tiles.Size, 1.4f * Tiles.Size)));

		var door = new IronDoorGate
		{
			Position = new Vector2(10 * Tiles.Size, 3 * Tiles.Size)
		};
		root.AddChild(door);
		root.AddChild(new IronDoorUnlock
		{
			Position = new Vector2(10 * Tiles.Size, 4.2f * Tiles.Size)
		});

		root.AddChild(new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, (H - 1) * Tiles.Size + 4),
			Target = RoomId.KeyLanding,
			SpawnId = "from_sealed",
			TriggerSize = new Vector2(28, 12)
		});

		// No rooms 9–10 — toast only past the open iron door
		root.AddChild(new CheckpointToastZone
		{
			Position = new Vector2(10 * Tiles.Size, 1.2f * Tiles.Size),
			Message = "Iron door open. Checkpoint 3 complete — Long Drop / Overfire later.",
			RequiresIronOpen = true,
			TriggerSize = new Vector2(28, 14)
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
		AddBorderWalls(root, w, h, town: false);
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

	private static void AddBorderWalls(Node2D root, int w, int h, bool town)
	{
		for (int x = 0; x < w; x++)
		{
			AddWall(root, x, 0, town);
			AddWall(root, x, h - 1, town);
		}
		for (int y = 1; y < h - 1; y++)
		{
			AddWall(root, 0, y, town);
			AddWall(root, w - 1, y, town);
		}
	}

	private static void AddWall(Node2D root, int x, int y, bool town)
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
		body.AddChild(town ? Assets.TileSprite("brick_wall") : Assets.ColdStackSprite("flue_wall"));
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
