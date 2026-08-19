using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace Overland;

/// <summary>
/// Builds SpriteFrames from hero_atlas.png + hero_atlas.json.
/// Pivot (16,47), fluewalker_* animation names, Nearest, no mipmaps.
/// </summary>
public static class HeroAtlas
{
	public const int PivotX = 16;
	public const int PivotY = 47;
	public const int CellW = 32;
	public const int CellH = 48;

	public static readonly Vector2 PivotOffset = new(-PivotX, -PivotY);

	private static SpriteFrames? _frames;
	private static Texture2D? _atlasTex;
	private static readonly Dictionary<string, AtlasTexture> FrameTextures = new();

	public static SpriteFrames Frames
	{
		get
		{
			_frames ??= Build();
			return _frames;
		}
	}

	public static Texture2D AtlasTexture
	{
		get
		{
			EnsureAtlasLoaded();
			return _atlasTex!;
		}
	}

	public static AtlasTexture? GetFrame(string fluewalkerName)
	{
		_ = Frames;
		return FrameTextures.TryGetValue(fluewalkerName, out var t) ? t : null;
	}

	public static void ApplyPivot(AnimatedSprite2D sprite)
	{
		sprite.Centered = false;
		sprite.Offset = PivotOffset;
		sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		sprite.TextureRepeat = CanvasItem.TextureRepeatEnum.Disabled;
	}

	private static void EnsureAtlasLoaded()
	{
		if (_atlasTex != null)
			return;
		_atlasTex = Assets.LoadPngNearest(
			"res://assets/v3/characters/hero/hero_atlas.png",
			repeat: false);
	}

	private static SpriteFrames Build()
	{
		EnsureAtlasLoaded();
		var frames = new SpriteFrames();
		// Drop the default "default" anim Godot inserts.
		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		var jsonText = Godot.FileAccess.GetFileAsString("res://assets/v3/characters/hero/hero_atlas.json");
		using var doc = JsonDocument.Parse(jsonText);
		var root = doc.RootElement.GetProperty("frames");

		// Group fluewalker_idle_down_01 → anim fluewalker_idle_down, ordered by suffix.
		var groups = new Dictionary<string, List<(int order, string name, JsonElement el)>>();
		foreach (var prop in root.EnumerateObject())
		{
			var name = prop.Name;
			if (!name.StartsWith("fluewalker_"))
				continue;
			var (anim, order) = SplitAnim(name);
			if (!groups.TryGetValue(anim, out var list))
			{
				list = new List<(int, string, JsonElement)>();
				groups[anim] = list;
			}
			list.Add((order, name, prop.Value));
		}

		foreach (var (anim, list) in groups)
		{
			list.Sort((a, b) => a.order.CompareTo(b.order));
			frames.AddAnimation(anim);
			var loop = anim.Contains("_idle_") || anim.Contains("_walk_")
				? SpriteFrames.LoopMode.Linear
				: SpriteFrames.LoopMode.None;
			frames.SetAnimationLoopMode(anim, loop);
			frames.SetAnimationSpeed(anim, anim.Contains("_walk_") ? 10.0 : anim.Contains("_idle_") ? 5.0 : 12.0);
			foreach (var (_, frameName, el) in list)
			{
				var atlas = MakeAtlas(el);
				FrameTextures[frameName] = atlas;
				frames.AddFrame(anim, atlas);
			}
		}

		return frames;
	}

	private static (string anim, int order) SplitAnim(string frameName)
	{
		// fluewalker_hurt_down (no index) → order 1
		// fluewalker_idle_down_01 → anim fluewalker_idle_down, order 1
		// fluewalker_victory_01 → anim fluewalker_victory
		var parts = frameName.Split('_');
		if (parts.Length >= 2 && int.TryParse(parts[^1], out var idx))
		{
			var anim = string.Join('_', parts[..^1]);
			return (anim, idx);
		}
		return (frameName, 1);
	}

	private static AtlasTexture MakeAtlas(JsonElement el)
	{
		var x = el.GetProperty("x").GetInt32();
		var y = el.GetProperty("y").GetInt32();
		var w = el.GetProperty("w").GetInt32();
		var h = el.GetProperty("h").GetInt32();
		return new AtlasTexture
		{
			Atlas = _atlasTex,
			Region = new Rect2(x, y, w, h),
			FilterClip = true
		};
	}
}
