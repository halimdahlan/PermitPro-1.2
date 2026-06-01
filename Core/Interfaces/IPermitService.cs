using Microsoft.AspNetCore.Http;

using PermitPro.Core.Entities;

namespace PermitPro.Core.Interfaces;

public interface IPermitService : IEntityOperation<Permit>
{
	Task ClosePermitAsync();

	Task UpdateCertificateAsync();

	Task SetPermitStatusAsync();

	IEnumerable<Permit> GetPermitsBySite(Site site);

	IEnumerable<Permit> GetPermitsBySite(Guid id);

	Task<byte[]> GetPdfBytesAsync(IFormCollection form);

}
