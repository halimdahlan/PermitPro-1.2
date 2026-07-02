#nullable disable

namespace PermitPro.App.Models.Ajax;

public class PermitCertificateUpdateRequest
{
	public Guid PermitId { get; set; }
	public Guid CompanyId { get; set; }
	public string PermitForm { get; set; }
}
