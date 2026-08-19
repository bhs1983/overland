using Godot;

namespace Overland;

public partial class Interactable : Area2D
{
	[Export] public string Prompt { get; set; } = "Talk";

	public override void _Ready()
	{
		CollisionLayer = 1 << 5;
		CollisionMask = 0;
		Monitoring = false;
		Monitorable = true;
		if (GetChildCount() == 0)
			AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 12 } });
	}

	public virtual void Interact(PlayerController player) { }
}

public partial class NpcInteractable : Interactable
{
	public string NpcName { get; set; } = "";
	public Color NpcColor { get; set; } = Colors.Gray;

	public override void _Ready()
	{
		base._Ready();
		var body = PixelSprite.MakeBody(new Vector2(12, 14), NpcColor);
		AddChild(body);
		var label = new Label
		{
			Text = NpcName.Split(' ')[0],
			Position = new Vector2(-20, -22),
			Size = new Vector2(40, 12)
		};
		label.AddThemeColorOverride("font_color", Palette.UiText);
		label.AddThemeFontSizeOverride("font_size", 8);
		AddChild(label);
	}

	public override void Interact(PlayerController player)
	{
		var ui = GetTree().GetFirstNodeInGroup("game_ui") as GameUi;
		ui?.ShowDialogue(NpcName, GetLine());
		ApplyEffect();
	}

	private string GetLine()
	{
		var gs = GameState.Instance;
		return NpcName switch
		{
			"Tamsin Cole" when gs.HirePaid =>
				"Stack’s still. You earned the coin.",
			"Tamsin Cole" when gs.OverfireDown =>
				"You shut it. Coin as promised. Hire’s closed.",
			"Tamsin Cole" when gs.HireTaken =>
				"Draft’s running the wrong way. Walk the Cold Stack. Shut what you find. Coin when you come back up.",
			"Tamsin Cole" =>
				"Draft’s running the wrong way. Walk the Cold Stack. Shut what you find. Coin when you come back up.",
			"Holt Vetch" when gs.HasCrackiron =>
				"Take Crackiron. It splits cooled clay. It’ll split what’s down there too.",
			"Holt Vetch" =>
				"Take Crackiron. It splits cooled clay. It’ll split what’s down there too.",
			"Wren Quill" when gs.HireTaken =>
				"I marked the mouth. Don’t linger in the long heat.",
			"Wren Quill" =>
				"Night fire’s for saving your place. Take the hire from Tamsin first.",
			"Rook Darnel" when gs.MouthOpen || gs.HireTaken =>
				"Mouth’s open. I lock it behind you if the air turns.",
			"Rook Darnel" =>
				"No hire, no mouth. Talk to Tamsin.",
			_ => "..."
		};
	}

	private void ApplyEffect()
	{
		var gs = GameState.Instance;
		switch (NpcName)
		{
			case "Tamsin Cole":
				if (gs.OverfireDown)
				{
					gs.HirePaid = true;
					gs.SliceComplete = true;
				}
				else
					gs.HireTaken = true;
				break;
			case "Holt Vetch":
				gs.HasCrackiron = true;
				break;
			case "Wren Quill":
				if (gs.HireTaken)
					gs.MapMarked = true;
				break;
			case "Rook Darnel":
				if (gs.HireTaken)
					gs.MouthOpen = true;
				break;
		}
		GetTree().CallGroup("world", "RefreshGates");
		GetTree().CallGroup("game_ui", "RefreshHud");
	}
}

public partial class SavePoint : Interactable
{
	public RoomId SaveRoom { get; set; } = RoomId.Kilnwalk;
	public bool NightFire { get; set; } = true;

	public override void _Ready()
	{
		base._Ready();
		AddChild(NightFire ? Assets.TileSprite("night_fire") : Assets.Sprite(Assets.Ui("save_mark")));
		Prompt = "Save";
	}

	public override void Interact(PlayerController player)
	{
		GameState.Instance.RecordSave(SaveRoom, player.GlobalPosition);
		SaveSystem.Instance.Save();
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Saved.");
	}
}

public partial class MouthGate : StaticBody2D
{
	private CollisionShape2D _col = null!;
	private Sprite2D _sprite = null!;

	public override void _Ready()
	{
		CollisionLayer = 1 << 0;
		_col = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(32, 16) }
		};
		AddChild(_col);
		_sprite = Assets.TileSprite("stack_mouth_sealed");
		_sprite.Scale = new Vector2(2, 1);
		AddChild(_sprite);
		Refresh();
	}

	public void Refresh()
	{
		var open = GameState.Instance.MouthOpen;
		_sprite.Texture = Assets.Town(open ? "stack_mouth_open" : "stack_mouth_sealed");
		_col.Disabled = open;
		CollisionLayer = open ? 0u : 1u;
	}
}

