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
using Wisej.Web.Ext.FullCalendar;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Provides tools to manage a calendar control, allowing for the addition, retrieval,
	/// deletion, and modification of events.
	/// </summary>
	[ApiCategory("Tools")]
	[Description("Provides tools to manage a calendar control.")]
	public class FullCalendarTools : ToolsContainer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FullCalendarTools"/> class with the specified calendar.
		/// </summary>
		/// <param name="calendar">The calendar to be managed by this tool.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided calendar is null.</exception>
		public FullCalendarTools(FullCalendar calendar)
		{
			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar));

			this.Calendar = calendar;
		}

		/// <summary>
		/// Gets or sets the calendar associated with this tool.
		/// </summary>
		[DefaultValue(null)]
		public FullCalendar Calendar
		{
			get;
			set;
		}

		#region Tools

		/// <summary>
		/// Adds one or more events to the calendar.
		/// </summary>
		/// <param name="titles">Titles of the events. Create a short description of the event from the question. Fix for typos and grammar error, do not repeat the date of the event.</param>
		/// <param name="start">Start date/time of the events. When not specified, deduct it from the type of event.</param>
		/// <param name="end">Ending date/time for the events. Deduct the ending date from the starting date and the title of the event.</param>
		/// <param name="allDay">Indicates whether the event is a full day event.</param>
		[SmartTool.Tool]
		[Description("Adds one or more events to the schedule. Use once for all events to add.")]
		protected virtual void add_events(

			[Description("Titles of the events. Create a short description of the event from the question. Fix for typos and grammar error, do not repeat the date of the event.")]
			string[] titles,

			[Description("Start date/time of the events. When not specified, deduct it from the type of event.")]
			DateTime[] start,

			[Description("Ending date/time for the events. Deduct the ending date from the starting date and the title of the event.")]
			DateTime[] end,

			[Description("Indicates whether the event is a full day event.")]
			bool[] allDay)
		{
			var calendar = this.Calendar;
			if (titles != null && start != null && end != null && allDay != null)
			{
				var count = new[] { titles.Length, start.Length, end.Length, allDay.Length }.Min();

				var eventsToAdd = new Event[count];
				var nextId = calendar.Events.Count + 1;
				for (var i = 0; i < count; i++)
				{
					if (allDay[i])
					{
						end[i] = start[i] = start[i].Date;
					}

					var ev = new Event
					{
						Id = $"{nextId++}",
						Title = titles[i],
						Start = start[i],
						End = end[i],
						AllDay = allDay[i]
					};

					eventsToAdd[i] = ev;
				}

				calendar.Events.AddRange(eventsToAdd);
			}
		}

		/// <summary>
		/// Retrieves events from the calendar within the specified date range.
		/// </summary>
		/// <param name="fromDate">Start date/time of the range requested.</param>
		/// <param name="toDate">End date/time of the range requested.</param>
		/// <returns>An array of objects containing the title, date, time, and duration of the events.</returns>
		[SmartTool.Tool]
		[Description("Returns the title, date, time and duration of the events between fromDate and toDate.")]
		protected virtual object[] get_events(

			[Description("Start date/time of the range requested.")]
			DateTime fromDate,

			[Description("End date/time of the range requested.")]
			DateTime toDate)
		{
			var calendar = this.Calendar;

			fromDate = fromDate.Date;
			toDate = toDate.Date.AddDays(1).AddTicks(-1);
			var events = calendar.Events.Where(e => e.Start >= fromDate && e.End <= toDate);

			return events.Select(e => new
			{
				id = e.Id,
				title = e.Title,
				start_datetime = e.Start,
				end_datetime = e.AllDay ? e.Start.AddDays(1) : e.End

			}).ToArray();
		}

		/// <summary>
		/// Deletes events from the calendar identified by their IDs.
		/// </summary>
		/// <param name="ids">IDs of the events to remove from the calendar.</param>
		[SmartTool.Tool]
		[Description("Deletes the events identified by their IDs from the calendar.")]
		protected virtual void delete_events(
			[Description("IDs of the events to remove from the calendar.")]
			string[] ids)
		{
			var calendar = this.Calendar;

			if (ids != null && ids.Length > 0)
			{
				var eventsToDelete = calendar.Events.Where(e => ids.Contains(e.Id)).ToArray();

				foreach (var e in eventsToDelete)
				{
					calendar.Events.Remove(e);
				}
			}
		}

		/// <summary>
		/// Changes the title, start date/time, and ending date/time of the events identified by their IDs.
		/// </summary>
		/// <param name="ids">IDs of the events to change.</param>
		/// <param name="newTitles">New title of the events.</param>
		/// <param name="newStarts">New start date/time of the events.</param>
		/// <param name="newEnds">New ending date/time of the events.</param>
		[SmartTool.Tool]
		[Description("Changes the title, the start date/time and the ending date/time of the events identified by their IDs.")]
		protected virtual void change_events(
			[Description("IDs of the events to change.")]
			string[] ids,

			[Description("New title of the events.")]
			string[] newTitles,

			[Description("New start date/time of the events.")]
			DateTime[] newStarts,

			[Description("New ending date/time of the events")]
			DateTime[] newEnds)
		{
			var calendar = this.Calendar;

			if (ids != null && newTitles != null && newStarts != null && newEnds != null)
			{
				var count = new[] { ids.Length, newTitles.Length, newStarts.Length, newEnds.Length }.Min();

				var eventsToChange = calendar.Events.Where(e => ids.Contains(e.Id)).Take(count).ToArray();

				for (var i = 0; i < eventsToChange.Length; i++)
				{
					var e = eventsToChange[i];
					e.Title = newTitles[i];
					e.Start = newStarts[i];
					e.End = newEnds[i];
				}
			}
		}

		#endregion

	}
}
