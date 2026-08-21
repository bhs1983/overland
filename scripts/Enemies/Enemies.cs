using Godot;

namespace Overland;

/// <summary>Shared steering. Enemies never teleport onto the hero.</summary>
internal static class EnemySteer
{
	public static Vector2 Chase(Vector2 from, Vector2 target, float speed, float stopAt)
	{
		var d = target - from;
		var dist = d.Length();
		if (dist <= stopAt || dist < 0.5f)
			return Vector2.Zero;
		return d * (speed / dist);
	}

	public static Vector2 Orbit(Vector2 from, Vector2 target, float desired, float speed, float sign)
	{
		var d = target - from;
		var dist = d.Length();
		if (dist < 0.5f)
			return new Vector2(sign, 0) * speed;
		var radial = d / dist;
		var tangent = new Vector2(-radial.Y, radial.X) * sign;
		var radialSpeed = Mathf.Clamp((dist - desired) * 3.2f, -speed, speed);
		return radial * radialSpeed + tangent * speed;
	}

	public static Vector2 Away(Vector2 from, Vector2 target, float speed)
	{
		var d = from - target;
		if (d.LengthSquared() < 1f)
			return new Vector2(speed, 0);
		return d.Normalized() * speed;
	}

	public static Vector2 Separate(Node2D self, float minDist)
	{
		var push = Vector2.Zero;
		var tree = self.GetTree();
		if (tree == null)
			return push;
		foreach (var n in tree.GetNodesInGroup("enemy"))
		{
			if (ReferenceEquals(n, self) || n is not Node2D other)
				continue;
			var d = self.GlobalPosition - other.GlobalPosition;
			var dist = d.Length();
			if (dist > 0.01f && dist < minDist)
				push += d / dist * (minDist - dist) * 4f;
		}
		return push;
	}

	public static float SignOf(string id) => (id.GetHashCode() & 1) == 0 ? 1f : -1f;
}

/// <summary>Sootling — 1 hit, rushes past, bellows staggers. Never parks on the hero.</summary>
public partial class Sootling : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "sootling_0";
	public bool IsAlive => _alive;

	private enum Phase { Approach, Telegraph, Lunge, Recover }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _staggered;
	private float _staggerT;
	private Phase _phase = Phase.Approach;
	private float _phaseT;
	private Vector2 _lungeDir = Vector2.Right;
	private bool _lungedHit;
	private float _wobble;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}

		CollisionLayer = 1 << 2;
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = Tiles.Px(0.375f) }
		});
		_body = Assets.Sprite(Assets.Enemy("sootling"));
		Assets.ApplyFeetPivot(_body);
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
		_wobble = (EnemyId.GetHashCode() & 255) * 0.05f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_alive || GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}

		var dt = (float)delta;
		if (_staggered)
		{
			_staggerT -= dt;
			Velocity = Vector2.Zero;
			if (_staggerT <= 0)
			{
				_staggered = false;
				Enter(Phase.Recover);
			}
			MoveAndSlide();
			return;
		}

		var player = PlayerController.Instance;
		if (player == null)
			return;

		_phaseT -= dt;
		var to = player.GlobalPosition - GlobalPosition;
		var dist = to.Length();

		switch (_phase)
		{
			case Phase.Approach:
				if (dist < Tiles.Px(0.7f))
				{
					Enter(Phase.Recover);
					break;
				}
				if (dist < Tiles.Px(2.2f))
				{
					Enter(Phase.Telegraph);
					break;
				}
				{
					var dir = to / Mathf.Max(dist, 1f);
					var perp = new Vector2(-dir.Y, dir.X);
					var w = Mathf.Sin(Time.GetTicksMsec() * 0.007f + _wobble);
					Velocity = dir * Tiles.Px(3.2f) + perp * w * Tiles.Px(1.5f);
					Velocity += EnemySteer.Separate(this, Tiles.Px(0.9f));
				}
				break;

			case Phase.Telegraph:
				Velocity = Vector2.Zero;
				if (_phaseT <= 0)
					Enter(Phase.Lunge);
				break;

			case Phase.Lunge:
				Velocity = _lungeDir * Tiles.Px(8.4f);
				if (!_lungedHit && dist < Tiles.Px(0.95f) && !player.AttackBusy)
				{
					_lungedHit = true;
					player.ApplyHit(_lungeDir);
				}
				if (_phaseT <= 0)
					Enter(Phase.Recover);
				break;

			case Phase.Recover:
				Velocity = EnemySteer.Away(GlobalPosition, player.GlobalPosition, Tiles.Px(2.4f));
				if (_phaseT <= 0)
					Enter(Phase.Approach);
				break;
		}

		MoveAndSlide();
	}

	private void Enter(Phase next)
	{
		_phase = next;
		var player = PlayerController.Instance;
		switch (next)
		{
			case Phase.Approach:
				_phaseT = 0;
				_body.Modulate = Colors.White;
				break;
			case Phase.Telegraph:
				_phaseT = 0.16f;
				_body.Modulate = Palette.Ember;
				if (player != null)
				{
					var d = player.GlobalPosition - GlobalPosition;
					_lungeDir = d.LengthSquared() > 1f ? d.Normalized() : Vector2.Right;
				}
				break;
			case Phase.Lunge:
				_phaseT = 0.26f;
				_lungedHit = false;
				_body.Modulate = Palette.KilnBloom;
				break;
			case Phase.Recover:
				_phaseT = 0.42f;
				_body.Modulate = Colors.White;
				break;
		}
	}

	public void TakeSwordHit(Vector2 fromDirection, int damage = 1)
	{
		if (!_alive)
			return;
		_alive = false;
		GameState.Instance.DefeatedEnemyIds.Add(EnemyId);
		_flash.Flash(_body, Palette.HitFlash, 0.08f);
		Hitstop.Pulse(this, 0.04f);
		GetTree().CreateTimer(0.08f, true, false, true).Timeout += () => QueueFree();
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_staggered = true;
		_staggerT = 0.45f;
		_flash.Flash(_body, Palette.BellowsPuff, 0.12f);
		GlobalPosition += fromDirection.Normalized() * Tiles.Px(0.625f);
	}
}

