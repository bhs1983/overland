using Godot;
using System.Collections.Generic;

namespace Overland;

/// <summary>Loads locked Slice 0 PNGs from res://assets/ (nearest filter via import).</summary>
public static class Assets
{
	private static readonly Dictionary<string, Texture2D> Cache = new();

	public static Texture2D Tex(string path)
	{
		if (Cache.TryGetValue(path, out var t) && t != null)
			return t;
		// Prefer PNG bytes. Checked-in .import files often point at missing .ctex after a fresh clone.
		if (path.EndsWith(".png") && Godot.FileAccess.FileExists(path))
			return LoadPngNearest(path);
		if (ResourceLoader.Exists(path))
		{
			t = GD.Load<Texture2D>(path);
			if (t != null)
			{
				Cache[path] = t;
				return t;
			}
		}
		Cache[path] = null!;
		return null!;
	}

	/// <summary>
	/// Load PNG bytes into ImageTexture (no mipmaps). Filter/Repeat are applied on the CanvasItem.
	/// Prefer this for v3 assets so Nearest + no-mipmaps hold without editor import.
	/// </summary>
	public static Texture2D LoadPngNearest(string path, bool repeat = false)
	{
		var key = $"{path}|png|{(repeat ? "r" : "n")}";
		if (Cache.TryGetValue(key, out var cached) && cached != null)
			return cached;

		var bytes = Godot.FileAccess.GetFileAsBytes(path);
		var img = new Image();
		var err = img.LoadPngFromBuffer(bytes);
		if (err != Error.Ok)
			GD.PushError($"LoadPngNearest failed {path}: {err}");
		var tex = ImageTexture.CreateFromImage(img);
		Cache[key] = tex;
		Cache[path] = tex;
		return tex;
	}

	public static string Variant(string stem, int x, int y, int n) =>
		$"{stem}_{(char)('a' + Math.Abs(x + y) % n)}";

	public static bool HasNativeTown(string name) =>
		Godot.FileAccess.FileExists($"res://assets/environment/town/{name}.png");

	public static Texture2D Town(string name) =>
		LoadPngNearest($"res://assets/environment/town/{name}.png");

	public static bool HasNativeCold(string name) =>
		Godot.FileAccess.FileExists($"res://assets/environment/cold/{name}.png");

	public static Texture2D ColdStack(string name) =>
		LoadPngNearest($"res://assets/environment/cold/{name}.png");

	public static Texture2D? ColdStackOrNull(string name)
	{
		var path = $"res://assets/environment/cold/{name}.png";
		if (!Godot.FileAccess.FileExists(path))
			return null;
		return LoadPngNearest(path);
	}

	public static Texture2D Prop(string name) =>
		LoadPngNearest($"res://assets/environment/props/{name}.png");

	public static Sprite2D PropSprite(string name) => Sprite(Prop(name));

	public static void ApplyFeetPivot(Sprite2D s, int cellW, int cellH)
	{
		s.Centered = false;
		s.Offset = new Vector2(-cellW / 2, -(cellH - 1));
		s.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
	}

	public static void ApplyFeetPivot(Sprite2D s)
	{
		var w = s.Texture?.GetWidth() ?? 32;
		var h = s.Texture?.GetHeight() ?? 32;
		ApplyFeetPivot(s, w, h);
	}
	public static Texture2D Item(string name) =>
		LoadPngNearest($"res://assets/ui/{name}.png");
	public static Texture2D Enemy(string name) =>
		LoadPngNearest($"res://assets/characters/enemies/{name}.png");
	public static Texture2D Npc(string name) =>
		LoadPngNearest($"res://assets/characters/npcs/{name}.png");
	public static Texture2D Vfx(string name) =>
		LoadPngNearest($"res://assets/vfx/{name}.png");
	public static Texture2D Parallax(string theme, string name) =>
		LoadPngNearest($"res://assets/environment/parallax/{theme}/{name}.png",
			repeat: name is "far_bg" or "mid_bg");
	public static Texture2D ParallaxShared(string name) =>
		LoadPngNearest($"res://assets/environment/parallax/shared/{name}.png");
	public static Texture2D Ui(string name) =>
		LoadPngNearest($"res://assets/ui/{name}.png");

	public static Sprite2D Sprite(Texture2D? tex, Vector2? centeredOffset = null, bool repeat = false)
	{
		var s = new Sprite2D
		{
			Texture = tex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			TextureRepeat = repeat
				? CanvasItem.TextureRepeatEnum.Enabled
				: CanvasItem.TextureRepeatEnum.Disabled,
			Centered = true
		};
		if (centeredOffset.HasValue)
			s.Offset = centeredOffset.Value;
		return s;
	}

	public static Sprite2D TileSprite(string townTileName)
	{
		return Sprite(Town(townTileName));
	}

	public static Sprite2D ColdStackSprite(string name)
	{
		return Sprite(ColdStack(name));
	}

	public static Sprite2D ColdStackFloorSprite() =>
		Sprite(ColdStack(Variant("ash_floor", 0, 0, 6)));
}
