using PermitPro.Core.Entities;
using PermitPro.Core.Enums;

namespace PermitPro.Core.Interfaces;

public interface ILogService
{
	#region "Asynchronous methods"

	Task LogMessageAsync(LogTypeEnum logType, string category, string message, UserInfo user);

	Task LogMessageAsync(LogTypeEnum logType, string category, string message, UserInfo user, string url);

	Task LogMessageAsync(LogTypeEnum logType, string category, string message, UserInfo user, string url, string serializedData);

	Task LogMessageAsync(LogTypeEnum logType, string category, string message, UserInfo user, string url, string serializedData, string entity);

	#endregion


	#region "Synchronous methods"

	void LogMessage(LogTypeEnum logType, string category, string message, UserInfo user);

	void LogMessage(LogTypeEnum logType, string category, string message, UserInfo user, string url);

	void LogMessage(LogTypeEnum logType, string category, string message, UserInfo user, string url, string serializedData);

	void LogMessage(LogTypeEnum logType, string category, string message, UserInfo user, string url, string serializedData, string entity);

	#endregion

}
