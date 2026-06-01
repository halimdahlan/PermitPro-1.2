#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Interfaces;

using System.Text.Json;

namespace PermitPro.App.Controllers;

public class SitesController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;


	public SitesController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
	}


	[Route("{company}/sites/{parentId?}")]
	public IActionResult Index(Guid company, string parentId = null)
	{
		var sites = _dbContext.Sites
			.Include(f => f.SiteCompany)
			.Where(x => x.IsActive == true && x.SiteType == SiteTypeEnum.Site && x.SiteCompany.Id == company);

		if (parentId == null)
		{
			sites = sites.Where(x => x.ParentId == Guid.Empty);
		}

		var siteList = sites
			.Select(x => new
			{
				x.Id,
				x.Name,
			})
			.ToList();

		siteList.Insert(0, new { Id = Guid.Empty, Name = "(select parent)" });

		ViewBag.SiteList = siteList;


		return View(new SitesViewModel
		{
			CompanyId = company,
			ParentId = parentId,
			SiteParent = parentId != null ? _dbContext.Sites.FirstOrDefault(e => e.Id == Guid.Parse(parentId)) : null,
		});
	}


	public IActionResult Site2()
	{
		return View();
	}


	public IActionResult Edit()
	{
		return View();
	}


	public IActionResult New()
	{
		return View();
	}


	[HttpPost]
	public JsonResult GetSites(Guid? id)
	{
		var locations = _dbContext.Sites
			.Where(e => id.HasValue ? e.ParentId == id : e.ParentId == Guid.Empty)
			.OrderBy(e => e.Name)
			.Select(e => new
			{
				id = e.Id,
				name = e.Name,
				image = $"/img/site-type/16x16/{e.SiteType.ToString().ToLower()}.png",
				siteType = e.SiteType,
				parentId = e.ParentId,
				expanded = e.Name.ToLower() == "iot",
				hasChildren = _dbContext.Sites.Where(f => f.ParentId == e.Id).Count() > 0
			})
			.ToList();

		return Json(locations.ToList());
	}


	[HttpGet("{company}/sites/grid/{parentId?}")]
	public JsonResult GetLocationGridView(Guid company, Guid parentId)
	{
		var data = _dbContext.Sites
			.Include(e => e.SiteCompany)
			.Where(e => e.SiteType == SiteTypeEnum.Site && e.SiteCompany.Id == company && e.ParentId == parentId);

		//if (parentId != Guid.Empty)
		//{
		//	data = data.Where(e => e.ParentId == parentId);
		//}

		var sites = data
			.Select(e => new SitesGridViewModel
			{
				Id = e.Id,
				Name = e.Name,
				Description = e.Description,
				ContactName = e.ContactName,
				ContactEmail = e.ContactEmail,
				IsActive = e.IsActive,
				ParentId = e.ParentId,
			});


		return new JsonResult(sites, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	[HttpGet("{company}/sites/hierarchical")]
	public JsonResult GetLocationsHierarchical(Guid company, Guid? id)
	{
		Guid? parentId = Guid.Empty;
		if (id != null) parentId = id;

		var data = _dbContext.Sites
			.Include(e => e.SiteCompany)
			.Where(e => e.SiteType == SiteTypeEnum.Site && e.SiteCompany.Id == company && e.ParentId == parentId)
			.OrderBy(e => e.ParentId)
			.OrderBy(e => e.Name)
			.Select(e => new
			{
				id = e.Id,
				name = e.Name,
				hasChildren = _dbContext.Sites.Any(f => f.SiteType == SiteTypeEnum.Site && f.SiteCompany.Id == company && f.ParentId == e.Id)
			});

		return new JsonResult(data, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	//[HttpGet("{companyId}/sites/{parentId?}")]
	//public IActionResult GetSites(Guid companyId, string parentId = null)
	//{
	//	var sites = _dbContext.Sites
	//		.Include(e => e.SiteCompany)
	//		.Where(e => e.SiteType == SiteTypeEnum.Site && e.SiteCompany.Id == companyId)
	//		.Select(e => new
	//		{
	//			e.Id,
	//			SiteName = e.Name,
	//			SiteDesc = e.Description,
	//			e.ContactName,
	//			e.ContactEmail,
	//			e.IsActive,
	//			e.ParentId
	//		});

	//	if (parentId != null)
	//	{
	//		sites = sites.Where(e => e.ParentId == Guid.Parse(parentId));
	//	}
	//	else
	//	{
	//		sites = sites.Where(e => e.ParentId == Guid.Empty);
	//	}

	//	return Ok(new
	//	{
	//		Data = sites.ToList()
	//	});
	//}


	[HttpGet("{companyId}/sites/all")]
	public ActionResult<IEnumerable<object>> GetAllSites(Guid companyId)
	{
		var locations = _dbContext.Sites
			.Include(e => e.SiteCompany)
			.Where(e => e.SiteType == SiteTypeEnum.Site && e.IsActive == true && e.SiteCompany.Id == companyId)
			.Select(e => new
			{
				Id = e.Id.ToString().ToLower(),
				e.Name
			})
			.ToList();

		return locations;
	}


	[HttpPost("{companyId}/sites")]
	[ValidateAntiForgeryToken]
	public IActionResult ManageSite(Guid companyId)
	{
		var req = Request.Form;

		try
		{
			//SiteTypeEnum siteType;
			Enum.TryParse(req["SiteType"].ToString(), out SiteTypeEnum siteType);

			if (req["Mode"] == "new")
			{
				Site site = new()
				{
					Name = req["Name"],
					Description = req["Description"],
					ContactEmail = req["Email"],
					ContactName = req["Contact"],
					Latitude = Convert.ToDecimal(req["Latitude"]),
					Longitude = Convert.ToDecimal(req["Longitude"]),
					IsActive = Convert.ToBoolean(req["IsActive"]),
					ShowInMap = Convert.ToBoolean(req["ShowInMap"]),
					SiteType = siteType,
					ParentId = Guid.Parse(req["ParentId"]),
					SiteCompany = _dbContext.Companies.FirstOrDefault(e => e.Id == Guid.Parse(req["CompanyId"])),
				};

				_dbContext.Sites.Add(site);
			}
			else
			{
				var site = _dbContext.Sites.FirstOrDefault(x => x.Id == Guid.Parse(req["Id"]));

				if (site != null)
				{
					site.Name = req["Name"];
					site.Description = req["Description"];
					site.ContactEmail = req["Email"];
					site.ContactName = req["Contact"];
					site.Latitude = Convert.ToDecimal(req["Latitude"]);
					site.Longitude = Convert.ToDecimal(req["Longitude"]);
					site.IsActive = Convert.ToBoolean(req["IsActive"]);
					site.ParentId = Guid.Parse(req["ParentId"]);
					site.ShowInMap = Convert.ToBoolean(req["ShowInMap"]);
					site.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();
				}

				_dbContext.Sites.Update(site);
			}

			_dbContext.SaveChanges();

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				ErrorMessage = ex.Message,
			});
		}
	}


	[HttpDelete("{companyId}/sites/{id}")]
	public async Task<IActionResult> DeleteSite(Guid companyId, Guid id)
	{
		try
		{
			var site = _dbContext.Sites
				.Include(e => e.Permits)
				.FirstOrDefault(x => x.Id == id);

			if (site != null)
			{
				_dbContext.Sites.Remove(site);
				await _dbContext.SaveChangesAsync();

				return Ok(new
				{
					Data = "OK"
				});
			}
			else
			{
				return NotFound(new
				{
					Message = "Unable to find site"
				});
			}

		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				ErrorMessage = ex.Message,
			});
		}
	}


	[HttpGet("{companyId}/sites/info/{id}")]
	public JsonResult GetSite(Guid companyId, Guid id)
	{
		try
		{
			var siteInfo = _dbContext.Sites
				.Select(e => new
				{
					e.Id,
					e.Name,
					e.Description,
					e.SiteType,
					e.ParentId,
					e.IsActive,
					e.ContactName,
					e.ContactEmail,
					e.Latitude,
					e.Longitude,
					e.ShowInMap,
					Parent = _dbContext.Sites
						.Select(s => new
						{
							s.Id,
							s.Name,
							s.Description,
							s.SiteType,
							s.IsActive,
							s.ContactName,
							s.ContactEmail,
						})
						.FirstOrDefault(s => s.Id == e.ParentId)
				})
				.FirstOrDefault(e => e.Id == id);

			// return Ok(new
			// {
			// 	Data = siteInfo
			// });

			return Json(siteInfo);
		}
		catch (Exception ex)
		{
			var errorMessage = ex.Message;
			return Json(null);
		}
	}


	#region "Map API"

	[HttpGet("{companyId}/sites/map/locations")]
	public JsonResult GetPermitsByLocationAsync(Guid companyId, Guid locationId)
	{
		try
		{
			var sites = _dbContext.Sites
				.Include(e => e.Permits)
				.Where(x => x.Permits.Count > 0 && x.Latitude != null && x.Longitude != null && x.ShowInMap == true && x.SiteCompany.Id == companyId)
				.Select(x => new 
				{
					x.Id,
					x.Name,
					x.Latitude,
					x.Longitude,
					Permits = x.Permits.Select(p => new
					{
						p.Id,
						p.PermitNo,
						p.PermitForm,
						p.PermitStatus,
					})
				})
				.ToList();

			return Json(sites);
		}
		catch (Exception ex)
		{
			var errorMessage = ex.Message;
			return Json(null);
		}
	}

	#endregion

}
