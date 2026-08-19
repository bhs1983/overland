using Godot;

namespace Overland;

/// <summary>
/// Legacy stub kept for scene refs. Prefer <see cref="SliceParallax"/> (ParallaxBackground).
/// Orthographic Camera2D only — no 3D camera.
/// </summary>
public partial class ParallaxCamera : Camera2D
{
	[Export] public Node2D? Target;
	[Export] public float SmoothSpeed = 8f;

	public override void _Process(double delta)
	{
		if (Target == null)
			return;
		GlobalPosition = GlobalPosition.Lerp(
			Target.GlobalPosition,
			1f - Mathf.Exp(-SmoothSpeed * (float)delta));
	}

	public void SetTarget(Node2D target) => Target = target;
}