public partial class RoomTransition : Area2D
{
	public RoomId Target { get; set; }
	public string SpawnId { get; set; } = "default";
	public bool RequiresMouthOpen { get; set; }
	public bool RequiresFanOpen { get; set; }
	public bool RequiresClinkerDown { get; set; }
	public bool RequiresIronOpen { get; set; }
	public Vector2 TriggerSize { get; set; } = new(20, 12);

	private bool _cooldown;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1 << 1;
		Monitoring = true;
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = TriggerSize }
		});
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_cooldown || body is not PlayerController)
			return;
		if (GetTree().GetFirstNodeInGroup("world") is WorldRoot world && !world.TransitionsReady)
			return;
		if (RequiresMouthOpen && !GameState.Instance.MouthOpen)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Mouth sealed. Take the hire.");
			return;
		}
		if (RequiresFanOpen && !GameState.Instance.FanOpened)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Dead fan blocks the east door. Puff the Folded Bellows.");
			return;
		}
		if (RequiresClinkerDown && !GameState.Instance.ClinkerDown)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("The Clinker still blocks the way north.");
			return;
		}
		if (RequiresIronOpen && !GameState.Instance.IronDoorOpen)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Iron door sealed. Needs the Stack Key.");
			return;
		}
		_cooldown = true;
		// Defer past physics query flush — inline GoToRoom during BodyEntered
		// breaks Area2D setup (e.g. Sootling hurtbox) with "Can't change this state while flushing queries".
		(GetTree().GetFirstNodeInGroup("world") as WorldRoot)?.CallDeferred(
			nameof(WorldRoot.GoToRoom), (int)Target, SpawnId);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is PlayerController)
			_cooldown = false;
	}
}

/// <summary>Ashdrift Hall ash pile — bellows pushes it off the tile.</summary>
public partial class AshPile : StaticBody2D, IBellowsTarget
{
	public string AshId { get; set; } = "ash_0";

	public override void _Ready()
	{
		if (GameState.Instance.ClearedAsh.Contains(AshId))
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 0;
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(14, 14) }
		});
		AddChild(Assets.ColdStackSprite("ash_pile"));
	}

	public void OnBellows(Vector2 fromDirection)
	{
		GameState.Instance.ClearedAsh.Add(AshId);
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Ash pushed aside.");
		QueueFree();
	}
}

/// <summary>Chest in Ashdrift Hall — Folded Bellows (chest art + folded_bellows item).</summary>
public partial class BellowsChest : Interactable
{
	public override void _Ready()
	{
		base._Ready();
		Prompt = "Open";
		var spr = Assets.ColdStackSprite("chest");
		AddChild(spr);
		if (GameState.Instance.BellowsChestOpened)
			spr.Modulate = new Color(1, 1, 1, 0.45f);
	}

	public override void Interact(PlayerController player)
	{
		var gs = GameState.Instance;
		if (gs.BellowsChestOpened)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Chest empty.");
			return;
		}
		gs.BellowsChestOpened = true;
		gs.HasFoldedBellows = true;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Folded Bellows — puff with K / X.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
		if (GetChildCount() > 1 && GetChild(1) is CanvasItem c)
			c.Modulate = new Color(1, 1, 1, 0.45f);
	}
}

/// <summary>Dead Fan Walk tool gate — puff bellows into the fan.</summary>
public partial class DeadFan : StaticBody2D, IBellowsTarget
{
	private Sprite2D _sprite = null!;

	public override void _Ready()
	{
		CollisionLayer = 1 << 0;
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(16, 16) }
		});
		_sprite = Assets.ColdStackSprite("dead_fan");
		AddChild(_sprite);
		var label = new Label
		{
			Text = "Dead Fan",
			Position = new Vector2(-18, -18),
			Size = new Vector2(48, 12)
		};
		label.AddThemeFontSizeOverride("font_size", 8);
		label.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(label);
		RefreshLook();
	}

	public void OnBellows(Vector2 fromDirection)
	{
		if (GameState.Instance.FanOpened)
			return;
		GameState.Instance.FanOpened = true;
		RefreshLook();
		GetTree().CallGroup("world", "RefreshGates");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Dead fan turns. East door opens.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	private void RefreshLook()
	{
		_sprite.Modulate = GameState.Instance.FanOpened
			? new Color(Palette.ColdDraftLight)
			: Colors.White;
	}
}

/// <summary>East door blocker in Dead Fan Walk — opens when FanOpened.</summary>
public partial class FanEastDoor : StaticBody2D
{
	private CollisionShape2D _col = null!;
	private Sprite2D _sprite = null!;

	public override void _Ready()
	{
		CollisionLayer = 1 << 0;
		_col = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(24, 40) }
		};
		AddChild(_col);
		_sprite = Assets.ColdStackSprite("iron_door_closed");
		AddChild(_sprite);
		AddToGroup("fan_east_door");
		Refresh();
	}

	public void Refresh()
	{
		var open = GameState.Instance.FanOpened;
		_col.Disabled = open;
		CollisionLayer = open ? 0u : 1u;
		_sprite.Texture = Assets.ColdStack(open ? "iron_door_open" : "iron_door_closed");
		_sprite.Modulate = Colors.White;
	}
}

