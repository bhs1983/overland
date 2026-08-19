using Godot;
using System.Collections.Generic;
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
	private Control _mapGraph = null!;
	private Label _mapStatus = null!;
	private bool _dialogueOpen;
	private float _toastTime;

	private static readonly (RoomId Id, Vector2 Pos)[] MapNodes =
	{
		(RoomId.Kilnwalk, new Vector2(280, 24)),
		(RoomId.StackMouth, new Vector2(280, 64)),
		(RoomId.AshdriftHall, new Vector2(280, 104)),
		(RoomId.DeadFanWalk, new Vector2(400, 104)),
		(RoomId.SettersAlcove, new Vector2(400, 144)),
		(RoomId.QuenchTrench, new Vector2(280, 144)),
		(RoomId.ClinkerYard, new Vector2(400, 184)),
		(RoomId.KeyLanding, new Vector2(400, 224)),
		(RoomId.SealedFlue, new Vector2(400, 264)),
		(RoomId.LongDrop, new Vector2(400, 304)),
		(RoomId.OverfireChamber, new Vector2(400, 344)),
	};

	private static readonly (RoomId A, RoomId B)[] MapEdges =
	{
		(RoomId.Kilnwalk, RoomId.StackMouth),
		(RoomId.StackMouth, RoomId.AshdriftHall),
		(RoomId.AshdriftHall, RoomId.DeadFanWalk),
		(RoomId.DeadFanWalk, RoomId.SettersAlcove),
		(RoomId.DeadFanWalk, RoomId.QuenchTrench),
		(RoomId.SettersAlcove, RoomId.QuenchTrench),
		(RoomId.QuenchTrench, RoomId.ClinkerYard),
		(RoomId.ClinkerYard, RoomId.KeyLanding),
		(RoomId.KeyLanding, RoomId.SealedFlue),
		(RoomId.SealedFlue, RoomId.LongDrop),
		(RoomId.LongDrop, RoomId.OverfireChamber),
	};

	public override void _Ready()
	{
		AddToGroup("game_ui");
		Layer = 10;

		_hpRow = new HBoxContainer { Position = new Vector2(16, 10) };
		_hpRow.AddThemeConstantOverride("separation", 2);
		AddChild(_hpRow);

		_itemRow = new HBoxContainer { Position = new Vector2(16, 30) };
		_itemRow.AddThemeConstantOverride("separation", 2);
		AddChild(_itemRow);

		_hud = new Label
		{
			Position = new Vector2(16, 50),
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

		var panel = new Panel
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
			BorderWidthRight = 2
		});
		var title = new Label
		{
			Text = "Pause Map — Slice 0",
			Position = new Vector2(16, 4),
			Size = new Vector2(400, 20)
		};
		title.AddThemeFontSizeOverride("font_size", 16);
		title.AddThemeColorOverride("font_color", Palette.UiAccent);
		panel.AddChild(title);

		_mapGraph = new Control
		{
			Position = Vector2.Zero,
			Size = new Vector2(640, 380)
		};
		panel.AddChild(_mapGraph);

		_mapStatus = new Label
		{
			Position = new Vector2(16, 372),
			Size = new Vector2(400, 20)
		};
		_mapStatus.AddThemeFontSizeOverride("font_size", 11);
		_mapStatus.AddThemeColorOverride("font_color", Palette.UiText);
		panel.AddChild(_mapStatus);

		var help = new Label
		{
			Text = "[Esc / M] close",
			Position = new Vector2(16, 392),
			Size = new Vector2(200, 20)
		};
		help.AddThemeFontSizeOverride("font_size", 11);
		help.AddThemeColorOverride("font_color", Palette.AshGrey);
		panel.AddChild(help);

		var loadBtn = new Button { Text = "Load Last Save", Position = new Vector2(360, 388), Size = new Vector2(130, 24) };
		loadBtn.Pressed += () =>
		{
			if (SaveSystem.Instance.Load())
				GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
			else
				ShowToast("No save found.");
		};
		var titleBtn = new Button { Text = "Return to Title", Position = new Vector2(500, 388), Size = new Vector2(130, 24) };
		titleBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Title.tscn");
		panel.AddChild(loadBtn);
		panel.AddChild(titleBtn);

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
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale
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
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale
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
			RebuildPauseGraph();
	}

	private void RebuildPauseGraph()
	{
		foreach (var c in _mapGraph.GetChildren())
			c.QueueFree();

		var gs = GameState.Instance;
		var pos = new Dictionary<RoomId, Vector2>();
		foreach (var (id, p) in MapNodes)
			pos[id] = p;

		foreach (var (a, b) in MapEdges)
		{
			var line = new Line2D
			{
				Width = 2,
				DefaultColor = Palette.AshGrey,
				Antialiased = false
			};
			line.AddPoint(pos[a] + new Vector2(16, 16));
			line.AddPoint(pos[b] + new Vector2(16, 16));
			_mapGraph.AddChild(line);
		}

		foreach (var (id, p) in MapNodes)
		{
			if (id == RoomId.SideFlue)
				continue;
			var entered = gs.RoomsEntered.Contains(id.ToString());
			var here = gs.CurrentRoom == id;
			var icon = id == RoomId.Kilnwalk ? "map_node_town" : "map_node_room";
			var node = new TextureRect
			{
				Texture = Assets.Ui(icon),
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				Position = p,
				CustomMinimumSize = new Vector2(32, 32),
				Size = new Vector2(32, 32),
				Modulate = entered || id == RoomId.Kilnwalk ? Colors.White : new Color(1, 1, 1, 0.35f),
				StretchMode = TextureRect.StretchModeEnum.Keep
			};
			_mapGraph.AddChild(node);
			if (here)
			{
				_mapGraph.AddChild(new TextureRect
				{
					Texture = Assets.Ui("map_node_here"),
					TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
					Position = p,
					CustomMinimumSize = new Vector2(32, 32),
					Size = new Vector2(32, 32),
					StretchMode = TextureRect.StretchModeEnum.Keep
				});
			}
			var name = new Label
			{
				Text = RoomNames.Display(id),
				Position = new Vector2(p.X - 120, p.Y + 8),
				Size = new Vector2(116, 16),
				HorizontalAlignment = HorizontalAlignment.Right
			};
			name.AddThemeFontSizeOverride("font_size", 11);
			name.AddThemeColorOverride("font_color", here ? Palette.UiAccent : Palette.UiText);
			_mapGraph.AddChild(name);
		}

		_mapStatus.Text =
			$"Hire {(gs.HireTaken ? "yes" : "no")}  Mouth {(gs.MouthOpen ? "open" : "sealed")}  " +
			$"Fan {(gs.FanOpened ? "open" : "dead")}  Iron {(gs.IronDoorOpen ? "open" : "sealed")}";
	}
}
