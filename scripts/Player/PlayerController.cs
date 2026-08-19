using Godot;
using System.Threading.Tasks;

namespace Overland;

public partial class PlayerController : CharacterBody2D
{
	public static PlayerController? Instance { get; private set; }

	[Export] public float MoveSpeed { get; set; } = 90f;
	[Export] public float DodgeDistance { get; set; } = 40f;
	[Export] public float DodgeTime { get; set; } = 0.12f;
	[Export] public float IFrameTime { get; set; } = 0.22f;

	private Sprite2D _sprite = null!;
	private AttackArc _attack = null!;
	private BellowsCone _bellows = null!;
	private Area2D _interactArea = null!;
	private FlashFx _flash = null!;

	private Vector2 _facing = Vector2.Down;
	private string _facingName = "down";
	private string _prevFacingName = "down";
	private bool _attacking;
	private bool _dodging;
	private bool _iframe;
	private bool _animBusy;
	private float _knockbackTime;
	private Vector2 _knockbackVel;
	private float _walkAnimT;
	private int _walkFrame;
	private float _turnFlashT;

	public override void _Ready()
	{
		Instance = this;
		CollisionLayer = 1 << 1;
		CollisionMask = 1 << 0;

		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(10, 10) },
			Position = new Vector2(0, 4)
		});

		_sprite = Assets.Sprite(Assets.Hero("idle_down"));
		_sprite.Offset = new Vector2(0, -4);
		AddChild(_sprite);

		_attack = new AttackArc();
		AddChild(_attack);
		_bellows = new BellowsCone();
		AddChild(_bellows);

		_interactArea = new Area2D
		{
			CollisionLayer = 0,
			CollisionMask = 1 << 5,
			Monitoring = true
		};
		_interactArea.AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 14 }
		});
		AddChild(_interactArea);

		_flash = new FlashFx();
		AddChild(_flash);
		AddToGroup("player");
		ApplySprite($"idle_{_facingName}", $"idle_down");
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Paused || GameState.Instance.InputLocked)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		var dt = (float)delta;
		if (_knockbackTime > 0)
		{
			_knockbackTime -= dt;
			Velocity = _knockbackVel;
			MoveAndSlide();
			return;
		}

		if (_dodging || _animBusy)
			return;

		var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (input.LengthSquared() > 0.01f)
		{
			_facing = input.Normalized();
			UpdateFacingName();
			Velocity = _facing * MoveSpeed;
			AdvanceWalk(dt);
		}
		else
		{
			Velocity = Vector2.Zero;
			if (_turnFlashT <= 0)
				ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
		}

		if (_turnFlashT > 0)
		{
			_turnFlashT -= dt;
			if (_turnFlashT <= 0 && input.LengthSquared() <= 0.01f)
				ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
		}

		if (!_attacking)
		{
			if (GameState.Instance.HasCrackiron && Input.IsActionJustPressed("attack"))
				_ = DoAttack();
			else if (GameState.Instance.HasFoldedBellows && Input.IsActionJustPressed("bellows"))
				_ = DoBellows();
			else if (Input.IsActionJustPressed("dodge"))
				_ = DoDodge();
			else if (Input.IsActionJustPressed("interact"))
				_ = DoInteract();
		}

		MoveAndSlide();
	}

	private void UpdateFacingName()
	{
		var next = Mathf.Abs(_facing.X) > Mathf.Abs(_facing.Y)
			? (_facing.X < 0 ? "left" : "right")
			: (_facing.Y < 0 ? "up" : "down");
		if (next != _facingName)
		{
			_prevFacingName = _facingName;
			_facingName = next;
			_walkFrame = 0;
			_walkAnimT = 0;
			TryPlayTurn();
		}
	}

	private void TryPlayTurn()
	{
		// Short turn frame when facing changes (Art turn_* names).
		string? turn = (_prevFacingName, _facingName) switch
		{
			("down", "left") or ("left", "down") => "turn_down_left",
			("left", "up") or ("up", "left") => "turn_left_up",
			("right", "down") or ("down", "right") => "turn_right_down",
			("up", "right") or ("right", "up") => "turn_up_right",
			_ => null
		};
		if (turn != null && Assets.HeroOrNull(turn) != null)
		{
			_sprite.Texture = Assets.Hero(turn);
			_turnFlashT = 0.08f;
		}
	}

	private void AdvanceWalk(float dt)
	{
		if (_turnFlashT > 0)
			return;
		_walkAnimT += dt;
		if (_walkAnimT >= 0.12f)
		{
			_walkAnimT = 0;
			_walkFrame = (_walkFrame + 1) % 4;
		}
		var frameName = $"walk_{_facingName}_{_walkFrame}";
		ApplySprite(frameName, $"idle_{_facingName}");
	}

	private void ApplySprite(string preferred, string fallback)
	{
		_sprite.Texture = Assets.HeroOrNull(preferred) ?? Assets.Hero(fallback);
	}

	private async Task DoAttack()
	{
		_attacking = true;
		_animBusy = true;
		Velocity = Vector2.Zero;
		_ = PlaySwingFrames();
		await _attack.Swing(this, _facing);
		ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
		_attacking = false;
		_animBusy = false;
	}

	private async Task PlaySwingFrames()
	{
		// 3 frames aligned with AttackArc telegraph + active (~0.12s total).
		for (int i = 0; i < 3; i++)
		{
			var name = $"swing_{_facingName}_{i}";
			var tex = Assets.HeroOrNull(name);
			if (tex == null && i == 0)
				tex = Assets.HeroOrNull("swing_down") ?? Assets.Hero("swing_down");
			if (tex != null)
				_sprite.Texture = tex;
			await ToSignal(GetTree().CreateTimer(0.04f), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private async Task DoBellows()
	{
		_attacking = true;
		_animBusy = true;
		Velocity = Vector2.Zero;
		ApplySprite($"interact_0", $"idle_{_facingName}");
		await _bellows.Puff(this, _facing);
		ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
		_attacking = false;
		_animBusy = false;
	}

	private async Task DoDodge()
	{
		_dodging = true;
		_iframe = true;
		var dir = _facing.LengthSquared() > 0.01f ? _facing : Vector2.Down;
		ApplySprite($"dodge_{_facingName}_0", $"idle_{_facingName}");
		var start = GlobalPosition;
		var target = start + dir * DodgeDistance;
		var t = 0f;
		var mid = false;
		while (t < DodgeTime)
		{
			t += (float)GetProcessDeltaTime();
			if (!mid && t >= DodgeTime * 0.5f)
			{
				mid = true;
				ApplySprite($"dodge_{_facingName}_1", $"idle_{_facingName}");
			}
			GlobalPosition = start.Lerp(target, Mathf.Clamp(t / DodgeTime, 0f, 1f));
			Velocity = Vector2.Zero;
			MoveAndSlide();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_dodging = false;
		ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
		await ToSignal(GetTree().CreateTimer(IFrameTime - DodgeTime), SceneTreeTimer.SignalName.Timeout);
		_iframe = false;
	}

	private async Task DoInteract()
	{
		ApplySprite("interact_0", $"idle_{_facingName}");
		await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
		ApplySprite("interact_1", $"idle_{_facingName}");
		TryInteract();
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (!GameState.Instance.InputLocked)
			ApplySprite($"idle_{_facingName}", $"idle_{_facingName}");
	}

	private void TryInteract()
	{
		Node2D? best = null;
		var bestDist = float.MaxValue;
		foreach (var area in _interactArea.GetOverlappingAreas())
		{
			var node = area is Interactable ? area : area.GetParent();
			if (node is Interactable && node is Node2D n2d)
			{
				var d = GlobalPosition.DistanceSquaredTo(n2d.GlobalPosition);
				if (d < bestDist)
				{
					bestDist = d;
					best = n2d;
				}
			}
		}
		if (best is Interactable interactable)
			interactable.Interact(this);
	}

	public void ApplyHit(Vector2 fromDirection, int damage = 1)
	{
		if (_iframe || GameState.Instance.Hp <= 0)
			return;
		GameState.Instance.Damage(damage);
		_flash.Flash(_sprite, Palette.HurtFlash, 0.12f);
		_knockbackVel = fromDirection.Normalized() * 120f;
		_knockbackTime = 0.12f;
		_iframe = true;
		GetTree().CreateTimer(0.4f).Timeout += () => _iframe = false;
		if (GameState.Instance.Hp <= 0)
		{
			GameState.Instance.InputLocked = true;
			GameState.Instance.Hp = GameState.MaxHp;
			(GetTree().GetFirstNodeInGroup("world") as WorldRoot)?.RespawnAtSave();
		}
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	public Vector2 Facing => _facing;
}
