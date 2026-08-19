using Godot;

namespace Overland;

/// <summary>Sootling — 1 hit, rushes, bellows staggers.</summary>
public partial class Sootling : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "sootling_0";
	public bool IsAlive => _alive;

	private CanvasItem _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _staggered;
	private float _staggerT;
	private float _hitCd;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}

		CollisionLayer = 1 << 2;
		CollisionMask = 0;
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 2 }
		});
		_body = Assets.Sprite(Assets.Enemy("sootling"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		_hitCd = 0.55f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}

		var dt = (float)delta;
		if (_hitCd > 0)
			_hitCd -= dt;
		if (_staggered)
		{
			_staggerT -= dt;
			Velocity = Vector2.Zero;
			if (_staggerT <= 0)
				_staggered = false;
			MoveAndSlide();
			return;
		}

		var player = PlayerController.Instance;
		if (player == null)
			return;

		var to = player.GlobalPosition - GlobalPosition;
		var distSq = to.LengthSquared();
		if (distSq < 16f * 16f)
		{
			Velocity = Vector2.Zero;
			var facing = player.Facing;
			if (facing.LengthSquared() > 0.0001f)
				GlobalPosition = player.GlobalPosition + facing.Normalized() * 8f;
			MoveAndSlide();
			if (GameState.Instance.HasCrackiron && Input.IsActionJustPressed("attack"))
				TakeSwordHit(player.Facing);
			return;
		}

		Velocity = to.Normalized() * 55f;
		MoveAndSlide();
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		_alive = false;
		GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
		_flash.Flash(_body, Palette.HitFlash, 0.08f);
		Hitstop.Pulse(this, 0.04f);
		GetTree().CreateTimer(0.08f).Timeout += () => QueueFree();
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_staggered = true;
		_staggerT = 0.45f;
		_flash.Flash(_body, Palette.BellowsPuff, 0.12f);
		GlobalPosition += fromDirection.Normalized() * 10f;
	}
}

/// <summary>Claywalker — crust soaks one sword hit until bellows softens.</summary>
public partial class Claywalker : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "claywalker_0";
	public bool IsAlive => _alive;

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _softened;
	private float _hitCd = 0.6f;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 6 } });
		_body = Assets.Sprite(Assets.EnemyV3("claywalker"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}
		_hitCd -= (float)delta;
		var player = PlayerController.Instance;
		if (player == null)
			return;
		var to = player.GlobalPosition - GlobalPosition;
		if (to.LengthSquared() < 18f * 18f)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			var busy = player.AttackBusy
				|| Input.IsActionJustPressed("attack")
				|| Input.IsActionJustPressed("bellows");
			if (_hitCd <= 0 && !busy)
			{
				_hitCd = 0.9f;
				player.ApplyHit(to.Normalized());
			}
			return;
		}
		Velocity = to.Normalized() * 28f;
		MoveAndSlide();
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		if (!_softened)
		{
			_flash.Flash(_body, Palette.Claywalker, 0.1f);
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Crust holds. Soften with Folded Bellows.");
			return;
		}
		_alive = false;
		GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
		_flash.Flash(_body, Palette.HitFlash, 0.08f);
		Hitstop.Pulse(this, 0.05f);
		GetTree().CreateTimer(0.1f).Timeout += () => QueueFree();
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_softened = true;
		_body.Modulate = Palette.ClaywalkerSoft;
		_flash.Flash(_body, Palette.BellowsPuff, 0.12f);
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Crust softened.");
	}
}

/// <summary>Brickleech — wall cling, drops when you pass, 1–2 hits.</summary>
public partial class Brickleech : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "brickleech_0";
	public bool IsAlive => _alive;
	public Vector2 ClingPos { get; set; }
	public float DropTriggerX { get; set; }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _dropped;
	private int _hp = 2;
	private float _hitCd = 0.5f;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 5 } });
		_body = Assets.Sprite(Assets.EnemyV3("brickleech"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		if (ClingPos != Vector2.Zero)
			GlobalPosition = ClingPos;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}
		var player = PlayerController.Instance;
		if (player == null)
			return;

		if (!_dropped)
		{
			if (Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X) < 18f
			    && player.GlobalPosition.Y > GlobalPosition.Y - 8f)
			{
				_dropped = true;
				(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Brickleech drops.");
			}
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		_hitCd -= (float)delta;
		var to = player.GlobalPosition - GlobalPosition;
		if (to.LengthSquared() < 14f * 14f)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (_hitCd <= 0)
			{
				_hitCd = 0.7f;
				player.ApplyHit(to.Normalized());
			}
			return;
		}
		Velocity = to.Normalized() * 48f;
		MoveAndSlide();
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		_hp -= damage;
		_flash.Flash(_body, Palette.HitFlash, 0.08f);
		Hitstop.Pulse(this, 0.04f);
		if (_hp <= 0)
		{
			_alive = false;
			GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
			GetTree().CreateTimer(0.08f).Timeout += () => QueueFree();
		}
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_dropped = true;
		GlobalPosition += fromDirection.Normalized() * 8f;
		_flash.Flash(_body, Palette.BellowsPuff, 0.1f);
	}
}

/// <summary>Clinker miniboss — bellows opens cracks; Crackiron hits the cracks.</summary>
public partial class Clinker : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "clinker_yard";
	public bool IsAlive => _alive;

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _cracked;
	private int _hp = 4;
	private float _hitCd = 0.8f;
	private float _crackTimer;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId) || GameState.Instance.ClinkerDown)
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10 } });
		_body = Assets.Sprite(Assets.EnemyV3("clinker"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		var label = new Label
		{
			Text = "the Clinker",
			Position = new Vector2(-28, -36),
			Size = new Vector2(80, 12)
		};
		label.AddThemeFontSizeOverride("font_size", 9);
		label.AddThemeColorOverride("font_color", Palette.ClinkerCrack);
		AddChild(label);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}
		var dt = (float)delta;
		_hitCd -= dt;
		if (_cracked)
		{
			_crackTimer -= dt;
			if (_crackTimer <= 0)
			{
				_cracked = false;
				_body.Modulate = Colors.White;
			}
		}
		var player = PlayerController.Instance;
		if (player == null)
			return;
		var to = player.GlobalPosition - GlobalPosition;
		if (to.LengthSquared() < 22f * 22f)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (_hitCd <= 0)
			{
				_hitCd = 1.1f;
				player.ApplyHit(to.Normalized(), 1);
			}
			return;
		}
		Velocity = to.Normalized() * 22f;
		MoveAndSlide();
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		if (!_cracked)
		{
			_flash.Flash(_body, Palette.Clinker, 0.1f);
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Armored slag. Puff bellows to open cracks.");
			return;
		}
		_hp -= damage;
		_flash.Flash(_body, Palette.HitFlash, 0.1f);
		Hitstop.Pulse(this, 0.06f);
		if (_hp <= 0)
		{
			_alive = false;
			GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
			GameState.Instance.ClinkerDown = true;
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Clinker falls. Key Landing opens.");
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
			GetTree().CreateTimer(0.15f).Timeout += () => QueueFree();
		}
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_cracked = true;
		_crackTimer = 2.2f;
		_body.Modulate = Palette.ClinkerCrack;
		_flash.Flash(_body, Palette.BellowsPuff, 0.12f);
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Cracks open. Hit with Crackiron.");
	}
}
