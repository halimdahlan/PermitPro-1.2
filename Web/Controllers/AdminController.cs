#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

using System.Security.Claims;

namespace PermitPro.App.Controllers;

public class AdminController : Controller
{
    private readonly SignInManager<UserInfo> _signInManager;
    private readonly UserManager<UserInfo> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogService _logService;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> _allowedLogoExts =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".svg" };

    public AdminController(
        SignInManager<UserInfo> signInManager,
        UserManager<UserInfo> userManager,
        ApplicationDbContext db,
        ILogService logService,
        IWebHostEnvironment env)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _logService = logService;
        _env = env;
    }


    // ── Auth ──────────────────────────────────────────────────────────────────

    [HttpGet("/admin/login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true && IsSuperUser())
            return Redirect("/admin/companies");

        return View(new AdminLoginViewModel { Email = string.Empty, Password = string.Empty });
    }

    [HttpPost("/admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View(model);
        }

        var isSuperUser = user.UserRoles.Any(ur => ur.Role.NormalizedName == "SUPERUSER");
        if (!isSuperUser)
        {
            ModelState.AddModelError(string.Empty, "Access denied. Super Admin credentials required.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "ADMIN_SIGN_IN", $"Super Admin ({user.UserName}) logged in.", user);
            return Redirect("/admin/companies");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked. Try again in 10 minutes.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid credentials.");
        return View(model);
    }

    [HttpGet("/admin/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/admin/login");
    }


    // ── Company management ────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("/admin")]
    public IActionResult Index() => Redirect("/admin/companies");

    [Authorize]
    [HttpGet("/admin/companies")]
    public async Task<IActionResult> Companies()
    {
        if (!IsSuperUser()) return Forbid();

        var companies = await _db.Companies
            .IgnoreQueryFilters()
            .Select(c => new AdminCompanyListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                LogoFileName = c.LogoFileName,
                UserCount = _db.Users.Count(u => u.UserCompany.Id == c.Id),
                SiteCount = _db.Sites.IgnoreQueryFilters().Count(s => s.SiteCompany.Id == c.Id),
                CreatedWhen = c.CreatedWhen,
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(companies);
    }

    [Authorize]
    [HttpGet("/admin/companies/new")]
    public IActionResult CompanyCreate()
    {
        if (!IsSuperUser()) return Forbid();
        return View("CompanyForm", new AdminCompanyFormViewModel { Name = string.Empty });
    }

    [Authorize]
    [HttpPost("/admin/companies/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyCreate(AdminCompanyFormViewModel model)
    {
        if (!IsSuperUser()) return Forbid();

        if (!ModelState.IsValid)
            return View("CompanyForm", model);

        var exists = await _db.Companies.IgnoreQueryFilters().AnyAsync(c => c.Name == model.Name);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "A company with this name already exists.");
            return View("CompanyForm", model);
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive,
        };

        if (model.LogoFile != null)
        {
            var (ok, fileName, error) = await SaveLogoAsync(model.LogoFile);
            if (!ok)
            {
                ModelState.AddModelError(nameof(model.LogoFile), error);
                return View("CompanyForm", model);
            }
            company.LogoFileName = fileName;
        }

        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "ADMIN_COMPANY_CREATE", $"Company '{company.Name}' created.", CurrentUser());

        TempData["SuccessMessage"] = $"Company \"{company.Name}\" created successfully.";
        return Redirect("/admin/companies");
    }

    [Authorize]
    [HttpGet("/admin/companies/{id:guid}/edit")]
    public async Task<IActionResult> CompanyEdit(Guid id)
    {
        if (!IsSuperUser()) return Forbid();

        var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (company == null) return NotFound();

        return View("CompanyForm", new AdminCompanyFormViewModel
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            IsActive = company.IsActive,
            ExistingLogoFileName = company.LogoFileName,
        });
    }

    [Authorize]
    [HttpPost("/admin/companies/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyEdit(Guid id, AdminCompanyFormViewModel model)
    {
        if (!IsSuperUser()) return Forbid();

        if (!ModelState.IsValid)
            return View("CompanyForm", model);

        var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (company == null) return NotFound();

        var nameTaken = await _db.Companies.IgnoreQueryFilters()
            .AnyAsync(c => c.Name == model.Name && c.Id != id);
        if (nameTaken)
        {
            ModelState.AddModelError(nameof(model.Name), "A company with this name already exists.");
            return View("CompanyForm", model);
        }

        company.Name = model.Name;
        company.Description = model.Description;
        company.IsActive = model.IsActive;

        if (model.RemoveLogo && !string.IsNullOrEmpty(company.LogoFileName))
        {
            DeleteLogoFile(company.LogoFileName);
            company.LogoFileName = null;
        }
        else if (model.LogoFile != null)
        {
            var (ok, fileName, error) = await SaveLogoAsync(model.LogoFile);
            if (!ok)
            {
                ModelState.AddModelError(nameof(model.LogoFile), error);
                model.ExistingLogoFileName = company.LogoFileName;
                return View("CompanyForm", model);
            }

            if (!string.IsNullOrEmpty(company.LogoFileName))
                DeleteLogoFile(company.LogoFileName);

            company.LogoFileName = fileName;
        }

        _db.Companies.Update(company);
        await _db.SaveChangesAsync();

        await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "ADMIN_COMPANY_EDIT", $"Company '{company.Name}' updated.", CurrentUser());

        TempData["SuccessMessage"] = $"Company \"{company.Name}\" updated successfully.";
        return Redirect("/admin/companies");
    }

    [Authorize]
    [HttpPost("/admin/companies/{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyToggle(Guid id)
    {
        if (!IsSuperUser()) return Forbid();

        var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (company == null) return NotFound();

        company.IsActive = !company.IsActive;
        _db.Companies.Update(company);
        await _db.SaveChangesAsync();

        var state = company.IsActive ? "activated" : "deactivated";
        await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "ADMIN_COMPANY_TOGGLE", $"Company '{company.Name}' {state}.", CurrentUser());

        TempData["SuccessMessage"] = $"Company \"{company.Name}\" {state}.";
        return Redirect("/admin/companies");
    }

    [Authorize]
    [HttpPost("/admin/companies/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyDelete(Guid id)
    {
        if (!IsSuperUser()) return Forbid();

        var company = await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (company == null) return NotFound();

        if (!string.IsNullOrEmpty(company.LogoFileName))
            DeleteLogoFile(company.LogoFileName);

        var currentUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        await _db.SoftDeleteAsync(company, currentUserId);

        await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "ADMIN_COMPANY_DELETE", $"Company '{company.Name}' soft-deleted.", CurrentUser());

        TempData["SuccessMessage"] = $"Company \"{company.Name}\" has been deleted.";
        return Redirect("/admin/companies");
    }


    // ── Logo helpers ──────────────────────────────────────────────────────────

    private async Task<(bool ok, string fileName, string error)> SaveLogoAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!_allowedLogoExts.Contains(ext))
            return (false, null, "Only PNG, JPG, JPEG, WebP, and SVG files are allowed.");

        if (file.Length > 2 * 1024 * 1024)
            return (false, null, "Logo must be smaller than 2 MB.");

        var dir = Path.Combine(_env.WebRootPath, "img", "logos");
        Directory.CreateDirectory(dir);

        var storedName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, storedName);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(fs);

        return (true, storedName, null);
    }

    private void DeleteLogoFile(string fileName)
    {
        var path = Path.Combine(_env.WebRootPath, "img", "logos", fileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    private bool IsSuperUser() =>
        User.IsInRole("Super User") || User.IsInRole("SUPERUSER");

    private UserInfo CurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return _db.Users.FirstOrDefault(u => u.Id == userId);
    }
}
