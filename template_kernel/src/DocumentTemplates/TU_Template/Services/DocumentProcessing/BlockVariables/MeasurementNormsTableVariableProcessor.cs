using System.Drawing;
using System.Linq;
using TU_Template.Models;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

/// <summary>
/// Обрабатывает маркер ${таблицы3} — таблицы "Нормы и режимы измерения электрических параметров при испытаниях микросхем".
/// Режим измерения: столбцы формируются из параметров предыдущей таблицы (операционные условия) + fвх и Rн (есть всегда).
/// Данные в строках — по электрическим параметрам; ячейки "Режим измерения" заполняются значениями из таблицы операционных условий.
/// </summary>
public partial class DocumentProcessorCore
{
    /// <summary>
    /// Значение по умолчанию для столбца «Режим измерения»: из операционных условий или "—"/"50" для fвх/Rн.
    /// </summary>
    private static string GetDefaultModeValue(Group group, int modeIndex)
    {
        if (modeIndex < group.OperatingConditionsParameters.Count)
        {
            var opCond = group.OperatingConditionsParameters[modeIndex];
            if (!string.IsNullOrWhiteSpace(opCond.Limit.AtMost)) return opCond.Limit.AtMost;
            if (!string.IsNullOrWhiteSpace(opCond.Limit.AtLeast)) return opCond.Limit.AtLeast;
            return "—";
        }
        if (modeIndex == group.OperatingConditionsParameters.Count)
            return "—"; // fвх
        return "50";    // Rн
    }

    /// <summary>
    /// Заголовок подколонки "Режим измерения" из параметра операционных условий: "Символ, единица" (единица из названия параметра).
    /// </summary>
    private static string GetMeasurementModeHeader(OperatingConditionsParameter p)
    {
        if (string.IsNullOrWhiteSpace(p.Name) || !p.Name.Contains(','))
            return string.IsNullOrWhiteSpace(p.Symbol) ? p.Name : p.Symbol;
        var unit = p.Name.Substring(p.Name.IndexOf(',') + 1).Trim();
        return string.IsNullOrWhiteSpace(p.Symbol) ? unit : $"{p.Symbol}, {unit}";
    }

