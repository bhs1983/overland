using Godot;

namespace Overland;

public static class PixelSprite
{
	public static Node2D MakeBody(Vector2 size, Color color, string name = "Body")
	{
		var root = new Node2D { Name = name };
		var poly = new Polygon2D
		{
			Polygon = new Vector2[]
			{
				-size / 2f,
				new Vector2(size.X / 2f, -size.Y / 2f),
				size / 2f,
				new Vector2(-size.X / 2f, size.Y / 2f)
			},
			Color = color
		};
		root.AddChild(poly);
		return root;
	}

	public static void SetBodyColor(Node2D body, Color color)
	{
		if (body.GetChildCount() > 0 && body.GetChild(0) is Polygon2D poly)
			poly.Color = color;
		else if (body is Sprite2D spr)
			spr.Modulate = color;
	}

	public static Color GetBodyColor(Node2D body)
	{
		if (body.GetChildCount() > 0 && body.GetChild(0) is Polygon2D poly)
			return poly.Color;
		return Colors.White;
	}
}

public partial class FlashFx : Node
{
	private Polygon2D? _poly;
	private CanvasItem? _fallback;
	private Color _base;
	private float _timeLeft;

	public void Flash(Node2D body, Color flash, float duration = 0.08f)
	{
		_timeLeft = duration;
		if (body is Sprite2D spr)
		{
			_fallback = spr;
			_base = spr.Modulate;
			spr.Modulate = flash;
			_poly = null;
			return;
		}
		if (body.GetChildCount() > 0 && body.GetChild(0) is Polygon2D poly)
		{
			_poly = poly;
			_base = poly.Color;
			poly.Color = flash;
			_fallback = null;
		}
		else
		{
			_fallback = body;
			_base = body.Modulate;
			body.Modulate = flash;
			_poly = null;
		}
	}

	public void Flash(CanvasItem target, Color flash, float duration = 0.08f)
	{
		_timeLeft = duration;
		_fallback = target;
		_poly = null;
		_base = target.Modulate;
		target.Modulate = flash;
	}

	public override void _Process(double delta)
	{
		if (_timeLeft <= 0)
			return;
		_timeLeft -= (float)delta;
		if (_timeLeft > 0)
			return;
		if (_poly != null)
			_poly.Color = _base;
		else if (_fallback != null)
			_fallback.Modulate = _base;
		_poly = null;
		_fallback = null;
	}
}

public static class Hitstop
{
	public static async void Pulse(Node host, float seconds = 0.05f)
	{
		if (GameState.Instance.HitstopActive)
			return;
		GameState.Instance.HitstopActive = true;
		Engine.TimeScale = 0.15;
		await host.ToSignal(host.GetTree().CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);
		Engine.TimeScale = 1.0;
		GameState.Instance.HitstopActive = false;
	}
}
