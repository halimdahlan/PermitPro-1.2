using MailKit.Net.Smtp;

using MimeKit;

using PermitPro.Core.Data;
using PermitPro.Core.Interfaces;
using PermitPro.Core.Models;

namespace PermitPro.Core.Services;

public class MessageService : IMessageService
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IAppSettingsService _appSettings;
	private readonly ILogService _logService;
	private readonly ICurrentUserService _currentUserService;

	public MessageService(ApplicationDbContext dbContext, IAppSettingsService appSettings, ILogService logService, ICurrentUserService currentUserService)
	{
		_dbContext = dbContext;
		_appSettings = appSettings;
		_logService = logService;
		_currentUserService = currentUserService;
	}

	public async Task SendEmailAsync(EmailInfo emailInfo)
	{
		var currentUser = _currentUserService.GetCurrentUser();
		var companyId = currentUser?.UserCompany?.Id ?? Guid.Empty;

		var server = await _appSettings.GetValueAsync(companyId, "email", "smtp_server");
		var port = await _appSettings.GetIntAsync(companyId, "email", "smtp_port");
		var senderName = await _appSettings.GetValueAsync(companyId, "email", "sender_name");
		var senderEmail = await _appSettings.GetValueAsync(companyId, "email", "sender_email");
		var userName = await _appSettings.GetValueAsync(companyId, "email", "email_username");
		var password = await _appSettings.GetValueAsync(companyId, "email", "email_password");

		var recipientName = emailInfo.Name;
		var recipientEmail = emailInfo.Email;

      var bodyBuilder = new BodyBuilder
      {
         HtmlBody = emailInfo.Body
      };

      var mailMessage = new MimeMessage();
		mailMessage.From.Add(new MailboxAddress(senderName, senderEmail!));

		if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
		{
			recipientEmail = "workflow@permitpro.app";
		}

		mailMessage.To.Add(new MailboxAddress(recipientName, recipientEmail!));
		mailMessage.Bcc.Add(new MailboxAddress("Workflow Admin", "workflow@permitpro.app"));

		mailMessage.Subject = emailInfo.Subject ?? string.Empty;
		mailMessage.Body = bodyBuilder.ToMessageBody();

		using var client = new SmtpClient();

		try
		{
			await client.ConnectAsync(server!, port, false);
			await client.AuthenticateAsync(userName!, password!);

			await client.SendAsync(mailMessage);
			await client.DisconnectAsync(true);

			var logMessage = $"Email has been sent to: {recipientName} ({recipientEmail}).";
			await _logService.LogMessageAsync(Enums.LogTypeEnum.Information, "SENDEMAIL", logMessage, currentUser!);
		}
		catch (Exception ex)
		{
			var logMessage = $"Error sending email to: {recipientName} ({recipientEmail}). Error: {ex.Message}";
			await _logService.LogMessageAsync(Enums.LogTypeEnum.Error, "SENDEMAIL", logMessage, currentUser!);
		}
	}


	public Task SendSMSAsync(string phoneNumber, string message)
	{
		throw new NotImplementedException();
	}


	public Task SaveToDatabaseAsync(string message)
	{
		throw new NotImplementedException();
	}
}
