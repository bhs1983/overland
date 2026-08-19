using System.Collections.Generic;

namespace Overland;

public enum SocketSide
{
	North,
	East,
	South,
	West
}

public enum SocketKind
{
	Standard,
	Gated
}

/// <summary>
/// One authored 2-cell opening. CellA/CellB are the wall indices WorldRoot already uses.
/// N/S cells are X; E/W cells are Y. Do not treat these as “centered on the side.”
/// </summary>
public readonly struct RoomSocket
{
	public RoomId Room { get; init; }
	public SocketSide Side { get; init; }
	public int CellA { get; init; }
	public int CellB { get; init; }
	public int Width => 2;
	public SocketKind Kind { get; init; }
	/// <summary>Gated only: hire, fan, clinker, iron.</summary>
	public string Gate { get; init; }
	public RoomId LeadsTo { get; init; }
}

public readonly struct RoomFootprint
{
	public RoomId Room { get; init; }
	public int W { get; init; }
	public int H { get; init; }
	public bool Terminal { get; init; }
}

/// <summary>
/// Slice 0 socket dump. Future assembler reads this; it does not recenter 14-wide rooms.
/// WorldRoot does not inherit RoomModule and does not consume this at runtime.
/// </summary>
public static class SocketCatalog
{
	public const int WidthTiles = 2;

	public static int WidthPx => WidthTiles * Tiles.Size;

	public static IReadOnlyList<RoomFootprint> Rooms { get; } = new RoomFootprint[]
	{
		Fp(RoomId.Kilnwalk, 20, 15),
		Fp(RoomId.StackMouth, 20, 14),
		Fp(RoomId.AshdriftHall, 20, 14),
		Fp(RoomId.DeadFanWalk, 18, 12),
		Fp(RoomId.SettersAlcove, 16, 14),
		Fp(RoomId.QuenchTrench, 20, 12),
		Fp(RoomId.ClinkerYard, 16, 14),
		Fp(RoomId.KeyLanding, 14, 12),
		Fp(RoomId.SealedFlue, 14, 12),
		Fp(RoomId.LongDrop, 14, 18),
		Fp(RoomId.OverfireChamber, 16, 16, terminal: true),
	};

	public static IReadOnlyList<RoomSocket> All { get; } = new RoomSocket[]
	{
		// Kilnwalk 20×15 — N 9–10 gated (hire / mouth)
		Sock(RoomId.Kilnwalk, SocketSide.North, 9, 10, SocketKind.Gated, "hire", RoomId.StackMouth),

		// Stack Mouth 20×14 — S 9–10, N 9–10
		Sock(RoomId.StackMouth, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.Kilnwalk),
		Sock(RoomId.StackMouth, SocketSide.North, 9, 10, SocketKind.Standard, "", RoomId.AshdriftHall),

		// Ashdrift Hall 20×14 — S 9–10, E (W−1) y 6–7
		Sock(RoomId.AshdriftHall, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.StackMouth),
		Sock(RoomId.AshdriftHall, SocketSide.East, 6, 7, SocketKind.Standard, "", RoomId.DeadFanWalk),

		// Dead Fan Walk 18×12 — W y 5–6, E y 5–6 gated fan, S 8–9 gated fan
		Sock(RoomId.DeadFanWalk, SocketSide.West, 5, 6, SocketKind.Standard, "", RoomId.AshdriftHall),
		Sock(RoomId.DeadFanWalk, SocketSide.East, 5, 6, SocketKind.Gated, "fan", RoomId.SettersAlcove),
		Sock(RoomId.DeadFanWalk, SocketSide.South, 8, 9, SocketKind.Gated, "fan", RoomId.QuenchTrench),

		// Setter's Alcove 16×14 — W y 6–7, S 9–10 (east of center)
		Sock(RoomId.SettersAlcove, SocketSide.West, 6, 7, SocketKind.Standard, "", RoomId.DeadFanWalk),
		Sock(RoomId.SettersAlcove, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.QuenchTrench),

		// Quench Trench 20×12 — N 9–10, E y 6–7, W y 6–7
		Sock(RoomId.QuenchTrench, SocketSide.North, 9, 10, SocketKind.Standard, "", RoomId.SettersAlcove),
		Sock(RoomId.QuenchTrench, SocketSide.East, 6, 7, SocketKind.Standard, "", RoomId.ClinkerYard),
		Sock(RoomId.QuenchTrench, SocketSide.West, 6, 7, SocketKind.Standard, "", RoomId.DeadFanWalk),

		// Clinker Yard 16×14 — W y 6–7, N 9–10 gated clinker
		Sock(RoomId.ClinkerYard, SocketSide.West, 6, 7, SocketKind.Standard, "", RoomId.QuenchTrench),
		Sock(RoomId.ClinkerYard, SocketSide.North, 9, 10, SocketKind.Gated, "clinker", RoomId.KeyLanding),

		// Key Landing 14×12 — S 9–10, N 9–10 near east wall (not recentered)
		Sock(RoomId.KeyLanding, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.ClinkerYard),
		Sock(RoomId.KeyLanding, SocketSide.North, 9, 10, SocketKind.Standard, "", RoomId.SealedFlue),

		// Sealed Flue 14×12 — S 9–10, N 9–10 near east wall, N gated iron
		Sock(RoomId.SealedFlue, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.KeyLanding),
		Sock(RoomId.SealedFlue, SocketSide.North, 9, 10, SocketKind.Gated, "iron", RoomId.LongDrop),

		// Long Drop 14×18 — S 9–10, N 9–10 near east wall
		Sock(RoomId.LongDrop, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.SealedFlue),
		Sock(RoomId.LongDrop, SocketSide.North, 9, 10, SocketKind.Standard, "", RoomId.OverfireChamber),

		// Overfire Chamber 16×16 — S 9–10, terminal
		Sock(RoomId.OverfireChamber, SocketSide.South, 9, 10, SocketKind.Standard, "", RoomId.LongDrop),
	};

	public static IReadOnlyList<RoomSocket> For(RoomId room)
	{
		var list = new List<RoomSocket>();
		foreach (var s in All)
		{
			if (s.Room == room)
				list.Add(s);
		}
		return list;
	}

	public static RoomFootprint Footprint(RoomId room)
	{
		foreach (var r in Rooms)
		{
			if (r.Room == room)
				return r;
		}
		return default;
	}

	public static RoomSocket? Opening(RoomId room, SocketSide side)
	{
		foreach (var s in All)
		{
			if (s.Room == room && s.Side == side)
				return s;
		}
		return null;
	}

	/// <summary>Wall cell index on the opening side: N/W = 0, S = H−1, E = W−1.</summary>
	public static int WallIndex(RoomId room, SocketSide side)
	{
		var fp = Footprint(room);
		return side switch
		{
			SocketSide.North => 0,
			SocketSide.West => 0,
			SocketSide.South => fp.H - 1,
			SocketSide.East => fp.W - 1,
			_ => 0
		};
	}

	private static RoomFootprint Fp(RoomId room, int w, int h, bool terminal = false) =>
		new() { Room = room, W = w, H = h, Terminal = terminal };

	private static RoomSocket Sock(
		RoomId room, SocketSide side, int a, int b, SocketKind kind, string gate, RoomId leadsTo) =>
		new()
		{
			Room = room,
			Side = side,
			CellA = a,
			CellB = b,
			Kind = kind,
			Gate = gate,
			LeadsTo = leadsTo
		};
}
