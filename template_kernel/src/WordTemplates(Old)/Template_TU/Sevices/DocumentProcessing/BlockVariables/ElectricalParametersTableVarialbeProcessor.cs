using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Template_TU.Models;
using Xceed.Document.NET;

namespace Template_TU.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    private List<List<Element>> GroupElements(IEnumerable<Element> elements)
    {
        List<Element> source = new(elements);
        List<List<Element>> groups = new();

        for (int i = 0; i < source.Count; i++)
        {
            var element = source[i];
            List<Element> group = [ element ];
            groups.Add(group);

            for (int j = i + 1; j < source.Count;)
            {
                var e = source[j];
                if (element.ParameterValues.SequenceEqual(e.ParameterValues))
                {
                    group.Add(e);
                    source.RemoveAt(j);
                }
                else
                {
                    j++;
                }
            }
        }

        return groups;
    }

    private void ProcessElectricalParameterTableVariable(Paragraph paragraph)
    {
        for (int groupIndex = 0; groupIndex < _data.Groups.Count; groupIndex++)
        {
            var group = _data.Groups[groupIndex];
            var elements = group.Elements;
            var groupedElements = GroupElements(elements);
            var elementNames = string.Join(", ", elements.Select(e => e.Name));

            bool singleParameter = groupedElements.Count == 1;

            var tableNameParagraph = paragraph.InsertParagraphBeforeSelf(
                $"Таблица 4.{groupIndex + 1} – Значения электрических параметров микросхем {elementNames}");
            tableNameParagraph.FontSize(13).SpacingLine(LineSpacing15).SpacingBefore(12);

            var table = paragraph.InsertTableBeforeSelf(
                (singleParameter ? 2 : 3) + group.ElectricalParameters.Count,
                3 + 2 * groupedElements.Count);

            paragraph.InsertParagraphBeforeSelf("");

            int parameterColumnsCount = 2 * groupedElements.Count;
            int lastColumn = 3 + parameterColumnsCount - 1;

            table.Rows[0].Cells[0].Paragraphs[0].Append("Наименование параметра, единица измерения");
            table.MergeCellsInColumn(0, 0, singleParameter ? 1 : 2);

            table.Rows[0].Cells[1].Paragraphs[0].Append("Буквенное обозначение параметра");
            table.MergeCellsInColumn(1, 0, singleParameter ? 1 : 2);

            table.Rows[0].Cells[lastColumn].Paragraphs[0].Append("Температура окружающей среды, \u00b0С");
            table.MergeCellsInColumn(lastColumn, 0, singleParameter ? 1 : 2);

            table.Rows[0].Cells[2].Paragraphs[0].Append("Норма параметра");
            table.Rows[0].MergeCells(2, 2 + parameterColumnsCount - 1);

            if (singleParameter)
            {
                table.Rows[1].Cells[2].Paragraphs[0].Append("не менее");
                table.Rows[1].Cells[3].Paragraphs[0].Append("не более");
            }
            else
            {
                for (int i = 0; i < groupedElements.Count; i++)
                {
                    int column = 2 + i * 2;
                    var elementsGroup = groupedElements[i];
                    string groupNames = string.Join(",\n", elementsGroup.Select(e => e.Name));
                    table.Rows[1].Cells[2 + i].Paragraphs[0].Append(groupNames);
                    table.Rows[1].MergeCells(2 + i, 2 + i + 1);
                    table.Rows[2].Cells[column].Paragraphs[0].Append("не менее");
                    table.Rows[2].Cells[column + 1].Paragraphs[0].Append("не более");
                }
            }

            foreach (var cell in table.Rows[singleParameter ? 1 : 2].Cells)
            {
                cell.SetBorder(TableCellBorderType.Bottom, new(BorderStyle.Tcbs_double, BorderSize.one, 1, Color.Black));
            }

            for (int i = 0; i < group.ElectricalParameters.Count; i++)
            {
                var parameter = group.ElectricalParameters[i];
                int rowIndex = (singleParameter ? 2 : 3) + i;
                var row = table.Rows[rowIndex];

                row.Cells[0].Paragraphs[0].Append(parameter.Name);
                row.Cells[1].Paragraphs[0].Append(parameter.Symbol);
                row.Cells[1 + parameterColumnsCount + 1].Paragraphs[0].Append(parameter.Temp);

                for (int j = 0; j < groupedElements.Count; j++)
                {
                    var element = groupedElements[j][0];
                    var value = element.ParameterValues.First(p => p.Parameter == parameter);
                    int column = 2 + j * 2;
                    row.Cells[column].Paragraphs[0].Append(value.Limits.AtLeast);
                    row.Cells[column + 1].Paragraphs[0].Append(value.Limits.AtMost);
                }
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

            for (int i = 0; i < group.ElectricalParameters.Count; i++)
            {
                int rowIndex = (singleParameter ? 2 : 3) + i;
                table.Rows[rowIndex].Cells[0].Paragraphs[0].Alignment = Alignment.left;
            }

            if (group.ElectricalParametersNotes.Count > 0)
            {
                var row = table.InsertRow();
                row.MinHeight = CmToSize(0.8);
                row.MergeCells(0, row.ColumnCount - 1);
                row.Cells[0].RemoveParagraphAt(0);

                List<Paragraph> notesParagraphs = new();
                foreach (var note in group.ElectricalParametersNotes)
                {
                    var p = row.Cells[0].InsertParagraph(note.Text);
                    notesParagraphs.Add(p);
                }

                notesParagraphs.FirstOrDefault()?.SpacingBefore(12);

                foreach (var p in notesParagraphs)
                {
                    p.FontSize(11);
                    p.SpacingLine(GetLineSpacing(1.15));
                    p.IndentationFirstLine = (float)CmToSize(1);
                }

                notesParagraphs.LastOrDefault()?.SpacingAfter(12);
            }

            /*float width = 496.15f; // 17.5
            //table.SetWidths(Enumerable.Repeat(width / table.ColumnCount, table.ColumnCount).ToArray());
            table.SetWidthsPercentage([30f, 15f, 20f, 20f, 15f], width);
            table.AutoFit = AutoFit.Fixed;*/
        }

        paragraph.Remove(false);
    }
}