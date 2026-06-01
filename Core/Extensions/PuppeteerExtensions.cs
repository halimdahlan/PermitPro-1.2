using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using PuppeteerSharp;

namespace PermitPro.Core.Extensions;

public static class PuppeteerExtensions
{
	private static string _executablePath = string.Empty;

	public static async Task PreparePuppeteerAsync(this IApplicationBuilder applicationBuilder, IWebHostEnvironment hostingEnvironment)
	{
		var downloadPath = Path.Join(hostingEnvironment.ContentRootPath, @"\puppeteer");
		var browserOptions = new BrowserFetcherOptions { Path = downloadPath };
		var browserFetcher = new BrowserFetcher(browserOptions);

		_executablePath = browserFetcher.GetExecutablePath("");

		await browserFetcher.DownloadAsync();
	}

	public static string ExecutablePath => _executablePath;
}
