using Godot;

namespace Overland;

/// <summary>
/// Orthographic 2D parallax for Kilnwalk / Cold Stack per assets/ART.md.
/// Far 0.2 Repeat, mid 0.5 Repeat, fog 0.25.
/// No camera-space FG props — 32px lamps/signs at zoom 2 sat on the lens.
/// Main plane (hero/tiles) stays at 1.0 — never parented here. No 3D camera.
/// </summary>
public partial class SliceParallax : Node2D
{
	public const int FarW = 720;
	public const int FarH = 144;
	public const int MidW = 720;
	public const int MidH = 192;
	/// <summary>
	/// Cookie coverage in tiles ≈ TextureScale (32px cookie / 32px tile).
	/// Zoom 2 made the old 5–10 scales read as a lamp on the lens; keep energy, shrink the disc.
	/// </summary>
	public const float CookieLantern = 2.5f;
	public const float CookieKiln = 3f;
	public const float CookieQuench = 3.2f;
	public const float CookieAsh = 4f;
	public const float CookieClinker = 4.5f;
	public const float CookieOverfire = 5f;

	private string _theme = "kilnwalk";
	private Node2D? _fogRoot;
	private float _fogDrift;

	public void SetTheme(string theme)
	{
		_theme = theme is "cold_stack" ? "cold_stack" : "kilnwalk";
		Rebuild();
	}

	public override void _Ready()
	{
		Rebuild();
	}

	public override void _Process(double delta)
	{
		if (_fogRoot == null)
			return;
		_fogDrift += (float)delta * 6f;
		_fogRoot.Position = new Vector2(Mathf.Sin(_fogDrift * 0.15f) * Tiles.Px(0.75f), Mathf.Cos(_fogDrift * 0.1f) * Tiles.Px(0.25f));
	}

	private void Rebuild()
	{
		_fogRoot = null;
		SetProcess(false);
		var kids = GetChildren();
		for (var i = kids.Count - 1; i >= 0; i--)
		{
			var n = kids[i];
			RemoveChild(n);
			n.Free();
		}

		// 16 / 8 were zoom-3 world px. 24 / 12 keep the same screen offset at zoom 2.
		AddTiled("Far", 0.2f, Assets.Parallax(_theme, "far_bg"), tileW: FarW, tileH: FarH, y: 24);
		AddTiled("Mid", 0.5f, Assets.Parallax(_theme, "mid_bg"), tileW: MidW, tileH: MidH, y: 12);

		var fog = new Parallax2D
		{
			Name = "Parallax_Fog",
			ScrollScale = new Vector2(0.25f, 0.25f),
			ZIndex = -5
		};
		_fogRoot = new Node2D { Name = "FogDrift" };
		for (int i = 0; i < 5; i++)
		{
			var w = Assets.Sprite(Assets.ParallaxShared("fog_wisp"));
			w.Position = new Vector2(40 + i * 70, 30 + (i % 3) * 18);
			w.Modulate = new Color(1, 1, 1, 0.55f);
			_fogRoot.AddChild(w);
		}
		fog.AddChild(_fogRoot);
		AddChild(fog);
		SetProcess(true);
	}

	private void AddTiled(string name, float rate, Texture2D tex, int tileW, int tileH, float y)
	{
		var layer = new Parallax2D
		{
			Name = $"Parallax_{name}",
			ScrollScale = new Vector2(rate, rate),
			RepeatSize = new Vector2(tileW, 0),
			RepeatTimes = 4,
			ZIndex = name == "Far" ? -20 : -10
		};
		var spr = new Sprite2D
		{
			Texture = tex,
			Centered = false,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
			RegionEnabled = true,
			RegionRect = new Rect2(0, 0, tileW, tileH),
			Position = new Vector2(0, y),
			Modulate = name == "Far" ? new Color(1, 1, 1, 0.85f) : new Color(0.92f, 0.95f, 1f, 0.9f)
		};
		layer.AddChild(spr);
		AddChild(layer);
	}

	/// <summary>Main-plane tall crown at 1.0× with −4..−8 Y offset. Not a 3D camera.</summary>
	public static Sprite2D TallTop(string theme, Vector2 basePos)
	{
		var name = theme == "kilnwalk" ? "tall_chimney_top" : "tall_flue_top";
		var s = Assets.Sprite(Assets.Parallax(theme, name));
		var yOff = Tiles.Px(-0.25f) - (Mathf.Abs(basePos.X) % 5);
		s.Position = basePos + new Vector2(0, Mathf.Clamp(yOff, Tiles.Px(-0.5f), Tiles.Px(-0.25f)));
		s.ZIndex = 5;
		return s;
	}

	public static PointLight2D Cookie(string theme, string lightName, Vector2 pos, float energy = 1.1f, float scale = CookieLantern)
	{
		var tex = Assets.Parallax(theme, lightName);
		return new PointLight2D
		{
			Texture = tex,
			Position = pos,
			Energy = energy,
			TextureScale = scale,
			ShadowEnabled = false
		};
	}
}
