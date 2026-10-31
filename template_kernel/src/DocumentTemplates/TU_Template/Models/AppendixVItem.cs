using System.Text.Json.Serialization;

namespace TU_Template.Models;

/// <summary>
/// Хранит данные единственного выбранного корпуса для Приложения В.
/// Сериализуется вместе с TemplateData.
/// </summary>
public class AppendixVItem
{
    [JsonPropertyName("caseName")]
    public string CaseName { get; set; } = "";

    [JsonPropertyName("unifiedGch")]
    public string UnifiedGch { get; set; } = "";

    [JsonPropertyName("descriptionCode")]
    public string DescriptionCode { get; set; } = "";

    [JsonPropertyName("descriptionName")]
    public string DescriptionName { get; set; } = "";
}
