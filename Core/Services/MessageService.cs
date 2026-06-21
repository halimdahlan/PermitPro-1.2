using MailKit.Net.Smtp;

using MimeKit;

using PermitPro.Core.Data;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;
using PermitPro.Core.Models;

namespace PermitPro.Core.Services;

public class MessageService : IMessageService
{
	private readonly ApplicationDbContext _dbContext;
	private readonly EmailSettings _emailSettings;
	private readonly ILogService _logService;
	private readonly ICurrentUserService _currentUserService;

	public MessageService(ApplicationDbContext dbContext, EmailSettings emailSettings, ILogService logService, ICurrentUserService currentUserService)
	{
		_dbContext = dbContext;
		_emailSettings = emailSettings;
		_logService = logService;
		_currentUserService = currentUserService;
	}

	public async Task SendEmailAsync(EmailInfo emailInfo)
	{
		var currentUser = _currentUserService.GetCurrentUser();
		var recipientName = emailInfo.Name;
		var recipientEmail = emailInfo.Email;

		var bodyBuilder = new BodyBuilder();
		bodyBuilder.HtmlBody = emailInfo.Body;

		var mailMessage = new MimeMessage();
		mailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));

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
			await client.ConnectAsync(_emailSettings.Server, _emailSettings.Port, false);
			await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password);

			await client.SendAsync(mailMessage);
			await client.DisconnectAsync(true);

			var logMessage = $"Email has been sent to: {recipientName} ({recipientEmail}).";
			await _logService.LogMessageAsync(Enums.LogTypeEnum.Information, "SENDEMAIL", logMessage, currentUser);
		}
		catch (Exception ex)
		{
			var logMessage = $"Error sending email to: {recipientName} ({recipientEmail}). Error: {ex.Message}";
			await _logService.LogMessageAsync(Enums.LogTypeEnum.Error, "SENDEMAIL", logMessage, currentUser);
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
