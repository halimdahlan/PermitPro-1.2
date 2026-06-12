using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities.Certificates;

public class HotWork : EntityBase
{
	public List<Permit> Permits { get; set; } = new();

	public bool IsCommunicationLinkEstablished { get; set; }

	public bool IsGasTesterCalibrated { get; set; }

	public bool WorkAreaHasBarriers { get; set; }

	public bool IsFireWatchDesignated { get; set; }

	public bool IsAreaSafe { get; set; }

	public bool IsAreaSparkSafe { get; set; }

	public bool IsFireFightingMaterialsPresent { get; set; }

	public string? IgnitionSources { get; set; }

	public UserInfo? GasTestPerformedBy { get; set; }

	public DateTime? GasTestPerformedWhen { get; set; }

	public string? InitialLELReading { get; set; }

	public string? LELTestFrequency { get; set; }

	public string? LELTestReadingJson { get; set; }

	public string? AtmosReadingJson { get; set; }

	public DateTime? WorkValidityFrom { get; set; }

	public DateTime? WorkValidityTo { get; set; }

	public DateTime? LeadPermitIssuerSignedWhen { get; set; }
	public UserInfo? LeadPermitIssuerSignedBy { get; set; }

	public DateTime? PermitIssuerSignedWhen { get; set; }
	public UserInfo? PermitIssuerSignedBy { get; set; }

	public DateTime? PermitWithdrawnWhen { get; set; }
	public UserInfo? PermitWithdrawnBy { get; set; }

}
