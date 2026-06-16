using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Extensions;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interceptors;

using System.Text;

var builder = WebApplication.CreateBuilder(args);
var appConfig = builder.Configuration;

// Add services to the container.
var connectionString = appConfig.GetConnectionString(builder.Environment.EnvironmentName);
var hangfireConnectionString = Environment.GetEnvironmentVariable("HANGFIRE_DB_CONNECTION");
var emailSettings = appConfig.GetSection("EmailSettings").Get<EmailSettings>();
var ptwSettings = appConfig.GetSection("PTWSettings").Get<PTWSettings>();
var jwtSettings = appConfig.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
	var auditableInterceptor = sp.GetService<UpdateAuditableEntitiesInterceptor>()!;

	options
		.UseSqlServer(connectionString)
		.AddInterceptors(auditableInterceptor);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<UserInfo, Role>(options =>
{
	options.SignIn.RequireConfirmedEmail = true;
	//options.Tokens.ProviderMap.Add("CustomEmailConfirmation", new TokenProviderDescriptor(typeof(CustomEmailConfirmationTokenProvider<SiteUser>)));
	//options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
})
	.AddRoles<Role>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();
//.AddTokenProvider<CustomEmailConfirmationTokenProvider<SiteUser>>("CustomEmailConfirmation")
//.AddDefaultUI();

// JWT Bearer is an additional scheme alongside the default Identity cookie scheme.
// Use [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] on
// API endpoints that should be authenticated via Bearer token.
builder.Services.AddAuthentication()
	.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtSettings!.Issuer,
			ValidAudience = jwtSettings.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
			ClockSkew = TimeSpan.Zero
		};
	});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddKendo();

builder.Services.Configure<IdentityOptions>(options =>
{
	// Password settings
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = true;
	options.Password.RequireUppercase = true;
	options.Password.RequiredLength = 12;
	options.Password.RequiredUniqueChars = 1;

	// Lockout settings
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
	options.Lockout.MaxFailedAccessAttempts = 3;
	options.Lockout.AllowedForNewUsers = true;

	// User settings
	options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+#$!%&*";
	options.User.RequireUniqueEmail = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
	// Cookie settings
	options.Cookie.HttpOnly = true;
	options.Cookie.SameSite = SameSiteMode.Strict;

	// Default session lifetime (RememberMe unchecked): 3 hours of inactivity.
	// When RememberMe IS checked, OnSigningIn overrides this to 30 days below.
	options.ExpireTimeSpan = TimeSpan.FromHours(3);
	options.SlidingExpiration = true;

	options.LoginPath = "/account/login";
	options.AccessDeniedPath = "/account/accessdenied";

	// Extend persistent-cookie lifetime when the user checks "Remember me".
	// Without this override, isPersistent:true would still expire after 3 hours,
	// defeating the purpose of the feature.
	options.Events.OnSigningIn = context =>
	{
		if (context.Properties.IsPersistent)
		{
			//context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
		}
		return Task.CompletedTask;
	};
});

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(60);
});

// Configure for ALL data protection tokens period to 30 minutes
#region "Data protection tokens"

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
	options.TokenLifespan = TimeSpan.FromMinutes(30);
});

#endregion


builder.Services.AddSingleton(emailSettings!);
builder.Services.AddSingleton(ptwSettings!);
builder.Services.AddSingleton(jwtSettings!);


// Add PermitPro custom services
builder.Services.AddPermitProServices();

// Add Hangfire
// builder.Services.AddHangfire(config =>
// {
// 	config
// 		.UseSimpleAssemblyNameTypeSerializer()
// 		.UseRecommendedSerializerSettings()
// 		.UseSqlServerStorage(hangfireConnectionString);
// });

// builder.Services.AddHangfireServer();

//builder.Services.AddTransient<IGeneralHelper, GeneralHelper>();

var app = builder.Build();

// Hangfire
//app.UseHangfireDashboard();

//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

//app.UseExceptionHandler("/home/error");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

//app.Use(async (context, next) =>
//{
//	context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
//	context.Response.Headers.Add("Content-Security-Policy", "default-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;script-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;");
//	context.Response.Headers.Add("X-Content-Security-Policy", "default-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;script-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;");
//	context.Response.Headers.Add("X-WebKit-CSP", "default-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;script-src 'self' 'unsafe-inline' data: https: localhost:* ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*;");

//	await next();
//});

app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{company}/{controller}/{action}/{id?}",
	defaults: new { company = Guid.Empty, controller = "landing", action = "index" });

app.MapRazorPages();

app.Run();