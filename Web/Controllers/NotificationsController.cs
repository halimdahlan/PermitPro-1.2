#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
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
			.AsNoTracking()
			.Include(e => e.NotificationUser)
			.Where(e => e.NotificationUser.Id == currentUser.Id && !e.IsArchived)
			.OrderByDescending(e => e.CreatedWhen)
			.Take(50)
			.Select(e => new NotificationViewModel
			{
				Id = e.Id,
				Title = e.Title,
				Message = e.Message,
				Url = e.Url ?? string.Empty,
				IsRead = e.IsRead,
				CreatedWhen = string.Format("{0} at {1}",
					GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("dd MMM, yyyy"),
					GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("hh:mm tt")),
			})
			.ToList();

		var model = new NotificationsPageViewModel
		{
			Notifications = notifications,
			UnreadCount = notifications.Count(n => !n.IsRead),
		};

		return View(model);
	}


	[HttpGet("{company}/notifications/count")]
	public IActionResult GetUnreadCount(Guid company)
	{
		var currentUser = _currentUserService.GetCurrentUser();
		if (currentUser == null) return Ok(new { count = 0 });

		var count = _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.Count(e => e.NotificationUser.Id == currentUser.Id && !e.IsRead && !e.IsArchived);

		return Ok(new { count });
	}


	[HttpPut("{company}/notifications/read")]
	public async Task<IActionResult> MarkAsRead(Guid company, [FromBody] NotificationIdsRequest request)
	{
		if (request?.Ids == null || request.Ids.Count == 0)
			return BadRequest();

		var currentUser = _currentUserService.GetCurrentUser();

		var notifications = await _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.Where(e => request.Ids.Contains(e.Id) && e.NotificationUser.Id == currentUser.Id)
			.ToListAsync();

		foreach (var n in notifications)
			n.IsRead = true;

		await _dbContext.SaveChangesAsync();

		return Ok(new { success = true, ids = request.Ids });
	}


	[HttpPut("{company}/notifications/read/all")]
	public async Task<IActionResult> MarkAllAsRead(Guid company)
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var notifications = await _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.Where(e => e.NotificationUser.Id == currentUser.Id && !e.IsRead && !e.IsArchived)
			.ToListAsync();

		foreach (var n in notifications)
			n.IsRead = true;

		await _dbContext.SaveChangesAsync();

		return Ok(new { success = true });
	}


	[HttpDelete("{company}/notifications/dismiss/{id}")]
	public async Task<IActionResult> Dismiss(Guid company, Guid id)
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var notification = await _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.FirstOrDefaultAsync(e => e.Id == id && e.NotificationUser.Id == currentUser.Id);

		if (notification == null)
			return NotFound();

		notification.IsArchived = true;
		notification.IsRead = true;
		await _dbContext.SaveChangesAsync();

		return Ok(new { success = true });
	}


	[HttpDelete("{company}/notifications/dismiss/all")]
	public async Task<IActionResult> DismissAll(Guid company)
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var notifications = await _dbContext.Notifications
			.Include(e => e.NotificationUser)
			.Where(e => e.NotificationUser.Id == currentUser.Id && !e.IsArchived)
			.ToListAsync();

		foreach (var n in notifications)
		{
			n.IsArchived = true;
			n.IsRead = true;
		}

		await _dbContext.SaveChangesAsync();

		return Ok(new { success = true });
	}
}

public class NotificationIdsRequest
{
	public List<Guid> Ids { get; set; } = [];
}
