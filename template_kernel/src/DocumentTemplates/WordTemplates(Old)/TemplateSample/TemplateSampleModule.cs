using Avalonia.Controls;
using TemplateCore;
using TemplateSample.Models;
using TemplateSample.Views;
using TemplatesCore;

namespace TemplateSample
{
    public class TemplateSampleModule : ITemplateModule
    {
        public string Name => "Template Sample";

        public ITemplateData TemplateData { get; }

        public TemplateSampleModule()
        {
            TemplateData = new TemplateData("", 0, false);
        }
    }
}
