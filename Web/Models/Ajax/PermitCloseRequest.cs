#nullable disable

namespace PermitPro.App.Models.Ajax;

public class PermitCloseRequest
{
	public Guid PermitId { get; set; }
	public string Notes { get; set; }
}
