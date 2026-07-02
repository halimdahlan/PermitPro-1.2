using Microsoft.AspNetCore.Http;

using PermitPro.Core.Entities;

namespace PermitPro.Core.Interfaces;

public interface IPermitService : IEntityOperation<Permit>
{
	Task ClosePermitAsync();
	Task ClosePermitAsync(Guid permitId, string notes);

	Task UpdateCertificateAsync();
	Task UpdateCertificateAsync(Guid permitId, Guid companyId, string permitForm);

	Task SetPermitStatusAsync();
	Task SetPermitStatusAsync(Guid permitId, string mode, string comments);

	Task CreateAsync(Guid companyId, string permitForm, string submissionType, IFormFileCollection files);
	Task UpdateAsync(Guid permitId, Guid companyId, string permitForm, string submissionType, IFormFileCollection files);

	IEnumerable<Permit> GetPermitsBySite(Site site);

	IEnumerable<Permit> GetPermitsBySite(Guid id);
}
