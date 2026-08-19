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

	public static Texture2D Town(string name) => Tex($"res://assets/tiles/town/{name}.png");
	public static Texture2D ColdStack(string name) => Tex($"res://assets/tiles/cold_stack/{name}.png");
	public static Texture2D? ColdStackOrNull(string name)
	{
		var path = $"res://assets/tiles/cold_stack/{name}.png";
		if (Cache.TryGetValue(path, out var cached) && cached != null)
			return cached;
		if (!Godot.FileAccess.FileExists(path))
			return null;
		return Tex(path);
	}
	public static Texture2D Hero(string name) => Tex($"res://assets/sprites/hero/{name}.png");
	public static Texture2D? HeroOrNull(string name)
	{
		var path = $"res://assets/sprites/hero/{name}.png";
		if (Cache.TryGetValue(path, out var cached) && cached != null)
			return cached;
		if (!Godot.FileAccess.FileExists(path))
			return null;
		return Tex(path);
	}
	public static Texture2D Item(string name) => Tex($"res://assets/sprites/items/{name}.png");
	public static Texture2D Enemy(string name) => Tex($"res://assets/sprites/enemies/{name}.png");
	public static Texture2D EnemyV3(string name) =>
		LoadPngNearest($"res://assets/v3/characters/enemies/{name}.png");
	public static Texture2D? EnemyV3OrNull(string name)
	{
		var path = $"res://assets/v3/characters/enemies/{name}.png";
		if (!Godot.FileAccess.FileExists(path))
			return null;
		return LoadPngNearest(path);
	}
	public static Texture2D Npc(string name) =>
		LoadPngNearest($"res://assets/v3/characters/npcs/{name}.png");
	public static Texture2D Vfx(string name) =>
		LoadPngNearest($"res://assets/v3/vfx/{name}.png");
	public static Texture2D Parallax(string theme, string name) =>
		LoadPngNearest($"res://assets/v3/environment/parallax/{theme}/{name}.png",
			repeat: name is "far_bg" or "mid_bg");
	public static Texture2D ParallaxShared(string name) =>
		LoadPngNearest($"res://assets/v3/environment/parallax/shared/{name}.png");
	public static Texture2D Ui(string name) => Tex($"res://assets/ui/{name}.png");

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

	/// <summary>Cold Stack floor — ash_floor when present, else town brick_floor stand-in.</summary>
	public static Sprite2D ColdStackFloorSprite()
	{
		return Sprite(ColdStackOrNull("ash_floor") ?? Town("brick_floor"));
	}
}
