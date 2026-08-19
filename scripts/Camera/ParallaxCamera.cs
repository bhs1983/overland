using Godot;

namespace Overland.Camera;

/// <summary>
/// Simple top-down camera that follows the player and supports multiple parallax layers.
/// Attach to a Camera2D. Child Node2Ds marked as parallax layers will scroll at different rates.
/// </summary>
public partial class ParallaxCamera : Camera2D
{
    [Export] public Node2D Target;
    [Export] public float SmoothSpeed = 5f;
    [Export] public Vector2 Offset = Vector2.Zero;

    // Optional: list of parallax layers and their scroll multipliers
    // 0.2 = far background, 0.5 = mid, 1.0 = main, 1.3 = foreground
    private Godot.Collections.Array<Node2D> _parallaxLayers = new();
    private Godot.Collections.Array<float> _parallaxMultipliers = new();

    public override void _Ready()
    {
        // Auto-detect children with names starting with "Parallax_"
        foreach (Node child in GetChildren())
        {
            if (child is Node2D layer && child.Name.ToString().StartsWith("Parallax_"))
            {
                _parallaxLayers.Add(layer);
                // Default multipliers based on common naming
                if (child.Name.ToString().Contains("Far")) _parallaxMultipliers.Add(0.2f);
                else if (child.Name.ToString().Contains("Mid")) _parallaxMultipliers.Add(0.5f);
                else if (child.Name.ToString().Contains("Fore")) _parallaxMultipliers.Add(1.3f);
                else _parallaxMultipliers.Add(1.0f);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Target == null) return;

        Vector2 desired = Target.GlobalPosition + Offset;
        GlobalPosition = GlobalPosition.Lerp(desired, 1f - Mathf.Exp(-SmoothSpeed * (float)delta));

        // Apply parallax offsets relative to camera movement
        // (Simple version — improve later with proper scroll tracking)
        for (int i = 0; i < _parallaxLayers.Count; i++)
        {
            // Placeholder: layers will be updated properly once we have a scroll delta system
        }
    }

    public void SetTarget(Node2D target)
    {
        Target = target;
    }
}
