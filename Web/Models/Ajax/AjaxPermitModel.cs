#nullable disable

namespace PermitPro.App.Models.Ajax;

public class AjaxPermitModel
{
	public string Id { get; set; }
	public string Description { get; set; }
	public string PermitForm { get; set; }
	public IFormFile UploadDocument { get; set; }
}
