using Godot;

namespace Overland;

public static class Palette
{
	// Locked Slice 0 palette (assets/palette.json)
	public static readonly Color SootBlack = new("0B0A0C");
	public static readonly Color DeepSoot = new("1C1714");
	public static readonly Color DarkBrick = new("3A2A22");
	public static readonly Color FiredClay = new("6B3A28");
	public static readonly Color KilnTerracotta = new("A85A32");
	public static readonly Color KilnOrange = new("D4783A");
	public static readonly Color Ember = new("E8A05A");
	public static readonly Color CanvasHighlight = new("F0C98A");
	public static readonly Color AshDark = new("2A2C30");
	public static readonly Color AshGrey = new("5A5E62");
	public static readonly Color AshLight = new("8B9094");
	public static readonly Color Canvas = new("C4C2BA");
	public static readonly Color WrapBone = new("E8E4D8");
	public static readonly Color ColdDraftDeep = new("1A3A48");
	public static readonly Color ColdDraft = new("3D6A78");
	public static readonly Color ColdDraftLight = new("7A9AA4");

	public static readonly Color Floor = DarkBrick;
	public static readonly Color FloorAlt = FiredClay;
	public static readonly Color Brick = KilnTerracotta;
	public static readonly Color BrickDark = DarkBrick;
	public static readonly Color Clay = KilnOrange;
	public static readonly Color Ash = AshGrey;
	public static readonly Color AshDarkColor = AshDark;
	public static readonly Color Soot = SootBlack;
	public static readonly Color Iron = AshDark;
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
