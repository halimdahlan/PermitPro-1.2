using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.Models.Reports
{
	public class ReportGridModel
	{
		public Guid Id { get; set; }

		[Display(Name = "PERMIT NO.")]
		public string? PermitNo { get; set; }

		[Display(Name = "PERMIT HOLDER")]
		public string? PermitHolderName { get; set; }

		public string? PermitHolderId { get; set; }

		[Display(Name = "LOCATION")]
		public string? Location { get; set; }

		[Display(Name = "START DATE")]
		public DateTime? StartDate { get; set; }

		[Display(Name = "END DATE")]
		public DateTime? EndDate { get; set; }

		[Display(Name = "DURATION (days)")]
		public int? DurationDays => (StartDate.HasValue && EndDate.HasValue)
			? (int?)Math.Round((EndDate.Value - StartDate.Value).TotalDays)
			: null;

		[Display(Name = "STATUS")]
		public string? PermitStatus { get; set; }

		[Display(Name = "CERTIFICATES")]
		public string? Certificates { get; set; }

		[Display(Name = "SUBMITTED ON")]
		public DateTime CreatedWhen { get; set; }

		[Display(Name = "SUBMITTED")]
		public string? CreatedWhenString { get; set; }

		public string? CreatedMonth { get; set; }

		public int CreatedYear { get; set; }

		public int PermitStatusEnum { get; set; }

		public string? LocationId { get; set; }
	}
}
