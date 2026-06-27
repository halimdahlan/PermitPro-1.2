namespace PermitPro.App.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int StatusCode { get; set; } = 500;
    public string Message { get; set; } = "Something went wrong on our end. Please try again.";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
