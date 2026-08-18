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
		// Exact SLICE-0.md quest lines (primary). Short follow-ups after flags.
		return NpcName switch
		{
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
				// Rook alone opens the mouth, and only after the hire.
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
	public override void _Ready()
	{
		base._Ready();
		AddChild(Assets.TileSprite("night_fire"));
		Prompt = "Save";
	}

	public override void Interact(PlayerController player)
	{
		GameState.Instance.RecordSave(RoomId.Kilnwalk, player.GlobalPosition);
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
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_cooldown || body is not PlayerController)
			return;
		if (RequiresMouthOpen && !GameState.Instance.MouthOpen)
		{
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Mouth sealed. Take the hire.");
			return;
		}
		// Checkpoint 1: town only — mouth may open, but dungeon is not in this PR.
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast(
			"Cold Stack sealed for Checkpoint 1. Town QA only.");
		_cooldown = true;
		GetTree().CreateTimer(0.6f).Timeout += () => _cooldown = false;
	}
}
