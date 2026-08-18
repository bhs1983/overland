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
		t = GD.Load<Texture2D>(path);
		Cache[path] = t;
		return t;
	}

	public static Texture2D Town(string name) => Tex($"res://assets/tiles/town/{name}.png");
	public static Texture2D Hero(string name) => Tex($"res://assets/sprites/hero/{name}.png");
	public static Texture2D Item(string name) => Tex($"res://assets/sprites/items/{name}.png");
	public static Texture2D Ui(string name) => Tex($"res://assets/ui/{name}.png");

	public static Sprite2D Sprite(Texture2D? tex, Vector2? centeredOffset = null)
	{
		var s = new Sprite2D
		{
			Texture = tex,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
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
}
