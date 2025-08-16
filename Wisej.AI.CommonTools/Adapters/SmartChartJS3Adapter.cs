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
using System.Linq;
using Wisej.AI.Tools;
using Wisej.Web;
using Wisej.Web.Ext.ChartJS3;

namespace Wisej.AI.Adapters
{
	/// <summary>
	/// Represents an adapter that enhances a ChartJS control with several AI features.
	/// </summary>
	/// <remarks>
	/// This class extends the functionality of the ChartJS3 control by integrating AI capabilities.
	/// It is part of the SmartAdapter API category and does not allow multiple extensions.
	/// </remarks>
	[ApiCategory("SmartAdapter")]
	[Extends(typeof(ChartJS3), allowMultiple: false)]
	[Description("Enhances a ChartJS control with several AI features.")]
	public class SmartChartJS3Adapter : SmartChartAdapter
	{
		/// <summary>
		/// Gets the ChartJS3 control associated with this adapter.
		/// </summary>
		/// <remarks>
		/// This property is not browsable in the property grid and is hidden from designer serialization.
		/// </remarks>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ChartJS3 ChartJS3
			=> (ChartJS3)this.Controls.FirstOrDefault();

		/// <summary>
		/// Called when a control is created.
		/// </summary>
		/// <param name="control">The control that was created.</param>
		/// <remarks>
		/// This method initializes the ChartJS3Tools with the associated ChartJS3 control.
		/// It ensures that the base class's OnControlCreated method is also called.
		/// </remarks>
		protected override void OnControlCreated(Control control)
		{
			this.UseTools(new ChartJS3Tools(this.ChartJS3));

			base.OnControlCreated(control);
		}
	}
}
