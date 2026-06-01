namespace PermitPro.App.Models.Ajax;

public class AjaxStepMoveRequest
{
	public bool MoveUp { get; set; }
	public int Current { get; set; }
	public int Previous {  get; set; }
	public int Next { get; set; }
}
