using System.Drawing;
using System.Linq;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    private void ProcessOperatingConditionsTableVariable(Paragraph paragraph)
    {
        for (int groupIndex = 0; groupIndex < _data.Groups.Count; groupIndex++)
        {
            var group = _data.Groups[groupIndex];

            var elements = group.Elements;
            var groupedElements = GroupElements(elements);
            var elementNames = string.Join(", ", elements.Select(e => e.Name));

            int tableNumber = _data.Groups.Count + 1 + groupIndex;
            // Важно: сначала вставляем таблицу относительно маркера, и только потом заголовок —
            // относительно самой таблицы. При обратном порядке на второй итерации цикла заголовок
            // попадает ВНУТРЬ уже вставленной таблицы, тело документа "съезжает" в неё, и дальше
            // ломается подстановка переменных (${ТУ}, ${названия}, ${п1} остаются в документе).
            var table = paragraph.InsertTableBeforeSelf(2 + group.OperatingConditionsParameters.Count, 7);

            var tableNameParagraph = table.InsertParagraphBeforeSelf(
                $"Т а б л и ц а 4.{tableNumber} – Предельно допустимые и предельные значения электрических режимов эксплуатации микросхем {elementNames}");
            tableNameParagraph.FontSize(13).SpacingLine(LineSpacing15).SpacingBefore(12);

            // См. ElectricalParametersTable: без пустого абзаца после каждой таблицы (накопление ¶ по группам).

            // Важно: сначала заполняем тексты, затем выполняем merge.
            // Иначе после MergeCells индексы в Row.Cells могут сдвигаться.
            var headerRow = table.Rows[0];
            headerRow.Cells[0].Paragraphs[0].Append("Наименование параметра, единица измерения, режим измерения");
            headerRow.Cells[1].Paragraphs[0].Append("Буквенное обозначение параметра");
            headerRow.Cells[2].Paragraphs[0].Append("Предельно допустимый режим");
            headerRow.Cells[4].Paragraphs[0].Append("Предельный режим");
            headerRow.Cells[6].Paragraphs[0].Append("Номер пункта примечания");

            table.Rows[1].Cells[2].Paragraphs[0].Append("не менее");
            table.Rows[1].Cells[3].Paragraphs[0].Append("не более");
            table.Rows[1].Cells[4].Paragraphs[0].Append("не менее");
            table.Rows[1].Cells[5].Paragraphs[0].Append("не более");

            table.MergeCellsInColumn(0, 0, 1);
            table.MergeCellsInColumn(1, 0, 1);
            table.MergeCellsInColumn(6, 0, 1);

            // Объединяем блоки режимов справа налево, чтобы индексы не "съезжали".
            headerRow.MergeCells(4, 5);
            headerRow.MergeCells(2, 3);

            foreach (var cell in table.Rows[1].Cells)
            {
                cell.SetBorder(TableCellBorderType.Bottom, new(BorderStyle.Tcbs_double, BorderSize.one, 1, Color.Black));
            }

            for (int i = 0; i < group.OperatingConditionsParameters.Count; i++)
            {
                var parameter = group.OperatingConditionsParameters[i];
                int rowIndex = 2 + i;
                var row = table.Rows[rowIndex];

                row.Cells[0].Paragraphs[0].Append(parameter.Name);
                row.Cells[1].Paragraphs[0].Append(parameter.Symbol);
                row.Cells[2].Paragraphs[0].Append(parameter.MaximumPermissible.AtLeast);
                row.Cells[3].Paragraphs[0].Append(parameter.MaximumPermissible.AtMost);
                row.Cells[4].Paragraphs[0].Append(parameter.Limit.AtLeast);
                row.Cells[5].Paragraphs[0].Append(parameter.Limit.AtMost);
                row.Cells[6].Paragraphs[0].Append(parameter.NoteRefs);
            }

            foreach (var row in table.Rows)
            {
                row.MinHeight = CmToSize(0.8);
                foreach (var cell in row.Cells)
                {
                    cell.VerticalAlignment = VerticalAlignment.Center;
                    foreach (var p in cell.Paragraphs)
                    {
                        p.Alignment = Alignment.center;
                        p.FontSize(11);
                    }
                }
            }

            for (int i = 0; i < group.OperatingConditionsParameters.Count; i++)
            {
                table.Rows[2 + i].Cells[0].Paragraphs[0].Alignment = Alignment.left;
            }

            if (group.OperatingConditionsParametersNotes.Count > 0)
            {
                var row = table.InsertRow();
                row.MinHeight = CmToSize(0.8);
                row.MergeCells(0, row.ColumnCount - 1);
                row.Cells[0].RemoveParagraphAt(0);

                row.Cells[0].InsertParagraph("Примечания", false, new() { Spacing = 2 });

                foreach (var note in group.OperatingConditionsParametersNotes)
                {
                    var p = row.Cells[0].InsertParagraph(note.Text);
                }

                row.Cells[0].Paragraphs.FirstOrDefault()?.SpacingBefore(12);

                foreach (var p in row.Cells[0].Paragraphs)
                {
                    p.FontSize(11);
                    p.SpacingLine(GetLineSpacing(1.15));
                    p.IndentationFirstLine = (float)CmToSize(1);
                }

                row.Cells[0].Paragraphs.LastOrDefault()?.SpacingAfter(12);
            }
            else if (group.OperatingConditionsParameters.Count > 0)
            {
                int lastDataRowIndex = 2 + group.OperatingConditionsParameters.Count - 1;
                if (lastDataRowIndex < table.RowCount)
                {
                    foreach (var p in table.Rows[lastDataRowIndex].Cells[0].Paragraphs)
                        p.SpacingAfter(12);
                }
            }
        }

        paragraph.Remove(false);
    }
}


