using Godot;
using System.Collections.Generic;

namespace Overland;

/// <summary>Persisted flags and live runtime for Slice 0.</summary>
public partial class GameState : Node
{
	public static GameState Instance { get; private set; } = null!;

	public const int MaxHp = 6;

	public int Hp { get; set; } = MaxHp;
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

	public RoomId CurrentRoom { get; set; } = RoomId.Kilnwalk;
	public RoomId LastSaveRoom { get; set; } = RoomId.Kilnwalk;
	public Vector2 LastSavePosition { get; set; } = new(160, 120);

	public HashSet<string> RoomsEntered { get; } = new();
	public HashSet<string> ClearedAsh { get; } = new();
	public HashSet<string> DefeatedEnemyIds { get; } = new();

	public bool InputLocked { get; set; }
	public bool Paused { get; set; }
	public bool HitstopActive { get; set; }

	public override void _Ready()
	{
		Instance = this;
		RoomsEntered.Add(RoomId.Kilnwalk.ToString());
	}

	public void ResetNewGame()
	{
		Hp = MaxHp;
		HireTaken = false;
		HasCrackiron = false;
		HasFoldedBellows = false;
		HasStackKey = false;
		MouthOpen = false;
		MapMarked = false;
		ClinkerDown = false;
		OverfireDown = false;
		HirePaid = false;
		FanOpened = false;
		IronDoorOpen = false;
		BellowsChestOpened = false;
		StackKeyTaken = false;
		SideFlueHealTaken = false;
		AlcoveHealTaken = false;
		SliceComplete = false;
		CurrentRoom = RoomId.Kilnwalk;
		LastSaveRoom = RoomId.Kilnwalk;
		LastSavePosition = new Vector2(160, 120);
		RoomsEntered.Clear();
		RoomsEntered.Add(RoomId.Kilnwalk.ToString());
		ClearedAsh.Clear();
		DefeatedEnemyIds.Clear();
		InputLocked = false;
		Paused = false;
		HitstopActive = false;
	}

	/// <summary>QA skip: hire + Crackiron, land Stack Mouth from_town (tile y=8).</summary>
	public void ApplyDebugStackMouthStart()
	{
		HireTaken = true;
		HasCrackiron = true;
		Hp = MaxHp;
		MouthOpen = true;
		MapMarked = true;
		CurrentRoom = RoomId.StackMouth;
		LastSaveRoom = RoomId.StackMouth;
		LastSavePosition = new Vector2(10 * Tiles.Size, 8 * Tiles.Size);
		RoomsEntered.Add(RoomId.StackMouth.ToString());
	}

	/// <summary>QA skip: CP3 ready at Dead Fan Walk east — bellows + fan open.</summary>
	public void ApplyDebugCp3Start()
	{
		HireTaken = true;
		HasCrackiron = true;
		HasFoldedBellows = true;
		Hp = MaxHp;
		MouthOpen = true;
		MapMarked = true;
		FanOpened = true;
		BellowsChestOpened = true;
		CurrentRoom = RoomId.DeadFanWalk;
		LastSaveRoom = RoomId.DeadFanWalk;
		LastSavePosition = new Vector2(14 * Tiles.Size, 6 * Tiles.Size);
		RoomsEntered.Add(RoomId.StackMouth.ToString());
		RoomsEntered.Add(RoomId.AshdriftHall.ToString());
		RoomsEntered.Add(RoomId.DeadFanWalk.ToString());
	}

	/// <summary>QA skip: iron open, land Long Drop ready for rooms 9–10.</summary>
	public void ApplyDebugBossStart()
	{
		ApplyDebugCp3Start();
		HasStackKey = true;
		StackKeyTaken = true;
		ClinkerDown = true;
		IronDoorOpen = true;
		CurrentRoom = RoomId.LongDrop;
		LastSaveRoom = RoomId.LongDrop;
		LastSavePosition = new Vector2(10 * Tiles.Size, 15 * Tiles.Size);
		RoomsEntered.Add(RoomId.SettersAlcove.ToString());
		RoomsEntered.Add(RoomId.QuenchTrench.ToString());
		RoomsEntered.Add(RoomId.ClinkerYard.ToString());
		RoomsEntered.Add(RoomId.KeyLanding.ToString());
		RoomsEntered.Add(RoomId.SealedFlue.ToString());
		RoomsEntered.Add(RoomId.LongDrop.ToString());
	}

	public void MarkRoomEntered(RoomId room)
	{
		RoomsEntered.Add(room.ToString());
		CurrentRoom = room;
	}

	public void RecordSave(RoomId room, Vector2 position)
	{
		LastSaveRoom = room;
		LastSavePosition = position;
		CurrentRoom = room;
	}

	public void Heal(int amount)
	{
		Hp = Mathf.Min(MaxHp, Hp + amount);
	}

	public void Damage(int amount)
	{
		Hp = Mathf.Max(0, Hp - amount);
	}
}
