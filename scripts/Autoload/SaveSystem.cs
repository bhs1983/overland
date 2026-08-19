using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Overland;

public partial class SaveSystem : Node
{
	public static SaveSystem Instance { get; private set; } = null!;

	public const int Version = 2;
	private const string SavePath = "user://slice0_save.json";

	public override void _Ready()
	{
		Instance = this;
	}

	public bool HasSave()
	{
		return Godot.FileAccess.FileExists(SavePath);
	}

	public void Save()
	{
		var gs = GameState.Instance;
		var data = new SaveData
		{
			Version = Version,
			Hp = gs.Hp,
			HireTaken = gs.HireTaken,
			HasCrackiron = gs.HasCrackiron,
			HasFoldedBellows = gs.HasFoldedBellows,
			HasStackKey = gs.HasStackKey,
			MouthOpen = gs.MouthOpen,
			MapMarked = gs.MapMarked,
			ClinkerDown = gs.ClinkerDown,
			OverfireDown = gs.OverfireDown,
			HirePaid = gs.HirePaid,
			FanOpened = gs.FanOpened,
			IronDoorOpen = gs.IronDoorOpen,
			BellowsChestOpened = gs.BellowsChestOpened,
			StackKeyTaken = gs.StackKeyTaken,
			SideFlueHealTaken = gs.SideFlueHealTaken,
			AlcoveHealTaken = gs.AlcoveHealTaken,
			SliceComplete = gs.SliceComplete,
			CurrentRoom = gs.LastSaveRoom.ToString(),
			SaveX = gs.LastSavePosition.X,
			SaveY = gs.LastSavePosition.Y,
			RoomsEntered = new List<string>(gs.RoomsEntered),
			ClearedAsh = new List<string>(gs.ClearedAsh),
			DefeatedEnemyIds = new List<string>(gs.DefeatedEnemyIds)
		};

		var json = JsonSerializer.Serialize(data);
		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
		file?.StoreString(json);
		GD.Print("Saved to ", SavePath);
	}

	public bool Load()
	{
		if (!HasSave())
			return false;

		using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
		if (file == null)
			return false;

		try
		{
			var data = JsonSerializer.Deserialize<SaveData>(file.GetAsText());
			if (data == null)
				return false;
			if (data.Version < Version)
			{
				GD.Print("Save version too old, ignoring");
				return false;
			}

			var gs = GameState.Instance;
			gs.ResetNewGame();
			gs.Hp = data.Hp;
			gs.HireTaken = data.HireTaken;
			gs.HasCrackiron = data.HasCrackiron;
			gs.HasFoldedBellows = data.HasFoldedBellows;
			gs.HasStackKey = data.HasStackKey;
			gs.MouthOpen = data.MouthOpen;
			gs.MapMarked = data.MapMarked;
			gs.ClinkerDown = data.ClinkerDown;
			gs.OverfireDown = data.OverfireDown;
			gs.HirePaid = data.HirePaid;
			gs.FanOpened = data.FanOpened;
			gs.IronDoorOpen = data.IronDoorOpen;
			gs.BellowsChestOpened = data.BellowsChestOpened;
			gs.StackKeyTaken = data.StackKeyTaken;
			gs.SideFlueHealTaken = data.SideFlueHealTaken;
			gs.AlcoveHealTaken = data.AlcoveHealTaken;
			gs.SliceComplete = data.SliceComplete;

			if (Enum.TryParse<RoomId>(data.CurrentRoom, out var room))
			{
				gs.LastSaveRoom = room;
				gs.CurrentRoom = room;
			}

			gs.LastSavePosition = new Vector2(data.SaveX, data.SaveY);
			gs.RoomsEntered.Clear();
			foreach (var r in data.RoomsEntered)
				gs.RoomsEntered.Add(r);
			gs.ClearedAsh.Clear();
			foreach (var a in data.ClearedAsh)
				gs.ClearedAsh.Add(a);
			gs.DefeatedEnemyIds.Clear();
			foreach (var e in data.DefeatedEnemyIds)
				gs.DefeatedEnemyIds.Add(e);

			return true;
		}
		catch (Exception ex)
		{
			GD.PrintErr("Load failed: ", ex.Message);
			return false;
		}
	}

	private sealed class SaveData
	{
		public int Version { get; set; }
		public int Hp { get; set; }
		public bool HireTaken { get; set; }
		public bool HasCrackiron { get; set; }
		public bool HasFoldedBellows { get; set; }
		public bool HasStackKey { get; set; }
		public bool MouthOpen { get; set; }
		public bool MapMarked { get; set; }
		public bool ClinkerDown { get; set; }
		public bool OverfireDown { get; set; }
		public bool HirePaid { get; set; }
		public bool FanOpened { get; set; }
		public bool IronDoorOpen { get; set; }
		public bool BellowsChestOpened { get; set; }
		public bool StackKeyTaken { get; set; }
		public bool SideFlueHealTaken { get; set; }
		public bool AlcoveHealTaken { get; set; }
		public bool SliceComplete { get; set; }
		public string CurrentRoom { get; set; } = "Kilnwalk";
		public float SaveX { get; set; }
		public float SaveY { get; set; }
		public List<string> RoomsEntered { get; set; } = new();
		public List<string> ClearedAsh { get; set; } = new();
		public List<string> DefeatedEnemyIds { get; set; } = new();
	}
}
