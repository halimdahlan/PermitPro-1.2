#nullable disable

using Microsoft.AspNetCore.Http;

namespace PermitPro.App.Models.Ajax;

public class PermitCreateRequest
{
	public Guid CompanyId { get; set; }
	public string PermitForm { get; set; }
	public string SubmissionType { get; set; }
	public IFormFileCollection Files { get; set; }
}
