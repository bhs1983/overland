namespace Overland;

/// <summary>Locked Slice 0 environment lines. Rooms talk; no extra NPC dialogue.</summary>
public static class RoomTalk
{
	public static string? Line(RoomId id) => id switch
	{
		RoomId.StackMouth => "Soot runs down the ceiling. Draft’s reversed.",
		RoomId.AshdriftHall => "Ash banked inward. Stack’s been coughing toward town.",
		RoomId.DeadFanWalk => "Fan seized with clinker. This is why the draft died.",
		RoomId.SettersAlcove => "Tools dropped mid-work. Floor still warm.",
		RoomId.QuenchTrench => "Quench never dumped. They failed to kill the heat here.",
		RoomId.ClinkerYard => "A whole charge fused. The failed firing, standing.",
		RoomId.KeyLanding => "Setter’s ring on the ledge. Heat came from below.",
		RoomId.SealedFlue => "Bolted from this side. Handprints point down.",
		_ => null
	};
}