/// <summary>Toast-only zone (e.g. CP3 end — no rooms 9–10).</summary>
public partial class CheckpointToastZone : Area2D
{
	public string Message { get; set; } = "";
	public bool RequiresFanOpen { get; set; }
	public bool RequiresIronOpen { get; set; }
	public Vector2 TriggerSize { get; set; } = new(16, 16);
	private bool _cooldown;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1 << 1;
		Monitoring = true;
		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = TriggerSize }
		});
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_cooldown || body is not PlayerController)
			return;
		if (RequiresFanOpen && !GameState.Instance.FanOpened)
			return;
		if (RequiresIronOpen && !GameState.Instance.IronDoorOpen)
			return;
		_cooldown = true;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast(Message);
		GetTree().CreateTimer(1.2f).Timeout += () => _cooldown = false;
	}
}

/// <summary>Setter's Alcove optional heal.</summary>
public partial class AlcoveHeal : Interactable
{
	public override void _Ready()
	{
		base._Ready();
		Prompt = "Rest";
		var s = Assets.ColdStackSprite("ash_pile");
		s.Modulate = Palette.ColdDraftLight;
		AddChild(s);
	}

	public override void Interact(PlayerController player)
	{
		if (GameState.Instance.AlcoveHealTaken)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Cool ash. Nothing left.");
			return;
		}
		GameState.Instance.AlcoveHealTaken = true;
		GameState.Instance.Heal(2);
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Warm ash. +2.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}
}

/// <summary>Key Landing — Stack Key after Clinker.</summary>
public partial class StackKeyPickup : Interactable
{
	public override void _Ready()
	{
		base._Ready();
		Prompt = "Take";
		AddChild(Assets.Sprite(Assets.Item("stack_key")));
		if (GameState.Instance.StackKeyTaken)
			Visible = false;
	}

	public override void Interact(PlayerController player)
	{
		if (GameState.Instance.StackKeyTaken)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Already have the Stack Key.");
			return;
		}
		if (!GameState.Instance.ClinkerDown)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Not yet. The Clinker still stands.");
			return;
		}
		GameState.Instance.StackKeyTaken = true;
		GameState.Instance.HasStackKey = true;
		Visible = false;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Stack Key — opens the Sealed Flue.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}
}

/// <summary>Sealed Flue iron door — Stack Key gate. No rooms 9–10 in CP3.</summary>
public partial class IronDoorGate : StaticBody2D
{
	private CollisionShape2D _col = null!;
	private Sprite2D _sprite = null!;

	public override void _Ready()
	{
		CollisionLayer = 1 << 0;
		_col = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(24, 48) }
		};
		AddChild(_col);
		_sprite = Assets.ColdStackSprite("iron_door_closed");
		AddChild(_sprite);
		AddToGroup("iron_door");
		Refresh();
	}

	public void Refresh()
	{
		var open = GameState.Instance.IronDoorOpen;
		_col.Disabled = open;
		CollisionLayer = open ? 0u : 1u;
		_sprite.Texture = Assets.ColdStack(open ? "iron_door_open" : "iron_door_closed");
	}
}

/// <summary>Interact to unlock Sealed Flue with Stack Key.</summary>
public partial class IronDoorUnlock : Interactable
{
	public override void _Ready()
	{
		base._Ready();
		Prompt = "Unlock";
	}

	public override void Interact(PlayerController player)
	{
		if (GameState.Instance.IronDoorOpen)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Iron door open.");
			return;
		}
		if (!GameState.Instance.HasStackKey)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Needs the Stack Key.");
			return;
		}
		GameState.Instance.IronDoorOpen = true;
		GetTree().CallGroup("world", "RefreshGates");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Iron door opens. Long Drop ahead.");
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}
}

/// <summary>Quench water visual + slight slow (cosmetic channel).</summary>
public partial class QuenchWaterTile : Sprite2D
{
	public override void _Ready()
	{
		Texture = Assets.ColdStack("quench_water");
		TextureFilter = TextureFilterEnum.Nearest;
		Centered = true;
	}
}

/// <summary>After Overfire — short stair back to Kilnwalk.</summary>
public partial class StairHome : Interactable
{
	public override void _Ready()
	{
		base._Ready();
		Prompt = "Climb";
		AddChild(Assets.ColdStackSprite("ledge"));
		var label = new Label
		{
			Text = "Stair",
			Position = new Vector2(-16, -20),
			Size = new Vector2(40, 12)
		};
		label.AddThemeFontSizeOverride("font_size", 8);
		label.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(label);
	}

	public override void Interact(PlayerController player)
	{
		if (!GameState.Instance.OverfireDown)
			return;
		(GetTree().GetFirstNodeInGroup("world") as WorldRoot)?.GoToRoom(RoomId.Kilnwalk, "from_boss");
	}
}
