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
using System.Drawing;
using System.Linq;
using Wisej.AI.Helpers;
using Wisej.Core;
using Wisej.Web.Ext.ChartJS3;

namespace Wisej.AI.Tools
{
	/// <summary>
	/// Represents a container for ChartJS3 tools, providing functionalities to manipulate and build charts.
	/// </summary>
	/// <remarks>
	/// This class is designed to work with ChartJS3 objects, allowing the AI to configure
	/// and build various types of charts.
	/// </remarks>
	[ApiCategory("Tools")]
	[Description("[ChartJS3Tools]")]
	public class ChartJS3Tools : ToolsContainer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChartJS3Tools"/> class with the specified chart.
		/// </summary>
		/// <param name="chart">The ChartJS3 object to be associated with this tools container.</param>
		/// <exception cref="ArgumentNullException">Thrown when the provided chart is null.</exception>
		public ChartJS3Tools(ChartJS3 chart)
		{
			if (chart == null)
				throw new ArgumentNullException(nameof(chart));

			this.Chart = chart;
		}

		/// <summary>
		/// Gets or sets the ChartJS3 object associated with this tools container.
		/// </summary>
		[DefaultValue(null)]
		public ChartJS3 Chart
		{
			get;
			set;
		}

		#region Tools

		/// <summary>
		/// Retrieves the dataset schema for the specified chart type.
		/// </summary>
		/// <param name="chartType">The type of chart for which to get the dataset schema.</param>
		/// <returns>An object representing the dataset schema for the specified chart type.</returns>
		[SmartTool.Tool]
		[Description("[ChartJS3Tools.get_dataset_schema]")]
		protected virtual object get_dataset_schema(

			[Description("[ChartJS3Tools.get_dataset_schema.chartType]")]
			ChartType chartType)
		{
			return GetDataSetSchema(chartType);
		}

		/// <summary>
		/// Builds a chart with the specified title, labels, and datasets.
		/// </summary>
		/// <param name="title">The title of the chart.</param>
		/// <param name="labels">The labels for the chart's data points.</param>
		/// <param name="dataSets">The datasets to be included in the chart.</param>
		/// <remarks>
		/// This method configures the chart's datasets and appearance based on the provided parameters.
		/// </remarks>
		[SmartTool.Tool]
		[Description("[ChartJS3Tools.build_chart]")]
		protected virtual void build_chart(

			[Description("[ChartJS3Tools.build_chart.title]")]
			string title,

			[Description("[ChartJS3Tools.build_chart.labels]")]
			string[] labels,

			[Description("[ChartJS3Tools.build_chart.dataSets]")]
			dynamic[] dataSets)
		{
			var chart = this.Chart;

			chart.DataSets.Clear();
			chart.Labels = labels;
			chart.Options.Plugins.Title.Text = title;

			foreach (var dataSet in dataSets)
			{
				if (!Enum.TryParse<ChartType>(dataSet.type, true, out ChartType chartType))
					chartType = chart.ChartType;

				switch (chartType)
				{
					case ChartType.Line:
						chart.DataSets.Add(new LineDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 3,
							BorderColor = TranslateColor(dataSet.borderColor),
							BackgroundColor = TranslateColor(dataSet.backgroundColor),
							ShowLine = dataSet.showLine ?? true,
							Fill = dataSet.fill ?? false,
							LineTension = dataSet.lineTension ?? 0.4
						});
						break;

					case ChartType.Pie:
						chart.DataSets.Add(new PieDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 0,
							BorderColor = TranslateColors(dataSet.borderColor),
							BackgroundColor = TranslateColors(dataSet.backgroundColor)
						});
						break;

					case ChartType.Radar:
						chart.DataSets.Add(new RadarDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 0,
							BorderColor = TranslateColors(dataSet.borderColor),
							BackgroundColor = TranslateColors(dataSet.backgroundColor),
							LineTension = dataSet.lineTension ?? 0.0
						});
						break;

