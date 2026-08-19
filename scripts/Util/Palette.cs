using Godot;

namespace Overland;

/// <summary>Locked Slice 0 32-color table. Hex must match assets/palette.json byte-for-byte.</summary>
public static class Palette
{
	public static readonly Color SootBlack = new("0A090B");
	public static readonly Color DeepSoot = new("1B1613");
	public static readonly Color DarkBrick = new("3C2B21");
	public static readonly Color FiredClay = new("72402C");
	public static readonly Color KilnTerracotta = new("B05C32");
	public static readonly Color KilnOrange = new("DC7A38");
	public static readonly Color Ember = new("ECA45A");
	public static readonly Color CanvasHighlight = new("F2CA8C");
	public static readonly Color AshDark = new("282A2E");
	public static readonly Color AshGrey = new("5C6064");
	public static readonly Color AshLight = new("8E9397");
	public static readonly Color Canvas = new("C6C4BA");
	public static readonly Color WrapBone = new("EAE6DA");
	public static readonly Color ColdDraftDeep = new("163848");
	public static readonly Color ColdDraft = new("3A6C7C");
	public static readonly Color ColdDraftLight = new("7C9EAA");
	public static readonly Color SootVoid = new("110D0B");
	public static readonly Color MidBrick = new("523628");
	public static readonly Color ClayMid = new("945032");
	public static readonly Color KilnBloom = new("F4B464");
	public static readonly Color FireLip = new("F8D8A4");
	public static readonly Color CanvasMid = new("B6B2A8");
	public static readonly Color AshBright = new("9EA4A8");
	public static readonly Color ColdMid = new("224858");
	public static readonly Color BadAir = new("8CB0B8");
	public static readonly Color TerracottaHot = new("C86C38");
	public static readonly Color BrickShadow = new("2C201A");
	public static readonly Color AshWarm = new("726C64");
	public static readonly Color CanvasDust = new("D4A072");
	public static readonly Color HairDeep = new("18120E");
	public static readonly Color Iron = new("3A4046");
	public static readonly Color IronLight = new("6E7478");

	public static readonly Color Floor = DarkBrick;
	public static readonly Color FloorAlt = FiredClay;
	public static readonly Color Brick = KilnTerracotta;
	public static readonly Color BrickDark = DarkBrick;
	public static readonly Color Clay = KilnOrange;
	public static readonly Color Ash = AshGrey;
	public static readonly Color AshDarkColor = AshDark;
	public static readonly Color Soot = SootBlack;
	public static readonly Color Water = ColdDraftDeep;
	public static readonly Color NightFire = KilnOrange;
	public static readonly Color NightFireCore = Ember;

	public static readonly Color PlayerBody = Canvas;
	public static readonly Color PlayerWrap = WrapBone;
	public static readonly Color PlayerAccent = DeepSoot;

	public static readonly Color Telegraph = Ember;
	public static readonly Color HitFlash = WrapBone;
	public static readonly Color HurtFlash = KilnOrange;
	public static readonly Color BellowsPuff = ColdDraftLight;

	public static readonly Color Sootling = AshGrey;
	public static readonly Color Claywalker = KilnTerracotta;
	public static readonly Color ClaywalkerSoft = Ember;
	public static readonly Color Brickleech = FiredClay;
	public static readonly Color Clinker = DeepSoot;
	public static readonly Color ClinkerCrack = Ember;
	public static readonly Color Overfire = KilnOrange;
	public static readonly Color OverfireHot = Ember;

	public static readonly Color NpcTamsin = FiredClay;
	public static readonly Color NpcHolt = DarkBrick;
	public static readonly Color NpcWren = ColdDraft;
	public static readonly Color NpcRook = AshGrey;

	public static readonly Color UiPanel = DeepSoot;
	public static readonly Color UiText = WrapBone;
	public static readonly Color UiAccent = KilnOrange;
}

public static class Tiles
{
	public const int Size = 16;
}
