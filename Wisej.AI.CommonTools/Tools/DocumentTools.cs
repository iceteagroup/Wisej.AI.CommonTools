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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
	/// Provides tools for processing, embedding, and querying documents using AI services.
	/// </summary>
	/// <remarks>
	/// The <see cref="DocumentTools"/> class enables document processing tasks such as:
	/// <list type="bullet">
	///   <item><description>Embedding documents for similarity and summarization tasks.</description></item>
	///   <item><description>Querying documents based on semantic similarity.</description></item>
	///   <item><description>Summarizing document content using clustering techniques.</description></item>
	/// </list>
	/// It integrates with various AI services, including tokenization, text splitting, document conversion, and embedding generation.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[DocumentTools]")]
	public class DocumentTools : ToolsContainer
	{
		private EmbeddedDocument _document;

		/// <summary>
		/// Gets or sets the tokenizer service used for truncating context tokens.
		/// </summary>
		[Inject]
		protected ITokenizerService TokenizerService { get; set; }

		/// <summary>
		/// Gets or sets the text splitter service used for splitting text into smaller chunks.
		/// </summary>
		[Inject]
		protected ITextSplitterService TextSplitterService { get; set; }

		/// <summary>
		/// Gets or sets the document conversion service used for converting documents to text.
		/// </summary>
		[Inject]
		protected IDocumentConversionService ConversionService { get; set; }

		/// <summary>
		/// Gets or sets the embedding generation service used for generating embeddings from text.
		/// </summary>
		[Inject]
		protected IEmbeddingGenerationService EmbeddingGenerationService { get; set; }

		/// <summary>
		/// Gets or sets the reranking service.
		/// </summary>
		[Inject]
		protected IRerankingService RerankingService { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentTools"/> class.
		/// </summary>
		/// <remarks>
		/// This constructor injects the required services into the <see cref="DocumentTools"/> instance using the application's service provider.
		/// </remarks>
		public DocumentTools()
		{
			Application.Services.Inject(this);
		}

		#region Properties

		/// <summary>
		/// Gets or sets the file path associated with the document.
		/// </summary>
		/// <remarks>
		/// Setting this property will reset the internal document and stream references. 
		/// If the value changes, the <see cref="Stream"/> and the internal document are set to <c>null</c>.
		/// </remarks>
		[DefaultValue(null)]
		public string FilePath
		{
			get => _filePath;
			set
			{
				if (_filePath != value)
				{
					_filePath = value;
					_document = null;
					_stream = null;
				}
			}
		}
		private string _filePath;

		/// <summary>
		/// Gets or sets the stream associated with the document.
		/// </summary>
		/// <remarks>
		/// Setting this property will reset the internal document and file path references. 
		/// If the value changes, the <see cref="FilePath"/> and the internal document are set to <c>null</c>.
		/// </remarks>
		[DefaultValue(null)]
		public Stream Stream
		{
			get => _stream;
			set
			{
				if (_stream != value)
				{
					_stream = value;
					_document = null;
					_filePath = null;
				}
			}
		}
		private Stream _stream;

		/// <summary>
		/// Gets or sets the file type of the document.
		/// </summary>
		/// <remarks>
		/// Setting this property will reset the internal document. 
		/// </remarks>
		[DefaultValue(null)]
		public string FileType
		{
			get => _fileType;
			set
			{
				if (_fileType != value)
				{
					_fileType = value;
					_document = null;
				}
			}
		}
		private string _fileType;

		/// <summary>
		/// Gets or sets the number of top chunks to retrieve.
		/// </summary>
		[DefaultValue(10)]
		public int TopN
		{
			get;
			set;
		} = 10;

		/// <summary>
		/// Get or sets the maximum number of vector clusters to
		/// generate when performing summarization tasks.
		/// </summary>
		[DefaultValue(5)]
		public int MaxClusters
		{
			get;
			set;
		} = 5;

		/// <summary>
		/// Gets or sets the minimum similarity threshold for document retrieval.
		/// </summary>
		[DefaultValue(0.25f)]
		public float MinSimilarity
		{
			get;
			set;
		} = 0.25f;

		/// <summary>
		/// Gets or sets the maximum number of context tokens.
		/// </summary>
		[DefaultValue(4096)]
		public int MaxContextTokens
		{
			get;
			set;
		} = 4096;

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

		//
		private async Task EnsureDocumentEmbeddingsAsync()
		{
			if (_document == null)
			{
				var filePath = this.FilePath;
				var fileType = this.FileType;

				if (String.IsNullOrEmpty(fileType) && !String.IsNullOrEmpty(filePath))
					fileType = Path.GetExtension(filePath);

				var stream = this.Stream;
				var documentName = Path.GetFileName(filePath) ?? "";

				if (stream == null && !String.IsNullOrEmpty(filePath))
					stream = File.OpenRead(filePath);

				try
				{
					var metadata = new Metadata();
					var text = this.ConversionService.Convert(stream, fileType, metadata);
					var chunks = this.TextSplitterService.Split(text);
					var embedding = await this.EmbeddingGenerationService.EmbedAsync(chunks);

					_document = new EmbeddedDocument(documentName, metadata, embedding);
				}
				finally
				{
					if (_stream == null && stream != null)
						stream.Dispose();
				}
			}
		}

		#endregion

		#region Tools

		/// <summary>
		/// Queries a single document based on the provided document name and question.
		/// </summary>
		/// <param name="question">The question to query the document for.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the query result as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentTools.query_document]")]
		protected virtual async Task<string> query_document(

			[Description("[DocumentTools.query_document.question]")]
			string question)
		{
			// ensure we have the embedded document.
			await EnsureDocumentEmbeddingsAsync();

			var document = _document;
			if (document == null)
				return "Failed reading the document";

			var embedding = document.GetEmbedding();
			var vectors = embedding.Vectors;
			var chunks = embedding.Chunks;

			var topN = this.TopN;
			var minSimilarity = this.MinSimilarity;
			var query = await EmbedQuestionAsync(question);
			var similarity = query.Vectors[0].CosineSimilarity(vectors);

			Array.Sort(similarity, chunks,
				Comparer<float>.Create((x, y)
					=> x == y
						? 0
						: x > y
							? -1
							: 1));

			var maxCount =
				Math.Min(
					topN,
					similarity.TakeWhile(s => s >= minSimilarity).Count());

			chunks = await RerankAsync(question, chunks.Take(maxCount).ToArray());

			return String.Join("\n", chunks.Take(maxCount));
		}

		/// <summary>
		/// Summarizes the content of a specified document.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation. The task result contains the summary as a string.</returns>
		[SmartTool.Tool]
		[Description("[DocumentTools.summarize_document]")]
		protected virtual async Task<string> summarize_document()
		{
			// ensure we have the embedded document.
			await EnsureDocumentEmbeddingsAsync();

			var document = _document;
			if (document == null)
				return "Failed reading the document";

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

			return sb.ToString();
		}

		#endregion
	}
}
