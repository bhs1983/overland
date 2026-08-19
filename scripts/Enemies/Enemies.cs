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
			Shape = new CircleShape2D { Radius = 6 }
		});
		_body = Assets.Sprite(Assets.EnemyV3OrNull("sootling") ?? Assets.Enemy("sootling"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
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
				GlobalPosition = player.GlobalPosition + facing.Normalized() * 12f;
			MoveAndSlide();
			// Delayed contact bite — skip while the hero is swinging/puffing.
			if (_hitCd <= 0 && !player.AttackBusy)
			{
				_hitCd = 0.85f;
				player.ApplyHit((player.GlobalPosition - GlobalPosition).Normalized());
			}
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
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 8 } });
		_body = Assets.Sprite(Assets.EnemyV3("claywalker"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
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
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 6 } });
		_body = Assets.Sprite(Assets.EnemyV3("brickleech"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
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
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 12 } });
		_body = Assets.Sprite(Assets.EnemyV3("clinker"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
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

/// <summary>The Overfire — walking kiln. Heat pulse (step out), swipe (hit after). Bellows shoves pulse ash.</summary>
public partial class Overfire : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "overfire_chamber";
	public bool IsAlive => _alive;

	private enum Phase { Idle, PulseTele, Pulse, SwipeTele, Swipe }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private int _hp = 8;
	private Phase _phase = Phase.Idle;
	private float _phaseT;
	private bool _ashShoved;
	private float _hitCd = 0.8f;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId) || GameState.Instance.OverfireDown)
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 14 } });
		_body = Assets.Sprite(Assets.EnemyV3("overfire"));
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
		var label = new Label
		{
			Text = "the Overfire",
			Position = new Vector2(-32, -44),
			Size = new Vector2(80, 12)
		};
		label.AddThemeFontSizeOverride("font_size", 9);
		label.AddThemeColorOverride("font_color", Palette.OverfireHot);
		AddChild(label);
		Enter(Phase.Idle);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}

		var dt = (float)delta;
		_phaseT -= dt;
		_hitCd -= dt;
		var player = PlayerController.Instance;
		if (player == null)
			return;

		var to = player.GlobalPosition - GlobalPosition;
		var dist = to.Length();

		switch (_phase)
		{
			case Phase.Idle:
				if (dist > 28f)
					Velocity = to.Normalized() * 20f;
				else
					Velocity = Vector2.Zero;
				MoveAndSlide();
				if (_phaseT <= 0)
					Enter(Phase.PulseTele);
				break;

			case Phase.PulseTele:
				Velocity = Vector2.Zero;
				MoveAndSlide();
				if (_phaseT <= 0)
					Enter(Phase.Pulse);
				break;

			case Phase.Pulse:
				Velocity = Vector2.Zero;
				MoveAndSlide();
				if (_phaseT <= 0)
					Enter(Phase.SwipeTele);
				break;

			case Phase.SwipeTele:
				Velocity = Vector2.Zero;
				MoveAndSlide();
				if (_phaseT <= 0)
					Enter(Phase.Swipe);
				break;

			case Phase.Swipe:
				Velocity = Vector2.Zero;
				MoveAndSlide();
				if (_phaseT <= 0)
					Enter(Phase.Idle);
				break;
		}
	}

	private void Enter(Phase next)
	{
		_phase = next;
		var player = PlayerController.Instance;
		var ui = GetTree().GetFirstNodeInGroup("game_ui") as GameUi;
		switch (next)
		{
			case Phase.Idle:
				_phaseT = 2.0f;
				_ashShoved = false;
				SetSprite("overfire");
				_body.Modulate = Colors.White;
				break;
			case Phase.PulseTele:
				_phaseT = 0.75f;
				SetSprite("overfire_pulse");
				_body.Modulate = Palette.OverfireHot;
				ui?.ShowToast("Heat pulse — step out.");
				break;
			case Phase.Pulse:
				_phaseT = 0.28f;
				SetSprite("overfire_pulse");
				if (player != null)
				{
					var d = player.GlobalPosition.DistanceTo(GlobalPosition);
					if (_ashShoved)
						ui?.ShowToast("Ash shoved. Pulse misses.");
					else if (d < 52f)
						player.ApplyHit((player.GlobalPosition - GlobalPosition).Normalized(), 1);
				}
				break;
			case Phase.SwipeTele:
				_phaseT = 0.4f;
				SetSprite("overfire_swipe");
				_body.Modulate = Colors.White;
				ui?.ShowToast("Swipe — hit after.");
				break;
			case Phase.Swipe:
				_phaseT = 0.22f;
				SetSprite("overfire_swipe");
				if (player != null && player.GlobalPosition.DistanceTo(GlobalPosition) < 30f && _hitCd <= 0)
				{
					_hitCd = 0.6f;
					player.ApplyHit((player.GlobalPosition - GlobalPosition).Normalized(), 1);
				}
				break;
		}
	}

	private void SetSprite(string name)
	{
		var tex = Assets.EnemyV3(name);
		if (tex != null)
			_body.Texture = tex;
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		_hp -= damage;
		_flash.Flash(_body, Palette.HitFlash, 0.1f);
		Hitstop.Pulse(this, 0.05f);
		if (_hp <= 0)
		{
			_alive = false;
			GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
			GameState.Instance.OverfireDown = true;
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Overfire falls. Stair back to Kilnwalk.");
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
			GetParent()?.AddChild(new StairHome
			{
				Position = new Vector2(10 * Tiles.Size, 3.2f * Tiles.Size)
			});
			GetTree().CreateTimer(0.2f).Timeout += () => QueueFree();
		}
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		if (_phase is Phase.PulseTele or Phase.Pulse)
		{
			_ashShoved = true;
			_flash.Flash(_body, Palette.BellowsPuff, 0.12f);
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Ash shoved aside.");
		}
		else
		{
			_flash.Flash(_body, Palette.BellowsPuff, 0.08f);
		}
	}
}
