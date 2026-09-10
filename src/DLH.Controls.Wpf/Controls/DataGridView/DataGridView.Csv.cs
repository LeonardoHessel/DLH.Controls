using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLH.Controls.Wpf;

public sealed class DataGridViewCsvOptions
{
    public string Delimiter { get; set; } = ";";
    public bool IncludeHeaders { get; set; } = true;
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    public Func<object, DataGridColumn, object?>? ValueSelector { get; set; }
}

public partial class DataGridView
{
    public void ExportCsv(TextWriter writer, DataGridViewCsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= new DataGridViewCsvOptions();
        if (string.IsNullOrEmpty(options.Delimiter)) throw new ArgumentException("O delimitador não pode ser vazio.", nameof(options));
        var columns = Columns.Where(column => column.Visibility == Visibility.Visible)
            .OrderBy(column => column.DisplayIndex).ToArray();
        if (options.IncludeHeaders)
            WriteCsvLine(writer, columns.Select(column => Convert.ToString(column.Header, options.Culture)), options.Delimiter);
        var view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (view is null) return;
        foreach (var item in view.Cast<object>().Where(item => item is not CollectionViewGroup))
            WriteCsvLine(writer, columns.Select(column => Convert.ToString(
                options.ValueSelector?.Invoke(item, column) ?? GetCsvValue(item, column), options.Culture)), options.Delimiter);
    }

    public void ExportCsv(Stream stream, DataGridViewCsvOptions? options = null, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(true), 1024, leaveOpen: true);
        ExportCsv(writer, options);
        writer.Flush();
    }

    private static object? GetCsvValue(object item, DataGridColumn column)
    {
        var binding = column.ClipboardContentBinding as Binding ?? (column as DataGridBoundColumn)?.Binding as Binding;
        var path = binding?.Path?.Path;
        if (string.IsNullOrWhiteSpace(path)) path = column.SortMemberPath;
        return string.IsNullOrWhiteSpace(path) ? null : ReadMemberPath(item, path);
    }

    private static void WriteCsvLine(TextWriter writer, IEnumerable<string?> values, string delimiter) =>
        writer.WriteLine(string.Join(delimiter, values.Select(value => EscapeCsv(value ?? string.Empty, delimiter))));

    private static string EscapeCsv(string value, string delimiter)
    {
        if (!value.Contains(delimiter, StringComparison.Ordinal) && !value.Contains('"') &&
            !value.Contains('\r') && !value.Contains('\n')) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
