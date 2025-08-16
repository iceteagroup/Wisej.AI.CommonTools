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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wisej.AI.Embeddings;
using Wisej.AI.Services;
using Wisej.Services;
using Wisej.Web;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Provides tools for searching and managing documents within a specified collection.
	/// </summary>
	/// <remarks>
	/// This class allows for embedding questions, querying documents, listing documents, and summarizing document content.
	/// It utilizes these services: <see cref="ITokenizerService"/>, <see cref="IEmbeddingStorageService"/>.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[DocumentSearchTools]")]
	public class DocumentSearchTools : ToolsContainer
	{
		private string _collectionName;
		private Predicate<EmbeddedDocument> _filter;

		/// <summary>
		/// Gets or sets the tokenizer service used for truncating content to fit within the maximum context tokens.
		/// </summary>
		[Inject]
		protected ITokenizerService TokenizerService { get; set; }

		/// <summary>
		/// Gets or sets the embedding storage service used for storing and retrieving embedded documents.
		/// </summary>
		[Inject]
		protected IEmbeddingStorageService EmbeddingStorageService { get; set; }

		/// <summary>
		/// Gets or sets the embedding generation service used for embedding questions.
		/// </summary>
		[Inject]
		protected IEmbeddingGenerationService EmbeddingGenerationService { get; set; }

		/// <summary>
		/// Gets or sets the reranking service.
		/// </summary>
		[Inject]
		protected IRerankingService RerankingService { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentSearchTools"/> class.
		/// </summary>
		/// <param name="collectionName">The name of the document collection. Default is an empty string.</param>
		/// <param name="filter">A predicate to filter embedded documents. Default is null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="collectionName"/> is null.</exception>
		public DocumentSearchTools(
			string collectionName = "",
			Predicate<EmbeddedDocument> filter = null)
		{
			if (collectionName == null)
				throw new ArgumentNullException(nameof(collectionName));

			_filter = filter;
			_collectionName = collectionName;

			Application.Services.Inject(this);
		}

		#region Properties

		/// <summary>
		/// Gets or sets the name of the document collection.
		/// </summary>
		[DefaultValue(null)]
		public virtual string CollectionName
		{
			get => _collectionName;
			set => _collectionName = value;
		}

		/// <summary>
		/// Gets or sets the number of top chunks to retrieve.
		/// </summary>
		[DefaultValue(10)]
		public virtual int TopN
		{
			get;
			set;
		} = 10;

		/// <summary>
		/// Get or sets the maximum number of vector clusters to
		/// generate when performing summarization tasks.
		/// </summary>
		[DefaultValue(5)]
		public virtual int MaxClusters
		{
			get;
			set;
		} = 5;

		/// <summary>
		/// Gets or sets the minimum similarity threshold for document retrieval.
		/// </summary>
		[DefaultValue(0.25f)]
		public virtual float MinSimilarity
		{
			get;
			set;
		} = 0.25f;

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
		/// Gets or sets the maximum number of documents that can be returned.
		/// </summary>
		[DefaultValue(100)]
		public virtual int MaxDocumentsSearch
		{
			get;
			set;
		} = 100;

		/// <summary>
		/// Gets or sets a value indicating whether reranking is enabled.
		/// </summary>
		[DefaultValue(false)]
		public virtual bool RerankingEnabled
		{
			get;
			set;
		} = false;

		#endregion

		#region Methods

		/// <summary>
		/// Asynchronously generates an embedding for the specified question.
		/// </summary>
		/// <param name="question">The question to be embedded. Must not be null or empty.</param>
		/// <remarks>
		/// This method checks if the provided <paramref name="question"/> is null or empty and returns <c>null</c> if so. 
		/// Otherwise, it delegates the embedding generation to the <c>EmbeddingGenerationService</c>.
		/// </remarks>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains the generated <see cref="Embedding"/> for the question, or <c>null</c> if the input is invalid.
		/// </returns>
		protected virtual Task<Embedding> EmbedQuestionAsync(string question)
		{
			if (String.IsNullOrEmpty(question))
				return Task.FromResult<Embedding>(null);

			return this.EmbeddingGenerationService.EmbedAsync(new[] { question });
		}

		/// <summary>
		/// Asynchronously reranks the provided text chunks based on their relevance to the given question.
		/// </summary>
		/// <param name="question">The question used as the basis for reranking.</param>
		/// <param name="chunks">An array of text chunks to be reranked.</param>
		/// <remarks>
		/// This method is intended to be overridden in derived classes to implement custom reranking logic. 
		/// The method should return the <paramref name="chunks"/> array reordered by relevance to the <paramref name="question"/>.
		/// </remarks>
		/// <returns>
		/// A task that represents the asynchronous operation. The task result contains an array of reranked text chunks.
		/// </returns>
		protected virtual async Task<string[]> RerankAsync(string question, string[] chunks)
		{
			return
				this.RerankingEnabled
					? await this.RerankingService.RerankAsync(question, chunks)
					: chunks;
		}

		#endregion

		#region Tools

		/// <summary>
		/// Queries all documents based on the provided question.
		/// </summary>
		/// <param name="question">The question to query documents for.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the query results as a string.</returns>
		/// <remarks>
		/// This method retrieves all documents that match the embedded question.
		/// </remarks>
		[SmartTool.Tool]
		[Description("[DocumentSearchTools.query_all_documents]")]
		protected virtual async Task<string> query_all_documents(

			[Description("[DocumentSearchTools.query_all_documents.question]")]
			string question)
		{
			var query = await EmbedQuestionAsync(question);
			var documents = await this.EmbeddingStorageService.QueryAsync(
				_collectionName,
				query?.Vectors[0], this.TopN, this.MinSimilarity, _filter);

			var sb = new StringBuilder();
			foreach (var document in documents)
			{
				// perform re-ranking
				var chunks = document.GetMatches()?.Chunks ?? [];
				chunks = await RerankAsync(question, chunks);

				sb.Append(
@$"
Name:'{document.Name}'
{document.Metadata.ToString()}
===
{String.Join("\n", chunks)}
===
");
			}

			return this.TokenizerService.TruncateContent(sb.ToString(), this.MaxContextTokens);
		}

		/// <summary>
		/// Lists all documents in the collection.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation. The task result contains a list of document names as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentSearchTools.list_all_documents]")]
		protected virtual async Task<string> list_all_documents()
		{
			var list = await this.EmbeddingStorageService.RetrieveAsync(_collectionName, false, _filter);

			return
				String.Join("\n",
					list.Select(d => $"{d.Name}"));
		}

		/// <summary>
		/// Queries a single document based on the provided document name and question.
		/// </summary>
		/// <param name="document_name">The name of the document to query.</param>
		/// <param name="question">The question to query the document for.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the query result as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentSearchTools.query_single_document]")]
		protected virtual async Task<string> query_single_document(

			[Description("[DocumentSearchTools.query_single_document.document_name]")]
			string document_name,

			[Description("[DocumentSearchTools.query_single_document.question]")]
			string question)
		{
			var query = await EmbedQuestionAsync(question);
			var document = await this.EmbeddingStorageService.QueryAsync(
				_collectionName,
				document_name,
				query?.Vectors[0], this.TopN, this.MinSimilarity);

			if (document == null)
				return $"Unable to read \"{document_name}\"";

			// perform re-ranking
			var chunks = document.GetMatches()?.Chunks ?? [];
			chunks = await RerankAsync(question, chunks);

			var text =
@$"
Name:'{document.Name}'
{document.Metadata.ToString()}
===
{String.Join("\n", chunks)}
===
";
			return this.TokenizerService.TruncateContent(text, this.MaxContextTokens);
		}

		/// <summary>
		/// Reads metadata for the specified documents.
		/// </summary>
		/// <param name="document_names">An array of document names to read metadata for.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the metadata as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentSearchTools.read_documents_metadata]")]
		protected virtual async Task<string> read_documents_metadata(

			[Description("[DocumentSearchTools.read_documents_metadata.document_names]")]
			string[] document_names)
		{
			var sb = new StringBuilder();

			foreach (var name in document_names)
			{
				var document = await this.EmbeddingStorageService.RetrieveAsync(
					_collectionName,
					name,
					false);

				if (document == null)
				{
					sb.AppendLine($"Unable to read \"{name}\"");
					sb.AppendLine("===");
				}
				else
				{
					sb.AppendLine($"Source:'{document.Name}'");
					sb.Append(document.Metadata.ToString());
					sb.AppendLine("===");
				}
			}

			return this.TokenizerService.TruncateContent(sb.ToString(), this.MaxContextTokens);
		}

		/// <summary>
		/// Summarizes the content of a specified document.
		/// </summary>
		/// <param name="document_name">The name of the document to summarize.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the summary as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentSearchTools.summarize_document]")]
		protected virtual async Task<string> summarize_document(

			[Description("[DocumentSearchTools.summarize_document.document_name]")]
			string document_name)
		{
			var document = await this.EmbeddingStorageService.RetrieveAsync(
				_collectionName,
				document_name,
				true);

			if (document == null)
				return $"Unable to read \"{document_name}\"";

			// when summarizing, we use the first chunks + the main centroid chunks.

			var sb = new StringBuilder();
			var embedding = document.GetEmbedding();

			sb.AppendLine(embedding.Chunks[0]);

			if (embedding.Vectors.Length < this.MaxClusters)
			{
				for (var i = 1; i < embedding.Vectors.Length; i++)
				{
					sb.AppendLine(embedding.Chunks[i]);
				}
			}
			else
			{
				var clusters = embedding.Vectors.ComputeClusters(this.MaxClusters);
				for (int i = 0; i < clusters.Length; i++)
				{
					var centroid = clusters[i].Centroid;
					var similarity = centroid.CosineSimilarity(embedding.Vectors);
					var maxIndex = Array.IndexOf(similarity, similarity.Max());
					if (maxIndex > 0)
						sb.AppendLine(embedding.Chunks[maxIndex]);
				}
			}

			var text = 
@$"
Name:'{document.Name}'
{document.Metadata.ToString()}
===
{sb.ToString()}
===
";

			return this.TokenizerService.TruncateContent(text, this.MaxContextTokens);
		}

		#endregion
	}
}
