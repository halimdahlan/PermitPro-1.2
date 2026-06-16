using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.Models.Ajax;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers;

[Authorize]
public class NotificationsController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly ICurrentUserService _currentUserService;


	public NotificationsController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
		, ICurrentUserService currentUserService
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
		_currentUserService = currentUserService;
	}


	public IActionResult Index()
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var notifications = _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.Where(e => e.IsRead == false && e.NotificationUser == currentUser)
			.OrderByDescending(e => e.CreatedWhen)
			.Take(5)
			.Select(e => new NotificationViewModel
			{
				Id = e.Id,
				Message = e.Message,
				Url = e.Url,
				IsRead = e.IsRead,
				CreatedWhen = string.Format("{0} at {1}", GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("dd MMM, yyyy"), GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("hh:mm tt")),
			})
			.AsEnumerable();

		return View(notifications);
	}


	public IActionResult ListAll()
	{
		return View();
	}


	[HttpPut("{company}/notifications/read")]
	public async Task<IActionResult> UpdateNotificationAsRead(AjaxNotificationRequest request)
	{
		if (request.Selected != null && request.Selected != string.Empty)
		{
			var tmp = request.Selected.Split(";");

			List<Notification> markAsRead = new();
			List<string> markSuccess = new();

			foreach (var item in tmp)
			{
				var notification = _dbContext.Notifications.SingleOrDefault(e => e.Id.ToString().ToLower() == item.ToLower());
				notification!.IsRead = true;

				markAsRead.Add(notification);

				markSuccess.Add(item);
			}

			_dbContext.Notifications.UpdateRange(markAsRead);
			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				Data = "OK",
				MarkedItems = string.Join(";", markSuccess),
			});
		}

		return BadRequest();
	}
}
