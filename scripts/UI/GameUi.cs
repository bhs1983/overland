using Godot;
using System.Text;

namespace Overland;

public partial class GameUi : CanvasLayer
{
	private HBoxContainer _hpRow = null!;
	private HBoxContainer _itemRow = null!;
	private Label _hud = null!;
	private PanelContainer _dialoguePanel = null!;
	private Label _dialogueName = null!;
	private Label _dialogueBody = null!;
	private Label _toast = null!;
	private Control _pauseMap = null!;
	private Label _mapLabel = null!;
	private bool _dialogueOpen;
	private float _toastTime;

	public override void _Ready()
	{
		AddToGroup("game_ui");
		Layer = 10;

		_hpRow = new HBoxContainer { Position = new Vector2(16, 10) };
		AddChild(_hpRow);

		_itemRow = new HBoxContainer { Position = new Vector2(16, 28) };
		_itemRow.AddThemeConstantOverride("separation", 4);
		AddChild(_itemRow);

		_hud = new Label
		{
			Position = new Vector2(16, 48),
			Size = new Vector2(900, 24)
		};
		_hud.AddThemeFontSizeOverride("font_size", 14);
		_hud.AddThemeColorOverride("font_color", Palette.UiText);
		AddChild(_hud);

		_toast = new Label
		{
			Position = new Vector2(16, 680),
			Size = new Vector2(900, 24),
			Visible = false
		};
		_toast.AddThemeFontSizeOverride("font_size", 14);
		_toast.AddThemeColorOverride("font_color", Palette.Ember);
		AddChild(_toast);

		BuildDialogue();
		BuildPauseMap();
		RefreshHud();
	}

	private void BuildDialogue()
	{
		_dialoguePanel = new PanelContainer
		{
			Visible = false,
			Position = new Vector2(240, 520),
			Size = new Vector2(800, 160)
		};
		var style = new StyleBoxFlat
		{
			BgColor = Palette.UiPanel,
			BorderColor = Palette.UiAccent,
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			ContentMarginLeft = 16,
			ContentMarginRight = 16,
			ContentMarginTop = 12,
			ContentMarginBottom = 12
		};
		_dialoguePanel.AddThemeStyleboxOverride("panel", style);
		var vbox = new VBoxContainer();
		_dialogueName = new Label();
		_dialogueName.AddThemeFontSizeOverride("font_size", 16);
		_dialogueName.AddThemeColorOverride("font_color", Palette.UiAccent);
		_dialogueBody = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_dialogueBody.AddThemeFontSizeOverride("font_size", 14);
		_dialogueBody.AddThemeColorOverride("font_color", Palette.UiText);
		var hint = new Label { Text = "[E / Enter] continue" };
		hint.AddThemeFontSizeOverride("font_size", 11);
		hint.AddThemeColorOverride("font_color", Palette.AshGrey);
		vbox.AddChild(_dialogueName);
		vbox.AddChild(_dialogueBody);
		vbox.AddChild(hint);
		_dialoguePanel.AddChild(vbox);
		AddChild(_dialoguePanel);
	}

