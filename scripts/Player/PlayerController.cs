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
	private Area2D _interactArea = null!;
	private FlashFx _flash = null!;

	private Vector2 _facing = Vector2.Down;
	private bool _attacking;
	private bool _dodging;
	private bool _iframe;
	private float _knockbackTime;
	private Vector2 _knockbackVel;

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

		if (_dodging)
			return;

		var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (input.LengthSquared() > 0.01f)
		{
			_facing = input.Normalized();
			Velocity = _facing * MoveSpeed;
			UpdateFacingSprite(false);
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		if (!_attacking)
		{
			if (GameState.Instance.HasCrackiron && Input.IsActionJustPressed("attack"))
				_ = DoAttack();
			else if (Input.IsActionJustPressed("dodge"))
				_ = DoDodge();
			else if (Input.IsActionJustPressed("interact"))
				TryInteract();
		}

		MoveAndSlide();
	}

	private void UpdateFacingSprite(bool swinging)
	{
		if (swinging)
		{
			_sprite.Texture = Assets.Hero("swing_down");
			return;
		}
		_sprite.Texture = Assets.Hero(
			Mathf.Abs(_facing.X) > Mathf.Abs(_facing.Y)
				? (_facing.X < 0 ? "idle_left" : "idle_right")
				: (_facing.Y < 0 ? "idle_up" : "idle_down"));
	}

	private async Task DoAttack()
	{
		_attacking = true;
		Velocity = Vector2.Zero;
		UpdateFacingSprite(true);
		await _attack.Swing(this, _facing);
		UpdateFacingSprite(false);
		_attacking = false;
	}

	private async Task DoDodge()
	{
		_dodging = true;
		_iframe = true;
		var dir = _facing.LengthSquared() > 0.01f ? _facing : Vector2.Down;
		var start = GlobalPosition;
		var target = start + dir * DodgeDistance;
		var t = 0f;
		while (t < DodgeTime)
		{
			t += (float)GetProcessDeltaTime();
			GlobalPosition = start.Lerp(target, Mathf.Clamp(t / DodgeTime, 0f, 1f));
			Velocity = Vector2.Zero;
			MoveAndSlide();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_dodging = false;
		await ToSignal(GetTree().CreateTimer(IFrameTime - DodgeTime), SceneTreeTimer.SignalName.Timeout);
		_iframe = false;
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