/// <summary>Claywalker — crust soaks one sword hit until bellows softens.</summary>
public partial class Claywalker : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "claywalker_0";
	public bool IsAlive => _alive;

	private enum Phase { Stalk, Windup, StepIn, Recoil }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _softened;
	private Phase _phase = Phase.Stalk;
	private float _phaseT;
	private float _holdDist;
	private Vector2 _stepDir = Vector2.Right;
	private bool _stepHit;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Tiles.Px(0.5f) } });
		_body = Assets.Sprite(Assets.Enemy("claywalker"));
		Assets.ApplyFeetPivot(_body, 32, 40);
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
		_holdDist = Tiles.Px(EnemySteer.SignOf(EnemyId) > 0 ? 1.7f : 2.15f);
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
		var player = PlayerController.Instance;
		if (player == null)
			return;
		var to = player.GlobalPosition - GlobalPosition;
		var dist = to.Length();
		var walk = _softened ? Tiles.Px(2.15f) : Tiles.Px(1.55f);

		switch (_phase)
		{
			case Phase.Stalk:
				Velocity = EnemySteer.Chase(GlobalPosition, player.GlobalPosition, walk, _holdDist);
				Velocity += EnemySteer.Separate(this, Tiles.Px(1.1f));
				if (_phaseT <= 0 && dist < _holdDist + Tiles.Px(0.35f) && dist > Tiles.Px(0.5f))
					Enter(Phase.Windup);
				break;

			case Phase.Windup:
				Velocity = Vector2.Zero;
				if (_phaseT <= 0)
					Enter(Phase.StepIn);
				break;

			case Phase.StepIn:
				Velocity = _stepDir * Tiles.Px(3.6f);
				if (!_stepHit && dist < Tiles.Px(1.05f) && !player.AttackBusy
					&& !Input.IsActionPressed("bellows") && !Input.IsActionPressed("attack")
					&& !player.PuffIframe)
				{
					_stepHit = true;
					player.ApplyHit(_stepDir);
				}
				if (_phaseT <= 0)
					Enter(Phase.Recoil);
				break;

			case Phase.Recoil:
				Velocity = EnemySteer.Away(GlobalPosition, player.GlobalPosition, Tiles.Px(2.2f));
				if (_phaseT <= 0)
					Enter(Phase.Stalk);
				break;
		}

		MoveAndSlide();
	}

	private void Enter(Phase next)
	{
		_phase = next;
		var player = PlayerController.Instance;
		switch (next)
		{
			case Phase.Stalk:
				_phaseT = 0.5f;
				_body.Modulate = Colors.White;
				break;
			case Phase.Windup:
				_phaseT = 0.38f;
				_body.Modulate = Palette.ClayMid;
				if (player != null)
				{
					var d = player.GlobalPosition - GlobalPosition;
					_stepDir = d.LengthSquared() > 1f ? d.Normalized() : Vector2.Down;
				}
				break;
			case Phase.StepIn:
				_phaseT = 0.22f;
				_stepHit = false;
				_body.Modulate = Palette.TerracottaHot;
				break;
			case Phase.Recoil:
				_phaseT = 0.4f;
				_body.Modulate = Colors.White;
				break;
		}
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
		GetTree().CreateTimer(0.1f, true, false, true).Timeout += () => QueueFree();
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_softened = true;
		var soft = Assets.Enemy("claywalker_soft");
		if (soft != null)
			_body.Texture = soft;
		_body.Modulate = Colors.White;
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

	private enum Phase { Cling, Strike, Retreat }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _dropped;
	private int _hp = 2;
	private Phase _phase = Phase.Cling;
	private float _phaseT;
	private Vector2 _home;
	private Vector2 _strikeDir = Vector2.Down;
	private bool _strikeHit;
	private int _strikeGrace;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId))
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Tiles.Px(0.375f) } });
		_body = Assets.Sprite(Assets.Enemy("brickleech"));
		Assets.ApplyFeetPivot(_body, 32, 32);
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
		if (ClingPos != Vector2.Zero)
			GlobalPosition = ClingPos;
		_home = ClingPos != Vector2.Zero ? ClingPos : GlobalPosition;
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
		var player = PlayerController.Instance;
		if (player == null)
			return;

		var to = player.GlobalPosition - GlobalPosition;
		var dist = to.Length();

		if (!_dropped)
		{
			if (Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X) < Tiles.Px(1.125f)
			    && player.GlobalPosition.Y > GlobalPosition.Y - Tiles.Px(0.5f))
			{
				_dropped = true;
				_home = ClingPos != Vector2.Zero ? ClingPos : GlobalPosition;
				(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Brickleech drops.");
				if (player.PuffIframe || Input.IsActionPressed("bellows") || Input.IsActionJustPressed("bellows"))
					Enter(Phase.Retreat);
				else
					Enter(Phase.Strike);
			}
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		switch (_phase)
		{
			case Phase.Strike:
				Velocity = _strikeDir * Tiles.Px(5.2f);
				if (_strikeGrace > 0)
				{
					_strikeGrace--;
					if (Input.IsActionPressed("bellows") || Input.IsActionJustPressed("bellows"))
						_strikeHit = true;
					break;
				}
				if (!_strikeHit && dist < Tiles.Px(0.9f) && !player.AttackBusy && !player.PuffIframe && !Input.IsActionPressed("bellows") && !Input.IsActionJustPressed("bellows"))
				{
					_strikeHit = true;
					player.ApplyHit(_strikeDir);
				}
				else if (!_strikeHit && (player.PuffIframe || Input.IsActionPressed("bellows") || Input.IsActionJustPressed("bellows")))
					_strikeHit = true;
				if (_phaseT <= 0)
					Enter(Phase.Retreat);
				break;

			case Phase.Retreat:
				Velocity = EnemySteer.Chase(GlobalPosition, _home, Tiles.Px(3.4f), Tiles.Px(0.25f));
				if (dist > Tiles.Px(5.5f) || GlobalPosition.DistanceTo(_home) < Tiles.Px(0.4f))
				{
					if (dist > Tiles.Px(5.5f))
					{
						_dropped = false;
						GlobalPosition = _home;
						_phase = Phase.Cling;
						_body.Modulate = Colors.White;
					}
					else if (_phaseT <= 0 && !player.PuffIframe)
						Enter(Phase.Strike);
				}
				else if (_phaseT <= 0 && !player.PuffIframe)
					Enter(Phase.Strike);
				break;
		}

		MoveAndSlide();
	}

	private void Enter(Phase next)
	{
		_phase = next;
		var player = PlayerController.Instance;
		switch (next)
		{
			case Phase.Strike:
				_phaseT = 0.24f;
				_strikeHit = false;
				_strikeGrace = 1;
				_body.Modulate = Palette.FiredClay;
				if (player != null)
				{
					var d = player.GlobalPosition - GlobalPosition;
					_strikeDir = d.LengthSquared() > 1f ? d.Normalized() : Vector2.Down;
				}
				break;
			case Phase.Retreat:
				_phaseT = 0.7f;
				_body.Modulate = Colors.White;
				break;
		}
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
			GetTree().CreateTimer(0.08f, true, false, true).Timeout += () => QueueFree();
		}
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_dropped = true;
		GlobalPosition += fromDirection.Normalized() * Tiles.Px(0.5f);
		_flash.Flash(_body, Palette.BellowsPuff, 0.1f);
	}
}

