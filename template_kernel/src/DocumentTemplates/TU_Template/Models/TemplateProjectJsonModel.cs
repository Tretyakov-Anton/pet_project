using System.Text.Json.Serialization;

namespace TU_Template.Models;

public class TemplateProjectJsonModel
{
    /// <summary>Человекочитаемое имя проекта (используется как имя файла по умолчанию).</summary>
    public string? Name { get; set; }

    /// <summary>Путь к исходному файлу шаблона (.docx), если был выбран.</summary>
    public string? SourcePath { get; set; }

    /// <summary>Путь к папке для сохранения результатов, если была выбрана.</summary>
    public string? ResultPath { get; set; }

    /// <summary>Все данные шаблона (переменные, группы, элементы и т.д.).</summary>
    public TemplateDataJsonModel TemplateData { get; set; } = new();
}

