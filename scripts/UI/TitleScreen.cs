using Godot;

namespace Overland;

public partial class TitleScreen : Control
{
	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		var bg = new ColorRect { Color = Palette.DeepSoot };
		bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var accent = new ColorRect
		{
			Color = Palette.DarkBrick,
			Position = new Vector2(0, 0),
			Size = new Vector2(1280, 160)
		};
		AddChild(accent);

		var title = new Label
		{
			Text = "OVERLAND",
			Position = new Vector2(80, 40),
			Size = new Vector2(800, 60)
		};
		title.AddThemeFontSizeOverride("font_size", 48);
		title.AddThemeColorOverride("font_color", Palette.KilnOrange);
		AddChild(title);

		var sub = new Label
		{
			Text = "Slice 0 — Checkpoint 1: Kilnwalk (town only)",
			Position = new Vector2(84, 100),
			Size = new Vector2(900, 30)
		};
		sub.AddThemeFontSizeOverride("font_size", 18);
		sub.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(sub);

		var blurb = new Label
		{
			Text = "Walk the ridge. Take the hire. Get Crackiron.\nMouth opens after Tamsin — dungeon is Checkpoint 2.",
			Position = new Vector2(84, 200),
			Size = new Vector2(700, 80)
		};
		blurb.AddThemeFontSizeOverride("font_size", 16);
		blurb.AddThemeColorOverride("font_color", Palette.AshGrey);
		AddChild(blurb);

		var newBtn = MakeButton("New Game", new Vector2(84, 320));
		newBtn.Pressed += () =>
		{
			GameState.Instance.ResetNewGame();
			// Hold Shift: skip Kilnwalk — Stack Mouth from_town with hire + Crackiron.
			if (Input.IsPhysicalKeyPressed(Key.Shift))
				GameState.Instance.ApplyDebugStackMouthStart();
			GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
		};
		AddChild(newBtn);

		var loadBtn = MakeButton("Load Save", new Vector2(84, 380));
		loadBtn.Disabled = !SaveSystem.Instance.HasSave();
		loadBtn.Pressed += () =>
		{
			if (SaveSystem.Instance.Load())
				GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
		};
		AddChild(loadBtn);

		var keys = new Label
		{
			Text = "Keys: WASD/Arrows move · J/Z attack · L/Shift dodge · E/Enter interact · Esc/M pause map\nQA: hold Shift + New Game → Stack Mouth (hire + Crackiron, from_town y=8)",
			Position = new Vector2(84, 460),
			Size = new Vector2(1100, 60)
		};
		keys.AddThemeFontSizeOverride("font_size", 13);
		keys.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(keys);

		var credit = new Label
		{
			Text = "Author of record: Brandon Smith. Designed by AI. Original IP — see LEGAL.md.",
			Position = new Vector2(84, 660),
			Size = new Vector2(1100, 30)
		};
		credit.AddThemeFontSizeOverride("font_size", 12);
		credit.AddThemeColorOverride("font_color", Palette.AshDark);
		AddChild(credit);
	}

	private static Button MakeButton(string text, Vector2 pos)
	{
		return new Button { Text = text, Position = pos, Size = new Vector2(220, 44) };
	}
}
