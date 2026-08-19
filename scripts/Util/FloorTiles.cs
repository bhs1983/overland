using Godot;

namespace Overland;

/// <summary>
/// TileSet built at runtime from individual town/cold PNGs (PNG bytes, nearest).
/// One atlas source per file. Source id = variant index. No 16-tile wall peering.
/// </summary>
public static class FloorTiles
{
	public const int BrickCount = 6;
	public const int StreetBase = 6;
	public const int StreetCount = 4;
	public const int AshCount = 6;
	public const int FrostBase = 6;
	public const int FrostCount = 2;
	public const int QuenchBase = 8;
	public const int QuenchCount = 2;

	private static TileSet? _town;
	private static TileSet? _cold;

	public static TileSet Town => _town ??= BuildTown();
	public static TileSet Cold => _cold ??= BuildCold();

	public static int BrickSource(int x, int y) => Math.Abs(x + y) % BrickCount;

	public static int StreetSource(int x, int y) => StreetBase + Math.Abs(x + y) % StreetCount;

	public static int AshSource(int x, int y) =>
		(x + y) % 8 == 0
			? FrostBase + Math.Abs(x + y) % FrostCount
			: Math.Abs(x + y) % AshCount;

	public static int QuenchSource(int x, int y) => QuenchBase + Math.Abs(x + y) % QuenchCount;

	private static TileSet BuildTown()
	{
		var ts = NewSet();
		foreach (var ch in "abcdef")
			AddSource(ts, Assets.Town($"brick_floor_{ch}"));
		foreach (var ch in "abcd")
			AddSource(ts, Assets.Town($"street_{ch}"));
		return ts;
	}

	private static TileSet BuildCold()
	{
		var ts = NewSet();
		foreach (var ch in "abcdef")
			AddSource(ts, Assets.ColdStack($"ash_floor_{ch}"));
		foreach (var ch in "ab")
			AddSource(ts, Assets.ColdStack($"frost_ash_{ch}"));
		foreach (var ch in "ab")
			AddSource(ts, Assets.ColdStack($"quench_water_{ch}"));
		return ts;
	}

	private static TileSet NewSet()
	{
		var ts = new TileSet { TileSize = new Vector2I(Tiles.Size, Tiles.Size) };
		return ts;
	}

	private static void AddSource(TileSet ts, Texture2D tex)
	{
		var src = new TileSetAtlasSource
		{
			Texture = tex,
			TextureRegionSize = new Vector2I(tex.GetWidth(), tex.GetHeight())
		};
		src.CreateTile(Vector2I.Zero);
		ts.AddSource(src);
	}
}
