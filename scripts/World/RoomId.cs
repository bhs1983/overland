namespace Overland;

public enum RoomId
{
	Kilnwalk,
	StackMouth,
	AshdriftHall,
	DeadFanWalk,
	SettersAlcove,
	QuenchTrench,
	ClinkerYard,
	KeyLanding,
	SealedFlue,
	LongDrop,
	OverfireChamber,
	SideFlue
}

public static class RoomNames
{
	public static string Display(RoomId id) => id switch
	{
		RoomId.Kilnwalk => "Kilnwalk",
		RoomId.StackMouth => "Stack Mouth",
		RoomId.AshdriftHall => "Ashdrift Hall",
		RoomId.DeadFanWalk => "Dead Fan Walk",
		RoomId.SettersAlcove => "Setter's Alcove",
		RoomId.QuenchTrench => "Quench Trench",
		RoomId.ClinkerYard => "Clinker Yard",
		RoomId.KeyLanding => "Key Landing",
		RoomId.SealedFlue => "Sealed Flue",
		RoomId.LongDrop => "Long Drop",
		RoomId.OverfireChamber => "Overfire Chamber",
		RoomId.SideFlue => "Side Flue",
		_ => id.ToString()
	};
}
