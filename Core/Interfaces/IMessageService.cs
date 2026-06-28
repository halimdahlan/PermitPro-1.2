using PermitPro.Core.Models;

namespace PermitPro.Core.Interfaces;

public interface IMessageService
{
	Task SendEmailAsync(EmailInfo emailInfo);

	Task SendEmailAsync(EmailInfo emailInfo, Guid companyId);

	Task SendSMSAsync(string phoneNumber, string message);

	Task SaveToDatabaseAsync(string message);
}
