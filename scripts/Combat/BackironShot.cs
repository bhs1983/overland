using Godot;
using System.Collections.Generic;

namespace Overland;

/// <summary>
/// Thrown Backiron — flies out along facing, then homes to the hand.
/// Hits once outbound and once on the return. Not a spell bar.
/// </summary>
public partial class BackironShot : Area2D
{
	public const float Speed = 10f;
	public const float RangeTiles = 5.25f;

	private Node2D _owner = null!;
	private Sprite2D _spr = null!;
	private Vector2 _outDir;
	private float _traveled;
	private bool _returning;
	private readonly HashSet<ulong> _hit = new();

	public static void Throw(Node2D owner, Vector2 facing)
	{
		if (GameState.Instance.BackironOut)
			return;
		var dir = facing.LengthSquared() > 0.0001f ? facing.Normalized() : Vector2.Down;
		var shot = new BackironShot();
		owner.GetTree().CurrentScene.AddChild(shot);
		shot.Launch(owner, dir);
	}

	public override void _Ready()
	{
		AddToGroup("backiron");
		CollisionLayer = 0;
		CollisionMask = (1 << 0) | (1 << 2);
		Monitoring = true;
		Monitorable = false;
		AddChild(new CollisionShape2D
		{
			Shape = new CircleShape2D { Radius = Tiles.Px(0.45f) }
		});
		_spr = Assets.Sprite(Assets.Item("backiron"));
		_spr.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		AddChild(_spr);
		BodyEntered += OnBody;
	}

	public void Launch(Node2D owner, Vector2 dir)
	{
		_owner = owner;
		_outDir = dir;
		GlobalPosition = owner.GlobalPosition + dir * Tiles.Px(0.6f);
		GameState.Instance.BackironOut = true;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Paused || GameState.Instance.InputLocked)
			return;
		if (!IsInstanceValid(_owner))
		{
			Catch();
			return;
		}

		var dt = (float)delta;
		_spr.Rotation += 14f * dt;

		if (!_returning)
		{
			var step = _outDir * Tiles.Px(Speed) * dt;
			GlobalPosition += step;
			_traveled += step.Length();
			if (_traveled >= Tiles.Px(RangeTiles))
				TurnBack();
			return;
		}

		var home = _owner.GlobalPosition;
		var to = home - GlobalPosition;
		var dist = to.Length();
		if (dist < Tiles.Px(0.55f))
		{
			Catch();
			return;
		}
		GlobalPosition += to / dist * Tiles.Px(Speed) * 1.12f * dt;
	}

	private void OnBody(Node2D body)
	{
		if (body == _owner)
			return;
		if (body is IDamageable dmg && dmg.IsAlive)
		{
			var id = body.GetInstanceId();
			if (!_hit.Add(id))
				return;
			dmg.TakeSwordHit(_returning ? (body.GlobalPosition - GlobalPosition) : _outDir);
			SparkBurst.SpawnHit(GetParent() ?? this, body.GlobalPosition);
			return;
		}
		if (!_returning && body is StaticBody2D)
			TurnBack();
	}

	private void TurnBack()
	{
		if (_returning)
			return;
		_returning = true;
		_hit.Clear();
	}

	private void Catch()
	{
		GameState.Instance.BackironOut = false;
		(GetTree().GetFirstNodeInGroup("game_ui") as GameUi)?.RefreshHud();
		QueueFree();
	}
}