	private void BuildPauseMap()
	{
		_pauseMap = new Control { Visible = false };
		_pauseMap.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.75f) };
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_pauseMap.AddChild(dim);

		var panel = new PanelContainer
		{
			Position = new Vector2(320, 120),
			Size = new Vector2(640, 420)
		};
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = Palette.UiPanel,
			BorderColor = Palette.FiredClay,
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			ContentMarginLeft = 20,
			ContentMarginRight = 20,
			ContentMarginTop = 16,
			ContentMarginBottom = 16
		});
		var vbox = new VBoxContainer();
		var title = new Label { Text = "Pause Map — Slice 0" };
		title.AddThemeFontSizeOverride("font_size", 20);
		title.AddThemeColorOverride("font_color", Palette.UiAccent);
		_mapLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_mapLabel.AddThemeFontSizeOverride("font_size", 14);
		_mapLabel.AddThemeColorOverride("font_color", Palette.UiText);
		var help = new Label { Text = "[Esc / M] close\nLoad from title screen." };
		help.AddThemeFontSizeOverride("font_size", 12);
		help.AddThemeColorOverride("font_color", Palette.AshGrey);
		var loadBtn = new Button { Text = "Load Last Save" };
		loadBtn.Pressed += () =>
		{
			if (SaveSystem.Instance.Load())
				GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
			else
				ShowToast("No save found.");
		};
		var titleBtn = new Button { Text = "Return to Title" };
		titleBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Title.tscn");
		vbox.AddChild(title);
		vbox.AddChild(_mapLabel);
		vbox.AddChild(help);
		vbox.AddChild(loadBtn);
		vbox.AddChild(titleBtn);
		panel.AddChild(vbox);
		_pauseMap.AddChild(panel);
		AddChild(_pauseMap);
	}

	public override void _Process(double delta)
	{
		if (_toastTime > 0)
		{
			_toastTime -= (float)delta;
			if (_toastTime <= 0)
				_toast.Visible = false;
		}

		if (_dialogueOpen && Input.IsActionJustPressed("interact"))
			CloseDialogue();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("pause_map"))
			return;

		if (_dialogueOpen)
		{
			CloseDialogue();
			GetViewport().SetInputAsHandled();
			return;
		}

		TogglePauseMap();
		GetViewport().SetInputAsHandled();
	}

	public void RefreshHud()
	{
		foreach (var c in _hpRow.GetChildren())
			c.QueueFree();
		for (int i = 0; i < GameState.Instance.Hp; i++)
		{
			var pip = new TextureRect
			{
				Texture = Assets.Ui("health_pip"),
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				CustomMinimumSize = new Vector2(16, 16),
				StretchMode = TextureRect.StretchModeEnum.Keep
			};
			_hpRow.AddChild(pip);
		}

		foreach (var c in _itemRow.GetChildren())
			c.QueueFree();
		var gs = GameState.Instance;
		void AddItem(string texName)
		{
			_itemRow.AddChild(new TextureRect
			{
				Texture = Assets.Item(texName),
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				CustomMinimumSize = new Vector2(16, 16),
				StretchMode = TextureRect.StretchModeEnum.Keep
			});
		}
		if (gs.HasCrackiron) AddItem("crackiron");
		if (gs.HasFoldedBellows) AddItem("folded_bellows");
		if (gs.HasStackKey) AddItem("stack_key");

		var sb = new StringBuilder();
		if (gs.HireTaken) sb.Append("Hire taken. ");
		if (gs.MapMarked) sb.Append("Mouth marked. ");
		if (gs.MouthOpen) sb.Append("Mouth open. ");
		if (gs.FanOpened) sb.Append("Fan open. ");
		if (gs.ClinkerDown) sb.Append("Clinker down. ");
		if (gs.IronDoorOpen) sb.Append("Iron open. ");
		if (gs.OverfireDown) sb.Append("Overfire down. ");
		if (gs.SliceComplete) sb.Append("Hire paid.");
		_hud.Text = sb.ToString();
	}

	public void ShowDialogue(string name, string body)
	{
		_dialogueOpen = true;
		GameState.Instance.InputLocked = true;
		_dialogueName.Text = name;
		_dialogueBody.Text = body;
		_dialoguePanel.Visible = true;
		RefreshHud();
	}

	public void CloseDialogue()
	{
		_dialogueOpen = false;
		_dialoguePanel.Visible = false;
		GameState.Instance.InputLocked = false;
	}

	public void ShowToast(string text)
	{
		_toast.Text = text;
		_toast.Visible = true;
		_toastTime = 2.4f;
	}

	private void TogglePauseMap()
	{
		var open = !_pauseMap.Visible;
		_pauseMap.Visible = open;
		GameState.Instance.Paused = open;
		if (open)
		{
			var gs = GameState.Instance;
			var here = RoomNames.Display(gs.CurrentRoom);
			var rooms = new StringBuilder();
			rooms.AppendLine($"Kilnwalk{(gs.RoomsEntered.Contains(nameof(RoomId.Kilnwalk)) ? "" : " (locked)")}{(gs.CurrentRoom == RoomId.Kilnwalk ? "  <here>" : "")}");
			void Line(RoomId id)
			{
				var entered = gs.RoomsEntered.Contains(id.ToString());
				rooms.AppendLine($"{RoomNames.Display(id)}{(entered ? "" : " — not yet")}{(gs.CurrentRoom == id ? "  <here>" : "")}");
			}
			Line(RoomId.StackMouth);
			Line(RoomId.AshdriftHall);
			Line(RoomId.DeadFanWalk);
			Line(RoomId.SettersAlcove);
			Line(RoomId.QuenchTrench);
			Line(RoomId.ClinkerYard);
			Line(RoomId.KeyLanding);
			Line(RoomId.SealedFlue);
			Line(RoomId.LongDrop);
			Line(RoomId.OverfireChamber);
			_mapLabel.Text =
				$"{rooms}\n" +
				$"Hire: {(gs.HireTaken ? "yes" : "no")}  Crackiron: {(gs.HasCrackiron ? "yes" : "no")}\n" +
				$"Folded Bellows: {(gs.HasFoldedBellows ? "yes" : "no")}  Fan: {(gs.FanOpened ? "open" : "dead")}\n" +
				$"Stack Key: {(gs.HasStackKey ? "yes" : "no")}  Iron: {(gs.IronDoorOpen ? "open" : "sealed")}\n" +
				$"Mouth: {(gs.MouthOpen ? "open" : "sealed")}  Clinker: {(gs.ClinkerDown ? "down" : "up")}\n" +
				$"Overfire: {(gs.OverfireDown ? "down" : "up")}  Hire paid: {(gs.HirePaid ? "yes" : "no")}\n\n" +
				"Slice 0 — Kilnwalk + Cold Stack rooms 1–10.";
			_ = here;
		}
	}
}
