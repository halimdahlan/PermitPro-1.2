using PermitPro.Core.Enums;

namespace PermitPro.App.ViewModels
{
	public class DashboardViewModel
	{
		public int TotalActive { get; set; }
		public int TotalPending { get; set; }
		public int TotalApproved { get; set; }
		public int TotalRejected { get; set; }
		public int TotalClosed { get; set; }
		public Guid CompanyId { get; set; }

		public List<DashboardChartData> StatusBreakdown { get; set; } = [];
		public List<DashboardLocationData> LocationBreakdown { get; set; } = [];
		public List<DashboardRecentPermit> RecentPermits { get; set; } = [];
		public List<DashboardActivityItem> RecentActivity { get; set; } = [];
	}

	public class DashboardChartData
	{
		public string Status { get; set; } = string.Empty;
		public int Count { get; set; }
		public decimal Percentage { get; set; }
		public string Color { get; set; } = string.Empty;
	}

	public class DashboardLocationData
	{
		public string SiteName { get; set; } = string.Empty;
		public int Count { get; set; }
		public decimal Percentage { get; set; }
	}

	public class DashboardRecentPermit
	{
		public Guid Id { get; set; }
		public string PermitNumber { get; set; } = string.Empty;
		public string PermitDescription { get; set; } = string.Empty;
		public string SiteName { get; set; } = string.Empty;
		public string RequestedByName { get; set; } = string.Empty;
		public PermitStatusEnum Status { get; set; }
		public DateTime CreatedWhen { get; set; }
	}

	public class DashboardActivityItem
	{
		public string IconClass { get; set; } = string.Empty;
		public string IconBackground { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string TimeAgo { get; set; } = string.Empty;
	}
}
