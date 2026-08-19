using Godot;

namespace Overland;

/// <summary>Spark VFX — not baked into swing body frames. Spawn on swing_03.</summary>
public partial class SparkBurst : Node2D
{
	public static void Spawn(Node parent, Vector2 globalPos, Vector2 facing)
	{
		var spark = new SparkBurst();
		parent.AddChild(spark);
		spark.GlobalPosition = globalPos + facing.Normalized() * Tiles.Px(0.875f);
	}

	public override void _Ready()
	{
		var spr = Assets.Sprite(Assets.Vfx("spark"));
		spr.TextureFilter = TextureFilterEnum.Nearest;
		AddChild(spr);
		var tween = CreateTween();
		tween.TweenProperty(spr, "modulate:a", 0f, 0.18f);
		tween.Parallel().TweenProperty(this, "position", Position + Vector2.Up * Tiles.Px(0.375f), 0.18f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
