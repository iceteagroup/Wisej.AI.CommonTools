///////////////////////////////////////////////////////////////////////////////
//
// (C) 2024 ICE TEA GROUP LLC - ALL RIGHTS RESERVED
//
// 
//
// ALL INFORMATION CONTAINED HEREIN IS, AND REMAINS
// THE PROPERTY OF ICE TEA GROUP LLC AND ITS SUPPLIERS, IF ANY.
// THE INTELLECTUAL PROPERTY AND TECHNICAL CONCEPTS CONTAINED
// HEREIN ARE PROPRIETARY TO ICE TEA GROUP LLC AND ITS SUPPLIERS
// AND MAY BE COVERED BY U.S. AND FOREIGN PATENTS, PATENT IN PROCESS, AND
// ARE PROTECTED BY TRADE SECRET OR COPYRIGHT LAW.
//
// DISSEMINATION OF THIS INFORMATION OR REPRODUCTION OF THIS MATERIAL
// IS STRICTLY FORBIDDEN UNLESS PRIOR WRITTEN PERMISSION IS OBTAINED
// FROM ICE TEA GROUP LLC.
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wisej.AI.Helpers;
using Wisej.AI.Services;
using Wisej.Services;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Represents a collection of tools for performing web searches and processing web content.
	/// </summary>
	/// <remarks>
	/// This class provides functionality to search the web using a specified query and read content from specified URLs.
	/// It utilizes various services such as <see cref="IWebSearchService"/>, <see cref="ITokenizerService"/>, 
	/// <see cref="IHttpClientService"/>, and <see cref="IDocumentConversionService"/> to perform its operations.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[WebSearchTools]")]
	public class WebSearchTools : ToolsContainer
	{
		/// <summary>
		/// Gets or sets the web search service used for searching the web.
		/// </summary>
		[Inject]
		protected IWebSearchService SearchService { get; set; }

		/// <summary>
		/// Gets or sets the tokenizer service used for truncating context tokens.
		/// </summary>
		[Inject]
		protected ITokenizerService TokenizerService { get; set; }

		/// <summary>
		/// Gets or sets the HTTP client service used for making HTTP requests.
		/// </summary>
		[Inject]
		protected IHttpClientService HttpClientService { get; set; }

		/// <summary>
		/// Gets or sets the document conversion service used for converting web content.
		/// </summary>
		[Inject]
		protected IDocumentConversionService DocumentConversionService { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of sites to search.
		/// </summary>
		[DefaultValue(5)]
		public virtual int MaxSites
		{
			get;
			set;
		} = 5;

		/// <summary>
		/// Gets or sets the maximum number of context tokens.
		/// </summary>
		[DefaultValue(4096)]
		public virtual int MaxContextTokens
		{
			get;
			set;
		} = 4096;

		/// <summary>
		/// Gets or sets the User-Agent header.
		/// </summary>
		[DefaultValue("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36")]
		public virtual string UserAgent
		{
			get;
			set;
		} = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

		/// <summary>
		/// Gets or sets the timeout in milliseconds.
		/// </summary>
		[DefaultValue(30000)]
		public virtual int Timeout
		{
			get;
			set;
		} = 30000;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebSearchTools"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor sets the <see cref="MaxSites"/> property of
		/// the <see cref="SearchService"/> if it is not null.
		/// </remarks>
		public WebSearchTools()
		{
			if (this.SearchService != null)
			{
				this.SearchService.MaxSites = this.MaxSites;
			}
		}

		#region Tools

		/// <summary>
		/// Searches the web using the specified query.
		/// </summary>
		/// <param name="query">The search query.</param>
		/// <returns>A task that represents the asynchronous search operation. The task result contains the search result as a string.</returns>
		[SmartTool.Tool]
		[Description("[WebSearchTools.search]")]
		protected virtual Task<string> search(

			[Description("[WebSearchTools.search.query]")]
			string query)
		{
			if (this.SearchService == null)
				return Task.FromResult((string)null);

			return this.SearchService.SearchAsync(query);
		}

		/// <summary>
		/// Reads content from the specified URLs.
		/// </summary>
		/// <param name="urls">An array of URLs to read content from.</param>
		/// <returns>A task that represents the asynchronous read operation. The task result contains the content as a string.</returns>
		[SmartTool.Tool]
		[Description("[WebSearchTools.read]")]
		protected virtual Task<string> read(

			[Description("[WebSearchTools.read.urls]")]
			string[] urls)
		{
			if (this.SearchService == null)
				return null;

			return DownloadAsync(urls);
		}

		#endregion

		#region Implementation

		/// <summary>
		/// Downloads content from the specified URLs asynchronously.
		/// </summary>
		/// <param name="urls">An array of URLs to download content from.</param>
		/// <returns>A task that represents the asynchronous download operation. The task result contains the downloaded content as a string.</returns>
		public virtual async Task<string> DownloadAsync(string[] urls)
		{
			var sb = new StringBuilder();

			for (int i = 0, l = Math.Min(this.MaxSites, urls.Length); i < l; i++)
			{
				var url = urls[i];
				var content = await DownloadUrlAsync(url);

				if (!String.IsNullOrEmpty(content))
				{
					sb.AppendLine($"SOURCE:{url}");
					sb.AppendLine(content);
					sb.AppendLine("===");
				}
			}

			return sb.ToString();
		}

		//
		private async Task<string> DownloadUrlAsync(string url)
		{
			var client = this.Client;

			try
			{
				var response = await client.GetAsync(url);
				if (response == null || !response.IsSuccessStatusCode)
					return "";

				var uri = new Uri(url);
				var contentType = Path.GetExtension(uri.LocalPath);
				if (contentType == "") contentType = "html";

				var stream = await response.Content.ReadAsStreamAsync();
				var content = this.DocumentConversionService.Convert(stream, contentType);

				return this.TokenizerService.TruncateContent(String.Join("\n", content), this.MaxContextTokens);
			}
			catch (Exception ex)
			{
				LogHelper.Log(TraceLevel.Info, ex);
			}

			return "";
		}

		//
		private IHttpClientService Client
		{
			get
			{
				if (!_clientInitialized)
				{
					_clientInitialized = true;
					_client = ServiceHelper.GetService<IHttpClientService>();
					_client.SetHandler(new HttpClientHandler
					{
						AllowAutoRedirect = true,
						AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
					});

					_client.SetDefaultHeader("User-Agent", this.UserAgent);
					_client.SetDefaultHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
					_client.SetDefaultHeader("Accept-Encoding", "gzip, deflate");
					_client.SetDefaultHeader("Cache-Control", "no-cache");
					_client.SetDefaultHeader("Cookie", "");
					_client.SetDefaultHeader("sec-ch-ua", "Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
					_client.SetDefaultHeader("sec-fetch-user", "?1");
					_client.SetDefaultHeader("sec-ch-ua-platform", "Windows");

					_client.Timeout = TimeSpan.FromMilliseconds(this.Timeout);
				}

				return _client;
			}
		}
		private bool _clientInitialized;
		private IHttpClientService _client;

		#endregion
	}
}
