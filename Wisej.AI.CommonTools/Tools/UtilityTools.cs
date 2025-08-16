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
using System.Threading.Tasks;
using Wisej.Ext.Geolocation;
using Wisej.Web;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Represents a collection of utility tools that provide various functionalities such
	/// as retrieving the current date and time, and obtaining the user's location.
	/// </summary>
	/// <remarks>
	/// This class extends the <c>ToolsContainer</c> and includes methods that can be used
	/// to perform common utility operations.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[UtilityTools]")]
	public class UtilityTools : ToolsContainer
	{
		Geolocation _geolocation;

		/// <summary>
		/// Finalizes an instance of the <see cref="UtilityTools"/> class.
		/// </summary>
		/// <remarks>
		/// This destructor ensures that the <c>Geolocation</c> resource is properly disposed of when the <c>UtilityTools</c> object is garbage collected.
		/// </remarks>
		~UtilityTools()
		{
			_geolocation?.Dispose();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UtilityTools"/> class.
		/// </summary>
		public UtilityTools()
		{
		}

		#region Tools

		/// <summary>
		/// Retrieves the current date and time formatted as a long date and time string.
		/// </summary>
		/// <remarks>
		/// This method returns the current date and time adjusted to the client's time zone.
		/// </remarks>
		/// <returns>A string representing the current date and time.</returns>
		[SmartTool.Tool]
		[Description("[UtilityTools.get_current_date_time]")]
		protected virtual string get_current_date_time()
		{
			var now = DateTime.Now.ToClientTime();
			return now.ToLongDateString() + " " + now.ToLongTimeString();
		}

		/// <summary>
		/// Asynchronously retrieves the user's current location as a JSON object.
		/// </summary>
		/// <remarks>
		/// This method uses the <c>Geolocation</c> service to obtain the user's current position and returns it as a JSON string.
		/// </remarks>
		/// <returns>A task that represents the asynchronous operation. The task result contains a JSON string of the user's location.</returns>		[SmartTool.Tool]
		[SmartTool.Tool]
		[Description("[UtilityTools.get_user_location]")]
		protected virtual async Task<string> get_user_location()
		{
			_geolocation = _geolocation ?? new Geolocation();

			var tcs = new TaskCompletionSource<string>();
			_geolocation.GetCurrentPosition((position) =>
			{
				tcs.SetResult(position.ToString());
			});

			Application.Update(Application.Current);
			return await tcs.Task;
		}

		#endregion

	}
}