    private void ProcessMeasurementNormsTableVariable(Paragraph paragraph)
    {
        for (int groupIndex = 0; groupIndex < _data.Groups.Count; groupIndex++)
        {
            var group = _data.Groups[groupIndex];
            // Если нет ни одного электрического параметра, то и таблица не нужна
            if (group.ElectricalParameters == null || group.ElectricalParameters.Count == 0)
                continue;

            var elements = group.Elements;
            var elementNames = elements == null || elements.Count == 0
                ? ""
                : string.Join(", ", elements.Select(e => e.Name));

            // Режим измерения: используем те же заголовки, что и в UI (MeasurementModeColumnHeaders),
            // чтобы количество и подписи столбцов полностью совпадали с тем, что видит пользователь.
            // Это избавляет от «лишних» столбцов в документе при изменении конфигурации режимов.
            var rawModeHeaders = group.MeasurementModeColumnHeaders?.ToList() ?? new List<string>();
            if (rawModeHeaders.Count == 0)
            {
                // Fallback: если по какой‑то причине заголовки не инициализированы,
                // строим их по старой схеме: все параметры условий + fвх, Rн.
                rawModeHeaders = group.OperatingConditionsParameters
                    .Select(GetMeasurementModeHeader)
                    .ToList();
                rawModeHeaders.Add("fвх, ГГц");
                rawModeHeaders.Add("Rн, Ом");
            }

            // Убираем пустые подзаголовки "Режим измерения", чтобы не появлялись пустые столбцы.
            // Важно: сохраняем индекс исходного заголовка для корректного чтения MeasurementModeValues.
            var visibleModeColumns = rawModeHeaders
                .Select((header, sourceIndex) => (Header: header, SourceIndex: sourceIndex))
                .Where(x => !string.IsNullOrWhiteSpace(x.Header))
                .ToList();

            // Страховка: если все заголовки пустые, оставляем как минимум fвх и Rн.
            if (visibleModeColumns.Count == 0)
            {
                visibleModeColumns.Add(("fвх, ГГц", Math.Max(0, rawModeHeaders.Count - 2)));
                visibleModeColumns.Add(("Rн, Ом", Math.Max(0, rawModeHeaders.Count - 1)));
            }

            var modeParamCount = visibleModeColumns.Count;
            // Базовые столбцы (индексы):
            // 0 - Наименование параметра, единица измерения
            // 1 - Буквенное обозначение параметра
            // 2 - Норма параметра: "не менее"
            // 3 - Норма параметра: "не более"
            // 4 - Температура окружающей среды, °С
            // 5 - Погрешность при измерении параметра, %
            //
            // Далее идут только столбцы "Режим измерения" (подстолбцы из modeHeaders).
            var totalColumns = 6 + modeParamCount;
            var dataRowCount = group.ElectricalParameters.Count;
            var totalRows = 2 + dataRowCount;

            // Нумерация начинается с 6.1: шаблон ссылается на "таблицы 6.1-6.{к1}",
            // а 6.{к1+1} уже занята таблицей квалификационных испытаний.
            var tableNumber = $"6.{groupIndex + 1}";
            var titleText = $"Т а б л и ц а {tableNumber} – Нормы и режимы измерения электрических параметров при испытаниях микросхем {elementNames}";

            // Важно: сначала вставляем таблицу относительно маркера, и только потом заголовок —
            // относительно самой таблицы. При обратном порядке на второй итерации цикла заголовок
            // попадает ВНУТРЬ уже вставленной таблицы, тело документа "съезжает" в неё, и дальше
            // ломается подстановка переменных (${ТУ}, ${названия}, ${п1} остаются в документе).
            var table = paragraph.InsertTableBeforeSelf(totalRows, totalColumns);

            var titleParagraph = table.InsertParagraphBeforeSelf(titleText);
            titleParagraph.FontSize(13).SpacingLine(LineSpacing15).SpacingBefore(12);

            // См. другие таблицы: без пустого абзаца после каждой таблицы (накопление ¶ по группам).

            // ----- Строка 0 заголовка -----
            // Важно: сначала заполняем тексты по исходным индексам,
            // потом объединяем ячейки. Иначе после MergeCells индексы в Row.Cells сдвигаются.
            var headerRow = table.Rows[0];
            headerRow.Cells[0].Paragraphs[0].Append("Наименование параметра, единица измерения");
            headerRow.Cells[1].Paragraphs[0].Append("Буквенное обозначение параметра");
            headerRow.Cells[2].Paragraphs[0].Append("Норма параметра");
            headerRow.Cells[4].Paragraphs[0].Append("Температура окружающей среды, \u00b0С");
            headerRow.Cells[5].Paragraphs[0].Append("Погрешность при измерении параметра, %");
            headerRow.Cells[6].Paragraphs[0].Append("Режим измерения");

            table.MergeCellsInColumn(0, 0, 1);
            table.MergeCellsInColumn(1, 0, 1);
            table.MergeCellsInColumn(4, 0, 1);
            table.MergeCellsInColumn(5, 0, 1);

            // Горизонтальные объединения делаем после заполнения текста.
            if (totalColumns > 6)
                headerRow.MergeCells(6, totalColumns - 1);
            headerRow.MergeCells(2, 3);

            // ----- Строка 1 заголовка: подзаголовки -----
            table.Rows[1].Cells[2].Paragraphs[0].Append("не менее");
            table.Rows[1].Cells[3].Paragraphs[0].Append("не более");

            int col = 6;
            foreach (var mode in visibleModeColumns)
            {
                table.Rows[1].Cells[col].Paragraphs[0].Append(mode.Header);
                col++;
            }

            // Двойная линия под заголовком
            foreach (var cell in table.Rows[1].Cells)
            {
                cell.SetBorder(TableCellBorderType.Bottom, new(BorderStyle.Tcbs_double, BorderSize.one, 1, Color.Black));
            }

            // ----- Строки данных: по одному на электрический параметр -----
            var elementsList = elements?.ToList() ?? new System.Collections.Generic.List<Element>();
            for (int i = 0; i < group.ElectricalParameters.Count; i++)
            {
                var param = group.ElectricalParameters[i];
                int rowIndex = 2 + i;
                var row = table.Rows[rowIndex];

                row.Cells[0].Paragraphs[0].Append(param.Name);
                row.Cells[1].Paragraphs[0].Append(param.Symbol);

                // Норма: из первого элемента группы (если есть)
                var pv = elementsList.Count > 0
                    ? elementsList[0].ParameterValues.FirstOrDefault(v => v.Parameter == param)
                    : null;
                row.Cells[2].Paragraphs[0].Append(pv?.Limits?.AtLeast ?? "—");
                row.Cells[3].Paragraphs[0].Append(pv?.Limits?.AtMost ?? "—");

                row.Cells[4].Paragraphs[0].Append(param.Temp ?? "—");
                row.Cells[5].Paragraphs[0].Append(!string.IsNullOrWhiteSpace(param.MeasurementError) ? param.MeasurementError : "—");

                // Режим измерения: из настроек параметра (MeasurementModeValues) или из операционных условий / по умолчанию
                col = 6;
                for (int modeIndex = 0; modeIndex < modeParamCount; modeIndex++)
                {
                    var sourceIndex = visibleModeColumns[modeIndex].SourceIndex;
                    var val = (param.MeasurementModeValues?.Count ?? 0) > sourceIndex && !string.IsNullOrWhiteSpace(param.MeasurementModeValues![sourceIndex].Value)
                        ? param.MeasurementModeValues[sourceIndex].Value
                        : GetDefaultModeValue(group, sourceIndex);
                    row.Cells[col].Paragraphs[0].Append(val);
                    col++;
                }
            }

            // Оформление всех строк
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

            // Наименование параметра — по левому краю в строках данных
            for (int i = 0; i < dataRowCount; i++)
            {
                table.Rows[2 + i].Cells[0].Paragraphs[0].Alignment = Alignment.left;
            }

            if (dataRowCount > 0)
            {
                int lastDataRowIndex = 2 + dataRowCount - 1;
                foreach (var p in table.Rows[lastDataRowIndex].Cells[0].Paragraphs)
                    p.SpacingAfter(12);
            }
        }

        paragraph.Remove(false);
    }
}
