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
		CollisionMask = 1 << 0;
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = 6 }
		});
		_body = Assets.Sprite(Assets.Enemy("sootling"));
		_body.ZIndex = 10; // draw above player so chase does not vanish under the hero
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
		// Stay in AttackArc range (~3–21px ahead); do not park under the hero.
		if (distSq < 14f * 14f)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (_hitCd <= 0)
			{
				_hitCd = 0.55f;
				player.ApplyHit(distSq > 0.0001f ? to : Vector2.Right);
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
