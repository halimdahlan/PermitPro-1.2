namespace PermitPro.Core.Interfaces;

public interface ITemplateService
{
	Task<string> RenderAsync<TViewModel>(string templateFileName, TViewModel viewModel);
}
