#nullable disable

using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Attachment : EntityBase
{
	public required string FileName { get; set; }

	public int FileSize { get; set; }

	public string ContentType { get; set; }

	public Permit Permit { get; set; }

	public Certificate Certificate { get; set; }

}
