using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

using PermitPro.Core.Interceptors;
using PermitPro.Core.Interfaces;
using PermitPro.Core.Services;

namespace PermitPro.Core.Extensions;

public static class CommonExtensions
{
	public static void AddPermitProServices(this IServiceCollection services)
	{
		services.AddMemoryCache();
		services.AddDataProtection();
		services.AddSingleton<UpdateAuditableEntitiesInterceptor>();
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
		services.AddScoped<ITemplateService, RazorViewsTemplateService>();
		services.AddScoped<IPermitService, PermitService>();
		services.AddScoped<IPermitPdfService, PermitPdfService>();
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IMessageService, MessageService>();
		services.AddScoped<ILogService, LogService>();
		services.AddScoped<IAppSettingsService, AppSettingsService>();
	}
}
