namespace PermitPro.App.Models.Charts;

public record DonutChartModel(string? Category, float? Value, string? Color);

public class BarChartModel
{
	public string? Category { get; set; }
	public int? TotalActive { get; set; }
	public int? TotalPending { get; set; }
	public int? TotalClosed { get; set; }
}