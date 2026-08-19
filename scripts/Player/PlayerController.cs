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

	private AnimatedSprite2D _sprite = null!;
	private AttackArc _attack = null!;
	private BellowsCone _bellows = null!;
	private Area2D _interactArea = null!;
	private FlashFx _flash = null!;

	private Vector2 _facing = Vector2.Down;
	private string _facingName = "down";
	private bool _attacking;
	private bool _dodging;
	private bool _iframe;
	private bool _animBusy;
	private float _knockbackTime;
	private Vector2 _knockbackVel;
	private bool _sparkSpawned;

	public override void _Ready()
	{
		Instance = this;
		CollisionLayer = 1 << 1;
		CollisionMask = 1 << 0;

		AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(10, 10) },
			Position = new Vector2(0, 2)
		});

		_sprite = new AnimatedSprite2D
		{
			SpriteFrames = HeroAtlas.Frames,
			TextureFilter = TextureFilterEnum.Nearest,
			TextureRepeat = TextureRepeatEnum.Disabled
		};
		HeroAtlas.ApplyPivot(_sprite);
		_sprite.FrameChanged += OnFrameChanged;
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
		PlayAnim($"fluewalker_idle_{_facingName}");
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
			PlayAnim($"fluewalker_walk_{_facingName}");
		}
		else
		{
			Velocity = Vector2.Zero;
			PlayAnim($"fluewalker_idle_{_facingName}");
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
			_facingName = next;
	}

	private void PlayAnim(string anim)
	{
		if (!_sprite.SpriteFrames.HasAnimation(anim))
			return;
		if (_sprite.Animation == anim && _sprite.IsPlaying())
			return;
		_sprite.Play(anim);
	}

	private void OnFrameChanged()
	{
		// Spark on swing_03 (1-indexed) → frame index 2.
		if (!_sparkSpawned && _sprite.Animation.ToString().StartsWith("fluewalker_swing_") && _sprite.Frame == 2)
		{
			_sparkSpawned = true;
			SparkBurst.Spawn(GetParent() ?? this, GlobalPosition, _facing);
		}
	}

	private async Task DoAttack()
	{
		_attacking = true;
		_animBusy = true;
		_sparkSpawned = false;
		Velocity = Vector2.Zero;
		PlayAnim($"fluewalker_swing_{_facingName}");
		_ = PlaySwingAndArc();
		await _attack.Swing(this, _facing);
		PlayAnim($"fluewalker_idle_{_facingName}");
		_attacking = false;
		_animBusy = false;
	}

	private async Task PlaySwingAndArc()
	{
		// Hold swing anim through AttackArc (~0.12s + frames).
		await ToSignal(GetTree().CreateTimer(0.16f), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task DoBellows()
	{
		_attacking = true;
		_animBusy = true;
		Velocity = Vector2.Zero;
		PlayAnim($"fluewalker_idle_{_facingName}");
		await _bellows.Puff(this, _facing);
		PlayAnim($"fluewalker_idle_{_facingName}");
		_attacking = false;
		_animBusy = false;
	}

	private async Task DoDodge()
	{
		_dodging = true;
		_iframe = true;
		var dir = _facing.LengthSquared() > 0.01f ? _facing : Vector2.Down;
		PlayAnim($"fluewalker_hop_{_facingName}");
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
		PlayAnim($"fluewalker_idle_{_facingName}");
		await ToSignal(GetTree().CreateTimer(IFrameTime - DodgeTime), SceneTreeTimer.SignalName.Timeout);
		_iframe = false;
	}

	private async Task DoInteract()
	{
		PlayAnim($"fluewalker_idle_{_facingName}");
		await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
		TryInteract();
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (!GameState.Instance.InputLocked)
			PlayAnim($"fluewalker_idle_{_facingName}");
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
		PlayAnim($"fluewalker_hurt_{_facingName}");
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
	public bool AttackBusy => _attacking;
}
