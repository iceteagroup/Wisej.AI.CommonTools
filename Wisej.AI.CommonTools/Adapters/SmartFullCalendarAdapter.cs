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
using Wisej.Web.Ext.FullCalendar;

namespace Wisej.AI.Adapters
{
	/// <summary>
	/// Represents an adapter for the FullCalendar control, extending the functionality of
	/// the SmartCalendarAdapter.
	/// </summary>
	/// <remarks>
	/// This class is designed to integrate with the FullCalendar control, providing
	/// additional tools and functionality specific to FullCalendar.
	/// </remarks>
	[ApiCategory("SmartAdapter")]
	[Extends(typeof(FullCalendar), allowMultiple: false)]
	[Description("Enhances a FullCalendar control with several AI features.")]
	public class SmartFullCalendarAdapter : SmartCalendarAdapter
	{
		/// <summary>
		/// Gets the FullCalendar control associated with this adapter.
		/// </summary>
		/// <remarks>
		/// This property retrieves the first FullCalendar control found within the adapter's controls collection.
		/// </remarks>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FullCalendar FullCalendar
			=> (FullCalendar)this.Controls.FirstOrDefault();

		/// <summary>
		/// Called when a control is created within the adapter.
		/// </summary>
		/// <param name="control">The control that has been created.</param>
		/// <remarks>
		/// This method initializes the FullCalendarTools with the associated FullCalendar control and then calls the base implementation.
		/// </remarks>
		protected override void OnControlCreated(Control control)
		{
			this.UseTools(new FullCalendarTools(this.FullCalendar));

			base.OnControlCreated(control);
		}
	}
}
