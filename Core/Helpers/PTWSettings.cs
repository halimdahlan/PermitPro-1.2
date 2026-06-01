#nullable disable

using PermitPro;

namespace PermitPro.Core.Helpers;

public class PTWSettings
{
    public List<PTWCertificate> Certificates { get; set; }
}

public class PTWCertificate
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}