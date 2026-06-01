using PermitPro.Core.Models;

namespace PermitPro.Core.Interfaces;

public interface IMessageService
{
	Task SendEmailAsync(EmailInfo emailInfo);

	Task SendSMSAsync(string phoneNumber, string message);

	Task SaveToDatabaseAsync(string message);
}
