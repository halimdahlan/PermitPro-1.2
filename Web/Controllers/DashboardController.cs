#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.Models.Charts;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Text.Json;

namespace PermitPro.App.Controllers;

public class DashboardController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly ICurrentUserService _currentUserService;

	public DashboardController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ICurrentUserService currentUserService
		, ISystemConfigurationService systemConfigurationService) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
		_currentUserService = currentUserService;

		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		};
	}


	#region "Views"

	public IActionResult Index(Guid company)
	{
		var permits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Include(e => e.Company)
			.Where(e => e.Company.Id == company && e.Site != null)
			.ToList();

		var allCount = permits.Count();

		var activePermits = permits
			.Where(e =>
				e.PermitStatus != PermitStatusEnum.KIV &&
				e.PermitStatus != PermitStatusEnum.Closed &&
				e.PermitStatus != PermitStatusEnum.Suspended &&
				e.PermitStatus != PermitStatusEnum.Draft)
			.Count();

		var pendingPermits = permits.Where(e => e.PermitStatus == PermitStatusEnum.Pending).Count();
		var approvedPermits = permits.Where(e => e.PermitStatus == PermitStatusEnum.Approved).Count();
		var rejectedPermits = permits.Where(e => e.PermitStatus == PermitStatusEnum.Rejected).Count();
		var closedPermits = permits.Where(e => e.PermitStatus == PermitStatusEnum.Closed).Count();

		// Status breakdown for chart
		var statusBreakdown = permits
			.GroupBy(e => e.PermitStatus)
			.Select(g => new DashboardChartData
			{
				Status = g.Key.ToString().ToUpper(),
				Count = g.Count(),
				Percentage = (decimal)(allCount > 0 ? Math.Round((decimal)g.Count() / allCount * 100, 1) : 0),
				Color = GeneralHelper.GetCategoryColor(g.Key)
			})
			.ToList();

		// Location breakdown for chart
		var locationBreakdown = permits
			.GroupBy(e => e.Site.Name)
			.OrderByDescending(g => g.Count())
			.Select(g => new DashboardLocationData
			{
				SiteName = g.Key,
				Count = g.Count(),
				Percentage = (decimal)(allCount > 0 ? Math.Round((decimal)g.Count() / allCount * 100, 1) : 0)
			})
			.Take(7)
			.ToList();

		// Recent permits (last 5)
		var recentPermits = permits
			.OrderByDescending(e => e.CreatedWhen)
			.Take(5)
			.Select(e => new DashboardRecentPermit
			{
				Id = e.Id,
				PermitNumber = e.PermitNo ?? "—",
				PermitType = e.PermitForm ?? "Standard",
				SiteName = e.Site?.Name ?? "—",
				RequestedByName = e.PermitHolderName ?? "—",
				Status = e.PermitStatus,
				CreatedWhen = e.CreatedWhen
			})
			.ToList();

		// Recent activity (simple mock)
		var recentActivity = new List<DashboardActivityItem>
		{
			new() { IconClass = "fa-solid fa-check", IconBackground = "#e8f0fb", Title = "Permit Approved", Description = "PTW-2026-0841 approved by Lead Permit Issuer", TimeAgo = "2 mins ago" },
			new() { IconClass = "fa-solid fa-clock", IconBackground = "#fff8e1", Title = "Permit Submitted", Description = "New confined space permit submitted", TimeAgo = "18 mins ago" },
			new() { IconClass = "fa-solid fa-xmark", IconBackground = "#fdeaea", Title = "Permit Rejected", Description = "PTW-2026-0837 rejected — incomplete docs", TimeAgo = "1 hour ago" },
			new() { IconClass = "fa-solid fa-user-plus", IconBackground = "#f0e8ff", Title = "User Added", Description = "New user Faizal Zin added as Permit Issuer", TimeAgo = "3 hours ago" },
			new() { IconClass = "fa-solid fa-rotate", IconBackground = "#e6f8f1", Title = "Permit Resumed", Description = "PTW-2026-0838 auto-resumed after suspension", TimeAgo = "Yesterday" }
		};

		return View(new DashboardViewModel
		{
			TotalActive = activePermits,
			TotalApproved = approvedPermits,
			TotalClosed = closedPermits,
			TotalPending = pendingPermits,
			TotalRejected = rejectedPermits,
			CompanyId = company,
			StatusBreakdown = statusBreakdown,
			LocationBreakdown = locationBreakdown,
			RecentPermits = recentPermits,
			RecentActivity = recentActivity
		});
	}


	public IActionResult About()
	{
		return View();
	}


	public IActionResult GetDonutChartModels(Guid company)
	{
		var allPermits = _dbContext.Permits
			.Where(e => e.Company.Id == company)
			.Count();

		var activePermits = _dbContext.Permits
			.Where(e =>
				e.Company.Id == company && (
				e.PermitStatus != PermitStatusEnum.KIV ||
				e.PermitStatus != PermitStatusEnum.Closed ||
				e.PermitStatus != PermitStatusEnum.Suspended)
			).Count();

		var pendingPermits = _dbContext.Permits
			.Where(e =>
				e.Company.Id == company &&
				e.PermitStatus == PermitStatusEnum.Pending
		).Count();

		var approvedPermits = _dbContext.Permits
			.Where(e =>
				e.Company.Id == company &&
				e.PermitStatus == PermitStatusEnum.Approved
			).Count();

		var closedPermits = _dbContext.Permits
			.Where(e =>
				e.Company.Id == company &&
				e.PermitStatus == PermitStatusEnum.Closed
			).Count();

		float pendingPercentage = pendingPermits / allPermits;
		float approvedPercentage = approvedPermits / allPermits;
		float closedPercentage = closedPermits / allPermits;

		var data = new DonutChartModel[]
		{
			new DonutChartModel(null, null, null) { Category = "Pending", Value = pendingPercentage, Color = "#f6c343" },
			new DonutChartModel(null, null, null) { Category = "Approved", Value = approvedPercentage, Color = "#00d97e" },
			new DonutChartModel(null, null, null) { Category = "Closed", Value = closedPercentage, Color = "#39afd1" },
		};

		return new JsonResult(data);
	}


	#endregion


	#region "API"

	[HttpGet("{company}/dashboard/charts/donut/permit/status")]
	public IActionResult GetDashboardDonutChartData(Guid company, string startDate, string endDate)
	{
		try
		{
			var allPermits = _dbContext.Permits
				.Include(e => e.Site)
				.Where(e => e.Company.Id == company && e.Site != null)
				.Count();

			var chartData = _dbContext.Permits
				.Include(e => e.Site)
				.Where(e => e.Company.Id == company && e.Site != null);

			if (startDate != null && endDate != null)
			{
				var rangeStart = DateTime.Parse(startDate);
				var rangeEnd = DateTime.Parse(endDate);

				allPermits = _dbContext.Permits
					.Include(e => e.Site)
					.Where(e => e.Company.Id == company && e.Site != null && (e.CreatedWhen >= rangeStart && e.CreatedWhen <= rangeEnd))
					.Count();

				chartData = _dbContext.Permits
					.Include(e => e.Site)
					.Where(e => e.Company.Id == company && e.Site != null && (e.CreatedWhen >= rangeStart && e.CreatedWhen <= rangeEnd));
			}

			var countDonut = chartData.Count();

			var groups = chartData
				.GroupBy(e => e.PermitStatus)
				.Select(e => new
				{
					Category = e.Key.ToString().ToUpper(),
					Value = string.Format("{0:N1}", ((float)e.Count() / (float)allPermits) * 100),
					Count = e.Count(),
					Color = GeneralHelper.GetCategoryColor(e.Key),
				})
				.ToList();

			return new JsonResult(groups, _jsonOptions);
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}


	[HttpGet("{company}/dashboard/charts/bar/permit/location")]
	public IActionResult GetDashboardBarChartDataByLocation(Guid company, string startDate, string endDate)
	{
		try
		{
			var allPermits = _dbContext.Permits
				.Include(e => e.Site)
				.Where(e => e.Company.Id == company && e.Site != null)
				.Count();

			var chartData = _dbContext.Permits
				.Include(e => e.Site)
				.Where(e => e.Company.Id == company && e.Site != null);

			if (startDate != null && endDate != null)
			{
				var rangeStart = DateTime.Parse(startDate);
				var rangeEnd = DateTime.Parse(endDate);

				// allPermits = _dbContext.Permits
				// .Where(e => e.Company.Id == company && (e.CreatedWhen >= rangeStart && e.CreatedWhen <= rangeEnd))
				// .Count();

				chartData = _dbContext.Permits
					.Include(e => e.Site)
					.Where(e => e.Company.Id == company && e.Site != null && (e.CreatedWhen >= rangeStart && e.CreatedWhen <= rangeEnd));
			}

			var countBar = chartData.Count();

			var groups = chartData
				.GroupBy(e => e.Site.Name)
				.Select(e => new BarChartModel
				{
					Category = e.Key.ToString(),
					TotalClosed = e.Where(f => f.PermitStatus == PermitStatusEnum.Closed).Count(),
					TotalPending = e.Where(f => f.PermitStatus == PermitStatusEnum.Pending).Count(),
					TotalActive = e.Where(f => f.PermitStatus != PermitStatusEnum.Draft && f.PermitStatus != PermitStatusEnum.KIV && f.PermitStatus != PermitStatusEnum.Closed && f.PermitStatus != PermitStatusEnum.Suspended).Count(),
				})
				.ToList();

			return new JsonResult(groups, _jsonOptions);
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}

	#endregion


	#region "Private static functions/methods"
	#endregion

}
