namespace PermitPro.App.ViewModels
{
	public class DashboardViewModel
	{
		public int TotalActive { get; set; }
		public int TotalPending {  get; set; }
		public int TotalApproved { get; set; }
		public int TotalRejected { get; set; }
		public int TotalClosed { get; set; }
		public Guid CompanyId { get; set; }
	}
}
