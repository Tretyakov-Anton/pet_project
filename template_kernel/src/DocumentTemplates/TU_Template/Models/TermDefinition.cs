using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TU_Template.Models;

public partial class TermDefinition : ObservableObject
{
    [JsonPropertyName("term")]
    public string Term { get; set; } = "";
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "";
    
    [JsonPropertyName("definition")]
    public string Definition { get; set; } = "";
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
    
    [JsonPropertyName("isCustom")]
    public bool IsCustom { get; set; } = true;
    
    public TermDefinition() { }
    
    public TermDefinition(string term, string symbol, string definition, string category = "")
    {
        Term = term;
        Symbol = symbol;
        Definition = definition;
        Category = category;
    }
}