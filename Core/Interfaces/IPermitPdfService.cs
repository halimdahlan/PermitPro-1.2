using Microsoft.AspNetCore.Http;

namespace PermitPro.Core.Interfaces;

public interface IPermitPdfService
{
    Task<byte[]> GetPdfBytesAsync(IFormCollection form);
}
