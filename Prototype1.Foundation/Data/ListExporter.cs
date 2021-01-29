using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Xml;

namespace Prototype1.Foundation.Data
{
    public static class ExporterExtensions
    {
        /// <summary>
        /// Exporter extension method for all IEnumerableOfT
        /// </summary>
        public static FluentExporter<T> GetExporter<T>(
            this IEnumerable<T> source, string seperator = ",") where T : class
        {
            return new FluentExporter<T>(source, seperator);
        }
    }

    /// <summary>
    /// Represents custom exportable column with a expression for the property name
    /// and a custom format string
    /// </summary>
    public class ExportableColumn<T>
    {
        public Expression<Func<T, object>> Func { get; private set; }
        public string HeaderString { get; private set; }
        public string CustomFormatString { get; private set; }

        public ExportableColumn(
            Expression<Func<T, object>> func,
            string headerString = "",
            string customFormatString = "")
        {
            Func = func;
            HeaderString = headerString;
            CustomFormatString = customFormatString;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NotExportedAttribute : Attribute
    {
    }

    /// <summary>
    /// Exporter that uses Expression tree parsing to work out what values to export for 
    /// columns, and will use additional data as specified in the List of ExportableColumn
    /// which defines whethere to use custom headers, or formatted output
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FluentExporter<T> where T : class
    {
        private readonly List<ExportableColumn<T>> _columns = new List<ExportableColumn<T>>();

        private readonly Dictionary<Expression<Func<T, object>>, Func<T, object>> _compiledFuncLookup =
            new Dictionary<Expression<Func<T, object>>, Func<T, object>>();

        private readonly List<string> _headers = new List<string>();
        private readonly IEnumerable<T> _sourceList;
        private readonly string _seperator;
        private bool _doneHeaders;

        public FluentExporter(IEnumerable<T> sourceList, string seperator = ",")
        {
            _sourceList = sourceList;
            _seperator = seperator;
        }

        public FluentExporter<T> AddExportableColumn(
            Expression<Func<T, object>> func,
            string headerString = "",
            string customFormatString = "")
        {
            _columns.Add(new ExportableColumn<T>(func, headerString, customFormatString));
            return this;
        }

        public FluentExporter<T> AddAllExportableColumns()
        {
            foreach (var ex in from prop in typeof (T).GetProperties()
                let input = Expression.Parameter(typeof (T), "x")
                let propExpression = Expression.Property(input, prop)
                let converter = Expression.Convert(propExpression, typeof (object))
                where prop.GetCustomAttribute<NotExportedAttribute>(true) == null
                select Expression.Lambda<Func<T, object>>(converter, new[] {input}))
            {
                _columns.Add(new ExportableColumn<T>(ex, string.Empty, string.Empty));
            }
            return this;
        }

        /// <summary>
        /// Export all specified columns as a string, 
        /// using seperator and column data provided
        /// where we may use custom or default headers 
        /// (depending on whether a custom header string was supplied)
        /// where we may use custom fomatted column data or default data 
        /// (depending on whether a custom format string was supplied)
        /// </summary>
        public void AsCsvString(TextWriter writer, bool readableFlag = true)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException(
                    "You need to specify at least one column to export value");

            foreach (var item in _sourceList)
            {
                var values = new List<string>();
                foreach (var exportableColumn in _columns)
                {
                    if (!_doneHeaders)
                    {
                        var header = string.IsNullOrEmpty(exportableColumn.HeaderString)
                            ? GetPropertyName(exportableColumn.Func)
                            : exportableColumn.HeaderString;
                        if (readableFlag)
                            header = header.SplitCamelCase();
                        _headers.Add(header);

                        var func = exportableColumn.Func.Compile();
                        _compiledFuncLookup.Add(exportableColumn.Func, func);
                        if (!string.IsNullOrEmpty(exportableColumn.CustomFormatString))
                        {
                            var value = func(item);
                            values.Add(value != null
                                ? string.Format(exportableColumn.CustomFormatString, value)
                                : "");
                        }
                        else
                        {
                            var value = func(item);
                            values.Add(value != null ? value.ToString() : "");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(exportableColumn.CustomFormatString))
                        {
                            var value = _compiledFuncLookup[exportableColumn.Func](item);
                            values.Add(value != null
                                ? string.Format(exportableColumn.CustomFormatString, value)
                                : "");
                        }
                        else
                        {
                            var value = _compiledFuncLookup[exportableColumn.Func](item);
                            values.Add(value != null ? value.ToString() : "");
                        }
                    }
                }
                if (!_doneHeaders)
                {
                    writer.WriteLine(_headers.Aggregate((start, end) => start + _seperator + end));
                    _doneHeaders = true;
                }
                writer.WriteLine(values.Aggregate((start, end) => start + _seperator + end.Replace(",", "")));
            }
        }

        /// <summary>
        /// Export all specified columns as a XML string, using column data provided
        /// and use custom headers depending on whether a custom header string was supplied.
        /// Use custom formatted column data or default data depending on whether a custom format string was supplied.
        /// </summary>
        public void ToXml(XmlTextWriter writer)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("You need to specify at least one element to export value");

            foreach (var item in _sourceList)
            {
                var values = new List<string>();
                foreach (var exportableColumn in _columns)
                {
                    if (!_doneHeaders)
                    {
                        _headers.Add(string.IsNullOrEmpty(exportableColumn.HeaderString)
                            ? MakeXmlNameLegal(GetPropertyName(exportableColumn.Func))
                            : MakeXmlNameLegal(exportableColumn.HeaderString));

                        var func = exportableColumn.Func.Compile();
                        _compiledFuncLookup.Add(exportableColumn.Func, func);
                        if (!string.IsNullOrEmpty(exportableColumn.CustomFormatString))
                        {
                            var value = func(item);
                            values.Add(value != null
                                ? string.Format(exportableColumn.CustomFormatString, value)
                                : "");
                        }
                        else
                        {
                            var value = func(item);
                            values.Add(value != null ? value.ToString() : "");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(exportableColumn.CustomFormatString))
                        {
                            var value = _compiledFuncLookup[exportableColumn.Func](item);
                            values.Add(value != null
                                ? string.Format(exportableColumn.CustomFormatString, value)
                                : "");
                        }
                        else
                        {
                            var value = _compiledFuncLookup[exportableColumn.Func](item);
                            values.Add(value != null ? value.ToString() : "");
                        }
                    }
                }
                if (!_doneHeaders)
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument(true);
                    writer.WriteProcessingInstruction("xml-stylesheet", "type='text/xsl' href='dump.xsl'");
                    writer.WriteComment("List Exporter dump");

                    // Write main document node and document properties
                    writer.WriteStartElement("dump");
                    writer.WriteAttributeString("date", DateTime.Now.ToString());

                    _doneHeaders = true;
                }

                writer.WriteStartElement("item");
                for (var i = 0; i < values.Count; i++)
                {
                    writer.WriteStartElement(_headers[i]);
                    writer.WriteString(values[i]);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.Flush();
        }

        /// <summary>
        /// Export to file, using the AsCSVString() method to supply the exportable data
        /// </summary>
        public void WhichIsExportedToFileLocation(StreamWriter fileWriter)
        {
            AsCsvString(fileWriter);
        }

        /// <summary>
        /// Gets a Name from an expression tree that is assumed to be a
        /// MemberExpression
        /// </summary>
        private static string GetPropertyName<T>(
            Expression<Func<T, object>> propertyExpression)
        {
            var lambda = propertyExpression as LambdaExpression;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = lambda.Body as UnaryExpression;
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambda.Body as MemberExpression;
            }

            var propertyInfo = memberExpression.Member as PropertyInfo;

            return propertyInfo.Name;
        }

        private static string MakeXmlNameLegal(string aString)
        {
            var newName = new StringBuilder();

            if (!char.IsLetter(aString[0]))
                newName.Append("_");

            // Must start with a letter or underscore.
            for (var i = 0; i <= aString.Length - 1; i++)
            {
                if (char.IsLetter(aString[i]) || char.IsNumber(aString[i]))
                {
                    newName.Append(aString[i]);
                }
                else
                {
                    newName.Append("_");
                }
            }
            return newName.ToString();
        }
    }
}