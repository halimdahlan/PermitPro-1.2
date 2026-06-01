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
		var allPermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null)
			.Count();

		var data = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null)
			.GroupBy(e => e.PermitStatus)
			.Select(e => new {
				Status = e.Key.ToString().ToUpper(),
				Count = e.Count(),
			}).ToList();

		var activePermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e =>
				e.Company.Id == company && e.Site != null &&
				e.PermitStatus != PermitStatusEnum.KIV &&
				e.PermitStatus != PermitStatusEnum.Closed &&
				e.PermitStatus != PermitStatusEnum.Suspended &&
				e.PermitStatus != PermitStatusEnum.Draft)
			.Count();

		var pendingPermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null && e.PermitStatus == PermitStatusEnum.Pending)
			.Count();

		var approvedPermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null && e.PermitStatus == PermitStatusEnum.Approved)
			.Count();

		var rejectedPermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null && e.PermitStatus == PermitStatusEnum.Rejected)
			.Count();

		var closedPermits = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.Site != null && e.PermitStatus == PermitStatusEnum.Closed)
			.Count();

		return View(new DashboardViewModel
		{
			TotalActive = activePermits,
			TotalApproved = approvedPermits,
			TotalClosed = closedPermits,
			TotalPending = pendingPermits,
			TotalRejected = rejectedPermits,
			CompanyId = company,
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
