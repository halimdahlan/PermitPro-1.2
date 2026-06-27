#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using PermitPro.App.Models;

using System.Diagnostics;

namespace PermitPro.App.Controllers;

[AllowAnonymous]
[Route("error")]
public class ErrorController : Controller
{
    [Route("{statusCode:int?}")]
    public IActionResult Index(int? statusCode)
    {
        var code = statusCode ?? 500;

        var message = code switch
        {
            400 => "The request could not be understood.",
            401 => "You need to be signed in to view this page.",
            403 => "You don't have permission to access this resource.",
            404 => "The page you're looking for doesn't exist.",
            408 => "The request timed out. Please try again.",
            _ => "Something went wrong on our end. Please try again."
        };

        Response.StatusCode = code;

        return View("Error", new ErrorViewModel
        {
            StatusCode = code,
            Message = message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
