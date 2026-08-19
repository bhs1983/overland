using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Overland;

/// <summary>
/// Headless art gate (allowlist). Fail v3 membership / legal / JPEG / magenta.
/// Warn CP2 tiles/sprites and 16px UI. Skip cookie alpha. Never require packed sheets.
/// </summary>
public partial class QaArtRunner : Node
{
	private enum Mode
	{
		Fail,
		Warn
	}

	private sealed class Row
	{
		public string Glob = "";
		public int? W;
		public int? H;
		public Mode Mode;
		public bool CheckPalette = true;
		public bool CheckAlpha = true;
		public bool CheckSeam;
	}

	private static readonly string[] Banned =
	{
		"hyrule", "zelda", "triforce", "sheikah", "ganon", "hylia", "korok",
		"daggerfall", "bethesda", "whiterun", "septim", "nirn", "daedra", "aedra",
		"whimble", "whimsicle", "master sword", "master_sword", "master-sword", "mastersword"
	};

	// Specific globs first. **/ so they match assets/v3/... now and assets/... after flatten.
	private static readonly Row[] Manifest =
	{
		new() { Glob = "**/characters/hero/hero_atlas.png", W = 338, H = 348, Mode = Mode.Fail },
		new() { Glob = "**/characters/npcs/*.png", W = 32, H = 48, Mode = Mode.Fail },
		new() { Glob = "**/characters/enemies/sootling.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "**/characters/enemies/brickleech.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "**/characters/enemies/claywalker*.png", W = 32, H = 40, Mode = Mode.Fail },
		new() { Glob = "**/characters/enemies/clinker*.png", W = 48, H = 48, Mode = Mode.Fail },
		new() { Glob = "**/characters/enemies/overfire*.png", W = 64, H = 64, Mode = Mode.Fail },
		new() { Glob = "**/vfx/spark.png", W = 16, H = 16, Mode = Mode.Fail },
		new() { Glob = "**/vfx/impacts.png", W = 124, H = 16, Mode = Mode.Fail },
		new() { Glob = "**/far_bg.png", W = 480, H = 96, Mode = Mode.Fail },
		new() { Glob = "**/mid_bg.png", W = 480, H = 128, Mode = Mode.Fail },
		new() { Glob = "**/fog_wisp.png", W = 48, H = 16, Mode = Mode.Fail },
		new() { Glob = "**/light_*.png", W = 32, H = 32, Mode = Mode.Fail, CheckAlpha = false },
		new() { Glob = "**/fg_lamp.png", W = 32, H = 64, Mode = Mode.Fail },
		new() { Glob = "**/fg_pipe.png", W = 32, H = 64, Mode = Mode.Fail },
		new() { Glob = "**/fg_overhang.png", W = 64, H = 32, Mode = Mode.Fail },
		new() { Glob = "**/fg_sign.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "**/tall_*.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "environment/town/*.png", W = 32, H = 32, Mode = Mode.Fail, CheckSeam = true },
		new() { Glob = "environment/cold/*.png", W = 32, H = 32, Mode = Mode.Fail, CheckSeam = true },
		new() { Glob = "environment/props/*.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "**/characters/**/*.png", Mode = Mode.Fail },
		new() { Glob = "**/vfx/**/*.png", Mode = Mode.Fail },
		new() { Glob = "ui/*.png", W = 32, H = 32, Mode = Mode.Fail },
		new() { Glob = "tiles/**", Mode = Mode.Warn, CheckPalette = true, CheckAlpha = true },
		new() { Glob = "sprites/**", Mode = Mode.Warn, CheckPalette = true, CheckAlpha = true },
	};

	private static readonly Dictionary<string, (int W, int H)> SizeOverride = new()
	{
		["stack_mouth_sealed"] = (64, 32),
		["stack_mouth_open"] = (64, 32),
		["kiln"] = (64, 64),
		["iron_door_closed"] = (64, 48),
		["iron_door_open"] = (64, 48),
		["stair"] = (32, 48),
		["door"] = (32, 32),
	};

	private static readonly HashSet<string> SeamNames = new()
	{
		"brick_floor", "street", "ash_floor", "frost_ash", "quench_water"
	};

	private int _fails;
	private int _warns;
	private readonly List<(int R, int G, int B)> _palette = new();

	public override void _Ready()
	{
		CallDeferred(nameof(Run));
	}

	private void Run()
	{
		try
		{
			LoadPalette();
			CheckPaletteCs();
			CheckPalettePng();
			CheckHeroAtlasJson();
			ScanTree("res://assets");
			// Packed sheets are not Slice 0 — absence is success.
			if (Godot.FileAccess.FileExists("res://assets/v3/environment/town_tiles.png")
				|| Godot.FileAccess.FileExists("res://assets/environment/town_tiles.png"))
			{
				Fail("packed town_tiles.png present — Slice 0 uses individuals");
			}

			if (_fails > 0)
			{
				GD.PrintErr($"QA ART FAIL ({_fails} fail, {_warns} warn)");
				GetTree().Quit(1);
				return;
			}

			GD.Print($"QA ART PASS — {_warns} warn (CP2 tiles/sprites/ui expected)");
			GetTree().Quit(0);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr("QA ART crashed: ", ex);
			GetTree().Quit(1);
		}
	}

	private void LoadPalette()
	{
		var txt = Godot.FileAccess.GetFileAsString("res://assets/palette.json");
		Must(!string.IsNullOrEmpty(txt), "palette.json missing");
		using var doc = JsonDocument.Parse(txt);
		var colors = doc.RootElement.GetProperty("colors");
		Must(colors.GetArrayLength() == 32, "palette.json must have 32 colors");
		foreach (var c in colors.EnumerateArray())
		{
			var hx = c.GetProperty("hex").GetString()!.TrimStart('#');
			_palette.Add((
				int.Parse(hx[0..2], System.Globalization.NumberStyles.HexNumber),
				int.Parse(hx[2..4], System.Globalization.NumberStyles.HexNumber),
				int.Parse(hx[4..6], System.Globalization.NumberStyles.HexNumber)));
		}
		GD.Print("OK palette.json — 32 colors");
	}

	private void CheckPaletteCs()
	{
		var cs = Godot.FileAccess.GetFileAsString("res://scripts/Util/Palette.cs");
		Must(!string.IsNullOrEmpty(cs), "Palette.cs missing");
		Must(!cs.Contains("Iron = AshDark"), "Palette.Iron must not alias AshDark");
		var json = Godot.FileAccess.GetFileAsString("res://assets/palette.json");
		using var doc = JsonDocument.Parse(json);
		foreach (var c in doc.RootElement.GetProperty("colors").EnumerateArray())
		{
			var snake = c.GetProperty("name").GetString()!;
			var hx = c.GetProperty("hex").GetString()!.TrimStart('#').ToUpperInvariant();
			var pascal = SnakeToPascal(snake);
			var ok = cs.Contains($"{pascal} = new(\"{hx}\")", System.StringComparison.OrdinalIgnoreCase);
			Must(ok, $"Palette.cs missing {pascal} = new(\"{hx}\")");
		}
		GD.Print("OK Palette.cs — hex matches palette.json");
	}

	private void CheckPalettePng()
	{
		var path = "res://assets/palette.png";
		Must(Godot.FileAccess.FileExists(path), "palette.png missing");
		var img = LoadPng(path);
		if (img == null)
			return;
		Must(img.GetWidth() == 32 && img.GetHeight() == 1, "palette.png must be 32x1");
		for (var x = 0; x < 32; x++)
		{
			var p = img.GetPixel(x, 0);
			var got = (p.R8, p.G8, p.B8);
			Must(got == _palette[x], $"palette.png[{x}] {Hex(got)} != json {Hex(_palette[x])}");
		}
		GD.Print("OK palette.png — 32x1 matches json");
	}

	private void CheckHeroAtlasJson()
	{
		var path = FindFirst("res://assets", "hero_atlas.json");
		if (path == null)
		{
			Fail("hero_atlas.json missing");
			return;
		}
		var txt = Godot.FileAccess.GetFileAsString(path);
		using var doc = JsonDocument.Parse(txt);
		var root = doc.RootElement;
		Must(root.GetProperty("pivot").GetProperty("x").GetInt32() == 16, "atlas pivot.x");
		Must(root.GetProperty("pivot").GetProperty("y").GetInt32() == 47, "atlas pivot.y");
		var cell = root.GetProperty("cell");
		Must(cell[0].GetInt32() == 32 && cell[1].GetInt32() == 48, "atlas cell 32x48");
		foreach (var fr in root.GetProperty("frames").EnumerateObject())
		{
			Must(fr.Name.StartsWith("fluewalker_"), $"atlas frame not fluewalker_*: {fr.Name}");
			Must(BannedToken(fr.Name) == null, $"banned token in atlas frame {fr.Name}");
			Must(fr.Value.GetProperty("w").GetInt32() == 32, $"{fr.Name} w");
			Must(fr.Value.GetProperty("h").GetInt32() == 48, $"{fr.Name} h");
			Must(fr.Value.GetProperty("pivot").GetProperty("x").GetInt32() == 16, $"{fr.Name} pivot.x");
			Must(fr.Value.GetProperty("pivot").GetProperty("y").GetInt32() == 47, $"{fr.Name} pivot.y");
		}
		GD.Print("OK hero_atlas.json — pivot 16,47 cell 32x48");
	}

	private void ScanTree(string dir)
	{
		var files = new List<string>();
		Collect(dir, files);
		files.Sort();
		foreach (var path in files)
		{
			var lower = path.ToLowerInvariant();
			var token = BannedToken(lower);
			if (token != null)
				Fail($"banned token '{token}' in {path}");

			if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
			{
				Fail($"JPEG is an error: {path}");
				continue;
			}

			if (!lower.EndsWith(".png"))
				continue;
			if (lower.EndsWith("palette.png"))
				continue;

			var bytes = Godot.FileAccess.GetFileAsBytes(path);
			if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
			{
				Fail($"JPEG magic in {path}");
				continue;
			}

			var img = LoadPng(path);
			if (img == null)
			{
				Fail($"unreadable PNG {path}");
				continue;
			}

			var magenta = CountMagenta(img);
			if (magenta > 0)
				Fail($"{path}: {magenta} opaque magenta-plate pixels");

			var row = MatchRow(path);
			if (row == null)
				continue;

			var stem = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
			var expectW = row.W;
			var expectH = row.H;
			var npath = path.Replace('\\', '/').ToLowerInvariant();
			if (npath.Contains("/environment/") && SizeOverride.TryGetValue(stem, out var ov))
			{
				expectW = ov.W;
				expectH = ov.H;
			}
			else if (row.Mode == Mode.Warn && expectW == null)
			{
				if (npath.Contains("/sprites/hero/"))
				{
					expectW = 16;
					expectH = 24;
				}
				else if (npath.Contains("/tiles/") || npath.Contains("/sprites/"))
				{
					expectW = 16;
					expectH = 16;
				}
			}

			if (expectW != null && expectH != null && (img.GetWidth() != expectW || img.GetHeight() != expectH))
			{
				Note(row.Mode, $"{path}: size {img.GetWidth()}x{img.GetHeight()} expected {expectW}x{expectH}");
			}

			if (row.CheckPalette)
			{
				var (off, opaque) = CountOffPalette(img);
				if (off > 0)
					Note(row.Mode, $"{path}: {off}/{opaque} opaque pixels off 32-color palette");
			}

			if (row.CheckAlpha)
			{
				var partial = CountPartialAlpha(img);
				if (partial > 0)
					Note(row.Mode, $"{path}: {partial} partial-alpha pixels (binary required)");
			}

			if (row.CheckSeam && row.Mode == Mode.Fail && IsFloorStem(stem))
			{
				var (lr, tb) = SeamScores(img);
				if (lr >= 12f || tb >= 12f)
					Fail($"{path}: 2x2 seam mean Δ lr={lr:0.0} tb={tb:0.0} (limit 12)");
			}
		}
	}

	private static bool IsFloorStem(string stem)
	{
		foreach (var prefix in SeamNames)
		{
			if (stem == prefix || stem.StartsWith(prefix + "_"))
				return true;
		}
		return false;
	}

	private Row? MatchRow(string path)
	{
		foreach (var row in Manifest)
		{
			if (GlobMatch(path, row.Glob))
				return row;
		}
		return null;
	}

	private void Collect(string dir, List<string> files)
	{
		using var d = DirAccess.Open(dir);
		if (d == null)
			return;
		d.ListDirBegin();
		while (true)
		{
			var name = d.GetNext();
			if (name == "")
				break;
			if (name.StartsWith('.'))
				continue;
			var path = dir.TrimEnd('/') + "/" + name;
			if (d.CurrentIsDir())
				Collect(path, files);
			else
				files.Add(path);
		}
	}

	private string? FindFirst(string dir, string fileName)
	{
		var files = new List<string>();
		Collect(dir, files);
		foreach (var f in files)
		{
			if (f.EndsWith("/" + fileName, System.StringComparison.OrdinalIgnoreCase))
				return f;
		}
		return null;
	}

	private Image? LoadPng(string path)
	{
		var bytes = Godot.FileAccess.GetFileAsBytes(path);
		var img = new Image();
		if (img.LoadPngFromBuffer(bytes) != Error.Ok)
			return null;
		if (img.GetFormat() != Image.Format.Rgba8)
			img.Convert(Image.Format.Rgba8);
		return img;
	}

	private int CountMagenta(Image img)
	{
		var n = 0;
		var w = img.GetWidth();
		var h = img.GetHeight();
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var p = img.GetPixel(x, y);
				if (p.A8 == 0)
					continue;
				if (NearMagenta(p.R8, p.G8, p.B8))
					n++;
			}
		}
		return n;
	}

	private static bool NearMagenta(int r, int g, int b)
	{
		return Manh(r, g, b, 255, 0, 255) < 80 || Manh(r, g, b, 255, 0, 170) < 80;
	}

	private (int Off, int Opaque) CountOffPalette(Image img)
	{
		var off = 0;
		var opaque = 0;
		var w = img.GetWidth();
		var h = img.GetHeight();
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var p = img.GetPixel(x, y);
				if (p.A8 == 0)
					continue;
				opaque++;
				if (!InPalette(p.R8, p.G8, p.B8))
					off++;
			}
		}
		return (off, opaque);
	}

	private bool InPalette(int r, int g, int b)
	{
		foreach (var c in _palette)
		{
			if (c.R == r && c.G == g && c.B == b)
				return true;
		}
		return false;
	}

	private static int CountPartialAlpha(Image img)
	{
		var n = 0;
		var w = img.GetWidth();
		var h = img.GetHeight();
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var a = img.GetPixel(x, y).A8;
				if (a != 0 && a != 255)
					n++;
			}
		}
		return n;
	}

	private static (float Lr, float Tb) SeamScores(Image img)
	{
		var w = img.GetWidth();
		var h = img.GetHeight();
		var lr = 0;
		for (var y = 0; y < h; y++)
		{
			var a = img.GetPixel(w - 1, y);
			var b = img.GetPixel(0, y);
			lr += Manh(a.R8, a.G8, a.B8, b.R8, b.G8, b.B8);
		}
		var tb = 0;
		for (var x = 0; x < w; x++)
		{
			var a = img.GetPixel(x, h - 1);
			var b = img.GetPixel(x, 0);
			tb += Manh(a.R8, a.G8, a.B8, b.R8, b.G8, b.B8);
		}
		return (lr / (float)System.Math.Max(h, 1), tb / (float)System.Math.Max(w, 1));
	}

	private static int Manh(int r, int g, int b, int r2, int g2, int b2)
	{
		return System.Math.Abs(r - r2) + System.Math.Abs(g - g2) + System.Math.Abs(b - b2);
	}

	private static bool GlobMatch(string path, string glob)
	{
		var norm = path.Replace('\\', '/');
		if (norm.StartsWith("res://"))
			norm = norm[6..];
		var g = glob.Replace('\\', '/');
		if (!g.StartsWith("**"))
			g = "**/" + g;
		var re = new System.Text.StringBuilder("^");
		for (var i = 0; i < g.Length; i++)
		{
			if (i + 2 < g.Length && g[i] == '*' && g[i + 1] == '*' && g[i + 2] == '/')
			{
				re.Append("(?:.*/)?");
				i += 2;
			}
			else if (i + 1 < g.Length && g[i] == '*' && g[i + 1] == '*')
			{
				re.Append(".*");
				i++;
			}
			else if (g[i] == '*')
				re.Append("[^/]*");
			else
				re.Append(Regex.Escape(g[i].ToString()));
		}
		re.Append('$');
		return Regex.IsMatch(norm, re.ToString(), RegexOptions.IgnoreCase);
	}

	private static string SnakeToPascal(string snake)
	{
		var parts = snake.Split('_');
		var s = "";
		foreach (var p in parts)
		{
			if (p.Length == 0)
				continue;
			s += char.ToUpperInvariant(p[0]) + p[1..];
		}
		return s;
	}

	private static string? BannedToken(string hay)
	{
		foreach (var w in Banned)
		{
			if (hay.Contains(w))
				return w;
		}
		return null;
	}

	private static string Hex((int R, int G, int B) c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";

	private void Note(Mode mode, string msg)
	{
		if (mode == Mode.Fail)
			Fail(msg);
		else
			Warn(msg);
	}

	private void Warn(string msg)
	{
		_warns++;
		GD.Print("WARN: ", msg);
	}

	private void Fail(string msg)
	{
		_fails++;
		GD.PrintErr("FAIL: ", msg);
	}

	private void Must(bool ok, string msg)
	{
		if (!ok)
			Fail(msg);
	}
}
