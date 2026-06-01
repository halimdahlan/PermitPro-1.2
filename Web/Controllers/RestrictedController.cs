using Microsoft.AspNetCore.Mvc;

namespace PermitPro.App.Controllers;

public class RestrictedController : Controller
{
	public IActionResult AccessDenied()
	{
		return View();
	}
}
