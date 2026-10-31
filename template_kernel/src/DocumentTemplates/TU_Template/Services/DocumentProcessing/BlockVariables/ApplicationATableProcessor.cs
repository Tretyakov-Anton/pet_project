using System.Collections.Generic;
using System.Linq;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

/// <summary>
/// Заполняет таблицу А.1 «Термины, определения и буквенные обозначения электрических параметров».
///
/// В шаблоне эта таблица размечена не фигурными маркерами, а заготовкой строки с подстановками
/// в круглых скобках: $(название параметра) | $(Обозначение параметра) | $(Описание параметра).
/// Такой синтаксис обработчик переменных не разбирает, поэтому таблицу находим по заголовку
/// и заполняем строками из вкладки «Приложение А».
/// </summary>
public partial class DocumentProcessorCore
{
    /// <summary>Текст, по которому опознаём шапку таблицы А.1.</summary>
    private const string ApplicationAHeaderMarker = "Термин по ТУ";

    private void ProcessApplicationATable()
    {
        var terms = _data.TermDefinitions?
            .Where(t => !string.IsNullOrWhiteSpace(t.Term) || !string.IsNullOrWhiteSpace(t.Definition))
            .ToList() ?? new List<Models.TermDefinition>();

        // Нечего вставлять — оставляем заготовку как есть, чтобы было видно незаполненное место.
        if (terms.Count == 0) return;

        var table = FindApplicationATable();
        if (table == null) return;

        // Строка-заготовка с $(...) — её удалим после вставки данных.
        var placeholderIndex = FindPlaceholderRowIndex(table);

        foreach (var term in terms)
        {
            var row = table.InsertRow();
            row.MinHeight = CmToSize(0.8);

            AppendCell(row, 0, term.Term);
            AppendCell(row, 1, term.Symbol);
            AppendCell(row, 2, term.Definition);
        }

        if (placeholderIndex >= 0 && placeholderIndex < table.RowCount)
            table.RemoveRow(placeholderIndex);
    }

    private Table? FindApplicationATable()
    {
        foreach (var table in _document.Tables)
        {
            if (table.RowCount == 0) continue;

            var header = string.Join(" ", table.Rows[0].Cells
                .SelectMany(c => c.Paragraphs)
                .Select(p => p.Text ?? ""));

            if (header.Contains(ApplicationAHeaderMarker))
                return table;
        }

        return null;
    }

    private static int FindPlaceholderRowIndex(Table table)
    {
        for (int i = 1; i < table.RowCount; i++)
        {
            var text = string.Join(" ", table.Rows[i].Cells
                .SelectMany(c => c.Paragraphs)
                .Select(p => p.Text ?? ""));

            if (text.Contains("$("))
                return i;
        }

        return -1;
    }

    private static void AppendCell(Row row, int columnIndex, string? value)
    {
        if (columnIndex >= row.Cells.Count) return;

        var cell = row.Cells[columnIndex];
        cell.VerticalAlignment = VerticalAlignment.Top;

        var paragraph = cell.Paragraphs[0];
        paragraph.Append(value ?? "");
        paragraph.Alignment = columnIndex == 1 ? Alignment.center : Alignment.left;
        paragraph.FontSize(11);
        paragraph.SpacingBefore(3);
        paragraph.SpacingAfter(3);
    }
}
