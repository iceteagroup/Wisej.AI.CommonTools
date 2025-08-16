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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Wisej.AI.Helpers;
using Wisej.AI.Services;
using Wisej.Services;
using Wisej.Web;
using static Wisej.AI.SmartSession;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Represents a set of tools for interacting with a database, including schema
	/// initialization and SQL execution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class provides functionalities to manage database connections, retrieve schema information, and execute SQL queries.
	/// It utilizes the <see cref="ITokenizerService"/> to truncate the generated context to fit within the <see cref="MaxContextTokens"/> value.
	/// </para>
	/// <para>
	/// By overriding the virtual properties in this class, you can provide predefined values for 
	/// connection, schema, columns, and server type. This allows for greater flexibility and 
	/// customization in how the class is used.
	/// </para>
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[DatabaseTools]")]
	public class DatabaseTools : ToolsContainer
	{
		private string _schema;
		private string _serverType;
		private DataTable _columns;
		private DbConnection _connection;

		/// <summary>
		/// Gets or sets the tokenizer service used for truncating context tokens.
		/// </summary>
		[Inject]
		protected ITokenizerService TokenizerService { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseTools"/> class with optional parameters.
		/// </summary>
		/// <remarks>
		/// You can provide the schema in three ways:
		/// <list type="bullet">
		/// <item>
		/// <description>Provide the <paramref name="columns"/> data table, which can be retrieved using <c>connection.GetSchema("Columns")</c>.</description>
		/// </item>
		/// <item>
		/// <description>Provide the <paramref name="schema"/> string, formatted in a way that is understandable by the LLM.</description>
		/// </item>
		/// <item>
		/// <description>If both parameters are null, you can override either the <see cref="Columns"/> or the <see cref="Schema"/> properties.</description>
		/// </item>
		/// </list>
		/// </remarks>
		/// <param name="connection">The database connection to use. Default is <c>null</c>.</param>
		/// <param name="columns">The data table containing "Columns" schema table. Default is <c>null</c>.</param>
		/// <param name="schema">The schema of the database. Default is <c>null</c>.</param>
		/// <param name="serverType">The type of the database server. Default is <c>null</c>.</param>
		public DatabaseTools(
			DbConnection connection = null,
			DataTable columns = null,
			string schema = null,
			string serverType = null)
		{
			_schema = schema;
			_columns = columns;
			_connection = connection;
			_serverType = serverType;

			InitializeSchema();

			Application.Services.Inject(this);
		}

		/// <summary>
		/// Gets the database connection.
		/// </summary>
		[DefaultValue(null)]
		public virtual DbConnection Connection
			=> _connection;

		/// <summary>
		/// Gets the data table containing column information.
		/// </summary>
		[DefaultValue(null)]
		public virtual DataTable Columns
			=> _columns;

		/// <summary>
		/// Gets the schema of the database.
		/// </summary>
		[DefaultValue(null)]
		public virtual string Schema
			=> _schema;

		/// <summary>
		/// Gets the type of the database server.
		/// </summary>
		public virtual string ServerType
			=> _serverType;

		/// <summary>
		/// Gets or sets the maximum number of context tokens.
		/// </summary>
		[DefaultValue(4096)]
		public virtual int MaxContextTokens
		{
			get;
			set;
		} = 4096;

		#region Tools

		/// <summary>
		/// Executes a SQL select query and returns the result as a string.
		/// </summary>
		/// <param name="sql">The SQL query to execute.</param>
		/// <returns>The result of the SQL query as a string.</returns>
		[SmartTool.Tool]
		[Description("[DatabaseTools.select]")]
		protected virtual string select(

			[Description("[DatabaseTools.select.sql]")]
			string sql)
		{
			// strip ```sql delimiters, some models can't help themselves.
			if (!String.IsNullOrEmpty(sql))
				sql = GetSQL(new Message { Text = sql });

			var data = ExecuteSelect(sql);
			return data;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Builds a database schema representation from the provided columns.
		/// </summary>
		/// <param name="columns">The data table containing column information.</param>
		/// <returns>A string representation of the database schema.</returns>
		/// <remarks>
		/// If the columns parameter is null, the method returns "Missing columns schema".
		/// </remarks>
		protected virtual string BuildDatabaseSchema(DataTable columns)
		{
			if (columns == null)
				return "Missing columns schema";

			var tables = new Dictionary<string, List<string>>();
			foreach (DataRow row in columns.Rows)
			{
				var dataType = (string)row["DATA_TYPE"];
				var tableName = (string)row["TABLE_NAME"];
				var columnName = (string)row["COLUMN_NAME"];

				if (!tables.TryGetValue(tableName, out List<string> tableColumns))
				{
					tables[tableName] = tableColumns = new List<string>();
				}

				tableColumns.Add($"[{columnName}] {dataType}");
			}

			var sb = new StringBuilder();
			sb.AppendLine();
			foreach (var table in tables)
			{
				sb.Append($"- [{table.Key}] (");
				sb.Append(String.Join(",", table.Value));
				sb.AppendLine(")");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns the JSON string returned in the <paramref name="message"/> by stripping
		/// the enclosing markers (```sql and ```) if present.
		/// </summary>
		/// <param name="message">Message with the response text that may be a JSON string.</param>
		/// <returns>JSON string.</returns>
		protected virtual string GetSQL(Message message)
			=> RegexHelper.GetSQL(message.Text);

		#endregion

		#region Implementation

		//
		private void InitializeSchema()
		{
			var schema = this.Schema;
			var connection = this.Connection;
			var serverType = this.ServerType;

			if (connection == null)
				return;

			// detect server type
			if (serverType == null)
				serverType = DetectServerType(connection.GetType().Name);

			// get the database name
			var databaseName = connection.Database;

			// build the schema
			if (schema == null)
			{
				var closeConnection = false;
				var columns = this.Columns;

				if (columns == null)
				{
					if (connection.State != ConnectionState.Open)
					{
						connection.Open();
						closeConnection = true;
					}

					try
					{
						columns = connection.GetSchema("Columns");
					}
					finally
					{
						if (closeConnection)
							connection.Close();
					}
				}

				schema = BuildDatabaseSchema(columns);
			}

			this.Parameters.Add("server_type", serverType);
			this.Parameters.Add("database_schema", schema);
			this.Parameters.Add("database_name", databaseName);
		}

		//
		private string DetectServerType(string connectionType)
		{
			if (connectionType.IndexOf("SqlConnection") > -1)
				return "SQLServer";

			if (connectionType.IndexOf("Oracle") > -1)
				return "Oracle";

			if (connectionType.IndexOf("MySql") > -1)
				return "MySql";

			if (connectionType.IndexOf("Postgre") > -1)
				return "PostgreSQL";

			if (connectionType.IndexOf("DB2") > -1)
				return "DB2";

			if (connectionType.EndsWith("Connection"))
				return connectionType.Substring(0, connectionType.Length - "Connection".Length);

			return connectionType;
		}

		//
		private string ExecuteSelect(string sql)
		{
			var connection = this.Connection;
			if (connection == null)
				return "Missing Connection";

			var closeConnection = false;
			if (connection.State != ConnectionState.Open)
			{
				connection.Open();
				closeConnection = true;
			}

			try
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = sql;
					var dataTable = new DataTable();
					dataTable.Load(command.ExecuteReader());

					// build the response table
					var sb = new StringBuilder();

					// header
					sb.AppendLine(
						String.Join(
							"|",
							dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName))
					);

					// rows
					var tokensCount = 0;
					var maxTokens = this.MaxContextTokens;
					foreach (DataRow row in dataTable.Rows)
					{
						var line = String.Join(
							"|",
							row.ItemArray.Select(
								x =>
								{
									if (x is string)
										return "\"" + Convert.ToString(x).Replace("\"", "\\\"") + "\"";
									else
										return Convert.ToString(x);
								}
							)
						);

						tokensCount += this.TokenizerService.CountTokens(line);
						if (tokensCount > maxTokens)
							break;

						sb.AppendLine(line);
					}

					return sb.ToString();
				}
			}
			finally
			{
				if (closeConnection)
					connection.Close();
			}
		}

		#endregion
	}
}
