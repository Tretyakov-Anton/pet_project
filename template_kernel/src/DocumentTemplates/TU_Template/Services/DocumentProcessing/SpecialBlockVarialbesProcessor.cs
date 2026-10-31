using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Drawing;
using Xceed.Document.NET;

namespace TU_Template.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    [GeneratedRegex(@"^\s*\$\s*\{(?<name>.*)}\s*$")] 
    private static partial Regex VariableParagraphRegex();

    [GeneratedRegex(@"\$\s*\{(?<name>[^}]+)}")]
    private static partial Regex VariableInlineRegex();

    private static double CmToSize(double cm) => 28.35142857142857f * cm;
    private const double LineSpacing15 = 18;
    private static double GetLineSpacing(double height) => 18 * height / 1.5;

    /// <summary>Имена блочных маркеров (после нормализации), для которых есть обработчик.</summary>
    private static readonly string[] SpecialBlockNames =
    {
        "таблицы1", "таблицы2", "таблицы3", "описания", "таблицаг1",
    };

    /// <summary>
    /// Нормализует имя маркера: регистр, пробелы и точки.
    /// Благодаря этому работают варианты "таблица г1", "таблица Г.1", "таблица  г 1".
    /// </summary>
    private static string NormalizeVariableName(string? variableName) =>
        (variableName ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace(".", "");

    private void ProcessSpecialBlockVariables()
    {
        // Маркер ищем КАЖДЫЙ РАЗ ЗАНОВО в живом документе.
        //
        // Раньше здесь снимался снимок (_document.Paragraphs.ToList()) и обрабатывался целиком.
        // После вставки таблиц объекты Paragraph из снимка держат устаревшие ссылки на родителя,
        // и следующая вставка попадает ВНУТРЬ ранее вставленной таблицы. Тело документа после
        // этого "съезжает" в неё, и обе последующие подстановки переменных перестают работать:
        // ${ТУ}, ${названия}, ${п1}, ${к1}..${к5} оставались в готовом документе.
        var processed = new HashSet<string>();

        // Блочных маркеров в шаблоне единицы; ограничение — страховка от зацикливания.
        for (int guard = 0; guard < 64; guard++)
        {
            var next = FindNextSpecialBlock(processed);
            if (next == null) break;

            processed.Add(next.Value.Name);
            ProcessSpecialBlockVariable(next.Value.Paragraph, next.Value.Name);
        }
    }

    /// <summary>
    /// Находит в текущем состоянии документа первый блочный маркер, который ещё не обрабатывали.
    /// </summary>
    private (Paragraph Paragraph, string Name)? FindNextSpecialBlock(HashSet<string> processed)
    {
        foreach (var paragraph in _document.Paragraphs)
        {
            var text = paragraph.Text ?? string.Empty;

            // Сначала пробуем строгое совпадение параграфа
            var match = VariableParagraphRegex().Match(text);
            if (!match.Success)
            {
                // Фоллбэк: ищем маркер внутри параграфа (на случай скрытых символов/стилей)
                match = VariableInlineRegex().Match(text);
            }

            if (!match.Success) continue;

            var name = NormalizeVariableName(match.Groups["name"].Value);
            if (!SpecialBlockNames.Contains(name)) continue;
            if (processed.Contains(name)) continue;

            return (paragraph, name);
        }

        return null;
    }

    private bool ProcessSpecialBlockVariable(Paragraph paragraph, string variableName)
    {
        var normalized = NormalizeVariableName(variableName);
        switch (normalized)
        {
            case "таблицы1":
                ProcessElectricalParameterTableVariable(paragraph);
                break;
            case "таблицы2":
                ProcessOperatingConditionsTableVariable(paragraph);
                break;
            case "описания":
                ProcessDescriptionVariable(paragraph);
                break;
            case "таблицаг1":
                ProcessApplicationGTableVariable(paragraph);
                break;
            case "таблицы3":
                ProcessMeasurementNormsTableVariable(paragraph);
                break;
            default:
                return false;
        }
        return true;
    }

    private void ProcessDescriptionVariable(Paragraph paragraph)
    {
        var formatting = paragraph.MagicText[0].formatting;
        foreach (var element in _data.Elements)
        {
            var e = element.Value;
            var p = paragraph.InsertParagraphBeforeSelf(
                $"Микросхема {e.Name} представляет собой {e.Description}.",
                false, formatting);
            p.SpacingLine(paragraph.LineSpacing);
            p.IndentationFirstLine = paragraph.IndentationFirstLine;
        }
        paragraph.Remove(false);
    }

    private static IEnumerable<string> GetApplicationGTypes(Models.DeviceGroup group)
    {
        return group.Types?
            .Select(t => t?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            ?? Enumerable.Empty<string>();
    }

    private void ProcessApplicationGTableVariable(Paragraph paragraph)
    {
        var groups = _data.DeviceGroups;
        if (groups == null || groups.Count == 0)
        {
            paragraph.Remove(false);
            return;
        }

        // Таблица Г.1: заголовок + по одной строке на каждый прибор + строка примечания.
        var table = paragraph.InsertTableBeforeSelf(2 + groups.Count, 2);
        // Не вставляем пустой абзац перед маркером: InsertParagraphBeforeSelf("") часто копирует
        // свойства абзаца-маркера (интервалы, «не отрывать от следующего», «не разрывать абзац»),
        // из‑за чего следующая таблица в документе (например «Лист регистрации изменений»)
        // может «разъезжаться» по страницам. Отступ до следующего блока задаём через SpacingAfter
        // у последнего абзаца таблицы (строка примечания).

        table.Rows[0].Cells[0].Paragraphs[0].Append("Наименование прибора\n(оборудования)");
        table.Rows[0].Cells[1].Paragraphs[0].Append("Тип прибора\n(оборудования)");

        foreach (var p in table.Rows[0].Cells[0].Paragraphs.Concat(table.Rows[0].Cells[1].Paragraphs))
        {
            p.Alignment = Alignment.center;
            p.FontSize(11);
        }

        // Двойная линия под заголовком
        foreach (var cell in table.Rows[0].Cells)
        {
            cell.SetBorder(TableCellBorderType.Bottom, new(BorderStyle.Tcbs_double, BorderSize.one, 1, Color.Black));
        }

        // Данные — каждая пара "наименование/тип" в отдельной строке,
        // чтобы начала строк в левой и правой колонках совпадали по высоте.
        for (int i = 0; i < groups.Count; i++)
        {
            var dataRow = table.Rows[1 + i];
            var group = groups[i];
            dataRow.Cells[0].Paragraphs[0].Append(group.Name);
            dataRow.Cells[1].Paragraphs[0].Append(string.Join(", ", GetApplicationGTypes(group)));
        }

        // Полностью убираем внутренние горизонтальные линии в блоке данных.
        // Оставляем только границу под заголовком и границу перед примечанием.
        for (int i = 0; i < groups.Count; i++)
        {
            var row = table.Rows[1 + i];
            foreach (var cell in row.Cells)
            {
                cell.SetBorder(TableCellBorderType.Top, new(BorderStyle.Tcbs_none, BorderSize.one, 0, Color.Black));
                cell.SetBorder(TableCellBorderType.Bottom, new(BorderStyle.Tcbs_none, BorderSize.one, 0, Color.Black));
            }
        }

        // Примечание внутри таблицы (нижняя объединенная строка).
        var noteRow = table.Rows[table.RowCount - 1];
        noteRow.MergeCells(0, 1);
        noteRow.Cells[0].Paragraphs[0].Append(
            "Примечание – Допускают применение приборов, отличных от указанных в перечне, " +
            "но обеспечивающих измерение требуемых параметров и установку испытательных режимов с заданной точностью.");

        // Общие настройки строк/ячеек
        foreach (var row in table.Rows)
        {
            row.MinHeight = CmToSize(0.8);
            foreach (var cell in row.Cells)
            {
                cell.VerticalAlignment = VerticalAlignment.Top;
                foreach (var p in cell.Paragraphs)
                {
                    p.Alignment = Alignment.left;
                    p.FontSize(11);
                    // Небольшой вертикальный "воздух" между текстом и горизонтальными линиями ячейки.
                    p.SpacingBefore(3);
                    p.SpacingAfter(3);
                }
            }
        }

        // Заголовки первой строки должны быть по центру.
        foreach (var p in table.Rows[0].Cells[0].Paragraphs.Concat(table.Rows[0].Cells[1].Paragraphs))
        {
            p.Alignment = Alignment.center;
        }

        foreach (var p in noteRow.Cells[0].Paragraphs)
        {
            p.Alignment = Alignment.left;
            p.SpacingBefore(0);
            p.SpacingAfter(12);
            p.SpacingLine(LineSpacing15);
            p.IndentationFirstLine = (float)CmToSize(1.25);
            p.Italic();
        }

        // Данные во 2-м столбце ("Тип прибора") — по центру.
        for (int i = 0; i < groups.Count; i++)
        {
            foreach (var p in table.Rows[1 + i].Cells[1].Paragraphs)
            {
                p.Alignment = Alignment.center;
                p.SpacingBefore(4);
                p.SpacingAfter(4);
            }
            foreach (var p in table.Rows[1 + i].Cells[0].Paragraphs)
            {
                p.SpacingBefore(4);
                p.SpacingAfter(4);
            }
            table.Rows[1 + i].MinHeight = CmToSize(1.0);
        }

        paragraph.Remove(false);
    }
}


