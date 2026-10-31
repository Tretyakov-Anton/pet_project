using System.Collections.Generic;
using System.Linq;
using Template_TU.Models;
using Xceed.Words.NET;

namespace Template_TU.Services.DocumentProcessing;

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

    //16.04.2025// Маслаков Артем
    //          !! Приложение по какой-то причине не полностью заполняет поля шаблона.
    //          !! Приложение не создает таблицы вообще
    //
    //             Чтобы разобраться в проекте и найти места с ошибками буду поочередно в каждом методе разбираться
    //             Нужно найти место, где считываются поля из файла а также места где заполняются поля и таблицы
    public void Process()// Главный метод обработки шаблона.
    {
        _tempVariables = new();
        InitializeGeneralVariables();

        ProcessConditions();

        ProcessSpecialBlockVariables();

        //ProcessConditionsInTables();

        ProcessTempVariables();
        ProcessVariables();
    }

    public void InitializeGeneralVariables()
    {
        var names = string.Join(", ", ((IList<Element>)_data.Elements).Select(e => e.Name));
        _tempVariables["названия"] = names;

        _tempVariables["к1"] = $"{_data.Groups.Count}";
        _tempVariables["к2"] = $"{_data.Groups.Count + 1}";
        _tempVariables["к3"] = $"{_data.Groups.Count * 2}";
        _tempVariables["к4"] = $"{_data.Groups.Count + 2}";
        _tempVariables["к5"] = $"{_data.Groups.Count + 2}";

        int uniqueParameterCount = _data.Groups.SelectMany(group => group.ElectricalParameters) // Flatten the list of parameters
                                               .Select(parameter => parameter.Name) // Select the Name property
                                               .Distinct() // Get unique names
                                               .Count(); // Count the unique names
        _tempVariables["п1"] = $"{uniqueParameterCount}";
        
        _tempVariables["д1"] = $"{88}"; // !!! пока не реализуемо. Нужно сначала создать модель и UI для создания дополнений
    }
}