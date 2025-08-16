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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Wisej.AI.Services;
using Wisej.AI.Tools;
using Wisej.Services;
using Wisej.Web;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// 
	/// </summary>
	[ApiCategory("Tools")]
	[Description("[ArxivTools]")]
	public class ArxivTools : ToolsContainer
	{
		private string _url;

		[Inject]
		private ITokenizerService TokenizerService { get; set; }

		[Inject]
		private IHttpClientService HttpClientService { get; set; }

		[Inject]
		private IDocumentConversionService DocumentConversionService { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		public ArxivTools(string url = "https://export.arxiv.org/api/query")
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			_url = url;

			Application.Services.Inject(this);

			this.HttpClientService.SetHandler(new HttpClientHandler
			{
				AllowAutoRedirect = true,
#if !NETCOREAPP
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#else
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
#endif
			});

		}

		/// <summary>
		/// 
		/// </summary>
		public virtual string Url
			=> _url;

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(5)]
		public int MaxSites
		{
			get;
			set;
		} = 5;

		/// <summary>
		/// 
		/// </summary>
		[DefaultValue(4096)]
		public int MaxContextTokens
		{
			get;
			set;
		} = 4096;

		#region Tools

		/// <summary>
		/// 
		/// </summary>
		/// <param name="search_query"></param>
		/// <returns></returns>
		[SmartTool.Tool]
		[Description("Use this tool to search a free archive of scholarly articles in many scientific fields, including physics, mathematics, computer science, and economics.")]
		protected virtual async Task<string> search(

			[Description("Query to send to the search engine. Use the user's question as is.")]
			string search_query)
		{
			var client = this.HttpClientService;
			var response = await client.GetAsync($"{this.Url}?search_query={WebUtility.UrlEncode(search_query)}&max_results={this.MaxSites}&sortBy=relevance");
			response.EnsureSuccessStatusCode();

			var doc = new XmlDocument();
			doc.LoadXml(await response.Content.ReadAsStringAsync());
			var nsmgr = new XmlNamespaceManager(doc.NameTable);
			nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");

			var index = 1;
			var sb = new StringBuilder();
			var entries = doc.SelectNodes("//atom:entry", nsmgr);
			foreach (XmlNode entry in entries)
			{
				var title = entry.SelectSingleNode("atom:title", nsmgr);
				var summary = entry.SelectSingleNode("atom:summary", nsmgr);
				var pdfUrl =  entry.SelectSingleNode("atom:link[@title=\"pdf\"]", nsmgr);

				sb.AppendLine($"{index++}:{pdfUrl.Attributes["href"].Value} (abstract:{summary.InnerText})");
			}

			return sb.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="urls"></param>
		/// <returns></returns>
		[SmartTool.Tool]
		[Description("[ArxivTools.read_documents]")]
		protected virtual async Task<string> read_documents(

			[Description("[ArxivTools.read_documents.urls]")]
			string[] urls
			)
		{
			return "";
		}

		private async Task<string> read_url(string url)
		{
			var client = this.HttpClientService;
			var response = await client.GetAsync(url);

			if (!response.IsSuccessStatusCode)
				return "";

			var uri = new Uri(url);
			var contentType = Path.GetExtension(uri.LocalPath);
			if (contentType == "") contentType = "html";

			var stream = await response.Content.ReadAsStreamAsync();
			var content = this.DocumentConversionService.Convert(stream, contentType);
			return String.Join("\n", content);
		}

		#endregion
	}
}