					case ChartType.PolarArea:
						chart.DataSets.Add(new PolarAreaDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 0,
							BorderColor = TranslateColors(dataSet.borderColor),
							BackgroundColor = TranslateColors(dataSet.backgroundColor)
						});
						break;

					case ChartType.Doughnut:
						chart.DataSets.Add(new DoughnutDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 0,
							BorderColor = TranslateColors(dataSet.borderColor),
							BackgroundColor = TranslateColors(dataSet.backgroundColor)
						});
						break;

					default:
					case ChartType.Bar:
						chart.DataSets.Add(new BarDataSet
						{
							Label = dataSet.label,
							Data = ((Array)dataSet.data).Cast<object>().ToArray(),
							Formatted = (string[])dataSet.formatted,
							BorderWidth = dataSet.borderWidth ?? 0,
							BorderColor = TranslateColors(dataSet.borderColor),
							BackgroundColor = TranslateColors(dataSet.backgroundColor),
							Stack = dataSet.stack
						});
						break;
				}
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Translates a color object into a <see cref="Color"/> instance.
		/// </summary>
		/// <param name="color">The color object to translate.</param>
		/// <returns>A <see cref="Color"/> instance representing the translated color.</returns>
		protected virtual Color TranslateColor(object color)
		{
			try
			{
				if (color is string hexColor)
				{
					return ColorHelper.ConvertFromString(hexColor);
				}

				if (color is string[] hexColors && hexColors.Length > 0)
				{
					return ColorHelper.ConvertFromString(hexColors[0]);
				}
			}
			catch { }

			return Color.Empty;
		}

		/// <summary>
		/// Translates an array of color objects into an array of <see cref="Color"/> instances.
		/// </summary>
		/// <param name="colors">The array of color objects to translate.</param>
		/// <returns>An array of <see cref="Color"/> instances representing the translated colors.</returns>
		protected virtual Color[] TranslateColors(object colors)
		{
			try
			{
				var list = new List<Color>();

				if (colors is string[] hexColors && hexColors.Length > 0)
				{
					for (int i = 0; i < hexColors.Length; i++)
					{
						list.Add(ColorHelper.ConvertFromString(hexColors[i]));
					}
				}

				else if (colors is string hexColor)
				{
					list.Add(ColorHelper.ConvertFromString(hexColor));
				}

				return
					list.Count > 0
					? list.ToArray()
					: null;
			}
			catch { }

			return null;
		}

		/// <summary>
		/// Retrieves the dataset schema for a given chart type.
		/// </summary>
		/// <param name="chartType">The type of chart for which to retrieve the dataset schema.</param>
		/// <returns>An object representing the dataset schema for the specified chart type.</returns>
		protected virtual object GetDataSetSchema(ChartType chartType)
		{
			Type dataSetType = null;
			var schema = new DynamicObject();

			switch (chartType)
			{
				case ChartType.Line:
					dataSetType = typeof(LineDataSet);
					break;

				case ChartType.Pie:
					dataSetType = typeof(PieDataSet);
					break;

				case ChartType.Doughnut:
					dataSetType = typeof(DoughnutDataSet);
					break;

				case ChartType.Radar:
					dataSetType = typeof(RadarDataSet);
					break;

				case ChartType.PolarArea:
					dataSetType = typeof(PolarAreaDataSet);
					break;

				default:
				case ChartType.Bar:
					dataSetType = typeof(BarDataSet);
					break;
			}

			var properties = TypeDescriptor.GetProperties(dataSetType);
			foreach (PropertyDescriptor property in properties)
			{
				if (!_supportedProperties.Contains(property.Name))
					continue;

				var type = property.PropertyType;
				var description = property.Description;
				var defaultValueAttr = ((DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)]);

				var defaultValue = "";
				if (defaultValueAttr != null)
				{
					var value = defaultValueAttr.Value ?? "null";
					defaultValue = Convert.ToString(value);
				}

				var nullableType = Nullable.GetUnderlyingType(type);
				if (nullableType != null)
					type = nullableType;

				// fix descriptions
				switch (property.Name)
				{
					case "Data":
						description = "Numbers representing the data to plot.";
						break;

					case "Formatted":
						description = "Labels for the values to display on the chart.";
						break;

					case "Type":
						description = "Type of chart (required).";
						break;
				}

				var field = new DynamicObject();
				field["type"] = GetTypeName(type);
				if (!String.IsNullOrEmpty(description))
					field["description"] = description;
				if (!String.IsNullOrEmpty(defaultValue))
					field["default"] = defaultValue;

				var fieldName = property.Name.ToCamelCase();
				schema[fieldName] = field;
			}

			return schema;
		}

		//
		private static string GetEnumValues(Type type)
		{
			if (!type.IsEnum)
				return "";

			return String.Join("|", Enum.GetNames(type));
		}

		//
		private static string GetTypeName(Type type)
		{
			var typeName = "";

			if (type.IsEnum)
			{
				typeName = $"string (values: {GetEnumValues(type)})";
			}
			else if (type.IsArray)
			{
				typeName = $"{GetTypeName(type.GetElementType())}[]";
			}
			else if (type == typeof(Color))
			{
				typeName = $"color";
			}
			else
			{
				switch (System.Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						typeName = "boolean";
						break;

					case TypeCode.String:
						typeName = "string";
						break;

					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						typeName = "integer";
						break;

					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.Decimal:
						typeName = "number";
						break;

					case TypeCode.DateTime:
						typeName = "string";
						break;

					case TypeCode.Object:
						typeName = "json";
						break;

					default:
						typeName = "string";
						break;
				}
			}

			return typeName;
		}

		private static string[] _supportedProperties = new[] {
				"Label",
				"Data",
				"Type",
				"Formatted",
				"BorderWidth",
				"BorderColor",
				"BackgroundColor",
				"LineTension",
				"Stack",
				"Fill",
				"ShowLine"
		};

		#endregion
	}
}
