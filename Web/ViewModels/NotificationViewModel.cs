#nullable disable

namespace PermitPro.App.ViewModels;

public class NotificationViewModel
{
	public Guid Id { get; set; }

	public string Title { get; set; }

	public string Message { get; set; }

	public string Url { get; set; }

	public string ItemBgColor { get; set; }

	public bool IsRead { get; set; }

	public string CreatedWhen { get; set; }

	public string CheckBoxIds { get; set; }
}
