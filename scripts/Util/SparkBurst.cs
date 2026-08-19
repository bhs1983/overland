using Godot;

namespace Overland;

/// <summary>16×16 impact VFX. Not baked into body frames. Spark stays 16×16.</summary>
public partial class SparkBurst : Node2D
{
	private string _tex = "spark";
	private float _life = 0.18f;
	private Vector2 _drift = Vector2.Up * Tiles.Px(0.375f);

	public static void Spawn(Node parent, Vector2 globalPos, Vector2 facing)
	{
		var tip = globalPos + facing.Normalized() * Tiles.Px(0.875f);
		Launch(parent, tip, "spark", 0.18f, Vector2.Up * Tiles.Px(0.375f));
		Launch(parent, tip + new Vector2(2, -1), "spark_b", 0.16f,
			Vector2.Up * Tiles.Px(0.3f) + facing.Normalized() * 2f);
	}

	public static void SpawnHit(Node parent, Vector2 globalPos)
	{
		Launch(parent, globalPos, "spark", 0.16f, Vector2.Up * Tiles.Px(0.25f));
		Launch(parent, globalPos + new Vector2(3, -2), "spark_b", 0.14f,
			new Vector2(2, -Tiles.Px(0.3f)));
	}

	public static void SpawnBellows(Node parent, Vector2 globalPos, Vector2 facing)
	{
		var tip = globalPos + facing.Normalized() * Tiles.Px(0.875f);
		Launch(parent, tip, "smoke", 0.28f, Vector2.Up * Tiles.Px(0.5f));
		Launch(parent, tip + facing.Normalized() * Tiles.Px(0.25f) + Vector2.Up * 2f,
			"ash_fall", 0.32f, Vector2.Down * Tiles.Px(0.25f));
	}

	private static void Launch(Node parent, Vector2 globalPos, string tex, float life, Vector2 drift)
	{
		var burst = new SparkBurst
		{
			_tex = tex,
			_life = life,
			_drift = drift
		};
		parent.AddChild(burst);
		burst.GlobalPosition = globalPos;
	}

	public override void _Ready()
	{
		var tex = Assets.Vfx(_tex);
		if (tex == null)
		{
			QueueFree();
			return;
		}

		var spr = Assets.Sprite(tex);
		spr.TextureFilter = TextureFilterEnum.Nearest;
		AddChild(spr);
		var tween = CreateTween();
		tween.TweenProperty(spr, "modulate:a", 0f, _life);
		tween.Parallel().TweenProperty(this, "position", Position + _drift, _life);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
