using Godot;

namespace Overland;

public partial class TitleScreen : Control
{
	/// <summary>Last-frame modifiers — click releases keys before Pressed, so sample here.</summary>
	private bool _shiftLatched;
	private bool _ctrlLatched;
	private bool _altLatched;

	public override void _Process(double delta)
	{
		_shiftLatched = Input.IsPhysicalKeyPressed(Key.Shift);
		_ctrlLatched = Input.IsPhysicalKeyPressed(Key.Ctrl);
		_altLatched = Input.IsPhysicalKeyPressed(Key.Alt);
	}

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
			Text = "Slice 0 — Kilnwalk and the Cold Stack",
			Position = new Vector2(84, 100),
			Size = new Vector2(900, 30)
		};
		sub.AddThemeFontSizeOverride("font_size", 18);
		sub.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(sub);

		var blurb = new Label
		{
			Text = "Walk the ridge. Take the hire. Get Crackiron.\nShut the Overfire. Coin when you come back up.",
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
			// Use latched Shift (last _Process), not IsPhysicalKeyPressed at click.
			var shift = _shiftLatched || Input.IsPhysicalKeyPressed(Key.Shift);
			var alt = _altLatched || Input.IsPhysicalKeyPressed(Key.Alt);
			var ctrl = _ctrlLatched || Input.IsPhysicalKeyPressed(Key.Ctrl);
			if (shift)
			{
				if (alt)
					GameState.Instance.ApplyDebugBossStart();
				else if (ctrl)
					GameState.Instance.ApplyDebugCp3Start();
				else
					GameState.Instance.ApplyDebugStackMouthStart();
			}
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
			Text = "Keys stay on the HUD. WASD move · E use · J attack / hammer · K bellows · L dodge · Esc map\nQA: Shift+New Stack Mouth · Ctrl+Shift+New Dead Fan · Alt+Shift+New Long Drop",
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
