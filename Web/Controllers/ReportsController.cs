#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using PermitPro.App.Controllers.Base;
using PermitPro.App.Models.Reports;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Text.Json;

namespace PermitPro.App.Controllers;

[Authorize]
public class ReportsController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;


	public ReportsController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
	}


	#region "GET"

	public IActionResult Index(Guid company)
	{
		var model = new ReportsViewModel
		{
			CompanyId = company,
			GridParamaters = new RouteValueDictionary { { "company", company } }
		};

		return View(model);
	}


	public JsonResult GetDropdownMonths()
	{
		var dtm = new DateTime(2024, 1, 1);

		List<dynamic> months = new(){
			new { Text = "(select)", Value = "" }
		};

		for (var n = 1; n < 13; n++)
		{
			months.Add(new
			{
				Text = dtm.ToString("MMMM"),
				Value = dtm.ToString("MMMM"),
			});

			dtm = dtm.AddMonths(1);
		}

		return Json(months);
	}


	public JsonResult GetDropdownYears()
	{
		var startYear = 2023;
		var endYear = DateTime.Now.Year + 1;

		List<dynamic> years = new()
		{
			new { Text = "(select)", Value = "" }
		};

		for (var n = startYear; n < endYear; n++)
		{
			years.Add(new
			{
				Text = n,
				Value = n,
			});
		}

		return Json(years);
	}


	public JsonResult GetDropdownCertificates()
	{
		var certificates = new[]
		{
			new { Text = "(select)", Value = "" },
			new { Text = "Hot Work", Value = "A" },
			new { Text = "Confined Space", Value = "B" },
			new { Text = "Radiation", Value = "C" },
			new { Text = "Excavation", Value = "D" },
			new { Text = "Isolation", Value = "E" },
			new { Text = "Method Statement", Value = "F" },
			new { Text = "Lifting & Hoisting", Value = "G" },
			new { Text = "Override", Value = "H" },
		};

		return Json(certificates);
	}


	public JsonResult GetDropdownPermitStatus()
	{
		var certificates = new[]
		{
			new { Text = "(select)", Value = "" },
			new { Text = "Draft", Value = "0" },
			new { Text = "Pending", Value = "1" },
			new { Text = "Approved", Value = "2" },
			new { Text = "Rejected", Value = "3" },
			new { Text = "Suspended", Value = "4" },
			new { Text = "Closed", Value = "6" },
		};

		return Json(certificates);
	}


	public JsonResult GetReportGrid(Guid company)
	{
		var permits = _dbContext.Permits
			.Include(e => e.Site)
			.Where(e => e.Company.Id == company && e.PermitWorkflowStep != null && e.Site != null)
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new
			{
				e.Id,
				PermitHolderId = e.CreatedBy.ToString(),
				PermitHolderName = _dbContext.Users.Select(u => new { UserId = u.Id, FullName = $"{u.FirstName} {u.LastName}" }).FirstOrDefault(n => n.UserId == e.CreatedBy.ToString().ToLower()).FullName, //_dbContext.Users.Select(u => new { u.Id, FullName = string.Format("{0} {1}", u.FirstName, u.LastName) }).FirstOrDefault(f => f.Id == e.CreatedBy.ToString()).FullName,
				PermitNo = string.Format("PTW{0:000000}", e.RunningNumber),
				Location = e.Site.Name,
				PermitStatus = e.PermitStatus.ToString(),
				e.PermitForm,
				e.CreatedWhen,
				LocationId = e.Site.Id,
				PermitStatusEnum = e.PermitStatus,
			})
			.AsEnumerable();

		//if (await _currentUserService.IsLeadPermitIssuer() || await _currentUserService.IsLeadPermitIssuer())
		//{
		//	permits = permits.Where(e => e.PermitHolderId.ToString() == _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
		//}

		List<ReportGridModel> list = new();

		if (permits != null)
		{
			foreach (var permit in permits)
			{
				JObject json = JObject.Parse(permit.PermitForm);

				var loc = json["general"]["location"];
				var dtmStart = (JValue)json["general"]["startDateTime"];
				var dtmEnd = (JValue)json["general"]["endDateTime"];

				List<JToken> tmpCerts = json["general"]["certificates"].Children().ToList();
				List<dynamic> certs = new();

				foreach (var cert in tmpCerts)
				{
					var certLetter = "A";

					if (cert["name"].ToString() == "hotwork") certLetter = "A";
					if (cert["name"].ToString() == "confinedspace") certLetter = "B";
					if (cert["name"].ToString() == "radiation") certLetter = "C";
					if (cert["name"].ToString() == "excavation") certLetter = "D";
					if (cert["name"].ToString() == "isolation") certLetter = "E";
					if (cert["name"].ToString() == "methodStatement") certLetter = "F";
					if (cert["name"].ToString() == "liftingHoisting") certLetter = "G";
					if (cert["name"].ToString() == "override") certLetter = "H";

					certs.Add(certLetter);
				}

				var dtm = GeneralHelper.GetDateInTimeZone(permit.CreatedWhen);

				var data = new ReportGridModel
				{
					Id = permit.Id,
					PermitNo = permit.PermitNo,
					PermitHolderName = permit.PermitHolderName,
					Location = permit.Location,
					PermitStatus = permit.PermitStatus,
					Certificates = string.Join(", ", certs),
					CreatedWhen = new DateTime(dtm.Year, dtm.Month, dtm.Day),
					CreatedWhenString = dtm.ToString("dd MMMM yyyy"),
					CreatedMonth = dtm.ToString("MMMM"),
					CreatedYear = Convert.ToInt16(dtm.ToString("yyyy")),
					LocationId = permit.LocationId.ToString(),
					PermitStatusEnum = (int)permit.PermitStatusEnum,
				};

				if (dtmStart.Value != null) data.StartDate = DateTime.Parse(dtmStart.ToString()).ToLocalTime();
				if (dtmEnd.Value != null) data.EndDate = DateTime.Parse(dtmEnd.ToString()).ToLocalTime();

				list.Add(data);
			}
		}

		return new JsonResult(list, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}

	#endregion


	#region "POST"
	#endregion


	#region "PUT"
	#endregion


	#region "DELETE"
	#endregion


	#region "Private functions/methods"
	#endregion

}
