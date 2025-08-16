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

using System.ComponentModel;
using System.Threading.Tasks;
using Wisej.Web;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Provides mathematical tools and utilities for evaluating expressions.
	/// </summary>
	/// <remarks>
	/// This class is part of the ToolsContainer and offers methods to perform mathematical operations.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[MathTools]")]
	public class MathTools : ToolsContainer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MathTools"/> class.
		/// </summary>
		public MathTools()
		{
		}

		#region Tools

		/// <summary>
		/// Evaluates a mathematical expression asynchronously.
		/// </summary>
		/// <param name="expression">The mathematical expression to evaluate.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the evaluated result of the expression.</returns>
		/// <remarks>
		/// This method uses the browser's evaluation engine to compute the result of the given expression.
		/// </remarks>
		[SmartTool.Tool]
		[Description("[MathTools.evaluate_expression]")]
		protected virtual Task<dynamic> evaluate_expression(

			[Description("[MathTools.evaluate_expression.expression]")]
			string expression)
		{
			var result = Application.EvalAsync(expression);
			Application.Update(Application.Current);
			return result;
		}

		#endregion

	}
}
