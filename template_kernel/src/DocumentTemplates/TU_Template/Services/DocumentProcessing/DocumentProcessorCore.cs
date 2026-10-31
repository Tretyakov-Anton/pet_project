using System.Collections.Generic;
using System.Linq;
using TU_Template.Models;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TU_Template.Services.DocumentProcessing;

public partial class DocumentProcessorCore
{
    private readonly DocX _document;
    private readonly TemplateData _data;

    private Dictionary<string, string> _tempVariables = new();

    public DocumentProcessorCore(DocX document, TemplateData data)
    {
        _document = document;
        _data = data;
    }

    /// <summary>
    /// Итоговые значения переменных после обработки: вычисленные ядром плюс заданные
    /// пользователем. Нужны для доработки колонтитулов уже сохранённого файла.
    /// Приоритет такой же, как в конвейере: вычисленные значения важнее пользовательских.
    /// </summary>
    public IReadOnlyDictionary<string, string> ResolvedVariables
    {
        get
        {
            var result = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var variable in _data.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name)) continue;
                result[variable.Name] = variable.Value ?? "";
            }

            foreach (var pair in _tempVariables)
                result[pair.Key] = pair.Value;

            return result;
        }
    }

    public void Process()
    {
        _tempVariables = new();
        InitializeGeneralVariables();

        ProcessConditions();
        ProcessSpecialBlockVariables();
        ProcessApplicationATable();
        ProcessTempVariables();
        ProcessVariables();

        // Чистки пустых абзацев здесь намеренно НЕТ.
        //
        // Раньше вызывался CollapseConsecutiveEmptyParagraphs(), но он считал «пустым» любой абзац
        // без текста — а в шаблоне такими являются служебные абзацы, несущие разрыв страницы
        // (w:br type="page") и свойства раздела (w:sectPr). Удаляя их, он склеивал разделы:
        // титульный лист съезжал в «Содержание», а «Содержание» — в раздел 1.
        // Проверено на шаблоне: без чистки получается 18 разрывов страниц и 4 раздела,
        // то есть ровно как в исходном файле; с чисткой оставалось 16 и 3.
        //
        // Чистить при этом нечего: обработчики таблиц специально не вставляют пустые абзацы,
        // и число цепочек пустых абзацев в готовом документе совпадает с шаблоном.
    }

    /// <summary>
    /// Записывает вычисленное значение переменной, не затирая значение,
    /// заданное пользователем на вкладке «Переменные».
    /// </summary>
    private void SetComputedVariable(string name, string value)
    {
        var userValue = _data.Variables
            .FirstOrDefault(v => string.Equals(v.Name, name, System.StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(userValue)) return;

        _tempVariables[name] = value;
    }

    private void InitializeGeneralVariables()
    {
        var names = string.Join(", ", ((IList<Element>)_data.Elements).Select(e => e.Name));
        _tempVariables["названия"] = names;

        int uniqueParameterCount = _data.Groups.SelectMany(group => group.ElectricalParameters)
                                               .Select(parameter => parameter.Name)
                                               .Distinct()
                                               .Count();
        _tempVariables["п1"] = $"{uniqueParameterCount}";

        // Количественные переменные шаблона — производные от числа групп:
        //   к1 — число таблиц электрических параметров (= число групп);
        //   к2 — номер первой таблицы предельных значений / таблицы квалификационных испытаний;
        //   к3 — номер последней таблицы предельных значений;
        //   к4 — номер таблицы приёмо-сдаточных испытаний;
        //   к5 — номер таблицы периодических испытаний.
        // Раньше их приходилось вводить вручную, и все перекрёстные ссылки шаблона
        // оставались незаполненными.
        int k1 = _data.Groups.Count;
        SetComputedVariable("к1", $"{k1}");
        SetComputedVariable("к2", $"{k1 + 1}");
        SetComputedVariable("к3", $"{k1 * 2}");
        SetComputedVariable("к4", $"{k1 + 2}");
        SetComputedVariable("к5", $"{k1 + 3}");

        // Приложение В — шифры документов разделов В.1 и В.2.
        // Источники объединяются: позиции, выбранные из справочника корпусов, плюс шифры,
        // введённые вручную (поведение ветки attachmentC — в разделе может быть несколько документов).
        var gchItems = _data.GchD2Items?.ToList() ?? new List<Models.GchD2Item>();
        if (gchItems.Count == 0 && _data.AppendixVItem is { } legacy && !string.IsNullOrWhiteSpace(legacy.CaseName))
        {
            gchItems.Add(new Models.GchD2Item
            {
                CaseName = legacy.CaseName,
                UnifiedGch = legacy.UnifiedGch,
                DescriptionCode = legacy.DescriptionCode,
                DescriptionName = legacy.DescriptionName,
            });
        }

        var manualB1 = _data.ApplicationVSectionB1?.Select(d => d.DocumentCode) ?? Enumerable.Empty<string>();
        var manualB2 = _data.ApplicationVSectionB2?.Select(d => d.DocumentCode) ?? Enumerable.Empty<string>();

        var sectionB1 = Clean(gchItems.Select(i => i.UnifiedGch).Concat(manualB1));
        var sectionB2 = Clean(gchItems.Select(i => i.DescriptionCode).Concat(manualB2));
        var caseNames = Clean(gchItems.Select(i => i.CaseName));
        var descNames = Clean(gchItems.Select(i => i.DescriptionName));

        _tempVariables["приложениеВ_ГЧ"]                = string.Join(", ", sectionB1);
        _tempVariables["приложениеВ_описание"]          = string.Join(", ", sectionB2);
        _tempVariables["приложениеВ_корпус"]            = string.Join(", ", caseNames);
        _tempVariables["приложениеВ_названиеОписания"]  = string.Join(", ", descNames);

        // Имена переменных из ветки attachmentC — чтобы работал и её вариант шаблона.
        _tempVariables["ПрилВ1"] = sectionB1.Count > 0 ? string.Join(", ", sectionB1) + "." : "";
        _tempVariables["ПрилВ2"] = sectionB2.Count > 0 ? string.Join(", ", sectionB2) + "." : "";

        static List<string> Clean(IEnumerable<string?> source) => source
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();
    }
}
