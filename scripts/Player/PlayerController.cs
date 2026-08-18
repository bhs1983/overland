using Godot;

namespace Overland.Player;

/// <summary>
/// Basic top-down player controller for Slice 0.
/// Movement + placeholder for Crackiron attack and Folded Bellows tool.
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 120f;
    [Export] public float Acceleration = 800f;
    [Export] public float Friction = 800f;

    private Vector2 _inputDirection = Vector2.Zero;

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        _inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (_inputDirection != Vector2.Zero)
        {
            Velocity = Velocity.MoveToward(_inputDirection * MoveSpeed, Acceleration * dt);
        }
        else
        {
            Velocity = Velocity.MoveToward(Vector2.Zero, Friction * dt);
        }

        MoveAndSlide();

        // Placeholder attack / tool
        if (Input.IsActionJustPressed("attack"))
        {
            // TODO: Crackiron swing
            GD.Print("Crackiron swing");
        }

        if (Input.IsActionJustPressed("use_tool"))
        {
            // TODO: Folded Bellows puff
            GD.Print("Folded Bellows used");
        }
    }
}
