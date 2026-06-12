using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities.Certificates;

public class ConfinedSpace : EntityBase
{
	public UserInfo? Attendant { get; set; }

	public string? WorkDescription { get; set; }

	public UserInfo? GasTestPerformedBy { get; set; }
	public DateTime? GasTestPerformedWhen { get; set; }

	public string? InitialLELReading { get; set; }

	public string? InitialOxygenReading { get; set; }

	public string? InitialToxicReading { get; set; }

	public string? GasTestReadingJson { get; set; }
}