/// <summary>Clinker miniboss — bellows opens cracks; Crackiron hits the cracks.</summary>
public partial class Clinker : CharacterBody2D, IDamageable
{
	public string EnemyId { get; set; } = "clinker_yard";
	public bool IsAlive => _alive;

	private enum Phase { Trudge, Plant, Slam }

	private Sprite2D _body = null!;
	private FlashFx _flash = null!;
	private bool _alive = true;
	private bool _cracked;
	private int _hp = 4;
	private float _crackTimer;
	private Phase _phase = Phase.Trudge;
	private float _phaseT;
	private Vector2 _slamDir = Vector2.Down;
	private bool _slamHit;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId) || GameState.Instance.ClinkerDown)
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Tiles.Px(0.75f) } });
		_body = Assets.Sprite(Assets.Enemy("clinker"));
		Assets.ApplyFeetPivot(_body, 48, 48);
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
		var dt = (float)delta;
		_phaseT -= dt;

		var player = PlayerController.Instance;
		if (player != null && !_cracked && (player.PuffIframe || Input.IsActionJustPressed("bellows")))
			TakeBellowsPuff(player.GlobalPosition - GlobalPosition);

		if (_cracked)
		{
			_crackTimer -= dt;
			if (_crackTimer <= 0)
			{
				_cracked = false;
				var sealedTex = Assets.Enemy("clinker");
				if (sealedTex != null)
					_body.Texture = sealedTex;
				_body.Modulate = Colors.White;
			}
			// Open cracks — stand still so Crackiron can land.
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (player == null)
			return;
		var to = player.GlobalPosition - GlobalPosition;
		var dist = to.Length();

		switch (_phase)
		{
			case Phase.Trudge:
				Velocity = EnemySteer.Chase(GlobalPosition, player.GlobalPosition, Tiles.Px(1.15f), Tiles.Px(2.2f));
				if (_phaseT <= 0 && dist < Tiles.Px(2.5f))
					Enter(Phase.Plant);
				break;

			case Phase.Plant:
				Velocity = Vector2.Zero;
				if (_phaseT <= 0)
					Enter(Phase.Slam);
				break;

			case Phase.Slam:
				Velocity = _slamDir * Tiles.Px(3.2f);
				if (!_slamHit && dist < Tiles.Px(1.55f) && !player.AttackBusy && !player.PuffIframe && !Input.IsActionPressed("bellows") && !Input.IsActionJustPressed("bellows"))
				{
					_slamHit = true;
					player.ApplyHit(_slamDir);
				}
				if (_phaseT <= 0)
					Enter(Phase.Trudge);
				break;
		}

		MoveAndSlide();
	}

	private void Enter(Phase next)
	{
		_phase = next;
		var player = PlayerController.Instance;
		switch (next)
		{
			case Phase.Trudge:
				_phaseT = 0.55f;
				if (!_cracked)
					_body.Modulate = Colors.White;
				break;
			case Phase.Plant:
				_phaseT = 0.55f;
				_body.Modulate = Palette.AshWarm;
				if (player != null)
				{
					var d = player.GlobalPosition - GlobalPosition;
					_slamDir = d.LengthSquared() > 1f ? d.Normalized() : Vector2.Down;
				}
				break;
			case Phase.Slam:
				_phaseT = 0.22f;
				_slamHit = false;
				_body.Modulate = Palette.Ember;
				break;
		}
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
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.ShowToast("Clinker falls. Slag cools into a hammer.");
			(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
			if (!GameState.Instance.BackironTaken)
			{
				GetParent()?.AddChild(new BackironPickup { Position = GlobalPosition });
			}
			GetTree().CreateTimer(0.15f, true, false, true).Timeout += () => QueueFree();
		}
	}

	public void TakeBellowsPuff(Vector2 fromDirection)
	{
		if (!_alive)
			return;
		_cracked = true;
		_crackTimer = 2.2f;
		var cracked = Assets.Enemy("clinker_cracked");
		if (cracked != null)
			_body.Texture = cracked;
		_body.Modulate = Colors.White;
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
	private float _orbitSign = 1f;

	public override void _Ready()
	{
		if (GameState.Instance.DefeatedEnemyIds.Contains(EnemyId) || GameState.Instance.OverfireDown)
		{
			QueueFree();
			return;
		}
		CollisionLayer = 1 << 2;
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Tiles.Px(0.875f) } });
		_body = Assets.Sprite(Assets.Enemy("overfire"));
		Assets.ApplyFeetPivot(_body, 64, 64);
		_body.ZIndex = 10;
		AddChild(_body);
		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("enemy");
		_orbitSign = EnemySteer.SignOf(EnemyId);
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
				Velocity = EnemySteer.Orbit(GlobalPosition, player.GlobalPosition, Tiles.Px(2.6f), Tiles.Px(1.35f), _orbitSign);
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
				if (dist > Tiles.Px(1.5f))
					Velocity = to.Normalized() * Tiles.Px(2.1f);
				else
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
					else if (d < Tiles.Px(3.25f))
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
				if (player != null && player.GlobalPosition.DistanceTo(GlobalPosition) < Tiles.Px(1.875f) && _hitCd <= 0)
				{
					_hitCd = 0.6f;
					player.ApplyHit((player.GlobalPosition - GlobalPosition).Normalized(), 1);
				}
				break;
		}
	}

	private void SetSprite(string name)
	{
		var tex = Assets.Enemy(name);
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
			GetTree().CreateTimer(0.2f, true, false, true).Timeout += () => QueueFree();
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
