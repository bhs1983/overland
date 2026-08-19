using Godot;

namespace Overland;

public interface IDamageable
{
	bool IsAlive { get; }
	void TakeSwordHit(Vector2 fromDirection, int damage = 1);
	void TakeBellowsPuff(Vector2 fromDirection);
}

public interface IBellowsTarget
{
	void OnBellows(Vector2 fromDirection);
}

public partial class AttackArc : Area2D
{
	private CollisionShape2D _shape = null!;
	private Polygon2D _visual = null!;
	private bool _active;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1 << 2; // enemy
		Monitoring = false;
		Monitorable = false;

		_shape = new CollisionShape2D
		{
			// Reach Claywalker (18px) and Clinker (22px) standoff, not just Sootling park.
			Shape = new RectangleShape2D { Size = new Vector2(Tiles.Px(2f), Tiles.Px(1.25f)) },
			Disabled = true
		};
		AddChild(_shape);

		_visual = new Polygon2D
		{
			Polygon = new Vector2[]
			{
				new(Tiles.Px(-0.25f), Tiles.Px(-0.4375f)), new(Tiles.Px(0.875f), Tiles.Px(-0.4375f)),
				new(Tiles.Px(0.875f), Tiles.Px(0.4375f)), new(Tiles.Px(-0.25f), Tiles.Px(0.4375f))
			},
			Color = Palette.Telegraph,
			Visible = false
		};
		AddChild(_visual);
	}

	public async System.Threading.Tasks.Task Swing(Node2D owner, Vector2 facing)
	{
		if (_active)
			return;
		_active = true;

		Position = facing.Normalized() * Tiles.Px(1f);
		Rotation = facing.Angle();

		// Keep _visual hidden — filled telegraph rect was the QA cream/orange block.
		_shape.Disabled = true;
		Monitoring = false;

		// 3–4 frame telegraph at ~60fps
		await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);

		_shape.Disabled = false;
		Monitoring = true;

		// Two physics frames so overlap queries see CharacterBody2D on layer 2.
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		foreach (var body in GetOverlappingBodies())
		{
			if (body is IDamageable dmg && dmg.IsAlive)
			{
				if (body is Node2D hit)
					SparkBurst.SpawnHit(owner.GetParent() ?? owner, hit.GlobalPosition);
				dmg.TakeSwordHit(facing);
			}
		}

		Hitstop.Pulse(owner, 0.04f);
		await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);

		_visual.Visible = false;
		_shape.Disabled = true;
		Monitoring = false;
		_active = false;
	}
}

public partial class BellowsCone : Area2D
{
	private CollisionShape2D _shape = null!;
	private Polygon2D _visual = null!;
	private bool _active;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = (1 << 2) | (1 << 0) | (1 << 5); // enemy, world props, interact
		Monitoring = false;

		_shape = new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = new Vector2(Tiles.Px(2.25f), Tiles.Px(1.25f)) },
			Disabled = true
		};
		AddChild(_shape);

		_visual = new Polygon2D
		{
			Polygon = new Vector2[]
			{
				new(Tiles.Px(-0.125f), Tiles.Px(-0.5f)), new(Tiles.Px(1.625f), Tiles.Px(-0.5f)),
				new(Tiles.Px(1.625f), Tiles.Px(0.5f)), new(Tiles.Px(-0.125f), Tiles.Px(0.5f))
			},
			Color = new Color(Palette.BellowsPuff, 0.7f),
			Visible = false
		};
		AddChild(_visual);
	}

	public async System.Threading.Tasks.Task Puff(Node2D owner, Vector2 facing)
	{
		if (_active)
			return;
		_active = true;

		Position = facing.Normalized() * Tiles.Px(1.25f);
		Rotation = facing.Angle();

		_visual.Visible = false; // same as AttackArc — no filled puff block
		_shape.Disabled = false;
		Monitoring = true;
		SparkBurst.SpawnBellows(owner.GetParent() ?? owner, owner.GlobalPosition, facing);

		// Wait two physics frames so Area2D overlap queries see StaticBody2D on layer 0 (DeadFan).
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		foreach (var body in GetOverlappingBodies())
		{
			if (body is IDamageable dmg && dmg.IsAlive)
				dmg.TakeBellowsPuff(facing);
			if (body is IBellowsTarget target)
				target.OnBellows(facing);
		}

		foreach (var area in GetOverlappingAreas())
		{
			if (area is IBellowsTarget target)
				target.OnBellows(facing);
			if (area.GetParent() is IBellowsTarget parentTarget)
				parentTarget.OnBellows(facing);
		}

		await ToSignal(GetTree().CreateTimer(0.12f), SceneTreeTimer.SignalName.Timeout);

		_visual.Visible = false;
		_shape.Disabled = true;
		Monitoring = false;
		_active = false;
	}
}
