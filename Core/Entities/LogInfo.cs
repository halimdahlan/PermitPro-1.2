using PermitPro.Core.Entities.Base;
using PermitPro.Core.Enums;

namespace PermitPro.Core.Entities;

public class LogInfo : EntityBase
{
	public string? LogName { get; set; }

	public string? LogDescription { get; set; }

	public LogTypeEnum LogType { get; set; }

	public string? ControllerName { get; set; }

	public string? ActionName { get; set; }

}
