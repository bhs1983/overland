using Godot;

namespace Overland;

/// <summary>Checkpoint 1 — Kilnwalk town only.</summary>
public partial class WorldRoot : Node2D
{
	private Node2D _roomLayer = null!;
	private PlayerController _player = null!;
	private MouthGate? _mouthGate;

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

		BuildKilnwalk();
		var spawn = GameState.Instance.LastSavePosition;
		if (spawn == Vector2.Zero)
			spawn = new Vector2(10 * Tiles.Size, 11 * Tiles.Size);
		_player.GlobalPosition = spawn;
		GameState.Instance.MarkRoomEntered(RoomId.Kilnwalk);
	}

	public void RespawnAtSave()
	{
		GameState.Instance.InputLocked = false;
		_player.GlobalPosition = GameState.Instance.LastSavePosition;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Woke at night fire.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	public void RefreshGates()
	{
		_mouthGate?.Refresh();
	}

	private void BuildKilnwalk()
	{
		foreach (var c in _roomLayer.GetChildren())
			c.QueueFree();

		var root = new Node2D { Name = "Kilnwalk" };
		_roomLayer.AddChild(root);

		const int W = 20;
		const int H = 15;

		// Floor: street / brick_floor mix (ridge street + kiln yard)
		for (int y = 1; y < H - 1; y++)
		{
			for (int x = 1; x < W - 1; x++)
			{
				var tile = (y < 6) ? "brick_floor" : "street";
				if (x >= 7 && x <= 12 && y >= 3 && y <= 7)
					tile = "brick_floor"; // kiln yard
				var s = Assets.TileSprite(tile);
				s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
				root.AddChild(s);
			}
		}

		// Walls
		for (int x = 0; x < W; x++)
		{
			AddWall(root, x, 0);
			AddWall(root, x, H - 1);
		}
		for (int y = 1; y < H - 1; y++)
		{
			AddWall(root, 0, y);
			AddWall(root, W - 1, y);
		}

		// Mouth gap at north
		ClearWallAt(root, 9, 0);
		ClearWallAt(root, 10, 0);

		var title = new Label
		{
			Text = "Kilnwalk",
			Position = new Vector2(8, 2),
			Size = new Vector2(200, 12)
		};
		title.AddThemeFontSizeOverride("font_size", 10);
		title.AddThemeColorOverride("font_color", Palette.UiText);
		root.AddChild(title);

		// Props — kiln yard
		PlaceTile(root, "kiln", 8, 4);
		PlaceTile(root, "kiln", 11, 4);
		PlaceTile(root, "door", 5, 2);

		// NPCs
		AddNpc(root, new Vector2(4 * Tiles.Size, 5 * Tiles.Size), "Tamsin Cole", Palette.NpcTamsin);
		AddNpc(root, new Vector2(4 * Tiles.Size, 9 * Tiles.Size), "Holt Vetch", Palette.NpcHolt);
		AddNpc(root, new Vector2(10 * Tiles.Size, 8 * Tiles.Size), "Wren Quill", Palette.NpcWren);
		AddNpc(root, new Vector2(14 * Tiles.Size, 4 * Tiles.Size), "Rook Darnel", Palette.NpcRook);

		// Night fire save (by Wren)
		var save = new SavePoint { Position = new Vector2(10 * Tiles.Size, 9.5f * Tiles.Size) };
		root.AddChild(save);
		GameState.Instance.LastSavePosition = new Vector2(10 * Tiles.Size, 11 * Tiles.Size);

		// Sealed stack mouth
		_mouthGate = new MouthGate
		{
			Position = new Vector2(10 * Tiles.Size, 1.5f * Tiles.Size)
		};
		root.AddChild(_mouthGate);

		// Transition hint (blocked for CP1)
		var door = new RoomTransition
		{
			Position = new Vector2(10 * Tiles.Size, 0.5f * Tiles.Size),
			Target = RoomId.StackMouth,
			RequiresMouthOpen = true,
			TriggerSize = new Vector2(28, 12)
		};
		root.AddChild(door);
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
		body.AddChild(Assets.TileSprite("brick_wall"));
		root.AddChild(body);
	}

	private static void ClearWallAt(Node2D root, int x, int y)
	{
		var name = $"Wall_{x}_{y}";
		var node = root.GetNodeOrNull(name);
		node?.QueueFree();
	}

	private static void PlaceTile(Node2D root, string tile, int x, int y)
	{
		var s = Assets.TileSprite(tile);
		s.Position = new Vector2(x * Tiles.Size + Tiles.Size / 2f, y * Tiles.Size + Tiles.Size / 2f);
		root.AddChild(s);
	}

	private static void AddNpc(Node2D root, Vector2 pos, string name, Color color)
	{
		root.AddChild(new NpcInteractable { Position = pos, NpcName = name, NpcColor = color });
	}
}
