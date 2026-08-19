using Godot;
using System.Collections.Generic;

namespace Overland;

public partial class GameUi : CanvasLayer
{
	private HBoxContainer _hpRow = null!;
	private HBoxContainer _itemRow = null!;
	private Label _bindAttack = null!;
	private Label _bindBellows = null!;
	private Label _bindUse = null!;
	private PanelContainer _contextPanel = null!;
	private Label _contextLabel = null!;
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

		var topPlate = new ColorRect
		{
			Color = new Color(Palette.SootVoid, 0.78f),
			Position = new Vector2(8, 6),
			Size = new Vector2(430, 50),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(topPlate);

		_hpRow = new HBoxContainer { Position = new Vector2(16, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
		_hpRow.AddThemeConstantOverride("separation", 2);
		AddChild(_hpRow);

		_itemRow = new HBoxContainer { Position = new Vector2(16, 30), MouseFilter = Control.MouseFilterEnum.Ignore };
		_itemRow.AddThemeConstantOverride("separation", 8);
		AddChild(_itemRow);

		BuildContextPrompt();
		BuildControlBar();

		_toast = new Label
		{
			Position = new Vector2(24, 600),
			Size = new Vector2(1232, 24),
			HorizontalAlignment = HorizontalAlignment.Center,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
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

		UpdateContext();
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
			_hpRow.AddChild(new TextureRect
			{
				Texture = Assets.Ui("health_pip"),
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				CustomMinimumSize = new Vector2(16, 16),
				Size = new Vector2(16, 16),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore
			});
		}

		foreach (var c in _itemRow.GetChildren())
			c.QueueFree();
		var gs = GameState.Instance;
		_itemRow.AddChild(ItemSlot("crackiron", "J", "Crackiron", gs.HasCrackiron));
		_itemRow.AddChild(ItemSlot("folded_bellows", "K", "Bellows", gs.HasFoldedBellows));
		_itemRow.AddChild(ItemSlot("stack_key", "", "Stack Key", gs.HasStackKey));

		if (_bindAttack != null)
			_bindAttack.Modulate = gs.HasCrackiron ? Colors.White : new Color(1, 1, 1, 0.35f);
		if (_bindBellows != null)
			_bindBellows.Modulate = gs.HasFoldedBellows ? Colors.White : new Color(1, 1, 1, 0.35f);
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
		_toastTime = 4.5f;
	}

	private void BuildContextPrompt()
	{
		_contextPanel = new PanelContainer
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_contextPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
		_contextPanel.AnchorLeft = 0.5f;
		_contextPanel.AnchorRight = 0.5f;
		_contextPanel.AnchorTop = 1;
		_contextPanel.AnchorBottom = 1;
		_contextPanel.OffsetLeft = -280;
		_contextPanel.OffsetRight = 280;
		_contextPanel.OffsetTop = -108;
		_contextPanel.OffsetBottom = -64;
		_contextPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(Palette.SootVoid, 0.88f),
			BorderColor = Palette.KilnOrange,
			BorderWidthBottom = 2,
			BorderWidthTop = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			ContentMarginLeft = 14,
			ContentMarginRight = 14,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		});
		_contextLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		_contextLabel.AddThemeFontSizeOverride("font_size", 16);
		_contextLabel.AddThemeColorOverride("font_color", Palette.WrapBone);
		_contextPanel.AddChild(_contextLabel);
		AddChild(_contextPanel);
	}

	private void BuildControlBar()
	{
		var bar = new ColorRect
		{
			Color = new Color(Palette.SootVoid, 0.86f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		bar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		bar.AnchorLeft = 0;
		bar.AnchorRight = 1;
		bar.AnchorTop = 1;
		bar.AnchorBottom = 1;
		bar.OffsetLeft = 0;
		bar.OffsetRight = 0;
		bar.OffsetTop = -48;
		bar.OffsetBottom = 0;
		AddChild(bar);

		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		row.AddThemeConstantOverride("separation", 18);
		bar.AddChild(row);

		row.AddChild(BindChip("WASD", "Move"));
		_bindUse = BindChip("E", "Use");
		row.AddChild(_bindUse);
		_bindAttack = BindChip("J/Z", "Attack");
		row.AddChild(_bindAttack);
		_bindBellows = BindChip("K/X", "Bellows");
		row.AddChild(_bindBellows);
		row.AddChild(BindChip("L", "Dodge"));
		row.AddChild(BindChip("Esc", "Map"));
	}

	private static Label BindChip(string key, string action)
	{
		var l = new Label
		{
			Text = $"{key}  {action}",
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		l.AddThemeFontSizeOverride("font_size", 13);
		l.AddThemeColorOverride("font_color", Palette.WrapBone);
		return l;
	}

	private static Control ItemSlot(string tex, string key, string name, bool owned)
	{
		var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 4);
		row.Modulate = owned ? Colors.White : new Color(1, 1, 1, 0.32f);
		row.AddChild(new TextureRect
		{
			Texture = Assets.Item(tex),
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			CustomMinimumSize = new Vector2(16, 16),
			Size = new Vector2(16, 16),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		});
		var caption = string.IsNullOrEmpty(key) ? name : $"{key}  {name}";
		var lab = new Label
		{
			Text = caption,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		lab.AddThemeFontSizeOverride("font_size", 12);
		lab.AddThemeColorOverride("font_color", owned ? Palette.WrapBone : Palette.AshGrey);
		row.AddChild(lab);
		return row;
	}

	private void UpdateContext()
	{
		if (_pauseMap.Visible)
		{
			_contextPanel.Visible = false;
			return;
		}

		if (_dialogueOpen)
		{
			_contextLabel.Text = "[E]  Continue";
			_contextPanel.Visible = true;
			_bindUse.AddThemeColorOverride("font_color", Palette.KilnOrange);
			return;
		}

		_bindUse.AddThemeColorOverride("font_color", Palette.WrapBone);

		var player = PlayerController.Instance;
		var target = player?.PeekInteractable();
		if (target == null)
		{
			_contextPanel.Visible = false;
			return;
		}

		_contextLabel.Text = FormatPrompt(target);
		_contextPanel.Visible = true;
		_bindUse.AddThemeColorOverride("font_color", Palette.KilnOrange);
	}

	private static string FormatPrompt(Interactable it)
	{
		return it switch
		{
			NpcInteractable n => $"[E]  Talk · {n.NpcName}",
			MouthHint when GameState.Instance.MouthOpen => "Walk north into the stack",
			MouthHint => "[E]  Inspect the mouth",
			SavePoint s => s.NightFire ? "[E]  Save at the night fire" : "[E]  Save",
			BellowsChest when GameState.Instance.BellowsChestOpened => "[E]  Empty chest",
			BellowsChest => "[E]  Open chest — Folded Bellows",
			AlcoveHeal when GameState.Instance.AlcoveHealTaken => "[E]  Cool ash",
			AlcoveHeal => "[E]  Rest — heal",
			StackKeyPickup => "[E]  Take Stack Key",
			IronDoorUnlock when GameState.Instance.IronDoorOpen => "[E]  Iron door open",
			IronDoorUnlock => GameState.Instance.HasStackKey
				? "[E]  Unlock with Stack Key"
				: "[E]  Locked — needs Stack Key",
			StairHome => "[E]  Climb back to Kilnwalk",
			_ => $"[E]  {it.Prompt}"
		};
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
