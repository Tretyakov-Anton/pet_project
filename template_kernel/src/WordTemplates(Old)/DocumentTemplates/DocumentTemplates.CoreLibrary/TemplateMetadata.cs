namespace DocumentTemplates.CoreLibrary
{
    public class TemplateMetadata : ITemplateMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
