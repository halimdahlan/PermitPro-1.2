#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

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
				PermitNumber = $"PTW{e.PermitNo}" ?? "—",
				PermitDescription = GetDescriptionFromJson(e.PermitForm),
				SiteName = e.Site?.Name ?? "—",
				RequestedByName = _dbContext.Users.IgnoreQueryFilters().Select(u => new { Id = u.Id, FullName = $"{u.FirstName} {u.LastName}" }).FirstOrDefault(u => u.Id == e.CreatedBy.ToString()).FullName,
				Status = e.PermitStatus,
				CreatedWhen = e.CreatedWhen
			})
			.ToList();

		// Recent activity from AuditLog (last 5 for this company)
		var recentActivity = _dbContext.AuditLogs
			.Include(e => e.AuditLogUser)
			.ThenInclude(u => u.UserCompany)
			.Where(e => e.LogType == LogTypeEnum.Information && e.AuditLogUser != null && e.AuditLogUser.UserCompany != null && e.AuditLogUser.UserCompany.Id == company)
			.OrderByDescending(e => e.CreatedWhen)
			.Take(3)
			.ToList()
			.Select(e => new DashboardActivityItem
			{
				IconClass = GetActivityIcon(e.Category, e.LogType),
				IconBackground = GetActivityBackground(e.LogType),
				IconColor = GetActivityIconColor(e.LogType),
				Title = GetActivityTitle(e.Category, e.LogType),
				Description = e.Description ?? string.Empty,
				TimeAgo = GetTimeAgo(e.CreatedWhen),
			})
			.ToList();

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
				e.PermitStatus != PermitStatusEnum.KIV &&
				e.PermitStatus != PermitStatusEnum.Closed &&
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

		float pendingPercentage = allPermits > 0 ? (float)pendingPermits / allPermits * 100f : 0f;
		float approvedPercentage = allPermits > 0 ? (float)approvedPermits / allPermits * 100f : 0f;
		float closedPercentage = allPermits > 0 ? (float)closedPermits / allPermits * 100f : 0f;

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

	private static string GetDescriptionFromJson(string json)
	{
		if (string.IsNullOrEmpty(json))
			return string.Empty;

		JObject jsonObj = JObject.Parse(json);
		string value = jsonObj["general"]["description"]?.ToString() ?? string.Empty;

		return value;
	}

	private static string GetActivityIcon(string category, LogTypeEnum logType)
	{
		if (logType == LogTypeEnum.Error)
			return "fa-solid fa-triangle-exclamation";

		return category?.ToUpperInvariant() switch
		{
			"SIGN_IN"         => "fa-solid fa-arrow-right-to-bracket",
			"CREATE_PERMIT"   => "fa-solid fa-circle-plus",
			"UPDATE_PERMIT"   => "fa-solid fa-pen",
			"DELETE_PERMIT"   => "fa-solid fa-trash",
			"PERMIT_APPROVAL" => "fa-solid fa-check",
			"PERMIT_CLOSED"   => "fa-solid fa-lock",
			"PERMIT_SUSPEND"  => "fa-solid fa-pause",
			"FORGOT_PASSWORD" => "fa-solid fa-key",
			"RESET_PASSWORD"  => "fa-solid fa-key",
			"SENDEMAIL"       => "fa-solid fa-envelope",
			"TOKEN"           => "fa-solid fa-shield",
			_                 => "fa-solid fa-circle-info",
		};
	}

	private static string GetActivityBackground(LogTypeEnum logType) => logType switch
	{
		LogTypeEnum.Error   => "#fdeaea",
		LogTypeEnum.Warning => "#fff8e1",
		_                   => "#e8f0fb",
	};

	private static string GetActivityIconColor(LogTypeEnum logType) => logType switch
	{
		LogTypeEnum.Error   => "#e74c3c",
		LogTypeEnum.Warning => "#e67e22",
		_                   => "#2c7be5",
	};

	private static string GetActivityTitle(string category, LogTypeEnum logType)
	{
		if (logType == LogTypeEnum.Error)
		{
			return category?.ToUpperInvariant() switch
			{
				"SIGN_IN"         => "Sign In Failed",
				"FORGOT_PASSWORD" => "Password Reset Error",
				"RESET_PASSWORD"  => "Password Reset Error",
				"PERMIT_SUSPEND"  => "Permit Suspend Failed",
				_                 => "Error",
			};
		}

		return category?.ToUpperInvariant() switch
		{
			"SIGN_IN"         => "User Signed In",
			"CREATE_PERMIT"   => "Permit Created",
			"UPDATE_PERMIT"   => "Permit Updated",
			"DELETE_PERMIT"   => "Permit Deleted",
			"PERMIT_APPROVAL" => "Permit Approved",
			"PERMIT_CLOSED"   => "Permit Closed",
			"PERMIT_SUSPEND"  => "Permit Suspended",
			"FORGOT_PASSWORD" => "Password Reset Requested",
			"RESET_PASSWORD"  => "Password Reset",
			"SENDEMAIL"       => "Email Sent",
			"TOKEN"           => "API Token Issued",
			_                 => category ?? "Activity",
		};
	}

	private static string GetTimeAgo(DateTime createdWhen)
	{
		var diff = DateTime.UtcNow - createdWhen;

		if (diff.TotalMinutes < 1)  return "Just now";
		if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min{((int)diff.TotalMinutes > 1 ? "s" : "")} ago";
		if (diff.TotalHours < 24)   return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours > 1 ? "s" : "")} ago";
		if (diff.TotalDays < 2)     return "Yesterday";
		if (diff.TotalDays < 7)     return $"{(int)diff.TotalDays} days ago";
		return createdWhen.ToLocalTime().ToString("dd MMM yyyy");
	}

	#endregion

}
